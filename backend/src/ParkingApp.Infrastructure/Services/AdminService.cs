using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.DTOs.Admin;
using ParkingApp.Application.Interfaces;
using ParkingApp.Domain.Entities;
using ParkingApp.Domain.Enums;
using ParkingApp.Infrastructure.Persistence;

namespace ParkingApp.Infrastructure.Services;

public class AdminService(AppDbContext context) : IAdminService
{
    public async Task<AnalyticsSummaryDto> GetAnalyticsAsync(int branchId)
    {
        var today = DateTime.UtcNow.Date;
        var weekStart = today.AddDays(-6);

        var sessions = await context.ParkingSessions
            .Where(s => s.BranchId == branchId)
            .AsNoTracking()
            .ToListAsync();

        var paid = sessions.Where(s => s.PaymentStatus == PaymentStatus.Paid).ToList();
        var totalRevenue = paid.Sum(s => s.CalculatedRent);
        var todayRevenue = paid
            .Where(s => (s.ExitTime ?? s.EntryTime) >= today)
            .Sum(s => s.CalculatedRent);

        var byType = paid
            .GroupBy(s => s.VehicleType)
            .Select(g => new RevenueByVehicleTypeDto(g.Key, g.Sum(s => s.CalculatedRent), g.Count()))
            .OrderBy(x => x.VehicleType)
            .ToList();

        // Ensure all three vehicle types appear in charts even with zero revenue.
        foreach (var type in Enum.GetValues<VehicleType>())
        {
            if (byType.All(x => x.VehicleType != type))
                byType.Add(new RevenueByVehicleTypeDto(type, 0, 0));
        }

        var last7 = Enumerable.Range(0, 7)
            .Select(offset => weekStart.AddDays(offset))
            .Select(day => new DailyRevenueDto(
                day,
                paid.Where(s => (s.ExitTime ?? s.EntryTime).Date == day).Sum(s => s.CalculatedRent)))
            .ToList();

        var slots = await context.ParkingSlots
            .Include(s => s.Zone)
            .Where(s => s.Zone.BranchId == branchId)
            .AsNoTracking()
            .ToListAsync();

        return new AnalyticsSummaryDto(
            TotalRevenue: totalRevenue,
            TodayRevenue: todayRevenue,
            ActiveSessions: sessions.Count(s => s.Status == SessionStatus.Active),
            CompletedSessions: sessions.Count(s => s.Status == SessionStatus.Completed),
            TotalSlots: slots.Count,
            OccupiedSlots: slots.Count(s => s.Status == SlotStatus.Occupied),
            RevenueByType: byType.OrderBy(x => x.VehicleType),
            Last7DaysRevenue: last7
        );
    }

    public async Task<IEnumerable<AuditLogDto>> GetAuditLogsForSessionAsync(int sessionId, int? branchScope)
    {
        if (branchScope.HasValue &&
            !await context.AllSessions.AnyAsync(s => s.Id == sessionId && s.BranchId == branchScope.Value))
            throw new KeyNotFoundException($"Session {sessionId} not found");

        var logs = await context.AuditLogs
            .Include(a => a.EditedByUser)
            .Where(a => a.ParkingSessionId == sessionId)
            .OrderByDescending(a => a.Timestamp)
            .AsNoTracking()
            .ToListAsync();

        return logs.Select(a => new AuditLogDto(
            a.Id,
            a.ParkingSessionId,
            a.EditedByUser?.FullName ?? "System",
            a.Action,
            a.OldValuesJson,
            a.NewValuesJson,
            a.Reason,
            a.Timestamp));
    }

    public async Task<IEnumerable<RateCardDto>> GetRateCardsAsync(int branchId)
    {
        var cards = await context.RateCards
            .Where(r => r.BranchId == branchId)
            .OrderBy(r => r.VehicleType)
            .AsNoTracking()
            .ToListAsync();

        return cards.Select(MapRateCard);
    }

