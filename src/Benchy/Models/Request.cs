using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Benchy.Models
{
    public class Request
    {
        public string Url { get; set; }
        public string Method { get; set; }
        public string ContentType { get; set; } = "application/json";
        public string Body { get; set; }
        public Dictionary<string, string> Headers { get; set; }

        /// <summary>
        /// Create a request based on fields provided in configuration
        /// </summary>
        /// <returns>HttpWebRequest</returns>
        public HttpWebRequest BuildRequest()
        {
            var request = (HttpWebRequest) WebRequest.Create(Url);

            request.Method = Method;
            request.ContentType = ContentType;

            foreach (var (header, value) in Headers)
            {
                request.Headers.Add(header, value);
            }

            if (string.IsNullOrWhiteSpace(Body) || Method != "Get")
                return request;

            var data = Encoding.ASCII.GetBytes(Body);
            request.ContentLength = data.Length;

            using var stream = request.GetRequestStream();
            stream.Write(data, 0, data.Length);

            return request;
        }
    }
}