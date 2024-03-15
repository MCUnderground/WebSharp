using WebSharp.Enums;

namespace WebSharp.Models
{
    public class HttpResponse
    {
        public HttpStatusCode StatusCode { get; set; }
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
        public string Body { get; set; } = string.Empty;

        public bool HasStarted { get; set; }
    }
}