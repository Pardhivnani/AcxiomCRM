using AcxiomCRM.Models.Entities;

namespace AcxiomCRM.ViewModels.Dashboard
{
    public class DashboardViewModel
    {
        // KPI Metrics
        public int TotalCustomers { get; set; }
        public int TotalLeads { get; set; }
        public int OpenLeads { get; set; }
        public int TotalOpportunities { get; set; }
        public int OpenOpportunities { get; set; }
        public int WonOpportunities { get; set; }
        public int LostOpportunities { get; set; }
        public decimal TotalPipelineValue { get; set; }
        public decimal WeightedPipelineValue { get; set; }
        public decimal TotalWonRevenue { get; set; }

        // Chart 1: Lead Status Chart
        public List<string> LeadStatusLabels { get; set; } = new();
        public List<int> LeadStatusCounts { get; set; } = new();

        // Chart 2: Opportunity Pipeline Stage Chart
        public List<string> OpportunityStageLabels { get; set; } = new();
        public List<decimal> OpportunityStageValues { get; set; } = new();
        public List<int> OpportunityStageCounts { get; set; } = new();

        // Chart 3: Monthly Sales Revenue Chart
        public List<string> MonthlySalesLabels { get; set; } = new();
        public List<decimal> MonthlySalesValues { get; set; } = new();

        // Lists
        public List<AcxiomCRM.Models.Entities.FollowUp> UpcomingFollowUps { get; set; } = new();
        public List<AcxiomCRM.Models.Entities.Activity> RecentActivities { get; set; } = new();
        public List<AcxiomCRM.Models.Entities.Opportunity> TopOpportunities { get; set; } = new();

        public string UserRole { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
    }
}
