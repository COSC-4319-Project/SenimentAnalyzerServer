﻿//Tilly Dewing Fall 2022
//Software Engineering 4319

using System;
using System.Text;
using System.Net.Sockets;
using System.Net.Security;

namespace SenimentAnalyzerServer
{
    class Server //Comunicates lexicon updates and cached history and authenticates logins.
    {
        public static int bufferSize = 8192; //8KB buffer size for standard messages

        //Recives incomming message and responds accordingly
        public static void HandleMessage(SslStream stream)
        {
            string message = ReciveMessage(stream, bufferSize);
            Console.WriteLine(message);
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
                            LexiconReqResponse(splitMes, stream);
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
                            response = HistoryRequestResponse(splitMes);
                            break;
                        case "ALL": //Contents request
                            response = AllHistoryResponse(splitMes, stream);
                            break;
                    }
                    break;
                case "UAP": //Password Change
                    response = UpdateAccountResponse(splitMes);
                    break;
                case "ACT": //Create account
                    response = HandleAccountMessage(splitMes);
                    break;
                case "CMD": //Delete account & other various commands
                    response = CommandResponse(splitMes);  
                    break;
                case "AHIS":
                    response = AddHistoryRecord(splitMes);
                    break;
                case "SLT":
                    response = Login.GetSaltFromPswd(splitMes[1]);
                    break;

            }
            //Respond if nessesary
            if (response != "")
            {
                Console.WriteLine(response);
                SendMessage(response,stream);
            }
        }
        //Handle account messages (split off from HandleMessage for clarity )
        static string HandleAccountMessage(string[] message)
        {
            if (message[1] == "RST")
            {
                if (message[2] == "REQ")
                {
                    return PassResetTokenReqResponse(message);
                }
                else
                {
                    return PassResetResponse(message);
                }
            }
            else
            {
                return CreateAccountResponse(message);
            }
        }

        //Message Responses:

        //Lexicons
        //--------------------------------------------------------------------------------------------
        // LEX|VER|lexNum - Client request version number of lexicon
        static string LexiconVerResponse(string[] message)                 
        {
            int lexNum = int.Parse(message[2]);
            return LexiconLoader.listVers[lexNum].ToString();
        }
        //Lexicon Update:
        //LEX|REQ|lexNum - Client requseted contents of Lexicon
        static void LexiconReqResponse(string[] message, SslStream stream)
        {
            int lexNum = int.Parse(message[2]);
            int len = LexiconLoader.wordLists[lexNum].Length; //message length

            if (lexNum == 5) //Emojiis need to be encoded in unicode
            {
                len = len * 8; //size increase for UTF-8 encoding
                SendMessage(len.ToString(), stream); //Send length of next message
                SendMessageUTF8(LexiconLoader.wordLists[lexNum], stream); //Send entire Lexicon list
            }
            else //All other lists can be ascii encoded to save space
            {
                SendMessage(len.ToString(), stream); //Send length of next message
                SendMessage(LexiconLoader.wordLists[lexNum], stream); //Send entire Lexicon list
            }
        }

        //Login Messages
        //--------------------------------------------------------------------------------------------
        // LGN|userName|password - Login
        static string LoginResponse(string[] message)                      
        {
            
            if (Login.CheckLoginHash(message[1], message[2]))
            {
                User user = SQLConnection.GetUser(message[1]); //Grab user record from database
                Console.WriteLine("Client Logged IN");
                return user.userID + "|name" ; //Return User ID and name
            }
            else
            {
                Console.WriteLine("Client Logged in Falied");
                return "INVALID";
            }

        }
        //Account Messages:
        //--------------------------------------------------------------------------------------------

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
        //ACT|RST|REQ|username|email
        static string PassResetTokenReqResponse(string[] message)
        {
            if (Login.GeneratePasswordReset(message[3], message[4]))
            {
                return "VALID";
            }

            return "INVALID";
        }

        //ACT|RST|username|token|newPassword
        static string PassResetResponse(string[] message)
        {
            if (Login.ResetPassword(message[2], message[3], message[4]))
            {
                return "VALID";
            }
            return "INVALID";
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
        //Delete User Command
        //CMD|DEL|userName
        static string CommandResponse(string[] message)
        {
            return "";

        }

        //History Messages
        //--------------------------------------------------------------------------------------------
        //HIS|asinID - request history for asinID
        static string HistoryRequestResponse(string[] message)
        {
            HistoryRec rec = SQLConnection.GetHistoryRec(message[2]);

            if (rec.numRev > 0)//Valid Record Returned.
            {
                if (rec.dateAnalyzed < (DateTime.Today.AddDays(-7)))
                {
                    return "0";
                }
                //asin|uID|adjRat|productName|numRev|numPos|numNeg|connfidence|dateAnalyzed|origRating
                return rec.asinID + "|" + rec.uID + "|" + rec.adjustedRating + "|" + rec.productName + "|" + rec.numRev + "|" + rec.numPos + "|" + rec.numNeg + "|" + rec.confidence + "|" + rec.dateAnalyzed + "|" + rec.origRating;
            }

            return "0";
        }
        //AHIS|asin|uID|adjRat|productName|numRev|numPos|numNeg|connfidence|dateAnalyzed|origRating
        public static string AddHistoryRecord(string[] message)
        {
            SQLConnection.DeleteHistoryRec(message[1]);
            SQLConnection.CreateHistoryRec(new HistoryRec(message));
            return "";
        }
        static string AllHistoryResponse(string[] message, SslStream stream)
        {
            int i = 0;
            if (!int.TryParse(message[2], out i))
            {
                return ""; //Make sure client input is valid.
            }
            HistoryRec[] recs = SQLConnection.GetUserHistory(i);

            foreach (HistoryRec rec in recs)
            {
                string recStr =  rec.asinID + "|" + rec.uID + "|" + rec.adjustedRating + "|" + rec.productName + "|" + rec.numRev + "|" + rec.numPos + "|" + rec.numNeg + "|" + rec.confidence + "|" + rec.dateAnalyzed + "|" + rec.origRating;
                SendMessage(recStr, stream);
            }
            return "";  
        }

        //Message Functions using Ascii & UTF8 encoding.
        //--------------------------------------------------------------------------------------------
        static void SendMessage(string message, SslStream stream)
        {
            message = "@" + message; //First character is split into another message for some reason in SSL
            byte[] buffer = Encoding.ASCII.GetBytes(message); //Encode string to byte array to be sent
            stream.Write(buffer, 0, buffer.Length); //sends byte array to client
        }
        static void SendMessage(int length, string message, SslStream stream)
        {
            message = "@" + message; //First character is split into another message for some reason in SSL
            byte[] buffer = Encoding.ASCII.GetBytes(message); //Encode string to byte array to be sent
            stream.Write(buffer, 0, length); //sends byte array to client
        }
        static void SendMessageUTF8(string message, SslStream stream)
        {
            message = "@" + message; //First character is split into another message for some reason in SSL
            byte[] buffer = Encoding.UTF8.GetBytes(message); //Encode string to byte array to be sent
            stream.Write(buffer, 0, buffer.Length); //sends byte array to client
        }
        static string ReciveMessage(SslStream stream, int bufferSize)
        {
            byte[] buffer = new byte[bufferSize];
            int i = stream.Read(buffer, 0, bufferSize);//Ignore first message
            i = stream.Read(buffer, 0, bufferSize);
            return Encoding.ASCII.GetString(buffer, 0, i);
        }
        static string ReciveMessageUTF8(SslStream stream, int bufferSize)
        {
            byte[] buffer = new byte[bufferSize];
            int i = stream.Read(buffer, 0, bufferSize); //Ignore first message
            i = stream.Read(buffer, 0, bufferSize);
            return Encoding.UTF8.GetString(buffer,0,i);
        }
    }
}

