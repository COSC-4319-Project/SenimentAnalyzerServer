using System;
using System.Collections.Generic;
using System.Reflection;
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

            cmd.CommandText = "CREATE TABLE IF NOT EXISTS login(sId int NOT NULL AUTO_INCREMENT, sName varchar(50), sUser varchar(20), sPassword varchar(255), primary key(sId))";
            //Console.WriteLine(cmd.CommandText);
            cmd.ExecuteNonQuery();

            cmd.CommandText = @"CREATE TABLE IF NOT EXISTS reviews(numRev int, numPos int, numNeg int, adjustedRating float, confidence float, UId int, asinID varchar(10), IDDate Date, prodName varchar(255), origRating float, primary key(UId,asinID))";
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


        public static User GetUser(string username)
        {
            User user = new User();
            string command = "SELECT * FROM login WHERE sUser= @Username";
            cmd = new MySqlCommand(command, con);
            cmd.Parameters.Add(new MySqlParameter("@Username", username)); //Parameters to protect against SQL injection.
            rdr = cmd.ExecuteReader();

            if (rdr.HasRows)
            {
                rdr.Read();
                user.userID = rdr.GetInt32("sID");
                user.username = rdr.GetString("sUser");
                user.password = rdr.GetString("sPassword");
                user.name = rdr.GetString("sName");
                rdr.Close();
            }
            else
            {
                user.userID = -1; //indicate empty rec
                rdr.Close();
            }
            
            return user;
        }

        public static void CreateUser(User user)
        {
            //Create command object
            string cmdText = "INSERT INTO login(sName, sUser, sPassword) VALUES (@name, @username, @password)";
            cmd = new MySqlCommand(cmdText, con);
            //Add Parameters
            cmd.Parameters.Add(new MySqlParameter("@name", user.name));
            cmd.Parameters.Add(new MySqlParameter("@username", user.username));
            cmd.Parameters.Add(new MySqlParameter("@password",user.password));
            //Execute table addition
            cmd.ExecuteNonQuery();
        }

        public static void UpdateUser(string username, string newPassword)
        {
            string cmdText = "UPDATE login SET sPassword= @password WHERE sUser= @username";
            cmd = new MySqlCommand(cmdText, con);
            cmd.Parameters.Add(new MySqlParameter("@password", newPassword));
            cmd.Parameters.Add(new MySqlParameter("@username", username));
            cmd.ExecuteNonQuery();
        }

        //reviews(sentVal int, numRev int, numPos int, numNeg int, adjustedRating float, confidence float, UId int, ASINID varchar(10), IDDate Date, foreign key(UId) references login(sID), primary key(UId,ASINID))
        public static HistoryRec GetHistoryRec(string productID)
        {
            HistoryRec rec = new HistoryRec();
            string cmdText = "SELECT * FROM reviews WHERE asinID= @productID";
            cmd = new MySqlCommand(cmdText, con);
            cmd.Parameters.Add(new MySqlParameter("@productID", productID));
            rdr = cmd.ExecuteReader();

            if (rdr.Read())
            {
                rec.asinID = rdr.GetString("asinID");
                rec.productName = rdr.GetString("prodName");
                rec.numRev = rdr.GetInt32("numRev");
                rec.numPos = rdr.GetInt32("numPos");
                rec.numNeg = rdr.GetInt32("numNeg");
                rec.adjustedRating = rdr.GetFloat("adjustedRating");
                rec.confidence = rdr.GetFloat("confidence");
                rec.uID = rdr.GetInt32("UId");
                rec.dateAnalyzed = rdr.GetDateTime("IDDate");
                rec.origRating = rdr.GetFloat("origRating");
            }
            else //if not found
            {
                rec.numRev = -1; //To indicate no record found
            }

            rdr.Close();
            return rec;
        }

        public static void CreateHistoryRec(HistoryRec rec)
        {
            // reviews(numRev int, numPos int, numNeg int, adjustedRating float, confidence float, UId int, asinID varchar(10), IDDate Date, prodName varchar(255), origRating float, primary key(UId,asinID))
            string cmdText = "INSERT INTO reviews(numRev, numPos, numNeg, adjustedRating, confidence, UId, asinID, IDDate,prodName, origRating) VALUES (@numRev, @numPos, @numNeg, @adjustedRating, @confidence, @UId, @asinID, STR_TO_DATE(@IDdate,'%m/%d/%Y %h:%i:%s %p'), @prodName, @origRating)";
            //cmd.CommandText = string.Format("INSERT INTO reviews(numRev, numPos, numNeg, adjustedRating, confidence, UId, asinID, IDDate,prodName, origRating) VALUES ('{0}','{1}','{2}','{3}','{4}','{5}','{6}',STR_TO_DATE('{7}','%m/%d/%Y %h:%i:%s %p'),'{8}','{9}')", rec.numRev,rec.numPos,rec.numNeg,rec.adjustedRating,rec.confidence,rec.uID,rec.asinID,rec.dateAnalyzed,rec.productName,rec.origRating);
            cmd = new MySqlCommand(cmdText, con);
            //Parameters
            cmd.Parameters.Add(new MySqlParameter("@numRev", rec.numRev));
            cmd.Parameters.Add(new MySqlParameter("@numPos", rec.numPos));
            cmd.Parameters.Add(new MySqlParameter("@numNeg", rec.numNeg));
            cmd.Parameters.Add(new MySqlParameter("@adjustedRating", rec.adjustedRating));
            cmd.Parameters.Add(new MySqlParameter("@confidence", rec.confidence));
            cmd.Parameters.Add(new MySqlParameter("@UId", rec.uID));
            cmd.Parameters.Add(new MySqlParameter("@asinID", rec.asinID));
            cmd.Parameters.Add(new MySqlParameter("@IDdate", rec.dateAnalyzed));
            cmd.Parameters.Add(new MySqlParameter("@prodName", rec.productName));
            cmd.Parameters.Add(new MySqlParameter("@origRating", rec.origRating));

            cmd.ExecuteNonQuery();
        }

        public static HistoryRec[] GetUserHistory(int uID)
        {
            List<HistoryRec> recList = new List<HistoryRec>();
            string cmdText = "SELECT * FROM reviews WHERE UId= @Uid";
            cmd = new MySqlCommand(cmdText, con);
            cmd.Parameters.Add(new MySqlParameter("@Uid", uID));

            rdr = cmd.ExecuteReader();

            while (rdr.Read())
            {
                HistoryRec rec = new HistoryRec();
                rec.asinID = rdr.GetString("asinID");
                rec.productName = rdr.GetString("prodName");
                rec.numRev = rdr.GetInt32("numRev");
                rec.numPos = rdr.GetInt32("numPos");
                rec.numNeg = rdr.GetInt32("numNeg");
                rec.adjustedRating = rdr.GetFloat("adjustedRating");
                rec.confidence = rdr.GetFloat("confidence");
                rec.uID = rdr.GetInt32("UId");
                rec.dateAnalyzed = rdr.GetDateTime("IDDate");
                rec.origRating = rdr.GetFloat("origRating");
                recList.Add(rec);
            }

            return recList.ToArray();
        }

        public static void DeleteHistoryRec(string asin)
        {
            string cmdText = "DELETE FROM reviews WHERE asinID= @asin";
            cmd = new MySqlCommand(cmdText, con);
            cmd.Parameters.Add(new MySqlParameter("@asin", asin));
            cmd.ExecuteNonQuery();
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

        public User(string userName, string password, int userID, string name)
        {
            this.username = userName;
            this.password = password;
            this.userID = userID;
            this.name = name;
        }
    }
    struct HistoryRec
    {
        public string asinID;
        public string productName;
        public int numRev;
        public int numNeg;
        public int numPos;
        public float confidence;
        public float adjustedRating;
        public float origRating;
        public int uID; 
        public DateTime dateAnalyzed;
        //AHIS|asin|uID|adjRat|productName|numRev|numPos|numNeg|connfidence|dateAnalyzed|origRating
        public HistoryRec(string[] message) //Parse response from server into record
        {
            asinID = message[1];
            uID = int.Parse(message[2]);
            adjustedRating = float.Parse(message[3]);
            productName = message[4];
            numRev = int.Parse(message[5]);
            numPos = int.Parse(message[6]);
            numNeg = int.Parse(message[7]);
            confidence = float.Parse(message[8]);
            dateAnalyzed = DateTime.Parse(message[9]);
            origRating = float.Parse(message[10]);
        }
    }
}
