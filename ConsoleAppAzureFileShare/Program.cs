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
                await share.CreateIfNotExistsAsync(); // Create a share if it don't already exist

                //Ensure that the share exists
                if (await share.ExistsAsync())
                {
                    Console.WriteLine($"Share created: {share.Name} is used.");

                    // Check this existing directory
                    Console.WriteLine("Enter a directory name");
                    var directoryName = Console.ReadLine();
                    ShareDirectoryClient directory = share.GetDirectoryClient(directoryName);

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
                        ShareFileClient file = directory.GetFileClient(fileName);

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
                    }
                }
                else
                {
                    Console.WriteLine($"CreateShareAsync failed...");
                }
            }
        }
        


        static async Task Main(string[] args)
        {
            Console.WriteLine("Enter the name of the File Share...");
            var shareName = Console.ReadLine();

            // Call the CreateShareAsync method
            Tasks CreateFileShare = new Tasks();
            await CreateFileShare.CreateShareAsync($"{shareName}");
            Console.WriteLine("CreateShareAsync created...");
            Console.ReadKey();
        }
    }
}
