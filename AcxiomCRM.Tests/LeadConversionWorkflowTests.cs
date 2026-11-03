using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Services.Implementations;
using AcxiomCRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AcxiomCRM.Tests
{
    public class LeadConversionWorkflowTests
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
            public List<AuditLog> Logs = new();
            public Task LogAsync(string? userId, string? userEmail, string action, string entityName, string? recordId, string? oldValue, string? newValue, string? ipAddress, string result = "Success")
            {
                Logs.Add(new AuditLog { Action = action, EntityName = entityName, RecordId = recordId, Result = result });
                return Task.CompletedTask;
            }
            public Task<ViewModels.Common.PagedResult<AuditLog>> GetAuditLogsAsync(string? userId, string? entityName, string? action, DateTime? fromDate, DateTime? toDate, int page = 1, int pageSize = 15) =>
                Task.FromResult(new ViewModels.Common.PagedResult<AuditLog>());
            public Task<AuditLog?> GetByIdAsync(int id) => Task.FromResult<AuditLog?>(null);
        }

        [Fact]
        public async Task ConvertLead_CreatesCustomerAndOpportunity_AndPreventsDuplicateConversion()
        {
            var context = CreateInMemoryDbContext();
            var userScope = new MockUserScopeService();
            var auditService = new MockAuditLogService();
            var customerService = new CustomerService(context, userScope, auditService);
            var leadService = new LeadService(context, userScope, auditService, customerService);

            // Seed lead
            var lead = new Lead
            {
                LeadCode = "LEAD-2001",
                LeadName = "Jane Prospect",
                Email = "jane@prospectco.com",
                Phone = "+1-555-9988",
                CompanyName = "Prospect Co LLC",
                Status = LeadStatus.Qualified,
                ExpectedValue = 75000m,
                Priority = LeadPriority.High
            };
            context.Leads.Add(lead);
            await context.SaveChangesAsync();

            var claimsUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "admin-1"),
                new Claim(ClaimTypes.Role, UserRoles.Admin)
            }, "TestAuth"));

            // Act 1: Convert Lead
            var (success, error, createdCustomer, createdOpportunity) = await leadService.ConvertLeadAsync(
                lead.LeadId,
                createOpportunity: true,
                opportunityName: "Prospect Co Deal",
                opportunityAmount: 75000m,
                expectedCloseDate: DateTime.UtcNow.AddDays(30),
                claimsUser);

            // Assert 1: Conversion Succeeded
            Assert.True(success);
            Assert.NotNull(createdCustomer);
            Assert.NotNull(createdOpportunity);
            Assert.Equal("Prospect Co LLC", createdCustomer.CustomerName);
            Assert.Equal(75000m, createdOpportunity.Amount);

            // Check database state
            var updatedLead = await context.Leads.FindAsync(lead.LeadId);
            Assert.NotNull(updatedLead);
            Assert.Equal(LeadStatus.Converted, updatedLead.Status);
            Assert.Equal(createdCustomer.CustomerId, updatedLead.CustomerId);

            // Assert 2: Duplicate conversion is blocked
            var (dupSuccess, dupError, _, _) = await leadService.ConvertLeadAsync(
                lead.LeadId,
                createOpportunity: false,
                opportunityName: null,
                opportunityAmount: null,
                expectedCloseDate: null,
                claimsUser);

            Assert.False(dupSuccess);
            Assert.Contains("already been converted", dupError);

            // Assert 3: Audit log was recorded
            Assert.Contains(auditService.Logs, l => l.Action == "Convert" && l.EntityName == "Lead");
        }
    }
}

