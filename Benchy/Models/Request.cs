using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net;
using System.Text;

namespace Benchy.Models
{
    // TODO: Next iteration to support non-GET requests maybe?
    public class Request
    {
        public string Url { get; set; }
        public string Method { get; set; }
        public string ContentType { get; set; } = "application/json";
        public string Query { get; set; }
        public string Body { get; set; }
        public Dictionary<string, string> Headers { get; set; }

        //TODO
        public HttpWebRequest BuildRequest()
        {
            var request = (HttpWebRequest) WebRequest.Create(Url);

            request.Method = Method;
            request.ContentType = ContentType;

            foreach (var Header in Headers)
            {
                request.Headers.Add(Header.Key, Header.Value);
            }

            if (!string.IsNullOrWhiteSpace(Body))
            {
                var data = Encoding.ASCII.GetBytes(Body);
                request.ContentLength = data.Length;

                using var stream = request.GetRequestStream();
                stream.Write(data, 0, data.Length);
            }

            return request;
        }
    }
}