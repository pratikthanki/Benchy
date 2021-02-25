using System.Collections.Generic;
using System.Threading.Tasks;
using Benchy.Models;

namespace Benchy.Reporters
{
    public interface IReporter
    {
        Task<bool> Write(IEnumerable<SummaryReport> report);
    }
}