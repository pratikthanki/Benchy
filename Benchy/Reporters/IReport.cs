using System.Collections.Generic;
using System.Threading.Tasks;

namespace Benchy.Reporters
{
    public interface IReport
    {
        Task<IList<T>> Read<T>(Configuration.Configuration configuration);

        Task<bool> Save<T>(IEnumerable<T> results, Configuration.Configuration configuration);
    }
}