    public async Task<RateCardDto> CreateRateCardAsync(CreateRateCardDto dto)
    {
        ValidateRateCard(dto.GracePeriodMinutes, dto.BaseHours, dto.BaseRate, dto.AdditionalHourlyRate, dto.DailyMaxCap);

        if (!await context.Branches.AnyAsync(b => b.Id == dto.BranchId))
            throw new KeyNotFoundException($"Branch {dto.BranchId} not found.");

        if (await context.RateCards.AnyAsync(r => r.BranchId == dto.BranchId && r.VehicleType == dto.VehicleType))
            throw new InvalidOperationException($"A rate card for {dto.VehicleType} already exists for this branch.");

        var card = new RateCard
        {
            BranchId = dto.BranchId,
            VehicleType = dto.VehicleType,
            GracePeriodMinutes = dto.GracePeriodMinutes,
            BaseHours = dto.BaseHours,
            BaseRate = dto.BaseRate,
            AdditionalHourlyRate = dto.AdditionalHourlyRate,
            DailyMaxCap = dto.DailyMaxCap
        };
        context.RateCards.Add(card);
        await context.SaveChangesAsync();
        return MapRateCard(card);
    }

    public async Task<RateCardDto> UpdateRateCardAsync(
        int rateCardId, UpdateRateCardDto dto, int? branchScope)
    {
        ValidateRateCard(dto.GracePeriodMinutes, dto.BaseHours, dto.BaseRate, dto.AdditionalHourlyRate, dto.DailyMaxCap);

        var card = await context.RateCards.FirstOrDefaultAsync(r =>
                r.Id == rateCardId && (!branchScope.HasValue || r.BranchId == branchScope.Value))
            ?? throw new KeyNotFoundException($"Rate card {rateCardId} not found.");

        card.GracePeriodMinutes = dto.GracePeriodMinutes;
        card.BaseHours = dto.BaseHours;
        card.BaseRate = dto.BaseRate;
        card.AdditionalHourlyRate = dto.AdditionalHourlyRate;
        card.DailyMaxCap = dto.DailyMaxCap;

        await context.SaveChangesAsync();
        return MapRateCard(card);
    }

    public async Task DeleteRateCardAsync(int rateCardId, int? branchScope)
    {
        var card = await context.RateCards.FirstOrDefaultAsync(r =>
                r.Id == rateCardId && (!branchScope.HasValue || r.BranchId == branchScope.Value))
            ?? throw new KeyNotFoundException($"Rate card {rateCardId} not found.");

        var hasLiveSessions = await context.ParkingSessions.AnyAsync(
            s => s.BranchId == card.BranchId &&
                 s.VehicleType == card.VehicleType &&
                 s.Status == SessionStatus.Active);

        if (hasLiveSessions)
            throw new InvalidOperationException(
                "Cannot delete a rate card while vehicles of this type are parked in the branch.");

        context.RateCards.Remove(card);
        await context.SaveChangesAsync();
    }

