using System.Text.Json;
using Models.RequestModel;
using Common.Services; 

namespace Models;

public class BackgroundWorker : BackgroundService
{
    private readonly LogQueue _queue;
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions;

    public BackgroundWorker(LogQueue queue, IHostEnvironment env)
    {
        _queue = queue;
        _filePath = Path.Combine(env.ContentRootPath, "Logs", "Log.json");
        _jsonOptions = new JsonSerializerOptions { WriteIndented = true };

        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var log = await _queue.PullLogAsync(stoppingToken);
                var jsonString = JsonSerializer.Serialize(log, _jsonOptions);
                await File.AppendAllTextAsync(_filePath, jsonString + Environment.NewLine, stoppingToken);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                await Console.Out.WriteLineAsync($"[Log Error]: {ex.Message}");
            }
        }
    }
}