import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import {
  CapacityStripDto,
  CheckInResponse,
  ParkingSessionDto,
  PaymentMode,
  PAYMENT_MODES,
  RentPreviewDto,
  SlotStatus,
  VEHICLE_META,
  VEHICLE_TYPES,
  VehicleType,
  ZoneStatusDto,
} from '../../core/models';
import { ToastService } from '../../core/toast.service';
import { QrTicketComponent } from '../../shared/qr-ticket/qr-ticket.component';

@Component({
  selector: 'app-staff',
  standalone: true,
  imports: [FormsModule, DatePipe, CurrencyPipe, QrTicketComponent],
  templateUrl: './staff.page.html',
})
export class StaffPage {
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);

  readonly vehicleTypes = VEHICLE_TYPES;
  readonly paymentModes = PAYMENT_MODES;
  readonly meta = VEHICLE_META;

  readonly slots = signal<ZoneStatusDto[]>([]);
  readonly capacity = signal<CapacityStripDto | null>(null);
  readonly sessions = signal<ParkingSessionDto[]>([]);
  readonly loading = signal(false);
  readonly submitting = signal(false);
  readonly error = signal<string | null>(null);

  readonly vehicleType = signal<VehicleType>('Car');
  readonly vehicleNumber = signal('');
  readonly driverPhone = signal('');
  readonly selectedSlotId = signal<number | null>(null);
  readonly ticket = signal<CheckInResponse | null>(null);

  readonly search = signal('');
  readonly typeFilter = signal<VehicleType | 'all'>('all');
  readonly now = signal(new Date());

  readonly editTarget = signal<ParkingSessionDto | null>(null);
  readonly editVehicle = signal('');
  readonly editPhone = signal('');
  readonly editSlotId = signal(0);
  readonly editReason = signal('');
  readonly editSaving = signal(false);

  readonly checkoutTarget = signal<ParkingSessionDto | null>(null);
  readonly rent = signal<RentPreviewDto | null>(null);
  readonly paymentMode = signal<PaymentMode>('Cash');
  readonly checkoutSaving = signal(false);

  readonly zonesForType = computed(() => this.slots().filter((z) => z.vehicleType === this.vehicleType()));

  readonly selectedInfo = computed(() => {
    const id = this.selectedSlotId();
    if (!id) return null;
    for (const zone of this.slots()) {
      const slot = zone.slots.find((s) => s.slotId === id);
      if (slot) return { slotCode: slot.slotCode, zoneName: zone.zoneName };
    }
    return null;
  });

  readonly activeSessions = computed(() => this.sessions().filter((s) => s.status === 'Active'));

  readonly filteredSessions = computed(() => {
    const query = this.search().trim().toLowerCase();
    const type = this.typeFilter();
    return this.activeSessions().filter((s) => {
      const matchesType = type === 'all' || s.vehicleType === type;
      const matchesQuery =
        !query ||
        s.vehicleNumber.toLowerCase().includes(query) ||
        s.slotCode.toLowerCase().includes(query) ||
        s.zoneName.toLowerCase().includes(query) ||
        (s.driverPhone ?? '').includes(query);
      return matchesType && matchesQuery;
    });
  });

  readonly shiftTotal = computed(() => {
    const c = this.capacity();
    return c ? c.totalBike + c.totalCar + c.totalHeavy : 0;
  });

  readonly shiftAvailable = computed(() => {
    const c = this.capacity();
    return c ? c.availableBike + c.availableCar + c.availableHeavy : 0;
  });

  readonly canCheckIn = computed(
    () => !!this.selectedSlotId() && this.vehicleNumber().trim().length >= 4 && !this.submitting(),
  );

  readonly editableSlots = computed(() => {
    const session = this.editTarget();
    if (!session) return [];
    const options = this.slots()
      .filter((z) => z.vehicleType === session.vehicleType)
      .flatMap((z) =>
        z.slots
          .filter((s) => s.status === 'Available' || s.slotId === session.slotId)
          .map((s) => ({
            id: s.slotId,
            label: `${s.slotCode} • ${z.zoneName}${s.slotId === session.slotId ? ' (current)' : ''}`,
          })),
      );
    return options;
  });

  constructor() {
    this.load();
    const timer = setInterval(() => this.now.set(new Date()), 1000);
    inject(DestroyRef).onDestroy(() => clearInterval(timer));
  }

  load(): void {
    const branchId = this.auth.user()?.branchId ?? null;
    this.loading.set(true);
    forkJoin({
      slots: this.api.slots(branchId),
      capacity: this.api.capacity(branchId),
      sessions: this.api.sessions('Active', false, branchId),
    }).subscribe({
      next: ({ slots, capacity, sessions }) => {
        this.slots.set(slots.zones);
        this.capacity.set(capacity);
        this.sessions.set(sessions);
        this.loading.set(false);
        if (this.selectedSlotId() && !this.isStillAvailable(this.selectedSlotId()!)) {
          this.selectedSlotId.set(null);
        }
      },
      error: (err) => {
        this.loading.set(false);
        this.toast.error(err?.error?.detail ?? 'Could not load gate data.');
      },
    });
  }

  total(type: VehicleType): number {
    const c = this.capacity();
    if (!c) return 0;
    return type === 'Bike' ? c.totalBike : type === 'Car' ? c.totalCar : c.totalHeavy;
  }

  available(type: VehicleType): number {
    const c = this.capacity();
    if (!c) return 0;
    return type === 'Bike' ? c.availableBike : type === 'Car' ? c.availableCar : c.availableHeavy;
  }

  pct(type: VehicleType): number {
    const total = this.total(type);
    return total === 0 ? 0 : Math.round((this.available(type) / total) * 100);
  }

  selectType(type: VehicleType): void {
    this.vehicleType.set(type);
    this.selectedSlotId.set(null);
  }

  setPlate(value: string): void {
    this.vehicleNumber.set(value.toUpperCase());
  }

  pickSlot(slotId: number): void {
    this.selectedSlotId.set(slotId);
  }

  slotClass(status: SlotStatus, slotId: number): string {
    if (status === 'Available') {
      return this.selectedSlotId() === slotId
        ? 'border-blue-600 bg-blue-600 text-white shadow-glow scale-[1.03]'
        : 'border-blue-200 bg-blue-50 text-blue-700 hover:border-blue-400 hover:bg-blue-100';
    }
    if (status === 'Occupied') return 'border-slate-200 bg-slate-100 text-slate-400 cursor-not-allowed';
    if (status === 'Reserved') return 'border-amber-200 bg-amber-50 text-amber-700 cursor-not-allowed';
    return 'border-sky-200 bg-sky-50 text-sky-700 cursor-not-allowed';
  }

  elapsed(entry: string): string {
    const diff = Math.max(0, Math.floor((this.now().getTime() - new Date(entry).getTime()) / 1000));
    const h = Math.floor(diff / 3600);
    const m = Math.floor((diff % 3600) / 60);
    const s = diff % 60;
    return h > 0 ? `${h}h ${String(m).padStart(2, '0')}m` : `${m}m ${String(s).padStart(2, '0')}s`;
  }

  checkIn(): void {
    const slotId = this.selectedSlotId();
    if (!slotId) return;

    this.submitting.set(true);
    this.error.set(null);

    this.api
      .checkIn({
        branchId: this.auth.user()?.branchId ?? null,
        slotId,
        vehicleNumber: this.vehicleNumber().trim(),
        vehicleType: this.vehicleType(),
        driverPhone: this.driverPhone().trim() || null,
      })
      .subscribe({
        next: (ticket) => {
          this.submitting.set(false);
          this.ticket.set(ticket);
          this.vehicleNumber.set('');
          this.driverPhone.set('');
          this.selectedSlotId.set(null);
          this.toast.success(`Checked in ${ticket.vehicleNumber} at ${ticket.slotCode}.`);
          this.load();
        },
        error: (err) => {
          this.submitting.set(false);
          this.error.set(err?.error?.detail ?? 'Check-in failed.');
        },
      });
  }

  openEdit(session: ParkingSessionDto): void {
    this.error.set(null);
    this.editTarget.set(session);
    this.editVehicle.set(session.vehicleNumber);
    this.editPhone.set(session.driverPhone ?? '');
    this.editSlotId.set(session.slotId);
    this.editReason.set('');
  }

  saveEdit(): void {
    const session = this.editTarget();
    if (!session) return;

    this.editSaving.set(true);
    this.error.set(null);

    this.api
      .editSession(session.id, {
        slotId: this.editSlotId() !== session.slotId ? this.editSlotId() : null,
        vehicleNumber: this.editVehicle().trim(),
        driverPhone: this.editPhone().trim(),
        reason: this.editReason().trim() || 'Ticket edited at gate',
      })
      .subscribe({
        next: () => {
          this.editSaving.set(false);
          this.editTarget.set(null);
          this.toast.success('Ticket updated. Change written to audit log.');
          this.load();
        },
        error: (err) => {
          this.editSaving.set(false);
          this.error.set(err?.error?.detail ?? 'Update failed.');
        },
      });
  }

  openCheckout(session: ParkingSessionDto): void {
    this.error.set(null);
    this.checkoutTarget.set(session);
    this.rent.set(null);
    this.paymentMode.set('Cash');
    this.api.liveRent(session.id).subscribe({
      next: (preview) => this.rent.set(preview),
      error: () => this.rent.set(null),
    });
  }

  confirmCheckout(): void {
    const session = this.checkoutTarget();
    if (!session) return;

    this.checkoutSaving.set(true);
    this.error.set(null);

    this.api.checkout(session.id, this.paymentMode()).subscribe({
      next: (receipt) => {
        this.checkoutSaving.set(false);
        this.checkoutTarget.set(null);
        this.toast.success(
          `Paid ${receipt.finalRent.toFixed(0)} via ${receipt.paymentMode}. ${receipt.slotCode} is free.`,
        );
        this.load();
      },
      error: (err) => {
        this.checkoutSaving.set(false);
        this.error.set(err?.error?.detail ?? 'Checkout failed.');
      },
    });
  }

  private isStillAvailable(slotId: number): boolean {
    return this.slots().some((zone) =>
      zone.slots.some((slot) => slot.slotId === slotId && slot.status === 'Available'),
    );
  }
}
