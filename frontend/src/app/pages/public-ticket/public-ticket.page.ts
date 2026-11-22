import { Component, DestroyRef, computed, effect, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import QRCode from 'qrcode';
import { ApiService } from '../../core/api.service';
import { PublicTicketDto, VEHICLE_META } from '../../core/models';

@Component({
  selector: 'app-public-ticket',
  standalone: true,
  imports: [DatePipe],
  templateUrl: './public-ticket.page.html',
})
export class PublicTicketPage {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);

  private readonly token = this.route.snapshot.paramMap.get('token') ?? '';

  readonly meta = VEHICLE_META;
  readonly ticket = signal<PublicTicketDto | null>(null);
  readonly error = signal<string | null>(null);
  readonly now = signal(new Date());
  readonly qr = signal<string | null>(null);

  readonly elapsedLabel = computed(() => {
    const t = this.ticket();
    if (!t) return '00:00:00';
    const end = t.exitTime ? new Date(t.exitTime) : this.now();
    const seconds = Math.max(0, Math.floor((end.getTime() - new Date(t.entryTime).getTime()) / 1000));
    const h = Math.floor(seconds / 3600);
    const m = Math.floor((seconds % 3600) / 60);
    const s = seconds % 60;
    return `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}`;
  });

  constructor() {
    this.fetch(false);

    const ticker = setInterval(() => this.now.set(new Date()), 1000);
    const poller = setInterval(() => {
      if (this.ticket()?.status === 'Active') this.fetch(true);
    }, 30000);

    inject(DestroyRef).onDestroy(() => {
      clearInterval(ticker);
      clearInterval(poller);
    });

    effect(() => {
      const t = this.ticket();
      if (!t) return;
      void QRCode.toDataURL(window.location.href, {
        width: 300,
        margin: 1,
        color: { dark: '#0f172a', light: '#ffffff' },
      })
        .then((data) => this.qr.set(data))
        .catch(() => this.qr.set(null));
    });
  }

  fetch(silent: boolean): void {
    if (!this.token) {
      this.error.set('This link is missing a ticket token.');
      return;
    }

    if (!silent) this.error.set(null);

    this.api.publicTicket(this.token).subscribe({
      next: (ticket) => {
        this.ticket.set(ticket);
        this.error.set(null);
      },
      error: (err) => {
        if (!silent) {
          this.error.set(err?.error?.detail ?? 'This parking pass does not exist or was removed.');
        }
      },
    });
  }

  tokenShort(token: string): string {
    return token.slice(0, 10).toUpperCase();
  }
}
