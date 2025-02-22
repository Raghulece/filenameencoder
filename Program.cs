using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using System.Data.SQLite;
using filerename.v1.Repository;
using System.Text.RegularExpressions;


namespace filerename.v1
{
    internal class Program
    {
        static string dbPath = "file_mapping.db";
        static string logPath = "rename_log.json";
        //Change the sal value to anything
        static int salt = 007;

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

                if (IsBase64Encoded(fileName))
                {
                    Console.WriteLine($"Skipping already Base64-encoded file: {fileName}");
                    continue;
                }

                //string encodedName = Convert.ToBase64String(Encoding.UTF8.GetBytes(fileName));
                string encodedName = EncodeWithSalt(fileName, salt);
                string newFilePath = Path.Combine(inputFolder, encodedName);
                string originalFilepath = Path.Combine(inputFolder, fileName);

                Console.WriteLine($"{fileName} -> {encodedName}");

                SaveToDatabase(fileName, encodedName, originalFilepath);
                logEntries.Add(new FileLog { OriginalName = fileName, NewName = encodedName, Timestamp = DateTime.Now, Filepath = originalFilepath });

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
                using var cmd = new SQLiteCommand("CREATE TABLE Files (OriginalName TEXT, EncodedName TEXT, Truefilepath TEXT)", conn);
                cmd.ExecuteNonQuery();
            }
        }

        static void SaveToDatabase(string originalName, string encodedName, string truefilepath)
        {
            using var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;");
            conn.Open();
            using var cmd = new SQLiteCommand("INSERT INTO Files (OriginalName, EncodedName, Truefilepath) VALUES (@orig, @enc, @truefilepath)", conn);
            cmd.Parameters.AddWithValue("@orig", originalName);
            cmd.Parameters.AddWithValue("@enc", encodedName);
            cmd.Parameters.AddWithValue("@truefilepath", truefilepath);
            cmd.ExecuteNonQuery();
        }

        static bool IsBase64Encoded(string str)
        {
            return Regex.IsMatch(str, "^[A-Za-z0-9+/]+={0,2}$") && (str.Length % 4 == 0);
        }
        static string EncodeWithSalt(string input, int offset)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(input);
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)((bytes[i] + offset) % 256);
            }
            string base64 = Convert.ToBase64String(bytes);
            return base64.Replace("/", "_").Replace("+", "-");
        }

    }


}
