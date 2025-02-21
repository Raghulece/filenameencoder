using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using System.Data.SQLite;
using filerename.v1.Repository;


namespace filerename.v1
{
    internal class Program
    {
        static string dbPath = "file_mapping.db";
        static string logPath = "rename_log.json";

        static void Main()
        {
            Console.Write("Enter the input folder path: ");
            string inputFolder = Console.ReadLine();

            if (!Directory.Exists(inputFolder))
            {
                Console.WriteLine("Invalid folder path.");
                return;
            }

            InitializeDatabase();
            List<FileLog> logEntries = new List<FileLog>();

            foreach (var file in Directory.GetFiles(inputFolder))
            {
                string fileName = Path.GetFileName(file);
                string encodedName = Convert.ToBase64String(Encoding.UTF8.GetBytes(fileName));
                string newFilePath = Path.Combine(inputFolder, encodedName);

                Console.WriteLine($"{fileName} -> {encodedName}");

                SaveToDatabase(fileName, encodedName);
                logEntries.Add(new FileLog { OriginalName = fileName, NewName = encodedName, Timestamp = DateTime.Now });

                File.Move(file, newFilePath);
            }

            File.WriteAllText(logPath, JsonSerializer.Serialize(logEntries, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine("Renaming complete. Changes logged in rename_log.json");
        }

        static void InitializeDatabase()
        {
            if (!File.Exists(dbPath))
            {
                SQLiteConnection.CreateFile(dbPath);
                using var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;");
                conn.Open();
                using var cmd = new SQLiteCommand("CREATE TABLE Files (OriginalName TEXT, EncodedName TEXT)", conn);
                cmd.ExecuteNonQuery();
            }
        }

        static void SaveToDatabase(string originalName, string encodedName)
        {
            using var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;");
            conn.Open();
            using var cmd = new SQLiteCommand("INSERT INTO Files (OriginalName, EncodedName) VALUES (@orig, @enc)", conn);
            cmd.Parameters.AddWithValue("@orig", originalName);
            cmd.Parameters.AddWithValue("@enc", encodedName);
            cmd.ExecuteNonQuery();
        }
    }


}
