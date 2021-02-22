using System;
using System.Net;

namespace Benchy.Models
{
    public class RequestReport
    {
        public Guid Id { get; set; }
        public string Url { get; set; }
        private HttpStatusCode StatusCode { get; set; }
        public double DurationMs { get; set; }
    }
}