    public async Task<IEnumerable<ParkingZoneDto>> GetZonesAsync(int branchId)
    {
        return await context.ParkingZones
            .Where(z => z.BranchId == branchId)
            .OrderBy(z => z.VehicleType).ThenBy(z => z.Name)
            .Select(z => new ParkingZoneDto(z.Id, z.BranchId, z.Name, z.VehicleType, z.Slots.Count))
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<ParkingZoneDto> CreateZoneAsync(CreateZoneDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("Zone name is required.");
        if (!await context.Branches.AnyAsync(b => b.Id == dto.BranchId))
            throw new KeyNotFoundException($"Branch {dto.BranchId} not found.");

        var name = dto.Name.Trim();
        if (await context.ParkingZones.AnyAsync(z => z.BranchId == dto.BranchId && z.Name == name))
            throw new InvalidOperationException($"Zone {name} already exists in this branch.");

        var zone = new ParkingZone
        {
            BranchId = dto.BranchId,
            Name = name,
            VehicleType = dto.VehicleType
        };
        context.ParkingZones.Add(zone);
        await context.SaveChangesAsync();
        return new ParkingZoneDto(zone.Id, zone.BranchId, zone.Name, zone.VehicleType, 0);
    }

    public async Task DeleteZoneAsync(int zoneId, int? branchScope)
    {
        var zone = await context.ParkingZones
            .Include(z => z.Slots)
            .FirstOrDefaultAsync(z =>
                z.Id == zoneId && (!branchScope.HasValue || z.BranchId == branchScope.Value))
            ?? throw new KeyNotFoundException($"Zone {zoneId} not found.");

        if (zone.Slots.Any(s => s.Status == SlotStatus.Occupied) ||
            await context.ParkingSessions.AnyAsync(s => s.Status == SessionStatus.Active && s.Slot.ZoneId == zoneId))
            throw new InvalidOperationException("Cannot delete a zone that has occupied slots or active sessions.");

        context.ParkingZones.Remove(zone);
        await context.SaveChangesAsync();
    }

    public async Task<IEnumerable<ParkingSlotDto>> GetSlotsAsync(int zoneId, int? branchScope)
    {
        return await context.ParkingSlots
            .Where(s => s.ZoneId == zoneId && (!branchScope.HasValue || s.Zone.BranchId == branchScope.Value))
            .OrderBy(s => s.SlotCode)
            .Select(s => new ParkingSlotDto(s.Id, s.ZoneId, s.SlotCode, s.Status))
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<ParkingSlotDto> CreateSlotAsync(CreateSlotDto dto, int? branchScope)
    {
        if (string.IsNullOrWhiteSpace(dto.SlotCode))
            throw new ArgumentException("Slot code is required.");

        if (!await context.ParkingZones.AnyAsync(z =>
                z.Id == dto.ZoneId && (!branchScope.HasValue || z.BranchId == branchScope.Value)))
            throw new KeyNotFoundException($"Zone {dto.ZoneId} not found.");

        var code = dto.SlotCode.Trim().ToUpperInvariant();
        if (await context.ParkingSlots.AnyAsync(s => s.ZoneId == dto.ZoneId && s.SlotCode == code))
            throw new InvalidOperationException($"Slot {code} already exists in this zone.");

        var slot = new ParkingSlot { ZoneId = dto.ZoneId, SlotCode = code, Status = SlotStatus.Available };
        context.ParkingSlots.Add(slot);
        await context.SaveChangesAsync();
        return new ParkingSlotDto(slot.Id, slot.ZoneId, slot.SlotCode, slot.Status);
    }

    public async Task<ParkingSlotDto> UpdateSlotStatusAsync(
        int slotId, UpdateSlotStatusDto dto, int? branchScope)
    {
        var slot = await context.ParkingSlots
            .Include(s => s.Zone)
            .FirstOrDefaultAsync(s =>
                s.Id == slotId && (!branchScope.HasValue || s.Zone.BranchId == branchScope.Value))
            ?? throw new KeyNotFoundException($"Slot {slotId} not found.");

        var hasActiveSession = await context.ParkingSessions
            .AnyAsync(s => s.SlotId == slotId && s.Status == SessionStatus.Active);

        if (hasActiveSession && dto.Status != SlotStatus.Occupied)
            throw new InvalidOperationException("This slot has an active session. Check the vehicle out first.");
        if (!hasActiveSession && dto.Status == SlotStatus.Occupied)
            throw new InvalidOperationException("Use check-in to occupy a slot.");

        slot.Status = dto.Status;
        await context.SaveChangesAsync();
        return new ParkingSlotDto(slot.Id, slot.ZoneId, slot.SlotCode, slot.Status);
    }

    public async Task DeleteSlotAsync(int slotId, int? branchScope)
    {
        var slot = await context.ParkingSlots
            .Include(s => s.Zone)
            .FirstOrDefaultAsync(s =>
                s.Id == slotId && (!branchScope.HasValue || s.Zone.BranchId == branchScope.Value))
            ?? throw new KeyNotFoundException($"Slot {slotId} not found.");

        if (await context.ParkingSessions.AnyAsync(s => s.SlotId == slotId && s.Status == SessionStatus.Active))
            throw new InvalidOperationException("Cannot delete a slot with an active session.");

        context.ParkingSlots.Remove(slot);
        await context.SaveChangesAsync();
    }

    public async Task<IEnumerable<StaffReconciliationDto>> GetShiftReconciliationAsync(int branchId, DateTime date)
    {
        var start = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
        var end = start.AddDays(1);

        var sessions = await context.ParkingSessions
            .Where(s => s.BranchId == branchId &&
                        s.Status == SessionStatus.Completed &&
                        s.PaymentStatus == PaymentStatus.Paid &&
                        s.ExitTime >= start && s.ExitTime < end &&
                        s.CheckedOutByUserId != null)
            .AsNoTracking()
            .ToListAsync();

        var staff = await context.AppUsers
            .Where(u => u.BranchId == branchId && u.Role == UserRole.Staff)
            .OrderBy(u => u.FullName)
            .AsNoTracking()
            .ToListAsync();

        return staff.Select(u =>
        {
            var mine = sessions.Where(s => s.CheckedOutByUserId == u.Id).ToList();
            var cash = mine.Where(s => s.PaymentMode == PaymentMode.Cash).Sum(s => s.CalculatedRent);
            var upi = mine.Where(s => s.PaymentMode == PaymentMode.UPI).Sum(s => s.CalculatedRent);
            var card = mine.Where(s => s.PaymentMode == PaymentMode.Card).Sum(s => s.CalculatedRent);
            return new StaffReconciliationDto(
                u.Id, u.FullName, mine.Count, cash, upi, card, cash + upi + card);
        });
    }

    public async Task<IEnumerable<AdminUserDto>> GetAdminUsersAsync(int? branchId = null)
    {
        var query = context.AppUsers
            .Include(u => u.Branch)
            .Where(u => u.Role == UserRole.Admin || u.Role == UserRole.Staff);

        if (branchId.HasValue)
            query = query.Where(u => u.BranchId == branchId.Value);

        var users = await query.OrderBy(u => u.Role).ThenBy(u => u.FullName).AsNoTracking().ToListAsync();
        return users.Select(MapUser);
    }

    public async Task<AdminUserDto> CreateAdminUserAsync(CreateAdminUserDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FullName) || string.IsNullOrWhiteSpace(dto.Email))
            throw new ArgumentException("Full name and email are required.");
        if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 8)
            throw new ArgumentException("Password must be at least 8 characters.");

