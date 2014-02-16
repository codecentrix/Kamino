using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Data.SqlServerCe;
using System.Xml;
using System.IO;
using Microsoft.Win32;


namespace wrexpt
{
    static class Program
    {
        /// The main entry point for the application.
        [STAThread]
        static void Main(string[] args)
        {
            if ((args == null) || (args.Length == 0))
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new FormExport());
            }
            else
            {
                int result = Program.Run(args);
                Environment.Exit(result);
            }
        }

        private static int Run(string[] args)
        {
            if (!ParseCmdLine(args))
            {
                ShowUsageHelp();
                return ERR_INVALID_CMD_LINE;
            }

            if (!File.Exists(Program.wrDbFileName))
            {
                System.Console.WriteLine("ERROR: cannot find database file: " + Program.wrDbFileName);
                return ERR_INVALID_FILE_PATH;
            }

            try
            {
                ExportDB(Program.wrDbFileName, Program.outFile, Program.password);
            }
            catch (Exception)
            {
                return ERR_EXPORT_FAILURE;
            }

            return ERR_OK;
        }

        public static string GetDBPath()
        {
            RegistryKey regkey = null;

            try
            {
                String keyPath = null;
                if (Environment.OSVersion.Version.Major >= 6)
                {
                    keyPath = @"Software\AppDataLow\Deskperience\WebReplay";
                }
                else
                {
                    keyPath = @"Software\Deskperience\WebReplay";
                }

                regkey = Registry.CurrentUser.OpenSubKey(keyPath);
                String dbPath = (String)regkey.GetValue("StorageCurrentPath");

                return dbPath;
            }
            catch (Exception)
            {

            }
            finally
            {
                if (regkey != null)
                {
                    regkey.Close();
                }
            }

            return null;
        }

        private static bool ParseCmdLine(string[] args)
        {
            foreach (string arg in args)
            {
                if (arg.Length < 4)
                {
                    return false;
                }

                if (arg.StartsWith("/f:"))
                {
                    Program.wrDbFileName = arg.Substring(3);
                }
                else if (arg.StartsWith("/p:"))
                {
                    Program.password = arg.Substring(3);
                }
                else if (arg.StartsWith("/o:"))
                {
                    Program.outFile = arg.Substring(3);
                }
                else
                {
                    return false;
                }
            }

            if (Program.wrDbFileName == null)
            {
                // No database provided. Try to find WR database from WR registry settings.
                String srDbDir = GetDBPath();
                Program.wrDbFileName = (srDbDir != null ? Path.Combine(srDbDir, "WR.sdf") : null);

                if (Program.wrDbFileName == null)
                {
                    // Cannot find WR databse.
                    return false;
                }
            }

            if (Program.outFile == null)
            {
                Program.outFile = Program.wrDbFileName + ".xml";
            }

            return true;
        }

        private static void ShowUsageHelp()
        {
            System.Console.WriteLine("Exports data from WebReplay database in XML format.");
            System.Console.WriteLine("Usage: WREXPT [/f:dbfile] [/p:password] [/o:outfile]\n");
            System.Console.WriteLine("dbfile  - full path for wr.sdf database; default is the current WebReplay database");
            System.Console.WriteLine("password- database password; default is empty string");
            System.Console.WriteLine("outfile - full path for output XML file; default value is dbfile + .xml\n");
        }

        public static void ExportDB(String dbFileName, String outFileName, String pwd)
        {
            System.Console.WriteLine("Start exporting database");

            SqlCeConnection sqlconnection = null;
            XmlDocument doc = new XmlDocument();

            try
            {
                System.Console.WriteLine("Open database " + dbFileName);

                // Create XML document.
                doc = new XmlDocument();
                XmlElement root = doc.CreateElement("webreplay");
                doc.AppendChild(root);

                // Open database.
                string connection = String.Format("Persist Security Info=False; Data Source = \"{0}\"; Password = \"{1}\"; Encrypt = TRUE;",
                                                  dbFileName, pwd);

                sqlconnection = new SqlCeConnection();
                sqlconnection.ConnectionString = connection;
                sqlconnection.Open();

                ExportLogins(sqlconnection, doc);
                ExportNotes(sqlconnection, doc);
                ExportBookmarks(sqlconnection, doc);

                // Save XML file.
                doc.Save(outFileName);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex);
                throw;
            }
            finally
            {
                if (sqlconnection != null)
                {
                    sqlconnection.Close();
                }
            }

            System.Console.WriteLine("Export complete");
        }

        private static void ExportBookmarks(SqlCeConnection conn, XmlDocument doc)
        {
            System.Console.WriteLine("Export bookmarks");

            SqlCeDataReader dr = null;
            XmlDocument taskDoc = new XmlDocument();
            int count = 0;

            try
            {
                SqlCeCommand sqlcommand = new SqlCeCommand(@"SELECT TaskName, Script FROM Tasks WHERE (IsDeleted=0) AND (Flags=4) ORDER BY TaskName");
                sqlcommand.Connection = conn;
                dr = sqlcommand.ExecuteReader();

                while (dr.Read())
                {
                    XmlElement bookmark = doc.CreateElement("bookmark");
                    bookmark.InnerText = dr["TaskName"].ToString();

                    String script = dr["Script"].ToString();
                    taskDoc.LoadXml(script);
                    bookmark.SetAttribute("url", taskDoc.DocumentElement.GetAttribute("URL"));

                    doc.DocumentElement.AppendChild(bookmark);
                    count++;
                }
            }
            finally
            {
                if (dr != null)
                {
                    dr.Close();
                }
            }

            System.Console.WriteLine(count + " bookmarks exported");
        }

        private static void ExportNotes(SqlCeConnection conn, XmlDocument doc)
        {
            System.Console.WriteLine("Export safe notes");

            SqlCeDataReader dr = null;
            int count = 0;

            try
            {
                SqlCeCommand sqlcommand = new SqlCeCommand(@"SELECT SafeName, SafeNote FROM SafeNotes WHERE (IsDeleted=0) ORDER BY SafeName");
                sqlcommand.Connection = conn;
                dr = sqlcommand.ExecuteReader();

                while (dr.Read())
                {
                    XmlElement note = doc.CreateElement("note");
                    note.InnerText = dr["SafeNote"].ToString();
                    note.SetAttribute("name", dr["SafeName"].ToString());

                    doc.DocumentElement.AppendChild(note);
                    count++;
                }
            }
            finally
            {
                if (dr != null)
                {
                    dr.Close();
                }
            }

            System.Console.WriteLine(count + " safe notes exported");
        }

        private static void ExportLogins(SqlCeConnection conn, XmlDocument doc)
        {
            System.Console.WriteLine("Export logins");

            SqlCeDataReader dr = null;
            int count = 0;

            try
            {
                SqlCeCommand sqlcommand = new SqlCeCommand(
                    @"SELECT DISTINCT L.LoginName, LS.SiteFullName, L.Note, L.UserName1, L.UserName2, L.Password1, L.Password2, LS.UserDesc1, LS.UserDesc2, LS.PasswordDesc1, LS.PasswordDesc2 " +
                    @"FROM LoginSites AS LS INNER JOIN Logins AS L ON LS.LoginID = L.LoginID " +
                    @"WHERE L.IsDeleted = 0");
                sqlcommand.Connection = conn;
                dr = sqlcommand.ExecuteReader();

                while (dr.Read())
                {
                    XmlElement login = doc.CreateElement("login");
                    login.InnerText = dr["Note"].ToString();
                    login.SetAttribute("pwd2RuntimeID", dr["PasswordDesc2"].ToString());
                    login.SetAttribute("user2RuntimeID", dr["UserDesc2"].ToString());
                    login.SetAttribute("pwd1RuntimeID", dr["PasswordDesc1"].ToString());
                    login.SetAttribute("user1RuntimeID", dr["UserDesc1"].ToString());
                    login.SetAttribute("password2", dr["Password2"].ToString());
                    login.SetAttribute("user2", dr["UserName2"].ToString());
                    login.SetAttribute("password", dr["Password1"].ToString());
                    login.SetAttribute("user", dr["UserName1"].ToString());
                    login.SetAttribute("site", dr["SiteFullName"].ToString());
                    login.SetAttribute("name", dr["LoginName"].ToString());

                    doc.DocumentElement.AppendChild(login);
                    count++;
                }
            }
            finally
            {
                if (dr != null)
                {
                    dr.Close();
                }
            }

            System.Console.WriteLine(count + " logins exported");
        }

        static string wrDbFileName;
        static string password = "";
        static string outFile;

        const int ERR_OK = 0;
        const int ERR_INVALID_CMD_LINE = 1;
        const int ERR_INVALID_FILE_PATH = 2;
        const int ERR_EXPORT_FAILURE = 3;
    }
}
