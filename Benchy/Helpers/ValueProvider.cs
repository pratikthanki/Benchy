using System;
using Microsoft.Extensions.Options;

namespace Benchy.Helpers
{
    public interface IValueProvider
    {
        int GetRandomInt(int upperRange);
    }

    public class ValueProvider : IValueProvider
    {
        private readonly Random _random;
        private readonly Configuration.Configuration _configuration;

        public ValueProvider(IOptions<Configuration.Configuration> configuration)
        {
            _configuration = configuration.Value;
            _random = new Random(_configuration.RandomSeed);
        }

        public int GetRandomInt(int upperRange)
        {
            return _random.Next(0, upperRange);
        }
    }
}