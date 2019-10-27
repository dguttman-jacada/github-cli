using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using github_cli.Exceptions;
using github_cli.Model;
using github_cli.Workloads.Communication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Octokit;

namespace github_cli.Workloads.Issues
{
    internal class GetProjectCards: IWorkload
    {
        private readonly ILogger<GetProjectCards> _logger;
        private readonly ClientFactory _clientFactory;
        private readonly string[] _projects;
        private readonly ColumnConfig? _columns;
        private readonly OutputConfig? _output;
        private readonly CsvFactory _csvFactory;

        public GetProjectCards(
            ILogger<GetProjectCards> logger,
            IOptions<ConfigRoot> options,
            ClientFactory clientFactory,
            CsvFactory csvFactory
        )
        {
            _logger = logger;
            _clientFactory = clientFactory;
            _projects = options.Value.GitHub?.Projects ?? throw new ConfigurationException(nameof(GitHubConfig.Projects));
            _columns = options.Value.GitHub?.Columns;
            _output = options.Value.Outputs?.FirstOrDefault(o => o.Workload == this.Name);
            _output?.Verify();
            _csvFactory = csvFactory;
        }


        public string Name => nameof(GetProjectCards);

        public async Task Execute()
        {
            var client = _clientFactory.GetClient();
            foreach (var proj in _projects.Select(p => new GitHubRepo(p)))
            {
                var project = await this.GetProject(client, proj);
                var columns = await this.GetColumns(client, project);
                var cards = await this.GetCards(client, project, columns);
                await this.LoadIssues(client.Issue, cards);
                await _csvFactory.Write(_output!, cards);
            }
        }

        private async Task<Project> GetProject(IGitHubClient client, GitHubRepo proj)
        {
            this._logger.LogInformation($"{nameof(GetProject)}: Getting project for '{proj.Owner}/{proj.Name}'...");
            var projects = await client.Repository.Project.GetAllForOrganization(proj.Owner);
            var project = projects.FirstOrDefault(p => p.Name == proj.Name);
            if (project is null) { throw new ArgumentException($"Project '{proj.Owner}/{proj.Name}' not found"); }
            return project;
        }

        private async Task<IReadOnlyList<ProjectColumn>> GetColumns(IGitHubClient client, Project project)
        {
            this._logger.LogInformation($"{nameof(GetColumns)}: Getting project's columns...");
            var columns = await client.Repository.Project.Column.GetAll(project.Id);
            return columns;
        }

        private async Task<List<CardWrapper>> GetCards(IGitHubClient client, Project project, IReadOnlyList<ProjectColumn> columns)
        {
            var allCards = new List<CardWrapper>();
            foreach (var column in columns)
            {
                if (_columns?.Enabled(column) == false)
                {
                    this._logger.LogInformation($"{nameof(GetCards)}: Skipping cards for '{column.Name}' column...");
                    continue;
                }
                this._logger.LogInformation($"{nameof(GetCards)}: Getting cards for '{column.Name}' column...");
                var cards = await client.Repository.Project.Card.GetAll(column.Id);
                allCards.AddRange(cards.Select(card => new CardWrapper(column, card)));
            }

            return allCards;
        }

        private async Task LoadIssues(IIssuesClient client, IEnumerable<CardWrapper> cards)
        {
            foreach (var card in cards)
            {
                this._logger.LogInformation($"{nameof(LoadIssues)}: Loading issue for card '{card.Number}'...");
                await card.LoadIssue(client);
                this._logger.LogInformation($"{nameof(LoadIssues)}: {card.Summary()}");
            }
        }
    }
}
