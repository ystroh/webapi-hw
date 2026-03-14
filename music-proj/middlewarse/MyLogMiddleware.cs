using System.Diagnostics;

namespace MyMiddleware;

/// <summary>
/// Middleware פשוט ללוגים: מדפיס מידע על כל בקשה נכנסת וכן אם קיימת כותרת Authorization (בצורה ממוסכת).
/// מטרת הלוג: לאפשר לדבג במהירות אם הלקוח שולח את ה‑Authorization header והאם הבקשה הגיעה לשרת
/// לפני שלב האימות/הרשאה. אין כאן טיפול ברגישות יתר — ה‑token מודפס בפורמט ממוסך בלבד.
/// </summary>
public class MyLogMiddleware
{
    private readonly RequestDelegate next;
    private readonly ILogger logger;


    public MyLogMiddleware(RequestDelegate next, ILogger<MyLogMiddleware> logger)
    {
        this.next = next;
        this.logger = logger;
    }

    public async Task Invoke(HttpContext c)
    {
        var sw = new Stopwatch();
        sw.Start();
        try
        {
            // לוג בסיסי של הבקשה: שיטה, נתיב והאם נשלחה כותרת Authorization (ממוּסכת)
            var authHeader = c.Request.Headers.ContainsKey("Authorization") ? c.Request.Headers["Authorization"].ToString() : null;
            var masked = authHeader == null ? "<none>" : (authHeader.Length > 20 ? authHeader.Substring(0, 20) + "..." : authHeader);
            logger.LogInformation("Incoming request: {method} {path} Authorization: {auth}", c.Request.Method, c.Request.Path, masked);
        }
        catch (Exception ex)
        {
            logger.LogWarning("Failed to log incoming request info: {msg}", ex.Message);
        }
        await next.Invoke(c);
        logger.LogDebug($"{c.Request.Path}.{c.Request.Method} took {sw.ElapsedMilliseconds}ms."
            + $" User: {c.User?.FindFirst("userId")?.Value ?? "unknown"}");
        
    }
}

public static partial class MiddlewareExtensions
{
    public static IApplicationBuilder UseMyLogMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<MyLogMiddleware>();
    }
}

