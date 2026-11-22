export type Role = 'SuperAdmin' | 'Admin' | 'Staff';
export type VehicleType = 'Bike' | 'Car' | 'HeavyVehicle';
export type SlotStatus = 'Available' | 'Occupied' | 'Reserved' | 'Maintenance';
export type SessionStatus = 'Active' | 'Completed';
export type PaymentMode = 'Cash' | 'UPI' | 'Card';
export type PaymentStatus = 'Pending' | 'Paid';

export interface LoginResponse {
  token: string;
  userId: number;
  fullName: string;
  role: Role;
  branchId: number | null;
  branchName: string | null;
  expiresAt: string;
}

export interface AuthUser {
  token: string;
  userId: number;
  fullName: string;
  role: Role;
  branchId: number | null;
  branchName: string | null;
  expiresAt: string;
}

export interface SlotStatusDto {
  slotId: number;
  slotCode: string;
  status: SlotStatus;
}

export interface ZoneStatusDto {
  zoneId: number;
  zoneName: string;
  vehicleType: VehicleType;
  totalSlots: number;
  availableSlots: number;
  slots: SlotStatusDto[];
}

export interface BranchSlotStatusDto {
  branchId: number;
  branchName: string;
  zones: ZoneStatusDto[];
}

export interface CapacityStripDto {
  totalBike: number;
  availableBike: number;
  totalCar: number;
  availableCar: number;
  totalHeavy: number;
  availableHeavy: number;
}

export interface CheckInRequest {
  branchId?: number | null;
  slotId: number;
  vehicleNumber: string;
  vehicleType: VehicleType;
  driverPhone?: string | null;
}

export interface CheckInResponse {
  sessionId: number;
  publicToken: string;
  ticketUrl: string;
  slotCode: string;
  zoneName: string;
  vehicleNumber: string;
  vehicleType: VehicleType;
  driverPhone: string | null;
  entryTime: string;
}

export interface ParkingSessionDto {
  id: number;
  publicToken: string;
  branchId: number;
  branchName: string;
  slotId: number;
  slotCode: string;
  zoneId: number;
  zoneName: string;
  vehicleNumber: string;
  vehicleType: VehicleType;
  driverPhone: string | null;
  entryTime: string;
  exitTime: string | null;
  status: SessionStatus;
  calculatedRent: number;
  paymentMode: PaymentMode | null;
  paymentStatus: PaymentStatus;
  checkedInBy: string;
  checkedOutBy: string | null;
  isDeleted: boolean;
  deletionReason: string | null;
  deletedAt: string | null;
}

export interface EditSessionRequest {
  slotId?: number | null;
  vehicleNumber?: string | null;
  vehicleType?: VehicleType | null;
  driverPhone?: string | null;
  reason?: string | null;
}

export interface CheckoutResponse {
  sessionId: number;
  publicToken: string;
  slotCode: string;
  zoneName: string;
  vehicleNumber: string;
  entryTime: string;
  exitTime: string;
  duration: string;
  finalRent: number;
  paymentMode: PaymentMode;
}

export interface RentPreviewDto {
  currentRent: number;
  elapsedDuration: string;
  isGracePeriod: boolean;
}

export interface PublicTicketDto {
  publicToken: string;
  slotCode: string;
  zoneName: string;
  vehicleNumber: string;
  vehicleType: VehicleType;
  driverPhone: string | null;
  entryTime: string;
  exitTime: string | null;
  status: SessionStatus;
  elapsedDuration: string;
  currentRent: number;
  paymentMode: PaymentMode | null;
  paymentStatus: PaymentStatus;
  branchName: string;
  branchAddress: string;
}

export interface RateCardDto {
  id: number;
  branchId: number;
  vehicleType: VehicleType;
  gracePeriodMinutes: number;
  baseHours: number;
  baseRate: number;
  additionalHourlyRate: number;
  dailyMaxCap: number;
}

export interface ParkingZoneDto {
  id: number;
  branchId: number;
  name: string;
  vehicleType: VehicleType;
  totalSlots: number;
}

export interface ParkingSlotDto {
  id: number;
  zoneId: number;
  slotCode: string;
  status: SlotStatus;
}

export interface AuditLogDto {
  id: number;
  parkingSessionId: number;
  editedBy: string;
  action: 'Updated' | 'SoftDeleted';
  oldValuesJson: string;
  newValuesJson: string;
  reason: string;
  timestamp: string;
}

export interface RevenueByVehicleTypeDto {
  vehicleType: VehicleType;
  totalRevenue: number;
  sessionCount: number;
}

export interface DailyRevenueDto {
  date: string;
  revenue: number;
}

export interface AnalyticsSummaryDto {
  totalRevenue: number;
  todayRevenue: number;
  activeSessions: number;
  completedSessions: number;
  totalSlots: number;
  occupiedSlots: number;
  revenueByType: RevenueByVehicleTypeDto[];
  last7DaysRevenue: DailyRevenueDto[];
}

export interface StaffReconciliationDto {
  staffId: number;
  staffName: string;
  sessionsHandled: number;
  cashCollected: number;
  upiCollected: number;
  cardCollected: number;
  totalCollected: number;
}

export interface BranchDto {
  id: number;
  name: string;
  address: string;
  isActive: boolean;
  createdAt: string;
}

export interface CrossBranchSummaryDto {
  branchId: number;
  branchName: string;
  totalSlots: number;
  occupiedSlots: number;
  occupancyRate: number;
  todayRevenue: number;
  totalRevenue: number;
  activeSessions: number;
}

export interface AdminUserDto {
  id: number;
  branchId: number | null;
  branchName: string | null;
  fullName: string;
  email: string;
  role: Role;
  isActive: boolean;
  createdAt: string;
}

export const VEHICLE_TYPES: VehicleType[] = ['Bike', 'Car', 'HeavyVehicle'];
export const PAYMENT_MODES: PaymentMode[] = ['Cash', 'UPI', 'Card'];

export const VEHICLE_META: Record<VehicleType, { label: string; icon: string; short: string }> = {
  Bike: { label: 'Bikes', icon: '🏍️', short: 'Bike' },
  Car: { label: 'Cars', icon: '🚗', short: 'Car' },
  HeavyVehicle: { label: 'Heavy Vehicles', icon: '🚚', short: 'Heavy' },
};
