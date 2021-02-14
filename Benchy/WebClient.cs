using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Benchy
{
    public interface IWebClient
    {
        Task<HttpResponseMessage> GetAsync(string url, CancellationToken cancellationToken);
    }

    public class WebClient : IWebClient
    {
        private readonly HttpClient _httpClient;
        private readonly TimeSpan timeout = TimeSpan.FromSeconds(10);

        private readonly HttpClientHandler httpClientHandler = new()
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };

        public WebClient()
        {
            _httpClient = new HttpClient(httpClientHandler) {Timeout = timeout};
        }

        public async Task<HttpResponseMessage> GetAsync(string url, CancellationToken cancellationToken)
        {
            return await _httpClient.GetAsync(
                url, 
                HttpCompletionOption.ResponseHeadersRead, 
                cancellationToken);
        }
    }
}