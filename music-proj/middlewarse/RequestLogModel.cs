using System.Diagnostics;
using Models.RequestModel;
using Models;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Http;

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
DateTime startTime = DateTime.Now;
var sw = Stopwatch.StartNew();

await _next(context);

sw.Stop();

var endpoint = context.GetEndpoint();
var actionDescriptor = endpoint?.Metadata.GetMetadata<ControllerActionDescriptor>();

var controllerName = actionDescriptor?.ControllerName ?? context.Request.Path.Value ?? "Unknown";
var actionName = actionDescriptor?.ActionName ?? context.Request.Method ?? "Unknown";

var log = new RequestLogModel
{
	StartTime = startTime,
	ControllerName = controllerName,
	ActionName = actionName,
	UserName = context.User.Identity?.Name ?? "Anonymous",
	DurationMs = sw.ElapsedMilliseconds
};

await _queue.QueueLogAsync(log);
}
}




