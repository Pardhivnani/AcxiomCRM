using AcxiomCRM.ViewModels.Dashboard;
using System.Security.Claims;

namespace AcxiomCRM.Services.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardViewModel> GetDashboardMetricsAsync(ClaimsPrincipal user);
    }
}

