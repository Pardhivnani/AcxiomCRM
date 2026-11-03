using AcxiomCRM.Models.Entities;
using AcxiomCRM.ViewModels.Common;
using AcxiomCRM.ViewModels.Reports;
using System.Security.Claims;

namespace AcxiomCRM.Services.Interfaces
{
    public interface IReportService
    {
        Task<PagedResult<CustomerReportRow>> GetCustomerReportAsync(ReportFilterViewModel filter, ClaimsPrincipal user);
        Task<PagedResult<LeadReportRow>> GetLeadReportAsync(ReportFilterViewModel filter, ClaimsPrincipal user);
        Task<PagedResult<FollowUpReportRow>> GetFollowUpReportAsync(ReportFilterViewModel filter, ClaimsPrincipal user);
        Task<PagedResult<OpportunityReportRow>> GetOpportunityReportAsync(ReportFilterViewModel filter, ClaimsPrincipal user);
        Task<List<PipelineReportRow>> GetPipelineReportAsync(ReportFilterViewModel filter, ClaimsPrincipal user);
        Task<PagedResult<LeadConversionReportRow>> GetLeadConversionReportAsync(ReportFilterViewModel filter, ClaimsPrincipal user);
        Task<List<UserActivityReportRow>> GetUserActivityReportAsync(ReportFilterViewModel filter, ClaimsPrincipal user);
        Task<PagedResult<AuditLog>> GetAuditReportAsync(ReportFilterViewModel filter, ClaimsPrincipal user);
        Task<(byte[] CsvBytes, string FileName)> ExportToCsvAsync(string reportType, ReportFilterViewModel filter, ClaimsPrincipal user);
    }
}

