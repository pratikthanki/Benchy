using System.Collections.Generic;
using System.Linq;
using Benchy.Models;

namespace Benchy.Helpers
{
    public interface ICalculationHandler
    {
        SummaryReport SummaryReport { get; set; }
        List<RequestReport> RequestReports { get; set; }
        void CreateSummary();
    }

    public class CalculationHandler : ICalculationHandler
    {
        public SummaryReport SummaryReport { get; set; }
        public List<RequestReport> RequestReports { get; set; }

        public CalculationHandler()
        {
            SummaryReport = new SummaryReport();
            RequestReports = new List<RequestReport>();
        }

        public void CreateSummary()
        {
            var requestsPerUrl = RequestReports
                .GroupBy(x => x.Url)
                .ToDictionary(x => x.Key, x => x.ToList());

            SummaryReport.StageSummary = requestsPerUrl.Select(x => Summarize(x.Value));
        }

        private static StageSummary Summarize(IList<RequestReport> requests)
        {
            var durations = requests.Select(x => x.DurationMs).OrderBy(_ => _).ToArray();

            return new StageSummary()
            {
                Url = requests.First().Url,
                Http2xx = requests.Count(x => x.RoundStatusCode() == 200),
                Http3xx = requests.Count(x => x.RoundStatusCode() == 300),
                Http4xx = requests.Count(x => x.RoundStatusCode() == 400),
                Http5xx = requests.Count(x => x.RoundStatusCode() == 500),
                Average = requests.Average(x => x.DurationMs),
                Minimum = requests.Min(x => x.DurationMs),
                Maximum = requests.Max(x => x.DurationMs),
                Percentile50 = CalculatePercentile(durations, 0.5),
                Percentile66 = CalculatePercentile(durations, 0.66),
                Percentile75 = CalculatePercentile(durations, 0.75),
                Percentile80 = CalculatePercentile(durations, 0.8),
                Percentile90 = CalculatePercentile(durations, 0.9),
                Percentile95 = CalculatePercentile(durations, 0.95),
                Percentile98 = CalculatePercentile(durations, 0.98),
                Percentile99 = CalculatePercentile(durations, 0.99),

                // TODO 
                Median = 0,
                StdDev = 0
            };
        }

        private static double CalculatePercentile(long[] durations, double percentile)
        {
            var realIndex = percentile * (durations.Length - 1);
            var index = (int) realIndex;
            var frac = realIndex - index;

            if (index + 1 < durations.Length)
            {
                return durations[index] * (1 - frac) + durations[index + 1] * frac;
            }

            return durations[index];
        }
    }
}