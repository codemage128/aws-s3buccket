
using Amazon;
using Amazon.S3;
using Amazon.S3.Transfer;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.IO;
using System.IO.Compression;
using System.Threading;
using ICSharpCode.SharpZipLib.Zip;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    class Program
    {
        private const string bucketName = "bbm-update";
        private const string keyName = "game_wallpater.png";
        private static string _filePath = "";
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public static string filePath
        {
            get { return _filePath; }
            set { _filePath = value; }
        }
        
        private static AmazonS3Client s3Client;
        private static readonly RegionEndpoint bucketRegion = RegionEndpoint.USEast1;
        private static ArrayList array;
        public static string currentDirectory = "";
        public static string sourceName = "";
        public static string sourcePath = "";
        public static string destinationDirectory = "";
        public static string destinationPath = "";
        public static string config_file_path = "";
        public static string timeSetting = "";
        public static string downloadSetting = "";
        public static int sec = 0;
        private static System.Timers.Timer aTimer;
        public static long nowTime = 0;
        private static long ConvertToTimestamp(DateTime value)
        {
            TimeSpan elapsedTime = value - Epoch;
            return (long)elapsedTime.TotalSeconds;
        }
        public static void Main(string[] args)
        {
            string[] environment = Environment.GetCommandLineArgs();
            foreach (string str in environment)
            {
                //Console.WriteLine(str);
            }
            s3Client = new AmazonS3Client(bucketRegion);

            currentDirectory = Directory.GetCurrentDirectory();
            sourceName = "source.zip";
            sourcePath = currentDirectory + "\\source.zip";
            destinationDirectory = currentDirectory + "\\download";
            destinationPath = destinationDirectory + "\\download.zip";
            config_file_path = currentDirectory + "\\config.txt";
            timeSetting = currentDirectory + "\\time.txt";
            downloadSetting = destinationDirectory + "\\time.txt";

            nowTime = ConvertToTimestamp(DateTime.Now);
            try { 
                using (FileStream fs = File.Create(config_file_path)) {
                    array = new ArrayList();
                    array.Add("config.txt");
                }
            } catch (Exception e) { }
            try
            {
                using (FileStream fs = File.Create(currentDirectory + "\\time.txt"))
                {
                    Console.WriteLine(DateTime.Now.ToString("h:mm:ss tt"));
                }
            } catch (Exception e) { }

            
            DownloadSetting(bucketRegion, downloadSetting, bucketName);

            if (args.Length != 0)
            {
                //Console.WriteLine("Error! No parameter");
            }
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-uploadFile")
                {
                    
                    compressAndUpload();
                }
                else if (args[i] == "-downloadFile")
                {
                    string text = File.ReadAllText(timeSetting);
                    Console.WriteLine(text);
                    SetTimer();
                    Console.WriteLine("\nPress the Enter key to exit the application...\n");
                    Console.WriteLine("The application started at {0:HH:mm:ss.fff}", DateTime.Now);
                    Console.ReadLine();
                    aTimer.Stop();
                    aTimer.Dispose();
                    
                }
            }
        }
        private static void SetTimer() {
            aTimer = new System.Timers.Timer(2000);
            aTimer.Elapsed += OnTimedEvent;
            aTimer.AutoReset = true;
            aTimer.Enabled = true;
        }
        private static void OnTimedEvent(object source, ElapsedEventArgs e) {
            //Console.WriteLine("I am Timer>>>>>>>>>");
            //Console.WriteLine(downloadSetting);
            if (File.Exists(downloadSetting))
            {
                using (StreamReader sr = File.OpenText(downloadSetting))
                {
                    string serverTime = "";

                    Console.WriteLine(sr.ReadLine());
                    
                    while ((serverTime = sr.ReadLine()) != null)
                    {
                        long l = long.Parse(serverTime);
                        
                        TimeSpan meTime = TimeSpan.FromSeconds(nowTime);
                        TimeSpan server = TimeSpan.FromSeconds(l);
                        TimeSpan timespan = meTime.Subtract(server);
                        //Console.WriteLine(int.Parse(timespan.ToString()));
                        //Console.WriteLine(text);
                        //Console.WriteLine(serverTime);
                        Console.WriteLine(timespan.Seconds);
                        if (int.Parse(timespan.ToString()) != 0)
                        {
                            //downloadAndExtract();
                            Console.WriteLine("Changed. Please update");
                        }
                        else {
                            Console.WriteLine("No Changed!");
                        }
                    }
                }
            }
        }
        public static void compressAndUpload() {
            compressDirectory(currentDirectory, sourceName, 2);
            UploadFile(s3Client, sourcePath, bucketName);
            UploadFile(s3Client, timeSetting, bucketName);
            System.IO.File.WriteAllText(currentDirectory + "\\time.txt", nowTime.ToString());
        }
        public static void downloadAndExtract() {
            DownloadFile(bucketRegion, destinationPath, destinationDirectory, destinationPath, bucketName);
        }
        public static void DownloadSetting(RegionEndpoint _bucketRegion, string _destinationsPath, string _bucketName) {
            TransferUtility fileTransferUtility = new
                    TransferUtility(new AmazonS3Client(_bucketRegion));
            Directory.CreateDirectory(Path.GetDirectoryName(_destinationsPath));
            fileTransferUtility.Download(_destinationsPath,
                                       _bucketName, "time.txt");
        }
        public static void DownloadFile(RegionEndpoint _bucketRegion, string _destinationsPath, string _extractDirectory, string _sourcePath, string _bucketName) {
            try
            {
                TransferUtility fileTransferUtility = new
                    TransferUtility(new AmazonS3Client(_bucketRegion));
                Directory.CreateDirectory(Path.GetDirectoryName(_destinationsPath));
                fileTransferUtility.Download(_destinationsPath,
                                           _bucketName, "source.zip");
                extractFile(_sourcePath, _extractDirectory);
                
            }
            catch (AmazonS3Exception s3Exception)
            {
                Console.WriteLine(s3Exception.Message,
                                  s3Exception.InnerException);
            }
        }

        public static void UploadFile(AmazonS3Client _s3Client, string _filepath, string _bucketName)
        {
            try
            {
                var fileTransferUtility = new TransferUtility(_s3Client);
                fileTransferUtility.Upload(_filepath, _bucketName);
                Console.WriteLine("OK");
            }
            catch (AmazonS3Exception e)
            {
                Console.WriteLine("Error encountered on server", e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("Unknow encountered on server", e.Message);
            }
        }
        public static void compressDirectory(string DirectoryPath, string OutputFile, int CompressionLevel)
        {
            try
            {
                string[] filenames = Directory.GetFiles(DirectoryPath);
                using (ZipOutputStream outputStream = new ZipOutputStream(File.Create(OutputFile)))
                {
                    outputStream.SetLevel(CompressionLevel);
                    byte[] buffer = new byte[4096];
                    //string realPath = DirectoryPath + "\\ConsoleApp2.exe";
                    ArrayList realPathList = new ArrayList();
                    realPathList.Add(DirectoryPath + "\\" +  System.AppDomain.CurrentDomain.FriendlyName);
                    foreach (string arrayItem in array) {
                        realPathList.Add(DirectoryPath +"\\"+ arrayItem);
                    }
                    foreach (string realPath in realPathList)
                    {
                        Console.WriteLine(realPath);
                        foreach (string file in filenames)
                        {
                            if (file == realPath)
                            {
                                ZipEntry entry = new ZipEntry(Path.GetFileName(file));
                                entry.DateTime = DateTime.Now;
                                outputStream.PutNextEntry(entry);
                                using (FileStream fs = File.OpenRead(file))
                                {
                                    int sourceBytes;
                                    do
                                    {
                                        sourceBytes = fs.Read(buffer, 0, buffer.Length);
                                        outputStream.Write(buffer, 0, sourceBytes);
                                    } while (sourceBytes > 0);
                                }
                            }
                        }
                       
                    }
                    outputStream.Finish();
                    outputStream.Close();
                    //Console.WriteLine("File successfully compressed");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception during processing", e);
            }

        }
        public static void extractFile(string sourceFile, string DirectoryPath)
        {
            var inputFile = sourceFile;
            //Directory.CreateDirectory(Path.GetDirectoryName(DirectoryPath));
            FastZip fastZip = new FastZip();
            string fileFilter = null;
            Console.WriteLine(DirectoryPath);
            //File.Move(System.AppDomain.CurrentDomain.FriendlyName + ".exe", "newfilename.exe");
            //File.Delete(System.AppDomain.CurrentDomain.FriendlyName + ".exe");
            fastZip.ExtractZip(inputFile, DirectoryPath, fileFilter);

        }
    }
}
