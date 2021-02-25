using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Benchy.Helpers;
using Benchy.Models;
using Microsoft.Extensions.Logging;

namespace Benchy.Services
{
    public interface IHttpService
    {
        Task<RequestReport> RecordRequestAsync(string url, CancellationToken cancellationToken);
    }

    public class HttpService : IHttpService
    {
        private readonly ILogger<HttpService> _logger;
        private readonly ITimeHandler _timeHandler;
        private readonly HttpClient _httpClient;
        private readonly TimeSpan timeout = TimeSpan.FromSeconds(10);

        private readonly HttpClientHandler httpClientHandler = new()
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };

        public HttpService(ILogger<HttpService> logger, ITimeHandler timeHandler)
        {
            _logger = logger;
            _timeHandler = timeHandler;
            _httpClient = new HttpClient(httpClientHandler) {Timeout = timeout};
        }

        public async Task<RequestReport> RecordRequestAsync(string url, CancellationToken cancellationToken)
        {
            var report = new RequestReport()
            {
                Id = Guid.NewGuid(),
                Url = url,
                Start = DateTime.UtcNow
            };

            _timeHandler.Start();

            var request = await GetAsync(url, cancellationToken);

            _timeHandler.Stop();

            report.DurationMs = _timeHandler.ElapsedMilliseconds();
            report.StatusCode = request.StatusCode;
            report.End = DateTime.UtcNow;

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