using AcxiomCRM.Models.Entities;
using AcxiomCRM.ViewModels.Common;

namespace AcxiomCRM.Services.Interfaces
{
    public interface IAuditLogService
    {
        Task LogAsync(string? userId, string? userEmail, string action, string entityName, string? recordId, string? oldValue, string? newValue, string? ipAddress, string result = "Success");
        Task<PagedResult<AuditLog>> GetAuditLogsAsync(string? userId, string? entityName, string? action, DateTime? fromDate, DateTime? toDate, int page = 1, int pageSize = 15);
        Task<AuditLog?> GetByIdAsync(int id);
    }
}

