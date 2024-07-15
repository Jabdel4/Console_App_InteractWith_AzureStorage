using System;
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
        // Create a file share
        public async Task CreateShareAsync(string shareName)
        {
            var config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
            var connectionString = config.GetSection("StorageCredentials")["StorageConnectionString"];

            ShareClient share = new ShareClient(connectionString, shareName);

            await share.CreateIfNotExistsAsync(); // Create a share if it don't already exist

            //Ensure that the share exists
            if (await share.ExistsAsync())
            {
                Console.WriteLine($"Share created: {share.Name}");

                // Check this existing directory
                ShareDirectoryClient directory = share.GetDirectoryClient("TestDirectory");

                // Create it if it doesn't already exist
                await directory.CreateIfNotExistsAsync();

                // Ensure that the directory exists before checking/creating a file
                if (await directory.ExistsAsync())
                {
                    ShareFileClient file = directory.GetFileClient("test1.txt");

                   if (await file.ExistsAsync())
                    {
                        Console.WriteLine($"File exists: {file.Name}");

                        // Download the file
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
                }
            }
        }


        // Execution
        static void Main(string[] args)
        {

            Console.WriteLine("Hello, World!");
        }
    }
}
