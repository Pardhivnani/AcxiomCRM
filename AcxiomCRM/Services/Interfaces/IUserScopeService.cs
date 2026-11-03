using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AcxiomCRM.Services.Interfaces
{
    public interface IUserScopeService
    {
        Task<List<string>?> GetAccessibleUserIdsAsync(ClaimsPrincipal user);
        Task<bool> CanAccessRecordAsync(ClaimsPrincipal user, string? recordOwnerUserId);
        Task<ApplicationUser?> GetCurrentUserAsync(ClaimsPrincipal user);
    }
}

