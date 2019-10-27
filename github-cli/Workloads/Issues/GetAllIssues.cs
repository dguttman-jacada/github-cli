using System.Linq;
using System.Threading.Tasks;
using github_cli.Exceptions;
using github_cli.Extensions;
using github_cli.Model;
using github_cli.Workloads.Communication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Octokit;

namespace github_cli.Workloads.Issues
{
    internal class GetAllIssues: IWorkload
    {
        private readonly ILogger<GetAllIssues> _logger;
        private readonly ClientFactory _clientFactory;
        private readonly GitHubRepo[] _repos;

        public GetAllIssues(
            ILogger<GetAllIssues> logger,
            IOptions<ConfigRoot> options,
            ClientFactory clientFactory
            )
        {
            _logger = logger;
            _clientFactory = clientFactory;
            this._repos = options.Value.GitHub?.Repositories ?? throw new ConfigurationException(nameof(ConfigRoot.GitHub.Repos));

        }

        public string Name => nameof(GetAllIssues);

        public async Task Execute()
        {
            var client = this._clientFactory.GetClient();
            foreach (var repo in this._repos)
            {
                this._logger.LogInformation($"{nameof(Execute)}: Getting issues for '{repo.Owner}/{repo.Name}'...");
                var issues = await client.Issue.GetAllForRepository(repo.Owner, repo.Name);
                this._logger.LogInformation($"{nameof(Execute)}: Found open: {issues.Count(i => i.State.Value == ItemState.Open)}\r\n" +
                                            $"{string.Join("\r\n", issues.Select(i => $" {i.Summary()}"))}");
                
            }
        }
    }
}
