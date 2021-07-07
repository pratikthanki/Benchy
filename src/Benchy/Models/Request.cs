using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace Benchy.Models
{
    public class Request
    {
        public string Url { get; set; }
        public Dictionary<string, string> Headers { get; set; }

        /// <summary>
        /// Create a request based on fields provided in configuration
        /// </summary>
        /// <returns>HttpWebRequest</returns>
        public HttpRequestMessage BuildRequest()
        {
            var request = new HttpRequestMessage()
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(Url)
            };

            foreach (var (header, value) in Headers)
            {
                request.Headers.Add(header, value);
            }

            return request;
        }

        public override string ToString()
        {
            static string AddQuotes(string str) => $"\"{str}\"";

            var headersAsString = Headers.Select(x => $"{AddQuotes(x.Key)}: {AddQuotes(x.Value)}");
            return $"{{Url: {AddQuotes(Url)}, Headers: [{string.Join(",", headersAsString)}]}}";
        }
    }
}