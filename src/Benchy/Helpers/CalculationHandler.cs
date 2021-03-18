using System;
using System.Collections.Generic;
using System.Linq;
using Benchy.Extensions;
using Benchy.Models;

namespace Benchy.Helpers
{
    public interface ICalculationHandler
    {
        void LogTestStart();
        void LogTestEnd();
        void AddRequestReport(RequestSummary requestSummary);
        void SetStatus(TaskStatus taskStatus);
        SummaryReport GetSummaryReport();
    }

    public class CalculationHandler : ICalculationHandler
    {
        private readonly SummaryReport SummaryReport;
        private readonly List<RequestSummary> RequestReports;

        public CalculationHandler()
        {
            SummaryReport = new SummaryReport();
            RequestReports = new List<RequestSummary>();
        }

        public void LogTestStart() => SummaryReport.TestStart = DateTimeOffset.UtcNow;
        public void LogTestEnd() => SummaryReport.TestEnd = DateTimeOffset.UtcNow;
        public void AddRequestReport(RequestSummary requestSummary) => RequestReports.Add(requestSummary);
        public void SetStatus(TaskStatus taskStatus) => SummaryReport.Status = taskStatus;

        public SummaryReport GetSummaryReport()
        {
            SummaryReport.StageSummary = RequestReports
                .GroupBy(x => (x.Url, x.Stage))
                .ToDictionary(x => x.Key, x => x.ToList())
                .Select(x => Summarize(x.Value));

            return SummaryReport;
        }

        private static StageSummary Summarize(IList<RequestSummary> requests)
        {
            var durations = requests.Select(x => x.DurationMs).OrderBy(_ => _).ToArray();

            var statusCodes = requests
                .GroupBy(x => x.RoundStatusCode())
                .ToDictionary(x => x.Key, x => x.ToList().Count);

            return new StageSummary
            {
                Stage = requests.First().Stage,
                Url = requests.First().Url,
                Http2xx = statusCodes.TryGetValue(200, out var count2xx) ? count2xx : 0,
                Http3xx = statusCodes.TryGetValue(300, out var count3xx) ? count3xx : 0,
                Http4xx = statusCodes.TryGetValue(400, out var count4xx) ? count4xx : 0,
                Http5xx = statusCodes.TryGetValue(500, out var count5xx) ? count5xx : 0,
                Average = Math.Round(requests.Average(x => x.DurationMs), 2),
                Minimum = requests.Min(x => x.DurationMs),
                Maximum = requests.Max(x => x.DurationMs),
                StdDev = durations.CalculateStandardDeviation(),
                Percentile50 = durations.CalculatePercentile(0.5),
                Percentile66 = durations.CalculatePercentile(0.66),
                Percentile75 = durations.CalculatePercentile(0.75),
                Percentile80 = durations.CalculatePercentile(0.8),
                Percentile90 = durations.CalculatePercentile(0.9),
                Percentile95 = durations.CalculatePercentile(0.95),
                Percentile98 = durations.CalculatePercentile(0.98),
                Percentile99 = durations.CalculatePercentile(0.99)
            };
        }
    }
}