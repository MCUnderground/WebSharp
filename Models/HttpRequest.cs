using HttpMethod = WebSharp.Enums.HttpMethod;

namespace WebSharp.Models
{
    public class HttpRequest
    {
        public HttpMethod Method { get; set; }
        public string Url { get; set; } = string.Empty;
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
        public string Body { get; set; } = string.Empty;
    }
}