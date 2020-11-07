using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using github_cli.Exceptions;
using github_cli.Extensions;
using github_cli.Model;
using github_cli.Workloads.Communication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Octokit;
using Octokit.GraphQL;
using Octokit.GraphQL.Core;
using Octokit.GraphQL.Model;
using Connection = Octokit.GraphQL.Connection;
using IConnection = Octokit.GraphQL.IConnection;

namespace github_cli.Workloads.Issues
{
    internal class MoveAwaitingTriageToPending : IWorkload
    {
        private readonly ILogger<GetProjectCards> _logger;
        private readonly ClientFactory _clientFactory;
        private readonly string[] _projects;
        private readonly string _defaultColumn;
        private readonly OutputConfig? _output;
        private readonly GraphClientFactory _graphFactory;

        public MoveAwaitingTriageToPending(
            ILogger<GetProjectCards> logger,
            IOptions<ConfigRoot> options,
            ClientFactory clientFactory,
            GraphClientFactory graphClientFactory
        )
        {
            _logger = logger;
            _clientFactory = clientFactory;
            _graphFactory = graphClientFactory;
            _projects = options.Value.GitHub?.Projects ?? throw new ConfigurationException(nameof(GitHubConfig.Projects));
            _defaultColumn = options.Value.GitHub?.Columns?.Default ?? throw new ConfigurationException(nameof(GitHubConfig.Columns.Default));
            _output = options.Value.Outputs?.FirstOrDefault(o => o.Workload == this.Name);
            _output?.Verify();
        }


        public string Name => nameof(MoveAwaitingTriageToPending);

        public async Task Execute()
        {
            var graph = _graphFactory.GetConnection();
            foreach (var proj in _projects)
            {
                var pendingCards = await this.GetProjectPendingIssues(graph, proj);
                if (pendingCards.Count == 0) continue;

                var destinationColumnId = await this.GetColumnId(graph, proj, _defaultColumn);
                foreach (var pendingCard in pendingCards)
                {
                    await this.MoveCardToColumn(graph, pendingCard, destinationColumnId);
                }
            }
        }

        private async Task<ID> GetColumnId(IConnection graph, string projectName, string columnName)
        {
            this._logger.LogInformation($"{nameof(GetColumnId)}: Searching for '{columnName}' column in '{projectName}'");
            var parser = new GitHubRepo(projectName);
            var query = new Query()
                .Organization(parser.Owner)
                .Projects(last: 1, search: parser.Name)
                .Nodes
                .Select(p => new
                {
                    Columns = p.Columns(100, null, null, null)
                        .Nodes
                        .Select(c => new
                        {
                            c.Name,
                            c.Id
                        })
                        .ToList()
                })
                .Compile();

            var res = await graph.Run(query);
            var column = res.SelectMany(p => p.Columns).FirstOrDefault(c => c.Name == columnName) 
                         ?? throw new NotFoundException($"Column '{columnName}' not found on '{projectName}'", HttpStatusCode.NotFound);

            this._logger.LogDebug($"{nameof(GetColumnId)}: Found {column.Id}");
            return column.Id;
        }

        private async Task<IReadOnlyCollection<ItemBase>> GetProjectPendingIssues(IConnection graph, string projectName)
        {
            this._logger.LogInformation($"{nameof(GetProjectPendingIssues)}: {projectName}");
            var parser = new GitHubRepo(projectName);
            var query = new Query()
                .Organization(parser.Owner)
                .Projects(last: 1, search: parser.Name)
                .Nodes
                .Select(p => new
                {
                    p.Name,
                    p.Number,
                    PendingCards = p.PendingCards(100, null, null, null, null)
                        .Nodes
                        .Select(c => new
                        {
                            Content = c.Content.Switch<ItemBase>(when => when
                                .Issue(i => new ItemBase.IssueItem(i.Number, i.Title, c.Id))
                                .PullRequest(pr => new ItemBase.PullRequestItem(pr.Number, pr.Title, c.Id))
                            )
                        })
                        .ToList()
                })
                .Compile();

            var res = await graph.Run(query);
            var result = res.SelectMany(p => p.PendingCards.Select(c => c.Content)).ToArray();
            if (result.Length == 0)
            {
                _logger.LogInformation($"{nameof(GetProjectPendingIssues)}: No pending cards found on '{projectName}'");
            }
            else
            {
                var cardList = result.Select(p => $"\t{p}");
                _logger.LogInformation($"{nameof(GetProjectPendingIssues)}: Found the following pending cards on '{projectName}':\r\n{string.Join("\r\n", cardList)}");
            }
            return result;

        }

        private async Task MoveCardToColumn(IConnection graph, ItemBase card, ID columnId)
        {
            this._logger.LogInformation($"{nameof(MoveCardToColumn)}: Moving '{card}' -> {columnId}");
            var mutation = new Mutation()
                .MoveProjectCard(new MoveProjectCardInput
                {
                    ClientMutationId = _graphFactory.ClientId,
                    AfterCardId = null,
                    CardId = card.CardId,
                    ColumnId = columnId
                })
                .CardEdge
                .Node
                .Select(c => c.Column.Id);
            var success = (await graph.Run(mutation)).Value != null;
            if (!success) throw new Exception($"Failed to move card '{card}'");
            this._logger.LogDebug($"{nameof(MoveCardToColumn)}: Card '{card}' moved successfully");
        }

        private abstract class ItemBase
        {
            public int Number { get; }
            public string Title { get; }
            public ID CardId { get; }

            private ItemBase(in int number, string title, ID cardId)
            {
                Number = number;
                Title = title;
                CardId = cardId;
            }

            #region Overrides of Object

            public override string ToString() => $"[{this.Number}] {this.Title}";

            #endregion

            public class IssueItem : ItemBase
            {
                public IssueItem(in int number, string title, ID cardId) : base(in number, title, cardId) { }
            }

            public class PullRequestItem: ItemBase
            {
                public PullRequestItem(in int number, string title, ID cardId) : base(in number, title, cardId) { }
            }
        }
    }
}
