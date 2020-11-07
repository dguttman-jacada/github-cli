using github_cli.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Octokit.GraphQL;

namespace github_cli.Workloads.Communication
{
    internal class GraphClientFactory
    {
        private readonly ILogger<GraphClientFactory> _logger;
        private readonly GitHubConfig _config;

        public GraphClientFactory(
            ILogger<GraphClientFactory> logger,
            IOptions<ConfigRoot> options
        )
        {
            _logger = logger;
            this._config = options.Value.GitHub ?? throw new ConfigurationException(nameof(ConfigRoot.GitHub));
        }

        public string ClientId => nameof(github_cli);

        public Connection GetConnection()
        {
            var productInformation = new ProductHeaderValue(ClientId, typeof(App).Assembly.GetName().Version!.ToString());
            var connection = new Connection(productInformation, this._config.Token);
            return connection;
        }
    }
}