using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Models.Entities
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [MaxLength(100)]
        public string? Department { get; set; }

        public string? ManagerId { get; set; }
        public virtual ApplicationUser? Manager { get; set; }

        public virtual ICollection<ApplicationUser> DirectReports { get; set; } = new List<ApplicationUser>();
        public virtual ICollection<Customer> AssignedCustomers { get; set; } = new List<Customer>();
        public virtual ICollection<Lead> AssignedLeads { get; set; } = new List<Lead>();
        public virtual ICollection<Opportunity> AssignedOpportunities { get; set; } = new List<Opportunity>();
        public virtual ICollection<FollowUp> AssignedFollowUps { get; set; } = new List<FollowUp>();
        public virtual ICollection<Activity> AssignedActivities { get; set; } = new List<Activity>();
    }
}

