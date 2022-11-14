using System;
using System.Collections.Generic;
using System.Threading;

namespace SenimentAnalyzerServer
{
    //Stores a random alpha numeric key default length 10 & the time the token was issued by the server.
    public class Token //Tokens used for logon sessions and password resets
    {
        public static List<Token> resetTokens = new List<Token>(); //Valid Password Reset Tokens
        public static List<Token> sessionTokens = new List<Token>(); //Valid Session Tokens

        const string pool = "abcdefghijklmnopqrstuvwxyz0123456789";

        public string key;
        public string userName;

        DateTime issuedTime;
        DateTime expireTime;
        //Pool of valid token chars

        //Creates a new token for a given user, 
        public Token(string userName, int validHours)
        {
            this.userName = userName;
            key = RandomString(10);
            issuedTime = DateTime.Now;
            expireTime = DateTime.Now.AddHours(validHours);
        }

        //Creates a new token for "username", with length "length" valid for "validhours"
        public Token(string userName, int length, int validHours)
        {
            this.userName = userName;
            key = RandomString(length);
            issuedTime = DateTime.Now;
            expireTime = DateTime.Now.AddHours(validHours);
        }

        //Generates random alpha numeric key
        private static string RandomString(int length)
        {

            var builder = new System.Text.StringBuilder();
            Random random = new Random(Guid.NewGuid().GetHashCode());

            for (var i = 0; i < length; i++)
            {
                var c = pool[random.Next(0, pool.Length)];
                builder.Append(c);
            }

            return builder.ToString();
        }

        //Takes a given token list and removes any expired tokens
        public static void RemoveExpiredTokens(List<Token> tokens)
        {
            for (int i = tokens.Count - 1; i >= 0; i--)
            {
                if (tokens[i].isExpired())
                {
                    tokens.RemoveAt(i);
                }
            }
        }

        //Background thread for removing expired tokens.
        public static void TokenManagement()
        {
            while (true)
            {
                RemoveExpiredTokens(resetTokens);
                RemoveExpiredTokens(sessionTokens);
                Console.WriteLine("Expired Tokens Cleared");
                Thread.Sleep(1800000); //Sleep 30 minutes
            }
        }

        //Returns true if reset token is in valid list and not expired
        public static bool FindResetToken(string key, string userName)
        {
            foreach (Token token in resetTokens)
            {
                if (token.key == key && token.userName == userName)
                {
                    return true;
                }
            }
            return false;
        }

        //Deletes password token from valid list
        public static void RemoveResetToken(string key)
        {
            for (int i = resetTokens.Count - 1; i >= 0; i--)
            {
                if (resetTokens[i].key == key)
                {
                    resetTokens.RemoveAt(i);
                    return;
                }
            }
        }

        //Returns true if token is expired
        public bool isExpired()
        {
            if (DateTime.Compare(issuedTime, expireTime) > 0)
            {
                return true;
            }

            return false;
        }
    }
}
