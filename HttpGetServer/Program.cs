using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace HttpGetServer
{
    class HttpGetServer
    {
        // THIS IS A SERVER AND A CLIENT AT THE SAME TIME!

        // Represents the server where the client will connect
        private TcpListener server;
        private int listeningPort;

        // Represents the client that will connect to us
        private TcpClient client;

        // For writing and reading messages
        private NetworkStream stream;

        // For the orange flash on the taskbar icon
        IntPtr handle;

        public HttpGetServer()
        {
            Console.Title = "HttpGetServer";
            handle = Process.GetCurrentProcess().MainWindowHandle;
            server = new TcpListener(IPAddress.Any, 7676);
        }

        // Goes through everything until the conversation can start
        public void Start()
        {
            // These methods all handle validation by themselves
            // You can only switch their order here, which is not recommended
            Logo();

            server.Start();

            while (true)
            {
                Handshaking();
                Responding();
                CloseConnection();
            }
        }


        // So multiple browsers can make requests
        private void CloseConnection()
        {
            client.GetStream().Close();
            client.Close();
            server.Stop();
            server.Start();
        }

        private void Responding()
        {
            setTextColor(2);
            // Formatting the received message
            string receivedMessage = DateTime.Now.ToString("[HH:mm:ss] ") + ReceiveRequest() + "\n";

            // Output the received message with a retro terminal effect
            Console.WriteLine(receivedMessage);

            // Orange flash effect on taskbar, to notify a new message has arrived
            FlashWindow(handle, true);

            string URL = getRequestedUrl(receivedMessage);

            //string htmlFile = File.ReadAllText(URL);
            SendResponse("HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nContent-Length: 12\r\n\r\nHello world!");
            //SendResponse("HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nContent-Length: " + GetContentLength(URL) + "\r\n\r\n" + htmlFile);

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine(DateTime.Now.ToString("[HH:mm:ss] ") + "Waiting for next request");
        }

        private string getRequestedUrl(string receivedMessage)
        {
            return receivedMessage.Substring(15, receivedMessage.IndexOf("HTTP/1.1") - 15);
        }

        public bool Handshaking()
        {
            // Waiting for client to connect
            client = server.AcceptTcpClient();

            // After successfull connection get the stream ready to read and write
            stream = client.GetStream();

            // Status text
            Console.WriteLine(DateTime.Now.ToString("[HH:mm:ss] ") + "Connection established with: " + client.Client.RemoteEndPoint);

            // This means the connection has been established, we can continue, see Run(); method
            return true;
        }

        public string ReceiveRequest()
        {
            byte[] buffer = new byte[2048];

            stream.Read(buffer, 0, buffer.Length);

            string receivedMessage = Encoding.Default.GetString(buffer);

            // Buffer size is larger than the actual message
            // The rest is filled with '\0' (' '), we trim it
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

        public int GetContentLength(string URL)
        {
            return 1;
        }

        public void Logo()
        {
            // Small Slant
            Console.Clear();
            Console.WriteLine("   __ ____  __       _____    __  ____                    ");
            Console.WriteLine("  / // / /_/ /____  / ___/__ / /_/ __/__ _____  _____ ____");
            Console.WriteLine(" / _  / __/ __/ _ \\/ (_ / -_) __/\\ \\/ -_) __/ |/ / -_) __/");
            Console.WriteLine("/_//_/\\__/\\__/ .__/\\___/\\__/\\__/___/\\__/_/  |___/\\__/_/   ");
            Console.WriteLine("            /_/                                           by erwin");
        }

        // For the orange flash on the taskbar icon
        [DllImport("user32.dll")]
        static extern bool FlashWindow(IntPtr hwnd, bool bInvert);
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            HttpGetServer server = new HttpGetServer();
            server.Logo();
            server.Start();

            Console.ReadKey();
        }
    }
}
