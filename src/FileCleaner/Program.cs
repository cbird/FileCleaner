using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FileCleaner.Models;
using log4net;
using log4net.Appender;
using log4net.Repository.Hierarchy;
using Newtonsoft.Json;

namespace FileCleaner
{
    public class Program
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof (Program));

        private static string GetLogDirectoryPath()
        {
            var rootAppender = ((Hierarchy) LogManager.GetRepository())
                .Root.Appenders.OfType<FileAppender>()
                .FirstOrDefault();

            return rootAppender != null ? Path.GetDirectoryName(rootAppender.File) : null;
        }

        private static void Main(string[] args)
        {
            try
            {
                Log.Info("Starting program...");
                string configPath = null;
                var logDirPath = GetLogDirectoryPath();

                try
                {
                    for (var i = 0; i < args.Length; ++i)
                    {
                        if (args[i].Equals("-config"))
                        {
                            configPath = args[++i];
                        }
                    }
                }
                catch (Exception argEx)
                {
                    Log.Warn("Invalid arguments, falling back to defaults.", argEx);
                    configPath = null;
                }

                var config = LoadConfig(configPath);

                CleanFolders(config);

                if (!string.IsNullOrEmpty(logDirPath))
                {
                    Log.Info("Removing old log files...");
                    Task.WaitAll(CleanFolderAsync(new FolderConfig
                    {
                        ExcludeExtensions = new List<string>(),
                        NbrOfDaysOld = 7,
                        Recursive = true,
                        Path = logDirPath
                    }));
                    Log.Info("Old log files removed!");
                }
                else
                {
                    Log.Warn("Could not find any log directory for FileCleaner, hence no logs were cleaned!");
                }

                Log.Info("Exiting program...");
            }
            catch (Exception ex)
            {
                Log.Fatal("An unhandled error occurred!", ex);
            }
        }

        public static List<T>[] Partition<T>(List<T> list, int totalPartitions)
        {
            if (list == null)
                throw new ArgumentNullException("list");

            if (totalPartitions < 1)
                throw new ArgumentOutOfRangeException("totalPartitions");

            var partitions = new List<T>[totalPartitions];

            var maxSize = (int) Math.Ceiling(list.Count/(double) totalPartitions);
            var k = 0;

            for (var i = 0; i < partitions.Length; i++)
            {
                partitions[i] = new List<T>();
                for (var j = k; j < k + maxSize; j++)
                {
                    if (j >= list.Count)
                        break;
                    partitions[i].Add(list[j]);
                }
                k += maxSize;
            }

            return partitions;
        }

        public static Config LoadConfig(string path = null)
        {
            Log.Info("Loading config...");

            path = string.IsNullOrEmpty(path) ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json") : path;

            try
            {
                using (var reader = new StreamReader(path))
                {
                    var json = reader.ReadToEnd();
                    var config = JsonConvert.DeserializeObject<Config>(json);
                    Log.Info("Loading config complete!");
                    return config;
                }
            }
            catch (Exception ex)
            {
                Log.Error(string.Format("Loading config failed! Path: {0}", path), ex);
                throw;
            }
        }

        public static bool IsBlacklisted(string path)
        {
            var isBlacklisted = false;
            var parsedPath = path.Replace("/", "\\").ToLower();
            parsedPath = parsedPath.EndsWith("\\") ? parsedPath.Substring(0, parsedPath.Length - 1) : parsedPath;
            var arr = parsedPath.Split('\\');

            if (arr.Length == 1)
            {
                isBlacklisted = true;
            }
            else if (arr[arr.Length-1].Equals("inetpub"))
            {
                isBlacklisted = true;
            }
            else if (arr[1].Equals("windows") || arr[1].Contains("program") || arr[1].Equals("users"))
            {
                isBlacklisted = true;
            }

            if (isBlacklisted)
            {
                Log.WarnFormat("The following folder cannot be cleaned {0}", path);
            }

            return isBlacklisted;
        }

        public static void CleanFolders(Config config)
        {
            Log.Info("Starting cleanup...");
            if (config == null || config.Folders == null || config.Folders.Count == 0)
            {
                Log.Info("Nothing to cleanup!");
                return;
            }

            var maxNbrOfTasks = Environment.ProcessorCount*2;
            maxNbrOfTasks = config.MaxThreadCount < maxNbrOfTasks ? maxNbrOfTasks : config.MaxThreadCount;
            var partions = Partition(config.Folders, maxNbrOfTasks).ToList();

            Task.WaitAll(partions.Select(CleanFolderPartsAsync).ToArray());

            Log.Info("Cleanup complete!");
        }

        private static async Task CleanFolderPartsAsync(List<FolderConfig> configs)
        {
            foreach (var conf in configs)
            {
                await CleanFolderAsync(conf);
            }
        }

        private static async Task<bool> CleanFolderAsync(FolderConfig config)
        {
            if (IsBlacklisted(config.Path))
                return false;

            var result = true;

            try
            {
                var directories = config.Recursive ? Directory.GetDirectories(config.Path) : new string[]{};
                var files = Directory.GetFiles(config.Path);

                for (var i = 0; i < directories.Count(); ++i)
                {
                    result = await CleanFolderAsync(config.Copy(directories[i]));
                }
                for (var i = 0; i < files.Count(); ++i)
                {
                    var info = new FileInfo(files[i]);

                    if (info.CreationTimeUtc < DateTime.UtcNow.AddDays(-config.NbrOfDaysOld) && !config.ExcludeExtensions.Contains(info.Extension.ToLower()))
                    {
                        File.Delete(files[i]);
                    }
                }

                if (!Directory.GetDirectories(config.Path).Any() && !Directory.GetFiles(config.Path).Any())
                {
                    Directory.Delete(config.Path);
                }
            }
            catch (Exception ex)
            {
                Log.Warn(string.Format("An error occurred while cleaning folder {0}. Continuing with next one...", config.Path), ex);
                result = false;
            }
            return result;
        }
    }
}