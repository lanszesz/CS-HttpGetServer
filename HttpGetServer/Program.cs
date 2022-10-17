using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace HttpGetServer
{
    class HttpGetServer
    {
        private TcpListener server;
        private int listeningPort;
        private TcpClient client;
        private NetworkStream stream;
        private string indexPage;
        IntPtr handle;

        public HttpGetServer()
        {
            Console.Title = "HttpGetServer";
            handle = Process.GetCurrentProcess().MainWindowHandle;
            indexPage = "index.html";
        }

        public void Start()
        {
            bool ok = false;
            while (!ok)
            {
                Console.Write("Listening PORT: ");
                try
                {
                    listeningPort = int.Parse(Console.ReadLine());
                    ok = true;
                }
                catch (Exception)
                {
                    Console.Clear();
                }
            }

            Console.Clear();
            Console.WriteLine("   __ ____  __       _____    __  ____                    ");
            Console.WriteLine("  / // / /_/ /____  / ___/__ / /_/ __/__ _____  _____ ____");
            Console.WriteLine(" / _  / __/ __/ _ \\/ (_ / -_) __/\\ \\/ -_) __/ |/ / -_) __/");
            Console.WriteLine("/_//_/\\__/\\__/ .__/\\___/\\__/\\__/___/\\__/_/  |___/\\__/_/   ");
            Console.WriteLine("            /_/                                           by erwin");

            server = new TcpListener(IPAddress.Any, listeningPort);
            server.Start();
            setTextColor(1);
            Console.WriteLine(DateTime.Now.ToString("[HH:mm:ss] ") + "Server started at PORT: " + listeningPort);

            RequestListenLoop();
        }

        private void RequestListenLoop()
        {
            while (true)
            {
                Handshaking();
                Respond();
                ResetConnection();
            }
        }

        public bool Handshaking()
        {
            setTextColor(1);

            client = server.AcceptTcpClient();
            stream = client.GetStream();

            Console.WriteLine(DateTime.Now.ToString("[HH:mm:ss] ") + "Connection established with: " + client.Client.RemoteEndPoint);

            return true;
        }

        private void Respond()
        {
            setTextColor(2);

            string receivedRequest = ReceiveRequest() + "\n";

            Console.WriteLine("[HH:mm:ss] " + receivedRequest + "\n");

            FlashWindow(handle, true);

            string URL = getRequestedUrl(receivedRequest);
            string HTML = "";
            try
            {
                HTML = File.ReadAllText(URL);
            }
            catch (Exception)
            {
                // In case of 404.html is not accessible or found
                SendResponse("HTTP/1.1 404 Not Found\r\nContent-Type: text/text\r\nContent-Length: 13\r\n\r\n404 Not Found");
                return;
            }

            SendResponse("HTTP/1.1 200 OK\r\nContent-Type: text/html\r\nContent-Length: " + HTML.Length + "\r\n\r\n" + HTML);

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine(DateTime.Now.ToString("[HH:mm:ss] ") + "Waiting for next request");
        }

        // Without this it won't work with multiple browsers
        // But sometimes it doesn't work anyways... You then need to refresh the page IN EVERY, then it should work properly
        private void ResetConnection()
        {
            client.GetStream().Close();
            client.Close();
            server.Stop();
            server.Start();
        }

        private string getRequestedUrl(string request)
        {
            int firstRowEnd = request.IndexOf("\r\n");
            string firstRow = "";

            // Sometimes the server can crash here, 
            // I tested it a lot, we will always get our page. Even if I return a 404 here
            try
            {
                firstRow = request.Substring(0, firstRowEnd);
            }
            catch (Exception)
            {
                return "404.html";
            }

            if (firstRow.Contains("GET / ") || (firstRow.Contains("GET /") && firstRow.Contains(".html")))
            {
                int endIndex = request.IndexOf(" HTTP/1.1");
                string path = request.Substring(5, endIndex - 4);

                if (path == " ")
                {
                    return indexPage;
                }
                else return path;
            }

            // First time I tested it the first row of the request won't always look like GET {page here} HTTP/1.1
            // That's why the upper rows are complicated...
            /*string port = listeningPort.ToString();
            if (firstRow.Contains(port + "/\n") || (firstRow.Contains(port + '/') && firstRow.Contains(".html")))
            {
                int index = request.IndexOf(port + '/');
                string path = request.Substring(index, firstRowEnd);

                if (path == " ")
                {
                    return request;
                }
                else return path;
            }*/

            return "404.html";
        }

        public string ReceiveRequest()
        {
            byte[] buffer = new byte[2048];

            stream.Read(buffer, 0, buffer.Length);

            string receivedMessage = Encoding.Default.GetString(buffer);

            receivedMessage = receivedMessage.TrimEnd('\0');

            return receivedMessage;
        }

        public void SendResponse(string message)
        {
            byte[] buffer = Encoding.Default.GetBytes(message);

            stream.Write(buffer, 0, buffer.Length);
        }

        public void setTextColor(byte color)
        {
            switch (color)
            {
                case 1:
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    break;
                case 2:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
            }
        }

        [DllImport("user32.dll")]
        static extern bool FlashWindow(IntPtr hwnd, bool bInvert);
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            HttpGetServer server = new HttpGetServer();
            server.Start();
        }
    }
}
