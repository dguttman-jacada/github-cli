using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration.Attributes;

namespace github_cli.Model
{
    internal class ProjectCardRecord
    {
        private static readonly string[] DefaultLabels = new string[0];
        private readonly CardWrapper _card;

        public ProjectCardRecord(CardWrapper card) => _card = card;

        [Index(0)] public string Id => _card.Number;
        [Index(1)] public string Status => _card.Lane;
        [Index(2)] public string Milestone => _card.Milestone ?? "";
        [Index(3)] public string Description => _card.Title ?? "";
        [Index(4)] public string Url => _card.Issue?.HtmlUrl ?? "";
        [Index(5)] public string[] Labels => _card.Issue?.Labels.Select(l => l.Name).ToArray() ?? DefaultLabels;

        public static void Write(OutputConfig config, IEnumerable<CardWrapper> cards)
        {
            if (File.Exists(config.File) && config.Overwrite != true) { throw new InvalidOperationException("File already exists");}
            using var writer = new StreamWriter(config.File!, false);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csv.WriteRecords(cards.Select(c => new ProjectCardRecord(c)));
        }
    }
}
