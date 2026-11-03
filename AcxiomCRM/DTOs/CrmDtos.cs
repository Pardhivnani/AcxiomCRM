using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.DTOs
{
    // Auth DTOs
    public class LoginRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponseDto
    {
        public bool Succeeded { get; set; }
        public string? Message { get; set; }
        public string? UserId { get; set; }
        public string? Email { get; set; }
        public string? FullName { get; set; }
        public List<string> Roles { get; set; } = new();
    }

    // Customer DTOs
    public class CustomerResponseDto
    {
        public int CustomerId { get; set; }
        public string CustomerCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public string? AssignedToUserId { get; set; }
        public string? AssignedToName { get; set; }
    }

    public class CustomerCreateDto
    {
        [Required(ErrorMessage = "Customer name is required.")]
        [MaxLength(150)]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone is required.")]
        [Phone(ErrorMessage = "Enter a valid phone number.")]
        [MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(150)]
        public string? CompanyName { get; set; }

        [MaxLength(250)]
        public string? Address { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(100)]
        public string? State { get; set; }

        public string? AssignedToUserId { get; set; }
    }

    public class CustomerUpdateDto : CustomerCreateDto
    {
        public string Status { get; set; } = "Active";
    }

    // Lead DTOs
    public class LeadResponseDto
    {
        public int LeadId { get; set; }
        public string LeadCode { get; set; } = string.Empty;
        public string LeadName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string Source { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal ExpectedValue { get; set; }
        public string Priority { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? AssignedToUserId { get; set; }
        public string? AssignedToName { get; set; }
        public int? CustomerId { get; set; }
    }

    public class LeadCreateDto
    {
        [Required(ErrorMessage = "Lead name is required.")]
        [MaxLength(150)]
        public string LeadName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone is required.")]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(150)]
        public string? CompanyName { get; set; }

        public string Source { get; set; } = "Website";
        public decimal ExpectedValue { get; set; } = 0m;
        public string Priority { get; set; } = "Medium";
        public string? Notes { get; set; }
        public string? AssignedToUserId { get; set; }
    }

    public class LeadUpdateDto : LeadCreateDto
    {
        public string Status { get; set; } = "New";
    }

    // Opportunity DTOs
    public class OpportunityResponseDto
    {
        public int OpportunityId { get; set; }
        public string OpportunityName { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int? LeadId { get; set; }
        public decimal Amount { get; set; }
        public string Stage { get; set; } = string.Empty;
        public int Probability { get; set; }
        public decimal WeightedPipeline { get; set; }
        public DateTime ExpectedCloseDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public string AssignedToUserId { get; set; } = string.Empty;
        public string? AssignedToName { get; set; }
        public string? Notes { get; set; }
    }

    public class OpportunityCreateDto
    {
        [Required(ErrorMessage = "Opportunity name is required.")]
        [MaxLength(150)]
        public string OpportunityName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Customer is required.")]
        public int CustomerId { get; set; }

        public int? LeadId { get; set; }

        [Required(ErrorMessage = "Opportunity Amount must be greater than 0.")]
        [Range(0.01, 9999999999.99, ErrorMessage = "Opportunity Amount must be greater than 0.")]
        public decimal Amount { get; set; }

        public string Stage { get; set; } = "Qualification";

        [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
        public int Probability { get; set; } = 10;

        [Required(ErrorMessage = "Expected close date is required.")]
        public DateTime ExpectedCloseDate { get; set; }

        public string? AssignedToUserId { get; set; }
        public string? Notes { get; set; }
    }

    public class OpportunityUpdateDto : OpportunityCreateDto
    {
        public string Status { get; set; } = "Open";
    }

    // FollowUp DTOs
    public class FollowUpResponseDto
    {
        public int FollowUpId { get; set; }
        public int? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public int? LeadId { get; set; }
        public string? LeadName { get; set; }
        public int? OpportunityId { get; set; }
        public DateTime FollowUpDate { get; set; }
        public string FollowUpType { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string? Remarks { get; set; }
        public string Status { get; set; } = string.Empty;
        public string AssignedToUserId { get; set; } = string.Empty;
        public string? AssignedToName { get; set; }
        public DateTime? CompletedDate { get; set; }
    }

    public class FollowUpCreateDto
    {
        public int? CustomerId { get; set; }
        public int? LeadId { get; set; }
        public int? OpportunityId { get; set; }

        [Required(ErrorMessage = "Follow-up date is required.")]
        public DateTime FollowUpDate { get; set; }

        public string FollowUpType { get; set; } = "Call";

        [Required(ErrorMessage = "Subject is required.")]
        [MaxLength(200)]
        public string Subject { get; set; } = string.Empty;

        public string? Remarks { get; set; }
        public string? AssignedToUserId { get; set; }
    }

    // Pipeline Report DTO
    public class PipelineReportResponseDto
    {
        public decimal TotalPipelineValue { get; set; }
        public decimal WeightedPipelineValue { get; set; }
        public int TotalOpportunities { get; set; }
        public List<StageSummaryDto> Stages { get; set; } = new();
    }

    public class StageSummaryDto
    {
        public string Stage { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Amount { get; set; }
        public decimal WeightedAmount { get; set; }
    }
}

