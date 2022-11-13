//Tilly Dewing Fall 2022
//Software Engineering 4319

using System;
using System.Net.Sockets;
using System.Threading;

namespace SenimentAnalyzerServer
{
    class Program 
    {
        static void Main(string[] args)
        {
            LexiconLoader.Load();
            SQLConnection.AttemptSQLConnection();
            Login.GetEmailTemplate();

            //Start Token Management (Clears expired tokens)
            Thread tokenManagement = new Thread(new ThreadStart(Login.TokenManagement));
            tokenManagement.Start();

            //Start tcp server
            TcpListener server = new TcpListener(System.Net.IPAddress.Any, 25555);
            server.Start();

            // Wait for connection...
            server.BeginAcceptTcpClient(OnClientConnecting, server);

            Console.ReadLine();
        }

        static void OnClientConnecting(IAsyncResult ar) //asynchronously accept incoming messages and respond acordingly.
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
