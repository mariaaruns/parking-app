using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.DTOs.Parking;
using ParkingApp.Application.Interfaces;
using ParkingApp.Domain.Entities;
using ParkingApp.Domain.Enums;
using ParkingApp.Infrastructure.Persistence;
using System.Text.Json;

namespace ParkingApp.Infrastructure.Services;

public class ParkingService(AppDbContext context, IRentCalculatorService rentCalculator) : IParkingService
{
    public async Task<BranchSlotStatusDto> GetSlotStatusAsync(int branchId)
    {
        var branch = await context.Branches.FindAsync(branchId)
            ?? throw new KeyNotFoundException($"Branch {branchId} not found");

        var zones = await context.ParkingZones
            .Include(z => z.Slots)
            .Where(z => z.BranchId == branchId)
            .AsNoTracking()
            .OrderBy(z => z.VehicleType)
            .ThenBy(z => z.Name)
            .ToListAsync();

        var zoneDtos = zones.Select(z => new ZoneStatusDto(
            ZoneId: z.Id,
            ZoneName: z.Name,
            VehicleType: z.VehicleType,
            TotalSlots: z.Slots.Count,
            AvailableSlots: z.Slots.Count(s => s.Status == SlotStatus.Available),
            Slots: z.Slots
                .OrderBy(s => s.SlotCode)
                .Select(s => new SlotStatusDto(s.Id, s.SlotCode, s.Status))
        ));

        return new BranchSlotStatusDto(branchId, branch.Name, zoneDtos);
    }

    public async Task<CapacityStripDto> GetCapacityStripAsync(int branchId)
    {
        var slots = await context.ParkingSlots
            .Include(s => s.Zone)
            .Where(s => s.Zone.BranchId == branchId)
            .AsNoTracking()
            .ToListAsync();

        return new CapacityStripDto(
            TotalBike: slots.Count(s => s.Zone.VehicleType == VehicleType.Bike),
            AvailableBike: slots.Count(s => s.Zone.VehicleType == VehicleType.Bike && s.Status == SlotStatus.Available),
            TotalCar: slots.Count(s => s.Zone.VehicleType == VehicleType.Car),
            AvailableCar: slots.Count(s => s.Zone.VehicleType == VehicleType.Car && s.Status == SlotStatus.Available),
            TotalHeavy: slots.Count(s => s.Zone.VehicleType == VehicleType.HeavyVehicle),
            AvailableHeavy: slots.Count(s => s.Zone.VehicleType == VehicleType.HeavyVehicle && s.Status == SlotStatus.Available)
        );
    }

    public async Task<CheckInResponseDto> CheckInAsync(CheckInRequestDto request, int userId)
    {
        if (string.IsNullOrWhiteSpace(request.VehicleNumber))
            throw new ArgumentException("Vehicle number is required.");

        var slot = await context.ParkingSlots
            .Include(s => s.Zone)
            .FirstOrDefaultAsync(s => s.Id == request.SlotId)
            ?? throw new KeyNotFoundException($"Slot {request.SlotId} not found");

        if (slot.Zone.VehicleType != request.VehicleType)
            throw new InvalidOperationException(
                $"Slot {slot.SlotCode} belongs to {slot.Zone.VehicleType} zone and cannot take a {request.VehicleType}.");

        if (slot.Zone.BranchId != request.BranchId)
            throw new InvalidOperationException(
                $"Slot {slot.SlotCode} belongs to another branch.");

        if (slot.Status != SlotStatus.Available)
            throw new InvalidOperationException($"Slot {slot.SlotCode} is not available.");

        var vehicleNumber = request.VehicleNumber.Trim().ToUpperInvariant();

        var alreadyParked = await context.ParkingSessions
            .AnyAsync(s => s.VehicleNumber == vehicleNumber && s.Status == SessionStatus.Active);
        if (alreadyParked)
            throw new InvalidOperationException($"Vehicle {vehicleNumber} already has an active parking session.");

        var session = new ParkingSession
        {
            BranchId = slot.Zone.BranchId,
            SlotId = slot.Id,
            PublicToken = GeneratePublicToken(),
            VehicleNumber = vehicleNumber,
            VehicleType = request.VehicleType,
            DriverPhone = string.IsNullOrWhiteSpace(request.DriverPhone) ? null : request.DriverPhone.Trim(),
            EntryTime = DateTime.UtcNow,
            Status = SessionStatus.Active,
            PaymentStatus = PaymentStatus.Pending,
            CalculatedRent = 0,
            CheckedInByUserId = userId
        };

        slot.Status = SlotStatus.Occupied;
        context.ParkingSessions.Add(session);

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException(
                $"Slot {slot.SlotCode} was just booked by another operator. Please pick a different slot.");
        }

