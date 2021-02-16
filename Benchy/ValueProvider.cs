using System;
using Microsoft.Extensions.Options;

namespace Benchy
{
    public interface IValueProvider
    {
        int GetNextInt();
    }
    
    public class ValueProvider : IValueProvider
    {
        private readonly Random _random;
        private readonly Configuration _configuration;

        public ValueProvider(IOptions<Configuration> configuration)
        {
            _configuration = configuration.Value;
            _random = new Random(_configuration.RandomSeed);
        }

        public int GetNextInt()
        {
            return _random.Next(0, _configuration.Urls.Length);
        }
    }
}