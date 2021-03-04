using System.Threading.Tasks;
using Benchy.Models;

namespace Benchy.Reporters
{
    public interface IReporter
    {
        Task Write(SummaryReport report);
    }
}