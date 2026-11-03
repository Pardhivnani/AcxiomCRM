using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Services.Implementations;
using AcxiomCRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AcxiomCRM.Tests
{
    public class CustomerUniquenessTests
    {
        private ApplicationDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        private class MockUserScopeService : IUserScopeService
        {
            public Task<List<string>?> GetAccessibleUserIdsAsync(ClaimsPrincipal user) => Task.FromResult<List<string>?>(null);
            public Task<bool> CanAccessRecordAsync(ClaimsPrincipal user, string? recordOwnerUserId) => Task.FromResult(true);
            public Task<ApplicationUser?> GetCurrentUserAsync(ClaimsPrincipal user) =>
                Task.FromResult<ApplicationUser?>(new ApplicationUser { Id = "admin-1", Email = "admin@test.com", FullName = "Admin User" });
        }

        private class MockAuditLogService : IAuditLogService
        {
            public Task LogAsync(string? userId, string? userEmail, string action, string entityName, string? recordId, string? oldValue, string? newValue, string? ipAddress, string result = "Success") => Task.CompletedTask;
            public Task<ViewModels.Common.PagedResult<AuditLog>> GetAuditLogsAsync(string? userId, string? entityName, string? action, DateTime? fromDate, DateTime? toDate, int page = 1, int pageSize = 15) =>
                Task.FromResult(new ViewModels.Common.PagedResult<AuditLog>());
            public Task<AuditLog?> GetByIdAsync(int id) => Task.FromResult<AuditLog?>(null);
        }

        [Fact]
        public async Task Customer_DuplicateEmail_IsPrevented()
        {
            var context = CreateInMemoryDbContext();
            var userScope = new MockUserScopeService();
            var auditService = new MockAuditLogService();
            var customerService = new CustomerService(context, userScope, auditService);

            var claimsUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "admin-1"),
                new Claim(ClaimTypes.Role, UserRoles.Admin)
            }, "TestAuth"));

            // 1. Create original customer
            var customer1 = new Customer
            {
                CustomerCode = "CUST-1001",
                CustomerName = "First Company",
                Email = "duplicate@check.com",
                Phone = "+1-555-1111",
                Status = CustomerStatus.Active
            };
            var (success1, _, _) = await customerService.CreateAsync(customer1, claimsUser);
            Assert.True(success1);

            // 2. Attempt creating duplicate with same email
            var customer2 = new Customer
            {
                CustomerCode = "CUST-1002",
                CustomerName = "Second Company",
                Email = "DUPLICATE@check.com", // Case insensitive check
                Phone = "+1-555-2222",
                Status = CustomerStatus.Active
            };
            var (success2, errorMessage2, _) = await customerService.CreateAsync(customer2, claimsUser);

            Assert.False(success2);
            Assert.Contains("already exists", errorMessage2);
        }

        [Fact]
        public async Task Customer_DuplicatePhone_IsPrevented()
        {
            var context = CreateInMemoryDbContext();
            var userScope = new MockUserScopeService();
            var auditService = new MockAuditLogService();
            var customerService = new CustomerService(context, userScope, auditService);

            var claimsUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "admin-1"),
                new Claim(ClaimTypes.Role, UserRoles.Admin)
            }, "TestAuth"));

            // 1. Create original customer
            var customer1 = new Customer
            {
                CustomerCode = "CUST-1001",
                CustomerName = "Phone Org 1",
                Email = "unique1@org.com",
                Phone = "+1-555-9999",
                Status = CustomerStatus.Active
            };
            var (success1, _, _) = await customerService.CreateAsync(customer1, claimsUser);
            Assert.True(success1);

            // 2. Attempt creating duplicate with same phone
            var customer2 = new Customer
            {
                CustomerCode = "CUST-1002",
                CustomerName = "Phone Org 2",
                Email = "unique2@org.com",
                Phone = "+1-555-9999",
                Status = CustomerStatus.Active
            };
            var (success2, errorMessage2, _) = await customerService.CreateAsync(customer2, claimsUser);

            Assert.False(success2);
            Assert.Contains("already exists", errorMessage2);
        }
    }
}

