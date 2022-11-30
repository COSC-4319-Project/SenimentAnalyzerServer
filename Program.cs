//Tilly Dewing Fall 2022
//Software Engineering 4319

using System;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace SenimentAnalyzerServer
{
    class Program 
    {
        static void Main(string[] args)
        {
            //Initialization Functions
            LexiconLoader.Load();
            SQLConnection.AttemptSQLConnection();
            Login.GetEmailTemplate();

            //Start Token Management (Clears expired tokens)
            Thread tokenManagement = new Thread(new ThreadStart(Token.TokenManagement));
            tokenManagement.Start();

            //Start tcp server
            TcpListener server = new TcpListener(IPAddress.Loopback, 5300);
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
                var stream = client.GetStream();

                //Get the tcp stream and wrap it in an SSL stream
                SslStream sslStream = new SslStream(stream, false);
                var certificate = new X509Certificate2("server.pfx", "password");
                sslStream.AuthenticateAsServer(certificate, false, System.Security.Authentication.SslProtocols.Default, false);

                Console.WriteLine("Client connected succesfully");
                
                //Respond to clients message
                Server.HandleMessage(sslStream);

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
