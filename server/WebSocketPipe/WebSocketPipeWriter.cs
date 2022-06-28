using Microsoft;
using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace server.WebSocketPipe;

internal sealed class WebSocketPipeWriter : PipeWriter
{
	public WebSocket InternalWebSocket { get; }

	private readonly Pipe _pipe;
	private PipeWriter Writer => _pipe.Writer;
	private PipeReader Reader => _pipe.Reader;

	public WebSocketPipeWriter(WebSocket webSocket, WebSocketPipeWriterOptions options)
	{
		Requires.NotNull(webSocket, nameof(webSocket));
		Requires.NotNull(options, nameof(options));

		InternalWebSocket = webSocket;
		_pipe = new Pipe(options.PipeOptions);
	}

	public override void Advance(int bytes)
	{
		Writer.Advance(bytes);
	}

	public override Memory<byte> GetMemory(int sizeHint = 0)
	{
		return Writer.GetMemory(sizeHint);
	}

	public override Span<byte> GetSpan(int sizeHint = 0)
	{
		return Writer.GetSpan(sizeHint);
	}

	public override void CancelPendingFlush()
	{
		Writer.CancelPendingFlush();
	}

	public override void Complete(Exception? exception = null)
	{
		Writer.Complete(exception);
	}

	public override async ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default)
	{
		ValueTask<FlushResult> flushTask = Writer.FlushAsync(cancellationToken);

		try
		{
			ReadResult result = await Reader.ReadAsync(cancellationToken);
			ReadOnlySequence<byte> buffer = result.Buffer;

			foreach (ReadOnlyMemory<byte> memory in buffer)
			{
				// workaround to filter non-json messages
				// if (memory.Span[0] != 123) continue;

				await InternalWebSocket.SendAsync(memory, WebSocketMessageType.Text, true, cancellationToken);
			}

			Reader.AdvanceTo(buffer.End);

			if (result.IsCompleted)
			{
				await Reader.CompleteAsync();
			}
		}
		catch (Exception ex)
		{
			await Reader.CompleteAsync(ex);
			throw;
		}

		return await flushTask;
	}
}