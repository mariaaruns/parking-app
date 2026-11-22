using System.Security.Claims;
using ParkingApp.Domain.Enums;

namespace ParkingApp.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(raw, out var id)
            ? id
            : throw new UnauthorizedAccessException("User identity is missing from the token.");
    }

    public static UserRole GetRole(this ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(ClaimTypes.Role);
        return Enum.TryParse<UserRole>(raw, true, out var role)
            ? role
            : throw new UnauthorizedAccessException("User role is missing from the token.");
    }

    public static int? GetBranchIdClaim(this ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue("BranchId");
        return int.TryParse(raw, out var id) ? id : null;
    }

    public static int? BranchScope(this ClaimsPrincipal user)
    {
        return user.GetRole() == UserRole.SuperAdmin ? null : user.ResolveBranchId(null);
    }

    /// <summary>
    /// Admins and Staff are always pinned to the branch inside their JWT.
    /// SuperAdmin may pass ?branchId= to inspect any branch.
    /// </summary>
    public static int ResolveBranchId(this ClaimsPrincipal user, int? requestedBranchId)
    {
        var role = user.GetRole();
        var claimBranchId = user.GetBranchIdClaim();

        if (role == UserRole.SuperAdmin)
        {
            var id = requestedBranchId ?? claimBranchId;
            return id ?? throw new ArgumentException("branchId is required for SuperAdmin requests.");
        }

        if (claimBranchId is null)
            throw new UnauthorizedAccessException("This account is not assigned to a branch.");

        // Never trust a branchId supplied by an Admin/Staff client.
        return claimBranchId.Value;
    }
}
