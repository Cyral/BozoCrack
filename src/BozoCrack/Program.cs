///////////////////////////////////////////////////////////////////
//   BozoCrack C# by Cyral: https://github.com/Cyral/BozoCrack   //
///////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BozoCrack
{
    class Program
    {
        private const string
            extension = ".txt",
            exitPrompt = "Press any key to exit...",
            searchUrl = "http://www.google.com/search?q=";

        private static readonly char[] splitChars = new char[] {' ','\t', '/', '=', '<', '>', ':'}; //Spaces, HTML, and Separators
        private static readonly Regex regex = new Regex("[0-9a-fA-F]{32}"); //MD5 Regex

        private static Dictionary<string, string> table;
        private static List<string> hashes;
        private static WebClient webClient;
        private static StringBuilder stringBuilder;
        private static string path, md5;

        static void Main(string[] args)
        {
            //Setup
            table = new Dictionary<string, string>();
            hashes = new List<string>();

            stringBuilder = new StringBuilder();
            webClient = new WebClient() { Proxy = null };

            FindPath(args); //Get MD5 or filename from command line, or have user input it
            CheckPath(); //Check that the file is valid
            ExtractHashes(); //Get hashes from file
            CrackHashes(); //For each hash found, search the web for it's original string

            Exit("\nFinished.\n");
        }

        private static string CalculateMD5Hash(string input)
        {
            //Calculate MD5 hash
            MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] hash = md5.ComputeHash(System.Text.Encoding.ASCII.GetBytes(input));

            //Convert to traditional hex string
            stringBuilder.Clear();
            for (int i = 0; i < hash.Length; i++)
                stringBuilder.Append(hash[i].ToString("x2"));
            return stringBuilder.ToString();
        }

        private static void FindPath(string[] args)
        {
            //Get filename or MD5 from command line, or have user input it
            string input = string.Empty;
            if (args.Length == 1)
                input = args[0];
            else if (args.Length > 1) {
                Exit("Too many arguments.");
            }
            else {
                Console.WriteLine("Input MD5 or path to MD5 list file (*{0})", extension);
                input = Console.ReadLine();
            }

            //Check to see if input was a file or MD5 hash
            if (regex.Match(input).Success)
                md5 = input;
            else
                path = input;
        }

        private static void CheckPath()
        {
            //Check that the file is valid
            if (string.IsNullOrWhiteSpace(path)) //If path is empty, we are looking for a single MD5, not a file
                return;
            if (!File.Exists(path))
                Exit("File not found or invalid MD5.");
            else if (!string.Equals(Path.GetExtension(path), extension))
                Exit(string.Format("File of wrong extension, must be {0}", extension));
        }

        private static void ExtractHashes()
        {
            //Get hashes from file
            string[] lines = string.IsNullOrWhiteSpace(path) ? new string[] { md5 } : File.ReadLines(path).ToArray<string>(); //If path is empty, user inputed a single MD5 instead of a file
            foreach (string line in lines)
            {
                Match match = regex.Match(line);
                if (match.Success)
                {
                    string value = match.Groups[0].Value; //Extracted MD5 without the rest of the line
                    if (!hashes.Contains(value)) //Don't add same value twice
                        hashes.Add(value);
                }
            }

            if (hashes.Count == 0)
                Exit("\nNo MD5 hashes found.");
            else
                Console.WriteLine("\n{0} unique {1} found.\n", hashes.Count, hashes.Count == 1 ? "hash" : "hashes");
        }


        private static void CrackHashes()
        {
            //For each hash found, search the web for it's original string
            foreach (string hash in hashes)
            {
                string password = CrackHash(hash); //Try to find hash, and add to hash table
                table.Add(hash, password);
                PrintHash(hash);
            }
        }

        private static string CrackHash(string hash)
        {
            //Search the web for the hash, and split all of the results by spaces and a few other characters
            string url = stringBuilder.Clear().Append(searchUrl).Append(hash).ToString(); //Create url
            string response = Encoding.UTF8.GetString(webClient.DownloadData(url)); //Send query
            string[] wordlist = response.Split(splitChars, StringSplitOptions.RemoveEmptyEntries); //Split response

            return DictionaryAttack(hash, wordlist);
        }

        private static string DictionaryAttack(string hash, string[] wordlist)
        {
            //Check if any of the strings on the page, after MD5, are equal to the hash
            foreach (string word in wordlist)
                if (string.Equals(CalculateMD5Hash(word), hash, StringComparison.OrdinalIgnoreCase))
                    return word;
            return string.Empty; //If not found
        }

        private static void PrintHash(string hash)
        {
            //Print a single hash and original password
            Console.ForegroundColor = ConsoleColor.Green;

            string password = table[hash]; //Fetch password from the table
            string result = stringBuilder.Clear().Append(hash).Append(":").Append(password).ToString();

            Console.WriteLine(result);
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        private static void PrintHashes()
        {
            //Method not used in this example, can be used to print all of the hashes and passwords to a file or console
            foreach (KeyValuePair<string, string> pair in table)
            {
                string answer = stringBuilder.Clear().Append(pair.Key).Append(":").Append(pair.Value).ToString();
                Console.WriteLine(answer);
            }
        }

        private static void Exit(string reason, ConsoleColor color = ConsoleColor.Red)
        {
            //Exit the application after user input with reason and color
            Console.ForegroundColor = color;
            Console.WriteLine(reason);
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(exitPrompt);
            Console.ReadKey();
            Environment.Exit(0);
        }
    }
}
