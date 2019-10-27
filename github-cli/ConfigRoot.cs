using System;
using Microsoft.Extensions.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using github_cli.Model;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.Hosting;
using Octokit;

namespace github_cli
{
    internal class ConfigRoot
    {
        public GitHubConfig? GitHub { get; set; }
        public OutputConfig[]? Outputs { get; set; }
    }

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    internal class GitHubConfig
    {
        public string? User { get; set; }
        public string? Token { get; set; }
        public string[]? Repos { get; set; }
        public string[]? Projects { get; set; }
        public ColumnConfig? Columns { get; set; }
        public string? DefaultMilestone { get; set; }

        internal GitHubRepo[]? Repositories => this.Repos.Select(p => new GitHubRepo(p)).ToArray();

        public override string ToString() => $"{nameof(GitHubConfig)}:\r\n" +
                                             $"          User: {this.User}\r\n" +
                                             $"      Password: {new string('*', this.Token?.Length ?? 0)}\r\n" +
                                              "  Repositories:\r\n" +
                                             $"{string.Join("\r\n", (this.Repos ?? new string[0]).Select(r => $"    - {r}"))}";
    }

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    internal class ColumnConfig
    {
        public string[]? Ignore { get; set; }
        public string[]? Select { get; set; }

        internal bool Enabled(ProjectColumn column)
        {
            if (Ignore?.Any(i => i == column.Name) == true) { return false; }
            return Select is null || (Select.Length == 1 && Select[0] == "*") || Select.Any(i => i == column.Name);
        }
    }

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    internal class OutputConfig
    {
        public string? Workload { get; set; }
        public string? File { get; set; }
        public bool? Overwrite { get; set; }
        public string? Model { get; set; }

        internal Type ModelType => Type.GetType(this.Model ?? throw new ArgumentException("Missing model name")) ?? throw new ArgumentException("Invalid model name");

        internal void Verify()
        {
            if (Workload is null || File is null || Model is null || ModelType is null)
            {
                throw new ArgumentException("Invalid workflow output configuration");
            }
        }
    }

    internal static class ConfigRootHelper
    {
        public static IHostBuilder UseConfigRoot(this IHostBuilder hostBuilder) => hostBuilder.ConfigureHostConfiguration(Configure);

        private static void Configure(IConfigurationBuilder configBuilder)
        {
            configBuilder
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile($"{AppDomain.CurrentDomain.BaseDirectory}\\config.json", optional: false)
                .AddJsonFile(source =>
                {
                    source.Path = ".config.local.json";
                    source.Optional = true;
                    source.ReloadOnChange = false;
                    source.FileProvider = new PhysicalFileProvider(Directory.GetCurrentDirectory(), ExclusionFilters.None);
                })
                .AddEnvironmentVariables()
                .AddCommandLine(Environment.GetCommandLineArgs());
        }
    }
}
