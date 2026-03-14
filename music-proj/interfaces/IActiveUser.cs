namespace Common.Active
{
    /// <summary>
    /// IActiveUser - ממשק הנוגע למידע על המשתמש הפעיל בבקשה הנוכחית.
    /// - מיועד להירשם כscoped בDI.
    /// - משמש שירותים כדי לקבל מידע על המשתמש בלי לפנות ישירות לHttpContext.
    /// </summary>
    public interface IActiveUser
    {
        int? Id { get; }
        string? Name { get; }
        string? Role { get; }
        bool IsAuthenticated { get; }
        bool IsInRole(string role);
    }
}
