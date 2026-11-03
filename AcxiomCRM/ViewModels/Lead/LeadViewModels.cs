using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.ViewModels.Lead
{
    public class LeadCreateEditViewModel
    {
        public int LeadId { get; set; }

        public string? LeadCode { get; set; }

        [Required(ErrorMessage = "Lead name is required.")]
        [MaxLength(150, ErrorMessage = "Lead name cannot exceed 150 characters.")]
        [Display(Name = "Lead Name")]
        public string LeadName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone is required.")]
        [Phone(ErrorMessage = "Enter a valid phone number.")]
        [MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(150)]
        [Display(Name = "Company Name")]
        public string? CompanyName { get; set; }

        [MaxLength(100)]
        public string Source { get; set; } = "Website";

        public LeadStatus Status { get; set; } = LeadStatus.New;

        [Range(0, 9999999999.99, ErrorMessage = "Expected value must be a non-negative amount.")]
        [Display(Name = "Expected Value ($)")]
        public decimal ExpectedValue { get; set; } = 0m;

        public LeadPriority Priority { get; set; } = LeadPriority.Medium;

        public string? Notes { get; set; }

        [Display(Name = "Assigned Sales Executive")]
        public string? AssignedToUserId { get; set; }

        public List<SelectListItem> SalesExecutivesList { get; set; } = new();
    }

    public class LeadConvertViewModel
    {
        public int LeadId { get; set; }
        public string LeadCode { get; set; } = string.Empty;
        public string LeadName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public decimal ExpectedValue { get; set; }

        [Display(Name = "Create Opportunity Deal upon Conversion")]
        public bool CreateOpportunity { get; set; } = true;

        [Display(Name = "Opportunity Deal Name")]
        public string? OpportunityName { get; set; }

        [Range(0.01, 9999999999.99, ErrorMessage = "Opportunity Amount must be greater than 0.")]
        [Display(Name = "Deal Amount ($)")]
        public decimal? OpportunityAmount { get; set; }

        [Display(Name = "Expected Close Date")]
        [DataType(DataType.Date)]
        public DateTime? ExpectedCloseDate { get; set; }
    }
}

