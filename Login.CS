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

        //returns Salted and hashed password
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

        //Checks valid login then stores new salted hashed in DB
        public static bool ChangePassword(string username, string currentPass, string newPass)
        {
            //server side input validation
            if (!ValidatePassword.IsMatch(newPass) || !ValidateUsername.IsMatch(username) || !ValidatePassword.IsMatch(currentPass))
            {
                return false;
            }

            if (CheckLogin(username, currentPass))
            {
                string saltedHash = SaltedHash(newPass);
                //SQL command to store new saltedhash under username
                SQLConnection.UpdateUser(username, saltedHash);
            }

            return false;
        }

        //creates an account record returns false if user already exists
        public static bool CreateAccount(string username, string password, string name)
        {
            //server side input validation
            if (!ValidatePassword.IsMatch(password) || !ValidateUsername.IsMatch(username) || !ValidateEmail.IsMatch(name))
            {
                return false;
            }

            if (!CheckUsername(username)) //if user is not in DB
            {
                User user = new User(username, SaltedHash(password), 0, name);
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

        //Generates and emails reset token to account email
        public static bool GeneratePasswordReset(string userName)
        {
            //server side input validation
            if (!ValidateUsername.IsMatch(userName))
            {
                return false;
            }

            User user = SQLConnection.GetUser(userName);
            if (user.userID == -1) //if user not found
            {
                //empty user rec
                return false;
            }

            Token token = new Token(userName, 16, 6); //new Token vaild for 6 Hours
            Token.resetTokens.Add(token); //add to valid tokem list

            var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential("rereview.4319@gmail.com", smtpPassword),
                EnableSsl = true,
            };

            smtpClient.Send("rereview.4319@gmail.com", user.name, "Re-Review Password Reset", string.Format(resetEmailBody, token.key));
            return true;
        }
        //Reset password using token
        public static bool ResetPassword(string username, string token, string newPassword)
        {
            //server side input validation
            if (!ValidatePassword.IsMatch(newPassword) || !ValidateUsername.IsMatch(username))
            {
                return false;
            }

            if (Token.FindResetToken(token, username)) //If token is valid
            {
                SQLConnection.UpdateUser(username, SaltedHash(newPassword)); //update password
                Token.RemoveResetToken(token); //removed used token
                return true;
            }

            return false;
        }
    }
}
