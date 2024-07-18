﻿using System;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;
using Azure;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Configuration;


namespace ConsoleAppAzureFileShare
{
    internal class Program
    {
        public class Tasks
        {
            // Create a file share
            public async Task CreateShareAsync(string shareName)
            {
                var config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
                var connectionString = config.GetSection("StorageCredentials")["StorageConnectionString"];

                ShareClient share = new ShareClient(connectionString, shareName);
                Console.WriteLine("Connection to Azure Storage succeeded...");

                Console.WriteLine("Create a new File Share");
                await share.CreateIfNotExistsAsync(); // Create a share if it doesn't already exist

                //Ensure that the share exists
                if (await share.ExistsAsync())
                {
                    Console.WriteLine($"Share created: {share.Name} is used.");

                    // Check this existing directory
                    Console.WriteLine("Enter a directory name");
                    var directoryName = Console.ReadLine();
                    ShareDirectoryClient directory = share.GetDirectoryClient($"{directoryName}");

                    // Create it if it doesn't already exist
                    Console.WriteLine($"Create a new directory named {directoryName}.");
                    await directory.CreateIfNotExistsAsync();
                    Console.WriteLine($"{directoryName} created successfully.");

                    // Ensure that the directory exists before checking/creating a file
                    if (await directory.ExistsAsync())
                    {
                        Console.WriteLine($"Directory created: {directory.Name} is used.");

                        Console.WriteLine("Enter a file name");
                        var fileName = Console.ReadLine();
                        ShareFileClient file = directory.GetFileClient($"{fileName}.txt");

                        if (await file.ExistsAsync())
                        {
                            Console.WriteLine($"File exists: {file.Name}");

                            // Download the file
                            Console.WriteLine("Download has started...");
                            ShareFileDownloadInfo download = await file.DownloadAsync();

                            // Save the data to a local file
                            using (FileStream stream = File.OpenWrite(@"downloadedTest1.txt"))
                            {
                                await download.Content.CopyToAsync(stream);
                                await stream.FlushAsync();
                                stream.Close();

                                // Display where the file was saved
                                Console.WriteLine($"File downloaded: {stream.Name}");
                            }
                        }
                        else
                        {
                            Console.WriteLine("This file doesn't exist...");
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"CreateShareAsync failed...");
                }
            }

            // Set the maximum size of a share
            public async Task SetMaxShareSizeAsync(string shareName, int increaseSizeInGiB)
            {
                const long ONE_GIBIBYTE = 10737420000; // Number of bytes in 1 gibibyte

                // Get the connection string from app settings
                var config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
                var connectionString = config.GetSection("StorageCredentials")["StorageConnectionString"];

                ShareClient share = new ShareClient(connectionString, shareName);

                // Create the share if it doesn't already exist
                await share.CreateIfNotExistsAsync();

                // Ensure that the share exists
                if (await share.ExistsAsync())
                {
                    // Get and display current share quota
                    ShareProperties properties = await share.GetPropertiesAsync();
                    Console.WriteLine($"Current share quota: {properties.QuotaInGB} GiB");

                    // Get and display current usage stats for the share
                    ShareStatistics stats = await share.GetStatisticsAsync();
                    Console.WriteLine($"Current share usage: {stats.ShareUsageInBytes} bytes");

                    // Convert current usage from bytes into GiB
                    int currentGiB = (int)(stats.ShareUsageInBytes / ONE_GIBIBYTE);
                    Console.WriteLine($"Current GiB: {currentGiB}");

                    // This line sets the quota to be the current 
                    // usage of the share plus the increase amount
                    await share.SetQuotaAsync(currentGiB + increaseSizeInGiB);

                    // Get the new quota and display it
                    properties = await share.GetPropertiesAsync();
                    Console.WriteLine($"New share quota: {properties.QuotaInGB} GiB");
                }
            }

        }



        static async Task Main(string[] args)
        {
            Console.WriteLine("Enter the name of the File Share...");
            var shareName = Console.ReadLine();

            Console.WriteLine("Enter the quota of the File Share (integer)...");
            var FileShareQuota = Convert.ToUInt16(Console.ReadLine()); // Convert the string input to int

            // Call the CreateShareAsync method
            Tasks CreateFileShare = new Tasks();
            await CreateFileShare.CreateShareAsync($"{shareName}");
            Console.WriteLine("CreateShareAsync done...");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();

            // Call the SetMaxShareSizeAsync method
            Console.WriteLine("------------ SetMaxShareSizeAsync --------------");
            Tasks MaxShareSizeAsync = new Tasks();
            await MaxShareSizeAsync.SetMaxShareSizeAsync($"{shareName}", FileShareQuota);
            Console.WriteLine($"The quota of {shareName} is {FileShareQuota} GiB.");
            Console.WriteLine("SetMaxShareSizeAsync done...");
            Console.ReadKey();
        }
    }
}
