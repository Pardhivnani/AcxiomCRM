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
    public class FollowUpService : IFollowUpService
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserScopeService _userScopeService;
        private readonly IAuditLogService _auditLogService;

        public FollowUpService(
            ApplicationDbContext context,
            IUserScopeService userScopeService,
            IAuditLogService auditLogService)
        {
            _context = context;
            _userScopeService = userScopeService;
            _auditLogService = auditLogService;
        }

        public async Task<PagedResult<FollowUp>> GetFollowUpsAsync(
            ClaimsPrincipal user,
            string? searchTerm,
            FollowUpStatus? status,
            FollowUpType? type,
            string? assignedToUserId,
            DateTime? startDate,
            DateTime? endDate,
            int page = 1,
            int pageSize = 10,
            string? sortBy = "FollowUpDate",
            bool isAscending = true)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var accessibleUserIds = await _userScopeService.GetAccessibleUserIdsAsync(user);

            var query = _context.FollowUps
                .Include(f => f.Customer)
                .Include(f => f.Lead)
                .Include(f => f.Opportunity)
                .Include(f => f.AssignedTo)
                .AsNoTracking()
                .AsQueryable();

            if (accessibleUserIds != null)
            {
                query = query.Where(f => accessibleUserIds.Contains(f.AssignedToUserId));
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(f =>
                    f.Subject.ToLower().Contains(term) ||
                    (f.Customer != null && f.Customer.CustomerName.ToLower().Contains(term)) ||
                    (f.Lead != null && f.Lead.LeadName.ToLower().Contains(term)));
            }

            if (status.HasValue)
            {
                query = query.Where(f => f.Status == status.Value);
            }

            if (type.HasValue)
            {
                query = query.Where(f => f.FollowUpType == type.Value);
            }

            if (!string.IsNullOrWhiteSpace(assignedToUserId))
            {
                query = query.Where(f => f.AssignedToUserId == assignedToUserId);
            }

            if (startDate.HasValue)
            {
                var start = startDate.Value.Date;
                query = query.Where(f => f.FollowUpDate >= start);
            }

            if (endDate.HasValue)
            {
                var end = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(f => f.FollowUpDate <= end);
            }

            query = sortBy switch
            {
                "Subject" => isAscending ? query.OrderBy(f => f.Subject) : query.OrderByDescending(f => f.Subject),
                "Status" => isAscending ? query.OrderBy(f => f.Status) : query.OrderByDescending(f => f.Status),
                "Type" => isAscending ? query.OrderBy(f => f.FollowUpType) : query.OrderByDescending(f => f.FollowUpType),
                _ => isAscending ? query.OrderBy(f => f.FollowUpDate) : query.OrderByDescending(f => f.FollowUpDate)
            };

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<FollowUp>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<FollowUp?> GetByIdAsync(int id, ClaimsPrincipal? user = null)
        {
            var followUp = await _context.FollowUps
                .Include(f => f.Customer)
                .Include(f => f.Lead)
                .Include(f => f.Opportunity)
                .Include(f => f.AssignedTo)
                .FirstOrDefaultAsync(f => f.FollowUpId == id);

            if (followUp == null)
                return null;

            if (user != null)
            {
                var canAccess = await _userScopeService.CanAccessRecordAsync(user, followUp.AssignedToUserId);
                if (!canAccess)
                    return null;
            }

            return followUp;
        }

        public async Task<(bool Success, string? ErrorMessage, FollowUp? FollowUp)> CreateAsync(
            FollowUp followUp,
            ClaimsPrincipal user,
            string? ipAddress = null)
        {
            if (string.IsNullOrWhiteSpace(followUp.Subject))
                return (false, "Subject is required.", null);

            if (followUp.Status == FollowUpStatus.Planned && followUp.FollowUpDate.Date < DateTime.UtcNow.Date)
                return (false, "Follow-up date cannot be earlier than today.", null);

            var currentUser = await _userScopeService.GetCurrentUserAsync(user);
            if (string.IsNullOrEmpty(followUp.AssignedToUserId))
            {
                followUp.AssignedToUserId = currentUser?.Id ?? string.Empty;
            }

            followUp.CreatedDate = DateTime.UtcNow;

            _context.FollowUps.Add(followUp);
            await _context.SaveChangesAsync();

            var logValue = JsonSerializer.Serialize(new
            {
                followUp.FollowUpId,
                followUp.Subject,
                followUp.FollowUpType,
                followUp.FollowUpDate,
                followUp.Status,
                followUp.AssignedToUserId,
                followUp.CustomerId,
                followUp.LeadId,
                followUp.OpportunityId
            });

            await _auditLogService.LogAsync(
                currentUser?.Id,
                currentUser?.Email,
                "Create",
                "FollowUp",
                followUp.FollowUpId.ToString(),
                null,
                logValue,
                ipAddress,
                "Success");

            return (true, null, followUp);
        }

        public async Task<(bool Success, string? ErrorMessage)> UpdateAsync(
            FollowUp followUp,
            ClaimsPrincipal user,
            string? ipAddress = null)
        {
            var existing = await _context.FollowUps.FirstOrDefaultAsync(f => f.FollowUpId == followUp.FollowUpId);
            if (existing == null)
                return (false, "Follow-up not found.");

            var canAccess = await _userScopeService.CanAccessRecordAsync(user, existing.AssignedToUserId);
            if (!canAccess)
                return (false, "You do not have permission to modify this follow-up.");

            if (followUp.Status == FollowUpStatus.Planned && followUp.FollowUpDate.Date < DateTime.UtcNow.Date)
                return (false, "Follow-up date cannot be earlier than today.");

            var oldValue = JsonSerializer.Serialize(new
            {
                existing.Subject,
                existing.FollowUpType,
                existing.FollowUpDate,
                existing.Remarks,
                existing.Status,
                existing.AssignedToUserId
            });

            existing.Subject = followUp.Subject;
            existing.FollowUpType = followUp.FollowUpType;
            existing.FollowUpDate = followUp.FollowUpDate;
            existing.Remarks = followUp.Remarks;
            existing.Status = followUp.Status;
            existing.AssignedToUserId = followUp.AssignedToUserId;
            existing.CustomerId = followUp.CustomerId;
            existing.LeadId = followUp.LeadId;
            existing.OpportunityId = followUp.OpportunityId;

            if (followUp.Status == FollowUpStatus.Completed && existing.CompletedDate == null)
            {
                existing.CompletedDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            var currentUser = await _userScopeService.GetCurrentUserAsync(user);
            var newValue = JsonSerializer.Serialize(new
            {
                existing.Subject,
                existing.FollowUpType,
                existing.FollowUpDate,
                existing.Remarks,
                existing.Status,
                existing.AssignedToUserId
            });

            await _auditLogService.LogAsync(
                currentUser?.Id,
                currentUser?.Email,
                "Update",
                "FollowUp",
                existing.FollowUpId.ToString(),
                oldValue,
                newValue,
                ipAddress,
                "Success");

            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> CompleteAsync(
            int id,
            string? remarks,
            ClaimsPrincipal user,
            string? ipAddress = null)
        {
            var followUp = await _context.FollowUps.FirstOrDefaultAsync(f => f.FollowUpId == id);
            if (followUp == null)
                return (false, "Follow-up not found.");

            var canAccess = await _userScopeService.CanAccessRecordAsync(user, followUp.AssignedToUserId);
            if (!canAccess)
                return (false, "You do not have permission to modify this follow-up.");

            var oldStatus = followUp.Status.ToString();
            followUp.Status = FollowUpStatus.Completed;
            followUp.CompletedDate = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(remarks))
            {
                followUp.Remarks = string.IsNullOrWhiteSpace(followUp.Remarks)
                    ? remarks
                    : $"{followUp.Remarks}\n[Completion Note]: {remarks}";
            }

            await _context.SaveChangesAsync();

            var currentUser = await _userScopeService.GetCurrentUserAsync(user);
            await _auditLogService.LogAsync(
                currentUser?.Id,
                currentUser?.Email,
                "Complete",
                "FollowUp",
                id.ToString(),
                $"Status: {oldStatus}",
                $"Status: Completed on {followUp.CompletedDate:yyyy-MM-dd HH:mm}",
                ipAddress,
                "Success");

            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> RescheduleAsync(
            int id,
            DateTime newDate,
            string? remarks,
            ClaimsPrincipal user,
            string? ipAddress = null)
        {
            if (newDate.Date < DateTime.UtcNow.Date)
                return (false, "Follow-up date cannot be earlier than today.");

            var followUp = await _context.FollowUps.FirstOrDefaultAsync(f => f.FollowUpId == id);
            if (followUp == null)
                return (false, "Follow-up not found.");

            var canAccess = await _userScopeService.CanAccessRecordAsync(user, followUp.AssignedToUserId);
            if (!canAccess)
                return (false, "You do not have permission to modify this follow-up.");

            var oldDateStr = followUp.FollowUpDate.ToString("yyyy-MM-dd HH:mm");
            followUp.FollowUpDate = newDate;
            followUp.Status = FollowUpStatus.Planned;
            if (!string.IsNullOrWhiteSpace(remarks))
            {
                followUp.Remarks = string.IsNullOrWhiteSpace(followUp.Remarks)
                    ? remarks
                    : $"{followUp.Remarks}\n[Rescheduled from {oldDateStr}]: {remarks}";
            }

            await _context.SaveChangesAsync();

            var currentUser = await _userScopeService.GetCurrentUserAsync(user);
            await _auditLogService.LogAsync(
                currentUser?.Id,
                currentUser?.Email,
                "Reschedule",
                "FollowUp",
                id.ToString(),
                $"FollowUpDate: {oldDateStr}",
                $"FollowUpDate: {newDate:yyyy-MM-dd HH:mm}",
                ipAddress,
                "Success");

            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> DeleteAsync(
            int id,
            ClaimsPrincipal user,
            string? ipAddress = null)
        {
            var followUp = await _context.FollowUps.FirstOrDefaultAsync(f => f.FollowUpId == id);
            if (followUp == null)
                return (false, "Follow-up not found.");

            var canAccess = await _userScopeService.CanAccessRecordAsync(user, followUp.AssignedToUserId);
            if (!canAccess)
                return (false, "You do not have permission to delete this follow-up.");

            var currentUser = await _userScopeService.GetCurrentUserAsync(user);

            _context.FollowUps.Remove(followUp);
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                currentUser?.Id,
                currentUser?.Email,
                "Delete",
                "FollowUp",
                id.ToString(),
                $"Deleted follow-up '{followUp.Subject}'",
                null,
                ipAddress,
                "Success");

            return (true, null);
        }

        public async Task<List<FollowUp>> GetUpcomingRemindersAsync(ClaimsPrincipal user, int days = 7)
        {
            var accessibleUserIds = await _userScopeService.GetAccessibleUserIdsAsync(user);
            var now = DateTime.UtcNow.Date;
            var horizon = now.AddDays(days).AddDays(1).AddTicks(-1);

            var query = _context.FollowUps
                .Include(f => f.Customer)
                .Include(f => f.Lead)
                .Include(f => f.AssignedTo)
                .Where(f => f.Status == FollowUpStatus.Planned && f.FollowUpDate >= now && f.FollowUpDate <= horizon);

            if (accessibleUserIds != null)
            {
                query = query.Where(f => accessibleUserIds.Contains(f.AssignedToUserId));
            }

            return await query.OrderBy(f => f.FollowUpDate).Take(10).ToListAsync();
        }

        public async Task<List<FollowUp>> GetOverdueFollowUpsAsync(ClaimsPrincipal user)
        {
            var accessibleUserIds = await _userScopeService.GetAccessibleUserIdsAsync(user);
            var today = DateTime.UtcNow.Date;

            var query = _context.FollowUps
                .Include(f => f.Customer)
                .Include(f => f.Lead)
                .Include(f => f.AssignedTo)
                .Where(f => f.Status == FollowUpStatus.Planned && f.FollowUpDate < today);

            if (accessibleUserIds != null)
            {
                query = query.Where(f => accessibleUserIds.Contains(f.AssignedToUserId));
            }

            return await query.OrderBy(f => f.FollowUpDate).Take(10).ToListAsync();
        }
    }
}

