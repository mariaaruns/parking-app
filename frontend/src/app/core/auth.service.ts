import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { API_BASE } from './api.config';
import { AuthUser, LoginResponse, Role } from './models';

const STORAGE_KEY = 'parkease.auth';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);

  readonly user = signal<AuthUser | null>(this.readStored());
  readonly token = computed(() => this.user()?.token ?? null);
  readonly role = computed<Role | null>(() => this.user()?.role ?? null);
  readonly isLoggedIn = computed(() => !!this.user());
  readonly initials = computed(() => {
    const name = this.user()?.fullName ?? '';
    return name
      .split(' ')
      .filter(Boolean)
      .slice(0, 2)
      .map((p) => p[0]?.toUpperCase())
      .join('');
  });

  login(email: string, password: string): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>(`${API_BASE}/auth/login`, { email, password })
      .pipe(tap((res) => this.persist(res)));
  }

  logout(): void {
    localStorage.removeItem(STORAGE_KEY);
    this.user.set(null);
  }

  hasRole(...roles: Role[]): boolean {
    const role = this.role();
    return !!role && roles.includes(role);
  }

  homeRoute(): string {
    switch (this.role()) {
      case 'SuperAdmin':
        return '/super-admin';
      case 'Admin':
        return '/admin';
      case 'Staff':
        return '/staff';
      default:
        return '/login';
    }
  }

  private persist(res: LoginResponse): void {
    const user: AuthUser = { ...res };
    localStorage.setItem(STORAGE_KEY, JSON.stringify(user));
    this.user.set(user);
  }

  private readStored(): AuthUser | null {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (!raw) return null;
      const user = JSON.parse(raw) as AuthUser;
      if (!user?.token) return null;
      if (user.expiresAt && new Date(user.expiresAt).getTime() < Date.now()) {
        localStorage.removeItem(STORAGE_KEY);
        return null;
      }
      return user;
    } catch {
      return null;
    }
  }
}
