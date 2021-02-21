using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Benchy.Helpers
{
    public interface IHttpService
    {
        Task<HttpResponseMessage> GetAsync(string url, CancellationToken cancellationToken);
    }

    public class HttpService : IHttpService
    {
        private readonly ILogger<HttpService> _logger;
        private readonly HttpClient _httpClient;
        private readonly TimeSpan timeout = TimeSpan.FromSeconds(10);

        private readonly HttpClientHandler httpClientHandler = new()
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };

        public HttpService(ILogger<HttpService> logger)
        {
            _logger = logger;
            _httpClient = new HttpClient(httpClientHandler) {Timeout = timeout};
        }

        public async Task<HttpResponseMessage> GetAsync(string url, CancellationToken cancellationToken)
        {
            var request = _httpClient.GetAsync(
                url,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            return await request;
        }
    }
}