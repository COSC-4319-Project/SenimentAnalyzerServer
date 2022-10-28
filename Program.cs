using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace SenimentAnalyzerServer
{
    class Program
    {
        static void Main(string[] args)
        {
            LexiconLoader.Load();

            TcpListener server = new TcpListener(System.Net.IPAddress.Any, 25555);
            server.Start();

            // Wait for connection...
            server.BeginAcceptTcpClient(OnClientConnecting, server);

            Console.ReadLine();
        }

        static void OnClientConnecting(IAsyncResult ar)
        {
            try
            {
                Console.WriteLine("Client connecting...");

                if (ar.AsyncState is null)
                {
                    throw new Exception("AsyncState is null. Pass it as an argument to BeginAcceptSocket method");
                }
                // Get the server. This was passed as an argument to BeginAcceptSocket method
                TcpListener s = (TcpListener)ar.AsyncState;

                // listen for more clients. Note its callback is this same method (recusive call)
                s.BeginAcceptTcpClient(OnClientConnecting, s);

                // Get the client that is connecting to this server
                TcpClient client = s.EndAcceptTcpClient(ar);

                Console.WriteLine("Client connected succesfully");

                Server.HandleMessage(client);

                // close the tcp connection
                client.Close();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }
    }
}
