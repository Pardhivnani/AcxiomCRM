-- ==============================================================================
-- AcxiomCRM Enterprise Database Schema - Microsoft SQL Server / Azure SQL DDL
-- Compatibility: SQL Server 2017 / 2019 / 2022 / Azure SQL Database
-- ==============================================================================

USE [master];
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'AcxiomCRM')
BEGIN
    CREATE DATABASE [AcxiomCRM];
END
GO

USE [AcxiomCRM];
GO

-- 1. Identity & Application Users
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AspNetUsers')
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] NVARCHAR(450) NOT NULL PRIMARY KEY,
        [UserName] NVARCHAR(256) NULL,
        [NormalizedUserName] NVARCHAR(256) NULL,
        [Email] NVARCHAR(256) NULL,
        [NormalizedEmail] NVARCHAR(256) NULL,
        [EmailConfirmed] BIT NOT NULL DEFAULT 0,
        [PasswordHash] NVARCHAR(MAX) NULL,
        [SecurityStamp] NVARCHAR(MAX) NULL,
        [ConcurrencyStamp] NVARCHAR(MAX) NULL,
        [PhoneNumber] NVARCHAR(MAX) NULL,
        [PhoneNumberConfirmed] BIT NOT NULL DEFAULT 0,
        [TwoFactorEnabled] BIT NOT NULL DEFAULT 0,
        [LockoutEnd] DATETIMEOFFSET NULL,
        [LockoutEnabled] BIT NOT NULL DEFAULT 1,
        [AccessFailedCount] INT NOT NULL DEFAULT 0,
        [FullName] NVARCHAR(100) NOT NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedDate] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        [Department] NVARCHAR(100) NULL,
        [ManagerId] NVARCHAR(450) NULL,
        CONSTRAINT [FK_AspNetUsers_Manager] FOREIGN KEY ([ManagerId]) REFERENCES [AspNetUsers]([Id]) ON DELETE NO ACTION
    );

    CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers]([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;
    CREATE INDEX [EmailIndex] ON [AspNetUsers]([NormalizedEmail]);
END
GO

-- 2. Identity Roles
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AspNetRoles')
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] NVARCHAR(450) NOT NULL PRIMARY KEY,
        [Name] NVARCHAR(256) NULL,
        [NormalizedName] NVARCHAR(256) NULL,
        [ConcurrencyStamp] NVARCHAR(MAX) NULL
    );

    CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles]([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;
END
GO

-- 3. Identity UserRoles Mapping
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AspNetUserRoles')
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] NVARCHAR(450) NOT NULL,
        [RoleId] NVARCHAR(450) NOT NULL,
        PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_Users] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_Roles] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles]([Id]) ON DELETE CASCADE
    );
END
GO

