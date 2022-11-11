//Tilly Dewing Fall 2022
//Software Engineering 4319

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Threading;

namespace SenimentAnalyzerServer
{
    class Login
    {
        public static List<Token> resetTokens = new List<Token>(); //Valid Password Reset Tokens
        public static List<Token> sessionTokens = new List<Token>(); //Valid Session Tokens
        public static string smtpPassword = "kwcbuwerypzlmwcz";

        public static string resetEmailBody;
        public static bool CheckLogin(string username, string password)
        {
            //Grab Stored password from SQL database
            User user = SQLConnection.GetUser(username);
            

            if (user.userID != -1) //username in database
            {
                //Convert to byte array
                byte[] hashBytes = Convert.FromBase64String(user.password);

                //Get Salt (first 16 bytes)
                byte[] salt = new byte[16];
                Array.Copy(hashBytes, 0, salt, 0, 16);

                //Compute hash for password with no salt
                var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000);
                byte[] hash = pbkdf2.GetBytes(20);

                //Compare results
                for (int i = 0; i < 20; i++)
                {
                    if (hashBytes[i + 16] != hash[i])
                    {
                        return false; //invalid password
                    }
                }
                //Correct
                return true;
            }

            return false; //invalid user
        }

        private static string SaltedHash(string plainText)
        {
            //Create a salt
            byte[] salt = new byte[16];
            new RNGCryptoServiceProvider().GetBytes(salt);
            //Create Bytes of hash value
            var pbkdf2 = new Rfc2898DeriveBytes(plainText, salt, 100000);
            byte[] hash = pbkdf2.GetBytes(20);
            //Combined Hash and Salt
            byte[] hashBytes = new byte[36];
            Array.Copy(salt, 0, hashBytes, 0, 16);
            Array.Copy(hash, 0, hashBytes, 16, 20);
            //Convert to string
            return Convert.ToBase64String(hashBytes);
        }

        public static bool ChangePassword(string username, string currentPass, string newPass)
        {
            if (CheckLogin(username, currentPass))
            {
                string saltedHash = SaltedHash(newPass);
                //SQL command to store new saltedhash under username
                SQLConnection.UpdateUser(username, saltedHash);
            }

            return false;
        }

        public static bool CreateAccount(string username, string password, string name)
        {
            if (!CheckUsername(username)) //if user is not in DB
            {
                User user = new User(username, SaltedHash(password), 0, name);
                SQLConnection.CreateUser(user);
                return true;
            }
            return false;

        }

        public static bool DeleteAccount(string username, string password)
        {
            if (CheckLogin(username, password))
            {
                SQLConnection.DeleteUser(username);
                return true;
            }
            else
            {
                return false;
            }
        }

        private static bool CheckUsername(string username) //returns true if username is in DB
        {
            if (SQLConnection.GetUser(username).userID > -1)
            {
                return true;
            }
            
            return false;
        }

        public static void RemoveExpiredTokens(List<Token> tokens) 
        {
            for(int i = tokens.Count - 1; i >= 0; i--)
            {
                if (tokens[i].isExpired())
                { 
                    tokens.RemoveAt(i);
                }
            }
        }
        
        public static void GetEmailTemplate()
        {
            string appPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            resetEmailBody = File.ReadAllText(appPath + @"\resetEmailTemplate.txt");
        }
        public static bool GeneratePasswordReset(string userName)
        {
            User user = SQLConnection.GetUser(userName);
            if (user.userID == -1) //if user not found
            {
                //empty user rec
                return false;
            }

            Token token = new Token(userName, 16, 6); //new Token vaild for 6 Hours
            resetTokens.Add(token); //add to valid tokem list

            var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential("rereview.4319@gmail.com", smtpPassword),
                EnableSsl = true,
            };

            smtpClient.Send("rereview.4319@gmail.com", user.name, "Re-Review Password Reset", string.Format(resetEmailBody, token.key));
            return true;
        }

        private static bool FindResetToken(string key, string userName)
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
        private static void RemoveResetToken(string key)
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

        public static bool ResetPassword(string username, string token, string newPassword)
        {
            if (FindResetToken(token, username)) //If token is valid
            {
                SQLConnection.UpdateUser(username, newPassword); //update password
                RemoveResetToken(token); //removed used token
                return true;
            }

            return false;
        }
        public static void TokenManagement() //Background thread for removing expired tokens.
        {
            while (true)
            {
                RemoveExpiredTokens(resetTokens);
                RemoveExpiredTokens(sessionTokens);
                Console.WriteLine("Expired Tokens Cleared");
                Thread.Sleep(1800000); //Sleep 30 minutes
            }
        }
    }

    //Stores a random alpha numeric key default length 10 & the time the token was issued by the server.
    public class Token //Tokens used for logon sessions and password resets
    {
        public string key;
        public string userName;
        DateTime issuedTime;
        DateTime expireTime;

        public Token(string userName, int validHours)
        {
            this.userName = userName;
            key = RandomString(10);
            issuedTime = DateTime.Now;
            expireTime = DateTime.Now.AddHours(validHours);
        }

        public Token(string userName, int length, int validHours)
        {
            this.userName = userName;
            key = RandomString(length);
            issuedTime = DateTime.Now;
            expireTime = DateTime.Now.AddHours(validHours);
        }
        private static string RandomString(int length)
        {
            const string pool = "abcdefghijklmnopqrstuvwxyz0123456789";
            var builder = new System.Text.StringBuilder();
            Random random = new Random(Guid.NewGuid().GetHashCode());

            for (var i = 0; i < length; i++)
            {
                var c = pool[random.Next(0, pool.Length)];
                builder.Append(c);
            }

            return builder.ToString();
        }

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
