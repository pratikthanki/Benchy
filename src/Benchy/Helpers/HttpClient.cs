using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Benchy.Models;
using Microsoft.Extensions.Logging;

namespace Benchy.Helpers
{
    public interface IHttpClient
    {
        Task<RequestSummary> RecordRequestAsync(
            string url,
            int stageId,
            CancellationToken cancellationToken);
    }

    public class HttpClient : IHttpClient
    {
        private readonly ILogger<HttpClient> _logger;
        private readonly ITimeHandler _timeHandler;
        private readonly System.Net.Http.HttpClient _httpClient;
        private readonly TimeSpan timeout = TimeSpan.FromSeconds(10);

        private readonly HttpClientHandler httpClientHandler = new()
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };

        public HttpClient(ILogger<HttpClient> logger, ITimeHandler timeHandler)
        {
            _logger = logger;
            _timeHandler = timeHandler;
            _httpClient = new System.Net.Http.HttpClient(httpClientHandler) {Timeout = timeout};
        }

        public async Task<RequestSummary> RecordRequestAsync(
            string url,
            int stageId,
            CancellationToken cancellationToken)
        {
            var report = new RequestSummary
            {
                Id = Guid.NewGuid(), StageId = stageId, Url = url, Start = DateTime.UtcNow
            };

            _timeHandler.Start();

            var request = await GetAsync(url, cancellationToken);

            _timeHandler.Stop();
            report.End = DateTime.UtcNow;

            report.DurationMs = _timeHandler.ElapsedMilliseconds();
            report.StatusCode = request.StatusCode;
            
            _logger.LogInformation($"Request sent: {report}");

            return report;
        }

        private async Task<HttpResponseMessage> GetAsync(string url, CancellationToken cancellationToken)
        {
            return await _httpClient.GetAsync(
                url,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
        }
    }
}