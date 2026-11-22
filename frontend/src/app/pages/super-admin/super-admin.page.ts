import { Component, computed, inject, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { AdminUserDto, BranchDto, CrossBranchSummaryDto, VEHICLE_META } from '../../core/models';
import { ToastService } from '../../core/toast.service';

@Component({
  selector: 'app-super-admin',
  standalone: true,
  imports: [FormsModule, DatePipe, CurrencyPipe],
  templateUrl: './super-admin.page.html',
})
export class SuperAdminPage {
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);

  readonly meta = VEHICLE_META;
  readonly loading = signal(false);
  readonly branches = signal<BranchDto[]>([]);
  readonly summary = signal<CrossBranchSummaryDto[]>([]);
  readonly admins = signal<AdminUserDto[]>([]);
  readonly selectedBranch = signal<number | 'all'>('all');

  readonly branchModal = signal(false);
  readonly adminModal = signal(false);
  readonly saving = signal(false);

  readonly branchName = signal('');
  readonly branchAddress = signal('');

  readonly adminBranchId = signal(0);
  readonly adminName = signal('');
  readonly adminEmail = signal('');
  readonly adminPassword = signal('');

  readonly activeBranches = computed(() => this.branches().filter((b) => b.isActive).length);

  readonly filteredSummary = computed(() => {
    const selected = this.selectedBranch();
    return selected === 'all' ? this.summary() : this.summary().filter((r) => r.branchId === selected);
  });

  readonly totals = computed(() => {
    const rows = this.filteredSummary();
    const slots = rows.reduce((sum, r) => sum + r.totalSlots, 0);
    const occupied = rows.reduce((sum, r) => sum + r.occupiedSlots, 0);
    return {
      slots,
      occupied,
      occupancy: slots === 0 ? 0 : Math.round((occupied / slots) * 100),
      active: rows.reduce((sum, r) => sum + r.activeSessions, 0),
      revenue: rows.reduce((sum, r) => sum + r.totalRevenue, 0),
      today: rows.reduce((sum, r) => sum + r.todayRevenue, 0),
    };
  });

  constructor() {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    forkJoin({
      branches: this.api.branches(),
      summary: this.api.branchSummary(),
      admins: this.api.admins(),
    }).subscribe({
      next: ({ branches, summary, admins }) => {
        this.branches.set(branches);
        this.summary.set(summary);
        this.admins.set(admins);
        if (this.adminBranchId() === 0 && branches.length) {
          this.adminBranchId.set(branches[0].id);
        }
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.toast.error(err?.error?.detail ?? 'Could not load the network overview.');
      },
    });
  }

  addressFor(branchId: number): string {
    return this.branches().find((b) => b.id === branchId)?.address ?? '';
  }

  isActive(branchId: number): boolean {
    return this.branches().find((b) => b.id === branchId)?.isActive ?? false;
  }

  initials(name: string): string {
    return name
      .split(' ')
      .filter(Boolean)
      .slice(0, 2)
      .map((p) => p[0]?.toUpperCase())
      .join('');
  }

  toggleBranch(branchId: number): void {
    const branch = this.branches().find((b) => b.id === branchId);
    if (!branch) return;

    this.api
      .updateBranch(branch.id, {
        name: branch.name,
        address: branch.address,
        isActive: !branch.isActive,
      })
      .subscribe({
        next: () => {
          this.toast.success(`${branch.name} ${branch.isActive ? 'paused' : 'reactivated'}.`);
          this.load();
        },
        error: (err) => this.toast.error(err?.error?.detail ?? 'Could not update the branch.'),
      });
  }

  openBranchModal(): void {
    this.branchName.set('');
    this.branchAddress.set('');
    this.branchModal.set(true);
  }

  createBranch(): void {
    if (!this.branchName().trim() || !this.branchAddress().trim()) return;

    this.saving.set(true);
    this.api.createBranch({ name: this.branchName().trim(), address: this.branchAddress().trim() }).subscribe({
      next: () => {
        this.saving.set(false);
        this.branchModal.set(false);
        this.toast.success('Branch created.');
        this.load();
      },
      error: (err) => {
        this.saving.set(false);
        this.toast.error(err?.error?.detail ?? 'Could not create the branch.');
      },
    });
  }

  openAdminModal(): void {
    this.adminName.set('');
    this.adminEmail.set('');
    this.adminPassword.set('');
    if (this.branches().length) this.adminBranchId.set(this.branches()[0].id);
    this.adminModal.set(true);
  }

  createAdmin(): void {
    if (!this.adminName().trim() || !this.adminEmail().trim() || this.adminPassword().length < 8) return;

    this.saving.set(true);
    this.api
      .createAdmin({
        branchId: this.adminBranchId(),
        fullName: this.adminName().trim(),
        email: this.adminEmail().trim(),
        password: this.adminPassword(),
      })
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.adminModal.set(false);
          this.toast.success('Admin account created.');
          this.load();
        },
        error: (err) => {
          this.saving.set(false);
          this.toast.error(err?.error?.detail ?? 'Could not create the admin.');
        },
      });
  }

  toggleAdmin(admin: AdminUserDto): void {
    this.api.toggleAdmin(admin.id, !admin.isActive).subscribe({
      next: () => {
        this.toast.success(`${admin.fullName} ${admin.isActive ? 'disabled' : 'enabled'}.`);
        this.load();
      },
      error: (err) => this.toast.error(err?.error?.detail ?? 'Could not update the admin.'),
    });
  }
}
