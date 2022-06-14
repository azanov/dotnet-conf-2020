using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace server
{
    public static class TaskExtensions
    {
        public static async Task<T?> WithCancellationToken<T>(this Task<T> source, CancellationToken cancellationToken)
        {
            var cancellationTask = new TaskCompletionSource<bool>();
            cancellationToken.Register(() => cancellationTask.SetCanceled());

            _ = await Task.WhenAny(source, cancellationTask.Task);

            if (cancellationToken.IsCancellationRequested)
                return default;
            return source.Result;
        }
    }
}
