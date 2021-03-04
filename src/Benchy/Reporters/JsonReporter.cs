using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Benchy.Models;
using Newtonsoft.Json;

namespace Benchy.Reporters
{
    public class JsonReporter : IReporter
    {
        public async Task Write(SummaryReport report)
        {
            var json = JsonConvert.SerializeObject(report);

            try
            {
                var path = Path.Combine(GetAbsolutePath(), "data.json");
                await File.WriteAllTextAsync(path, json);
            }
            catch (Exception)
            {
                // do nothing
            }
        }

        private static string GetAbsolutePath()
        {
            const string relativePath = @"../../..";

            var _dataRoot = new FileInfo(typeof(Program).Assembly.Location);

            Debug.Assert(_dataRoot.Directory != null);
            var assemblyFolderPath = _dataRoot.Directory.FullName;

            var fullPath = Path.Combine(assemblyFolderPath, relativePath);

            return fullPath;
        }
    }
}