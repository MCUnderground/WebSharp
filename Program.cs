using WebSharp.Server;

namespace WebSharp
{
    public class Program
    {
        public static async Task Main()
        {
            var server = await WebSharpServer.CreateAsync("127.0.0.1", 9999);
        }
    }
}