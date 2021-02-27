using System;
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
                await System.IO.File.WriteAllTextAsync(@"data.json", json);
            }
            catch (Exception)
            {
                // do nothing
            }
        }
    }
}