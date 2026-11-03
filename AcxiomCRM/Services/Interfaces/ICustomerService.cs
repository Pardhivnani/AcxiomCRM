using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.ViewModels.Common;
using System.Security.Claims;

namespace AcxiomCRM.Services.Interfaces
{
    public interface ICustomerService
    {
        Task<PagedResult<Customer>> GetCustomersAsync(
            ClaimsPrincipal user,
            string? searchTerm,
            CustomerStatus? status,
            string? assignedToUserId,
            int page = 1,
            int pageSize = 10,
            string? sortBy = "CustomerName",
            bool isAscending = true);

        Task<Customer?> GetByIdAsync(int id, ClaimsPrincipal? user = null);
        Task<Customer?> GetWithDetailsAsync(int id, ClaimsPrincipal user);
        Task<(bool Success, string? ErrorMessage, Customer? Customer)> CreateAsync(Customer customer, ClaimsPrincipal user, string? ipAddress = null);
        Task<(bool Success, string? ErrorMessage)> UpdateAsync(Customer customer, ClaimsPrincipal user, string? ipAddress = null);
        Task<(bool Success, string? ErrorMessage)> DeleteOrDeactivateAsync(int id, ClaimsPrincipal user, string? ipAddress = null);
        Task<bool> IsEmailUniqueAsync(string email, int? excludeCustomerId = null);
        Task<bool> IsPhoneUniqueAsync(string phone, int? excludeCustomerId = null);
        Task<string> GenerateCustomerCodeAsync();
    }
}

