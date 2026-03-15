using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Controllers;
using Models.RequestModel;
using Common.Services; // חשוב מאוד לזיהוי LogQueue

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly LogQueue _queue;

    public RequestLoggingMiddleware(RequestDelegate next, LogQueue queue)
    {
        _next = next;
        _queue = queue;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var startTime = DateTime.Now;
        var stopwatch = Stopwatch.StartNew();

        await _next(context);

        stopwatch.Stop();

        var endpoint = context.GetEndpoint();
        var descriptor = endpoint?.Metadata.GetMetadata<ControllerActionDescriptor>();

        var logEntry = new RequestLogModel
        {
            StartTime = startTime,
            ControllerName = descriptor?.ControllerName ?? context.Request.Path,
            ActionName = descriptor?.ActionName ?? context.Request.Method,
            UserName = context.User.Identity?.Name ?? "Anonymous",
            DurationMs = stopwatch.ElapsedMilliseconds
        };

        await _queue.QueueLogAsync(logEntry);
    }
}