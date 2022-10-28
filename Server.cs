using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace SenimentAnalyzerServer
{
    class Server
    {
        public static int bufferSize = 2048; //2KB buffer size for standard messages

        public static void HandleMessage(TcpClient client)
        {
            //Recive message from client;
            byte[] buffer = new byte[bufferSize];
            int i = client.Client.Receive(buffer);
            string message = System.Text.Encoding.ASCII.GetString(buffer);
            string[] splitMes = message.Split('|');

            //Generate correct response
            string response = "";
            switch (splitMes[0])
            {
                case "LEX": //Lexicon Messages
                    switch (splitMes[1])
                    {
                        case "VER":
                            response = LexiconVerResponse(splitMes);
                            break;
                        case "REQ":
                            LexiconReqResponse(splitMes, client);
                            break;
                    }
                    break;
                case "LGN": //Logon attempt
                    response = LoginResponse(splitMes);
                    break;
                case "REQ": //History Request
                    response = RequestResponse(splitMes);
                    break;
                case "UAP": //Password Change
                    response = UpdateAccountResponse(splitMes);
                    break;
                case "ACT": //Create account
                    response = CreateAccountResponse(splitMes);
                    break;
                case "CMD": //Delete account & other various commands
                    response = CommandResponse(splitMes);  
                    break;

            }
            //Respond if nessesary
            if (response != "")
            {
                SendMessage(response,client);
            }
        }
       //Message Responses                                                      message                    description
        static string LexiconVerResponse(string[] message)                 // LEX|VER|lexNum - Client request version number of lexicon
        {
            int lexNum = int.Parse(message[2]);
            return LexiconLoader.listVers[lexNum].ToString();
        }

        static void LexiconReqResponse(string[] message, TcpClient client) // LEX|REQ|lexNum - Client requseted contents of Lexicon
        {
            int lexNum = int.Parse(message[2]);
            int len = LexiconLoader.wordLists[lexNum].Length; //message length

            if (lexNum == 5) //Emojiis need to be encoded in unicode
            {
                len = len * 8; //size increase for UTF-8 encoding
                SendMessage(len.ToString(), client); //Send length of next message
                SendMessageUTF8(LexiconLoader.wordLists[lexNum], client); //Send entire Lexicon list
            }
            else //All other lists can be ascii encoded to save space
            {
                SendMessage(len.ToString(), client); //Send length of next message
                SendMessage(LexiconLoader.wordLists[lexNum], client); //Send entire Lexicon list
            }
        }

        static string LoginResponse(string[] message)                      // LGN|userName
        {
            User user = SQLConnection.GetUser(message[1]); //Grab user record from database
            return user.password; //return salted hash for comparison

        }

        static string CreateAccountResponse(string[] message)   
        { 
            return "";

        }

        static string UpdateAccountResponse(string[] message)
        {
            return "";

        }

        static string CommandResponse(string[] message)
        {
            return "";

        }

        static string RequestResponse(string[] message)
        { 
            return "";

        }

        static void SendMessage(string message, TcpClient client)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(message); //Encode string to byte array to be sent
            client.GetStream().Write(buffer, 0, buffer.Length); //sends byte array to client
        }
        static void SendMessage(int length, string message, TcpClient client)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(message); //Encode string to byte array to be sent
            client.GetStream().Write(buffer, 0, length); //sends byte array to client
        }
        static void SendMessageUTF8(string message, TcpClient client)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(message); //Encode string to byte array to be sent
            client.GetStream().Write(buffer, 0, buffer.Length); //sends byte array to client
        }
        static string ReciveMessage(TcpClient client, int bufferSize)
        {
            byte[] buffer = new byte[bufferSize];
            int i = client.Client.Receive(buffer);
            return System.Text.Encoding.ASCII.GetString(buffer);
        }
        static string ReciveMessageUTF8(TcpClient client, int bufferSize)
        {
            byte[] buffer = new byte[bufferSize];
            int i = client.Client.Receive(buffer);
            return System.Text.Encoding.UTF8.GetString(buffer);
        }
    }
}

