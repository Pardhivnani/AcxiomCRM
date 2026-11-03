using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Services.Interfaces;
using AcxiomCRM.ViewModels.Dashboard;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;

namespace AcxiomCRM.Services.Implementations
{
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserScopeService _userScopeService;

        public DashboardService(ApplicationDbContext context, IUserScopeService userScopeService)
        {
            _context = context;
            _userScopeService = userScopeService;
        }

        public async Task<DashboardViewModel> GetDashboardMetricsAsync(ClaimsPrincipal user)
        {
            var accessibleUserIds = await _userScopeService.GetAccessibleUserIdsAsync(user);
            var currentUser = await _userScopeService.GetCurrentUserAsync(user);

            var model = new DashboardViewModel
            {
                UserName = currentUser?.FullName ?? user.Identity?.Name ?? "User",
                UserRole = user.IsInRole(UserRoles.Admin)
                    ? "Administrator"
                    : (user.IsInRole(UserRoles.Manager) ? "Sales Manager" : "Sales Executive")
            };

            // 1. Customers
            var custQuery = _context.Customers.AsNoTracking().AsQueryable();
            if (accessibleUserIds != null)
            {
                custQuery = custQuery.Where(c => c.AssignedToUserId != null && accessibleUserIds.Contains(c.AssignedToUserId));
            }
            model.TotalCustomers = await custQuery.CountAsync();

            // 2. Leads
            var leadQuery = _context.Leads.AsNoTracking().AsQueryable();
            if (accessibleUserIds != null)
            {
                leadQuery = leadQuery.Where(l => l.AssignedToUserId != null && accessibleUserIds.Contains(l.AssignedToUserId));
            }
            model.TotalLeads = await leadQuery.CountAsync();
            model.OpenLeads = await leadQuery.CountAsync(l =>
                l.Status == LeadStatus.New || l.Status == LeadStatus.Contacted || l.Status == LeadStatus.Qualified);

            // Lead status chart data
            var leadStatuses = Enum.GetValues<LeadStatus>();
            var leadCountsByStatus = await leadQuery
                .GroupBy(l => l.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.Status, g => g.Count);

            foreach (var status in leadStatuses)
            {
                model.LeadStatusLabels.Add(status.ToString());
                model.LeadStatusCounts.Add(leadCountsByStatus.GetValueOrDefault(status, 0));
            }

            // 3. Opportunities
            var oppQuery = _context.Opportunities.AsNoTracking().AsQueryable();
            if (accessibleUserIds != null)
            {
                oppQuery = oppQuery.Where(o => accessibleUserIds.Contains(o.AssignedToUserId));
            }

            var opps = await oppQuery.ToListAsync();

            model.TotalOpportunities = opps.Count;
            model.OpenOpportunities = opps.Count(o => o.Status == OpportunityStatus.Open);
            model.WonOpportunities = opps.Count(o => o.Status == OpportunityStatus.Won);
            model.LostOpportunities = opps.Count(o => o.Status == OpportunityStatus.Lost);

            var openOpps = opps.Where(o => o.Status == OpportunityStatus.Open).ToList();
            model.TotalPipelineValue = openOpps.Sum(o => o.Amount);
            model.WeightedPipelineValue = Math.Round(openOpps.Sum(o => o.Amount * o.Probability / 100m), 2);
            model.TotalWonRevenue = opps.Where(o => o.Status == OpportunityStatus.Won).Sum(o => o.Amount);

            // Opportunity pipeline stage chart
            var stages = Enum.GetValues<OpportunityStage>();
            foreach (var stage in stages)
            {
                var stageOpps = opps.Where(o => o.Stage == stage).ToList();
                model.OpportunityStageLabels.Add(stage.ToString());
                model.OpportunityStageValues.Add(stageOpps.Sum(o => o.Amount));
                model.OpportunityStageCounts.Add(stageOpps.Count);
            }

            // 4. Monthly sales won revenue (last 6 months)
            var now = DateTime.UtcNow;
            var wonOpps = opps.Where(o => o.Status == OpportunityStatus.Won).ToList();

            for (int i = 5; i >= 0; i--)
            {
                var targetMonth = now.AddMonths(-i);
                var monthName = targetMonth.ToString("MMM yyyy", CultureInfo.InvariantCulture);
                model.MonthlySalesLabels.Add(monthName);

                var monthWonSum = wonOpps
                    .Where(o => o.ExpectedCloseDate.Year == targetMonth.Year && o.ExpectedCloseDate.Month == targetMonth.Month)
                    .Sum(o => o.Amount);

                model.MonthlySalesValues.Add(monthWonSum);
            }

            // 5. Upcoming follow-ups
            var today = DateTime.UtcNow.Date;
            var followUpQuery = _context.FollowUps
                .Include(f => f.Customer)
                .Include(f => f.Lead)
                .Include(f => f.AssignedTo)
                .Where(f => f.Status == FollowUpStatus.Planned && f.FollowUpDate >= today)
                .AsNoTracking()
                .AsQueryable();

            if (accessibleUserIds != null)
            {
                followUpQuery = followUpQuery.Where(f => accessibleUserIds.Contains(f.AssignedToUserId));
            }

            model.UpcomingFollowUps = await followUpQuery
                .OrderBy(f => f.FollowUpDate)
                .Take(5)
                .ToListAsync();

            // 6. Recent activities
            var actQuery = _context.Activities
                .Include(a => a.Customer)
                .Include(a => a.Lead)
                .Include(a => a.AssignedTo)
                .AsNoTracking()
                .AsQueryable();

            if (accessibleUserIds != null)
            {
                actQuery = actQuery.Where(a => accessibleUserIds.Contains(a.AssignedToUserId));
            }

            model.RecentActivities = await actQuery
                .OrderByDescending(a => a.ActivityDate)
                .Take(5)
                .ToListAsync();

            // 7. Top Opportunities
            model.TopOpportunities = openOpps
                .OrderByDescending(o => o.Amount)
                .Take(5)
                .ToList();

            return model;
        }
    }
}

