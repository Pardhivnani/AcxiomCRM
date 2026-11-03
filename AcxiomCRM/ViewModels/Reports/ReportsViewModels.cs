using AcxiomCRM.ViewModels.Common;

namespace AcxiomCRM.ViewModels.Reports
{
    public class ReportFilterViewModel
    {
        public string ReportType { get; set; } = "pipeline";
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? Status { get; set; }
        public string? UserId { get; set; }
        public string? SearchTerm { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 15;
    }

    public class CustomerReportRow
    {
        public string CustomerCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string AssignedTo { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public int DealsCount { get; set; }
        public decimal TotalDealsValue { get; set; }
    }

    public class LeadReportRow
    {
        public string LeadCode { get; set; } = string.Empty;
        public string LeadName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public decimal ExpectedValue { get; set; }
        public string AssignedTo { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }

    public class FollowUpReportRow
    {
        public int FollowUpId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public DateTime FollowUpDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string RelatedTo { get; set; } = string.Empty;
        public string AssignedTo { get; set; } = string.Empty;
        public DateTime? CompletedDate { get; set; }
    }

    public class OpportunityReportRow
    {
        public string OpportunityName { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Stage { get; set; } = string.Empty;
        public int Probability { get; set; }
        public decimal WeightedPipeline { get; set; }
        public DateTime ExpectedCloseDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string AssignedTo { get; set; } = string.Empty;
    }

    public class PipelineReportRow
    {
        public string Stage { get; set; } = string.Empty;
        public int OpportunityCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal WeightedAmount { get; set; }
        public decimal PercentageOfTotal { get; set; }
    }

    public class LeadConversionReportRow
    {
        public string LeadCode { get; set; } = string.Empty;
        public string LeadName { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string ConvertedCustomerName { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public DateTime LeadCreatedDate { get; set; }
        public DateTime ConvertedDate { get; set; }
        public int DaysToConvert { get; set; }
        public string ConvertedBy { get; set; } = string.Empty;
    }

    public class UserActivityReportRow
    {
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int TotalActivities { get; set; }
        public int CallsCount { get; set; }
        public int MeetingsCount { get; set; }
        public int EmailsCount { get; set; }
        public int TasksCount { get; set; }
        public int FollowUpsCompleted { get; set; }
    }

    public class ReportContainerViewModel
    {
        public ReportFilterViewModel Filter { get; set; } = new();
        public object? ReportData { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}

