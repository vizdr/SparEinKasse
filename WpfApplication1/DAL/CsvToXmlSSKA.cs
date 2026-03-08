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
        private string PathToStorageXmlFile { get; set; }
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
            return PathToFilesUtil.GetNonCategorizedInputCSVFilePaths.Length > 0;
        }

        public bool UpdateDataBank()
        {
            int filesCounter = 0;
            PathToFilesUtil.ObtainInputRawCsvFiles();

            if (isInputCsvFilesAvailable())
            {
                if (ProvideCategorization())
                {
                    // if there are raw CSV files, categorization should be processed before update. If there are categorized CSV files, update can be processed immediately.
                    int categorizedFilesCount = PathToFilesUtil.GetCategorizedInputCSVFilePaths.Length;
                    for (int i = 0; i < categorizedFilesCount; i++)
                    {
                        string pathToProcessedCSVFile = PathToFilesUtil.GetNameOfFirstInputCategorizedCsvFile();
                        if (!string.IsNullOrEmpty(pathToProcessedCSVFile) && TryUpdateSavedXml(pathToProcessedCSVFile))
                        {
                            PathToFilesUtil.MoveFileToArxiv(pathToProcessedCSVFile, "_arxive");
                            filesCounter++;
                        }
                    }
                }
            }
            else PathToFilesUtil.CreateSskaFolderAndFile();

            if (filesCounter > 0)
            {
                MessageBox.Show("Update is completed. It was processed " + filesCounter + (filesCounter == 1 ? " file." : " files."), Config.AppName, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                int remainingRawFiles = PathToFilesUtil.GetNonCategorizedInputCSVFilePaths.Length;
                if (remainingRawFiles > 0)
                {
                    MessageBox.Show("The " + remainingRawFiles + " file(s) to update were not processed.", Config.AppName);
                }
                else
                {
                    MessageBox.Show("No files to update found.", Config.AppName, MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            return filesCounter > 0;
        }

        private bool ProvideCategorization()
        {
            bool isCategorizationSucceeded = false;
            string[] rawCsvFiles = PathToFilesUtil.GetNonCategorizedInputCSVFilePaths;
            // categorization should be processed. Only raw CSV Input, w/o appended category fields, is accepted.
            if (rawCsvFiles.Length == 0)
                return isCategorizationSucceeded;

            foreach (string rawCsvFile in rawCsvFiles)
            {
                string csvFileWithCategoryFields = PathToFilesUtil.CreateCategorizedCsvFile(rawCsvFile);
                if (File.Exists(csvFileWithCategoryFields))
                {
                    // fill file with categories;
                    FormatterCSVCategories formatterCsv;
                    string path2CategorizationFile = FormatterCSVCategories.defaultPath2Categorization;
                    if (!File.Exists(path2CategorizationFile))
                    {
                        path2CategorizationFile = Config.PathToCategorizationFile;
                        if (!File.Exists(path2CategorizationFile))
                        {
                            path2CategorizationFile = @"../../../Categorization/" + Path.GetFileName(FormatterCSVCategories.defaultPath2Categorization);
                        }
                        if (!File.Exists(path2CategorizationFile))
                        {
                            MessageBox.Show("File to parse categories is missing: " + Path.GetFullPath(FormatterCSVCategories.defaultPath2Categorization) + " or: " + Path.GetFullPath(path2CategorizationFile));
                        }
                        else
                        {
                            formatterCsv = new FormatterCSVCategories(pathToCategoriesCSV: path2CategorizationFile, pathToInputCSV: rawCsvFile, pathToOutputCSV: csvFileWithCategoryFields);
                            ProcessCategoriyFile(formatterCsv);
                            isCategorizationSucceeded = true;
                        }
                    }
                    else
                    {
                        formatterCsv = new FormatterCSVCategories(pathToInputCSV: rawCsvFile, pathToOutputCSV: csvFileWithCategoryFields);
                        ProcessCategoriyFile(formatterCsv);
                        isCategorizationSucceeded = true;
                    }
                }

                PathToFilesUtil.MoveFileToArxiv(rawCsvFile, "_arxiv");
            }
            return isCategorizationSucceeded;
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
                    else throw new XmlException("Nothing to update or datum is missing.");
                }
                MessageBox.Show("SSKA analyzer: The data bank is being updated.", "SSKA analyzer", MessageBoxButton.OK, MessageBoxImage.Information);
                return true;
            }
            catch (XmlException e)
            {
                MessageBox.Show(e.Message, Config.AppName + ": Xml File was not load!", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, Config.AppName + ": Unable to form Xml File", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public bool UpdateCategorization()
        {
            // --- Resolve Categorization.csv path (same fallbacks as ProvideCategorization) ---
            string path2CatFile = FormatterCSVCategories.defaultPath2Categorization;
            if (!File.Exists(path2CatFile))
                path2CatFile = Config.PathToCategorizationFile;
            if (!File.Exists(path2CatFile))
                path2CatFile = @"../../../Categorization/" +
                               Path.GetFileName(FormatterCSVCategories.defaultPath2Categorization);
            if (!File.Exists(path2CatFile))
            {
                MessageBox.Show("Categorization file not found: " + Path.GetFullPath(path2CatFile),
                    Config.AppName, MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // --- Load category keywords (pass empty string for input CSV — constructor handles gracefully) ---
            var formatter = new FormatterCSVCategories(
                pathToCategoriesCSV: path2CatFile,
                pathToInputCSV: string.Empty,
                pathToOutputCSV: string.Empty);
            formatter.GetCategoriesAndKeywordsFromFile();

            if (formatter.GetCountOfAvailibleCategories == 0)
            {
                MessageBox.Show("No categories loaded from: " + path2CatFile, Config.AppName);
                return false;
            }

            // --- Update Arxiv.xml ---
            try
            {
                lock (_fileLock)
                {
                    XElement arxiv = XElement.Load(PathToStorageXmlFile);
                    foreach (var tx in arxiv.Elements(Config.TransactionField))
                    {
                        string buchungstext = tx.Element(Config.BuchungsTextField)?.Value ?? string.Empty;
                        string verwendungszweck = tx.Element(Config.VerwendZweckField)?.Value ?? string.Empty;
                        string beguenstigter = tx.Element(Config.BeguenstigterField)?.Value ?? string.Empty;

                        var cat = formatter.MatchCategory(beguenstigter, verwendungszweck, buchungstext);
                        tx.Element(Config.CategoryIdField)?.SetValue(cat.Key.ToString());
                        tx.Element(Config.CategoryField)?.SetValue(cat.Value);
                    }
                    File.WriteAllText(PathToStorageXmlFile, arxiv.ToString());
                    DataSource = arxiv;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Config.AppName + ": XML categorization update failed.",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // --- Update *categorized*.csv files in the Arxiv folder ---
            if (Directory.Exists(Config.PathToXmlStorageFolder))
            {
                foreach (string csvFile in Directory.GetFiles(Config.PathToXmlStorageFolder, "*categorized*.csv"))
                    UpdateCategorizationInCsvFile(csvFile, formatter);
            }

            return true;
        }

        private void UpdateCategorizationInCsvFile(string csvPath, FormatterCSVCategories formatter)
        {
            try
            {
                var encoding = Encoding.GetEncoding(Config.EncodePage);
                string[] lines = File.ReadAllLines(csvPath, encoding);
                if (lines.Length < 2) return;

                string[] headers = lines[0].Split(Config.Delimiter4CSVFile.ToCharArray());
                int catIdIdx = Array.IndexOf(headers, Config.CategoryIdField);
                int catIdx = Array.IndexOf(headers, Config.CategoryField);
                int buchungsIdx = Array.IndexOf(headers, Config.BuchungsTextField);
                int verwendungIdx = Array.IndexOf(headers, Config.VerwendZweckField);
                int beguenstigterIdx = Array.IndexOf(headers, Config.BeguenstigterField);

                if (catIdIdx < 0 || catIdx < 0) return; // columns not present — skip file

                for (int i = 1; i < lines.Length; i++)
                {
                    string[] fields = lines[i].Split(Config.Delimiter4CSVFile.ToCharArray());
                    if (fields.Length <= Math.Max(catIdIdx, catIdx)) continue;

                    string buchungstext = buchungsIdx >= 0 && buchungsIdx < fields.Length ? fields[buchungsIdx] : string.Empty;
                    string verwendungszweck = verwendungIdx >= 0 && verwendungIdx < fields.Length ? fields[verwendungIdx] : string.Empty;
                    string beguenstigter = beguenstigterIdx >= 0 && beguenstigterIdx < fields.Length ? fields[beguenstigterIdx] : string.Empty;

                    var cat = formatter.MatchCategory(beguenstigter, verwendungszweck, buchungstext);
                    fields[catIdIdx] = cat.Key.ToString();
                    fields[catIdx] = cat.Value;
                    lines[i] = string.Join(Config.Delimiter4CSVFile, fields);
                }

                File.WriteAllLines(csvPath, lines, encoding);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not update: " + Path.GetFileName(csvPath) + "\n" + ex.Message, Config.AppName);
            }
        }

        private string[] ReplaceSymbolGetSeparator(ref string[] src, char symbol2Remove = '\"', char symbol2Replace = ' ')
        {
            for (int i = 0; i < src.GetLength(0); i++)
            {
                if (src[i].Contains(symbol2Remove))
                    src[i] = src[i].Replace(symbol2Remove, symbol2Replace);
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
                    splittedArr[j] = splittedArr[j].Trim();
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
            try
            {
                lock (_fileLock)
                {
                    string[] sources = File.ReadAllLines(pathToFileToRead, Encoding.GetEncoding(Config.EncodePage));
                    string[] sep = ReplaceSymbolGetSeparator(ref sources);
                    List<string[]> source = FilterStrings(sources, sep);
                    string[] sourceHeaders = GetHeader(source);

                    if (sourceHeaders.Length > 0)
                    {
                        source = RemoveHeader(source);
                        XElemBuilder xElemsBuilder = new X10ElemBuilder(sourceHeaders, AccountsLogic);
                        XElement res = xElemsBuilder.BuildXElement(source);
                        if (res != null)
                            return res;
                        throw new XmlException("Update of Xml DataBank file failed.");
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
            }
            catch (XmlException e)
            {
                MessageBox.Show(e.Message, "!!! " + Config.AppName + ": Unable to create XmlElement.", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return null;
        }
    }
}
