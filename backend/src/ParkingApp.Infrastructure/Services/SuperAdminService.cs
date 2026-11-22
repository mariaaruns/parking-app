using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.DTOs.Admin;
using ParkingApp.Application.DTOs.SuperAdmin;
using ParkingApp.Application.Interfaces;
using ParkingApp.Domain.Entities;
using ParkingApp.Domain.Enums;
using ParkingApp.Infrastructure.Persistence;

namespace ParkingApp.Infrastructure.Services;

public class SuperAdminService(AppDbContext context) : ISuperAdminService
{
    public async Task<IEnumerable<BranchDto>> GetAllBranchesAsync()
    {
        return await context.Branches
            .OrderBy(b => b.Name)
            .Select(b => new BranchDto(b.Id, b.Name, b.Address, b.IsActive, b.CreatedAt))
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<BranchDto> CreateBranchAsync(CreateBranchDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Address))
            throw new ArgumentException("Branch name and address are required.");

        var branch = new Branch
        {
            Name = dto.Name.Trim(),
            Address = dto.Address.Trim(),
            IsActive = true
        };
        context.Branches.Add(branch);
        await context.SaveChangesAsync();
        return new BranchDto(branch.Id, branch.Name, branch.Address, branch.IsActive, branch.CreatedAt);
    }

    public async Task<BranchDto> UpdateBranchAsync(int branchId, UpdateBranchDto dto)
    {
        var branch = await context.Branches.FindAsync(branchId)
            ?? throw new KeyNotFoundException($"Branch {branchId} not found.");

        if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Address))
            throw new ArgumentException("Branch name and address are required.");

        branch.Name = dto.Name.Trim();
        branch.Address = dto.Address.Trim();
        branch.IsActive = dto.IsActive;
        await context.SaveChangesAsync();
        return new BranchDto(branch.Id, branch.Name, branch.Address, branch.IsActive, branch.CreatedAt);
    }

    public async Task<IEnumerable<CrossBranchSummaryDto>> GetCrossBranchSummaryAsync()
    {
        var today = DateTime.UtcNow.Date;
        var branches = await context.Branches.OrderBy(b => b.Name).AsNoTracking().ToListAsync();

        var slots = await context.ParkingSlots
            .Include(s => s.Zone)
            .AsNoTracking()
            .ToListAsync();

        var sessions = await context.ParkingSessions.AsNoTracking().ToListAsync();

        return branches.Select(b =>
        {
            var branchSlots = slots.Where(s => s.Zone.BranchId == b.Id).ToList();
            var total = branchSlots.Count;
            var occupied = branchSlots.Count(s => s.Status == SlotStatus.Occupied);
            var branchSessions = sessions.Where(s => s.BranchId == b.Id).ToList();
            var paid = branchSessions.Where(s => s.PaymentStatus == PaymentStatus.Paid).ToList();

            return new CrossBranchSummaryDto(
                b.Id,
                b.Name,
                total,
                occupied,
                total == 0 ? 0 : Math.Round(occupied * 100m / total, 1),
                paid.Where(s => (s.ExitTime ?? s.EntryTime) >= today).Sum(s => s.CalculatedRent),
                paid.Sum(s => s.CalculatedRent),
                branchSessions.Count(s => s.Status == SessionStatus.Active));
        });
    }

    public async Task<IEnumerable<AdminUserDto>> GetAllAdminsAsync()
    {
        var users = await context.AppUsers
            .Include(u => u.Branch)
            .Where(u => u.Role == UserRole.Admin)
            .OrderBy(u => u.Branch!.Name).ThenBy(u => u.FullName)
            .AsNoTracking()
            .ToListAsync();

        return users.Select(MapUser);
    }

    public async Task<AdminUserDto> CreateAdminAsync(CreateAdminUserDto dto)
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

    public async Task<AdminUserDto> ToggleAdminStatusAsync(int userId, bool isActive)
    {
        var user = await context.AppUsers.Include(u => u.Branch).FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new KeyNotFoundException($"User {userId} not found.");

        if (user.Role != UserRole.Admin)
            throw new InvalidOperationException("Only Admin accounts can be toggled from this endpoint.");

        user.IsActive = isActive;
        await context.SaveChangesAsync();
        return MapUser(user);
    }

    private static AdminUserDto MapUser(AppUser u) => new(
        u.Id, u.BranchId, u.Branch?.Name, u.FullName, u.Email, u.Role, u.IsActive, u.CreatedAt);
}
