using server.WebSocketPipe;
using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace server.WebSocketPipe
{
	public static class PipelinesExtensions
	{

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static PipeWriter AsPipeWriter(this WebSocket webSocket, WebSocketPipeWriterOptions? options = null)
		{
			return new WebSocketPipeWriter(webSocket, options ?? WebSocketPipeWriterOptions.Default);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static PipeReader AsPipeReader(this WebSocket webSocket, WebSocketPipeReaderOptions? options = null)
		{
			return new WebSocketPipeReader(webSocket, options ?? WebSocketPipeReaderOptions.Default);
		}
	}
}
