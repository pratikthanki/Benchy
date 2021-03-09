using System;
using System.Collections.Generic;
using System.Linq;

namespace Benchy.Extensions
{
    public static class StatisticsExtensions
    {
        public static double CalculatePercentile(this IReadOnlyList<long> durations, double percentile)
        {
            var realIndex = percentile * (durations.Count - 1);
            var index = (int) realIndex;
            var frac = realIndex - index;

            if (index + 1 < durations.Count)
            {
                return durations[index] * (1 - frac) + durations[index + 1] * frac;
            }

            return Math.Round((double) durations[index], 3);
        }

        public static double CalculateStandardDeviation(this IReadOnlyCollection<long> durations)
        {
            var avg = durations.Average();
            var sum = durations.Sum(d => Math.Pow(d - avg, 2));

            var stdDev = Math.Sqrt(sum / (durations.Count - 1));

            return Math.Round(stdDev, 3);
        }
    }
}