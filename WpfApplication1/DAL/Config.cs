using System;
using System.Collections.Specialized;
using WpfApplication1.Properties;
using System.Globalization;

namespace WpfApplication1.DAL
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
        // Source CSV Headers
        public const string XmlFileRoot = "Root";
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
        public static string KontonummerField = Settings.Default.BeneficiaryAccField[0];
        public static string BLZField = Settings.Default.IntBankCodeField[0];
        public static string BetragField = Settings.Default.BankOperValueField[0];//Settings.Default.Properties["StorageFileName"].Name;
        public static string WaehrungField = Settings.Default.CurrencyField[0];
        public static string CurrGroupSeparator = NumberFormatInfo.CurrentInfo.CurrencyGroupSeparator;
        public static string CurrDecimalSeparator = NumberFormatInfo.CurrentInfo.CurrencyDecimalSeparator;
        public static string PathToXmlStorageFolder = Environment.GetFolderPath(
                                                    Environment.SpecialFolder.MyDocuments) + @"\MySSKA\Arxiv";
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
                default:
                    break;
            }
            return res;
        }

        public static string Delimiter4CSVFile = Settings.Default.DelimiterCSVInput[0];
        public static string CategoryIdField = "CategoryID";
        public static string CategoryField = "Category";
        public static string PathToCategorizationFile = PathToSskaDownloadsFolder + @"\Categorization\Categorization.csv";
        public static int CountCAMTFields = 16;
        public static string Preliminar = "vorgemerkt";
    }
}
