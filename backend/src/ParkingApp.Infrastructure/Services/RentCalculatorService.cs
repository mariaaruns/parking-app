using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.Interfaces;
using ParkingApp.Domain.Enums;
using ParkingApp.Infrastructure.Persistence;

namespace ParkingApp.Infrastructure.Services;

public class RentCalculatorService(AppDbContext context) : IRentCalculatorService
{
    public async Task<decimal> CalculateRentAsync(int branchId, VehicleType vehicleType, DateTime entryTime, DateTime? exitTime = null)
    {
        var rateCard = await context.RateCards
            .FirstOrDefaultAsync(r => r.BranchId == branchId && r.VehicleType == vehicleType)
            ?? throw new InvalidOperationException($"No rate card found for branch {branchId} and vehicle type {vehicleType}");

        var checkTime = exitTime ?? DateTime.UtcNow;
        var totalMinutes = (checkTime - entryTime).TotalMinutes;

        // Grace period: no charge
        if (totalMinutes <= rateCard.GracePeriodMinutes)
            return 0;

        var totalHours = totalMinutes / 60.0;
        decimal rent;

        // Calculate per 24-hour cycle
        var fullDays = (int)(totalHours / 24);
        var remainingHours = totalHours - (fullDays * 24);

        // Charge for full days
        rent = fullDays * rateCard.DailyMaxCap;

        // Charge for remaining hours
        if (remainingHours > 0)
        {
            if (remainingHours <= (double)rateCard.BaseHours)
            {
                rent += rateCard.BaseRate;
            }
            else
            {
                var extraHours = remainingHours - (double)rateCard.BaseHours;
                var extraChargeSlots = (int)Math.Ceiling(extraHours); // Ceil to next hour
                var dailyRent = rateCard.BaseRate + (extraChargeSlots * rateCard.AdditionalHourlyRate);
                rent += Math.Min(dailyRent, rateCard.DailyMaxCap);
            }
        }

        return Math.Round(rent, 2);
    }

    public string FormatDuration(DateTime entryTime, DateTime? exitTime = null)
    {
        var duration = (exitTime ?? DateTime.UtcNow) - entryTime;
        if (duration.TotalHours >= 1)
            return $"{(int)duration.TotalHours}h {duration.Minutes}m";
        return $"{duration.Minutes}m";
    }

    public async Task<bool> IsGracePeriodAsync(int branchId, VehicleType vehicleType, DateTime entryTime)
    {
        var rateCard = await context.RateCards
            .FirstOrDefaultAsync(r => r.BranchId == branchId && r.VehicleType == vehicleType);
        if (rateCard is null) return false;
        return (DateTime.UtcNow - entryTime).TotalMinutes <= rateCard.GracePeriodMinutes;
    }
}
