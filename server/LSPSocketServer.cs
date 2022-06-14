using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Server;
using server.WebSocketPipe;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace server
{
    internal class LSPSocketServer : IHostedService
    {
        //private readonly ILogger<LSPSocketServer> Logger;
        private readonly HttpListener HttpListener = new();

        public LSPSocketServer(ILogger<LSPSocketServer> logger)
        {
            //Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            HttpListener.Prefixes.Add("http://localhost:9000/");
        }

        private Dictionary<string, LanguageServer> _clients = new Dictionary<string, LanguageServer>();

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            //Logger.LogInformation("Started");
            HttpListener.Start();
            while (!cancellationToken.IsCancellationRequested)
            {
                HttpListenerContext? context = await HttpListener.GetContextAsync().WithCancellationToken(cancellationToken);
                if (context is null)
                    return;

                if (!context.Request.IsWebSocketRequest)
                    context.Response.Abort();
                else
                {
                    HttpListenerWebSocketContext? webSocketContext =
                        await context.AcceptWebSocketAsync(subProtocol: null).WithCancellationToken(cancellationToken);

                    if (webSocketContext is null)
                        return;

                    string clientId = Guid.NewGuid().ToString();
                    WebSocket webSocket = webSocketContext.WebSocket;
                    
                    _ = Task.Run(async () =>
                    {
                        // TODO: handle client disconnect etc.

                        var server = await LanguageServer.From(options => ConfigureServer(options, webSocket.AsPipeReader(), webSocket.AsPipeWriter()));
                        _clients.TryAdd(clientId, server);

                        //while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested) { }

                        //server.ForcefulShutdown();

                    });
                }
            }
        }

        public static void ConfigureServer(LanguageServerOptions options, PipeReader input, PipeWriter output)
        {
            options
                .WithInput(input)
                .WithOutput(output)
                .ConfigureLogging(
                    x => x
                        .ClearProviders()
                        .AddLanguageProtocolLogging()
                        .SetMinimumLevel(LogLevel.Debug)
                )
                .WithServices(services =>
                {
                    services
                        .AddSingleton<TextDocumentStore>()
                         .AddSingleton<CompletionProvider>()
                         .AddSingleton<HoverProvider>()
                         .AddSingleton<TokenProvider>()
                         .AddSingleton<OutlineProvider>()
                         .AddSingleton<CodeActionProvider>()
                         .AddSingleton<CodeActionProvider.CommandHandler>()
                         .AddSingleton<CodeLensProvider>()
                         .AddSingleton<FoldingRangeProvider>()
                         .AddSingleton<SelectionRangeProvider>()
                        //.ConfigureSection<IniConfiguration>("ini")
                        //.ConfigureSection<NinConfiguration>("nin")
                        ;
                })
                //.WithConfigurationSection("ini")
                //.WithConfigurationSection("nin")
                .OnInitialized((instance, client, server, ct) =>
                {
                    
                    return Task.CompletedTask;
                });
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            //Logger.LogInformation("Stopping...");
            HttpListener.Stop();
            //Logger.LogInformation("Stopped");
            return Task.CompletedTask;
        }
    }
}
