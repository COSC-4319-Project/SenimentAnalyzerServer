using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenimentAnalyzerServer
{
    class SQLConnection
    {
        public static User GetUser(string username)
        {
            User user = new User();
            //SQL DATABASE: Get User info from database 
            return user;
        }

        public static void CreateUser(User user)
        {
            //SQL DATABASE: Add User info to database 
        }

        public static void UpdateUser(User user, string newPassword)
        {
            //SQL DATABASE: Update password entry in database
        }

        public static HistoryRec GetHistory(string productID)
        {
            HistoryRec rec = new HistoryRec();

            //SQL DATABASE: Get History info from database for product id if found;

            //if not found
            rec.numReviews = -1; //To indicate no record found
            return rec;
        }

        public static void DeleteUser(string userName)
        {
            //SQL DATABASE: delete user entry in database
        }
    }

    struct User
    {
        public string username;
        public string password;
    }
    struct HistoryRec
    {
        public string productID;
        public float modifedRating;
        public int numReviews;
        public DateTime dateAnalyzed;
    }
}
