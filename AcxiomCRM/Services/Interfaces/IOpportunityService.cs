using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.ViewModels.Common;
using System.Security.Claims;

namespace AcxiomCRM.Services.Interfaces
{
    public interface IOpportunityService
    {
        Task<PagedResult<Opportunity>> GetOpportunitiesAsync(
            ClaimsPrincipal user,
            string? searchTerm,
            OpportunityStage? stage,
            OpportunityStatus? status,
            string? assignedToUserId,
            int? customerId,
            int page = 1,
            int pageSize = 10,
            string? sortBy = "CreatedDate",
            bool isAscending = false);

        Task<Opportunity?> GetByIdAsync(int id, ClaimsPrincipal? user = null);
        Task<Opportunity?> GetWithDetailsAsync(int id, ClaimsPrincipal user);
        Task<(bool Success, string? ErrorMessage, Opportunity? Opportunity)> CreateAsync(Opportunity opportunity, ClaimsPrincipal user, string? ipAddress = null);
        Task<(bool Success, string? ErrorMessage)> UpdateAsync(Opportunity opportunity, ClaimsPrincipal user, string? ipAddress = null);
        Task<(bool Success, string? ErrorMessage)> UpdateStageAsync(int id, OpportunityStage newStage, ClaimsPrincipal user, string? ipAddress = null);
        Task<(bool Success, string? ErrorMessage)> DeleteAsync(int id, ClaimsPrincipal user, string? ipAddress = null);
        Task<(decimal TotalPipeline, decimal WeightedPipeline, int TotalOpportunities)> GetPipelineSummaryAsync(ClaimsPrincipal user);
    }
}

