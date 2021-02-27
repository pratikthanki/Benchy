using System;
using System.Collections.Generic;
using Benchy.Configuration;

namespace Benchy.Models
{
    public class SummaryReport
    {
        public DateTimeOffset TestStart { get; set; }
        public DateTimeOffset TestEnd { get; set; }
        public TaskStatus Status { get; set; }
        public IEnumerable<StageSummary> StageSummary { get; set; }
    }

    public class StageSummary : Stage
    {
        public int StageId { get; set; }
        public string Url { get; set; }
        public int Http2xx { get; set; }
        public int Http3xx { get; set; }
        public int Http4xx { get; set; }
        public int Http5xx { get; set; }
        public double Average { get; set; }
        public double Minimum { get; set; }
        public double Maximum { get; set; }
        public double Median { get; set; }
        public double StdDev { get; set; }
        public double Percentile50 { get; set; }
        public double Percentile66 { get; set; }
        public double Percentile75 { get; set; }
        public double Percentile80 { get; set; }
        public double Percentile90 { get; set; }
        public double Percentile95 { get; set; }
        public double Percentile98 { get; set; }
        public double Percentile99 { get; set; }
    }
}