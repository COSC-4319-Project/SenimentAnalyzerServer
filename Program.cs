//Tilly Dewing Fall 2022
//Software Engineering 4319

using System;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Security.Cryptography;
using System.IO;
using Org.BouncyCastle.Asn1.X509;

namespace SenimentAnalyzerServer
{
    class Program 
    {
        //Cert
        //private static X509Certificate2 certificate = new X509Certificate2("server.pfx", "Heck!nS3cure");
        static void Main(string[] args)
        {
            //MakeCert();
            LexiconLoader.Load();
            SQLConnection.AttemptSQLConnection();
            Login.GetEmailTemplate();

            //Start Token Management (Clears expired tokens)
            Thread tokenManagement = new Thread(new ThreadStart(Token.TokenManagement));
            tokenManagement.Start();

            //Start tcp server
            //TcpListener server = new TcpListener(System.Net.IPAddress.Any, 25555);
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
                SslStream sslStream = new SslStream(stream, false);
                var certificate = new X509Certificate2("server.pfx", "password");
                sslStream.AuthenticateAsServer(certificate, false, System.Security.Authentication.SslProtocols.Default, false);

                Console.WriteLine("Client connected succesfully");

                Server.HandleMessage(sslStream);

                // close the tcp connection
                client.Close();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        static void MakeCert() //Create a self signed cert.
        {
            var ecdsa = ECDsa.Create(); // generate asymmetric key pair
            var req = new CertificateRequest("cn=foobar", ecdsa, HashAlgorithmName.SHA256);
            var cert = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(5));

            // Create PFX (PKCS #12) with private key
            File.WriteAllBytes("server.pfx", cert.Export(X509ContentType.Pfx, "Heck!nS3cure"));

            // Create Base 64 encoded CER (public key only)
            File.WriteAllText("server.pfx", "-----BEGIN CERTIFICATE-----\r\n" + Convert.ToBase64String(cert.Export(X509ContentType.Cert), Base64FormattingOptions.InsertLineBreaks) + "\r\n-----END CERTIFICATE-----");
        }
    }
}
