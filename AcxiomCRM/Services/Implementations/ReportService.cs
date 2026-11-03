using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Services.Interfaces;
using AcxiomCRM.ViewModels.Common;
using AcxiomCRM.ViewModels.Reports;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;
using System.Text;

namespace AcxiomCRM.Services.Implementations
{
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserScopeService _userScopeService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReportService(
            ApplicationDbContext context,
            IUserScopeService userScopeService,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userScopeService = userScopeService;
            _userManager = userManager;
        }

        public async Task<PagedResult<CustomerReportRow>> GetCustomerReportAsync(ReportFilterViewModel filter, ClaimsPrincipal user)
        {
            var accessibleUserIds = await _userScopeService.GetAccessibleUserIdsAsync(user);

            var query = _context.Customers
                .Include(c => c.AssignedTo)
                .Include(c => c.Opportunities)
                .AsNoTracking()
                .AsQueryable();

            if (accessibleUserIds != null)
                query = query.Where(c => c.AssignedToUserId != null && accessibleUserIds.Contains(c.AssignedToUserId));

            if (!string.IsNullOrWhiteSpace(filter.UserId))
                query = query.Where(c => c.AssignedToUserId == filter.UserId);

            if (!string.IsNullOrWhiteSpace(filter.Status) && Enum.TryParse<CustomerStatus>(filter.Status, out var statusVal))
                query = query.Where(c => c.Status == statusVal);

            if (filter.FromDate.HasValue)
                query = query.Where(c => c.CreatedDate >= filter.FromDate.Value.Date);

            if (filter.ToDate.HasValue)
                query = query.Where(c => c.CreatedDate <= filter.ToDate.Value.Date.AddDays(1).AddTicks(-1));

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.Trim().ToLower();
                query = query.Where(c => c.CustomerName.ToLower().Contains(term) || c.Email.ToLower().Contains(term));
            }

            var totalCount = await query.CountAsync();
            var page = Math.Max(1, filter.Page);
            var pageSize = Math.Max(1, filter.PageSize);

            var customers = await query
                .OrderByDescending(c => c.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new
                {
                    c.CustomerCode,
                    c.CustomerName,
                    c.Email,
                    c.Phone,
                    CompanyName = c.CompanyName ?? "-",
                    Status = c.Status.ToString(),
                    AssignedTo = c.AssignedTo != null ? c.AssignedTo.FullName : "Unassigned",
                    c.CreatedDate,
                    DealsCount = c.Opportunities.Count,
                    OpportunityAmounts = c.Opportunities.Select(o => o.Amount).ToList()
                })
                .ToListAsync();

            var items = customers.Select(c => new CustomerReportRow
            {
                CustomerCode = c.CustomerCode,
                CustomerName = c.CustomerName,
                Email = c.Email,
                Phone = c.Phone,
                CompanyName = c.CompanyName,
                Status = c.Status,
                AssignedTo = c.AssignedTo,
                CreatedDate = c.CreatedDate,
                DealsCount = c.DealsCount,
                TotalDealsValue = c.OpportunityAmounts.Sum()
            }).ToList();

            return new PagedResult<CustomerReportRow>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<PagedResult<LeadReportRow>> GetLeadReportAsync(ReportFilterViewModel filter, ClaimsPrincipal user)
        {
            var accessibleUserIds = await _userScopeService.GetAccessibleUserIdsAsync(user);

            var query = _context.Leads
                .Include(l => l.AssignedTo)
                .AsNoTracking()
                .AsQueryable();

            if (accessibleUserIds != null)
                query = query.Where(l => l.AssignedToUserId != null && accessibleUserIds.Contains(l.AssignedToUserId));

            if (!string.IsNullOrWhiteSpace(filter.UserId))
                query = query.Where(l => l.AssignedToUserId == filter.UserId);

            if (!string.IsNullOrWhiteSpace(filter.Status) && Enum.TryParse<LeadStatus>(filter.Status, out var statusVal))
                query = query.Where(l => l.Status == statusVal);

            if (filter.FromDate.HasValue)
                query = query.Where(l => l.CreatedDate >= filter.FromDate.Value.Date);

            if (filter.ToDate.HasValue)
                query = query.Where(l => l.CreatedDate <= filter.ToDate.Value.Date.AddDays(1).AddTicks(-1));

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.Trim().ToLower();
                query = query.Where(l => l.LeadName.ToLower().Contains(term) || l.Email.ToLower().Contains(term));
            }

