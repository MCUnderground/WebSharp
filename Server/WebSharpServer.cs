using System.Net;
using System.Net.Sockets;
using System.Text;
using WebSharp.Models;
using HttpMethod = WebSharp.Enums.HttpMethod;
using HttpStatusCode = WebSharp.Enums.HttpStatusCode;

namespace WebSharp.Server
{
    public class WebSharpServer
    {
        private string _ip;
        private int _port;
        private string _rootDirectory;

        public WebSharpServer(string ip, int port, string rootDirectory)
        {
            _ip = ip;
            _port = port;
            _rootDirectory = rootDirectory;
        }

        public static async Task<WebSharpServer> CreateAsync(string ip, int port, string rootDirectory = "wwwroot")
        {
            var server = new WebSharpServer(ip, port, rootDirectory);
            await server.StartServer();
            return server;
        }

        private async Task StartServer()
        {
            TcpListener server = new TcpListener(IPAddress.Parse(_ip), _port);

            server.Start();
            Console.WriteLine($"Server has started on {_ip}:{_port}.{Environment.NewLine}Waiting for a connection...");

            while (true)
            {
                TcpClient client = await server.AcceptTcpClientAsync();

                if (client == null)
                {
                    Console.WriteLine("No client connected.");
                }
                else
                {
                    Console.WriteLine("A client connected.");
                    _ = Task.Run(() => HandleClient(client)); // Run a task to handle the client.
                }
            }
        }

        private async void HandleClient(TcpClient client)
        {
            NetworkStream stream = client.GetStream();

            while (true)
            {
                while (!stream.DataAvailable) ;

                byte[] bytes = new byte[client.Available];

                await stream.ReadAsync(bytes, 0, bytes.Length);

                string data = Encoding.UTF8.GetString(bytes);

                Console.WriteLine(data);

                // Parse the request data to get the requested path and method
                var requestLines = data.Split('\n');
                var requestLine = requestLines[0].Split(' ');
                var httpMethod = Enum.Parse<HttpMethod>(requestLine[0]);
                var path = requestLine[1].TrimEnd('/');

                HttpResponse? response;

                if (httpMethod == HttpMethod.GET)
                {
                    // Handle GET request
                    response = await HandleGetRequest(path);
                }
                else if (httpMethod == HttpMethod.POST)
                {
                    // Handle POST request
                    response = await HandlePostRequest(path, requestLines);
                }
                else
                {
                    // Unsupported method
                    response = CreateTextResponse(HttpStatusCode.MethodNotAllowed, "Method Not Allowed");
                }

                var responseBytes = GetBytesFromResponse(response);
                await stream.WriteAsync(responseBytes, 0, responseBytes.Length);

                client.Close();
                break;

                if (!data.Contains("Connection: keep-alive"))
                {
                    client.Close();
                    break;
                }
            }
        }

        private async Task<HttpResponse> HandlePostRequest(string path, string[] requestLines)
        {
            throw new NotImplementedException();
        }

        private async Task<HttpResponse> HandleGetRequest(string path)
        {
            if (string.IsNullOrEmpty(path) || path == "/")
            {
                // If no specific file is requested, serve index.html by default
                path = "/index.html";
            }

            // Read the requested file from the disk
            var filePath = $"{_rootDirectory}{path}";
            if (File.Exists(filePath))
            {
                var fileContent = await File.ReadAllTextAsync(filePath);
                var response = CreateHtmlResponse(HttpStatusCode.OK, fileContent);
                return response;
            }
            else
            {
                var response = CreateTextResponse(HttpStatusCode.NotFound, "404 Not Found");
                return response;
            }
        }

        public HttpResponse CreateJsonResponse(HttpStatusCode statusCode, string jsonBody)
        {
            return new HttpResponse
            {
                StatusCode = statusCode,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } },
                Body = jsonBody
            };
        }

        public HttpResponse CreateHtmlResponse(HttpStatusCode statusCode, string htmlBody)
        {
            return new HttpResponse
            {
                StatusCode = statusCode,
                Headers = new Dictionary<string, string> { { "Content-Type", "text/html" } },
                Body = htmlBody
            };
        }

        public HttpResponse CreateTextResponse(HttpStatusCode statusCode, string textBody)
        {
            return new HttpResponse
            {
                StatusCode = statusCode,
                Headers = new Dictionary<string, string> { { "Content-Type", "text/plain" } },
                Body = textBody
            };
        }

        public byte[] GetBytesFromResponse(HttpResponse response)
        {
            StringBuilder responseBuilder = new StringBuilder();

            // Add status line
            responseBuilder.Append($"HTTP/1.1 {(int)response.StatusCode} {response.StatusCode}\r\n");

            // Add headers
            foreach (var header in response.Headers)
            {
                responseBuilder.Append($"{header.Key}: {header.Value}\r\n");
            }

            // Add blank line to separate headers from body
            responseBuilder.Append("\r\n");

            // Add body
            responseBuilder.Append(response.Body);

            responseBuilder.Append("\r\n");

            var responseString = responseBuilder.ToString();

            Console.WriteLine($"Response: \r\n{responseString}");
            return Encoding.UTF8.GetBytes(responseString);
        }
    }
}