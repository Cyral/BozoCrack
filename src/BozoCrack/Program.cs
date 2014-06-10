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
            exitString = "exit",
            placeholderString = "<hash>",
            searchFile = "search.txt",
            resultsFile = "results.txt",
            commentString = @"#",
            defaultSearchUrls = "#List of websites to use to search for hashes\nhttp://www.md5-hash.com/md5-hashing-decrypt/\n#https://www.google.com/search?q=";

        private static readonly char[] splitChars = new char[] {' ','\t', '/', '=', '<', '>', ':'}; //Spaces, HTML, and Separators
        private static readonly Regex md5Regex = new Regex(@"[0-9a-fA-F]{32}"); //MD5 Regex
        private static readonly Regex urlRegex = new Regex(@"^(ht|f)tp(s?)\:\/\/[0-9a-zA-Z]([-.\w]*[0-9a-zA-Z])*(:(0-9)*)*(\/?)([a-zA-Z0-9\-\.\?\,\'\/\\\+&amp;=%\$#_]*)?$"); //Search regex

        private static Dictionary<string, string> table;
        private static List<string> hashes;
        private static List<string> websites;

        private static WebClient webClient;
        private static StringBuilder stringBuilder;
        private static string path, md5;
        private static bool error;

        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            //Setup
            table = new Dictionary<string, string>();
            hashes = new List<string>();
            websites = new List<string>();

            stringBuilder = new StringBuilder();
            webClient = new WebClient() { Proxy = null };

            FindSearchWebsite(); //Read or Create the file of search websites to look for MD5s

            Loop(args); //Main program code/loop
        }

        private static void Loop(string[] args)
        {
            string line = string.Empty; //If user types "exit" then close the application, otherwise loop
            while (line != exitString)
            {
                FindPath(args); //Get MD5 or filename from command line, or have user input it
                CheckInput(); //Check that the file is valid
                if (!error) { //If error occured with input, skip final steps and restart
                    ExtractHashes(); //Get hashes from file
                    CrackHashes(); //For each hash found, search the web for it's original string
                    SaveHashes();
                }
                error = false;

                Console.WriteLine("Type '{0}' to exit, press Enter to continue.", exitString);
                line = Console.ReadLine();
                Reset();
            }
        }

        private static void FindSearchWebsite()
        {
            //Read or Create the file of search websites to look for MD5s
            //Examples:
            //http://www.md5-hash.com/md5-hashing-decrypt/fcf1eed8596699624167416a1e7e122e
            //https://www.google.com/search?q=fcf1eed8596699624167416a1e7e122e

            //Find path of the website list
            string appDirectory = (new FileInfo(System.Reflection.Assembly.GetEntryAssembly().Location)).Directory.FullName;
            string path = Path.Combine(appDirectory, searchFile).ToString();

            //If it doesn't exit, create it
            if (!File.Exists(path)) {
                try {
                    File.Create(path).Close();
                    File.WriteAllLines(path, defaultSearchUrls.Split('\n'));
                }
                catch (IOException e) {
                    Console.WriteLine("Exception: {0}", e.Message);
                }
            }

            //Read websites from the file
            foreach (string line in File.ReadLines(path)) {
                Match match = urlRegex.Match(line);
                if (match.Success) {
                    string value = match.Groups[0].Value; //Url
                    if (!websites.Contains(value) && !value.StartsWith(commentString)) //Don't add same value twice, allow comments
                        websites.Add(value);
                }
            }

            if (websites.Count == 0)
                Error(string.Format("No websites specified in {0}", searchFile));
            else {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("{0} search site{1} loaded:", websites.Count, websites.Count == 1 ? string.Empty : "s");
                foreach (string site in websites)
                    Console.WriteLine(stringBuilder.Clear().Append(site).Append(placeholderString).ToString());
                Console.WriteLine("\n"); //Spacer
                Console.ForegroundColor = ConsoleColor.Gray;
            }
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
                Error("Too many arguments.");
            }
            else {
                Console.WriteLine("Input MD5 or path to MD5 list file (*{0})", extension);
                input = Console.ReadLine();
            }

            //Check to see if input was a file or MD5 hash
            if (md5Regex.Match(input).Success)
                md5 = input;
            else if (string.IsNullOrWhiteSpace(input))
                Error("You must input a file name or MD5 string.");
            else
                path = input;
        }

        private static void CheckInput()
        {
            //Check that the file is valid
            if (string.IsNullOrWhiteSpace(path)) //If path is empty, we are looking for a single MD5, not a file
                return;
            if (!File.Exists(path))
                Error("File not found or invalid MD5.");
            else if (!string.Equals(Path.GetExtension(path), extension))
                Error(string.Format("File of wrong extension, must be {0}", extension));
        }

        private static void ExtractHashes()
        {
            //Get hashes from file
            string[] lines = string.IsNullOrWhiteSpace(path) ? new string[] { md5 } : File.ReadLines(path).ToArray<string>(); //If path is empty, user inputed a single MD5 instead of a file
            foreach (string line in lines)
            {
                Match match = md5Regex.Match(line);
                if (match.Success) {
                    string value = match.Groups[0].Value; //Extracted MD5 without the rest of the line
                    if (!hashes.Contains(value)) //Don't add same value twice
                        hashes.Add(value);
                }
            }

            if (hashes.Count == 0)
                Error("\nNo MD5 hashes found.");
            else if (!string.IsNullOrWhiteSpace(path)) //Only show number of hashes in path mode
                Console.WriteLine("\n{0} unique hash{1} found.\n", hashes.Count, hashes.Count == 1 ? string.Empty : "es");
            else
                Console.WriteLine("\n"); //Spacer
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
            foreach (string searchUrl in websites)
            {
                try {
                    string url = stringBuilder.Clear().Append(searchUrl).Append(hash).ToString(); //Create url
                    string response = Encoding.UTF8.GetString(webClient.DownloadData(url)); //Send query
                    string[] wordlist = response.Split(splitChars, StringSplitOptions.RemoveEmptyEntries); //Split response

                    string answer = DictionaryAttack(hash, wordlist);
                    if (!string.IsNullOrWhiteSpace(answer)) //If result is not null, return the password/original string found
                        return answer;
                }
                catch (WebException e) { //Catch 404, 503, etc errors
                    Console.WriteLine("Exception: {0}", e.Message);
                }
            }
            return string.Empty;
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

        private static void SaveHashes()
        {
            string appDirectory = (new FileInfo(System.Reflection.Assembly.GetEntryAssembly().Location)).Directory.FullName;
            string file = Path.Combine(appDirectory, resultsFile).ToString();

            //Write all of the lines in the table to a file
            if (!File.Exists(path))
            {
                string[] lines = new string[table.Count];
                for (int i = 0; i < lines.Count(); i++) {
                    KeyValuePair<string, string> pair = table.ElementAt(i);
                    lines[i] = stringBuilder.Clear().Append(pair.Key).Append(":").Append(pair.Value).ToString();
                }
                try {
                    File.WriteAllLines(file, lines);
                }
                catch (IOException e) {
                    Console.WriteLine("Exception: {0}", e.Message);
                }
            }
        }

        private static void Error(string reason, ConsoleColor color = ConsoleColor.Red)
        {
            //If the user inputs something invalid, display a message and restart the process
            Console.ForegroundColor = color;
            Console.WriteLine(reason + "\n");
            Console.ForegroundColor = ConsoleColor.Gray;
            error = true;
            Reset();
        }

        private static void Reset()
        {
            path = md5 = string.Empty; //Reset
            table.Clear();
            hashes.Clear();
        }
    }
}
