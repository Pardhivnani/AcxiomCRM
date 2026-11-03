using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AcxiomCRM.Services.Implementations
{
    public class UserScopeService : IUserScopeService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public UserScopeService(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<ApplicationUser?> GetCurrentUserAsync(ClaimsPrincipal user)
        {
            if (user.Identity == null || !user.Identity.IsAuthenticated)
                return null;

            return await _userManager.GetUserAsync(user);
        }

        public async Task<List<string>?> GetAccessibleUserIdsAsync(ClaimsPrincipal user)
        {
            if (user.IsInRole(UserRoles.Admin))
            {
                return null; // Null indicates full unrestricted access to all records
            }

            var currentUser = await _userManager.GetUserAsync(user);
            if (currentUser == null)
            {
                return new List<string>(); // No access
            }

            if (user.IsInRole(UserRoles.Manager))
            {
                // Manager can access own records + all direct reports
                var reportIds = await _context.Users
                    .Where(u => u.ManagerId == currentUser.Id)
                    .Select(u => u.Id)
                    .ToListAsync();

                reportIds.Add(currentUser.Id);
                return reportIds;
            }

            // SalesExecutive can only access their own assigned records
            return new List<string> { currentUser.Id };
        }

        public async Task<bool> CanAccessRecordAsync(ClaimsPrincipal user, string? recordOwnerUserId)
        {
            if (user.IsInRole(UserRoles.Admin))
                return true;

            var accessibleIds = await GetAccessibleUserIdsAsync(user);
            if (accessibleIds == null)
                return true;

            if (string.IsNullOrEmpty(recordOwnerUserId))
                return false;

            return accessibleIds.Contains(recordOwnerUserId);
        }
    }
}

