using System;
using Microsoft.Extensions.Options;

namespace Benchy.Helpers
{
    public interface IValueProvider
    {
        int GetRandomInt();
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

        public int GetRandomInt()
        {
            return _random.Next(0, _configuration.Urls.Length);
        }
    }
}