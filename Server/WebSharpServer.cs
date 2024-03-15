using System.Net;
using System.Net.Sockets;
using System.Text;
using WebSharp.Models;
using HttpMethod = WebSharp.Enums.HttpMethod;
using HttpStatusCode = WebSharp.Enums.HttpStatusCode;

namespace WebSharp.Server
{
    public delegate Task Middleware(HttpRequest request, HttpResponse response, Func<Task> next);
    public delegate Task RequestHandler(HttpRequest request, HttpResponse response);

    public class WebSharpServer
    {
        private readonly string _ip;
        private readonly int _port;
        private readonly string _rootDirectory;

        private readonly List<Middleware> _middleware = new List<Middleware>();
        private readonly Dictionary<string, RequestHandler> _routes = new Dictionary<string, RequestHandler>();

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

        public void Use(Middleware middleware)
        {
            _middleware.Add(middleware);
        }

        public void Route(string path, RequestHandler handler)
        {
            _routes[path] = handler;
        }

        private async Task StartServer()
        {
            TcpListener server = new TcpListener(IPAddress.Parse(_ip), _port);

            Use(async (request, response, next) =>
            {
                Console.WriteLine($"Received {request.Method} request for {request.Url}");
                await next();
            });

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
                    Console.WriteLine(client.Client.RemoteEndPoint?.ToString() + " connected.");
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

                Memory<byte> memoryBytes = new Memory<byte>(bytes);
                await stream.ReadAsync(memoryBytes);

                string data = Encoding.UTF8.GetString(bytes);

                Console.WriteLine(data);

                // Parse the request data to get the requested path and method
                var requestLines = data.Split('\n');
                var requestLine = requestLines[0].Split(' ');
                var httpMethod = Enum.Parse<HttpMethod>(requestLine[0]);
                var path = requestLine[1].TrimEnd('/');



                HttpResponse? response = new HttpResponse();

                var request = new HttpRequest { Method = httpMethod, Url = path };

                // Run middleware
                await RunMiddleware(request, response);

                if (string.IsNullOrEmpty(response.Body))
                {
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
                    else if (httpMethod == HttpMethod.PUT)
                    {
                        // Handle PUT request
                        response = await HandlePutRequest(path, requestLines);
                    }
                    else if (httpMethod == HttpMethod.DELETE)
                    {
                        // Handle DELETE request
                        response = await HandleDeleteRequest(path);
                    }
                    else
                    {
                        // Unsupported method
                        response = CreateTextResponse(HttpStatusCode.MethodNotAllowed, "Method Not Allowed");
                    }
                }


                // Handle request based on route
                await HandleRequest(request, response);

                var responseBytes = GetBytesFromResponse(response);
                await stream.WriteAsync(new ReadOnlyMemory<byte>(responseBytes));



                if (!data.Contains("Connection: keep-alive"))
                {
                    client.Close();
                    break;
                }
            }
        }

        private async Task RunMiddleware(HttpRequest request, HttpResponse response)
        {
            int index = -1;

            async Task Next()
            {
                if (++index < _middleware.Count)
                {
                    await _middleware[index](request, response, Next);
                }
            }

            await Next();
        }

        private async Task HandleRequest(HttpRequest request, HttpResponse response)
        {
            if (_routes.TryGetValue(request.Url, out var handler))
            {
                await handler(request, response);
            }
            else
            {
                response = await HandleGetRequest(request.Url);
            }
        }



        private Task<HttpResponse?> HandleDeleteRequest(string path)
        {
            throw new NotImplementedException();
        }

        private Task<HttpResponse?> HandlePutRequest(string path, string[] requestLines)
        {
            throw new NotImplementedException();
        }

        private Task<HttpResponse> HandlePostRequest(string path, string[] requestLines)
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


        public HttpResponse CreateHtmlResponse(HttpStatusCode statusCode, string htmlBody)
        {
            var responseBodyBytes = Encoding.UTF8.GetBytes(htmlBody);
            return new HttpResponse
            {
                StatusCode = statusCode,
                Headers = new Dictionary<string, string>
                {
                    { "Content-Type", "text/html" },
                    { "Content-Length", responseBodyBytes.Length.ToString() }
                },
                Body = htmlBody
            };
        }

        public HttpResponse CreateJsonResponse(HttpStatusCode statusCode, string jsonBody)
        {
            var responseBodyBytes = Encoding.UTF8.GetBytes(jsonBody);
            return new HttpResponse
            {
                StatusCode = statusCode,
                Headers = new Dictionary<string, string>
                {
                    { "Content-Type", "application/json" },
                    { "Content-Length", responseBodyBytes.Length.ToString() }
                },
                Body = jsonBody
            };
        }

        public HttpResponse CreateTextResponse(HttpStatusCode statusCode, string textBody)
        {
            var responseBodyBytes = Encoding.UTF8.GetBytes(textBody);
            return new HttpResponse
            {
                StatusCode = statusCode,
                Headers = new Dictionary<string, string>
                {
                    { "Content-Type", "text/plain" },
                    { "Content-Length", responseBodyBytes.Length.ToString() }
                },
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