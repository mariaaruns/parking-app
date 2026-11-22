import { Component, computed, inject, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import {
  AnalyticsSummaryDto,
  AuditLogDto,
  BranchDto,
  ParkingSessionDto,
  ParkingSlotDto,
  ParkingZoneDto,
  RateCardDto,
  SlotStatus,
  StaffReconciliationDto,
  VEHICLE_META,
  VEHICLE_TYPES,
  VehicleType,
} from '../../core/models';
import { ToastService } from '../../core/toast.service';

type AdminView = 'overview' | 'logs' | 'config' | 'reconciliation';
type LogTab = 'Active' | 'Completed' | 'Deleted';

@Component({
  selector: 'app-admin',
  standalone: true,
  imports: [FormsModule, DatePipe, CurrencyPipe],
  templateUrl: './admin.page.html',
})
export class AdminPage {
  readonly auth = inject(AuthService);
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);

  readonly views: { key: AdminView; label: string; icon: string }[] = [
    { key: 'overview', label: 'Overview', icon: '📈' },
    { key: 'logs', label: 'Parking logs', icon: '🧾' },
    { key: 'config', label: 'Configuration', icon: '⚙️' },
    { key: 'reconciliation', label: 'Reconciliation', icon: '🧮' },
  ];
  readonly logTabs: LogTab[] = ['Active', 'Completed', 'Deleted'];
  readonly vehicleTypes = VEHICLE_TYPES;
  readonly meta = VEHICLE_META;

  readonly view = signal<AdminView>('overview');
  readonly logTab = signal<LogTab>('Active');
  readonly loading = signal(false);
  readonly branches = signal<BranchDto[]>([]);
  readonly branchId = signal<number | null>(this.auth.user()?.branchId ?? null);

  readonly analytics = signal<AnalyticsSummaryDto | null>(null);
  readonly sessions = signal<ParkingSessionDto[]>([]);
  readonly search = signal('');
  readonly typeFilter = signal<VehicleType | 'all'>('all');

  readonly auditOpen = signal(false);
  readonly auditLoading = signal(false);
  readonly auditSession = signal<ParkingSessionDto | null>(null);
  readonly auditLogs = signal<AuditLogDto[]>([]);

  readonly deleteTarget = signal<ParkingSessionDto | null>(null);
  readonly deleteReason = signal('');
  readonly deleteSaving = signal(false);

  readonly rateCards = signal<RateCardDto[]>([]);
  readonly rateEdit = signal<RateCardDto | null>(null);
  readonly rateGrace = signal(0);
  readonly rateBaseHours = signal(2);
  readonly rateBaseRate = signal(0);
  readonly rateExtra = signal(0);
  readonly rateCap = signal(0);
  readonly rateSaving = signal(false);

  readonly zones = signal<ParkingZoneDto[]>([]);
  readonly selectedZoneId = signal<number | null>(null);
  readonly zoneSlots = signal<ParkingSlotDto[]>([]);
  readonly newZoneName = signal('');
  readonly newZoneType = signal<VehicleType>('Car');
  readonly newSlotCode = signal('');

  readonly reconDate = signal(new Date().toISOString().slice(0, 10));
  readonly reconciliation = signal<StaffReconciliationDto[]>([]);

  readonly occupancyPct = computed(() => {
    const a = this.analytics();
    if (!a || a.totalSlots === 0) return 0;
    return Math.round((a.occupiedSlots / a.totalSlots) * 100);
  });

  readonly selectedZone = computed(() => this.zones().find((z) => z.id === this.selectedZoneId()) ?? null);

  readonly missingRateTypes = computed(() => {
    const existing = new Set(this.rateCards().map((r) => r.vehicleType));
    return VEHICLE_TYPES.filter((t) => !existing.has(t));
  });

  readonly filteredSessions = computed(() => {
    const tab = this.logTab();
    const query = this.search().trim().toLowerCase();
    const type = this.typeFilter();

    return this.sessions().filter((s) => {
      const inTab =
        tab === 'Deleted'
          ? s.isDeleted
          : !s.isDeleted && s.status === tab;
      const matchesType = type === 'all' || s.vehicleType === type;
      const matchesQuery =
        !query ||
        s.vehicleNumber.toLowerCase().includes(query) ||
        s.slotCode.toLowerCase().includes(query) ||
        s.zoneName.toLowerCase().includes(query) ||
        (s.driverPhone ?? '').includes(query) ||
        s.checkedInBy.toLowerCase().includes(query);
      return inTab && matchesType && matchesQuery;
    });
  });

  constructor() {
    if (this.auth.role() === 'SuperAdmin') {
      this.api.branches().subscribe({
        next: (branches) => {
          this.branches.set(branches);
          if (!this.branchId() && branches.length) this.branchId.set(branches[0].id);
          this.loadAll();
        },
        error: () => this.toast.error('Could not load branches.'),
      });
    } else {
      this.loadAll();
    }
  }

  loadAll(): void {
    this.loading.set(true);
    this.loadSessions();
    this.loadAnalytics();
    this.loadConfig();
    this.loadReconciliation();
  }

  loadSessions(): void {
    this.api.sessions(null, true, this.branchId()).subscribe({
      next: (rows) => {
        this.sessions.set(rows);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.toast.error(err?.error?.detail ?? 'Could not load parking logs.');
      },
    });
  }

  loadAnalytics(): void {
    this.api.analytics(this.branchId()).subscribe({
      next: (data) => this.analytics.set(data),
      error: () => this.analytics.set(null),
    });
  }

  loadConfig(): void {
    this.api.rateCards(this.branchId()).subscribe({
      next: (cards) => this.rateCards.set(cards),
      error: () => this.rateCards.set([]),
    });
    this.api.zones(this.branchId()).subscribe({
      next: (zones) => {
        this.zones.set(zones);
        if (this.selectedZoneId() && !zones.some((z) => z.id === this.selectedZoneId())) {
          this.selectedZoneId.set(null);
          this.zoneSlots.set([]);
        }
      },
      error: () => this.zones.set([]),
    });
  }

  loadReconciliation(): void {
    this.api.reconciliation(this.reconDate(), this.branchId()).subscribe({
      next: (rows) => this.reconciliation.set(rows),
      error: () => this.reconciliation.set([]),
    });
  }

  onBranchChange(id: number): void {
    this.branchId.set(id);
    this.selectedZoneId.set(null);
    this.zoneSlots.set([]);
    this.loadAll();
  }

  countFor(tab: LogTab): number {
    return this.sessions().filter((s) =>
      tab === 'Deleted' ? s.isDeleted : !s.isDeleted && s.status === tab,
    ).length;
  }

  barPct(value: number): number {
    const rows = this.analytics()?.revenueByType ?? [];
    const max = Math.max(1, ...rows.map((r) => r.totalRevenue));
    return Math.round((value / max) * 100);
  }

  dayPct(value: number): number {
    const rows = this.analytics()?.last7DaysRevenue ?? [];
    const max = Math.max(1, ...rows.map((r) => r.revenue));
    return Math.max(2, Math.round((value / max) * 100));
  }

  barColor(type: VehicleType): string {
    return type === 'Bike'
      ? 'bg-gradient-to-r from-blue-600 to-cyan-400'
      : type === 'Car'
        ? 'bg-gradient-to-r from-blue-500 to-indigo-400'
        : 'bg-gradient-to-r from-indigo-600 to-violet-400';
  }

  openAudit(session: ParkingSessionDto): void {
    this.auditSession.set(session);
    this.auditOpen.set(true);
    this.auditLoading.set(true);
    this.auditLogs.set([]);
    this.api.auditForSession(session.id).subscribe({
      next: (logs) => {
        this.auditLogs.set(logs);
        this.auditLoading.set(false);
      },
      error: () => {
        this.auditLoading.set(false);
        this.toast.error('Could not load the audit trail.');
      },
    });
  }

  closeAudit(): void {
    this.auditOpen.set(false);
    this.auditSession.set(null);
    this.auditLogs.set([]);
  }

  entries(json: string): { key: string; value: string }[] {
    try {
      const obj = JSON.parse(json) as Record<string, unknown>;
      return Object.entries(obj).map(([key, value]) => ({
        key,
        value: value === null || value === undefined ? '—' : String(value),
      }));
    } catch {
      return [{ key: 'raw', value: json }];
    }
  }

  valueFor(json: string, key: string): string {
    try {
      const obj = JSON.parse(json) as Record<string, unknown>;
      return obj[key] !== undefined && obj[key] !== null ? String(obj[key]) : '—';
    } catch {
      return '—';
    }
  }

  openDelete(session: ParkingSessionDto): void {
    this.deleteTarget.set(session);
    this.deleteReason.set('');
  }

  confirmDelete(): void {
    const session = this.deleteTarget();
    const reason = this.deleteReason().trim();
    if (!session || !reason) return;

    this.deleteSaving.set(true);
    this.api.softDeleteSession(session.id, reason).subscribe({
      next: () => {
        this.deleteSaving.set(false);
        this.deleteTarget.set(null);
        this.toast.success(`${session.vehicleNumber} soft-deleted and audit-logged.`);
        this.loadAll();
      },
      error: (err) => {
        this.deleteSaving.set(false);
        this.toast.error(err?.error?.detail ?? 'Delete failed.');
      },
    });
  }

  openRate(card: RateCardDto): void {
    this.rateEdit.set(card);
    this.rateGrace.set(card.gracePeriodMinutes);
    this.rateBaseHours.set(card.baseHours);
    this.rateBaseRate.set(card.baseRate);
    this.rateExtra.set(card.additionalHourlyRate);
    this.rateCap.set(card.dailyMaxCap);
  }

  saveRate(): void {
    const card = this.rateEdit();
    if (!card) return;

    this.rateSaving.set(true);
    this.api
      .updateRateCard(card.id, {
        gracePeriodMinutes: this.rateGrace(),
        baseHours: this.rateBaseHours(),
        baseRate: this.rateBaseRate(),
        additionalHourlyRate: this.rateExtra(),
        dailyMaxCap: this.rateCap(),
      })
      .subscribe({
        next: () => {
          this.rateSaving.set(false);
          this.rateEdit.set(null);
          this.toast.success('Rate card updated.');
          this.loadConfig();
        },
        error: (err) => {
          this.rateSaving.set(false);
          this.toast.error(err?.error?.detail ?? 'Could not update the rate card.');
        },
      });
  }

  addRateCard(type: VehicleType): void {
    this.api
      .createRateCard({
        branchId: this.branchId(),
        vehicleType: type,
        gracePeriodMinutes: 10,
        baseHours: 2,
        baseRate: type === 'Bike' ? 20 : type === 'Car' ? 40 : 80,
        additionalHourlyRate: type === 'Bike' ? 10 : type === 'Car' ? 20 : 40,
        dailyMaxCap: type === 'Bike' ? 100 : type === 'Car' ? 200 : 400,
      })
      .subscribe({
        next: () => {
          this.toast.success(`${this.meta[type].short} rate card added.`);
          this.loadConfig();
        },
        error: (err) => this.toast.error(err?.error?.detail ?? 'Could not add the rate card.'),
      });
  }

  removeRateCard(card: RateCardDto): void {
    if (!window.confirm(`Delete the ${card.vehicleType} rate card?`)) return;
    this.api.deleteRateCard(card.id).subscribe({
      next: () => {
        this.toast.success('Rate card removed.');
        this.loadConfig();
      },
      error: (err) => this.toast.error(err?.error?.detail ?? 'Could not delete the rate card.'),
    });
  }

  selectZone(zone: ParkingZoneDto): void {
    this.selectedZoneId.set(zone.id);
    this.api.zoneSlots(zone.id).subscribe({
      next: (slots) => this.zoneSlots.set(slots),
      error: () => this.zoneSlots.set([]),
    });
  }

  createZone(): void {
    const name = this.newZoneName().trim();
    if (!name) return;

    this.api.createZone({ branchId: this.branchId(), name, vehicleType: this.newZoneType() }).subscribe({
      next: () => {
        this.newZoneName.set('');
        this.toast.success('Zone created.');
        this.loadConfig();
      },
      error: (err) => this.toast.error(err?.error?.detail ?? 'Could not create the zone.'),
    });
  }

  removeZone(zone: ParkingZoneDto): void {
    if (!window.confirm(`Delete zone ${zone.name} and all its slots?`)) return;
    this.api.deleteZone(zone.id).subscribe({
      next: () => {
        if (this.selectedZoneId() === zone.id) {
          this.selectedZoneId.set(null);
          this.zoneSlots.set([]);
        }
        this.toast.success('Zone deleted.');
        this.loadConfig();
      },
      error: (err) => this.toast.error(err?.error?.detail ?? 'Could not delete the zone.'),
    });
  }

  addSlot(): void {
    const zone = this.selectedZone();
    const code = this.newSlotCode().trim();
    if (!zone || !code) return;

    this.api.createSlot({ zoneId: zone.id, slotCode: code }).subscribe({
      next: () => {
        this.newSlotCode.set('');
        this.toast.success('Slot added.');
        this.selectZone(zone);
        this.loadConfig();
      },
      error: (err) => this.toast.error(err?.error?.detail ?? 'Could not add the slot.'),
    });
  }

  setSlotStatus(slot: ParkingSlotDto, status: SlotStatus): void {
    this.api.updateSlotStatus(slot.id, status).subscribe({
      next: () => {
        this.toast.success(`${slot.slotCode} set to ${status}.`);
        const zone = this.selectedZone();
        if (zone) this.selectZone(zone);
      },
      error: (err) => {
        this.toast.error(err?.error?.detail ?? 'Could not update slot status.');
        const zone = this.selectedZone();
        if (zone) this.selectZone(zone);
      },
    });
  }

  removeSlot(slot: ParkingSlotDto): void {
    if (!window.confirm(`Delete slot ${slot.slotCode}?`)) return;
    this.api.deleteSlot(slot.id).subscribe({
      next: () => {
        const zone = this.selectedZone();
        if (zone) this.selectZone(zone);
        this.toast.success('Slot deleted.');
      },
      error: (err) => this.toast.error(err?.error?.detail ?? 'Could not delete the slot.'),
    });
  }

  onReconDate(value: string): void {
    this.reconDate.set(value);
    this.loadReconciliation();
  }

  reconTotal(field: keyof StaffReconciliationDto): number {
    return this.reconciliation().reduce((sum, row) => sum + Number(row[field] ?? 0), 0);
  }
}
