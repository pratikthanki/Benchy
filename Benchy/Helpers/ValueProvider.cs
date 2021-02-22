using System;
using Microsoft.Extensions.Options;

namespace Benchy.Helpers
{
    public interface IValueProvider
    {
        string GetRandomUrl();
        int GetRandomUserCount(int concurrentUsers);
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

        public string GetRandomUrl()
        {
            return _configuration.Urls[GetRandomInt(_configuration.Urls.Length)];
        }

        public int GetRandomUserCount(int concurrentUsers)
        {
            return GetRandomInt(concurrentUsers) + 1;
        }

        private int GetRandomInt(int upperRange)
        {
            return _random.Next(0, upperRange);
        }
    }
}