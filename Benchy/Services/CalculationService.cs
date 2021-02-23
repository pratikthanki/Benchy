using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Benchy.Models;

namespace Benchy.Services
{
    public interface ICalculationService
    {
        SummaryReport SummaryReport { get; set; }
        List<RequestReport> RequestReports { get; set; }
        void CreateSummary();
    }

    public class CalculationService : ICalculationService
    {
        public SummaryReport SummaryReport { get; set; }
        public List<RequestReport> RequestReports { get; set; }

        public CalculationService()
        {
            SummaryReport = new SummaryReport();
            RequestReports = new List<RequestReport>();
        }

        public void CreateSummary()
        {
            var requestsPerUrl = RequestReports
                .GroupBy(x => x.Url)
                .ToDictionary(x => x.Key, x => Summarize(x.ToList()));

            SummaryReport.RequestSummary = requestsPerUrl.Select(x => x.Value);
        }

        private StageSummary Summarize(List<RequestReport> requests)
        {
            return new StageSummary()
            {
                Url = requests.First().Url,
                Http2xx = requests.Count(x => RoundStatusCodeDown(x.StatusCode) == 200),
                Http3xx = requests.Count(x => RoundStatusCodeDown(x.StatusCode) == 300),
                Http4xx = requests.Count(x => RoundStatusCodeDown(x.StatusCode) == 400),
                Http5xx = requests.Count(x => RoundStatusCodeDown(x.StatusCode) == 500),
                Average = requests.Average(x => x.DurationMs),
                Minimum = requests.Min(x => x.DurationMs),
                Maximum = requests.Max(x => x.DurationMs),
                Median = 0,
                StdDev = 0,
                Percentile50 = CalculatePercentile(requests, 0.5),
                Percentile66 = CalculatePercentile(requests, 0.66),
                Percentile75 = CalculatePercentile(requests, 0.75),
                Percentile80 = CalculatePercentile(requests, 0.8),
                Percentile90 = CalculatePercentile(requests, 0.9),
                Percentile95 = CalculatePercentile(requests, 0.95),
                Percentile98 = CalculatePercentile(requests, 0.98),
                Percentile99 = CalculatePercentile(requests, 0.99)
            };
        }

        private double CalculatePercentile(IList<RequestReport> responses, double percentile)
        {
            var durations = responses.OrderBy(x => x.DurationMs).Select(x => x.DurationMs).ToList();
            var n = (int) Math.Round(durations.Count * percentile + 0.5, 0);

            return durations[n - 1];
        }

        private int RoundStatusCodeDown(HttpStatusCode statusCode)
        {
            return (int) Math.Floor((int) statusCode / 100.0) * 100;
        }
    }
}