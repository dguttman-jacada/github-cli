using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using Microsoft.Extensions.Logging;

namespace github_cli.Workloads
{
    internal class CsvFactory
    {
        private readonly ILogger<CsvFactory> _logger;

        public CsvFactory(ILogger<CsvFactory> logger)
        {
            _logger = logger;
        }

        public async Task Write<T>(OutputConfig config, IEnumerable<T> items)
        {
            if (File.Exists(config.File) && config.Overwrite != true) { throw new InvalidOperationException("File already exists"); }
         
            _logger.LogInformation($"{nameof(Write)}: Opening writing stream '{config.File}'");
            await using var writer = new StreamWriter(config.File!, false);
            using var csv = new CsvWriter(writer);

            var outputType = config.ModelType;
            _logger.LogInformation($"{nameof(Write)}: Converting '{typeof(T).Name}' --> '{outputType.Name}'");

            var converted = items.Select(i =>
            {
                var c = Activator.CreateInstance(outputType, i);
                return c;
            });

            _logger.LogInformation($"{nameof(Write)}: Writing...");
            csv.WriteRecords(converted);
            _logger.LogInformation($"{nameof(Write)}: Done.");

            await csv.FlushAsync();
        }
    }
}