            var totalCount = await query.CountAsync();
            var page = Math.Max(1, filter.Page);
            var pageSize = Math.Max(1, filter.PageSize);

            var items = await query
                .OrderByDescending(l => l.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new LeadReportRow
                {
                    LeadCode = l.LeadCode,
                    LeadName = l.LeadName,
                    Email = l.Email,
                    Phone = l.Phone,
                    CompanyName = l.CompanyName ?? "-",
                    Source = l.Source,
                    Status = l.Status.ToString(),
                    Priority = l.Priority.ToString(),
                    ExpectedValue = l.ExpectedValue,
                    AssignedTo = l.AssignedTo != null ? l.AssignedTo.FullName : "Unassigned",
                    CreatedDate = l.CreatedDate
                })
                .ToListAsync();

            return new PagedResult<LeadReportRow>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<PagedResult<FollowUpReportRow>> GetFollowUpReportAsync(ReportFilterViewModel filter, ClaimsPrincipal user)
        {
            var accessibleUserIds = await _userScopeService.GetAccessibleUserIdsAsync(user);

            var query = _context.FollowUps
                .Include(f => f.Customer)
                .Include(f => f.Lead)
                .Include(f => f.AssignedTo)
                .AsNoTracking()
                .AsQueryable();

            if (accessibleUserIds != null)
                query = query.Where(f => accessibleUserIds.Contains(f.AssignedToUserId));

            if (!string.IsNullOrWhiteSpace(filter.UserId))
                query = query.Where(f => f.AssignedToUserId == filter.UserId);

            if (!string.IsNullOrWhiteSpace(filter.Status) && Enum.TryParse<FollowUpStatus>(filter.Status, out var statusVal))
                query = query.Where(f => f.Status == statusVal);

            if (filter.FromDate.HasValue)
                query = query.Where(f => f.FollowUpDate >= filter.FromDate.Value.Date);

            if (filter.ToDate.HasValue)
                query = query.Where(f => f.FollowUpDate <= filter.ToDate.Value.Date.AddDays(1).AddTicks(-1));

            var totalCount = await query.CountAsync();
            var page = Math.Max(1, filter.Page);
            var pageSize = Math.Max(1, filter.PageSize);

            var rawFollowUps = await query
                .OrderByDescending(f => f.FollowUpDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(f => new
                {
                    f.FollowUpId,
                    f.Subject,
                    Type = f.FollowUpType.ToString(),
                    f.FollowUpDate,
                    Status = f.Status.ToString(),
                    CustomerName = f.Customer != null ? f.Customer.CustomerName : null,
                    LeadName = f.Lead != null ? f.Lead.LeadName : null,
                    AssignedTo = f.AssignedTo != null ? f.AssignedTo.FullName : "Unassigned",
                    f.CompletedDate
                })
                .ToListAsync();

            var items = rawFollowUps.Select(f => new FollowUpReportRow
            {
                FollowUpId = f.FollowUpId,
                Subject = f.Subject,
                Type = f.Type,
                FollowUpDate = f.FollowUpDate,
                Status = f.Status,
                RelatedTo = f.CustomerName ?? f.LeadName ?? "-",
                AssignedTo = f.AssignedTo,
                CompletedDate = f.CompletedDate
            }).ToList();

            return new PagedResult<FollowUpReportRow>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<PagedResult<OpportunityReportRow>> GetOpportunityReportAsync(ReportFilterViewModel filter, ClaimsPrincipal user)
        {
            var accessibleUserIds = await _userScopeService.GetAccessibleUserIdsAsync(user);

            var query = _context.Opportunities
                .Include(o => o.Customer)
                .Include(o => o.AssignedTo)
                .AsNoTracking()
                .AsQueryable();

            if (accessibleUserIds != null)
                query = query.Where(o => accessibleUserIds.Contains(o.AssignedToUserId));

            if (!string.IsNullOrWhiteSpace(filter.UserId))
                query = query.Where(o => o.AssignedToUserId == filter.UserId);

            if (!string.IsNullOrWhiteSpace(filter.Status) && Enum.TryParse<OpportunityStage>(filter.Status, out var stageVal))
                query = query.Where(o => o.Stage == stageVal);

            if (filter.FromDate.HasValue)
                query = query.Where(o => o.CreatedDate >= filter.FromDate.Value.Date);

            if (filter.ToDate.HasValue)
                query = query.Where(o => o.CreatedDate <= filter.ToDate.Value.Date.AddDays(1).AddTicks(-1));

            var totalCount = await query.CountAsync();
            var page = Math.Max(1, filter.Page);
            var pageSize = Math.Max(1, filter.PageSize);

            var rawOpps = await query
                .OrderByDescending(o => o.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new
                {
                    o.OpportunityName,
                    CustomerName = o.Customer != null ? o.Customer.CustomerName : "-",
                    o.Amount,
                    Stage = o.Stage.ToString(),
                    o.Probability,
                    o.ExpectedCloseDate,
                    Status = o.Status.ToString(),
                    AssignedTo = o.AssignedTo != null ? o.AssignedTo.FullName : "Unassigned"
                })
                .ToListAsync();

            var items = rawOpps.Select(o => new OpportunityReportRow
            {
                OpportunityName = o.OpportunityName,
                CustomerName = o.CustomerName,
                Amount = o.Amount,
                Stage = o.Stage,
                Probability = o.Probability,
                WeightedPipeline = Math.Round(o.Amount * o.Probability / 100m, 2),
                ExpectedCloseDate = o.ExpectedCloseDate,
                Status = o.Status,
                AssignedTo = o.AssignedTo
            }).ToList();

            return new PagedResult<OpportunityReportRow>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<List<PipelineReportRow>> GetPipelineReportAsync(ReportFilterViewModel filter, ClaimsPrincipal user)
        {
            var accessibleUserIds = await _userScopeService.GetAccessibleUserIdsAsync(user);

            var query = _context.Opportunities.AsNoTracking().AsQueryable();

            if (accessibleUserIds != null)
                query = query.Where(o => accessibleUserIds.Contains(o.AssignedToUserId));

            if (!string.IsNullOrWhiteSpace(filter.UserId))
                query = query.Where(o => o.AssignedToUserId == filter.UserId);

            if (filter.FromDate.HasValue)
                query = query.Where(o => o.CreatedDate >= filter.FromDate.Value.Date);

            if (filter.ToDate.HasValue)
                query = query.Where(o => o.CreatedDate <= filter.ToDate.Value.Date.AddDays(1).AddTicks(-1));

            var opps = await query.ToListAsync();
            var totalPipeline = opps.Sum(o => o.Amount);

            var stages = Enum.GetValues<OpportunityStage>();
            var rows = new List<PipelineReportRow>();

            foreach (var stage in stages)
            {
                var stageOpps = opps.Where(o => o.Stage == stage).ToList();
                var stageAmount = stageOpps.Sum(o => o.Amount);
                var stageWeighted = Math.Round(stageOpps.Sum(o => o.Amount * o.Probability / 100m), 2);
                var pct = totalPipeline > 0 ? Math.Round((stageAmount / totalPipeline) * 100m, 1) : 0m;

                rows.Add(new PipelineReportRow
                {
                    Stage = stage.ToString(),
                    OpportunityCount = stageOpps.Count,
                    TotalAmount = stageAmount,
                    WeightedAmount = stageWeighted,
                    PercentageOfTotal = pct
                });
            }

            return rows;
        }

        public async Task<PagedResult<LeadConversionReportRow>> GetLeadConversionReportAsync(ReportFilterViewModel filter, ClaimsPrincipal user)
        {
            var accessibleUserIds = await _userScopeService.GetAccessibleUserIdsAsync(user);

            var query = _context.Leads
                .Include(l => l.Customer)
                .Include(l => l.AssignedTo)
                .Where(l => l.Status == LeadStatus.Converted && l.CustomerId != null)
                .AsNoTracking()
                .AsQueryable();

            if (accessibleUserIds != null)
                query = query.Where(l => l.AssignedToUserId != null && accessibleUserIds.Contains(l.AssignedToUserId));

            if (!string.IsNullOrWhiteSpace(filter.UserId))
                query = query.Where(l => l.AssignedToUserId == filter.UserId);

            if (filter.FromDate.HasValue)
                query = query.Where(l => l.CreatedDate >= filter.FromDate.Value.Date);

            if (filter.ToDate.HasValue)
                query = query.Where(l => l.CreatedDate <= filter.ToDate.Value.Date.AddDays(1).AddTicks(-1));

            var totalCount = await query.CountAsync();
            var page = Math.Max(1, filter.Page);
            var pageSize = Math.Max(1, filter.PageSize);

            var rawLeads = await query
                .OrderByDescending(l => l.ModifiedDate ?? l.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new
                {
                    l.LeadCode,
                    l.LeadName,
                    CompanyName = l.CompanyName ?? "-",
                    ConvertedCustomerName = l.Customer != null ? l.Customer.CustomerName : "-",
                    l.Source,
                    LeadCreatedDate = l.CreatedDate,
                    ConvertedDate = l.ModifiedDate ?? l.CreatedDate,
                    ConvertedBy = l.AssignedTo != null ? l.AssignedTo.FullName : "System"
                })
                .ToListAsync();

            var items = rawLeads.Select(l => new LeadConversionReportRow
            {
                LeadCode = l.LeadCode,
                LeadName = l.LeadName,
                CompanyName = l.CompanyName,
                ConvertedCustomerName = l.ConvertedCustomerName,
                Source = l.Source,
                LeadCreatedDate = l.LeadCreatedDate,
                ConvertedDate = l.ConvertedDate,
                DaysToConvert = Math.Max(0, (int)(l.ConvertedDate - l.LeadCreatedDate).TotalDays),
                ConvertedBy = l.ConvertedBy
            }).ToList();

            return new PagedResult<LeadConversionReportRow>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<List<UserActivityReportRow>> GetUserActivityReportAsync(ReportFilterViewModel filter, ClaimsPrincipal user)
        {
            var accessibleUserIds = await _userScopeService.GetAccessibleUserIdsAsync(user);

            var usersQuery = _context.Users.AsNoTracking().AsQueryable();
            if (accessibleUserIds != null)
            {
                usersQuery = usersQuery.Where(u => accessibleUserIds.Contains(u.Id));
            }

            var users = await usersQuery.ToListAsync();

            var actQuery = _context.Activities.AsNoTracking().AsQueryable();
            if (filter.FromDate.HasValue) actQuery = actQuery.Where(a => a.ActivityDate >= filter.FromDate.Value.Date);
            if (filter.ToDate.HasValue) actQuery = actQuery.Where(a => a.ActivityDate <= filter.ToDate.Value.Date.AddDays(1).AddTicks(-1));
            var activities = await actQuery.ToListAsync();

            var fuQuery = _context.FollowUps.Where(f => f.Status == FollowUpStatus.Completed).AsNoTracking().AsQueryable();
            if (filter.FromDate.HasValue) fuQuery = fuQuery.Where(f => f.FollowUpDate >= filter.FromDate.Value.Date);
            if (filter.ToDate.HasValue) fuQuery = fuQuery.Where(f => f.FollowUpDate <= filter.ToDate.Value.Date.AddDays(1).AddTicks(-1));
            var followUps = await fuQuery.ToListAsync();

            var rows = new List<UserActivityReportRow>();
            foreach (var u in users)
            {
                var userRoles = await _userManager.GetRolesAsync(u);
                var userActs = activities.Where(a => a.AssignedToUserId == u.Id).ToList();

                rows.Add(new UserActivityReportRow
                {
                    UserName = u.FullName,
                    Email = u.Email ?? "",
                    Role = userRoles.FirstOrDefault() ?? "User",
                    TotalActivities = userActs.Count,
                    CallsCount = userActs.Count(a => a.ActivityType == ActivityType.Call),
                    MeetingsCount = userActs.Count(a => a.ActivityType == ActivityType.Meeting),
                    EmailsCount = userActs.Count(a => a.ActivityType == ActivityType.Email),
                    TasksCount = userActs.Count(a => a.ActivityType == ActivityType.Task),
                    FollowUpsCompleted = followUps.Count(f => f.AssignedToUserId == u.Id)
                });
            }

            return rows.OrderByDescending(r => r.TotalActivities).ToList();
        }

        public async Task<PagedResult<AuditLog>> GetAuditReportAsync(ReportFilterViewModel filter, ClaimsPrincipal user)
        {
            var query = _context.AuditLogs.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.UserId))
                query = query.Where(a => a.UserId == filter.UserId || (a.UserEmail != null && a.UserEmail.Contains(filter.UserId)));

            if (!string.IsNullOrWhiteSpace(filter.Status))
                query = query.Where(a => a.EntityName == filter.Status);

            if (filter.FromDate.HasValue)
                query = query.Where(a => a.CreatedDate >= filter.FromDate.Value.Date);

            if (filter.ToDate.HasValue)
                query = query.Where(a => a.CreatedDate <= filter.ToDate.Value.Date.AddDays(1).AddTicks(-1));

            var totalCount = await query.CountAsync();
            var page = Math.Max(1, filter.Page);
            var pageSize = Math.Max(1, filter.PageSize);

            var items = await query
                .OrderByDescending(a => a.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<AuditLog>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<(byte[] CsvBytes, string FileName)> ExportToCsvAsync(
            string reportType,
            ReportFilterViewModel filter,
            ClaimsPrincipal user)
        {
            var sb = new StringBuilder();
            string fileName;
            var dateSuffix = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");

            switch (reportType.ToLowerInvariant())
            {
                case "customer":
                    fileName = $"Customers_Report_{dateSuffix}.csv";
                    sb.AppendLine("CustomerCode,CustomerName,Email,Phone,CompanyName,Status,AssignedTo,CreatedDate,DealsCount,TotalDealsValue");
                    filter.PageSize = 10000;
                    var custData = await GetCustomerReportAsync(filter, user);
                    foreach (var c in custData.Items)
                    {
                        sb.AppendLine($"\"{Escape(c.CustomerCode)}\",\"{Escape(c.CustomerName)}\",\"{Escape(c.Email)}\",\"{Escape(c.Phone)}\",\"{Escape(c.CompanyName)}\",\"{Escape(c.Status)}\",\"{Escape(c.AssignedTo)}\",\"{c.CreatedDate:yyyy-MM-dd}\",{c.DealsCount},{c.TotalDealsValue:F2}");
                    }
                    break;

                case "lead":
                    fileName = $"Leads_Report_{dateSuffix}.csv";
                    sb.AppendLine("LeadCode,LeadName,Email,Phone,CompanyName,Source,Status,Priority,ExpectedValue,AssignedTo,CreatedDate");
                    filter.PageSize = 10000;
                    var leadData = await GetLeadReportAsync(filter, user);
                    foreach (var l in leadData.Items)
                    {
                        sb.AppendLine($"\"{Escape(l.LeadCode)}\",\"{Escape(l.LeadName)}\",\"{Escape(l.Email)}\",\"{Escape(l.Phone)}\",\"{Escape(l.CompanyName)}\",\"{Escape(l.Source)}\",\"{Escape(l.Status)}\",\"{Escape(l.Priority)}\",{l.ExpectedValue:F2},\"{Escape(l.AssignedTo)}\",\"{l.CreatedDate:yyyy-MM-dd}\"");
                    }
                    break;

                case "opportunity":
                    fileName = $"Opportunities_Report_{dateSuffix}.csv";
                    sb.AppendLine("OpportunityName,CustomerName,Amount,Stage,Probability,WeightedPipeline,ExpectedCloseDate,Status,AssignedTo");
                    filter.PageSize = 10000;
                    var oppData = await GetOpportunityReportAsync(filter, user);
                    foreach (var o in oppData.Items)
                    {
                        sb.AppendLine($"\"{Escape(o.OpportunityName)}\",\"{Escape(o.CustomerName)}\",{o.Amount:F2},\"{Escape(o.Stage)}\",{o.Probability}%,{o.WeightedPipeline:F2},\"{o.ExpectedCloseDate:yyyy-MM-dd}\",\"{Escape(o.Status)}\",\"{Escape(o.AssignedTo)}\"");
                    }
                    break;

                case "pipeline":
                    fileName = $"Sales_Pipeline_Report_{dateSuffix}.csv";
                    sb.AppendLine("Stage,OpportunityCount,TotalAmount,WeightedAmount,PercentageOfTotal");
                    var pipelineData = await GetPipelineReportAsync(filter, user);
                    foreach (var p in pipelineData)
                    {
                        sb.AppendLine($"\"{Escape(p.Stage)}\",{p.OpportunityCount},{p.TotalAmount:F2},{p.WeightedAmount:F2},{p.PercentageOfTotal}%");
                    }
                    break;

                case "conversion":
                    fileName = $"Lead_Conversion_Report_{dateSuffix}.csv";
                    sb.AppendLine("LeadCode,LeadName,CompanyName,ConvertedCustomer,Source,LeadCreatedDate,ConvertedDate,DaysToConvert,ConvertedBy");
                    filter.PageSize = 10000;
                    var convData = await GetLeadConversionReportAsync(filter, user);
                    foreach (var cv in convData.Items)
                    {
                        sb.AppendLine($"\"{Escape(cv.LeadCode)}\",\"{Escape(cv.LeadName)}\",\"{Escape(cv.CompanyName)}\",\"{Escape(cv.ConvertedCustomerName)}\",\"{Escape(cv.Source)}\",\"{cv.LeadCreatedDate:yyyy-MM-dd}\",\"{cv.ConvertedDate:yyyy-MM-dd}\",{cv.DaysToConvert},\"{Escape(cv.ConvertedBy)}\"");
                    }
                    break;

                case "activity":
                    fileName = $"User_Activity_Report_{dateSuffix}.csv";
                    sb.AppendLine("UserName,Email,Role,TotalActivities,Calls,Meetings,Emails,Tasks,FollowUpsCompleted");
                    var actData = await GetUserActivityReportAsync(filter, user);
                    foreach (var a in actData)
                    {
                        sb.AppendLine($"\"{Escape(a.UserName)}\",\"{Escape(a.Email)}\",\"{Escape(a.Role)}\",{a.TotalActivities},{a.CallsCount},{a.MeetingsCount},{a.EmailsCount},{a.TasksCount},{a.FollowUpsCompleted}");
                    }
                    break;

                case "followup":
                    fileName = $"FollowUps_Report_{dateSuffix}.csv";
                    sb.AppendLine("Id,Subject,Type,FollowUpDate,Status,RelatedTo,AssignedTo,CompletedDate");
                    filter.PageSize = 10000;
                    var fuData = await GetFollowUpReportAsync(filter, user);
                    foreach (var f in fuData.Items)
                    {
                        sb.AppendLine($"{f.FollowUpId},\"{Escape(f.Subject)}\",\"{Escape(f.Type)}\",\"{f.FollowUpDate:yyyy-MM-dd HH:mm}\",\"{Escape(f.Status)}\",\"{Escape(f.RelatedTo)}\",\"{Escape(f.AssignedTo)}\",\"{f.CompletedDate:yyyy-MM-dd}\"");
                    }
                    break;

                case "audit":
                default:
                    fileName = $"Audit_Report_{dateSuffix}.csv";
                    sb.AppendLine("AuditLogId,CreatedDate,UserEmail,Action,EntityName,RecordId,Result,IpAddress,OldValue,NewValue");
                    filter.PageSize = 10000;
                    var auditData = await GetAuditReportAsync(filter, user);
                    foreach (var al in auditData.Items)
                    {
                        sb.AppendLine($"{al.AuditLogId},\"{al.CreatedDate:yyyy-MM-dd HH:mm:ss}\",\"{Escape(al.UserEmail ?? "")}\",\"{Escape(al.Action)}\",\"{Escape(al.EntityName)}\",\"{Escape(al.RecordId ?? "")}\",\"{Escape(al.Result)}\",\"{Escape(al.IpAddress ?? "")}\",\"{Escape(al.OldValue ?? "")}\",\"{Escape(al.NewValue ?? "")}\"");
                    }
                    break;
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return (bytes, fileName);
        }

        private static string Escape(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            return input.Replace("\"", "\"\"").Replace("\r", " ").Replace("\n", " ");
        }
    }
}

