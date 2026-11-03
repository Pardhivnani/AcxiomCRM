using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using Microsoft.AspNetCore.Identity;

namespace AcxiomCRM.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            // 1. Seed Roles
            foreach (var role in UserRoles.AllRoles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // 2. Seed Users
            // Admin
            var adminUser = await userManager.FindByEmailAsync("admin@acxiomcrm.local");
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = "admin@acxiomcrm.local",
                    Email = "admin@acxiomcrm.local",
                    FullName = "System Administrator",
                    Department = "IT Operations",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow.AddMonths(-6)
                };
                var result = await userManager.CreateAsync(adminUser, "Admin@12345!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, UserRoles.Admin);
                }
            }

            // Manager
            var managerUser = await userManager.FindByEmailAsync("manager@acxiomcrm.local");
            if (managerUser == null)
            {
                managerUser = new ApplicationUser
                {
                    UserName = "manager@acxiomcrm.local",
                    Email = "manager@acxiomcrm.local",
                    FullName = "Sarah Jenkins",
                    Department = "Sales Management",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow.AddMonths(-5)
                };
                var result = await userManager.CreateAsync(managerUser, "Manager@12345!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(managerUser, UserRoles.Manager);
                }
            }

            // Sales Executive 1
            var sales1User = await userManager.FindByEmailAsync("sales1@acxiomcrm.local");
            if (sales1User == null)
            {
                sales1User = new ApplicationUser
                {
                    UserName = "sales1@acxiomcrm.local",
                    Email = "sales1@acxiomcrm.local",
                    FullName = "Alex Rivera",
                    Department = "Direct Sales",
                    ManagerId = managerUser.Id,
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow.AddMonths(-4)
                };
                var result = await userManager.CreateAsync(sales1User, "Sales@12345!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(sales1User, UserRoles.SalesExecutive);
                }
            }

            // Sales Executive 2
            var sales2User = await userManager.FindByEmailAsync("sales2@acxiomcrm.local");
            if (sales2User == null)
            {
                sales2User = new ApplicationUser
                {
                    UserName = "sales2@acxiomcrm.local",
                    Email = "sales2@acxiomcrm.local",
                    FullName = "Emma Watson",
                    Department = "Enterprise Sales",
                    ManagerId = managerUser.Id,
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow.AddMonths(-3)
                };
                var result = await userManager.CreateAsync(sales2User, "Sales@12345!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(sales2User, UserRoles.SalesExecutive);
                }
            }

            // 3. Seed Customers
            if (!context.Customers.Any())
            {
                var customers = new List<Customer>
                {
                    new Customer
                    {
                        CustomerCode = "CUST-1001",
                        CustomerName = "Apex Global Technologies",
                        Email = "contact@apextech.com",
                        Phone = "+1-555-0101",
                        CompanyName = "Apex Technologies Inc.",
                        Address = "100 Innovation Way, Suite 400",
                        City = "San Jose",
                        State = "CA",
                        Status = CustomerStatus.Active,
                        CreatedDate = DateTime.UtcNow.AddMonths(-3),
                        CreatedByUserId = adminUser.Id,
                        AssignedToUserId = sales1User.Id
                    },
                    new Customer
                    {
                        CustomerCode = "CUST-1002",
                        CustomerName = "BlueHorizon Logistics",
                        Email = "info@bluehorizon.io",
                        Phone = "+1-555-0102",
                        CompanyName = "BlueHorizon Freight Co.",
                        Address = "450 Harbour Blvd",
                        City = "Seattle",
                        State = "WA",
                        Status = CustomerStatus.Active,
                        CreatedDate = DateTime.UtcNow.AddMonths(-2),
                        CreatedByUserId = managerUser.Id,
                        AssignedToUserId = sales1User.Id
                    },
                    new Customer
                    {
                        CustomerCode = "CUST-1003",
                        CustomerName = "Crestview Health Systems",
                        Email = "procurement@crestviewhealth.org",
                        Phone = "+1-555-0103",
                        CompanyName = "Crestview Medical Group",
                        Address = "820 Wellness Parkway",
                        City = "Dallas",
                        State = "TX",
                        Status = CustomerStatus.Active,
                        CreatedDate = DateTime.UtcNow.AddMonths(-2),
                        CreatedByUserId = adminUser.Id,
                        AssignedToUserId = sales2User.Id
                    },
                    new Customer
                    {
                        CustomerCode = "CUST-1004",
                        CustomerName = "Delta Manufacturing Corp",
                        Email = "sales@deltamfg.com",
                        Phone = "+1-555-0104",
                        CompanyName = "Delta Industrial Supply",
                        Address = "1200 Assembly Line Rd",
                        City = "Detroit",
                        State = "MI",
                        Status = CustomerStatus.Active,
                        CreatedDate = DateTime.UtcNow.AddMonths(-1),
                        CreatedByUserId = managerUser.Id,
                        AssignedToUserId = sales2User.Id
                    },
                    new Customer
                    {
                        CustomerCode = "CUST-1005",
                        CustomerName = "Epsilon Retail Network",
                        Email = "partnerships@epsilonstores.com",
                        Phone = "+1-555-0105",
                        CompanyName = "Epsilon Stores Group",
                        Address = "742 Retail Plaza",
                        City = "Chicago",
                        State = "IL",
                        Status = CustomerStatus.Inactive,
                        CreatedDate = DateTime.UtcNow.AddDays(-15),
                        CreatedByUserId = adminUser.Id,
                        AssignedToUserId = sales1User.Id
                    }
                };

                context.Customers.AddRange(customers);
                await context.SaveChangesAsync();
            }

            // 4. Seed Leads
            if (!context.Leads.Any())
            {
                var cust1 = context.Customers.First(c => c.CustomerCode == "CUST-1001");
                var leads = new List<Lead>
                {
                    new Lead
                    {
                        LeadCode = "LEAD-1001",
                        LeadName = "David Miller",
                        Email = "david.miller@vortexai.com",
                        Phone = "+1-555-0201",
                        CompanyName = "Vortex AI Solutions",
                        Source = "Website",
                        Status = LeadStatus.Qualified,
                        ExpectedValue = 45000m,
                        Priority = LeadPriority.High,
                        Notes = "Interested in CRM enterprise licensing with 100+ seats.",
                        CreatedDate = DateTime.UtcNow.AddDays(-20),
                        AssignedToUserId = sales1User.Id
                    },
                    new Lead
                    {
                        LeadCode = "LEAD-1002",
                        LeadName = "Sophia Martinez",
                        Email = "smartinez@nexuseco.com",
                        Phone = "+1-555-0202",
                        CompanyName = "Nexus Clean Energy",
                        Source = "Referral",
                        Status = LeadStatus.Contacted,
                        ExpectedValue = 28000m,
                        Priority = LeadPriority.Medium,
                        Notes = "Initial demo completed. Follow-up meeting scheduled next week.",
                        CreatedDate = DateTime.UtcNow.AddDays(-14),
                        AssignedToUserId = sales1User.Id
                    },
                    new Lead
                    {
                        LeadCode = "LEAD-1003",
                        LeadName = "Robert Chen",
                        Email = "rchen@quantumbio.com",
                        Phone = "+1-555-0203",
                        CompanyName = "Quantum Biotech Labs",
                        Source = "Cold Call",
                        Status = LeadStatus.New,
                        ExpectedValue = 60000m,
                        Priority = LeadPriority.High,
                        Notes = "Inbound query through partner network.",
                        CreatedDate = DateTime.UtcNow.AddDays(-5),
                        AssignedToUserId = sales2User.Id
                    },
                    new Lead
                    {
                        LeadCode = "LEAD-1004",
                        LeadName = "Jessica Taylor",
                        Email = "jtaylor@beaconfinance.com",
                        Phone = "+1-555-0204",
                        CompanyName = "Beacon Capital Advisory",
                        Source = "Advertisement",
                        Status = LeadStatus.Converted,
                        ExpectedValue = 85000m,
                        Priority = LeadPriority.High,
                        Notes = "Successfully converted to Apex Global account project.",
                        CreatedDate = DateTime.UtcNow.AddDays(-30),
                        AssignedToUserId = sales1User.Id,
                        CustomerId = cust1.CustomerId
                    },
                    new Lead
                    {
                        LeadCode = "LEAD-1005",
                        LeadName = "Marcus Brody",
                        Email = "mbrody@zenithlog.com",
                        Phone = "+1-555-0205",
                        CompanyName = "Zenith Freight Solutions",
                        Source = "Website",
                        Status = LeadStatus.Lost,
                        ExpectedValue = 15000m,
                        Priority = LeadPriority.Low,
                        Notes = "Chose an in-house bespoke tool.",
                        CreatedDate = DateTime.UtcNow.AddDays(-25),
                        AssignedToUserId = sales2User.Id
                    }
                };

                context.Leads.AddRange(leads);
                await context.SaveChangesAsync();
            }

            // 5. Seed Opportunities
            if (!context.Opportunities.Any())
            {
                var cust1 = context.Customers.First(c => c.CustomerCode == "CUST-1001");
                var cust2 = context.Customers.First(c => c.CustomerCode == "CUST-1002");
                var cust3 = context.Customers.First(c => c.CustomerCode == "CUST-1003");
                var cust4 = context.Customers.First(c => c.CustomerCode == "CUST-1004");

                var opportunities = new List<Opportunity>
                {
                    new Opportunity
                    {
                        OpportunityName = "Apex Enterprise Cloud Migration",
                        CustomerId = cust1.CustomerId,
                        Amount = 120000m,
                        Stage = OpportunityStage.Negotiation,
                        Probability = 80,
                        ExpectedCloseDate = DateTime.UtcNow.AddDays(25),
                        Status = OpportunityStatus.Open,
                        CreatedDate = DateTime.UtcNow.AddDays(-35),
                        AssignedToUserId = sales1User.Id,
                        Notes = "Final contract review with legal department."
                    },
                    new Opportunity
                    {
                        OpportunityName = "BlueHorizon Fleet Tracking Integration",
                        CustomerId = cust2.CustomerId,
                        Amount = 65000m,
                        Stage = OpportunityStage.Proposal,
                        Probability = 60,
                        ExpectedCloseDate = DateTime.UtcNow.AddDays(40),
                        Status = OpportunityStatus.Open,
                        CreatedDate = DateTime.UtcNow.AddDays(-20),
                        AssignedToUserId = sales1User.Id,
                        Notes = "Proposal submitted, awaiting executive signoff."
                    },
                    new Opportunity
                    {
                        OpportunityName = "Crestview HIPAA Compliant CRM Portal",
                        CustomerId = cust3.CustomerId,
                        Amount = 95000m,
                        Stage = OpportunityStage.Qualification,
                        Probability = 30,
                        ExpectedCloseDate = DateTime.UtcNow.AddDays(60),
                        Status = OpportunityStatus.Open,
                        CreatedDate = DateTime.UtcNow.AddDays(-10),
                        AssignedToUserId = sales2User.Id,
                        Notes = "Technical architecture qualification in progress."
                    },
                    new Opportunity
                    {
                        OpportunityName = "Delta Manufacturing IoT Module",
                        CustomerId = cust4.CustomerId,
                        Amount = 150000m,
                        Stage = OpportunityStage.Won,
                        Probability = 100,
                        ExpectedCloseDate = DateTime.UtcNow.AddDays(-10),
                        Status = OpportunityStatus.Won,
                        CreatedDate = DateTime.UtcNow.AddDays(-60),
                        AssignedToUserId = sales2User.Id,
                        Notes = "Deal closed and contract signed successfully!"
                    },
                    new Opportunity
                    {
                        OpportunityName = "Apex Analytics Add-on Pack",
                        CustomerId = cust1.CustomerId,
                        Amount = 30000m,
                        Stage = OpportunityStage.Won,
                        Probability = 100,
                        ExpectedCloseDate = DateTime.UtcNow.AddDays(-2),
                        Status = OpportunityStatus.Won,
                        CreatedDate = DateTime.UtcNow.AddDays(-25),
                        AssignedToUserId = sales1User.Id,
                        Notes = "Closed won deal for reporting add-on."
                    },
                    new Opportunity
                    {
                        OpportunityName = "BlueHorizon Regional Expansion",
                        CustomerId = cust2.CustomerId,
                        Amount = 40000m,
                        Stage = OpportunityStage.Lost,
                        Probability = 0,
                        ExpectedCloseDate = DateTime.UtcNow.AddDays(-15),
                        Status = OpportunityStatus.Lost,
                        CreatedDate = DateTime.UtcNow.AddDays(-45),
                        AssignedToUserId = sales1User.Id,
                        Notes = "Customer deferred regional expansion project to next fiscal year."
                    }
                };

                context.Opportunities.AddRange(opportunities);
                await context.SaveChangesAsync();
            }

            // 6. Seed FollowUps
            if (!context.FollowUps.Any())
            {
                var cust1 = context.Customers.First(c => c.CustomerCode == "CUST-1001");
                var cust2 = context.Customers.First(c => c.CustomerCode == "CUST-1002");
                var lead1 = context.Leads.First(l => l.LeadCode == "LEAD-1001");

                var followUps = new List<FollowUp>
                {
                    new FollowUp
                    {
                        CustomerId = cust1.CustomerId,
                        FollowUpDate = DateTime.UtcNow.AddDays(2),
                        FollowUpType = FollowUpType.Call,
                        Subject = "Contract review discussion with CIO",
                        Remarks = "Discuss SLA clauses and payment milestones.",
                        Status = FollowUpStatus.Planned,
                        AssignedToUserId = sales1User.Id,
                        CreatedDate = DateTime.UtcNow.AddDays(-2)
                    },
                    new FollowUp
                    {
                        LeadId = lead1.LeadId,
                        FollowUpDate = DateTime.UtcNow.AddDays(4),
                        FollowUpType = FollowUpType.Demo,
                        Subject = "Product live walkthrough for Vortex team",
                        Remarks = "Prepare custom demo on multi-tenant pipeline.",
                        Status = FollowUpStatus.Planned,
                        AssignedToUserId = sales1User.Id,
                        CreatedDate = DateTime.UtcNow.AddDays(-1)
                    },
                    new FollowUp
                    {
                        CustomerId = cust2.CustomerId,
                        FollowUpDate = DateTime.UtcNow.AddDays(-3),
                        FollowUpType = FollowUpType.Meeting,
                        Subject = "Proposal walkthrough meeting",
                        Remarks = "Meeting conducted successfully. Sent updated budget sheet.",
                        Status = FollowUpStatus.Completed,
                        CompletedDate = DateTime.UtcNow.AddDays(-3),
                        AssignedToUserId = sales1User.Id,
                        CreatedDate = DateTime.UtcNow.AddDays(-7)
                    },
                    new FollowUp
                    {
                        CustomerId = cust1.CustomerId,
                        FollowUpDate = DateTime.UtcNow.AddDays(-1),
                        FollowUpType = FollowUpType.Call,
                        Subject = "Check payment terms feedback",
                        Remarks = "Postponed due to client vacation.",
                        Status = FollowUpStatus.Missed,
                        AssignedToUserId = sales2User.Id,
                        CreatedDate = DateTime.UtcNow.AddDays(-4)
                    }
                };

                context.FollowUps.AddRange(followUps);
                await context.SaveChangesAsync();
            }

            // 7. Seed Activities
            if (!context.Activities.Any())
            {
                var cust1 = context.Customers.First(c => c.CustomerCode == "CUST-1001");
                var lead1 = context.Leads.First(l => l.LeadCode == "LEAD-1001");

                var activities = new List<Activity>
                {
                    new Activity
                    {
                        ActivityType = ActivityType.Email,
                        Subject = "Sent enterprise proposal deck",
                        Description = "Emailed detailed proposal including SLA specifications and implementation timeline.",
                        ActivityDate = DateTime.UtcNow.AddDays(-5),
                        CustomerId = cust1.CustomerId,
                        AssignedToUserId = sales1User.Id,
                        Status = ActivityStatus.Completed,
                        CreatedDate = DateTime.UtcNow.AddDays(-5)
                    },
                    new Activity
                    {
                        ActivityType = ActivityType.Call,
                        Subject = "Introductory discovery call",
                        Description = "Discussed requirements with engineering lead and CRM evaluation committee.",
                        ActivityDate = DateTime.UtcNow.AddDays(-10),
                        LeadId = lead1.LeadId,
                        AssignedToUserId = sales1User.Id,
                        Status = ActivityStatus.Completed,
                        CreatedDate = DateTime.UtcNow.AddDays(-10)
                    },
                    new Activity
                    {
                        ActivityType = ActivityType.Task,
                        Subject = "Prepare compliance checklist for Crestview Health",
                        Description = "Assemble HIPAA compliance audit documentation.",
                        ActivityDate = DateTime.UtcNow.AddDays(1),
                        AssignedToUserId = sales2User.Id,
                        Status = ActivityStatus.InProgress,
                        CreatedDate = DateTime.UtcNow.AddDays(-1)
                    }
                };

                context.Activities.AddRange(activities);
                await context.SaveChangesAsync();
            }

            // 8. Seed Audit Logs
            if (!context.AuditLogs.Any())
            {
                var auditLogs = new List<AuditLog>
                {
                    new AuditLog
                    {
                        UserId = adminUser.Id,
                        UserEmail = adminUser.Email,
                        Action = "Login",
                        EntityName = "Auth",
                        RecordId = adminUser.Id,
                        OldValue = null,
                        NewValue = "User authenticated successfully via credentials.",
                        CreatedDate = DateTime.UtcNow.AddDays(-10),
                        IpAddress = "127.0.0.1",
                        Result = "Success"
                    },
                    new AuditLog
                    {
                        UserId = adminUser.Id,
                        UserEmail = adminUser.Email,
                        Action = "Create",
                        EntityName = "Customer",
                        RecordId = "1",
                        OldValue = null,
                        NewValue = "Created Customer 'Apex Global Technologies' (CUST-1001).",
                        CreatedDate = DateTime.UtcNow.AddMonths(-3),
                        IpAddress = "127.0.0.1",
                        Result = "Success"
                    },
                    new AuditLog
                    {
                        UserId = sales1User.Id,
                        UserEmail = sales1User.Email,
                        Action = "Convert",
                        EntityName = "Lead",
                        RecordId = "4",
                        OldValue = "Lead status: Qualified",
                        NewValue = "Converted lead 'Jessica Taylor' to customer and linked account.",
                        CreatedDate = DateTime.UtcNow.AddDays(-30),
                        IpAddress = "127.0.0.1",
                        Result = "Success"
                    },
                    new AuditLog
                    {
                        UserId = sales2User.Id,
                        UserEmail = sales2User.Email,
                        Action = "Update",
                        EntityName = "Opportunity",
                        RecordId = "4",
                        OldValue = "Stage: Negotiation, Probability: 80%",
                        NewValue = "Stage: Won, Probability: 100%, Amount: $150,000",
                        CreatedDate = DateTime.UtcNow.AddDays(-10),
                        IpAddress = "127.0.0.1",
                        Result = "Success"
                    }
                };

                context.AuditLogs.AddRange(auditLogs);
                await context.SaveChangesAsync();
            }
        }
    }
}

