using System.Collections.Generic;
using System.Threading.Tasks;

namespace Benchy.Reporters
{
    public class CsvReport : IReport
    {
        // TODO
        public Task<IList<T>> Read<T>(Configuration.Configuration configuration)
        {
            throw new System.NotImplementedException();
        }

        // TODO
        public Task<bool> Save<T>(IEnumerable<T> results, Configuration.Configuration configuration)
        {
            throw new System.NotImplementedException();
        }
    }
}