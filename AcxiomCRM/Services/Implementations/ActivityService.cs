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
    public class ActivityService : IActivityService
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserScopeService _userScopeService;
        private readonly IAuditLogService _auditLogService;

        public ActivityService(
            ApplicationDbContext context,
            IUserScopeService userScopeService,
            IAuditLogService auditLogService)
        {
            _context = context;
            _userScopeService = userScopeService;
            _auditLogService = auditLogService;
        }

        public async Task<PagedResult<Activity>> GetActivitiesAsync(
            ClaimsPrincipal user,
            string? searchTerm,
            ActivityType? type,
            ActivityStatus? status,
            string? assignedToUserId,
            DateTime? startDate,
            DateTime? endDate,
            int page = 1,
            int pageSize = 10,
            string? sortBy = "ActivityDate",
            bool isAscending = false)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var accessibleUserIds = await _userScopeService.GetAccessibleUserIdsAsync(user);

            var query = _context.Activities
                .Include(a => a.Customer)
                .Include(a => a.Lead)
                .Include(a => a.Opportunity)
                .Include(a => a.AssignedTo)
                .AsNoTracking()
                .AsQueryable();

            if (accessibleUserIds != null)
            {
                query = query.Where(a => accessibleUserIds.Contains(a.AssignedToUserId));
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(a =>
                    a.Subject.ToLower().Contains(term) ||
                    (a.Description != null && a.Description.ToLower().Contains(term)) ||
                    (a.Customer != null && a.Customer.CustomerName.ToLower().Contains(term)) ||
                    (a.Lead != null && a.Lead.LeadName.ToLower().Contains(term)));
            }

            if (type.HasValue)
            {
                query = query.Where(a => a.ActivityType == type.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(a => a.Status == status.Value);
            }

            if (!string.IsNullOrWhiteSpace(assignedToUserId))
            {
                query = query.Where(a => a.AssignedToUserId == assignedToUserId);
            }

            if (startDate.HasValue)
            {
                var start = startDate.Value.Date;
                query = query.Where(a => a.ActivityDate >= start);
            }

            if (endDate.HasValue)
            {
                var end = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(a => a.ActivityDate <= end);
            }

            query = sortBy switch
            {
                "Subject" => isAscending ? query.OrderBy(a => a.Subject) : query.OrderByDescending(a => a.Subject),
                "Type" => isAscending ? query.OrderBy(a => a.ActivityType) : query.OrderByDescending(a => a.ActivityType),
                "Status" => isAscending ? query.OrderBy(a => a.Status) : query.OrderByDescending(a => a.Status),
                _ => isAscending ? query.OrderBy(a => a.ActivityDate) : query.OrderByDescending(a => a.ActivityDate)
            };

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Activity>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<Activity?> GetByIdAsync(int id, ClaimsPrincipal? user = null)
        {
            var activity = await _context.Activities
                .Include(a => a.Customer)
                .Include(a => a.Lead)
                .Include(a => a.Opportunity)
                .Include(a => a.AssignedTo)
                .FirstOrDefaultAsync(a => a.ActivityId == id);

            if (activity == null)
                return null;

            if (user != null)
            {
                var canAccess = await _userScopeService.CanAccessRecordAsync(user, activity.AssignedToUserId);
                if (!canAccess)
                    return null;
            }

            return activity;
        }

        public async Task<(bool Success, string? ErrorMessage, Activity? Activity)> CreateAsync(
            Activity activity,
            ClaimsPrincipal user,
            string? ipAddress = null)
        {
            if (string.IsNullOrWhiteSpace(activity.Subject))
                return (false, "Subject is required.", null);

            var currentUser = await _userScopeService.GetCurrentUserAsync(user);
            if (string.IsNullOrEmpty(activity.AssignedToUserId))
            {
                activity.AssignedToUserId = currentUser?.Id ?? string.Empty;
            }

            activity.CreatedDate = DateTime.UtcNow;

            _context.Activities.Add(activity);
            await _context.SaveChangesAsync();

            var logValue = JsonSerializer.Serialize(new
            {
                activity.ActivityId,
                activity.Subject,
                activity.ActivityType,
                activity.ActivityDate,
                activity.Status,
                activity.AssignedToUserId,
                activity.CustomerId,
                activity.LeadId,
                activity.OpportunityId
            });

            await _auditLogService.LogAsync(
                currentUser?.Id,
                currentUser?.Email,
                "Create",
                "Activity",
                activity.ActivityId.ToString(),
                null,
                logValue,
                ipAddress,
                "Success");

            return (true, null, activity);
        }

        public async Task<(bool Success, string? ErrorMessage)> UpdateAsync(
            Activity activity,
            ClaimsPrincipal user,
            string? ipAddress = null)
        {
            var existing = await _context.Activities.FirstOrDefaultAsync(a => a.ActivityId == activity.ActivityId);
            if (existing == null)
                return (false, "Activity not found.");

            var canAccess = await _userScopeService.CanAccessRecordAsync(user, existing.AssignedToUserId);
            if (!canAccess)
                return (false, "You do not have permission to modify this activity.");

            var oldValue = JsonSerializer.Serialize(new
            {
                existing.Subject,
                existing.ActivityType,
                existing.ActivityDate,
                existing.Description,
                existing.Status,
                existing.AssignedToUserId
            });

            existing.Subject = activity.Subject;
            existing.ActivityType = activity.ActivityType;
            existing.ActivityDate = activity.ActivityDate;
            existing.Description = activity.Description;
            existing.Status = activity.Status;
            existing.AssignedToUserId = activity.AssignedToUserId;
            existing.CustomerId = activity.CustomerId;
            existing.LeadId = activity.LeadId;
            existing.OpportunityId = activity.OpportunityId;

            await _context.SaveChangesAsync();

            var currentUser = await _userScopeService.GetCurrentUserAsync(user);
            var newValue = JsonSerializer.Serialize(new
            {
                existing.Subject,
                existing.ActivityType,
                existing.ActivityDate,
                existing.Description,
                existing.Status,
                existing.AssignedToUserId
            });

            await _auditLogService.LogAsync(
                currentUser?.Id,
                currentUser?.Email,
                "Update",
                "Activity",
                existing.ActivityId.ToString(),
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
            var activity = await _context.Activities.FirstOrDefaultAsync(a => a.ActivityId == id);
            if (activity == null)
                return (false, "Activity not found.");

            var canAccess = await _userScopeService.CanAccessRecordAsync(user, activity.AssignedToUserId);
            if (!canAccess)
                return (false, "You do not have permission to delete this activity.");

            var currentUser = await _userScopeService.GetCurrentUserAsync(user);

            _context.Activities.Remove(activity);
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                currentUser?.Id,
                currentUser?.Email,
                "Delete",
                "Activity",
                id.ToString(),
                $"Deleted activity '{activity.Subject}'",
                null,
                ipAddress,
                "Success");

            return (true, null);
        }

        public async Task<List<Activity>> GetRecentActivitiesAsync(ClaimsPrincipal user, int count = 5)
        {
            var accessibleUserIds = await _userScopeService.GetAccessibleUserIdsAsync(user);

            var query = _context.Activities
                .Include(a => a.Customer)
                .Include(a => a.Lead)
                .Include(a => a.AssignedTo)
                .AsNoTracking()
                .AsQueryable();

            if (accessibleUserIds != null)
            {
                query = query.Where(a => accessibleUserIds.Contains(a.AssignedToUserId));
            }

            return await query.OrderByDescending(a => a.ActivityDate).Take(count).ToListAsync();
        }
    }
}

