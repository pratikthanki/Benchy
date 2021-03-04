using System;
using System.Collections.Generic;
using System.Linq;
using Benchy.Models;

namespace Benchy.Helpers
{
    public interface ICalculationHandler
    {
        SummaryReport SummaryReport { get; }
        void CreateSummary();
        void LogStart();
        void LogEnd();
        void AddRequestReport(RequestReport requestReport);
    }

    public class CalculationHandler : ICalculationHandler
    {
        public SummaryReport SummaryReport { get; private set; }
        private readonly List<RequestReport> RequestReports;

        public CalculationHandler()
        {
            SummaryReport = new SummaryReport();
            RequestReports = new List<RequestReport>();
        }

        public void CreateSummary()
        {
            SummaryReport.StageSummary = RequestReports
                .GroupBy(x => (x.Url, x.StageId))
                .ToDictionary(x => x.Key, x => x.ToList())
                .Select(x => Summarize(x.Value));
        }

        public void LogStart() => SummaryReport.TestStart = DateTimeOffset.UtcNow;
        public void LogEnd() => SummaryReport.TestEnd = DateTimeOffset.UtcNow;
        public void AddRequestReport(RequestReport requestReport) => RequestReports.Add(requestReport);

        private static StageSummary Summarize(IList<RequestReport> requests)
        {
            var durations = requests.Select(x => x.DurationMs).OrderBy(_ => _).ToArray();
            var statusCodes = requests
                .GroupBy(x => x.RoundStatusCode())
                .ToDictionary(x => x.Key, x => x.ToList().Count);

            var count = 0;
            return new StageSummary()
            {
                StageId = requests.First().StageId,
                Url = requests.First().Url,
                Http2xx = statusCodes.TryGetValue(200, out count) ? count : 0,
                Http3xx = statusCodes.TryGetValue(300, out count) ? count : 0,
                Http4xx = statusCodes.TryGetValue(400, out count) ? count : 0,
                Http5xx = statusCodes.TryGetValue(500, out count) ? count : 0,
                Average = Math.Round(requests.Average(x => x.DurationMs), 2),
                Minimum = requests.Min(x => x.DurationMs),
                Maximum = requests.Max(x => x.DurationMs),
                StdDev = CalculateStandardDeviation(durations),
                Percentile50 = CalculatePercentile(durations, 0.5),
                Percentile66 = CalculatePercentile(durations, 0.66),
                Percentile75 = CalculatePercentile(durations, 0.75),
                Percentile80 = CalculatePercentile(durations, 0.8),
                Percentile90 = CalculatePercentile(durations, 0.9),
                Percentile95 = CalculatePercentile(durations, 0.95),
                Percentile98 = CalculatePercentile(durations, 0.98),
                Percentile99 = CalculatePercentile(durations, 0.99)
            };
        }

        private static double CalculatePercentile(IReadOnlyList<long> durations, double percentile)
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

        private static double CalculateStandardDeviation(IReadOnlyCollection<long> durations)
        {
            var avg = durations.Average();
            var sum = durations.Sum(d => Math.Pow(d - avg, 2));

            var stdDev = Math.Sqrt(sum / (durations.Count - 1));

            return Math.Round(stdDev, 3);
        }
    }
}