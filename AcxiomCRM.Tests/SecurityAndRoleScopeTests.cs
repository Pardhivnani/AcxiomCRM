using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Services.Implementations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AcxiomCRM.Tests
{
    public class SecurityAndRoleScopeTests
    {
        private ApplicationDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task SalesExecutive_CannotAccessOtherExecutivesRecords()
        {
            var context = CreateInMemoryDbContext();

            // Setup team structure:
            // Manager: mgr-1
            // Sales Exec 1: sales-1 (Reports to mgr-1)
            // Sales Exec 2: sales-2 (Reports to mgr-1)
            var manager = new ApplicationUser { Id = "mgr-1", UserName = "mgr@test.com", FullName = "Manager" };
            var sales1 = new ApplicationUser { Id = "sales-1", UserName = "sales1@test.com", FullName = "Sales 1", ManagerId = "mgr-1" };
            var sales2 = new ApplicationUser { Id = "sales-2", UserName = "sales2@test.com", FullName = "Sales 2", ManagerId = "mgr-1" };

            context.Users.AddRange(manager, sales1, sales2);
            await context.SaveChangesAsync();

            // Mock UserManager
            var store = new Microsoft.AspNetCore.Identity.EntityFrameworkCore.UserStore<ApplicationUser>(context);
            var userManager = new UserManager<ApplicationUser>(
                store, null!, new PasswordHasher<ApplicationUser>(), null!, null!, null!, null!, null!, null!);

            var scopeService = new UserScopeService(userManager, context);

            // Sales 1 ClaimsPrincipal
            var sales1Principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "sales-1"),
                new Claim(ClaimTypes.Name, "sales1@test.com"),
                new Claim(ClaimTypes.Role, UserRoles.SalesExecutive)
            }, "TestAuth"));

            // Sales 1 trying to access sales 2's record
            var canAccessOther = await scopeService.CanAccessRecordAsync(sales1Principal, "sales-2");
            Assert.False(canAccessOther);

            // Sales 1 trying to access sales 1's record
            var canAccessOwn = await scopeService.CanAccessRecordAsync(sales1Principal, "sales-1");
            Assert.True(canAccessOwn);
        }

        [Fact]
        public async Task Manager_CanAccessDirectReportsRecords()
        {
            var context = CreateInMemoryDbContext();

            var manager = new ApplicationUser { Id = "mgr-1", UserName = "mgr@test.com", FullName = "Manager" };
            var sales1 = new ApplicationUser { Id = "sales-1", UserName = "sales1@test.com", FullName = "Sales 1", ManagerId = "mgr-1" };

            context.Users.AddRange(manager, sales1);
            await context.SaveChangesAsync();

            var store = new Microsoft.AspNetCore.Identity.EntityFrameworkCore.UserStore<ApplicationUser>(context);
            var userManager = new UserManager<ApplicationUser>(
                store, null!, new PasswordHasher<ApplicationUser>(), null!, null!, null!, null!, null!, null!);

            var scopeService = new UserScopeService(userManager, context);

            // Manager ClaimsPrincipal
            var mgrPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "mgr-1"),
                new Claim(ClaimTypes.Name, "mgr@test.com"),
                new Claim(ClaimTypes.Role, UserRoles.Manager)
            }, "TestAuth"));

            // Manager should be able to access Sales 1's record because Sales 1 reports to Manager
            var canAccessReport = await scopeService.CanAccessRecordAsync(mgrPrincipal, "sales-1");
            Assert.True(canAccessReport);
        }
    }
}

