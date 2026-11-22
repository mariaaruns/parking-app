using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.DTOs.Public;
using ParkingApp.Application.Interfaces;
using ParkingApp.Domain.Enums;
using ParkingApp.Infrastructure.Persistence;

namespace ParkingApp.Infrastructure.Services;

public class PublicService(AppDbContext context, IRentCalculatorService rentCalculator) : IPublicService
{
    public async Task<PublicTicketDto> GetTicketByTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new KeyNotFoundException("Ticket token is required.");

        var session = await context.ParkingSessions
            .Include(s => s.Slot).ThenInclude(sl => sl.Zone)
            .Include(s => s.Branch)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.PublicToken == token)
            ?? throw new KeyNotFoundException("Ticket not found.");

        var isActive = session.Status == SessionStatus.Active;
        var rent = isActive
            ? await rentCalculator.CalculateRentAsync(
                session.BranchId, session.VehicleType, session.EntryTime)
            : session.CalculatedRent;

        return new PublicTicketDto(
            PublicToken: session.PublicToken,
            SlotCode: session.Slot.SlotCode,
            ZoneName: session.Slot.Zone.Name,
            VehicleNumber: session.VehicleNumber,
            VehicleType: session.VehicleType,
            DriverPhone: session.DriverPhone,
            EntryTime: session.EntryTime,
            ExitTime: session.ExitTime,
            Status: session.Status,
            ElapsedDuration: rentCalculator.FormatDuration(session.EntryTime, session.ExitTime),
            CurrentRent: rent,
            PaymentMode: session.PaymentMode,
            PaymentStatus: session.PaymentStatus,
            BranchName: session.Branch.Name,
            BranchAddress: session.Branch.Address
        );
    }
}
