using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.ViewModels.Common;
using System.Security.Claims;

namespace AcxiomCRM.Services.Interfaces
{
    public interface ILeadService
    {
        Task<PagedResult<Lead>> GetLeadsAsync(
            ClaimsPrincipal user,
            string? searchTerm,
            LeadStatus? status,
            LeadPriority? priority,
            string? assignedToUserId,
            int page = 1,
            int pageSize = 10,
            string? sortBy = "CreatedDate",
            bool isAscending = false);

        Task<Lead?> GetByIdAsync(int id, ClaimsPrincipal? user = null);
        Task<Lead?> GetWithDetailsAsync(int id, ClaimsPrincipal user);
        Task<(bool Success, string? ErrorMessage, Lead? Lead)> CreateAsync(Lead lead, ClaimsPrincipal user, string? ipAddress = null);
        Task<(bool Success, string? ErrorMessage)> UpdateAsync(Lead lead, ClaimsPrincipal user, string? ipAddress = null);
        Task<(bool Success, string? ErrorMessage)> DeleteAsync(int id, ClaimsPrincipal user, string? ipAddress = null);
        Task<string> GenerateLeadCodeAsync();

        // Lead Conversion Workflow
        Task<(bool Success, string? ErrorMessage, Customer? CreatedCustomer, Opportunity? CreatedOpportunity)> ConvertLeadAsync(
            int leadId,
            bool createOpportunity,
            string? opportunityName,
            decimal? opportunityAmount,
            DateTime? expectedCloseDate,
            ClaimsPrincipal user,
            string? ipAddress = null);
    }
}

