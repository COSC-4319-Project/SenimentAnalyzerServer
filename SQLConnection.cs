using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace SenimentAnalyzerServer
{
    class SQLConnection
    {
        private static string server = "localhost";
        private static string userid = "sent";
        private static string password = "password";
        private static string database = "review";

        private static MySqlConnection con;
        private static MySqlCommand cmd;

        private static MySqlDataReader rdr;

        public static void InitializeDatabase()
        {
            cmd = new MySqlCommand();
            cmd.Connection = con;

            cmd.CommandText = "CREATE TABLE IF NOT EXISTS login(sId int , sName varchar(40), sUser varchar(20), sPassword varchar(255), primary key(sUser, sID))";
            cmd.ExecuteNonQuery();

            cmd.CommandText = @"CREATE TABLE IF NOT EXISTS reviews(sentVal int, numRev int, numPos int, numNeg int, adjustedRating float, confidence float, UId int, ASINID varchar(10), IDDate Date, foreign key(UId) references login(sID), primary key(UId,ASINID))";
            cmd.ExecuteNonQuery();
        }

        public static bool AttemptSQLConnection()
        {
            string cs = "server=" + server + ";uid=" + userid + ";pwd=" + password + ";database=" + database;
            //string myConnectionString = "server=127.0.0.1;uid=sent;" + "pwd=password;database=review";
            //string cs = string.Format("Server={0}; database={1}; UID={2}; password={3}", server, database, userid, password);
            con = new MySqlConnection(cs);
            try
            {
                con.Open();
                Console.WriteLine("Connected to SQL Database Initializing Tables:");
                InitializeDatabase();
                Console.WriteLine("Tables Initialized");
                return true;
            }
            catch (MySqlException e)
            {
                Console.WriteLine("Error: Unable to connect to SQL database. Check login information and server status.");
                Console.WriteLine(e.ToString());
                return false;
            }
        }
        public static void CloseConnection()
        {
            if (con != null)
            {
                con.Close();
            }
        }


        public static User GetUser(string username)
        {
            User user = new User();
            cmd.CommandText = "SELECT * FROM login WHERE sUser=" + username;
            rdr = cmd.ExecuteReader();

            if (rdr.HasRows)
            {
                rdr.Read();
                user.userID = rdr.GetInt32("sID");
                user.username = rdr.GetString("sUser");
                user.password = rdr.GetString("sPassword");
                rdr.Close();
            }
            else
            {
                rdr.Close();
            }

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
            rec.numRev = -1; //To indicate no record found
            return rec;
        }

        public static void DeleteUser(string userName)
        {
            //SQL DATABASE: delete user entry in database
        }
    }

    struct User
    {
        public int userID;
        public string username;
        public string password;
        public string name;
    }
    struct HistoryRec
    {
        public string asinID;
        public int sentimentVal;
        public int numRev;
        public int numNeg;
        public int numPos;
        public float confidence;
        public float adjustedRating;
        public DateTime dateAnalyzed;
    }
}