        var email = dto.Email.Trim().ToLowerInvariant();
        if (await context.AppUsers.AnyAsync(u => u.Email == email))
            throw new InvalidOperationException("A user with this email already exists.");
        if (!await context.Branches.AnyAsync(b => b.Id == dto.BranchId))
            throw new KeyNotFoundException($"Branch {dto.BranchId} not found.");

        var user = new AppUser
        {
            BranchId = dto.BranchId,
            FullName = dto.FullName.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = UserRole.Admin,
            IsActive = true
        };
        context.AppUsers.Add(user);
        await context.SaveChangesAsync();
        await context.Entry(user).Reference(u => u.Branch).LoadAsync();
        return MapUser(user);
    }

    public async Task<AdminUserDto> UpdateUserStatusAsync(int userId, UpdateUserStatusDto dto)
    {
        var user = await context.AppUsers.Include(u => u.Branch).FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new KeyNotFoundException($"User {userId} not found.");

        if (user.Role == UserRole.SuperAdmin)
            throw new InvalidOperationException("SuperAdmin accounts cannot be modified here.");

        user.IsActive = dto.IsActive;
        await context.SaveChangesAsync();
        return MapUser(user);
    }

    private static void ValidateRateCard(
        int grace, decimal baseHours, decimal baseRate, decimal extra, decimal cap)
    {
        if (grace < 0) throw new ArgumentException("Grace period cannot be negative.");
        if (baseHours <= 0) throw new ArgumentException("Base hours must be greater than zero.");
        if (baseRate < 0 || extra < 0 || cap <= 0)
            throw new ArgumentException("Rates must be positive values.");
        if (cap < baseRate)
            throw new ArgumentException("Daily cap cannot be lower than the base rate.");
    }

    private static RateCardDto MapRateCard(RateCard r) => new(
        r.Id, r.BranchId, r.VehicleType, r.GracePeriodMinutes,
        r.BaseHours, r.BaseRate, r.AdditionalHourlyRate, r.DailyMaxCap);

    private static AdminUserDto MapUser(AppUser u) => new(
        u.Id, u.BranchId, u.Branch?.Name, u.FullName, u.Email, u.Role, u.IsActive, u.CreatedAt);
}
