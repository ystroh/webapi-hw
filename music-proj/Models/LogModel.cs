using System;

namespace Models.RequestModel
{
    public class RequestLogModel
    {
        public DateTime StartTime { get; set; }
        public string ControllerName { get; set; }
        public string ActionName { get; set; }
        public string UserName { get; set; }
        public long DurationMs { get; set; }

        public override string ToString()
        {
            return $"[{StartTime:yyyy-MM-dd HH:mm:ss}] | User: {UserName ?? "Anonymous"} | Path: {ControllerName}/{ActionName} | Duration: {DurationMs}ms";
        }
    }
}