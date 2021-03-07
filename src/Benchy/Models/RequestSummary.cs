using System;
using System.Net;

namespace Benchy.Models
{
    public class RequestSummary
    {
        public Guid Id { get; set; }
        public int StageId { get; set; }
        public string Url { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public long DurationMs { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }

        public int RoundStatusCode()
        {
            return (int) Math.Floor((int) StatusCode / 100.0) * 100;
        }

        public override string ToString()
        {
            return $"Id: {Id}; Url: {Url}";
        }
    }
}