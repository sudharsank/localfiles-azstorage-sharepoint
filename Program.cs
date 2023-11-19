using FTLocalToAzure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Renci.SshNet;

// To get the configuration from appsettings.json file
var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
IConfiguration config = builder.Build();
var configSettings = config.GetSection("AppSettings").Get<ISettings>();

// Configure the logger
using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole());
ILogger logger = factory.CreateLogger("LocalFilesToAzure");

/* Main method to execute to start the upload process */
async Task ProcessFilesToUpload()
{
    logger.LogInformation("Starting the upload process.");
    try
    {
        logger.LogInformation("Checking for settings.");
        if(checkForSettings())
        {
            // Getting the configured directory in a string variable
            string strDir = configSettings?.FilePath;
            // Checking if the directory exists or not
            logger.LogInformation("Checking whether directory exists or not.");
            if(Directory.Exists(strDir))
            {
                logger.LogInformation("Enumerating the files.");
                // Enumerating the files and storing in the files object
                var files = from objFile in Directory.EnumerateFiles(strDir) select objFile;
                logger.LogInformation("Filtering the files.");
                // Filtering some of the irrelevant files
                IEnumerable<string> finalFiles = files.Where(f => Path.GetExtension(f).ToLower() != ".exe");
                if (finalFiles.Count() > 0)
                {
                    UploadFilesViaSFTP(finalFiles);
                }
                else logger.LogInformation("No files to upload.");
            }
        } else
        {
            
            logger.LogCritical("Configuration missing!");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex.ToString());
    }
    Console.ReadLine();
}

/* Method to check all the settings are configured or not */
bool checkForSettings()
{
    bool ret = true;
    if (string.IsNullOrEmpty(configSettings?.FilePath)) ret = false;
    else if (string.IsNullOrEmpty(configSettings?.SFTPConnString)) ret = false;
    else if (string.IsNullOrEmpty(configSettings?.SFTPUsername)) ret = false;
    else if (string.IsNullOrEmpty(configSettings?.SFTPPassword)) ret = false;
    return ret;
}

/* Method to upload the files to Azure via SFTP */
async void UploadFilesViaSFTP(IEnumerable<string> files)
{
    try
    {
        logger.LogInformation("Started to upload files to Azure.");
        IEnumerator<string> filesEnum = files.GetEnumerator();
        if (configSettings != null)
        {
            using (var sftpClient = new SftpClient(configSettings.SFTPConnString, configSettings.SFTPUsername, configSettings.SFTPPassword))
            {
                sftpClient.Connect();
                while (filesEnum.MoveNext())
                {
                    Console.Write($"Trying to upload the file - '{filesEnum.Current}'");
                    string strFilename = string.Format("{0}", Path.GetFileNameWithoutExtension(filesEnum.Current), Path.GetExtension(filesEnum.Current));
                    using (var uplfileStream = System.IO.File.OpenRead(filesEnum.Current))
                    {
                        if (!sftpClient.IsConnected) sftpClient.Connect();
                        sftpClient.UploadFile(uplfileStream, strFilename, true);
                        Console.WriteLine("\t Uploaded");
                    }
                }
                sftpClient.Disconnect();
                logger.LogInformation("All files uploaded!");
            }
        }
        else
        {
            logger.LogCritical("No config settings found");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex.ToString());
    }
}

await ProcessFilesToUpload();