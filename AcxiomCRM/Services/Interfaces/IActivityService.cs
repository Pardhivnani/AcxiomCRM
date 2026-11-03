using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.ViewModels.Common;
using System.Security.Claims;

namespace AcxiomCRM.Services.Interfaces
{
    public interface IActivityService
    {
        Task<PagedResult<Activity>> GetActivitiesAsync(
            ClaimsPrincipal user,
            string? searchTerm,
            ActivityType? type,
            ActivityStatus? status,
            string? assignedToUserId,
            DateTime? startDate,
            DateTime? endDate,
            int page = 1,
            int pageSize = 10,
            string? sortBy = "ActivityDate",
            bool isAscending = false);

        Task<Activity?> GetByIdAsync(int id, ClaimsPrincipal? user = null);
        Task<(bool Success, string? ErrorMessage, Activity? Activity)> CreateAsync(Activity activity, ClaimsPrincipal user, string? ipAddress = null);
        Task<(bool Success, string? ErrorMessage)> UpdateAsync(Activity activity, ClaimsPrincipal user, string? ipAddress = null);
        Task<(bool Success, string? ErrorMessage)> DeleteAsync(int id, ClaimsPrincipal user, string? ipAddress = null);
        Task<List<Activity>> GetRecentActivitiesAsync(ClaimsPrincipal user, int count = 5);
    }
}

