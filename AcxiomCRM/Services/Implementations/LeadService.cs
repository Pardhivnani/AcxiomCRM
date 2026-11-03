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
    public class LeadService : ILeadService
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserScopeService _userScopeService;
        private readonly IAuditLogService _auditLogService;
        private readonly ICustomerService _customerService;

        public LeadService(
            ApplicationDbContext context,
            IUserScopeService userScopeService,
            IAuditLogService auditLogService,
            ICustomerService customerService)
        {
            _context = context;
            _userScopeService = userScopeService;
            _auditLogService = auditLogService;
            _customerService = customerService;
        }

        public async Task<string> GenerateLeadCodeAsync()
        {
            var lastLead = await _context.Leads
                .OrderByDescending(l => l.LeadId)
                .Select(l => l.LeadId)
                .FirstOrDefaultAsync();

            int nextNumber = (lastLead > 0 ? lastLead : 1000) + 1;
            return $"LEAD-{nextNumber}";
        }

        public async Task<PagedResult<Lead>> GetLeadsAsync(
            ClaimsPrincipal user,
            string? searchTerm,
            LeadStatus? status,
            LeadPriority? priority,
            string? assignedToUserId,
            int page = 1,
            int pageSize = 10,
            string? sortBy = "CreatedDate",
            bool isAscending = false)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var accessibleUserIds = await _userScopeService.GetAccessibleUserIdsAsync(user);

            var query = _context.Leads
                .Include(l => l.AssignedTo)
                .Include(l => l.Customer)
                .AsNoTracking()
                .AsQueryable();

            if (accessibleUserIds != null)
            {
                query = query.Where(l => l.AssignedToUserId != null && accessibleUserIds.Contains(l.AssignedToUserId));
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(l =>
                    l.LeadName.ToLower().Contains(term) ||
                    l.Email.ToLower().Contains(term) ||
                    l.Phone.Contains(term) ||
                    (l.CompanyName != null && l.CompanyName.ToLower().Contains(term)) ||
                    l.LeadCode.ToLower().Contains(term));
            }

            if (status.HasValue)
            {
                query = query.Where(l => l.Status == status.Value);
            }

            if (priority.HasValue)
            {
                query = query.Where(l => l.Priority == priority.Value);
            }

            if (!string.IsNullOrWhiteSpace(assignedToUserId))
            {
                query = query.Where(l => l.AssignedToUserId == assignedToUserId);
            }

            query = sortBy switch
            {
                "LeadName" => isAscending ? query.OrderBy(l => l.LeadName) : query.OrderByDescending(l => l.LeadName),
                "LeadCode" => isAscending ? query.OrderBy(l => l.LeadCode) : query.OrderByDescending(l => l.LeadCode),
                "CompanyName" => isAscending ? query.OrderBy(l => l.CompanyName) : query.OrderByDescending(l => l.CompanyName),
                "Status" => isAscending ? query.OrderBy(l => l.Status) : query.OrderByDescending(l => l.Status),
                "Priority" => isAscending ? query.OrderBy(l => l.Priority) : query.OrderByDescending(l => l.Priority),
                "ExpectedValue" => isAscending ? query.OrderBy(l => l.ExpectedValue) : query.OrderByDescending(l => l.ExpectedValue),
                _ => isAscending ? query.OrderBy(l => l.CreatedDate) : query.OrderByDescending(l => l.CreatedDate)
            };

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Lead>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<Lead?> GetByIdAsync(int id, ClaimsPrincipal? user = null)
        {
            var lead = await _context.Leads
                .Include(l => l.AssignedTo)
                .Include(l => l.Customer)
                .FirstOrDefaultAsync(l => l.LeadId == id);

            if (lead == null)
                return null;

            if (user != null)
            {
                var canAccess = await _userScopeService.CanAccessRecordAsync(user, lead.AssignedToUserId);
                if (!canAccess)
                    return null;
            }

            return lead;
        }

        public async Task<Lead?> GetWithDetailsAsync(int id, ClaimsPrincipal user)
        {
            var lead = await _context.Leads
                .Include(l => l.AssignedTo)
                .Include(l => l.Customer)
                .Include(l => l.Opportunities).ThenInclude(o => o.AssignedTo)
                .Include(l => l.FollowUps.OrderByDescending(f => f.FollowUpDate)).ThenInclude(f => f.AssignedTo)
                .Include(l => l.Activities.OrderByDescending(a => a.ActivityDate)).ThenInclude(a => a.AssignedTo)
                .FirstOrDefaultAsync(l => l.LeadId == id);

            if (lead == null)
                return null;

            var canAccess = await _userScopeService.CanAccessRecordAsync(user, lead.AssignedToUserId);
            if (!canAccess)
                return null;

            return lead;
        }

        public async Task<(bool Success, string? ErrorMessage, Lead? Lead)> CreateAsync(
            Lead lead,
            ClaimsPrincipal user,
            string? ipAddress = null)
        {
            if (string.IsNullOrWhiteSpace(lead.LeadName))
                return (false, "Lead name is required.", null);

            if (string.IsNullOrWhiteSpace(lead.Email))
                return (false, "Email is required.", null);

            if (string.IsNullOrWhiteSpace(lead.Phone))
                return (false, "Phone is required.", null);

            if (string.IsNullOrEmpty(lead.LeadCode))
            {
                lead.LeadCode = await GenerateLeadCodeAsync();
            }

            var currentUser = await _userScopeService.GetCurrentUserAsync(user);
            if (string.IsNullOrEmpty(lead.AssignedToUserId) && user.IsInRole(UserRoles.SalesExecutive))
            {
                lead.AssignedToUserId = currentUser?.Id;
            }

            lead.CreatedDate = DateTime.UtcNow;

            _context.Leads.Add(lead);
            await _context.SaveChangesAsync();

            var logValue = JsonSerializer.Serialize(new
            {
                lead.LeadId,
                lead.LeadCode,
                lead.LeadName,
                lead.Email,
                lead.Phone,
                lead.CompanyName,
                lead.Status,
                lead.ExpectedValue,
                lead.AssignedToUserId
            });

            await _auditLogService.LogAsync(
                currentUser?.Id,
                currentUser?.Email,
                "Create",
                "Lead",
                lead.LeadId.ToString(),
                null,
                logValue,
                ipAddress,
                "Success");

            return (true, null, lead);
        }

        public async Task<(bool Success, string? ErrorMessage)> UpdateAsync(
            Lead lead,
            ClaimsPrincipal user,
            string? ipAddress = null)
        {
            var existing = await _context.Leads.FirstOrDefaultAsync(l => l.LeadId == lead.LeadId);
            if (existing == null)
                return (false, "Lead not found.");

            var canAccess = await _userScopeService.CanAccessRecordAsync(user, existing.AssignedToUserId);
            if (!canAccess)
                return (false, "You do not have permission to modify this lead.");

            // Prevent changing status back if already converted
            if (existing.Status == LeadStatus.Converted && lead.Status != LeadStatus.Converted)
            {
                return (false, "Converted leads cannot have their status reversed.");
            }

            var oldValue = JsonSerializer.Serialize(new
            {
                existing.LeadName,
                existing.Email,
                existing.Phone,
                existing.CompanyName,
                existing.Source,
                existing.Status,
                existing.ExpectedValue,
                existing.Priority,
                existing.AssignedToUserId
            });

            existing.LeadName = lead.LeadName;
            existing.Email = lead.Email;
            existing.Phone = lead.Phone;
            existing.CompanyName = lead.CompanyName;
            existing.Source = lead.Source;
            existing.Status = lead.Status;
            existing.ExpectedValue = lead.ExpectedValue;
            existing.Priority = lead.Priority;
            existing.Notes = lead.Notes;
            existing.AssignedToUserId = lead.AssignedToUserId;
            existing.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var currentUser = await _userScopeService.GetCurrentUserAsync(user);
            var newValue = JsonSerializer.Serialize(new
            {
                existing.LeadName,
                existing.Email,
                existing.Phone,
                existing.CompanyName,
                existing.Source,
                existing.Status,
                existing.ExpectedValue,
                existing.Priority,
                existing.AssignedToUserId
            });

            await _auditLogService.LogAsync(
                currentUser?.Id,
                currentUser?.Email,
                "Update",
                "Lead",
                existing.LeadId.ToString(),
                oldValue,
                newValue,
                ipAddress,
                "Success");

            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> DeleteAsync(
            int id,
            ClaimsPrincipal user,
            string? ipAddress = null)
        {
            var lead = await _context.Leads
                .Include(l => l.Opportunities)
                .Include(l => l.FollowUps)
                .Include(l => l.Activities)
                .FirstOrDefaultAsync(l => l.LeadId == id);

            if (lead == null)
                return (false, "Lead not found.");

            var canAccess = await _userScopeService.CanAccessRecordAsync(user, lead.AssignedToUserId);
            if (!canAccess)
                return (false, "You do not have permission to delete this lead.");

            if (lead.Status == LeadStatus.Converted)
                return (false, "Converted leads cannot be deleted because they are associated with an active customer account.");

            var currentUser = await _userScopeService.GetCurrentUserAsync(user);

            _context.Leads.Remove(lead);
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                currentUser?.Id,
                currentUser?.Email,
                "Delete",
                "Lead",
                id.ToString(),
                $"Deleted lead '{lead.LeadName}' ({lead.LeadCode})",
                null,
                ipAddress,
                "Success");

            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage, Customer? CreatedCustomer, Opportunity? CreatedOpportunity)> ConvertLeadAsync(
            int leadId,
            bool createOpportunity,
            string? opportunityName,
            decimal? opportunityAmount,
            DateTime? expectedCloseDate,
            ClaimsPrincipal user,
            string? ipAddress = null)
        {
            var lead = await _context.Leads.FirstOrDefaultAsync(l => l.LeadId == leadId);
            if (lead == null)
                return (false, "Lead not found.", null, null);

            var canAccess = await _userScopeService.CanAccessRecordAsync(user, lead.AssignedToUserId);
            if (!canAccess)
                return (false, "You do not have permission to convert this lead.", null, null);

            if (lead.Status == LeadStatus.Converted)
                return (false, "This lead has already been converted into a customer.", null, null);

            var currentUser = await _userScopeService.GetCurrentUserAsync(user);

            // Check if customer with this email or phone already exists
            Customer? customer = await _context.Customers.FirstOrDefaultAsync(c =>
                c.Email.ToLower() == lead.Email.ToLower() || c.Phone == lead.Phone);

            if (customer == null)
            {
                customer = new Customer
                {
                    CustomerCode = await _customerService.GenerateCustomerCodeAsync(),
                    CustomerName = !string.IsNullOrWhiteSpace(lead.CompanyName) ? lead.CompanyName : lead.LeadName,
                    Email = lead.Email,
                    Phone = lead.Phone,
                    CompanyName = lead.CompanyName,
                    Status = CustomerStatus.Active,
                    CreatedDate = DateTime.UtcNow,
                    CreatedByUserId = currentUser?.Id,
                    AssignedToUserId = lead.AssignedToUserId ?? currentUser?.Id
                };

                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();

                await _auditLogService.LogAsync(
                    currentUser?.Id,
                    currentUser?.Email,
                    "Create",
                    "Customer",
                    customer.CustomerId.ToString(),
                    null,
                    $"Created customer '{customer.CustomerName}' from Lead {lead.LeadCode}",
                    ipAddress,
                    "Success");
            }

            Opportunity? opportunity = null;
            if (createOpportunity)
            {
                var oppName = !string.IsNullOrWhiteSpace(opportunityName) ? opportunityName : $"{customer.CustomerName} - New Business Deal";
                var amount = opportunityAmount.HasValue && opportunityAmount.Value > 0 ? opportunityAmount.Value : (lead.ExpectedValue > 0 ? lead.ExpectedValue : 1000m);
                var closeDate = expectedCloseDate.HasValue && expectedCloseDate.Value.Date >= DateTime.UtcNow.Date
                    ? expectedCloseDate.Value
                    : DateTime.UtcNow.AddDays(30);

                opportunity = new Opportunity
                {
                    OpportunityName = oppName,
                    CustomerId = customer.CustomerId,
                    LeadId = lead.LeadId,
                    Amount = amount,
                    Stage = OpportunityStage.Qualification,
                    Probability = 20,
                    ExpectedCloseDate = closeDate,
                    Status = OpportunityStatus.Open,
                    CreatedDate = DateTime.UtcNow,
                    AssignedToUserId = lead.AssignedToUserId ?? currentUser?.Id ?? string.Empty,
                    Notes = $"Created via conversion of Lead {lead.LeadCode} ({lead.LeadName})."
                };

                _context.Opportunities.Add(opportunity);
                await _context.SaveChangesAsync();

                await _auditLogService.LogAsync(
                    currentUser?.Id,
                    currentUser?.Email,
                    "Create",
                    "Opportunity",
                    opportunity.OpportunityId.ToString(),
                    null,
                    $"Created Opportunity '{opportunity.OpportunityName}' from Lead {lead.LeadCode}",
                    ipAddress,
                    "Success");
            }

            // Update lead status and link
            var oldLeadStatus = lead.Status.ToString();
            lead.Status = LeadStatus.Converted;
            lead.CustomerId = customer.CustomerId;
            lead.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                currentUser?.Id,
                currentUser?.Email,
                "Convert",
                "Lead",
                lead.LeadId.ToString(),
                $"Status: {oldLeadStatus}",
                $"Converted to Customer ID {customer.CustomerId} ({customer.CustomerName})",
                ipAddress,
                "Success");

            return (true, null, customer, opportunity);
        }
    }
}

