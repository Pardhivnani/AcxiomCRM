CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
    "ProductVersion" TEXT NOT NULL
);

BEGIN TRANSACTION;

CREATE TABLE "AspNetRoles" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_AspNetRoles" PRIMARY KEY,
    "Name" TEXT NULL,
    "NormalizedName" TEXT NULL,
    "ConcurrencyStamp" TEXT NULL
);

CREATE TABLE "AspNetUsers" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_AspNetUsers" PRIMARY KEY,
    "FullName" TEXT NOT NULL,
    "IsActive" INTEGER NOT NULL,
    "CreatedDate" TEXT NOT NULL,
    "Department" TEXT NULL,
    "ManagerId" TEXT NULL,
    "UserName" TEXT NULL,
    "NormalizedUserName" TEXT NULL,
    "Email" TEXT NULL,
    "NormalizedEmail" TEXT NULL,
    "EmailConfirmed" INTEGER NOT NULL,
    "PasswordHash" TEXT NULL,
    "SecurityStamp" TEXT NULL,
    "ConcurrencyStamp" TEXT NULL,
    "PhoneNumber" TEXT NULL,
    "PhoneNumberConfirmed" INTEGER NOT NULL,
    "TwoFactorEnabled" INTEGER NOT NULL,
    "LockoutEnd" TEXT NULL,
    "LockoutEnabled" INTEGER NOT NULL,
    "AccessFailedCount" INTEGER NOT NULL,
    CONSTRAINT "FK_AspNetUsers_AspNetUsers_ManagerId" FOREIGN KEY ("ManagerId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "AuditLogs" (
    "AuditLogId" INTEGER NOT NULL CONSTRAINT "PK_AuditLogs" PRIMARY KEY AUTOINCREMENT,
    "UserId" TEXT NULL,
    "UserEmail" TEXT NULL,
    "Action" TEXT NOT NULL,
    "EntityName" TEXT NOT NULL,
    "RecordId" TEXT NULL,
    "OldValue" TEXT NULL,
    "NewValue" TEXT NULL,
    "CreatedDate" TEXT NOT NULL,
    "IpAddress" TEXT NULL,
    "Result" TEXT NOT NULL
);

CREATE TABLE "AspNetRoleClaims" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_AspNetRoleClaims" PRIMARY KEY AUTOINCREMENT,
    "RoleId" TEXT NOT NULL,
    "ClaimType" TEXT NULL,
    "ClaimValue" TEXT NULL,
    CONSTRAINT "FK_AspNetRoleClaims_AspNetRoles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles" ("Id") ON DELETE CASCADE
);

CREATE TABLE "AspNetUserClaims" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_AspNetUserClaims" PRIMARY KEY AUTOINCREMENT,
    "UserId" TEXT NOT NULL,
    "ClaimType" TEXT NULL,
    "ClaimValue" TEXT NULL,
    CONSTRAINT "FK_AspNetUserClaims_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

