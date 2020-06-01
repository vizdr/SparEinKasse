using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.IO;
using System.Xml;
using System.Globalization;
using System.Windows;
using WpfApplication1.Properties;
using WpfApplication1;
using System.Collections;
using System.Collections.Specialized;


namespace DataAccess
{
    public static class Config
    {
        static Config()
        {
            //Settings.Default.Upgrade();
            //Settings.Default.Reset();
            //Settings.Default.Reload();
            Settings.Default.Save();
        }
        private static readonly string[] DefaultCsvFieldsToReset = {"Transaction","Auftragskonto","Buchungstag","Valutadatum",
                                       "Buchungstext","Verwendungszweck","Begünstigter-Zahlungspflichtiger",
                                       "Kontonummer","BLZ","Betrag","Währung", "IBAN", "BIC" , "Info" };
        public static string TransactionField = Settings.Default.TransactionField[0];
        public static string AuftragsKontoField = Settings.Default.ContributorAccField[0];
        public static string BuchungstagField = Settings.Default.PaymentDateField[0];
        public static string WertDatumField = Settings.Default.BankOperDateField[0];
        public static string BuchungsTextField = Settings.Default.BankOperTypeField[0];
        public static string VerwendZweckField = Settings.Default.PaymentPurposeField[0];
        public static string BeguenstigterField = Settings.Default.BeneficiaryField[0];
        public static  string KontonummerField = Settings.Default.BeneficiaryAccField[0];
        public static string BLZField = Settings.Default.IntBankCodeField[0];
        public static string BetragField = Settings.Default.BankOperValueField[0];//Settings.Default.Properties["StorageFileName"].Name;
        public static string WaehrungField = Settings.Default.CurrencyField[0];
        public static string CurrGroupSeparator = NumberFormatInfo.CurrentInfo.CurrencyGroupSeparator;
        public static  string CurrDecimalSeparator = NumberFormatInfo.CurrentInfo.CurrencyDecimalSeparator;
        public static  string PathToXmlStorageFolder = Environment.GetFolderPath(
                                                    Environment.SpecialFolder.MyDocuments)+ @"\MySSKA\Arxiv";            
        public static readonly string PathToSskaDownloadsFolder = Environment.GetFolderPath(
                                                    Environment.SpecialFolder.MyDocuments) + @"\MySSKA";
        public static int EncodePage = Int32.Parse(Settings.Default.CodePage[0]); // 1252 - West;  1251 - Cyrilic
        public static readonly string AppName = "SSKA analyzer";
        public static StringCollection ContributorAccounts = Settings.Default.ContributorAccounts;
        public static string ExchYearDay(string iniDateString)
        {
            //replaces DD-MM-CCYY or DD-MM-YY to ssd:date format CCYY.MM.DD
            if (iniDateString.Length < 9)
                iniDateString = iniDateString.Insert(6, "20");
            string year = iniDateString.Substring(iniDateString.Length - 4);
            string month = iniDateString.Substring(iniDateString.Length - 7, 2);
            string day = iniDateString.Substring(0, 2);
            if ((month + day).Equals("0229"))
                day = "28";
            return year + "-" + month + "-" + day;
        }
        public static CultureInfo GetCSVCultureInfo()
        {
            CultureInfo res = CultureInfo.CurrentCulture;
            switch (EncodePage)
            {
                case 1252:
                    res = CultureInfo.GetCultureInfo("de-DE");
                    break;
                case 1251:
                    res = CultureInfo.GetCultureInfo("ru-RU");
                    break;
            }
            return res;
        }
        

    }

