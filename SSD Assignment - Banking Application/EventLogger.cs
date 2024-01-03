using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json;

//  NOTE: ***************************************************
//  I am currently storing logs on my Windows system adm Viwer, and additionally, I am archiving them on my GitHub Gists for broader accessibility and version control.
//  ***************************************************

namespace Banking_Application
{
    public static class EventLogger
    {
        private static string logName = "BankAppLog";
        private static string sourceName = "BankAppSource";

        // Save the vents in my GitHub Gists
        private static string githubToken = "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"; // This key expired, create a new key with right Gits permission to see the logs 


        static EventLogger()
        {
            if (!EventLog.SourceExists(sourceName))
            {
                EventLog.CreateEventSource(sourceName, logName);
                Console.WriteLine($"Event source '{sourceName}' created.");
            }
        }

        public static EventRecord ReadLastEvent()
        {
            EventLogQuery query = new EventLogQuery(logName, PathType.LogName,
                $"*[System[Provider[@Name='{sourceName}']]]")
            {
                ReverseDirection = true
            };

            using (EventLogReader logReader = new EventLogReader(query))
            {
                return logReader.ReadEvent();
            }
        }


        public static void ReadAllEvents()
        {
            EventLogQuery query = new EventLogQuery(logName, PathType.LogName,
                $"*[System[Provider[@Name='{sourceName}']]]")
            {
                ReverseDirection = true
            };

            using (EventLogReader logReader = new EventLogReader(query))
            {
                for (EventRecord eventInstance = logReader.ReadEvent(); eventInstance != null; eventInstance = logReader.ReadEvent())
                {
                    Console.WriteLine($"Event ID: {eventInstance.Id}");
                    Console.WriteLine($"Level: {eventInstance.LevelDisplayName}");
                    Console.WriteLine($"Message: {eventInstance.FormatDescription()}");
                    Console.WriteLine();
                }
            }
        }

        public static void WriteEvent(string message, EventLogEntryType eventType, DateTime timestamp)
        {
            // Get the system user Name
            string userName = Environment.UserName;

            using (EventLog eventLog = new EventLog(logName))
            {
                eventLog.Source = sourceName;

                // Include user information and timestamp in the log message
                string formattedMessage = $"{message} - User: {userName} - Timestamp: {timestamp}";

                eventLog.WriteEntry(formattedMessage, eventType);

                // Upload log to GitHub Gist
                //  _ = UploadToGitHubGist(formattedMessage);
            }
        }

        //  logging a specific event
        public static void LogEvent(string eventName, string additionalInfo)
        {
            DateTime timestamp = DateTime.Now;
            WriteEvent($"{eventName}: {additionalInfo}", EventLogEntryType.Information, timestamp);
        }



        //Update to git file called log.txt
        public static async Task UploadToGitHubGist(string content)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("Authorization", $"token {githubToken}");


                    // Construct the update Gist API URL
                    string updateUrl = $"https://api.github.com/gists";

                    // Get the existing Gist content
                    var existingGist = await httpClient.GetStringAsync(updateUrl);
                    dynamic existingGistData = JsonConvert.DeserializeObject(existingGist);

                    // Update the Gist with the new content
                    existingGistData.files["log.txt"].content += "\n" + content;

                    var updatedContent = JsonConvert.SerializeObject(new
                    {
                        files = existingGistData.files,
                        description = existingGistData.description
                    });

                    var requestContent = new StringContent(updatedContent, Encoding.UTF8, "application/json");
                    var response = await httpClient.PatchAsync(updateUrl, requestContent);

                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Log appended to Gist. Gist URL: {updateUrl}");
                    }
                    else
                    {
                        Console.WriteLine($"Failed to append log to Gist. Status code: {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception during Gist update: {ex.Message}");
            }
        }

    }


}