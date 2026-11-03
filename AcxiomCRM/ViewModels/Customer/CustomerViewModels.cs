using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.ViewModels.Customer
{
    public class CustomerCreateEditViewModel
    {
        public int CustomerId { get; set; }

        public string? CustomerCode { get; set; }

        [Required(ErrorMessage = "Customer name is required.")]
        [MaxLength(150, ErrorMessage = "Customer name cannot exceed 150 characters.")]
        [Display(Name = "Customer Name")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [MaxLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone is required.")]
        [Phone(ErrorMessage = "Enter a valid phone number.")]
        [MaxLength(20, ErrorMessage = "Phone cannot exceed 20 characters.")]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(150)]
        [Display(Name = "Company Name")]
        public string? CompanyName { get; set; }

        [MaxLength(250)]
        public string? Address { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(100)]
        public string? State { get; set; }

        public CustomerStatus Status { get; set; } = CustomerStatus.Active;

        [Display(Name = "Assigned Sales Executive")]
        public string? AssignedToUserId { get; set; }

        public List<SelectListItem> SalesExecutivesList { get; set; } = new();
    }

    public class CustomerDetailsViewModel
    {
        public AcxiomCRM.Models.Entities.Customer Customer { get; set; } = null!;
        public decimal TotalOpportunityValue { get; set; }
        public int ActiveDealsCount { get; set; }
    }
}
