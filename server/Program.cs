using System;
using OmniSharp.Extensions.LanguageServer;
using OmniSharp.Extensions.LanguageServer.Server;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.IO;
using System.IO.Pipes;
using System.IO.Pipelines;
using Nerdbank.Streams;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Collections.Immutable;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Diagnostics;
using Microsoft.Extensions.Hosting;

namespace server
{
    public static class Program
    {
        static async Task Main(string[] args)
        {
            IHostBuilder hostBuilder = Host.CreateDefaultBuilder(args)
           .ConfigureServices(services =>
           {
               
               services.AddSingleton<LSPSocketServer>();
               services.AddHostedService<LSPSocketServer>();
           });
            IHost host = hostBuilder.Build();
            await host.RunAsync();


        }

        

        

        public static IServiceCollection ConfigureSection<TOptions>(this IServiceCollection services, string? sectionName)
            where TOptions : class
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddOptions()
                .AddSingleton<IOptionsChangeTokenSource<TOptions>>(
                _ => new ConfigurationChangeTokenSource<TOptions>(
                    Options.DefaultName,
                    sectionName == null ? _.GetRequiredService<IConfiguration>() : _.GetRequiredService<IConfiguration>().GetSection(sectionName)
                )
            );
            return services.AddSingleton<IConfigureOptions<TOptions>>(
                _ => new NamedConfigureFromConfigurationOptions<TOptions>(
                    Options.DefaultName,
                    sectionName == null ? _.GetRequiredService<IConfiguration>() : _.GetRequiredService<IConfiguration>().GetSection(sectionName)
                )
            );
        }
    }
}