CREATE TABLE "AspNetUserLogins" (
    "LoginProvider" TEXT NOT NULL,
    "ProviderKey" TEXT NOT NULL,
    "ProviderDisplayName" TEXT NULL,
    "UserId" TEXT NOT NULL,
    CONSTRAINT "PK_AspNetUserLogins" PRIMARY KEY ("LoginProvider", "ProviderKey"),
    CONSTRAINT "FK_AspNetUserLogins_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

CREATE TABLE "AspNetUserRoles" (
    "UserId" TEXT NOT NULL,
    "RoleId" TEXT NOT NULL,
    CONSTRAINT "PK_AspNetUserRoles" PRIMARY KEY ("UserId", "RoleId"),
    CONSTRAINT "FK_AspNetUserRoles_AspNetRoles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_AspNetUserRoles_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

CREATE TABLE "AspNetUserTokens" (
    "UserId" TEXT NOT NULL,
    "LoginProvider" TEXT NOT NULL,
    "Name" TEXT NOT NULL,
    "Value" TEXT NULL,
    CONSTRAINT "PK_AspNetUserTokens" PRIMARY KEY ("UserId", "LoginProvider", "Name"),
    CONSTRAINT "FK_AspNetUserTokens_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Customers" (
    "CustomerId" INTEGER NOT NULL CONSTRAINT "PK_Customers" PRIMARY KEY AUTOINCREMENT,
    "CustomerCode" TEXT NOT NULL,
    "CustomerName" TEXT NOT NULL,
    "Email" TEXT NOT NULL,
    "Phone" TEXT NOT NULL,
    "CompanyName" TEXT NULL,
    "Address" TEXT NULL,
    "City" TEXT NULL,
    "State" TEXT NULL,
    "Status" INTEGER NOT NULL,
    "CreatedDate" TEXT NOT NULL,
    "ModifiedDate" TEXT NULL,
    "CreatedByUserId" TEXT NULL,
    "AssignedToUserId" TEXT NULL,
    CONSTRAINT "FK_Customers_AspNetUsers_AssignedToUserId" FOREIGN KEY ("AssignedToUserId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Customers_AspNetUsers_CreatedByUserId" FOREIGN KEY ("CreatedByUserId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "Leads" (
    "LeadId" INTEGER NOT NULL CONSTRAINT "PK_Leads" PRIMARY KEY AUTOINCREMENT,
    "LeadCode" TEXT NOT NULL,
    "LeadName" TEXT NOT NULL,
    "Email" TEXT NOT NULL,
    "Phone" TEXT NOT NULL,
    "CompanyName" TEXT NULL,
    "Source" TEXT NOT NULL,
    "Status" INTEGER NOT NULL,
    "ExpectedValue" decimal(18,2) NOT NULL,
    "Priority" INTEGER NOT NULL,
    "Notes" TEXT NULL,
    "CreatedDate" TEXT NOT NULL,
    "ModifiedDate" TEXT NULL,
    "AssignedToUserId" TEXT NULL,
    "CustomerId" INTEGER NULL,
    CONSTRAINT "FK_Leads_AspNetUsers_AssignedToUserId" FOREIGN KEY ("AssignedToUserId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Leads_Customers_CustomerId" FOREIGN KEY ("CustomerId") REFERENCES "Customers" ("CustomerId") ON DELETE SET NULL
);

CREATE TABLE "Opportunities" (
    "OpportunityId" INTEGER NOT NULL CONSTRAINT "PK_Opportunities" PRIMARY KEY AUTOINCREMENT,
    "OpportunityName" TEXT NOT NULL,
    "CustomerId" INTEGER NOT NULL,
    "LeadId" INTEGER NULL,
    "Amount" decimal(18,2) NOT NULL,
    "Stage" INTEGER NOT NULL,
    "Probability" INTEGER NOT NULL,
    "ExpectedCloseDate" TEXT NOT NULL,
    "Status" INTEGER NOT NULL,
    "CreatedDate" TEXT NOT NULL,
    "ModifiedDate" TEXT NULL,
    "AssignedToUserId" TEXT NOT NULL,
    "Notes" TEXT NULL,
    CONSTRAINT "FK_Opportunities_AspNetUsers_AssignedToUserId" FOREIGN KEY ("AssignedToUserId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Opportunities_Customers_CustomerId" FOREIGN KEY ("CustomerId") REFERENCES "Customers" ("CustomerId") ON DELETE RESTRICT,
    CONSTRAINT "FK_Opportunities_Leads_LeadId" FOREIGN KEY ("LeadId") REFERENCES "Leads" ("LeadId") ON DELETE SET NULL
);

CREATE TABLE "Activities" (
    "ActivityId" INTEGER NOT NULL CONSTRAINT "PK_Activities" PRIMARY KEY AUTOINCREMENT,
    "ActivityType" INTEGER NOT NULL,
    "Subject" TEXT NOT NULL,
    "Description" TEXT NULL,
    "ActivityDate" TEXT NOT NULL,
    "CustomerId" INTEGER NULL,
    "LeadId" INTEGER NULL,
    "OpportunityId" INTEGER NULL,
    "AssignedToUserId" TEXT NOT NULL,
    "Status" INTEGER NOT NULL,
    "CreatedDate" TEXT NOT NULL,
    CONSTRAINT "FK_Activities_AspNetUsers_AssignedToUserId" FOREIGN KEY ("AssignedToUserId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Activities_Customers_CustomerId" FOREIGN KEY ("CustomerId") REFERENCES "Customers" ("CustomerId") ON DELETE SET NULL,
    CONSTRAINT "FK_Activities_Leads_LeadId" FOREIGN KEY ("LeadId") REFERENCES "Leads" ("LeadId") ON DELETE SET NULL,
    CONSTRAINT "FK_Activities_Opportunities_OpportunityId" FOREIGN KEY ("OpportunityId") REFERENCES "Opportunities" ("OpportunityId") ON DELETE SET NULL
);

CREATE TABLE "FollowUps" (
    "FollowUpId" INTEGER NOT NULL CONSTRAINT "PK_FollowUps" PRIMARY KEY AUTOINCREMENT,
    "CustomerId" INTEGER NULL,
    "LeadId" INTEGER NULL,
    "OpportunityId" INTEGER NULL,
    "FollowUpDate" TEXT NOT NULL,
    "FollowUpType" INTEGER NOT NULL,
    "Subject" TEXT NOT NULL,
    "Remarks" TEXT NULL,
    "Status" INTEGER NOT NULL,
    "AssignedToUserId" TEXT NOT NULL,
    "CreatedDate" TEXT NOT NULL,
    "CompletedDate" TEXT NULL,
    CONSTRAINT "FK_FollowUps_AspNetUsers_AssignedToUserId" FOREIGN KEY ("AssignedToUserId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_FollowUps_Customers_CustomerId" FOREIGN KEY ("CustomerId") REFERENCES "Customers" ("CustomerId") ON DELETE SET NULL,
    CONSTRAINT "FK_FollowUps_Leads_LeadId" FOREIGN KEY ("LeadId") REFERENCES "Leads" ("LeadId") ON DELETE SET NULL,
    CONSTRAINT "FK_FollowUps_Opportunities_OpportunityId" FOREIGN KEY ("OpportunityId") REFERENCES "Opportunities" ("OpportunityId") ON DELETE SET NULL
);

CREATE INDEX "IX_Activities_AssignedToUserId" ON "Activities" ("AssignedToUserId");

CREATE INDEX "IX_Activities_CustomerId" ON "Activities" ("CustomerId");

CREATE INDEX "IX_Activities_LeadId" ON "Activities" ("LeadId");

CREATE INDEX "IX_Activities_OpportunityId" ON "Activities" ("OpportunityId");

CREATE INDEX "IX_AspNetRoleClaims_RoleId" ON "AspNetRoleClaims" ("RoleId");

CREATE UNIQUE INDEX "RoleNameIndex" ON "AspNetRoles" ("NormalizedName");

CREATE INDEX "IX_AspNetUserClaims_UserId" ON "AspNetUserClaims" ("UserId");

CREATE INDEX "IX_AspNetUserLogins_UserId" ON "AspNetUserLogins" ("UserId");

CREATE INDEX "IX_AspNetUserRoles_RoleId" ON "AspNetUserRoles" ("RoleId");

CREATE INDEX "EmailIndex" ON "AspNetUsers" ("NormalizedEmail");

CREATE INDEX "IX_AspNetUsers_ManagerId" ON "AspNetUsers" ("ManagerId");

CREATE UNIQUE INDEX "UserNameIndex" ON "AspNetUsers" ("NormalizedUserName");

CREATE INDEX "IX_AuditLogs_Action" ON "AuditLogs" ("Action");

CREATE INDEX "IX_AuditLogs_CreatedDate" ON "AuditLogs" ("CreatedDate");

CREATE INDEX "IX_AuditLogs_EntityName" ON "AuditLogs" ("EntityName");

CREATE INDEX "IX_AuditLogs_UserId" ON "AuditLogs" ("UserId");

CREATE INDEX "IX_Customers_AssignedToUserId" ON "Customers" ("AssignedToUserId");

CREATE INDEX "IX_Customers_CreatedByUserId" ON "Customers" ("CreatedByUserId");

CREATE UNIQUE INDEX "IX_Customers_CustomerCode" ON "Customers" ("CustomerCode");

CREATE UNIQUE INDEX "IX_Customers_Email" ON "Customers" ("Email");

CREATE UNIQUE INDEX "IX_Customers_Phone" ON "Customers" ("Phone");

CREATE INDEX "IX_FollowUps_AssignedToUserId" ON "FollowUps" ("AssignedToUserId");

CREATE INDEX "IX_FollowUps_CustomerId" ON "FollowUps" ("CustomerId");

CREATE INDEX "IX_FollowUps_LeadId" ON "FollowUps" ("LeadId");

CREATE INDEX "IX_FollowUps_OpportunityId" ON "FollowUps" ("OpportunityId");

CREATE INDEX "IX_Leads_AssignedToUserId" ON "Leads" ("AssignedToUserId");

CREATE INDEX "IX_Leads_CustomerId" ON "Leads" ("CustomerId");

CREATE UNIQUE INDEX "IX_Leads_LeadCode" ON "Leads" ("LeadCode");

CREATE INDEX "IX_Opportunities_AssignedToUserId" ON "Opportunities" ("AssignedToUserId");

CREATE INDEX "IX_Opportunities_CustomerId" ON "Opportunities" ("CustomerId");

CREATE INDEX "IX_Opportunities_LeadId" ON "Opportunities" ("LeadId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261103162806_InitialCreate', '8.0.11');

COMMIT;

