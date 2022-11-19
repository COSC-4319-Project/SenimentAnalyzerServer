//Tilly Dewing Fall 2022
//Software Engineering 4319

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;

namespace SenimentAnalyzerServer
{
    class Login
    {
        public static string smtpPassword = "kwcbuwerypzlmwcz"; //Uniquie app & hardware specific token for smtp login
        public static string resetEmailBody; //unformated string for body of reset email.

        //Regex for final server side validation
        public static Regex ValidateUsername = new Regex("^[a-zA-Z0-9]+$");
        public static Regex ValidatePassword = new Regex("^(?=.*?[A-Z])(?=.*?[a-z])(?=.*?[0-9])(?=.*?[#?!@$%^&*-]).{8,}$");
        public static Regex ValidateEmail = new Regex("^[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?$");
        
        //Compares salted hash returns true if login valid
        public static bool CheckLogin(string username, string password)
        {
            //server side input validation
            if (!ValidateUsername.IsMatch(username) || !ValidatePassword.IsMatch(password))
            {
                return false;
            }

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

        //Compares salted hash returns true if login valid
        public static bool CheckLoginHash(string username, string hashedPswd)
        {
            //Validate username is in correct format
            if (!ValidateUsername.IsMatch(username))
            {
                return false;
            }
            //try and get user record from database
            User user = SQLConnection.GetUser(username);

            if (user.userID != -1)  //if user exsists in database
            {
                return CompareHashes(user.password, hashedPswd);
            }

            return false; //User dosent exist
        }

        public static string GetSaltFromPswd(string username)
        {
            if (!ValidateUsername.IsMatch(username)) //Validate input.
            {
                return "";
            }

            User user = SQLConnection.GetUser(username); //Get user record

            if (user.userID != -1) //username in database
            { 
                byte[] hashBytes = Convert.FromBase64String(user.password);
                byte[] salt = new byte[16];//extract salt.
                Array.Copy(hashBytes, 0, salt, 0, 16);
                return Convert.ToBase64String(salt);
            }

            return "";
        }

        private static string GetSaltFromEmail(string username)
        {
            User user = SQLConnection.GetUser(username); //Get user record

            if (user.userID != -1) //username in database
            {
                byte[] hashBytes = Convert.FromBase64String(user.name);
                byte[] salt = new byte[16];//extract salt.
                Array.Copy(hashBytes, 0, salt, 0, 16);
                Console.WriteLine(Convert.ToBase64String(salt));
                return Convert.ToBase64String(salt);
            }

            return "";
        }

        public static string HashFromSalt(string plainText, string strSalt)
        {
            if (strSalt == "INVALID")
            {
                return "INVALID";
            }

            //Convert salt
            byte[] salt = Convert.FromBase64String(strSalt);

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

        private static bool CompareHashes(string hash1, string hash2)
        {
            Console.WriteLine("Hash1: " + hash1);
            Console.WriteLine("Hash1: " + hash2);

            //convert hash strings into byte array
            byte[] hashBytes1 = Convert.FromBase64String(hash1);
            byte[] hashBytes2 = Convert.FromBase64String(hash2);

            //Compare hashes:
            for (int i = 0; i < 36; i++)
            {
                if (hashBytes1[i] != hashBytes2[i])
                {
                    return false; //invalid password
                }
            }
            //Correct
            return true;
        }

        //Checks valid login then stores new salted hashed in DB
        public static bool ChangePassword(string username, string currentPass, string newPass)
        {
            //server side input validation
            if (!ValidateUsername.IsMatch(username))
            {
                return false;
            }

            if (CheckLoginHash(username, currentPass))
            {
                //SQL command to store new saltedhash under username
                SQLConnection.UpdateUser(username, newPass);
            }

            return false;
        }

        //creates an account record returns false if user already exists
        public static bool CreateAccount(string username, string password, string name)
        {
            //server side input validation
            if (!ValidateUsername.IsMatch(username))
            {
                return false;
            }

            if (!CheckUsername(username)) //if user is not in DB
            {
                User user = new User(username, password, 0, name);
                SQLConnection.CreateUser(user);
                return true;
            }
            return false;

        }

        //deletes a given user account (requires valid login)
        public static bool DeleteAccount(string username, string password)
        {
            //server side input validation
            if (!ValidatePassword.IsMatch(password) || !ValidateUsername.IsMatch(username))
            {
                return false;
            }

            if (CheckLoginHash(username, password))
            {
                SQLConnection.DeleteUser(username);
                return true;
            }
            else
            {
                return false;
            }
        }

        //returns true if username is in DB
        private static bool CheckUsername(string username) 
        {
            if (SQLConnection.GetUser(username).userID > -1)
            {
                return true;
            }
            
            return false;
        }

        //Loads reset email template from file
        public static void GetEmailTemplate()
        {
            string appPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            resetEmailBody = File.ReadAllText(appPath + @"\resetEmailTemplate.txt");
        }

        //Checks if email and username are valid and match what is stored in DB
        public static bool CheckEmail(string username, string email)
        {
            //server side input validation
            if (!ValidateUsername.IsMatch(username) | !ValidateEmail.IsMatch(email))
            {
                return false;
            }
            string salt = GetSaltFromEmail(username); //extract salt from stored email.
            User user = SQLConnection.GetUser(username); //Get user record

            if (CompareHashes(HashFromSalt(email, salt), user.name))
            {
                return true;
            }
            
            return false;
        }

        //Generates and emails reset token to account email
        public static bool GeneratePasswordReset(string username, string email)
        {
            if (!CheckEmail(username, email))
            {
                Console.WriteLine("Email not a match");
                return false;
            }

            User user = SQLConnection.GetUser(username);
            if (user.userID == -1) //if user not found
            {
                //empty user rec
                return false;
            }

            Token token = new Token(username, 16, 6); //new Token vaild for 6 Hours
            Token.resetTokens.Add(token); //add to valid tokem list

            var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential("rereview.4319@gmail.com", smtpPassword),
                EnableSsl = true,
            };

            smtpClient.Send("rereview.4319@gmail.com", email, "Re-Review Password Reset", string.Format(resetEmailBody, token.key));
            return true;
        }
        //Reset password using token
        public static bool ResetPassword(string username, string token, string newPassword)
        {
            //server side input validation
            if (!ValidateUsername.IsMatch(username))
            {
                return false;
            }

            if (Token.FindResetToken(token, username)) //If token is valid
            {
                SQLConnection.UpdateUser(username, newPassword); //update password
                Token.RemoveResetToken(token); //removed used token
                return true;
            }

            return false;
        }
    }
}
