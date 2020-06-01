using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Collections;
using WpfApplication1;
using WpfApplication1.Properties;
using System.Windows;
using System.Globalization;


namespace WpfApplication1.DAL
{
    public abstract class XElemBuilder
    {
        protected List<int> keyIndexes;
        protected static SortedList targetHeaders;
        protected XElemBuilder Successor { get; set; }
        protected SortedList ResolveHeaders(string[] headers, CsvHeaderResolver fieldsResolver)
        {
            targetHeaders = new SortedList();

            for (int i = 0; i < headers.Length; i++)   
            {
                int fieldIndex = fieldsResolver.FindTargetFieldIndex(headers[i]); // index of required(target) field in Settings
                if (fieldIndex >= 0)
                    targetHeaders.Add(fieldIndex, i); // TargetFieldIndex: Key -  to sort; i from parameter: headers[i] - Value
                //else 
                    //throw new Exception(String.Format("Unable to resolve header {0} : {1}.", i, headers[i]));
            }

            return targetHeaders;
        }

        protected virtual  bool IsAllKeyIndexesFound(SortedList targetHeaders)
        {
            IEnumerable<int> targetIndexes = targetHeaders.GetKeyList().Cast<int>();
            return keyIndexes.Except<int>(targetIndexes).Count<int>() == 0;
        }

