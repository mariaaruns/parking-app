import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE } from './api.config';
import {
  AdminUserDto,
  AnalyticsSummaryDto,
  AuditLogDto,
  BranchDto,
  BranchSlotStatusDto,
  CapacityStripDto,
  CheckInRequest,
  CheckInResponse,
  CheckoutResponse,
  CrossBranchSummaryDto,
  EditSessionRequest,
  ParkingSessionDto,
  ParkingSlotDto,
  ParkingZoneDto,
  PaymentMode,
  PublicTicketDto,
  RateCardDto,
  RentPreviewDto,
  SessionStatus,
  SlotStatus,
  StaffReconciliationDto,
  VehicleType,
} from './models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);

  private params(obj: Record<string, unknown>): HttpParams {
    let params = new HttpParams();
    for (const [key, value] of Object.entries(obj)) {
      if (value !== null && value !== undefined && value !== '') {
        params = params.set(key, String(value));
      }
    }
    return params;
  }

  // ---------- Auth ----------
  me(): Observable<{ userId: number; fullName: string; role: string; branchId: number | null }> {
    return this.http.get<{ userId: number; fullName: string; role: string; branchId: number | null }>(
      `${API_BASE}/auth/me`,
    );
  }

  // ---------- Parking ----------
  slots(branchId?: number | null): Observable<BranchSlotStatusDto> {
    return this.http.get<BranchSlotStatusDto>(`${API_BASE}/parking/slots`, {
      params: this.params({ branchId }),
    });
  }

  capacity(branchId?: number | null): Observable<CapacityStripDto> {
    return this.http.get<CapacityStripDto>(`${API_BASE}/parking/capacity`, {
      params: this.params({ branchId }),
    });
  }

  checkIn(body: CheckInRequest): Observable<CheckInResponse> {
    return this.http.post<CheckInResponse>(`${API_BASE}/parking/check-in`, body);
  }

  sessions(
    status?: SessionStatus | null,
    includeDeleted = false,
    branchId?: number | null,
  ): Observable<ParkingSessionDto[]> {
    return this.http.get<ParkingSessionDto[]>(`${API_BASE}/parking/sessions`, {
      params: this.params({ status, includeDeleted, branchId }),
    });
  }

  session(id: number): Observable<ParkingSessionDto> {
    return this.http.get<ParkingSessionDto>(`${API_BASE}/parking/${id}`);
  }

  liveRent(id: number): Observable<RentPreviewDto> {
    return this.http.get<RentPreviewDto>(`${API_BASE}/parking/${id}/live-rent`);
  }

  editSession(id: number, body: EditSessionRequest): Observable<ParkingSessionDto> {
    return this.http.put<ParkingSessionDto>(`${API_BASE}/parking/${id}`, body);
  }

  checkout(id: number, paymentMode: PaymentMode): Observable<CheckoutResponse> {
    return this.http.post<CheckoutResponse>(`${API_BASE}/parking/${id}/checkout`, { paymentMode });
  }

  softDeleteSession(id: number, reason: string): Observable<void> {
    return this.http.delete<void>(`${API_BASE}/parking/${id}`, { body: { reason } });
  }

  // ---------- Public ----------
  publicTicket(token: string): Observable<PublicTicketDto> {
    return this.http.get<PublicTicketDto>(`${API_BASE}/public/tickets/${encodeURIComponent(token)}`);
  }

  // ---------- Admin ----------
  analytics(branchId?: number | null): Observable<AnalyticsSummaryDto> {
    return this.http.get<AnalyticsSummaryDto>(`${API_BASE}/admin/analytics`, {
      params: this.params({ branchId }),
    });
  }

  auditForSession(sessionId: number): Observable<AuditLogDto[]> {
    return this.http.get<AuditLogDto[]>(`${API_BASE}/admin/audit/session/${sessionId}`);
  }

  rateCards(branchId?: number | null): Observable<RateCardDto[]> {
    return this.http.get<RateCardDto[]>(`${API_BASE}/admin/ratecards`, {
      params: this.params({ branchId }),
    });
  }

  createRateCard(body: {
    branchId?: number | null;
    vehicleType: VehicleType;
    gracePeriodMinutes: number;
    baseHours: number;
    baseRate: number;
    additionalHourlyRate: number;
    dailyMaxCap: number;
  }): Observable<RateCardDto> {
    return this.http.post<RateCardDto>(`${API_BASE}/admin/ratecards`, body);
  }

  updateRateCard(
    id: number,
    body: {
      gracePeriodMinutes: number;
      baseHours: number;
      baseRate: number;
      additionalHourlyRate: number;
      dailyMaxCap: number;
    },
  ): Observable<RateCardDto> {
    return this.http.put<RateCardDto>(`${API_BASE}/admin/ratecards/${id}`, body);
  }

  deleteRateCard(id: number): Observable<void> {
    return this.http.delete<void>(`${API_BASE}/admin/ratecards/${id}`);
  }

  zones(branchId?: number | null): Observable<ParkingZoneDto[]> {
    return this.http.get<ParkingZoneDto[]>(`${API_BASE}/admin/zones`, {
      params: this.params({ branchId }),
    });
  }

  createZone(body: {
    branchId?: number | null;
    name: string;
    vehicleType: VehicleType;
  }): Observable<ParkingZoneDto> {
    return this.http.post<ParkingZoneDto>(`${API_BASE}/admin/zones`, body);
  }

  deleteZone(id: number): Observable<void> {
    return this.http.delete<void>(`${API_BASE}/admin/zones/${id}`);
  }

  zoneSlots(zoneId: number): Observable<ParkingSlotDto[]> {
    return this.http.get<ParkingSlotDto[]>(`${API_BASE}/admin/zones/${zoneId}/slots`);
  }

  createSlot(body: { zoneId: number; slotCode: string }): Observable<ParkingSlotDto> {
    return this.http.post<ParkingSlotDto>(`${API_BASE}/admin/slots`, body);
  }

  updateSlotStatus(id: number, status: SlotStatus): Observable<ParkingSlotDto> {
    return this.http.put<ParkingSlotDto>(`${API_BASE}/admin/slots/${id}/status`, { status });
  }

  deleteSlot(id: number): Observable<void> {
    return this.http.delete<void>(`${API_BASE}/admin/slots/${id}`);
  }

  reconciliation(date: string, branchId?: number | null): Observable<StaffReconciliationDto[]> {
    return this.http.get<StaffReconciliationDto[]>(`${API_BASE}/admin/reconciliation`, {
      params: this.params({ date, branchId }),
    });
  }

  // ---------- SuperAdmin ----------
  branches(): Observable<BranchDto[]> {
    return this.http.get<BranchDto[]>(`${API_BASE}/superadmin/branches`);
  }

  createBranch(body: { name: string; address: string }): Observable<BranchDto> {
    return this.http.post<BranchDto>(`${API_BASE}/superadmin/branches`, body);
  }

  updateBranch(
    id: number,
    body: { name: string; address: string; isActive: boolean },
  ): Observable<BranchDto> {
    return this.http.put<BranchDto>(`${API_BASE}/superadmin/branches/${id}`, body);
  }

  branchSummary(): Observable<CrossBranchSummaryDto[]> {
    return this.http.get<CrossBranchSummaryDto[]>(`${API_BASE}/superadmin/branches/summary`);
  }

  admins(): Observable<AdminUserDto[]> {
    return this.http.get<AdminUserDto[]>(`${API_BASE}/superadmin/admins`);
  }

  createAdmin(body: {
    branchId: number;
    fullName: string;
    email: string;
    password: string;
  }): Observable<AdminUserDto> {
    return this.http.post<AdminUserDto>(`${API_BASE}/superadmin/admins`, body);
  }

  toggleAdmin(id: number, isActive: boolean): Observable<AdminUserDto> {
    return this.http.put<AdminUserDto>(`${API_BASE}/superadmin/admins/${id}/status`, { isActive });
  }
}
