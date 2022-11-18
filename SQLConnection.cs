﻿using System;
using System.Collections.Generic;
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
            cmd.Parameters.Add(new MySqlParameter("@Username", username));
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
            cmd.CommandText = "INSERT INTO login(sName, sUser, sPassword) VALUES ('" + user.name + "','" + user.username + "','" + user.password + "')";
            cmd.ExecuteNonQuery();
        }

        public static void UpdateUser(string username, string newPassword)
        {
            cmd.CommandText = string.Format("UPDATE login SET sPassword='{0}' WHERE sUser='{1}'", newPassword, username);
            //cmd.CommandText = "UPDATE login Set sPassword='" + newPassword + "' WHERE sUser='" + newPassword + "'";
            cmd.ExecuteNonQuery();
        }

        //reviews(sentVal int, numRev int, numPos int, numNeg int, adjustedRating float, confidence float, UId int, ASINID varchar(10), IDDate Date, foreign key(UId) references login(sID), primary key(UId,ASINID))
        public static HistoryRec GetHistoryRec(string productID)
        {
            HistoryRec rec = new HistoryRec();
            cmd.CommandText = string.Format("SELECT * FROM reviews WHERE asinID='{0}'", productID);
            //cmd.CommandText = "SELECT * FROM reviews WHERE asinID=" + productID;
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
            cmd.CommandText = string.Format("INSERT INTO reviews(numRev, numPos, numNeg, adjustedRating, confidence, UId, asinID, IDDate,prodName, origRating) VALUES ('{0}','{1}','{2}','{3}','{4}','{5}','{6}',STR_TO_DATE('{7}','%m/%d/%Y %h:%i:%s %p'),'{8}','{9}')", rec.numRev,rec.numPos,rec.numNeg,rec.adjustedRating,rec.confidence,rec.uID,rec.asinID,rec.dateAnalyzed,rec.productName,rec.origRating);
            //cmd.CommandText = "INSERT INTO reviews(sentVal, numRev, numPos, numNeg, adjustedRating, confidence, UId, ASINID, IDDate) VALUES ('" + rec.sentimentVal + "'," + rec.numRev + "','" + rec.numPos + "','" + rec.numNeg + "','" + rec.adjustedRating + "','" + rec.confidence + "','" + rec.uID + "','" + rec.asinID + "','" + rec.dateAnalyzed + "')";
            cmd.ExecuteNonQuery();
        }

        public static HistoryRec[] GetUserHistory(int uID)
        {
            List<HistoryRec> recList = new List<HistoryRec>();
            cmd.CommandText = string.Format("SELECT * FROM reviews WHERE UId='{0}'", uID);
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
            cmd.CommandText = string.Format("DELETE FROM reviews WHERE asinID='{0}'", asin);
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