    static class PathToFilesUtil
    {
        private static readonly object _fileLock;       
        static PathToFilesUtil()
        {
            _fileLock = new object();
        }
        public static string GetNameInputCsvFile()
        {
            string[] files = {};
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

        

        public static string  CreateSskaFolderAndFile()
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
                        MessageBox.Show("The necessary folders and empty arxiv file 'Arxiv.xml' have been created.", Config.AppName ,MessageBoxButton.OK); 
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
                            for (int i = 0; File.Exists(GetArxivedCsvFilePath(fileName)); i++ ) fileName = fileName + "_" + i;                           
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
  
    public class CsvToXmlSSKA
    {
        public static string PathToStorageXmlFile { get; private set; }
        private static bool isExceptionUnhandled = false;
        private static bool isDataBankUpdateFailed = false;
        public static XElement DataSource { get; private set; }
        private readonly object _fileLock = new object();       
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
            //finally
            //{
            //    DataSource = null;
            //}
        }

        public static bool isInputCsvFileAvailable()
        {
            string[] files = { };
            if (Directory.Exists(Config.PathToSskaDownloadsFolder))
                files = Directory.GetFiles(Config.PathToSskaDownloadsFolder, @"*.csv");
            bool res = !isExceptionUnhandled & files.Length > 0 ? true : false;
            return res;
        }

        public void UpdateDataBank()
        {
            isExceptionUnhandled = false;
            int filesCounter = 0;
            if (isInputCsvFileAvailable())
            {
                while (isInputCsvFileAvailable())
                    if (TryUpdateSavedXml(PathToFilesUtil.GetNameInputCsvFile()))
                    {
                        PathToFilesUtil.MoveFileToArxiv();
                        filesCounter++;
                    }
            }
            else PathToFilesUtil.CreateSskaFolderAndFile();
            MessageBox.Show("Update is completed. It was processed " + filesCounter + (filesCounter == 1 ? " file." : " files."), Config.AppName, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public bool TryUpdateSavedXml(string fileToReadPath)
        {
            
            XElement savedXml = new XElement("Root");
            try
            {
                lock (_fileLock)
                {
                    XElement newXml = this.GetXmlElem(fileToReadPath);
                    if (newXml != null)
                    {
                        if (isInputCsvFileAvailable())
                            savedXml = XElement.Load(PathToStorageXmlFile);                    //  Xnodes collections to compare                                                                                                                
                        IEnumerable<XNode> mergedElements = savedXml.Nodes().Union<XNode>(newXml.Nodes(), XElement.EqualityComparer);
                        File.WriteAllText(PathToStorageXmlFile, new XElement("Root", mergedElements).ToString());
                        DataSource = XElement.Load(PathToStorageXmlFile);
                    }
                    else throw new XmlException("Nothing to update.");
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
        private string[] RefineStringsGetSeparator(ref string[] src) 
        {
            if (src[0].StartsWith("\"") && src[0].EndsWith("\""))
            {
                for (int i = 0; i < src.GetLength(0); i++)
                {
                    src[i] = src[i].Substring(1);
                    src[i] = src[i].Remove(src[i].Length - 1);
                }
                return new string[] { "\";\"" };
            }
            return new string[] { ";" };
        }
        private List<string[]> FilterStrings(string[] src, string[] sep)
        {
            List<string[]> res = new List<string[]>();
            foreach (string el in src)
            {
                string[] testArr = el.Split(sep, StringSplitOptions.None);
                //if (testArr.GetLength(0) >= Config.CsvFields.GetLength(0) && !string.IsNullOrEmpty(testArr[0]) && !string.IsNullOrEmpty(testArr[3]))
                    res.Add(testArr);
            }
            return res;
        }
        private string[] GetHeader(List<string[]> textFileStrings)
        {
            return textFileStrings.FirstOrDefault();
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
                    string[] sep = RefineStringsGetSeparator(ref sources);
                    List<string[]> source = FilterStrings(sources, sep);
                    string[] headers = GetHeader(source);
                    source = RemoveHeader(source);
                    XElemBuilder XElemsBuilder = new X10ElemBuilder(headers);
                    res = XElemsBuilder.BuildXElement(source);
                    if (res != null)
                        return res; //XElemsBuilder.BuildXElement(source) ?? new XElement("Root");
                    else throw new XmlException("Update of Xml DataBank file failed. "); 
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
