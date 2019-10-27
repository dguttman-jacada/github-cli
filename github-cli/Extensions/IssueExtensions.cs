using System.Linq;
using Octokit;

namespace github_cli.Extensions
{
    internal static class IssueExtensions
    {
        public static string Summary(this Issue issue)
        {
            return $"{issue.Number}[{issue.Milestone?.Title ?? "null"}]: {issue.Title}\r\n" +
                   $"    {string.Join(", ", issue.Labels.Select(l => l.Name))}";
        }

        public static IssueUpdate SetMilestone(this IssueUpdate update, Milestone milestone)
        {
            update.Milestone = milestone.Number;
            return update;
        }
    }
}
