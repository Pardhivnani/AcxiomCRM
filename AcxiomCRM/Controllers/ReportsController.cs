using AcxiomCRM.Models.Enums;
using AcxiomCRM.Services.Interfaces;
using AcxiomCRM.ViewModels.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly IReportService _reportService;
        private readonly IUserService _userService;

        public ReportsController(IReportService reportService, IUserService userService)
        {
            _reportService = reportService;
            _userService = userService;
        }

        public async Task<IActionResult> Index(
            string reportType = "pipeline",
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? status = null,
            string? userId = null,
            string? searchTerm = null,
            int page = 1,
            int pageSize = 15)
        {
            var filter = new ReportFilterViewModel
            {
                ReportType = reportType.ToLowerInvariant(),
                FromDate = fromDate,
                ToDate = toDate,
                Status = status,
                UserId = userId,
                SearchTerm = searchTerm,
                Page = page,
                PageSize = pageSize
            };

            var container = new ReportContainerViewModel
            {
                Filter = filter
            };

            var salesUsers = await _userService.GetSalesExecutivesAsync();
            ViewBag.UsersList = new SelectList(salesUsers, "Id", "FullName", userId);

            switch (filter.ReportType)
            {
                case "customer":
                    container.Title = "Customer Accounts Report";
                    container.Description = "Detailed analysis of customer accounts, active deals, and portfolio value.";
                    container.ReportData = await _reportService.GetCustomerReportAsync(filter, User);
                    break;

                case "lead":
                    container.Title = "Lead Performance Report";
                    container.Description = "Analysis of inbound lead sources, status transitions, and expected pipeline values.";
                    container.ReportData = await _reportService.GetLeadReportAsync(filter, User);
                    break;

                case "followup":
                    container.Title = "Follow-Up Engagements Report";
                    container.Description = "Review of scheduled customer touches, completion rates, and overdue items.";
                    container.ReportData = await _reportService.GetFollowUpReportAsync(filter, User);
                    break;

                case "opportunity":
                    container.Title = "Deal Opportunities Report";
                    container.Description = "Comprehensive log of active and closed deals with probability weighting.";
                    container.ReportData = await _reportService.GetOpportunityReportAsync(filter, User);
                    break;

                case "conversion":
                    container.Title = "Lead Conversion Velocity Report";
                    container.Description = "Analysis of qualified leads that successfully transitioned into active accounts.";
                    container.ReportData = await _reportService.GetLeadConversionReportAsync(filter, User);
                    break;

                case "activity":
                    container.Title = "Sales Team Activity Report";
                    container.Description = "Metrics across calls made, meetings held, emails sent, and tasks completed.";
                    container.ReportData = await _reportService.GetUserActivityReportAsync(filter, User);
                    break;

                case "audit":
                    if (!User.IsInRole(UserRoles.Admin))
                    {
                        return Forbid();
                    }
                    container.Title = "System Audit & Compliance Log Report";
                    container.Description = "Security audit trail tracking creates, updates, status transitions, and logins.";
                    container.ReportData = await _reportService.GetAuditReportAsync(filter, User);
                    break;

                case "pipeline":
                default:
                    container.Filter.ReportType = "pipeline";
                    container.Title = "Sales Pipeline Stage Distribution Report";
                    container.Description = "Breakdown of total and weighted pipeline capital across qualification stages.";
                    container.ReportData = await _reportService.GetPipelineReportAsync(filter, User);
                    break;
            }

            return View(container);
        }

        [HttpGet]
        public async Task<IActionResult> ExportCsv(
            string reportType,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? status = null,
            string? userId = null,
            string? searchTerm = null)
        {
            if (string.Equals(reportType, "audit", StringComparison.OrdinalIgnoreCase) && !User.IsInRole(UserRoles.Admin))
            {
                return Forbid();
            }

            var filter = new ReportFilterViewModel
            {
                ReportType = reportType,
                FromDate = fromDate,
                ToDate = toDate,
                Status = status,
                UserId = userId,
                SearchTerm = searchTerm
            };

            var (bytes, fileName) = await _reportService.ExportToCsvAsync(reportType, filter, User);
            return File(bytes, "text/csv", fileName);
        }
    }
}

