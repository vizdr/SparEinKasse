using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using WpfApplication1.Properties;
using System.Text;

namespace WpfApplication1.DAL
{
    static class PathToFilesUtil
    {
        static string[] categorizedInputCSVfiles;
        private static readonly object _fileLock;
        static PathToFilesUtil()
        {
            _fileLock = new object();            
        }
        public static string[] GetInputRawCsvFiles()
        {
            try
            {
                categorizedInputCSVfiles = Directory.GetFiles(Config.PathToSskaDownloadsFolder, @"*categorized.csv");
                string[] allInputCSVfiles = Directory.GetFiles(Config.PathToSskaDownloadsFolder, @"*.csv");
                return allInputCSVfiles.Except(categorizedInputCSVfiles).ToArray();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, Config.AppName + ": Unable to get Target File.", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        public static string GetNameInputCategorizedCsvFile()
        {          
            try
            {
                categorizedInputCSVfiles = Directory.GetFiles(Config.PathToSskaDownloadsFolder, @"*categorized.csv");
                return categorizedInputCSVfiles.Length > 0 ? categorizedInputCSVfiles[0] : string.Empty;
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
                try
                {
                    if (!Directory.Exists(Config.PathToXmlStorageFolder))
                        Directory.CreateDirectory(Config.PathToXmlStorageFolder);
                    using (StreamWriter sw = File.CreateText(pathToArxivFile))
                    {
                        sw.WriteLine(@"<Root>");
                        sw.WriteLine(@"</Root>");
                        sw.Close();
                        MessageBox.Show("The necessary folders and empty arxiv file 'Arxiv.xml' have been created.", Config.AppName, MessageBoxButton.OK);
                        return pathToArxivFile;
                    }
                }
                catch (IOException e)
                {
                    MessageBox.Show(e.Message, Config.AppName + ": Unable to create new arxiv file", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            return string.Empty;
        }

        public static string GetArxivedCsvFilePath(string fileName)
        {
            return Path.Combine(Config.PathToXmlStorageFolder, Path.GetFileName(fileName)); ;
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
                FileStream fileStream = File.Create(categorizedFileName);
                if(File.Exists(categorizedFileName))
                {
                    fileStream.Close();
                }
                return categorizedFileName;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failure to create or move csv file with suffix categorized", ex.Message);
                return String.Empty;
            }
        }

        static void AppendCategoryFields(ref string[] sources)
        {
            string headers = sources.FirstOrDefault();
            if (headers != null)
            {
                sources[0] = headers.Insert(headers.Length, Config.Delimiter4CSVFile + Config.CategoryIdField + Config.Delimiter4CSVFile + Config.CategoryField);
            }
            for (int i = 0; i < sources.Length; i++)
            {
                if (i > 0)
                {
                    sources[i] = sources[i].Insert(sources[i].Length, Config.Delimiter4CSVFile + Config.Delimiter4CSVFile);
                }
            }
        }
        public static void MoveFileToArxiv()
        {
            try
            {
                lock (_fileLock)
                {
                    if (!Directory.Exists(Config.PathToXmlStorageFolder))
                        Directory.CreateDirectory(Config.PathToXmlStorageFolder);
                    if (CsvToXmlSSKA.isInputCsvFilesCategorized())
                    {
                        string fileName = AppendSuffixToFileName(GetNameInputCategorizedCsvFile(), "_arxiv");
                        for (int i = 0; File.Exists(GetArxivedCsvFilePath(fileName)); i++)
                            fileName = $"{fileName}_{i}";
                        File.Move(GetNameInputCategorizedCsvFile(), GetArxivedCsvFilePath(fileName));
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), Config.AppName + ": Unable to move File in Arxiv.", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
