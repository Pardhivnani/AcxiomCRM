namespace AcxiomCRM.Models.Enums
{
    public static class UserRoles
    {
        public const string Admin = "Admin";
        public const string Manager = "Manager";
        public const string SalesExecutive = "SalesExecutive";

        public static readonly string[] AllRoles = new[] { Admin, Manager, SalesExecutive };
    }

    public enum CustomerStatus
    {
        Active = 1,
        Inactive = 2,
        Blocked = 3
    }

    public enum LeadStatus
    {
        New = 1,
        Contacted = 2,
        Qualified = 3,
        Unqualified = 4,
        Converted = 5,
        Lost = 6
    }

    public enum LeadPriority
    {
        Low = 1,
        Medium = 2,
        High = 3
    }

    public enum OpportunityStage
    {
        Qualification = 1,
        Proposal = 2,
        Negotiation = 3,
        Won = 4,
        Lost = 5
    }

    public enum OpportunityStatus
    {
        Open = 1,
        Won = 2,
        Lost = 3
    }

    public enum FollowUpType
    {
        Call = 1,
        Meeting = 2,
        Email = 3,
        Demo = 4,
        FollowUp = 5
    }

    public enum FollowUpStatus
    {
        Planned = 1,
        Completed = 2,
        Missed = 3,
        Cancelled = 4
    }

    public enum ActivityType
    {
        Call = 1,
        Meeting = 2,
        Email = 3,
        Task = 4
    }

    public enum ActivityStatus
    {
        Pending = 1,
        InProgress = 2,
        Completed = 3,
        Cancelled = 4
    }
}

