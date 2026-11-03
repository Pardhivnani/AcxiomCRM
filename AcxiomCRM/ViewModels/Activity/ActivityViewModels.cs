using AcxiomCRM.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.ViewModels.Activity
{
    public class ActivityCreateEditViewModel
    {
        public int ActivityId { get; set; }

        [Display(Name = "Activity Type")]
        public ActivityType ActivityType { get; set; } = ActivityType.Task;

        [Required(ErrorMessage = "Subject is required.")]
        [MaxLength(200, ErrorMessage = "Subject cannot exceed 200 characters.")]
        public string Subject { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required(ErrorMessage = "Activity date is required.")]
        [Display(Name = "Activity Date")]
        public DateTime ActivityDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Related Customer (Optional)")]
        public int? CustomerId { get; set; }

        [Display(Name = "Related Lead (Optional)")]
        public int? LeadId { get; set; }

        [Display(Name = "Related Opportunity (Optional)")]
        public int? OpportunityId { get; set; }

        [Required(ErrorMessage = "Assigned user is required.")]
        [Display(Name = "Assigned User")]
        public string AssignedToUserId { get; set; } = string.Empty;

        public ActivityStatus Status { get; set; } = ActivityStatus.Completed;

        public List<SelectListItem> CustomersList { get; set; } = new();
        public List<SelectListItem> LeadsList { get; set; } = new();
        public List<SelectListItem> OpportunitiesList { get; set; } = new();
        public List<SelectListItem> UsersList { get; set; } = new();
    }
}

