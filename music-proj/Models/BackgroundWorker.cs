using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.IO;
using System.Text.Json;
using Models;
using Models.RequestModel;

namespace Models
{
    public class BackgroundWorker : BackgroundService
    {
        private readonly LogQueue _queue;
        private readonly string _filePath;

        public BackgroundWorker(LogQueue queue, IHostEnvironment env)
        {
            _queue = queue;

            // יצירת הנתיב המלא לתיקיית Logs בתוך הפרויקט
            _filePath = Path.Combine(env.ContentRootPath, "Logs", "Log.json");

            // וודאי שהתיקייה קיימת, אם לא - ניצור אותה
            var dir = Path.GetDirectoryName(_filePath) ?? env.ContentRootPath;
            Directory.CreateDirectory(dir);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // שליפת הלוג מהתור (מחכה כאן עד שיגיע לוג או עד שהשרת ייסגר)
                    var log = await _queue.PullLogAsync(stoppingToken);

                    // הפיכת האובייקט למחרוזת JSON
                    var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
                    string jsonString = JsonSerializer.Serialize(log, jsonOptions);

                    // כתיבה לקובץ - הוספה לסוף הקובץ (Append)
                    await File.AppendAllTextAsync(_filePath, jsonString + Environment.NewLine, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // השרת נסגר - יוצאים מהלולאה
                    break;
                }
                catch (Exception ex)
                {
                    // טיפול בשגיאות כתיבה כדי שה-Worker לא יקרוס
                    Console.WriteLine($"Error writing log: {ex.Message}");
                }
            }
        }
    }
}