using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Security.Claims;

public class MusicHub : Hub 
{
    // מילון בטוח לריצה במקביל השומר לכל משתמש את רשימת החיבורים הפעילים שלו
    private static readonly ConcurrentDictionary<string, HashSet<string>> _userConnections =
        new ConcurrentDictionary<string, HashSet<string>>();

    /// <summary>
    /// מתבצע בעת חיבור לקוח חדש: הוספת מזהה החיבור לרשימה של המשתמש
    /// </summary>
    public override async Task OnConnectedAsync() 
    {
        var userId = GetUserId();

        if (!string.IsNullOrEmpty(userId)) 
        {
            _userConnections.AddOrUpdate(userId,
                new HashSet<string> { Context.ConnectionId },
                (key, currentConnections) => 
                {
                    lock(currentConnections) { currentConnections.Add(Context.ConnectionId); }
                    return currentConnections;
                });
        }
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// מתבצע בעת ניתוק לקוח: הסרת מזהה החיבור הספציפי מרשימת המשתמש
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception) 
    {
        var userId = GetUserId();

        if (!string.IsNullOrEmpty(userId) && _userConnections.TryGetValue(userId, out var connections)) 
        {
            lock(connections) { connections.Remove(Context.ConnectionId); }
        }
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// שליפת כל מזהי החיבור הפעילים עבור משתמש ספציפי
    /// </summary>
    public static IEnumerable<string> GetConnections(string userId) 
    {
        return _userConnections.TryGetValue(userId, out var connections)
               ? connections.ToList()
               : Enumerable.Empty<string>();
    }

    // חילוץ מזהה המשתמש מתוך ה-Identity
    private string? GetUserId() => 
        Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? Context.User?.Identity?.Name;
}