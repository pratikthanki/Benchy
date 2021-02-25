using System.Collections.Generic;
using System.Threading.Tasks;
using Benchy.Models;

namespace Benchy.Reporters
{
    public class JsonReporter : IReporter
    {
        public Task<bool> Write(IEnumerable<SummaryReport> report)
        {
            throw new System.NotImplementedException();
        }
    }
}