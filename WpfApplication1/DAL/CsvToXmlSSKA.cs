using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Windows;
using System.IO;
using WpfApplication1.Properties;
using System.Xml;
using CategoryFormatter;
using WpfApplication1.BusinessLogic;

namespace WpfApplication1.DAL
{
    public class CsvToXmlSSKA : IDataSourceProvider
    {
        private string[] rawCSVInputFiles;
        private string[] categorizedCSVInputFiles;
        private string PathToStorageXmlFile { get; set; }
        private bool isExceptionUnhandled = false;
        public XElement DataSource { get; private set; }
        private readonly object _fileLock = new object();

        /// <summary>
        /// AccountsLogic instance for CSV processing. Set via property injection to avoid circular dependency.
        /// </summary>
        public AccountsLogic AccountsLogic { get; set; }

        public CsvToXmlSSKA()
        {
            PathToStorageXmlFile = Config.PathToXmlStorageFolder + @"\" + Settings.Default.StorageFileName;
            try
            {
                if (File.Exists(PathToStorageXmlFile))
                    DataSource = XElement.Load(PathToStorageXmlFile);
                else
                {
                    DataSource = XElement.Load(PathToFilesUtil.CreateSskaFolderAndFile());
                    MessageBox.Show("The data bank file is empty.", Config.AppName, MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, Config.AppName + ": Xml Storage File was not load!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Reloads the data source from the XML storage file.
        /// </summary>
        public void Reload()
        {
            try
            {
                if (File.Exists(PathToStorageXmlFile))
                    DataSource = XElement.Load(PathToStorageXmlFile);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, Config.AppName + ": Reload failed!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public bool isInputCsvFilesAvailable()
        {
            if (Directory.Exists(Config.PathToSskaDownloadsFolder))
                rawCSVInputFiles = Directory.GetFiles(Config.PathToSskaDownloadsFolder, @"*.csv");
            bool res = !isExceptionUnhandled & (rawCSVInputFiles.Length > 0);
            return res;
        }

        public bool isInputCsvFilesCategorized()
        {
            if (Directory.Exists(Config.PathToSskaDownloadsFolder))
                categorizedCSVInputFiles = Directory.GetFiles(Config.PathToSskaDownloadsFolder, @"*categorized*.csv");
            bool res = !isExceptionUnhandled & (categorizedCSVInputFiles.Length > 0);
            return res;
        }

        public bool UpdateDataBank()
        {
            isExceptionUnhandled = false;
            int filesCounter = 0;
            if (isInputCsvFilesAvailable())
            {
                bool isCategorizationSucced = false;
                if (!isInputCsvFilesCategorized())
                {
                    isCategorizationSucced = ProvideCategorization();
                }
                if (isCategorizationSucced)
                {
                    while (isInputCsvFilesCategorized())
                        if (TryUpdateSavedXml(PathToFilesUtil.GetNameInputCategorizedCsvFile()))
                        {
                            PathToFilesUtil.MoveFileToArxiv();
                            filesCounter++;
                        }
                }
                return isCategorizationSucced;
            }
            else PathToFilesUtil.CreateSskaFolderAndFile();
            if (filesCounter > 0)
            {
                MessageBox.Show("Update is completed. It was processed " + filesCounter + (filesCounter == 1 ? " file." : " files."), Config.AppName, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("File(s) to update not found.", Config.AppName);
            }
            return (filesCounter > 0);
        }

        private bool ProvideCategorization()
        {
            bool isCategorizatioSucseeded = false;
            string[] rawCsvFiles = PathToFilesUtil.GetInputRawCsvFiles();
            // categorization should be processed. Only raw CSV Input, w/o appended category fields, is accepted.
            if (rawCsvFiles.Length == 0)
            {
                return isCategorizatioSucseeded;
            }
            foreach (string rawCsvFile in rawCsvFiles)
            {
                string csvFileWithCategoryFields = PathToFilesUtil.CreateCategorizedCsvFile(rawCsvFile);
                if (File.Exists(csvFileWithCategoryFields))
                {
                    // fill file with categories;
                    FormatterCSVCategories formatterCsv;
                    string path2CaegorizationFile = FormatterCSVCategories.defaultPath2Categorization;
                    if (!File.Exists(path2CaegorizationFile))
                    {
                        path2CaegorizationFile = Config.PathToCategorizationFile;
                        if (!File.Exists(path2CaegorizationFile))
                        {
                            path2CaegorizationFile = @"../../../Categorization/" + Path.GetFileName(FormatterCSVCategories.defaultPath2Categorization);
                        }                       
                        if (!File.Exists(path2CaegorizationFile))
                        {
                            MessageBox.Show("File to parse categories is missing: " + Path.GetFullPath(FormatterCSVCategories.defaultPath2Categorization) + " or: " + Path.GetFullPath(path2CaegorizationFile));
                        }
                        else
                        {
                            formatterCsv = new FormatterCSVCategories(pathToCategoriesCSV: path2CaegorizationFile, pathToInputCSV: rawCsvFile, pathToOutputCSV: csvFileWithCategoryFields);
                            ProcessCategoriyFile(formatterCsv);
                            isCategorizatioSucseeded = true;
                        }
                    }
                    else
                    {
                        formatterCsv = new FormatterCSVCategories(pathToInputCSV: rawCsvFile, pathToOutputCSV: csvFileWithCategoryFields);
                        ProcessCategoriyFile(formatterCsv);
                        isCategorizatioSucseeded = true;
                    }
                }
                string file2Arxive = PathToFilesUtil.GetArxivedCsvFilePath(PathToFilesUtil.AppendSuffixToFileName(rawCsvFile, "_arxiv"));
                if (!File.Exists(file2Arxive))
                {
                    try
                    {
                        File.Move(rawCsvFile, file2Arxive);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Input CSV File can not be moved to Arxive folder:", ex.Message);
                    }                    
                }
            }
            return isCategorizatioSucseeded;
        }
        private void ProcessCategoriyFile(FormatterCSVCategories formatterCsv)
        {
            if (formatterCsv.GetCountOfAvailibleCategories == 0)
            {
                formatterCsv.GetCategoriesAndKeywordsFromFile();
            }
            
            formatterCsv.ProcessCSVInput();
            formatterCsv.FormCSVOutput();
        }
        public bool TryUpdateSavedXml(string fileToReadPath)
        {
            IEnumerable<XElement> mergedElements = null;
            XElement savedXml = new XElement(Config.XmlFileRoot);
            try
            {
                lock (_fileLock)
                {
                    XElement newXml = GetXmlElem(fileToReadPath);
                    if (newXml != null && newXml.HasElements)
                    {
                        if (isInputCsvFilesAvailable())
                            savedXml = XElement.Load(PathToStorageXmlFile);
                        if (savedXml.HasElements)
                        {
                            mergedElements = savedXml.Elements(Config.TransactionField).Union(newXml.Elements(Config.TransactionField), new SSKA_XmlEqualityComparer());
                        }
                        else
                        {
                            savedXml.AddFirst(newXml.Elements(Config.TransactionField));
                        }
                        File.WriteAllText(PathToStorageXmlFile, new XElement(Config.XmlFileRoot, mergedElements ?? savedXml.Elements()).ToString());
                        DataSource = XElement.Load(PathToStorageXmlFile);
                    }
                    else throw new XmlException("Nothing to update or datum missing.");
                }
                MessageBox.Show("SSKA analyzer: The data bank is being updated.", "SSKA analyzer", MessageBoxButton.OK, MessageBoxImage.Information);
                return true;
            }
            catch (XmlException e)
            {
                MessageBox.Show(e.Message, Config.AppName + ": Xml File was not load!", MessageBoxButton.OK, MessageBoxImage.Error);
                isExceptionUnhandled = true;
                return false;
            }

            catch (Exception e)
            {
                MessageBox.Show(e.Message, Config.AppName + ": Unable to form Xml File", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
        private string[] ReplaceSymbolGetSeparator(ref string[] src, char symbol2Remove = '\"', char symbol2Replace = ' ')
        {
            for (int i = 0; i < src.GetLength(0); i++)
            {
                if (src[i].Contains(symbol2Remove))
                {
                    src[i] = src[i].Replace(symbol2Remove, symbol2Replace);
                }
            }
            return new string[] { Config.Delimiter4CSVFile };
        }
        private List<string[]> FilterStrings(string[] src, string[] sep)
        {
            List<string[]> res = new List<string[]>();
            foreach (string el in src)
            {
                string[] splittedArr = el.Split(sep, StringSplitOptions.None);
                for (int j = 0; j < splittedArr.Length; j++)
                {
                    splittedArr[j] = splittedArr[j].Trim();

                }
                res.Add(splittedArr);
            }
            return res;
        }
        private string[] GetHeader(List<string[]> textFileStrings)
        {
            return textFileStrings.FirstOrDefault(); // first line contains headers
        }
        private List<string[]> RemoveHeader(List<string[]> textFileStrings)
        {
            return textFileStrings.Skip(1).ToList();
        }
        public XElement GetXmlElem(string pathToFileToRead)
        {
            XElement res;
            try
            {
                lock (_fileLock)
                {
                    string[] sources = File.ReadAllLines(pathToFileToRead, /*Encoding.Default*/ Encoding.GetEncoding(Config.EncodePage)).ToArray<string>();
                    string[] sep = ReplaceSymbolGetSeparator(ref sources);
                    List<string[]> source = FilterStrings(sources, sep);
                    string[] sourceHeaders = GetHeader(source);

                    if (sourceHeaders.Length > 0)
                    {
                        source = RemoveHeader(source);
                        XElemBuilder XElemsBuilder = new X10ElemBuilder(sourceHeaders, AccountsLogic);
                        res = XElemsBuilder.BuildXElement(source);
                        if (res != null)
                            return res; //XElemsBuilder.BuildXElement(source) ?? new XElement("Root");
                        else throw new XmlException("Update of Xml DataBank file failed. ");
                    }
                    else
                    {
                        MessageBox.Show("Headers error", "Header in source file are missing.", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }

            catch (IOException e)
            {
                MessageBox.Show(e.Message, Config.AppName + ": File is blocked!", MessageBoxButton.OK, MessageBoxImage.Error);
                isExceptionUnhandled = true;
            }
            catch (XmlException e)
            {
                MessageBox.Show(e.Message, "!!! " + Config.AppName + ": Unable to create XmlElement.", MessageBoxButton.OK, MessageBoxImage.Error);
                isExceptionUnhandled = true;
            }
            return null;
        }


    }
}
