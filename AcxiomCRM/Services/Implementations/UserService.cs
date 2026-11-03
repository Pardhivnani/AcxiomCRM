using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Services.Interfaces;
using AcxiomCRM.ViewModels.Common;
using AcxiomCRM.ViewModels.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AcxiomCRM.Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly IAuditLogService _auditLogService;

        public UserService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            IAuditLogService auditLogService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _auditLogService = auditLogService;
        }

        public async Task<PagedResult<UserItemViewModel>> GetUsersAsync(
            string? searchTerm,
            string? role,
            bool? isActive,
            int page = 1,
            int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var query = _context.Users
                .Include(u => u.Manager)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(u => u.FullName.ToLower().Contains(term) || (u.Email != null && u.Email.ToLower().Contains(term)));
            }

            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == isActive.Value);
            }

            var users = await query.OrderByDescending(u => u.CreatedDate).ToListAsync();
            var userViewModels = new List<UserItemViewModel>();

            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                var primaryRole = roles.FirstOrDefault() ?? "None";

                if (!string.IsNullOrWhiteSpace(role) && !string.Equals(primaryRole, role, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var isLockedOut = u.LockoutEnd.HasValue && u.LockoutEnd.Value > DateTimeOffset.UtcNow;

                userViewModels.Add(new UserItemViewModel
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email ?? "",
                    Role = primaryRole,
                    Department = u.Department,
                    ManagerName = u.Manager != null ? u.Manager.FullName : null,
                    IsActive = u.IsActive,
                    IsLockedOut = isLockedOut,
                    CreatedDate = u.CreatedDate
                });
            }

            var totalCount = userViewModels.Count;
            var pagedItems = userViewModels
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedResult<UserItemViewModel>
            {
                Items = pagedItems,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<List<ApplicationUser>> GetSalesExecutivesAsync()
        {
            var salesUsers = await _userManager.GetUsersInRoleAsync(UserRoles.SalesExecutive);
            return salesUsers.Where(u => u.IsActive).OrderBy(u => u.FullName).ToList();
        }

        public async Task<List<ApplicationUser>> GetManagersAsync()
        {
            var managerUsers = await _userManager.GetUsersInRoleAsync(UserRoles.Manager);
            return managerUsers.Where(u => u.IsActive).OrderBy(u => u.FullName).ToList();
        }

        public async Task<UserEditViewModel?> GetUserForEditAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return null;

            var roles = await _userManager.GetRolesAsync(user);

            return new UserEditViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? "",
                Role = roles.FirstOrDefault() ?? UserRoles.SalesExecutive,
                Department = user.Department,
                ManagerId = user.ManagerId,
                IsActive = user.IsActive
            };
        }

        public async Task<(bool Success, string? ErrorMessage)> CreateUserAsync(
            UserCreateViewModel model,
            ClaimsPrincipal adminUser,
            string? ipAddress = null)
        {
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
                return (false, "A user with this email address already exists.");

            var newUser = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                Department = model.Department,
                ManagerId = model.ManagerId,
                EmailConfirmed = true,
                IsActive = model.IsActive,
                CreatedDate = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(newUser, model.Password);
            if (!createResult.Succeeded)
            {
                return (false, string.Join("; ", createResult.Errors.Select(e => e.Description)));
            }

            if (!string.IsNullOrEmpty(model.Role))
            {
                if (!await _roleManager.RoleExistsAsync(model.Role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(model.Role));
                }
                await _userManager.AddToRoleAsync(newUser, model.Role);
            }

            var currentAdmin = await _userManager.GetUserAsync(adminUser);
            await _auditLogService.LogAsync(
                currentAdmin?.Id,
                currentAdmin?.Email,
                "Create",
                "User",
                newUser.Id,
                null,
                $"Created user '{newUser.FullName}' ({newUser.Email}) with role '{model.Role}'",
                ipAddress,
                "Success");

            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> UpdateUserAsync(
            UserEditViewModel model,
            ClaimsPrincipal adminUser,
            string? ipAddress = null)
        {
            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
                return (false, "User not found.");

            var currentAdmin = await _userManager.GetUserAsync(adminUser);
            if (currentAdmin?.Id == user.Id && !model.IsActive)
            {
                return (false, "You cannot deactivate your own administrator account.");
            }

            var existingUserWithEmail = await _userManager.FindByEmailAsync(model.Email);
            if (existingUserWithEmail != null && existingUserWithEmail.Id != user.Id)
            {
                return (false, "Email address is already in use by another account.");
            }

            var oldRoles = await _userManager.GetRolesAsync(user);
            var oldRole = oldRoles.FirstOrDefault() ?? "None";

            user.FullName = model.FullName;
            user.Email = model.Email;
            user.UserName = model.Email;
            user.Department = model.Department;
            user.ManagerId = model.ManagerId;
            user.IsActive = model.IsActive;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return (false, string.Join("; ", updateResult.Errors.Select(e => e.Description)));
            }

            // Update role if changed
            if (!string.Equals(oldRole, model.Role, StringComparison.OrdinalIgnoreCase))
            {
                await _userManager.RemoveFromRolesAsync(user, oldRoles);
                if (!await _roleManager.RoleExistsAsync(model.Role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(model.Role));
                }
                await _userManager.AddToRoleAsync(user, model.Role);
            }

            await _auditLogService.LogAsync(
                currentAdmin?.Id,
                currentAdmin?.Email,
                "Update",
                "User",
                user.Id,
                $"Role: {oldRole}, Active: {user.IsActive}",
                $"Updated user '{user.FullName}', Role: {model.Role}, Active: {user.IsActive}",
                ipAddress,
                "Success");

            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> ToggleActiveStatusAsync(
            string id,
            ClaimsPrincipal adminUser,
            string? ipAddress = null)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return (false, "User not found.");

            var currentAdmin = await _userManager.GetUserAsync(adminUser);
            if (currentAdmin?.Id == user.Id)
                return (false, "You cannot deactivate your own administrator account.");

            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);

            var action = user.IsActive ? "Activate" : "Deactivate";
            await _auditLogService.LogAsync(
                currentAdmin?.Id,
                currentAdmin?.Email,
                action,
                "User",
                user.Id,
                $"IsActive: {!user.IsActive}",
                $"IsActive: {user.IsActive}",
                ipAddress,
                "Success");

            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> UnlockUserAsync(
            string id,
            ClaimsPrincipal adminUser,
            string? ipAddress = null)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return (false, "User not found.");

            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);

            var currentAdmin = await _userManager.GetUserAsync(adminUser);
            await _auditLogService.LogAsync(
                currentAdmin?.Id,
                currentAdmin?.Email,
                "Unlock",
                "User",
                user.Id,
                "Lockout: Active",
                "Lockout: Cleared",
                ipAddress,
                "Success");

            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> ResetPasswordAsync(
            ResetPasswordAdminViewModel model,
            ClaimsPrincipal adminUser,
            string? ipAddress = null)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
                return (false, "User not found.");

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

            if (!resetResult.Succeeded)
            {
                return (false, string.Join("; ", resetResult.Errors.Select(e => e.Description)));
            }

            var currentAdmin = await _userManager.GetUserAsync(adminUser);
            await _auditLogService.LogAsync(
                currentAdmin?.Id,
                currentAdmin?.Email,
                "ResetPassword",
                "User",
                user.Id,
                null,
                $"Password securely reset for user '{user.Email}'",
                ipAddress,
                "Success");

            return (true, null);
        }
    }
}

