﻿namespace WebSharp.Models
{
    public class HttpRequest
    {
        public HttpMethod Method { get; set; }
        public string Url { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public string Body { get; set; }
    }
}