-- 4. Customer Management
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Customers')
BEGIN
    CREATE TABLE [Customers] (
        [CustomerId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [CustomerCode] NVARCHAR(50) NOT NULL,
        [CustomerName] NVARCHAR(150) NOT NULL,
        [Email] NVARCHAR(150) NOT NULL,
        [Phone] NVARCHAR(20) NOT NULL,
        [CompanyName] NVARCHAR(150) NULL,
        [Address] NVARCHAR(250) NULL,
        [City] NVARCHAR(100) NULL,
        [State] NVARCHAR(100) NULL,
        [Status] INT NOT NULL DEFAULT 1, -- 1: Active, 2: Inactive, 3: Blocked
        [CreatedDate] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        [ModifiedDate] DATETIME2 NULL,
        [CreatedByUserId] NVARCHAR(450) NULL,
        [AssignedToUserId] NVARCHAR(450) NULL,
        CONSTRAINT [FK_Customers_CreatedBy] FOREIGN KEY ([CreatedByUserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Customers_AssignedTo] FOREIGN KEY ([AssignedToUserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE NO ACTION
    );

    CREATE UNIQUE INDEX [IX_Customers_CustomerCode] ON [Customers]([CustomerCode]);
    CREATE UNIQUE INDEX [IX_Customers_Email] ON [Customers]([Email]);
    CREATE UNIQUE INDEX [IX_Customers_Phone] ON [Customers]([Phone]);
END
GO

-- 5. Lead Management
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Leads')
BEGIN
    CREATE TABLE [Leads] (
        [LeadId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [LeadCode] NVARCHAR(50) NOT NULL,
        [LeadName] NVARCHAR(150) NOT NULL,
        [Email] NVARCHAR(150) NOT NULL,
        [Phone] NVARCHAR(20) NOT NULL,
        [CompanyName] NVARCHAR(150) NULL,
        [Source] NVARCHAR(100) NOT NULL DEFAULT 'Website',
        [Status] INT NOT NULL DEFAULT 1, -- 1: New, 2: Contacted, 3: Qualified, 4: Unqualified, 5: Converted, 6: Lost
        [ExpectedValue] DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        [Priority] INT NOT NULL DEFAULT 2, -- 1: Low, 2: Medium, 3: High
        [Notes] NVARCHAR(MAX) NULL,
        [CreatedDate] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        [ModifiedDate] DATETIME2 NULL,
        [AssignedToUserId] NVARCHAR(450) NULL,
        [CustomerId] INT NULL,
        CONSTRAINT [FK_Leads_AssignedTo] FOREIGN KEY ([AssignedToUserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Leads_Customer] FOREIGN KEY ([CustomerId]) REFERENCES [Customers]([CustomerId]) ON DELETE SET NULL
    );

    CREATE UNIQUE INDEX [IX_Leads_LeadCode] ON [Leads]([LeadCode]);
    CREATE INDEX [IX_Leads_Status] ON [Leads]([Status]);
    CREATE INDEX [IX_Leads_AssignedToUserId] ON [Leads]([AssignedToUserId]);
END
GO

-- 6. Opportunity Management
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Opportunities')
BEGIN
    CREATE TABLE [Opportunities] (
        [OpportunityId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [OpportunityName] NVARCHAR(150) NOT NULL,
        [CustomerId] INT NOT NULL,
        [LeadId] INT NULL,
        [Amount] DECIMAL(18,2) NOT NULL,
        [Stage] INT NOT NULL DEFAULT 1, -- 1: Qualification, 2: Proposal, 3: Negotiation, 4: Won, 5: Lost
        [Probability] INT NOT NULL DEFAULT 10,
        [ExpectedCloseDate] DATETIME2 NOT NULL,
        [Status] INT NOT NULL DEFAULT 1, -- 1: Open, 2: Won, 3: Lost
        [CreatedDate] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        [ModifiedDate] DATETIME2 NULL,
        [AssignedToUserId] NVARCHAR(450) NOT NULL,
        [Notes] NVARCHAR(MAX) NULL,
        CONSTRAINT [FK_Opportunities_Customer] FOREIGN KEY ([CustomerId]) REFERENCES [Customers]([CustomerId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Opportunities_Lead] FOREIGN KEY ([LeadId]) REFERENCES [Leads]([LeadId]) ON DELETE SET NULL,
        CONSTRAINT [FK_Opportunities_AssignedTo] FOREIGN KEY ([AssignedToUserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE NO ACTION
    );

    CREATE INDEX [IX_Opportunities_CustomerId] ON [Opportunities]([CustomerId]);
    CREATE INDEX [IX_Opportunities_Stage] ON [Opportunities]([Stage]);
    CREATE INDEX [IX_Opportunities_Status] ON [Opportunities]([Status]);
    CREATE INDEX [IX_Opportunities_AssignedToUserId] ON [Opportunities]([AssignedToUserId]);
END
GO

-- 7. Follow-Up Management
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'FollowUps')
BEGIN
    CREATE TABLE [FollowUps] (
        [FollowUpId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [CustomerId] INT NULL,
        [LeadId] INT NULL,
        [OpportunityId] INT NULL,
        [FollowUpDate] DATETIME2 NOT NULL,
        [FollowUpType] INT NOT NULL DEFAULT 1, -- 1: Call, 2: Meeting, 3: Email, 4: Demo, 5: FollowUp
        [Subject] NVARCHAR(200) NOT NULL,
        [Remarks] NVARCHAR(MAX) NULL,
        [Status] INT NOT NULL DEFAULT 1, -- 1: Planned, 2: Completed, 3: Missed, 4: Cancelled
        [AssignedToUserId] NVARCHAR(450) NOT NULL,
        [CreatedDate] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        [CompletedDate] DATETIME2 NULL,
        CONSTRAINT [FK_FollowUps_Customer] FOREIGN KEY ([CustomerId]) REFERENCES [Customers]([CustomerId]) ON DELETE SET NULL,
        CONSTRAINT [FK_FollowUps_Lead] FOREIGN KEY ([LeadId]) REFERENCES [Leads]([LeadId]) ON DELETE SET NULL,
        CONSTRAINT [FK_FollowUps_Opportunity] FOREIGN KEY ([OpportunityId]) REFERENCES [Opportunities]([OpportunityId]) ON DELETE SET NULL,
        CONSTRAINT [FK_FollowUps_AssignedTo] FOREIGN KEY ([AssignedToUserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE NO ACTION
    );

    CREATE INDEX [IX_FollowUps_FollowUpDate] ON [FollowUps]([FollowUpDate]);
    CREATE INDEX [IX_FollowUps_Status] ON [FollowUps]([Status]);
    CREATE INDEX [IX_FollowUps_AssignedToUserId] ON [FollowUps]([AssignedToUserId]);
END
GO

-- 8. Activity Management
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Activities')
BEGIN
    CREATE TABLE [Activities] (
        [ActivityId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [ActivityType] INT NOT NULL DEFAULT 4, -- 1: Call, 2: Meeting, 3: Email, 4: Task
        [Subject] NVARCHAR(200) NOT NULL,
        [Description] NVARCHAR(MAX) NULL,
        [ActivityDate] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        [CustomerId] INT NULL,
        [LeadId] INT NULL,
        [OpportunityId] INT NULL,
        [AssignedToUserId] NVARCHAR(450) NOT NULL,
        [Status] INT NOT NULL DEFAULT 3, -- 1: Pending, 2: InProgress, 3: Completed, 4: Cancelled
        [CreatedDate] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [FK_Activities_Customer] FOREIGN KEY ([CustomerId]) REFERENCES [Customers]([CustomerId]) ON DELETE SET NULL,
        CONSTRAINT [FK_Activities_Lead] FOREIGN KEY ([LeadId]) REFERENCES [Leads]([LeadId]) ON DELETE SET NULL,
        CONSTRAINT [FK_Activities_Opportunity] FOREIGN KEY ([OpportunityId]) REFERENCES [Opportunities]([OpportunityId]) ON DELETE SET NULL,
        CONSTRAINT [FK_Activities_AssignedTo] FOREIGN KEY ([AssignedToUserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE NO ACTION
    );

    CREATE INDEX [IX_Activities_ActivityDate] ON [Activities]([ActivityDate]);
    CREATE INDEX [IX_Activities_AssignedToUserId] ON [Activities]([AssignedToUserId]);
END
GO

-- 9. Security Audit Logging (Append-Only)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AuditLogs')
BEGIN
    CREATE TABLE [AuditLogs] (
        [AuditLogId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId] NVARCHAR(450) NULL,
        [UserEmail] NVARCHAR(150) NULL,
        [Action] NVARCHAR(100) NOT NULL,
        [EntityName] NVARCHAR(100) NOT NULL,
        [RecordId] NVARCHAR(100) NULL,
        [OldValue] NVARCHAR(MAX) NULL,
        [NewValue] NVARCHAR(MAX) NULL,
        [CreatedDate] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        [IpAddress] NVARCHAR(50) NULL,
        [Result] NVARCHAR(50) NOT NULL DEFAULT 'Success'
    );

    CREATE INDEX [IX_AuditLogs_CreatedDate] ON [AuditLogs]([CreatedDate]);
    CREATE INDEX [IX_AuditLogs_EntityName] ON [AuditLogs]([EntityName]);
    CREATE INDEX [IX_AuditLogs_UserId] ON [AuditLogs]([UserId]);
    CREATE INDEX [IX_AuditLogs_Action] ON [AuditLogs]([Action]);
END
GO

PRINT 'AcxiomCRM schema created successfully with all primary keys, foreign keys, unique constraints, and indexes.';
GO

