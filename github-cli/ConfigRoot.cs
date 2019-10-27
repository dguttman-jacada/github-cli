using System;
using Microsoft.Extensions.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;

namespace github_cli
{
    internal class ConfigRoot
    {
        public GitHubConfig? GitHub { get; set; }

        public static IConfigurationRoot Build(IConfigurationBuilder builder, string[] args)
        {
            builder
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
                .AddCommandLine(args);

            var config = builder.Build();
            return config;
        }
    }

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    internal class GitHubConfig
    {
        public string? User { get; set; }
        public string? Token { get; set; }
        public string[]? Projects { get; set; }

        public override string ToString() => $"{nameof(GitHubConfig)}: {this.User} {new string('*', this.Token?.Length ?? 0)}\r\n" +
                                             $"Projects: {string.Join("\r\n", this.Projects ?? new string[0])}";
    }
}
