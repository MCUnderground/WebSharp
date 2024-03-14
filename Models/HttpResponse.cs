using WebSharp.Enums;

namespace WebSharp.Models
{
    public class HttpResponse
    {
        public HttpStatusCode StatusCode { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public string Body { get; set; }
    }
}