using System.Collections.Generic;
using Benchy.Models;

namespace Benchy.Configuration
{
    public class Configuration
    {
        /// <summary>
        /// Set the mode for the service to run as
        /// </summary>
        public Mode Mode { get; set; }

        /// <summary>
        /// Set the output report type, options are: Json and Csv
        /// </summary>
        public ReportType ReportType { get; set; }

        /// <summary>
        /// List of urls 
        /// </summary>
        public string[] Urls { get; set; }

        /// <summary>
        /// Delay between starting each stage
        /// </summary>
        public int SecondsDelayBetweenStages { get; set; }

        /// <summary>
        /// Display results at each stage 
        /// </summary>
        public bool OutputStageSummary { get; set; } = false;

        /// <summary>
        /// Json file of summary statistics for each stage
        /// </summary>
        public bool OutputSummaryReport { get; set; } = true;

        /// <summary>
        /// List of stages
        /// </summary>
        public List<Stage> Stages { get; set; }

        /// <summary>
        /// Set seed to create reproducible test runs
        /// </summary>
        public int RandomSeed { get; set; }
        
        /// <summary>
        /// Whether to print to the console
        /// </summary>
        public bool ConsoleLog { get; set; }
        
        /// <summary>
        /// Set of headers to pass with the query
        /// </summary>
        public Dictionary<string, string> Headers { get; set; }
    }

    public class Stage
    {
        /// <summary>
        /// Total target requests 
        /// </summary>
        public int Requests { get; set; }

        /// <summary>
        /// Concurrent (user) requests to make 
        /// </summary>
        public int VirtualUsers { get; set; }

        public override string ToString()
        {
            return $"Requests: {Requests}; VirtualUsers: {VirtualUsers}";
        }
    }
}