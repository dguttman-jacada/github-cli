using System;
using System.Collections.Generic;
using System.Text;

namespace github_cli.Model
{
    internal class GitHubRepo
    {
        public GitHubRepo(string projectName)
        {
            var split = projectName.Split("/");
            this.Owner = split[0];
            this.Name = split[1];
        }

        public string Owner { get; }
        public string Name { get; }
    }
}
