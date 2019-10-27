using System;
using System.Linq;
using System.Threading.Tasks;
using Octokit;

namespace github_cli.Model
{
    internal class CardWrapper
    {
        private readonly Uri _uri;

        public CardWrapper(ProjectColumn column, ProjectCard card)
        {
            Column = column;
            Card = card;
            _uri = new Uri(card.ContentUrl);
        }

        public ProjectColumn Column { get; }
        public ProjectCard Card { get; }
        public Issue? Issue { get; private set; }

        public string Owner => _uri.Segments[2].TrimEnd('/');
        public string Repo => _uri.Segments[3].TrimEnd('/');
        public string Number => _uri.Segments[5].TrimEnd('/');
        public string Lane => Column.Name;
        public string? Title => this.Issue?.Title;
        public string? Milestone => this.Issue?.Milestone?.Title;

        public async Task LoadIssue(IIssuesClient client)
        {
            var issue = await client.Get(this.Owner, this.Repo, int.Parse(this.Number));
            this.Issue = issue;
        }

        public string Summary()
        {
            return $"{this.Number}[{this.Lane ?? "Lane?"}]: {this.Title ?? "Title?"}\r\n" +
                   $"    @{this.Milestone ?? "Milestone?"} {string.Join(", ", this.Issue?.Labels.Select(l => l.Name) ?? new string[0])}";
        }
    }
}
