using System;
using System.IO;
using System.Linq;
using System.Windows;
using WpfApplication1.Properties;

namespace WpfApplication1.DAL
{
    static class PathToFilesUtil
    {
        static string[] categorizedInputCSVfiles;
        static string[] nonCategorizedInputCSVfiles;
        private static readonly object _fileLock = new object();

        public static void ObtainInputRawCsvFiles()
        {
            try
            {
                categorizedInputCSVfiles = Directory.GetFiles(Config.PathToSskaDownloadsFolder, @"*categorized.csv");
                string[] allInputCSVfiles = Directory.GetFiles(Config.PathToSskaDownloadsFolder, @"*.csv");
                nonCategorizedInputCSVfiles = allInputCSVfiles.Except(categorizedInputCSVfiles).ToArray();
                // move old categorized to arxive, only non-categorized csv files to be processed further
                foreach (var categorizedCSVFile in categorizedInputCSVfiles)
                {
                    MoveFileToArxiv(categorizedCSVFile, "_old_arxive");
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, Config.AppName + ": Unable to get Target File.", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static string[] GetCategorizedInputCSVFilePaths => Directory.GetFiles(Config.PathToSskaDownloadsFolder, @"*categorized.csv");

        public static string[] GetNonCategorizedInputCSVFilePaths => nonCategorizedInputCSVfiles;

        public static string GetNameOfFirstInputCategorizedCsvFile()
        {
            try
            {
                var files = GetCategorizedInputCSVFilePaths;
                return files.Length > 0 ? files[0] : string.Empty;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, Config.AppName + ": Unable to get Target categorized File.", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return string.Empty;
        }

        public static string CreateSskaFolderAndFile()
        {
            string pathToArxivFile = Path.Combine(Config.PathToXmlStorageFolder, Settings.Default.StorageFileName);
            if (!File.Exists(pathToArxivFile))
            {
                try
                {
                    if (!Directory.Exists(Config.PathToXmlStorageFolder))
                        Directory.CreateDirectory(Config.PathToXmlStorageFolder);
                    using (StreamWriter sw = File.CreateText(pathToArxivFile))
                    {
                        sw.WriteLine(@"<Root>");
                        sw.WriteLine(@"</Root>");
                        MessageBox.Show("The necessary folders and empty arxiv file 'Arxiv.xml' have been created.", Config.AppName, MessageBoxButton.OK);
                        return pathToArxivFile;
                    }
                }
                catch (IOException e)
                {
                    MessageBox.Show(e.Message, Config.AppName + ": Unable to create new arxiv file", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            return string.Empty;
        }

        public static string AppendSuffixToFileName(string fileName, string suffix)
        {
            string newFilename = Path.GetFileNameWithoutExtension(fileName) + suffix + Path.GetExtension(fileName).ToLower();
            return Path.Combine(Path.GetDirectoryName(fileName), newFilename);
        }

        public static string CreateCategorizedCsvFile(string rawCsvFile)
        {
            string categorizedFileName = AppendSuffixToFileName(rawCsvFile, "_categorized");
            try
            {
                using (File.Create(categorizedFileName)) { }
                return categorizedFileName;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failure to create csv file with suffix categorized", ex.Message);
                return string.Empty;
            }
        }

        public static void MoveFileToArxiv(string pathToCSVFile, string suffix)
        {
            try
            {
                lock (_fileLock)
                {
                    if (!Directory.Exists(Config.PathToXmlStorageFolder))
                        Directory.CreateDirectory(Config.PathToXmlStorageFolder);

                    if (!string.IsNullOrEmpty(pathToCSVFile))
                    {
                        string fileName = GetUniqueCsvFileName(pathToCSVFile, suffix);
                        File.Move(pathToCSVFile, fileName);
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), Config.AppName + ": Unable to move File in Arxiv.", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static string GetUniqueCsvFileName(string baseName, string suffix)
        {
            string dir = Config.PathToXmlStorageFolder;
            string fn = Path.GetFileNameWithoutExtension(baseName);
            string filePath = Path.Combine(dir, fn + suffix + ".csv");
            int counter = 1;
            while (File.Exists(filePath))
            {
                filePath = Path.Combine(dir, $"{fn}{suffix}_{counter}.csv");
                counter++;
            }
            return filePath;
        }
    }
}
