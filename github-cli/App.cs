using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using github_cli.Workloads;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace github_cli
{
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    internal class App : IHostedService
    {
        private readonly ILogger<App> _logger;
        private readonly CancellationTokenSource _tokenSource;
        private readonly ConfigRoot _config;
        private readonly IWorkload[] _workloads;
        private readonly RootCommand _rootCommand;

        public App(ILogger<App> logger,
            IOptions<ConfigRoot> options,
            CancellationTokenSource tokenSource,
            IEnumerable<IWorkload> workloads,
            RootCommand rootCommand)
        {
            _logger = logger;
            _tokenSource = tokenSource;
            _config = options.Value;
            _workloads = workloads.ToArray();
            _rootCommand = rootCommand;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var parsed = this._rootCommand.Parse(Environment.GetCommandLineArgs().Skip(1).ToArray());
            if (parsed.Errors.Count > 0) { throw new InvalidOperationException("Unsupported arguments"); }
            var command = parsed.CommandResult.Command.Name;
            var workload = this._workloads.FirstOrDefault(w => w.Name == command);
            if (workload is null) { throw new InvalidOperationException($"Unsupported command '{command}'"); }
            this._logger.LogInformation($"{nameof(StartAsync)}: Executing workload '{workload.Name}'...");
            await workload.Execute();
            _tokenSource.Cancel();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            this._logger.LogInformation(nameof(StopAsync));
            return Task.CompletedTask;
        }
    }
}
