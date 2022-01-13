using System;
using System.IO;
using System.Windows;



namespace WpfApplication1.DAL
{
    static class PathToFilesUtil
    {
        private static readonly object _fileLock;
        static PathToFilesUtil()
        {
            _fileLock = new object();
        }
        public static string GetNameInputCsvFile()
        {
            string[] files = { };
            try
            {
                files = Directory.GetFiles(Config.PathToSskaDownloadsFolder, @"*.csv");
                return files.Length > 0 ? files[0] : string.Empty;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, Config.AppName + ": Unable to get Target File.", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return string.Empty;
        }

        public static string CreateSskaFolderAndFile()
        {
            string pathToArxivFile = Path.Combine(Config.PathToXmlStorageFolder, "Arxiv.xml");
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
            return Config.PathToXmlStorageFolder + @"\" + fileName + ".csv";
        }

        public static void MoveFileToArxiv()
        {
            try
            {
                lock (_fileLock)
                {
                    if (!Directory.Exists(Config.PathToXmlStorageFolder))
                        Directory.CreateDirectory(Config.PathToXmlStorageFolder);
                    if (CsvToXmlSSKA.isInputCsvFileAvailable())
                    {
                        string fileName = Path.GetFileNameWithoutExtension(GetNameInputCsvFile()) + "_arxiv";
                        for (int i = 0; File.Exists(GetArxivedCsvFilePath(fileName)); i++) fileName = fileName + "_" + i;
                        File.Move(GetNameInputCsvFile(), GetArxivedCsvFilePath(fileName));
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
