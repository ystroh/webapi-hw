using System.Threading.Channels;
using System.Threading;
using Models.RequestModel;

namespace Models
{
	public class LogQueue
	{
		private readonly Channel<RequestLogModel> _queue;

		public LogQueue()
		{
			_queue = Channel.CreateUnbounded<RequestLogModel>();
		}

		public async ValueTask QueueLogAsync(RequestLogModel log)
		{
			await _queue.Writer.WriteAsync(log);
		}

		public async ValueTask<RequestLogModel> PullLogAsync(CancellationToken ct)
		{
			return await _queue.Reader.ReadAsync(ct);
		}
	}
}