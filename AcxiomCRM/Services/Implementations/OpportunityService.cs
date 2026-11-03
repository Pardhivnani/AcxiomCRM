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
    public class OpportunityService : IOpportunityService
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserScopeService _userScopeService;
        private readonly IAuditLogService _auditLogService;

        public OpportunityService(
            ApplicationDbContext context,
            IUserScopeService userScopeService,
            IAuditLogService auditLogService)
        {
            _context = context;
            _userScopeService = userScopeService;
            _auditLogService = auditLogService;
        }

        public async Task<PagedResult<Opportunity>> GetOpportunitiesAsync(
            ClaimsPrincipal user,
            string? searchTerm,
            OpportunityStage? stage,
            OpportunityStatus? status,
            string? assignedToUserId,
            int? customerId,
            int page = 1,
            int pageSize = 10,
            string? sortBy = "CreatedDate",
            bool isAscending = false)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var accessibleUserIds = await _userScopeService.GetAccessibleUserIdsAsync(user);

            var query = _context.Opportunities
                .Include(o => o.Customer)
                .Include(o => o.AssignedTo)
                .Include(o => o.Lead)
                .AsNoTracking()
                .AsQueryable();

            if (accessibleUserIds != null)
            {
                query = query.Where(o => accessibleUserIds.Contains(o.AssignedToUserId));
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(o =>
                    o.OpportunityName.ToLower().Contains(term) ||
                    (o.Customer != null && o.Customer.CustomerName.ToLower().Contains(term)));
            }

            if (stage.HasValue)
            {
                query = query.Where(o => o.Stage == stage.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(o => o.Status == status.Value);
            }

            if (!string.IsNullOrWhiteSpace(assignedToUserId))
            {
                query = query.Where(o => o.AssignedToUserId == assignedToUserId);
            }

            if (customerId.HasValue)
            {
                query = query.Where(o => o.CustomerId == customerId.Value);
            }

            query = sortBy switch
            {
                "OpportunityName" => isAscending ? query.OrderBy(o => o.OpportunityName) : query.OrderByDescending(o => o.OpportunityName),
                "CustomerName" => isAscending ? query.OrderBy(o => o.Customer!.CustomerName) : query.OrderByDescending(o => o.Customer!.CustomerName),
                "Amount" => isAscending ? query.OrderBy(o => o.Amount) : query.OrderByDescending(o => o.Amount),
                "Stage" => isAscending ? query.OrderBy(o => o.Stage) : query.OrderByDescending(o => o.Stage),
                "Probability" => isAscending ? query.OrderBy(o => o.Probability) : query.OrderByDescending(o => o.Probability),
                "ExpectedCloseDate" => isAscending ? query.OrderBy(o => o.ExpectedCloseDate) : query.OrderByDescending(o => o.ExpectedCloseDate),
                "Status" => isAscending ? query.OrderBy(o => o.Status) : query.OrderByDescending(o => o.Status),
                _ => isAscending ? query.OrderBy(o => o.CreatedDate) : query.OrderByDescending(o => o.CreatedDate)
            };

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Opportunity>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<Opportunity?> GetByIdAsync(int id, ClaimsPrincipal? user = null)
        {
            var opp = await _context.Opportunities
                .Include(o => o.Customer)
                .Include(o => o.AssignedTo)
                .Include(o => o.Lead)
                .FirstOrDefaultAsync(o => o.OpportunityId == id);

            if (opp == null)
                return null;

            if (user != null)
            {
                var canAccess = await _userScopeService.CanAccessRecordAsync(user, opp.AssignedToUserId);
                if (!canAccess)
                    return null;
            }

            return opp;
        }

        public async Task<Opportunity?> GetWithDetailsAsync(int id, ClaimsPrincipal user)
        {
            var opp = await _context.Opportunities
                .Include(o => o.Customer)
                .Include(o => o.AssignedTo)
                .Include(o => o.Lead)
                .Include(o => o.FollowUps.OrderByDescending(f => f.FollowUpDate)).ThenInclude(f => f.AssignedTo)
                .Include(o => o.Activities.OrderByDescending(a => a.ActivityDate)).ThenInclude(a => a.AssignedTo)
                .FirstOrDefaultAsync(o => o.OpportunityId == id);

            if (opp == null)
                return null;

            var canAccess = await _userScopeService.CanAccessRecordAsync(user, opp.AssignedToUserId);
            if (!canAccess)
                return null;

            return opp;
        }

        public async Task<(bool Success, string? ErrorMessage, Opportunity? Opportunity)> CreateAsync(
            Opportunity opportunity,
            ClaimsPrincipal user,
            string? ipAddress = null)
        {
            if (string.IsNullOrWhiteSpace(opportunity.OpportunityName))
                return (false, "Opportunity name is required.", null);

            if (opportunity.Amount <= 0)
                return (false, "Opportunity Amount must be greater than 0.", null);

            if (opportunity.Probability < 0 || opportunity.Probability > 100)
                return (false, "Probability must be between 0 and 100.", null);

            if (opportunity.ExpectedCloseDate.Date < DateTime.UtcNow.Date && opportunity.Status == OpportunityStatus.Open)
                return (false, "Expected Close Date cannot be in the past.", null);

            // Verify Customer exists
            var customerExists = await _context.Customers.AnyAsync(c => c.CustomerId == opportunity.CustomerId);
            if (!customerExists)
                return (false, "The selected customer does not exist.", null);

            // Verify Assigned user
            var userExists = await _context.Users.AnyAsync(u => u.Id == opportunity.AssignedToUserId);
            if (!userExists)
                return (false, "The assigned user is not valid.", null);

            // Stage-to-Status alignment
            SyncStageAndStatus(opportunity);

            opportunity.CreatedDate = DateTime.UtcNow;

            _context.Opportunities.Add(opportunity);
            await _context.SaveChangesAsync();

            var currentUser = await _userScopeService.GetCurrentUserAsync(user);
            var logValue = JsonSerializer.Serialize(new
            {
                opportunity.OpportunityId,
                opportunity.OpportunityName,
                opportunity.CustomerId,
                opportunity.Amount,
                opportunity.Stage,
                opportunity.Probability,
                opportunity.ExpectedCloseDate,
                opportunity.Status,
                opportunity.AssignedToUserId
            });

            await _auditLogService.LogAsync(
                currentUser?.Id,
                currentUser?.Email,
                "Create",
                "Opportunity",
                opportunity.OpportunityId.ToString(),
                null,
                logValue,
                ipAddress,
                "Success");

            return (true, null, opportunity);
        }

        public async Task<(bool Success, string? ErrorMessage)> UpdateAsync(
            Opportunity opportunity,
            ClaimsPrincipal user,
            string? ipAddress = null)
        {
            var existing = await _context.Opportunities.FirstOrDefaultAsync(o => o.OpportunityId == opportunity.OpportunityId);
            if (existing == null)
                return (false, "Opportunity not found.");

            var canAccess = await _userScopeService.CanAccessRecordAsync(user, existing.AssignedToUserId);
            if (!canAccess)
                return (false, "You do not have permission to modify this opportunity.");

            if (opportunity.Amount <= 0)
                return (false, "Opportunity Amount must be greater than 0.");

            if (opportunity.Probability < 0 || opportunity.Probability > 100)
                return (false, "Probability must be between 0 and 100.");

            if (opportunity.ExpectedCloseDate.Date < DateTime.UtcNow.Date && opportunity.Status == OpportunityStatus.Open)
                return (false, "Expected Close Date cannot be in the past.");

            var oldValue = JsonSerializer.Serialize(new
            {
                existing.OpportunityName,
                existing.CustomerId,
                existing.Amount,
                existing.Stage,
                existing.Probability,
                existing.ExpectedCloseDate,
                existing.Status,
                existing.AssignedToUserId
            });

            existing.OpportunityName = opportunity.OpportunityName;
            existing.CustomerId = opportunity.CustomerId;
            existing.LeadId = opportunity.LeadId;
            existing.Amount = opportunity.Amount;
            existing.Stage = opportunity.Stage;
            existing.Probability = opportunity.Probability;
            existing.ExpectedCloseDate = opportunity.ExpectedCloseDate;
            existing.Status = opportunity.Status;
            existing.AssignedToUserId = opportunity.AssignedToUserId;
            existing.Notes = opportunity.Notes;
            existing.ModifiedDate = DateTime.UtcNow;

            SyncStageAndStatus(existing);

            await _context.SaveChangesAsync();

            var currentUser = await _userScopeService.GetCurrentUserAsync(user);
            var newValue = JsonSerializer.Serialize(new
            {
                existing.OpportunityName,
                existing.CustomerId,
                existing.Amount,
                existing.Stage,
                existing.Probability,
                existing.ExpectedCloseDate,
                existing.Status,
                existing.AssignedToUserId
            });

            await _auditLogService.LogAsync(
                currentUser?.Id,
                currentUser?.Email,
                "Update",
                "Opportunity",
                existing.OpportunityId.ToString(),
                oldValue,
                newValue,
                ipAddress,
                "Success");

            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> UpdateStageAsync(
            int id,
            OpportunityStage newStage,
            ClaimsPrincipal user,
            string? ipAddress = null)
        {
            var existing = await _context.Opportunities.FirstOrDefaultAsync(o => o.OpportunityId == id);
            if (existing == null)
                return (false, "Opportunity not found.");

            var canAccess = await _userScopeService.CanAccessRecordAsync(user, existing.AssignedToUserId);
            if (!canAccess)
                return (false, "You do not have permission to modify this opportunity.");

            var oldStage = existing.Stage.ToString();
            existing.Stage = newStage;
            SyncStageAndStatus(existing);
            existing.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var currentUser = await _userScopeService.GetCurrentUserAsync(user);
            await _auditLogService.LogAsync(
                currentUser?.Id,
                currentUser?.Email,
                "UpdateStage",
                "Opportunity",
                existing.OpportunityId.ToString(),
                $"Stage: {oldStage}",
                $"Stage changed to {newStage}",
                ipAddress,
                "Success");

            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> DeleteAsync(
            int id,
            ClaimsPrincipal user,
            string? ipAddress = null)
        {
            var opportunity = await _context.Opportunities.FirstOrDefaultAsync(o => o.OpportunityId == id);
            if (opportunity == null)
                return (false, "Opportunity not found.");

            var canAccess = await _userScopeService.CanAccessRecordAsync(user, opportunity.AssignedToUserId);
            if (!canAccess)
                return (false, "You do not have permission to delete this opportunity.");

            var currentUser = await _userScopeService.GetCurrentUserAsync(user);

            _context.Opportunities.Remove(opportunity);
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                currentUser?.Id,
                currentUser?.Email,
                "Delete",
                "Opportunity",
                id.ToString(),
                $"Deleted opportunity '{opportunity.OpportunityName}' (${opportunity.Amount})",
                null,
                ipAddress,
                "Success");

            return (true, null);
        }

        public async Task<(decimal TotalPipeline, decimal WeightedPipeline, int TotalOpportunities)> GetPipelineSummaryAsync(ClaimsPrincipal user)
        {
            var accessibleUserIds = await _userScopeService.GetAccessibleUserIdsAsync(user);

            var query = _context.Opportunities
                .Where(o => o.Status == OpportunityStatus.Open);

            if (accessibleUserIds != null)
            {
                query = query.Where(o => accessibleUserIds.Contains(o.AssignedToUserId));
            }

            var opps = await query.Select(o => new { o.Amount, o.Probability }).ToListAsync();

            var totalAmount = opps.Sum(o => o.Amount);
            var weighted = opps.Sum(o => o.Amount * o.Probability / 100m);

            return (totalAmount, Math.Round(weighted, 2), opps.Count);
        }

        private static void SyncStageAndStatus(Opportunity opp)
        {
            if (opp.Stage == OpportunityStage.Won)
            {
                opp.Status = OpportunityStatus.Won;
                opp.Probability = 100;
            }
            else if (opp.Stage == OpportunityStage.Lost)
            {
                opp.Status = OpportunityStatus.Lost;
                opp.Probability = 0;
            }
            else
            {
                opp.Status = OpportunityStatus.Open;
            }
        }
    }
}

