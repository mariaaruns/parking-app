using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ParkingApp.Domain.Entities;
using ParkingApp.Domain.Enums;
using ParkingApp.Infrastructure.Persistence;

namespace ParkingApp.Infrastructure.Persistence;

public class DbSeeder(AppDbContext context, ILogger<DbSeeder> logger)
{
    public async Task SeedAsync()
    {
        try
        {
            await context.Database.MigrateAsync();

            if (await context.Branches.AnyAsync())
            {
                logger.LogInformation("Database already seeded. Skipping.");
                return;
            }

            logger.LogInformation("Seeding database...");

            // --- Branches ---
            var branch1 = new Branch { Name = "ParkEase Central", Address = "12 MG Road, Bangalore, KA 560001", IsActive = true };
            var branch2 = new Branch { Name = "ParkEase Airport", Address = "Terminal 2, Kempegowda Int'l Airport, KA 562300", IsActive = true };
            await context.Branches.AddRangeAsync(branch1, branch2);
            await context.SaveChangesAsync();

            // --- Users ---
            var superAdmin = new AppUser
            {
                BranchId = null,
                FullName = "Super Administrator",
                Email = "superadmin@parking.app",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@1234"),
                Role = UserRole.SuperAdmin,
                IsActive = true
            };
            var admin1 = new AppUser
            {
                BranchId = branch1.Id,
                FullName = "Ravi Kumar",
                Email = "admin1@parking.app",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@1234"),
                Role = UserRole.Admin,
                IsActive = true
            };
            var admin2 = new AppUser
            {
                BranchId = branch2.Id,
                FullName = "Priya Sharma",
                Email = "admin2@parking.app",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@1234"),
                Role = UserRole.Admin,
                IsActive = true
            };
            var staff1 = new AppUser
            {
                BranchId = branch1.Id,
                FullName = "Arjun Singh",
                Email = "staff1@parking.app",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Staff@1234"),
                Role = UserRole.Staff,
                IsActive = true
            };
            var staff2 = new AppUser
            {
                BranchId = branch1.Id,
                FullName = "Deepa Nair",
                Email = "staff2@parking.app",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Staff@1234"),
                Role = UserRole.Staff,
                IsActive = true
            };
            var staff3 = new AppUser
            {
                BranchId = branch2.Id,
                FullName = "Mohammed Farhan",
                Email = "staff3@parking.app",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Staff@1234"),
                Role = UserRole.Staff,
                IsActive = true
            };
            await context.AppUsers.AddRangeAsync(superAdmin, admin1, admin2, staff1, staff2, staff3);
            await context.SaveChangesAsync();

            // --- Rate Cards Branch 1 ---
            await context.RateCards.AddRangeAsync(
                new RateCard { BranchId = branch1.Id, VehicleType = VehicleType.Bike, GracePeriodMinutes = 10, BaseHours = 2, BaseRate = 20, AdditionalHourlyRate = 10, DailyMaxCap = 100 },
                new RateCard { BranchId = branch1.Id, VehicleType = VehicleType.Car, GracePeriodMinutes = 10, BaseHours = 2, BaseRate = 40, AdditionalHourlyRate = 20, DailyMaxCap = 200 },
                new RateCard { BranchId = branch1.Id, VehicleType = VehicleType.HeavyVehicle, GracePeriodMinutes = 15, BaseHours = 2, BaseRate = 80, AdditionalHourlyRate = 40, DailyMaxCap = 400 },
                // --- Rate Cards Branch 2 ---
                new RateCard { BranchId = branch2.Id, VehicleType = VehicleType.Bike, GracePeriodMinutes = 5, BaseHours = 1, BaseRate = 30, AdditionalHourlyRate = 15, DailyMaxCap = 150 },
                new RateCard { BranchId = branch2.Id, VehicleType = VehicleType.Car, GracePeriodMinutes = 5, BaseHours = 1, BaseRate = 60, AdditionalHourlyRate = 30, DailyMaxCap = 300 },
                new RateCard { BranchId = branch2.Id, VehicleType = VehicleType.HeavyVehicle, GracePeriodMinutes = 10, BaseHours = 1, BaseRate = 120, AdditionalHourlyRate = 60, DailyMaxCap = 600 }
            );
            await context.SaveChangesAsync();

            // --- Zones + Slots Branch 1 ---
            var zoneBike1 = new ParkingZone { BranchId = branch1.Id, Name = "Zone-B", VehicleType = VehicleType.Bike };
            var zoneCar1 = new ParkingZone { BranchId = branch1.Id, Name = "Zone-C", VehicleType = VehicleType.Car };
            var zoneHeavy1 = new ParkingZone { BranchId = branch1.Id, Name = "Zone-H", VehicleType = VehicleType.HeavyVehicle };
            // --- Zones Branch 2 ---
            var zoneBike2 = new ParkingZone { BranchId = branch2.Id, Name = "Zone-B", VehicleType = VehicleType.Bike };
            var zoneCar2 = new ParkingZone { BranchId = branch2.Id, Name = "Zone-C", VehicleType = VehicleType.Car };
            var zoneHeavy2 = new ParkingZone { BranchId = branch2.Id, Name = "Zone-H", VehicleType = VehicleType.HeavyVehicle };

            await context.ParkingZones.AddRangeAsync(zoneBike1, zoneCar1, zoneHeavy1, zoneBike2, zoneCar2, zoneHeavy2);
            await context.SaveChangesAsync();

            // Slots for Branch 1
            var slotsB1 = GenerateSlots(zoneBike1.Id, "B", 10)
                .Concat(GenerateSlots(zoneCar1.Id, "C", 15))
                .Concat(GenerateSlots(zoneHeavy1.Id, "H", 5));

            // Slots for Branch 2
            var slotsB2 = GenerateSlots(zoneBike2.Id, "B", 8)
                .Concat(GenerateSlots(zoneCar2.Id, "C", 12))
                .Concat(GenerateSlots(zoneHeavy2.Id, "H", 4));

            await context.ParkingSlots.AddRangeAsync(slotsB1.Concat(slotsB2));
            await context.SaveChangesAsync();

            // --- Sample Sessions ---
            var bikeSlots = await context.ParkingSlots
                .Include(s => s.Zone)
                .Where(s => s.Zone.BranchId == branch1.Id && s.Zone.VehicleType == VehicleType.Bike)
                .Take(3).ToListAsync();

            var carSlots = await context.ParkingSlots
                .Include(s => s.Zone)
                .Where(s => s.Zone.BranchId == branch1.Id && s.Zone.VehicleType == VehicleType.Car)
                .Take(3).ToListAsync();

            // Mark slots as occupied
            foreach (var slot in bikeSlots.Take(2).Concat(carSlots.Take(2)))
                slot.Status = SlotStatus.Occupied;
            await context.SaveChangesAsync();

            var activeSessions = new List<ParkingSession>
            {
                new() {
                    BranchId = branch1.Id, SlotId = bikeSlots[0].Id,
                    PublicToken = Guid.NewGuid().ToString("N"),
                    VehicleNumber = "KA01AB1234", VehicleType = VehicleType.Bike,
                    DriverPhone = "9876543210",
                    EntryTime = DateTime.UtcNow.AddHours(-1.5),
                    Status = SessionStatus.Active, PaymentStatus = PaymentStatus.Pending,
                    CalculatedRent = 20, CheckedInByUserId = staff1.Id
                },
                new() {
                    BranchId = branch1.Id, SlotId = bikeSlots[1].Id,
                    PublicToken = Guid.NewGuid().ToString("N"),
                    VehicleNumber = "KA02CD5678", VehicleType = VehicleType.Bike,
                    EntryTime = DateTime.UtcNow.AddMinutes(-45),
                    Status = SessionStatus.Active, PaymentStatus = PaymentStatus.Pending,
                    CalculatedRent = 20, CheckedInByUserId = staff1.Id
                },
                new() {
                    BranchId = branch1.Id, SlotId = carSlots[0].Id,
                    PublicToken = Guid.NewGuid().ToString("N"),
                    VehicleNumber = "MH12EF9012", VehicleType = VehicleType.Car,
                    DriverPhone = "9123456789",
                    EntryTime = DateTime.UtcNow.AddHours(-3),
                    Status = SessionStatus.Active, PaymentStatus = PaymentStatus.Pending,
                    CalculatedRent = 80, CheckedInByUserId = staff2.Id
                },
                new() {
                    BranchId = branch1.Id, SlotId = carSlots[1].Id,
                    PublicToken = Guid.NewGuid().ToString("N"),
                    VehicleNumber = "TN09GH3456", VehicleType = VehicleType.Car,
                    EntryTime = DateTime.UtcNow.AddHours(-0.5),
                    Status = SessionStatus.Active, PaymentStatus = PaymentStatus.Pending,
                    CalculatedRent = 40, CheckedInByUserId = staff2.Id
                }
            };

            // Completed session for history
            var completedSlot = bikeSlots[2];
            var completedSession = new ParkingSession
            {
                BranchId = branch1.Id, SlotId = completedSlot.Id,
                PublicToken = Guid.NewGuid().ToString("N"),
                VehicleNumber = "AP16IJ7890", VehicleType = VehicleType.Bike,
                EntryTime = DateTime.UtcNow.AddHours(-5),
                ExitTime = DateTime.UtcNow.AddHours(-2),
                Status = SessionStatus.Completed, PaymentStatus = PaymentStatus.Paid,
                PaymentMode = PaymentMode.UPI, CalculatedRent = 30,
                CheckedInByUserId = staff1.Id, CheckedOutByUserId = staff1.Id
            };

            await context.ParkingSessions.AddRangeAsync(activeSessions);
            await context.ParkingSessions.AddAsync(completedSession);
            await context.SaveChangesAsync();

            logger.LogInformation("Database seeding completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    private static IEnumerable<ParkingSlot> GenerateSlots(int zoneId, string prefix, int count)
    {
        return Enumerable.Range(1, count).Select(i => new ParkingSlot
        {
            ZoneId = zoneId,
            SlotCode = $"{prefix}-{i:D2}",
            Status = SlotStatus.Available
        });
    }
}
