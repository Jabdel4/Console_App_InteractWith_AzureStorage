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
                Console.WriteLine("Connection to Azure Storage succeeded...");

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

            //https://stackoverflow.com/questions/77111976/how-can-i-copy-one-file-from-one-folder-to-another-within-an-azure-fileshare-usi

            public async Task CopyFileAsync(string shareName, string sourceFilePath, string destFilePath)
            {
                var config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
                var connectionString = config.GetSection("StorageCredentials")["StorageConnectionString"];
                Console.WriteLine("Connection to Azure Storage succeeded...");

                ShareClient shareClient = new ShareClient(connectionString, shareName);
                ShareDirectoryClient sourceDir = shareClient.GetDirectoryClient(Path.GetDirectoryName(sourceFilePath));
                ShareDirectoryClient destDir = shareClient.GetDirectoryClient(Path.GetDirectoryName(destFilePath));

                if (await sourceDir.ExistsAsync() && await destDir.ExistsAsync())
                {
                    ShareFileClient sourceFile = sourceDir.GetFileClient(Path.GetFileName(sourceFilePath));
                    ShareFileClient destFile = destDir.GetFileClient(Path.GetFileName(destFilePath));

                    if (await sourceFile.ExistsAsync())
                    {
                        ShareFileProperties properties = await sourceFile.GetPropertiesAsync();
                        if (properties.CopyStatus == CopyStatus.Success)
                        {
                            Console.WriteLine("File already copied.");
                        }
                        else
                        {
                            await destFile.StartCopyAsync(sourceFile.Uri);

                            //await WaitForCopyToCompleteAsync(destFile);

                            Console.WriteLine("File copied successfully.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Source file does not exist.");
                    }
                }
                else
                {
                    Console.WriteLine("Source or destination folder does not exist.");
                }


            }


            // Generate a shared access signature for a file or file share
            //public Uri GetFileSasUri(string shareName, string filePath, ShareFileSasPermissions permissions)
            //{
            //    // Get the connection string from app settings
            //    var config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
            //    var saName = config.GetSection("StorageCredentials")["StorageAccountName"];
            //    var saKey = config.GetSection("StorageCredentials")["StorageAccountKey"];
            //    Console.WriteLine("Connection to Azure Storage succeeded...");

            //    ShareSasBuilder fileSAS = new ShareSasBuilder()
            //    {
            //        ShareName = shareName,
            //        FilePath = filePath,

            //        // Specify an Azure file resource
            //        Resource = "f",

            //        // Expires in 24 hours
            //        ExpiresOn = DateTime.Now.AddMinutes(10),
            //    };

            //    // Set the permissions for the SAS
            //    fileSAS.SetPermissions(permissions);

            //    // Create a SharedKeyCredential that we can use to sign the SAS token
            //    StorageSharedKeyCredential credential = new StorageSharedKeyCredential(saName, saKey);

            //    // Build a SAS URI
            //    UriBuilder fileSasUri = new UriBuilder($"https://{saName}.file.core.windows.net/{fileSAS.ShareName}/{fileSAS.FilePath}");
            //    fileSasUri.Query = fileSAS.ToSasQueryParameters(credential).ToString();

            //    // Return the URI
            //    return fileSasUri.Uri;
            //}

        }



        static async Task Main(string[] args)
        {
            Console.WriteLine("Enter the name of the File Share...");
            var shareName = Console.ReadLine();

            //Console.WriteLine("Enter the quota of the File Share (integer)...");
            //var FileShareQuota = Convert.ToUInt16(Console.ReadLine()); // Convert the string input to int

            //// Call the CreateShareAsync method
            //Console.WriteLine("------------ CreateShareAsync --------------");
            //Tasks CreateFileShare = new Tasks();
            //await CreateFileShare.CreateShareAsync($"{shareName}");
            //Console.WriteLine("CreateShareAsync done...");
            //Console.WriteLine("Press any key to continue...");
            //Console.ReadKey();

            //// Call the SetMaxShareSizeAsync method
            //Console.WriteLine("------------ SetMaxShareSizeAsync --------------");
            //Tasks MaxShareSizeAsync = new Tasks();
            //await MaxShareSizeAsync.SetMaxShareSizeAsync($"{shareName}", FileShareQuota);
            //Console.WriteLine($"The quota of {shareName} is {FileShareQuota} GiB.");
            //Console.WriteLine("SetMaxShareSizeAsync done...");
            //Console.WriteLine("Press any key to continue...");
            //Console.ReadKey();

            // Call the CopyFileAsync method
            Console.WriteLine("------------ CopyFileAsync --------------");
            Tasks CopyFile = new Tasks();
            Console.WriteLine("Enter the source file path... Like > <sourcefolder_name/sourcefile_name.txt>");
            var sourceFilePath = Console.ReadLine();
            Console.WriteLine("Enter the destination file path... Like > <destinationfolder_name/destinationfile_name.txt>");
            var destFilePath = Console.ReadLine();
            Console.WriteLine("Copy is starting...");
            await CopyFile.CopyFileAsync($"{shareName}", $"{sourceFilePath}", $"{destFilePath}");
            Console.WriteLine("CopyFileAsync done...");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();



            // Call the GetFileSasUri method
            //Console.WriteLine("------------ GetFileSasUri --------------");
            //Tasks FileSasUri = new Tasks();
            //await FileSasUri.GetFileSasUri($"{shareName}", "test", ShareFileSasPermissions.Read);
            //Console.WriteLine($"The generated SAS Token is: {FileSasUri.Uri} ");
            //Console.WriteLine("GetFileSasUri done...");
            //Console.WriteLine("Press any key to continue...");
            //Console.ReadKey();
        }
    }
}
