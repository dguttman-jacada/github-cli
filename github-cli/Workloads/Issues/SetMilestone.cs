using System.Collections.Generic;
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
    internal class SetMilestone : IWorkload
    {
        private readonly ILogger<SetMilestone> _logger;
        private readonly ClientFactory _clientFactory;
        private readonly GitHubRepo[] _repos;
        private readonly string _defaultMilestoneName;

        public SetMilestone(
            ILogger<SetMilestone> logger,
            IOptions<ConfigRoot> options,
            ClientFactory clientFactory
            )
        {
            _logger = logger;
            _clientFactory = clientFactory;
            this._repos = options.Value.GitHub?.Repositories ?? throw new ConfigurationException(nameof(ConfigRoot.GitHub.Repos));
            this._defaultMilestoneName = options.Value.GitHub?.DefaultMilestone ??
                                         throw new ConfigurationException(nameof(ConfigRoot.GitHub.DefaultMilestone));

        }

        public string Name => nameof(SetMilestone);

        public async Task Execute()
        {
            var client = this._clientFactory.GetClient();
            foreach (var repo in this._repos)
            {
                this._logger.LogInformation($"{nameof(Execute)}: Getting un-milestoned issues for '{repo.Owner}/{repo.Name}'...");
                var issues = await client.Issue.GetAllForRepository(repo.Owner, repo.Name);
                var unMilestoned = issues.Where(i => i.Milestone is null).ToArray();
                this._logger.LogInformation($"{nameof(Execute)}: Found un-miletoned: {unMilestoned.Length}");
                if (unMilestoned.Length == 0) { continue;}
                await this.SetDefaultMilestone(client, unMilestoned, repo);
            }
        }

        private async Task<Milestone> GetOrCreateDefaultMilestone(GitHubClient client, GitHubRepo repo)
        {
            this._logger.LogInformation($"{nameof(GetOrCreateDefaultMilestone)}: Getting default milestone '{this._defaultMilestoneName}' for '{repo.Owner}/{repo.Name}'...");
            var milestones = await client.Issue.Milestone.GetAllForRepository(repo.Owner, repo.Name);
            var defaultMilestone = milestones.FirstOrDefault(m => m.Title == this._defaultMilestoneName);
            if (defaultMilestone is null)
            {
                this._logger.LogInformation($"{nameof(GetOrCreateDefaultMilestone)}: Default milestone '{this._defaultMilestoneName}' not found for '{repo.Owner}/{repo.Name}'. Creating one...");
                defaultMilestone = await client.Issue.Milestone.Create(repo.Owner, repo.Name, new NewMilestone(this._defaultMilestoneName));
            }
            else
            {
                this._logger.LogInformation($"{nameof(GetOrCreateDefaultMilestone)}: Found default milestone '{defaultMilestone.Title}[{defaultMilestone.Number}]' for '{repo.Owner}/{repo.Name}'.");
            }
            return defaultMilestone;
        }

        private async Task SetDefaultMilestone(GitHubClient client, IEnumerable<Issue> issues, GitHubRepo repo)
        {
            var defaultMilestone = await this.GetOrCreateDefaultMilestone(client, repo);
            foreach (var issue in issues)
            {
                this._logger.LogInformation($"{nameof(SetDefaultMilestone)}: Setting default milestone for issue '{issue.Number}'...");
                await client.Issue.Update(repo.Owner, repo.Name, issue.Number, issue.ToUpdate().SetMilestone(defaultMilestone));
            }
        }
    }
}
