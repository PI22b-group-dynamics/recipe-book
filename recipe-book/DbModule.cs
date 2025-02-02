﻿using recipe_book.Properties;
using System.Data.SQLite;

namespace recipe_book
{
    internal static class DbModule
    {
        public static readonly SQLiteConnection Conn;

        static DbModule()
        {
            DirectoryInfo dir = new(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "recipe-book"
            ));
            if (!dir.Exists)
                dir.Create();
            Conn = new SQLiteConnection($"""Data Source = {Path.Combine(dir.FullName, "Database.sqlite")}""");
            Conn.Open();

            SQLiteCommand cmd = CreateCommand(Resources.InitDB); 
            cmd.ExecuteNonQuery();
        }

        public static SQLiteCommand CreateCommand(string query, params SQLiteParameter[] parameters)
        {
            SQLiteCommand cmd = Conn.CreateCommand();
            cmd.CommandText = query;
            cmd.Parameters.AddRange(parameters);
            return cmd;
        }
    }
}
