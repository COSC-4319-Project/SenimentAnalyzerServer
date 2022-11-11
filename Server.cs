//Tilly Dewing Fall 2022
//Software Engineering 4319

using System;
using System.Text;
using System.Net.Sockets;

namespace SenimentAnalyzerServer
{
    class Server //Comunicates lexicon updates and cached history.
    {
        public static int bufferSize = 2048; //2KB buffer size for standard messages

        public static void HandleMessage(TcpClient client)
        {
            string message = ReciveMessage(client, bufferSize);
            string[] splitMes = message.Split('|');

            //Generate correct response
            string response = "";
            switch (splitMes[0])
            {
                case "LEX": //Lexicon Messages
                    switch (splitMes[1])
                    {
                        case "VER": //Version Request
                            response = LexiconVerResponse(splitMes);
                            break;
                        case "REQ": //Contents request
                            LexiconReqResponse(splitMes, client);
                            break;
                    }
                    break;
                case "LGN": //Logon attempt
                    response = LoginResponse(splitMes);
                    break;
                case "HIS": //History Request
                    switch (splitMes[1])
                    {
                        case "SNG": //Version Request
                            response = RequestResponse(splitMes);
                            break;
                        case "ALL": //Contents request
                            response = AllHistoryResponse(splitMes);
                            break;
                    }
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

        //Message Responses 
        // LEX|VER|lexNum - Client request version number of lexicon
        static string LexiconVerResponse(string[] message)                 
        {
            int lexNum = int.Parse(message[2]);
            return LexiconLoader.listVers[lexNum].ToString();
        }

        // LEX|REQ|lexNum - Client requseted contents of Lexicon
        static void LexiconReqResponse(string[] message, TcpClient client)
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

        // LGN|userName|password - Login
        static string LoginResponse(string[] message)                      
        {

            if (Login.CheckLogin(message[1], message[2]))
            {
                User user = SQLConnection.GetUser(message[1]); //Grab user record from database
                return user.userID + "|" + user.name; //Return User ID and name
            }
            else
            {
                return "INVALID";
            }

        }

        //ACT|userName|password|name - create account
        static string CreateAccountResponse(string[] message)   
        {
            if (Login.CreateAccount(message[1], message[2], message[3]))
            {
                return "VALID";
            }
            else
            {
                return "INVALID";
            }

        }

        //ACT|RST|REQ|username
        static string PassResetTokenReqResponse(string[] message)
        {
            if (Login.GeneratePasswordReset(message[3]))
            {
                return "Reset Sent";
            }

            return "Username not found";
        }

        //ACT|RST|username|token|newPassword
        static string PassResetResponse(string[] message)
        {
            if (Login.ResetPassword(message[2], message[3], message[4]))
            {
                return "Password Reset!";
            }
            return "Invalid Username or Token";
        }
        //UAP|userName|curPassword|newPassword - update password
        static string UpdateAccountResponse(string[] message)
        {
            if (Login.ChangePassword(message[1], message[2], message[3]))
            {
                return "VALID";
            }
            else
            {
                return "INVALID";
            }
        }

        //CMD|DEL|userName
        static string CommandResponse(string[] message)
        {
            return "";

        }

        //HIS|asinID - request history for asinID
        static string RequestResponse(string[] message)
        {
            HistoryRec rec = SQLConnection.GetHistoryRec(message[2]);

            if (rec.numRev > 0)//Valid Record Returned.
            {
                if (rec.dateAnalyzed < (DateTime.Today.AddDays(-7)))
                {
                    return "0";
                }
                //asin|uID|adjRat|sent|numRev|numPos|numNeg|connfidence|dateAnalyzed
                return rec.asinID + "|" + rec.uID + "|" + rec.adjustedRating + "|" + rec.productName + "|" + rec.numRev + "|" + rec.numPos + "|" + rec.numNeg + "|" + rec.confidence + "|" + rec.dateAnalyzed;
            }

            return "0";
        }
        static string AllHistoryResponse(string[] message)
        {
            //HistoryRec[] recs = SQLConnection.GetHistoryRec(message[2]);
            return "";  
        }

        //Message Functions using Ascii & UTF8 encoding.
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
            return System.Text.Encoding.ASCII.GetString(buffer, 0, i);
        }
        static string ReciveMessageUTF8(TcpClient client, int bufferSize)
        {
            byte[] buffer = new byte[bufferSize];
            int i = client.Client.Receive(buffer);
            return System.Text.Encoding.UTF8.GetString(buffer,0,i);
        }
    }
}

