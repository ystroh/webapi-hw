using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Security.Claims;

public class MusicHub : Hub {

  // מילון סטטי ששומר לכל UserId (המפתח) רשימה של ConnectionIds (הערך)
  // ConcurrentDictionary בטוח לשימוש כשכמה אנשים מתחברים במקביל (מונע Race Conditions בזיכרון)
  private static readonly ConcurrentDictionary<string, HashSet<string>> _userConnections =
      new ConcurrentDictionary<string, HashSet<string>>();

  // פונקציה שרצה אוטומטית כשדפדפן מתחבר:
  public override async Task OnConnectedAsync() {
      // נעדיף להשתמש ב-ClaimTypes.NameIdentifier כדי להתאים ל-IActiveUser
      var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? Context.User?.Identity?.Name;

      if (!string.IsNullOrEmpty(userId)) {
          _userConnections.AddOrUpdate(userId,
              new HashSet<string> { Context.ConnectionId },
              (key, oldList) => {
                  lock(oldList) { oldList.Add(Context.ConnectionId); }
                  return oldList;
              });
      }
      await base.OnConnectedAsync();
  }

  // פונקציה שרצה אוטומטית כשדפדפן נסגר או מתנתק:
  public override async Task OnDisconnectedAsync(Exception? exception) {
      var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? Context.User?.Identity?.Name;

      if (!string.IsNullOrEmpty(userId) && _userConnections.TryGetValue(userId, out var connections)) {
          lock(connections) { connections.Remove(Context.ConnectionId); }
      }
      await base.OnDisconnectedAsync(exception);
  }

  // פונקציית עזר שתחזיר לנו את כל ה-Connections של משתמש ספציפי
  public static IEnumerable<string> GetConnections(string userId) {
      return _userConnections.TryGetValue(userId, out var connections)
             ? connections.ToList()
             : Enumerable.Empty<string>();
  }
}