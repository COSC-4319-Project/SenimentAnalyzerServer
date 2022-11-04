//Tilly Dewing Fall 2022
//Software Engineering 4319

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace SenimentAnalyzerServer
{
    class Login
    {

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
    }
}
