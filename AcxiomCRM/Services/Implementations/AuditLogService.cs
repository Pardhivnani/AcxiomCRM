using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Services.Interfaces;
using AcxiomCRM.ViewModels.Common;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services.Implementations
{
    public class AuditLogService : IAuditLogService
    {
        private readonly ApplicationDbContext _context;

        public AuditLogService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(
            string? userId,
            string? userEmail,
            string action,
            string entityName,
            string? recordId,
            string? oldValue,
            string? newValue,
            string? ipAddress,
            string result = "Success")
        {
            try
            {
                var log = new AuditLog
                {
                    UserId = userId,
                    UserEmail = userEmail,
                    Action = action,
                    EntityName = entityName,
                    RecordId = recordId,
                    OldValue = oldValue,
                    NewValue = newValue,
                    CreatedDate = DateTime.UtcNow,
                    IpAddress = ipAddress ?? "127.0.0.1",
                    Result = result
                };

                _context.AuditLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch
            {
                // Never let audit logging failure crash application operations
            }
        }

        public async Task<PagedResult<AuditLog>> GetAuditLogsAsync(
            string? userId,
            string? entityName,
            string? action,
            DateTime? fromDate,
            DateTime? toDate,
            int page = 1,
            int pageSize = 15)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 15;

            var query = _context.AuditLogs.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(userId))
            {
                query = query.Where(a => a.UserId == userId || (a.UserEmail != null && a.UserEmail.Contains(userId)));
            }

            if (!string.IsNullOrWhiteSpace(entityName))
            {
                query = query.Where(a => a.EntityName == entityName);
            }

            if (!string.IsNullOrWhiteSpace(action))
            {
                query = query.Where(a => a.Action == action);
            }

            if (fromDate.HasValue)
            {
                var start = fromDate.Value.Date;
                query = query.Where(a => a.CreatedDate >= start);
            }

            if (toDate.HasValue)
            {
                var end = toDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(a => a.CreatedDate <= end);
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(a => a.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<AuditLog>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<AuditLog?> GetByIdAsync(int id)
        {
            return await _context.AuditLogs.AsNoTracking().FirstOrDefaultAsync(a => a.AuditLogId == id);
        }
    }
}

