using System;
using System.Collections.Generic;
using System.Linq;
using Benchy.Models;

namespace Benchy.Services
{
    public class CalculationService
    {
        public CalculationService()
        {
            
        }

        private double CalculatePercentile(IList<RequestReport> responses, double percentile)
        {
            var durations = responses.Select(x => x.DurationMs).ToList();
            var n = (int) Math.Round((durations.Count * percentile) + 0.5, 0);

            return durations[n - 1];
        }

        private int RoundStatusCodeDown(int statusCode)
        {
            return (int) Math.Floor(statusCode / 100.0) * 100;
        }
    }
}