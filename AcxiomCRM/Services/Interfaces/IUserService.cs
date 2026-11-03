using AcxiomCRM.Models.Entities;
using AcxiomCRM.ViewModels.Common;
using AcxiomCRM.ViewModels.Users;
using System.Security.Claims;

namespace AcxiomCRM.Services.Interfaces
{
    public interface IUserService
    {
        Task<PagedResult<UserItemViewModel>> GetUsersAsync(string? searchTerm, string? role, bool? isActive, int page = 1, int pageSize = 10);
        Task<List<ApplicationUser>> GetSalesExecutivesAsync();
        Task<List<ApplicationUser>> GetManagersAsync();
        Task<UserEditViewModel?> GetUserForEditAsync(string id);
        Task<(bool Success, string? ErrorMessage)> CreateUserAsync(UserCreateViewModel model, ClaimsPrincipal adminUser, string? ipAddress = null);
        Task<(bool Success, string? ErrorMessage)> UpdateUserAsync(UserEditViewModel model, ClaimsPrincipal adminUser, string? ipAddress = null);
        Task<(bool Success, string? ErrorMessage)> ToggleActiveStatusAsync(string id, ClaimsPrincipal adminUser, string? ipAddress = null);
        Task<(bool Success, string? ErrorMessage)> UnlockUserAsync(string id, ClaimsPrincipal adminUser, string? ipAddress = null);
        Task<(bool Success, string? ErrorMessage)> ResetPasswordAsync(ResetPasswordAdminViewModel model, ClaimsPrincipal adminUser, string? ipAddress = null);
    }
}

