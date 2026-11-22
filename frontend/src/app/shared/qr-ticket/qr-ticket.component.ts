import { Component, effect, inject, input, output, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import QRCode from 'qrcode';
import { CheckInResponse, VEHICLE_META } from '../../core/models';
import { ToastService } from '../../core/toast.service';

@Component({
  selector: 'app-qr-ticket',
  standalone: true,
  imports: [DatePipe],
  templateUrl: './qr-ticket.component.html',
})
export class QrTicketComponent {
  readonly ticket = input.required<CheckInResponse>();
  readonly closed = output<void>();

  private readonly toast = inject(ToastService);

  readonly qr = signal<string | null>(null);
  readonly meta = VEHICLE_META;

  constructor() {
    effect(() => {
      const t = this.ticket();
      const url = this.publicUrl();
      void QRCode.toDataURL(url, {
        width: 360,
        margin: 1,
        color: { dark: '#0f172a', light: '#ffffff' },
        errorCorrectionLevel: 'M',
      })
        .then((data) => this.qr.set(data))
        .catch(() => this.qr.set(null));
    });
  }

  publicUrl(): string {
    const token = this.ticket().publicToken;
    return `${window.location.origin}/p/${token}`;
  }

  copy(): void {
    void navigator.clipboard.writeText(this.publicUrl());
    this.toast.success('Public pass link copied to clipboard.');
  }

  whatsapp(): void {
    const text = encodeURIComponent(
      `Your ParkEase parking pass for ${this.ticket().vehicleNumber} (Slot ${this.ticket().slotCode}, Zone ${this.ticket().zoneName}):\n${this.publicUrl()}`,
    );
    window.open(`https://wa.me/?text=${text}`, '_blank');
  }

  printSlip(): void {
    const win = window.open('', '_blank', 'width=420,height=600');
    if (!win) return;
    const t = this.ticket();
    win.document.write(`
      <!doctype html>
      <html>
        <head>
          <title>Parking Slip — ${t.vehicleNumber}</title>
          <style>
            body { font-family: monospace; padding: 18px; color: #000; }
            h1 { font-size: 16px; margin: 0 0 4px; text-transform: uppercase; }
            hr { border: none; border-top: 1px dashed #000; margin: 10px 0; }
            .row { display: flex; justify-content: space-between; margin: 3px 0; font-size: 12px; }
            .slot { font-size: 28px; font-weight: bold; text-align: center; margin: 8px 0; }
            .qr { text-align: center; margin: 10px 0; }
            .qr img { width: 140px; height: 140px; }
          </style>
        </head>
        <body>
          <h1>ParkEase — Entry Slip</h1>
          <p style="margin: 0; font-size: 11px;">Zone ${t.zoneName}</p>
          <hr />
          <div class="slot">${t.slotCode}</div>
          <div class="row"><span>Vehicle:</span><strong>${t.vehicleNumber}</strong></div>
          <div class="row"><span>Type:</span><span>${t.vehicleType}</span></div>
          <div class="row"><span>Entry:</span><span>${new Date(t.entryTime).toLocaleString()}</span></div>
          ${t.driverPhone ? `<div class="row"><span>Phone:</span><span>${t.driverPhone}</span></div>` : ''}
          <hr />
          <div class="qr">${this.qr() ? `<img src="${this.qr()}" />` : ''}</div>
          <p style="font-size: 9px; text-align: center;">Scan or visit: ${this.publicUrl()}</p>
        </body>
      </html>
    `);
    win.document.close();
    win.focus();
    win.print();
  }
}