        return new CheckInResponseDto(
            SessionId: session.Id,
            PublicToken: session.PublicToken,
            TicketUrl: $"/p/{session.PublicToken}",
            SlotCode: slot.SlotCode,
            ZoneName: slot.Zone.Name,
            VehicleNumber: session.VehicleNumber,
            VehicleType: session.VehicleType,
            DriverPhone: session.DriverPhone,
            EntryTime: session.EntryTime
        );
    }

    public async Task<ParkingSessionDto> GetSessionAsync(
        int sessionId, int userId, UserRole role, int? branchScope)
    {
        var session = await GetSessionWithIncludes(sessionId, branchScope);
        return MapToDto(session);
    }

    public async Task<IEnumerable<ParkingSessionDto>> GetSessionsAsync(
        int branchId, SessionStatus? status, bool includeDeleted = false)
    {
        IQueryable<ParkingSession> query = includeDeleted
            ? context.AllSessions.Where(s => s.BranchId == branchId)
            : context.ParkingSessions.Where(s => s.BranchId == branchId);

        if (status.HasValue)
            query = query.Where(s => s.Status == status.Value);

        var sessions = await query
            .Include(s => s.Slot).ThenInclude(sl => sl.Zone)
            .Include(s => s.Branch)
            .Include(s => s.CheckedInByUser)
            .Include(s => s.CheckedOutByUser)
            .OrderByDescending(s => s.EntryTime)
            .AsNoTracking()
            .ToListAsync();

        return sessions.Select(MapToDto);
    }

    public async Task<ParkingSessionDto> EditSessionAsync(
        int sessionId, EditSessionRequestDto request, int userId, UserRole role, int? branchScope)
    {
        var session = await GetSessionWithIncludes(sessionId, branchScope);

        // Business rule: Staff can never edit a completed ticket.
        if (role == UserRole.Staff && session.Status == SessionStatus.Completed)
            throw new UnauthorizedAccessException("Staff cannot edit completed sessions.");

        var oldValues = new Dictionary<string, object?>
        {
            ["vehicleNumber"] = session.VehicleNumber,
            ["vehicleType"] = session.VehicleType.ToString(),
            ["driverPhone"] = session.DriverPhone,
            ["slotId"] = session.SlotId,
            ["slotCode"] = session.Slot.SlotCode
        };

        var newSlot = session.Slot;

        if (request.SlotId.HasValue && request.SlotId.Value != session.SlotId)
        {
            newSlot = await context.ParkingSlots
                .Include(s => s.Zone)
                .FirstOrDefaultAsync(s => s.Id == request.SlotId.Value)
                ?? throw new KeyNotFoundException($"Slot {request.SlotId} not found");

            if (newSlot.Zone.BranchId != session.BranchId)
                throw new InvalidOperationException("Cannot move a vehicle to a slot in another branch.");
            if (newSlot.Status != SlotStatus.Available)
                throw new InvalidOperationException($"Slot {newSlot.SlotCode} is not available.");
            if (newSlot.Zone.VehicleType != session.VehicleType)
                throw new InvalidOperationException(
                    $"Slot {newSlot.SlotCode} is in a {newSlot.Zone.VehicleType} zone.");
        }

        if (!string.IsNullOrWhiteSpace(request.VehicleNumber))
            session.VehicleNumber = request.VehicleNumber.Trim().ToUpperInvariant();

        if (request.VehicleType.HasValue && request.VehicleType.Value != session.VehicleType)
        {
            if (newSlot.Zone.VehicleType != request.VehicleType.Value)
                throw new InvalidOperationException(
                    $"The selected slot is a {newSlot.Zone.VehicleType} slot. Pick a {request.VehicleType} slot to change vehicle type.");
            session.VehicleType = request.VehicleType.Value;
        }

        if (request.DriverPhone is not null)
            session.DriverPhone = string.IsNullOrWhiteSpace(request.DriverPhone) ? null : request.DriverPhone.Trim();

        if (newSlot.Id != session.SlotId)
        {
            session.Slot.Status = SlotStatus.Available;
            newSlot.Status = SlotStatus.Occupied;
            session.SlotId = newSlot.Id;
            session.Slot = newSlot;
        }

        var newValues = new Dictionary<string, object?>
        {
            ["vehicleNumber"] = session.VehicleNumber,
            ["vehicleType"] = session.VehicleType.ToString(),
            ["driverPhone"] = session.DriverPhone,
            ["slotId"] = session.SlotId,
            ["slotCode"] = session.Slot.SlotCode
        };

        var changedOld = oldValues.Where(kv => !Equals(kv.Value, newValues[kv.Key]))
            .ToDictionary(kv => kv.Key, kv => kv.Value);
        var changedNew = newValues.Where(kv => !Equals(kv.Value, oldValues[kv.Key]))
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        if (changedOld.Count > 0)
        {
            context.AuditLogs.Add(new AuditLog
            {
                ParkingSessionId = sessionId,
                EditedByUserId = userId,
                Action = AuditAction.Updated,
                OldValuesJson = JsonSerializer.Serialize(changedOld),
                NewValuesJson = JsonSerializer.Serialize(changedNew),
                Reason = string.IsNullOrWhiteSpace(request.Reason) ? "Ticket edited" : request.Reason.Trim(),
                Timestamp = DateTime.UtcNow
            });
        }

        await context.SaveChangesAsync();
        return MapToDto(session);
    }

    public async Task<CheckoutResponseDto> CheckoutAsync(
        int sessionId, CheckoutRequestDto request, int userId, int? branchScope)
    {
        var session = await GetSessionWithIncludes(sessionId, branchScope);

        if (session.Status == SessionStatus.Completed)
            throw new InvalidOperationException("Session is already completed.");

        var exitTime = DateTime.UtcNow;
        var finalRent = await rentCalculator.CalculateRentAsync(
            session.BranchId, session.VehicleType, session.EntryTime, exitTime);

        session.ExitTime = exitTime;
        session.Status = SessionStatus.Completed;
        session.CalculatedRent = finalRent;
        session.PaymentMode = request.PaymentMode;
        session.PaymentStatus = PaymentStatus.Paid;
        session.CheckedOutByUserId = userId;

        // Release the slot so the next vehicle can use it.
        session.Slot.Status = SlotStatus.Available;

        await context.SaveChangesAsync();

        return new CheckoutResponseDto(
            SessionId: session.Id,
            PublicToken: session.PublicToken,
            SlotCode: session.Slot.SlotCode,
            ZoneName: session.Slot.Zone.Name,
            VehicleNumber: session.VehicleNumber,
            EntryTime: session.EntryTime,
            ExitTime: exitTime,
            Duration: rentCalculator.FormatDuration(session.EntryTime, exitTime),
            FinalRent: finalRent,
            PaymentMode: request.PaymentMode
        );
    }

    public async Task SoftDeleteAsync(
        int sessionId, SoftDeleteRequestDto request, int userId, int? branchScope)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new ArgumentException("Deletion reason is required.");

        var session = await GetSessionWithIncludes(sessionId, branchScope);

        context.AuditLogs.Add(new AuditLog
        {
            ParkingSessionId = sessionId,
            EditedByUserId = userId,
            Action = AuditAction.SoftDeleted,
            OldValuesJson = JsonSerializer.Serialize(new
            {
                Status = session.Status.ToString(),
                session.VehicleNumber,
                VehicleType = session.VehicleType.ToString(),
                session.SlotId,
                slotCode = session.Slot.SlotCode,
                session.CalculatedRent,
                PaymentStatus = session.PaymentStatus.ToString()
            }),
            NewValuesJson = JsonSerializer.Serialize(new
            {
                Status = "Deleted",
                session.VehicleNumber,
                VehicleType = session.VehicleType.ToString(),
                session.SlotId,
                slotCode = session.Slot.SlotCode,
                session.CalculatedRent,
                PaymentStatus = session.PaymentStatus.ToString(),
                Reason = request.Reason.Trim()
            }),
            Reason = request.Reason.Trim(),
            Timestamp = DateTime.UtcNow
        });

        if (session.Status == SessionStatus.Active)
            session.Slot.Status = SlotStatus.Available;

        session.IsDeleted = true;
        session.DeletedByUserId = userId;
        session.DeletedAt = DateTime.UtcNow;
        session.DeletionReason = request.Reason.Trim();

        await context.SaveChangesAsync();
    }

    public async Task<RentPreviewDto> GetLiveRentAsync(int sessionId, int? branchScope)
    {
        var session = await context.ParkingSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == sessionId && (!branchScope.HasValue || s.BranchId == branchScope.Value))
            ?? throw new KeyNotFoundException($"Session {sessionId} not found");

        var rent = session.Status == SessionStatus.Completed
            ? session.CalculatedRent
            : await rentCalculator.CalculateRentAsync(
                session.BranchId, session.VehicleType, session.EntryTime, session.ExitTime);

        var isGrace = session.Status == SessionStatus.Active &&
                      await rentCalculator.IsGracePeriodAsync(
                          session.BranchId, session.VehicleType, session.EntryTime);

        return new RentPreviewDto(
            CurrentRent: rent,
            ElapsedDuration: rentCalculator.FormatDuration(session.EntryTime, session.ExitTime),
            IsGracePeriod: isGrace
        );
    }

    private async Task<ParkingSession> GetSessionWithIncludes(int sessionId, int? branchScope = null)
    {
        return await context.ParkingSessions
            .Include(s => s.Slot).ThenInclude(sl => sl.Zone)
            .Include(s => s.Branch)
            .Include(s => s.CheckedInByUser)
            .Include(s => s.CheckedOutByUser)
            .FirstOrDefaultAsync(s =>
                s.Id == sessionId && (!branchScope.HasValue || s.BranchId == branchScope.Value))
            ?? throw new KeyNotFoundException($"Session {sessionId} not found");
    }

    private static string GeneratePublicToken()
    {
        // 128-bit random, URL-safe, never sequential.
        return Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N")[..8];
    }

    private static ParkingSessionDto MapToDto(ParkingSession s) => new(
        Id: s.Id,
        PublicToken: s.PublicToken,
        BranchId: s.BranchId,
        BranchName: s.Branch?.Name ?? "",
        SlotId: s.SlotId,
        SlotCode: s.Slot?.SlotCode ?? "",
        ZoneId: s.Slot?.ZoneId ?? 0,
        ZoneName: s.Slot?.Zone?.Name ?? "",
        VehicleNumber: s.VehicleNumber,
        VehicleType: s.VehicleType,
        DriverPhone: s.DriverPhone,
        EntryTime: s.EntryTime,
        ExitTime: s.ExitTime,
        Status: s.Status,
        CalculatedRent: s.CalculatedRent,
        PaymentMode: s.PaymentMode,
        PaymentStatus: s.PaymentStatus,
        CheckedInBy: s.CheckedInByUser?.FullName ?? "",
        CheckedOutBy: s.CheckedOutByUser?.FullName,
        IsDeleted: s.IsDeleted,
        DeletionReason: s.DeletionReason,
        DeletedAt: s.DeletedAt
    );
}
