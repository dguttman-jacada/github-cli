using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace github_cli
{
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    internal class App : IHostedService
    {
        private readonly ILogger<App> _logger;
        private readonly ConfigRoot _config;

        public App(ILogger<App> logger,
            IOptions<ConfigRoot> options)
        {
            _logger = logger;
            _config = options.Value;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            this._logger.LogInformation($"{nameof(StartAsync)}: {this._config.GitHub}");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            this._logger.LogInformation(nameof(StopAsync));
            return Task.CompletedTask;
        }
    }
}
