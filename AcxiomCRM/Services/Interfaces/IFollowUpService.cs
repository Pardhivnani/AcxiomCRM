using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.ViewModels.Common;
using System.Security.Claims;

namespace AcxiomCRM.Services.Interfaces
{
    public interface IFollowUpService
    {
        Task<PagedResult<FollowUp>> GetFollowUpsAsync(
            ClaimsPrincipal user,
            string? searchTerm,
            FollowUpStatus? status,
            FollowUpType? type,
            string? assignedToUserId,
            DateTime? startDate,
            DateTime? endDate,
            int page = 1,
            int pageSize = 10,
            string? sortBy = "FollowUpDate",
            bool isAscending = true);

        Task<FollowUp?> GetByIdAsync(int id, ClaimsPrincipal? user = null);
        Task<(bool Success, string? ErrorMessage, FollowUp? FollowUp)> CreateAsync(FollowUp followUp, ClaimsPrincipal user, string? ipAddress = null);
        Task<(bool Success, string? ErrorMessage)> UpdateAsync(FollowUp followUp, ClaimsPrincipal user, string? ipAddress = null);
        Task<(bool Success, string? ErrorMessage)> CompleteAsync(int id, string? remarks, ClaimsPrincipal user, string? ipAddress = null);
        Task<(bool Success, string? ErrorMessage)> RescheduleAsync(int id, DateTime newDate, string? remarks, ClaimsPrincipal user, string? ipAddress = null);
        Task<(bool Success, string? ErrorMessage)> DeleteAsync(int id, ClaimsPrincipal user, string? ipAddress = null);
        Task<List<FollowUp>> GetUpcomingRemindersAsync(ClaimsPrincipal user, int days = 7);
        Task<List<FollowUp>> GetOverdueFollowUpsAsync(ClaimsPrincipal user);
    }
}