        protected static string GetAdoptedDouble(object dec)
        {
            double tmp = 0d;
            if (double.TryParse(dec.ToString(), NumberStyles.Any, CultureInfo.CurrentCulture, out tmp))
                return tmp.ToString(CultureInfo.InvariantCulture);
            if(double.TryParse(dec.ToString(), NumberStyles.Any, Config.GetCSVCultureInfo(), out tmp))
                return tmp.ToString(CultureInfo.InvariantCulture);
            try
            {
                tmp = double.Parse(dec.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture);
            }
           
            catch (FormatException e)
            {
                MessageBox.Show(e.Message, "Unable to parse string in the currency. Try to change Encode Page in the Settings and reload the data.",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return tmp.ToString("F2");
        }

        abstract public XElement BuildXElement(List<string[]> source);
       
    }

    public class XSimpleElemBuilder : XElemBuilder
    {
        public string SavedBankAccount { get; set; }
        private static string bankAccount;
        public XSimpleElemBuilder(/*string[] headers*/)
        {
          
            this.keyIndexes = new List<int>() { Settings.Default.PaymentDateFieldIndex, Settings.Default.BankOperDateFieldIndex, Settings.Default.PaymentPurposeFieldIndex, Settings.Default.BankOperValueFieldIndex };
    
            //Successor = new X***ElemBuilder();
        }

        public static string BankAccount
        {
            get
            {
                return bankAccount;
            }
            set
            {
                bankAccount = value; 
            }
            
        }

        public override XElement BuildXElement(List<string[]> source)
        {
            if (XElemBuilder.targetHeaders.Count >= 4 && IsAllKeyIndexesFound(XElemBuilder.targetHeaders))
            {
                InitialaizeAccountsWindow();
                XElement cust = new XElement(Config.XmlFileRoot,
                    from fields in source
                    where !fields[0].Equals(string.Empty) & !fields[1].Equals(string.Empty)
                    select new XElement(Config.TransactionField, new XAttribute(Config.AuftragsKontoField, bankAccount),    // Auftragskonto
                        new XElement(Config.BuchungstagField, fields[(int)targetHeaders.GetByIndex(0)]),                                               // Buchungstag   
                        new XElement(Config.WertDatumField, Config.ExchYearDay(fields[(int)targetHeaders.GetByIndex(1)]), new XAttribute("type", "date")),  // Valutadatum
                        new XElement(Config.BuchungsTextField, "---"),                                               // Buchungstext
                        new XElement(Config.VerwendZweckField, fields[(int)targetHeaders.GetByIndex(2)]),                                               // Verwendungszweck   
                        new XElement(Config.BeguenstigterField, "---"),      // Begünstigter
                        new XElement(Config.KontonummerField, String.Empty),                                               // Kontonummer   
                        new XElement(Config.BLZField, String.Empty),                                               // BLZ   
                        new XElement(Config.BetragField,                                                           // Betrag       
                             GetAdoptedDouble(fields[(int)targetHeaders.GetByIndex(3)]),
                                new XAttribute(Config.WaehrungField, " Euro"), new XAttribute("type", "double"))   // Währung
                        )
                    );
                return cust;
            }
            else if (Successor != null)
            {
                return Successor.BuildXElement(source);
            }
            else
            {
                MessageBox.Show(String.Format(" Only {0} from  4 required header(s) is(are) resolved. \r\n The file was processed, but no new data is appended. \r\n The update of headers dictionary is required.", XElemBuilder.targetHeaders.Count));   
                return null;
            }
        }

        private void InitialaizeAccountsWindow()
        {
            WindowAccount window = new WindowAccount();
            window.Activate();
            window.ShowDialog();
        }
    }

    public class X10ElemBuilder : XElemBuilder
    {
        public X10ElemBuilder(string[] headers)
        {
            this.keyIndexes = new List<int>() { Settings.Default.ContributorAccFieldIndex, Settings.Default.PaymentDateFieldIndex,
                Settings.Default.BankOperDateFieldIndex, Settings.Default.BankOperTypeFieldIndex, 
                Settings.Default.PaymentPurposeFieldIndex, Settings.Default.BeneficiaryFieldIndex,
                Settings.Default.BeneficiaryAccFieldIndex, Settings.Default.IntBankCodeFieldIndex,
                Settings.Default.BankOperValueFieldIndex, Settings.Default.CurrencyFieldIndex
            };   
            XElemBuilder.targetHeaders = ResolveHeaders(headers, new CsvTargetFieldResolver(CsvTargetFieldsChain.Instance));
            Successor = new XSimpleElemBuilder();
        }
        public override XElement BuildXElement(List<string[]> source)
        {
            if (XElemBuilder.targetHeaders.Count >= 10 && IsAllKeyIndexesFound(XElemBuilder.targetHeaders))
                {
                    XElement cust = new XElement(Config.XmlFileRoot,
                        from fields in source
                        where !fields[1].Equals(string.Empty) & !fields[2].Equals(string.Empty) 
                        select new XElement(Config.TransactionField, new XAttribute(Config.AuftragsKontoField, fields[(int)targetHeaders.GetByIndex(0)]),    // Auftragskonto
                            new XElement(Config.BuchungstagField, Config.ExchYearDay(fields[(int)targetHeaders.GetByIndex(1)]), new XAttribute("type", "date")), // Buchungstag   
                            new XElement(Config.WertDatumField, Config.ExchYearDay(fields[(int)targetHeaders.GetByIndex(2)]), new XAttribute("type", "date")),  // Valutadatum
                            new XElement(Config.BuchungsTextField, fields[(int)targetHeaders.GetByIndex(3)]),                                               // Buchungstext
                            new XElement(Config.VerwendZweckField, fields[(int)targetHeaders.GetByIndex(4)]),                                               // Verwendungszweck   
                            new XElement(Config.BeguenstigterField, fields[(int)targetHeaders.GetByIndex(5)].Equals(string.Empty) ? "---" : fields[(int)targetHeaders.GetByIndex(5)]),      // Begünstigter
                            new XElement(Config.KontonummerField, fields[(int)targetHeaders.GetByIndex(6)]),                                               // Kontonummer   
                            new XElement(Config.BLZField, fields[(int)targetHeaders.GetByIndex(7)]),                                               // BLZ   
                            new XElement(Config.BetragField,                                                           // Betrag       
                                 GetAdoptedDouble(fields[(int)targetHeaders.GetByIndex(8)]),    
                                    new XAttribute(Config.WaehrungField, fields[(int)targetHeaders.GetByIndex(9)]), new XAttribute("type", "double"))   // Währung
                            )
                        );
                    return cust;
                }
            WindowWarning dialog = new WindowWarning();
            dialog.labelMessage.Content = " Not all acting fields are found!\r\n Only basic functionality is availible.\r\n Press: \r\n Ok - To proceed; \r\n Stop - To cancel databank update. ";
            dialog.buttonOk.Click += delegate { dialog.Close(); };
            dialog.buttonStop.Click += delegate { dialog.Close(); Successor = null; };
            dialog.ShowDialog();
            
            if(Successor != null)
                 return Successor.BuildXElement(source);
            return null;   
        }
    }

    
}
