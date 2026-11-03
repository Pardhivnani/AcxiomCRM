using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Services.Interfaces;
using AcxiomCRM.ViewModels.Common;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace AcxiomCRM.Services.Implementations
{
    public class CustomerService : ICustomerService
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserScopeService _userScopeService;
        private readonly IAuditLogService _auditLogService;

        public CustomerService(
            ApplicationDbContext context,
            IUserScopeService userScopeService,
            IAuditLogService auditLogService)
        {
            _context = context;
            _userScopeService = userScopeService;
            _auditLogService = auditLogService;
        }

        public async Task<string> GenerateCustomerCodeAsync()
        {
            var lastCustomer = await _context.Customers
                .OrderByDescending(c => c.CustomerId)
                .Select(c => c.CustomerId)
                .FirstOrDefaultAsync();

            int nextNumber = (lastCustomer > 0 ? lastCustomer : 1000) + 1;
            return $"CUST-{nextNumber}";
        }

        public async Task<bool> IsEmailUniqueAsync(string email, int? excludeCustomerId = null)
        {
            if (string.IsNullOrWhiteSpace(email)) return true;
            var normalizedEmail = email.Trim().ToLower();

            var query = _context.Customers.Where(c => c.Email.ToLower() == normalizedEmail);
            if (excludeCustomerId.HasValue)
            {
                query = query.Where(c => c.CustomerId != excludeCustomerId.Value);
            }

            return !await query.AnyAsync();
        }

        public async Task<bool> IsPhoneUniqueAsync(string phone, int? excludeCustomerId = null)
        {
            if (string.IsNullOrWhiteSpace(phone)) return true;
            var normalizedPhone = phone.Trim();

            var query = _context.Customers.Where(c => c.Phone == normalizedPhone);
            if (excludeCustomerId.HasValue)
            {
                query = query.Where(c => c.CustomerId != excludeCustomerId.Value);
            }

            return !await query.AnyAsync();
        }

        public async Task<PagedResult<Customer>> GetCustomersAsync(
            ClaimsPrincipal user,
            string? searchTerm,
            CustomerStatus? status,
            string? assignedToUserId,
            int page = 1,
            int pageSize = 10,
            string? sortBy = "CustomerName",
            bool isAscending = true)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var accessibleUserIds = await _userScopeService.GetAccessibleUserIdsAsync(user);

            var query = _context.Customers
                .Include(c => c.AssignedTo)
                .Include(c => c.CreatedBy)
                .AsNoTracking()
                .AsQueryable();

            // Enforce ownership visibility
            if (accessibleUserIds != null)
            {
                query = query.Where(c => c.AssignedToUserId != null && accessibleUserIds.Contains(c.AssignedToUserId));
            }

            // Search filter
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(c =>
                    c.CustomerName.ToLower().Contains(term) ||
                    c.Email.ToLower().Contains(term) ||
                    c.Phone.Contains(term) ||
                    (c.CompanyName != null && c.CompanyName.ToLower().Contains(term)) ||
                    c.CustomerCode.ToLower().Contains(term));
            }

            // Status filter
            if (status.HasValue)
            {
                query = query.Where(c => c.Status == status.Value);
            }

            // Assigned user filter
            if (!string.IsNullOrWhiteSpace(assignedToUserId))
            {
                query = query.Where(c => c.AssignedToUserId == assignedToUserId);
            }

            // Sorting
            query = sortBy switch
            {
                "CustomerCode" => isAscending ? query.OrderBy(c => c.CustomerCode) : query.OrderByDescending(c => c.CustomerCode),
                "Email" => isAscending ? query.OrderBy(c => c.Email) : query.OrderByDescending(c => c.Email),
                "CompanyName" => isAscending ? query.OrderBy(c => c.CompanyName) : query.OrderByDescending(c => c.CompanyName),
                "CreatedDate" => isAscending ? query.OrderBy(c => c.CreatedDate) : query.OrderByDescending(c => c.CreatedDate),
                "Status" => isAscending ? query.OrderBy(c => c.Status) : query.OrderByDescending(c => c.Status),
                _ => isAscending ? query.OrderBy(c => c.CustomerName) : query.OrderByDescending(c => c.CustomerName)
            };

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Customer>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<Customer?> GetByIdAsync(int id, ClaimsPrincipal? user = null)
        {
            var customer = await _context.Customers
                .Include(c => c.AssignedTo)
                .Include(c => c.CreatedBy)
                .FirstOrDefaultAsync(c => c.CustomerId == id);

            if (customer == null)
                return null;

            if (user != null)
            {
                var canAccess = await _userScopeService.CanAccessRecordAsync(user, customer.AssignedToUserId);
                if (!canAccess)
                    return null;
            }

            return customer;
        }

        public async Task<Customer?> GetWithDetailsAsync(int id, ClaimsPrincipal user)
        {
            var customer = await _context.Customers
                .Include(c => c.AssignedTo)
                .Include(c => c.CreatedBy)
                .Include(c => c.Leads).ThenInclude(l => l.AssignedTo)
                .Include(c => c.Opportunities).ThenInclude(o => o.AssignedTo)
                .Include(c => c.FollowUps.OrderByDescending(f => f.FollowUpDate)).ThenInclude(f => f.AssignedTo)
                .Include(c => c.Activities.OrderByDescending(a => a.ActivityDate)).ThenInclude(a => a.AssignedTo)
                .FirstOrDefaultAsync(c => c.CustomerId == id);

            if (customer == null)
                return null;

            var canAccess = await _userScopeService.CanAccessRecordAsync(user, customer.AssignedToUserId);
            if (!canAccess)
                return null;

            return customer;
        }

        public async Task<(bool Success, string? ErrorMessage, Customer? Customer)> CreateAsync(
            Customer customer,
            ClaimsPrincipal user,
            string? ipAddress = null)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(customer.CustomerName))
                return (false, "Customer name is required.", null);

            if (string.IsNullOrWhiteSpace(customer.Email))
                return (false, "Email is required.", null);

            if (string.IsNullOrWhiteSpace(customer.Phone))
                return (false, "Phone is required.", null);

            if (!await IsEmailUniqueAsync(customer.Email))
                return (false, "A customer with this email address already exists.", null);

            if (!await IsPhoneUniqueAsync(customer.Phone))
                return (false, "A customer with this phone number already exists.", null);

            var currentUser = await _userScopeService.GetCurrentUserAsync(user);
            customer.CreatedByUserId = currentUser?.Id;
            if (string.IsNullOrEmpty(customer.CustomerCode))
            {
                customer.CustomerCode = await GenerateCustomerCodeAsync();
            }
            customer.CreatedDate = DateTime.UtcNow;

            // If sales executive is creating and assigned user is not specified, assign to self
            if (string.IsNullOrEmpty(customer.AssignedToUserId) && user.IsInRole(UserRoles.SalesExecutive))
            {
                customer.AssignedToUserId = currentUser?.Id;
            }

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            var logValue = JsonSerializer.Serialize(new
            {
                customer.CustomerId,
                customer.CustomerCode,
                customer.CustomerName,
                customer.Email,
                customer.Phone,
                customer.CompanyName,
                customer.Status,
                customer.AssignedToUserId
            });

            await _auditLogService.LogAsync(
                currentUser?.Id,
                currentUser?.Email,
                "Create",
                "Customer",
                customer.CustomerId.ToString(),
                null,
                logValue,
                ipAddress,
                "Success");

            return (true, null, customer);
        }

        public async Task<(bool Success, string? ErrorMessage)> UpdateAsync(
            Customer customer,
            ClaimsPrincipal user,
            string? ipAddress = null)
        {
            var existing = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerId == customer.CustomerId);
            if (existing == null)
                return (false, "Customer not found.");

            var canAccess = await _userScopeService.CanAccessRecordAsync(user, existing.AssignedToUserId);
            if (!canAccess)
                return (false, "You do not have permission to modify this customer.");

            if (!await IsEmailUniqueAsync(customer.Email, customer.CustomerId))
                return (false, "A customer with this email address already exists.");

            if (!await IsPhoneUniqueAsync(customer.Phone, customer.CustomerId))
                return (false, "A customer with this phone number already exists.");

            var oldValue = JsonSerializer.Serialize(new
            {
                existing.CustomerName,
                existing.Email,
                existing.Phone,
                existing.CompanyName,
                existing.Address,
                existing.City,
                existing.State,
                existing.Status,
                existing.AssignedToUserId
            });

            existing.CustomerName = customer.CustomerName;
            existing.Email = customer.Email;
            existing.Phone = customer.Phone;
            existing.CompanyName = customer.CompanyName;
            existing.Address = customer.Address;
            existing.City = customer.City;
            existing.State = customer.State;
            existing.Status = customer.Status;
            existing.AssignedToUserId = customer.AssignedToUserId;
            existing.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var currentUser = await _userScopeService.GetCurrentUserAsync(user);
            var newValue = JsonSerializer.Serialize(new
            {
                existing.CustomerName,
                existing.Email,
                existing.Phone,
                existing.CompanyName,
                existing.Address,
                existing.City,
                existing.State,
                existing.Status,
                existing.AssignedToUserId
            });

            await _auditLogService.LogAsync(
                currentUser?.Id,
                currentUser?.Email,
                "Update",
                "Customer",
                existing.CustomerId.ToString(),
                oldValue,
                newValue,
                ipAddress,
                "Success");

            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> DeleteOrDeactivateAsync(
            int id,
            ClaimsPrincipal user,
            string? ipAddress = null)
        {
            var customer = await _context.Customers
                .Include(c => c.Opportunities)
                .Include(c => c.Leads)
                .Include(c => c.FollowUps)
                .FirstOrDefaultAsync(c => c.CustomerId == id);

            if (customer == null)
                return (false, "Customer not found.");

            var canAccess = await _userScopeService.CanAccessRecordAsync(user, customer.AssignedToUserId);
            if (!canAccess)
                return (false, "You do not have permission to delete or deactivate this customer.");

            var currentUser = await _userScopeService.GetCurrentUserAsync(user);

            // If customer has related records, deactivate instead of hard deleting to preserve referential integrity
            bool hasDependencies = customer.Opportunities.Any() || customer.Leads.Any() || customer.FollowUps.Any();

            if (hasDependencies)
            {
                customer.Status = CustomerStatus.Inactive;
                customer.ModifiedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                await _auditLogService.LogAsync(
                    currentUser?.Id,
                    currentUser?.Email,
                    "Deactivate",
                    "Customer",
                    customer.CustomerId.ToString(),
                    "Status: Active",
                    "Status: Inactive (Deactivated due to related CRM history)",
                    ipAddress,
                    "Success");
            }
            else
            {
                _context.Customers.Remove(customer);
                await _context.SaveChangesAsync();

                await _auditLogService.LogAsync(
                    currentUser?.Id,
                    currentUser?.Email,
                    "Delete",
                    "Customer",
                    id.ToString(),
                    $"Deleted customer '{customer.CustomerName}'",
                    null,
                    ipAddress,
                    "Success");
            }

            return (true, null);
        }
    }
}

