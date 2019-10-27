using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using github_cli.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Octokit;

namespace github_cli.Workloads.Communication
{
    internal class ClientFactory
    {
        private readonly ILogger<ClientFactory> _logger;
        private readonly GitHubConfig _config;

        public ClientFactory(
            ILogger<ClientFactory> logger,
            IOptions<ConfigRoot> options
        )
        {
            _logger = logger;
            this._config = options.Value.GitHub ?? throw new ConfigurationException(nameof(ConfigRoot.GitHub));
        }

        public GitHubClient GetClient()
        {
            var client = new GitHubClient(new ProductHeaderValue(
                name: nameof(github_cli),
                version: typeof(App).Assembly.GetName().Version!.ToString()))
            {
                Credentials = new Credentials(this._config.Token)
            };
            return client;
        }
    }
}
