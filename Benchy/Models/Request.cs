namespace Benchy.Models
{
    // TODO: Next iteration to support non-GET requests maybe?
    public class Request
    {
        public string Url { get; set; }
        public string Method { get; set; }
        public string Query { get; set; }
        public string Body { get; set; }
        public string Headers { get; set; }
    }
}