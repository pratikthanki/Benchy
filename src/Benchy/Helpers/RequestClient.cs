using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Benchy.Configuration;
using Benchy.Models;
using Microsoft.Extensions.Logging;

namespace Benchy.Helpers
{
    public interface IRequestClient
    {
        ValueTask<RequestSummary> RecordRequestAsync(
            string url,
            Stage stage,
            Dictionary<string, string> headers,
            CancellationToken cancellationToken);
    }

    public class RequestClient : IRequestClient
    {
        private readonly ILogger<RequestClient> _logger;
        private readonly ITimeHandler _timeHandler;

        public RequestClient(ILogger<RequestClient> logger, ITimeHandler timeHandler)
        {
            _logger = logger;
            _timeHandler = timeHandler;
        }

        public async ValueTask<RequestSummary> RecordRequestAsync(
            string url,
            Stage stage,
            Dictionary<string, string> headers,
            CancellationToken cancellationToken)
        {
            var report = new RequestSummary
            {
                Id = Guid.NewGuid(), Stage = stage, Url = url, Start = DateTime.UtcNow
            };

            var httpRequestMessage = BuildHttpRequestMessage(url, headers);

            var client = new HttpClient();

            _timeHandler.Start();

            var response = await client.SendAsync(httpRequestMessage, cancellationToken);

            _timeHandler.Stop();
            report.End = DateTime.UtcNow;

            report.DurationMs = _timeHandler.ElapsedMilliseconds();
            report.StatusCode = response.StatusCode;

            _logger.LogInformation($"Request sent: {report}");

            return report;
        }

        private static HttpRequestMessage BuildHttpRequestMessage(string url, Dictionary<string, string> headers)
        {
            var requestMethod = HttpMethod.Get;
            var httpRequestMessage = new HttpRequestMessage
            {
                Method = requestMethod,
                RequestUri = new Uri(url)
            };

            foreach (var (key, value) in headers)
            {
                httpRequestMessage.Headers.Add(key, value);
            }

            return httpRequestMessage;
        }
    }
}