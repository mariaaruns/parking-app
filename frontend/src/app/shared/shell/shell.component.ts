import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { Role } from '../../core/models';

interface NavItem {
  label: string;
  short: string;
  path: string;
  icon: string;
  roles: Role[];
}

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, DatePipe],
  templateUrl: './shell.component.html',
})
export class ShellComponent {
  readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly now = signal(new Date());
  readonly roleLabel = computed(() => {
    switch (this.auth.role()) {
      case 'SuperAdmin':
        return 'Super Admin';
      case 'Admin':
        return 'Branch Admin';
      case 'Staff':
        return 'Gate Staff';
      default:
        return '';
    }
  });

  readonly navItems = computed<NavItem[]>(() => {
    const role = this.auth.role();
    const items: NavItem[] = [
      { label: 'Gate Workspace', short: 'Gate', path: '/staff', icon: '🛂', roles: ['Staff'] },
      { label: 'Management', short: 'Admin', path: '/admin', icon: '📊', roles: ['Admin', 'SuperAdmin'] },
      { label: 'Network Control', short: 'Network', path: '/super-admin', icon: '🌐', roles: ['SuperAdmin'] },
    ];
    return items.filter((item) => !!role && item.roles.includes(role));
  });

  constructor() {
    const timer = setInterval(() => this.now.set(new Date()), 1000);
    inject(DestroyRef).onDestroy(() => clearInterval(timer));
  }

  logout(): void {
    this.auth.logout();
    void this.router.navigate(['/login']);
  }
}
