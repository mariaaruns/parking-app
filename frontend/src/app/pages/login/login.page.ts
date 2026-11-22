import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { VEHICLE_META, VehicleType } from '../../core/models';

interface DemoAccount {
  label: string;
  role: string;
  email: string;
  password: string;
  accent: string;
}

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './login.page.html',
})
export class LoginPage {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly vehicleTypes: VehicleType[] = ['Bike', 'Car', 'HeavyVehicle'];
  readonly meta = VEHICLE_META;
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]],
  });

  readonly demos: DemoAccount[] = [
    {
      label: 'Super Admin',
      role: 'SuperAdmin',
      email: 'superadmin@parking.app',
      password: 'Admin@1234',
      accent: 'bg-violet-100 text-violet-700 border border-violet-200',
    },
    {
      label: 'Branch Admin',
      role: 'Admin',
      email: 'admin1@parking.app',
      password: 'Admin@1234',
      accent: 'bg-sky-100 text-sky-700 border border-sky-200',
    },
    {
      label: 'Gate Staff',
      role: 'Staff',
      email: 'staff1@parking.app',
      password: 'Staff@1234',
      accent: 'bg-blue-100 text-blue-700 border border-blue-200',
    },
  ];

  constructor() {
    if (this.auth.isLoggedIn()) {
      void this.router.navigateByUrl(this.auth.homeRoute());
    }
  }

  zoneFor(type: VehicleType): string {
    return type === 'Bike' ? 'B' : type === 'Car' ? 'C' : 'H';
  }

  fill(demo: DemoAccount): void {
    this.form.setValue({ email: demo.email, password: demo.password });
    this.error.set(null);
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    const { email, password } = this.form.getRawValue();
    this.auth.login(email, password).subscribe({
      next: () => {
        this.loading.set(false);
        void this.router.navigateByUrl(this.auth.homeRoute());
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(
          err?.error?.detail ?? err?.error?.title ?? 'Unable to sign in. Check the API is running.',
        );
      },
    });
  }
}
