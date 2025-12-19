namespace FlowFlex.Domain.Shared.Constants;

/// <summary>
/// Email related constants
/// </summary>
public static class EmailConstants
{
    /// <summary>
    /// Email provider types
    /// </summary>
    public static class Provider
    {
        public const string Outlook = "Outlook";
        public const string Gmail = "Gmail";
    }

    /// <summary>
    /// Sync status values
    /// </summary>
    public static class SyncStatus
    {
        public const string Active = "Active";
        public const string Error = "Error";
        public const string Disabled = "Disabled";
        public const string Syncing = "Syncing";
    }

    /// <summary>
    /// Email folder names (local)
    /// </summary>
    public static class Folder
    {
        public const string Inbox = "Inbox";
        public const string Sent = "Sent";
        public const string Drafts = "Drafts";
        public const string Trash = "Trash";
        public const string Archive = "Archive";
    }

    /// <summary>
    /// Outlook folder IDs (Graph API)
    /// </summary>
    public static class OutlookFolder
    {
        public const string Inbox = "inbox";
        public const string SentItems = "sentitems";
        public const string Drafts = "drafts";
        public const string DeletedItems = "deleteditems";
        public const string Archive = "archive";
    }

    /// <summary>
    /// Email labels
    /// </summary>
    public static class Label
    {
        public const string External = "External";
        public const string Internal = "Internal";
    }

    /// <summary>
    /// Message types
    /// </summary>
    public static class MessageType
    {
        public const string Email = "Email";
        public const string Notification = "Notification";
    }

    /// <summary>
    /// Default sync settings
    /// </summary>
    public static class SyncSettings
    {
        public const int DefaultIntervalMinutes = 15;
        public const int MinIntervalMinutes = 5;
        public const int MaxIntervalMinutes = 1440;
        public const int DefaultMaxCount = 100;
        public const int FullSyncMaxCount = 2000;
        public const int BatchSize = 50;
    }
}
