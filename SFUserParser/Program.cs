using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;
using System.Diagnostics;
using Newtonsoft.Json.Linq;

namespace SFUserParser
{
    class Program
    {
        static void Main(string[] args)
        {
            // Parsing arguments
            if (args.Length < 1)
            {
                Console.WriteLine("ERROR: You must specify a user name.");
                Console.WriteLine("Press any key to exit.");
                Console.ReadLine();
                return;
            }

            // =====================================================================

            // Define some variables
            string userID;
            List<string> modeluids;

            // =====================================================================

            // Start a stopwatch
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            // =====================================================================

            // Get a user id
            userID = GetUserID(args[0]);
            Console.WriteLine("User name: {0}", args[0]);
            Console.WriteLine("User ID: {0}", userID);
            Console.WriteLine("Fetching uids list, please wait...");
            Console.WriteLine("");

            // =====================================================================

            // Get the model uids
            modeluids = GetUids(userID);
            Console.WriteLine("Found {0} models.", modeluids.Count);

            // =====================================================================
            
            // Flush uids list to file
            File.WriteAllLines("uids.txt", modeluids);
            Console.WriteLine("uids.txt created. Use it with SFTool: SFTool.exe -l uids.txt");

            // =====================================================================

            // Stop our stopwatch and print elapsed time
            stopwatch.Stop();
            TimeSpan ts = stopwatch.Elapsed;
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
            Console.WriteLine("Time taken: {0}", elapsedTime);
        }

        // Get a user ID from its name
        public static string GetUserID(string authorName)
        {
            // Define some variables
            string htmlPageBaseUrl = "https://sketchfab.com/";
            string userID = "";

            // Perform a request - to get the html page with user id
            HttpWebRequest req = (HttpWebRequest) WebRequest.Create(htmlPageBaseUrl + authorName + "/models");
            HttpWebResponse resp = (HttpWebResponse) req.GetResponse();

            // If the server answered with code 200 - continue.
            if (resp.StatusCode == HttpStatusCode.OK)
            {
                // Define some variables
                string dataFromStream;

                // Deserialize our data
                using (Stream dataStream = resp.GetResponseStream())
                {
                    // Read our stream and convert it to UTF-8 string
                    using (StreamReader reader = new StreamReader(dataStream, Encoding.UTF8))
                    {
                        dataFromStream = reader.ReadToEnd();
                    }
                }

                // Get a User ID from HTML string
                userID = Regex.Match(dataFromStream, "(?<=data-profile-user=\")(.*?)(?=\")").ToString();
            }

            // Return our data
            return userID;
        }

        // Get a list of model ids of specified author
        public static List<string> GetUids(string nameHash)
        {
            // Define some variables
            string nextUrl = "https://sketchfab.com/i/models/?user=" + nameHash;
            List<Dictionary<string, dynamic>> models = new List<Dictionary<string, dynamic>>();
            List<string> uids = new List<string>();

            // Perform a looped request - to get the json with model ids
            do
            {
                HttpWebRequest req = (HttpWebRequest) WebRequest.Create(nextUrl);
                HttpWebResponse resp = (HttpWebResponse) req.GetResponse();

                // If the server answered with code 200 - continue.
                if (resp.StatusCode == HttpStatusCode.OK)
                {
                    // Define some variables
                    Dictionary<string, dynamic> jsonData;
                    string dataFromStream;

                    // Deserialize our data
                    using (Stream dataStream = resp.GetResponseStream())
                    {
                        // Read our stream and convert it to UTF-8 string
                        using (StreamReader reader = new StreamReader(dataStream, Encoding.UTF8))
                        {
                            dataFromStream = reader.ReadToEnd();
                        }
                    }

                    // Deserialize
                    JObject jObject = JObject.Parse(dataFromStream);
                    jsonData = jObject.ToObject<Dictionary<string, dynamic>>();

                    // Work with the config
                    foreach (KeyValuePair<string, dynamic> kvp in jsonData)
                    {
                        // Process resulting array and find a url with next part of uids
                        switch (kvp.Key)
                        {
                            case "next":
                                if (kvp.Value != null)
                                {
                                    nextUrl = kvp.Value.ToString();
                                }
                                else
                                {
                                    nextUrl = null;
                                }

                                break;

                            case "results":
                                foreach (JObject modelsObj in kvp.Value)
                                {
                                    models.Add(modelsObj.ToObject<Dictionary<string, dynamic>>());
                                }

                                break;
                        }
                    }

                    // Foreach our List to work with sub-arrays
                    foreach (Dictionary<string, dynamic> modelInfo in models)
                    {
                        // Get the model ID
                        foreach (KeyValuePair<string, dynamic> kvp in modelInfo)
                        {
                            if (kvp.Key == "uid")
                            {
                                string uid = kvp.Value.ToString();

                                // Check if uid does not exist then add it to the List
                                if (!uids.Contains(uid))
                                {
                                    uids.Add(uid);
                                    // Console.WriteLine("uid = {0}", uid); // Debug only
                                }
                            }
                        }
                    }
                }

                // Console.WriteLine("Next URL: {0}", nextUrl);
            }
            while (nextUrl != null);

            // Return our data
            return uids;
        }
    }
}