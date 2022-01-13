using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using WpfApplication1.Properties;
using System.Windows;
using System.Globalization;


namespace WpfApplication1.DAL
{
    public abstract class XElemBuilder
    {
        protected List<uint> keyIndexes;
        protected static Dictionary<uint, uint> targetHeaders;
        protected XElemBuilder Successor { get; set; }
        protected Dictionary<uint, uint> ResolveHeaders(string[] headers, CsvHeaderResolver fieldsResolver)
        {
            targetHeaders = new Dictionary<uint, uint>();

            for (uint i = 0; i < headers.Length; i++)
            {
                int fieldIndex = fieldsResolver.FindTargetFieldIndex(headers[i]); // index of required(target) field in Settings
                if (fieldIndex >= 0)
                    targetHeaders.Add((uint)fieldIndex, i); // TargetFieldIndex: Key -  to sort; i from parameter: headers[i] - Value
                                                            //else 
                                                            //throw new Exception(String.Format("Unable to resolve header {0} : {1}.", i, headers[i]));
            }
            return targetHeaders;
        }

        protected virtual bool IsAllKeyIndexesFound(Dictionary<uint, uint> targetHeaders)
        {
            IEnumerable<uint> targetIndexes = targetHeaders.Keys;
            return keyIndexes.Except<uint>(targetIndexes).Count<uint>() == 0;
        }

        protected static string GetAdoptedDouble(object dec)
        {
            double tmp = 0d;
            if (double.TryParse(dec.ToString(), NumberStyles.Any, CultureInfo.CurrentCulture, out tmp))
                return tmp.ToString(CultureInfo.InvariantCulture);
            if (double.TryParse(dec.ToString(), NumberStyles.Any, Config.GetCSVCultureInfo(), out tmp))
                return tmp.ToString(CultureInfo.InvariantCulture);
            try
            {
                tmp = double.Parse(dec.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture);
            }
            catch (FormatException e)
            {
                MessageBox.Show(e.Message, "Unable to parse string for the currency field. Try to change Encode Page in the Settings and reload the data.",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                throw new Exception("Please select appropriate CodePage in Settings");
            }
            return tmp.ToString("F2");
        }
        abstract public XElement BuildXElement(List<string[]> source);
    }

    public class XSimpleElemBuilder : XElemBuilder
    {
        public string SavedBankAccount { get; set; }
        public XSimpleElemBuilder() => this.keyIndexes = new List<uint>() { Settings.Default.PaymentDateFieldIndex, Settings.Default.BankOperDateFieldIndex, Settings.Default.BankOperTypeFieldIndex, Settings.Default.PaymentPurposeFieldIndex, Settings.Default.BeneficiaryFieldIndex, Settings.Default.BankOperValueFieldIndex };
        public static string BankAccount { get; set; }
        public override XElement BuildXElement(List<string[]> source)
        {
            if ((XElemBuilder.targetHeaders.Count + 1) >= keyIndexes.Count && IsAllKeyIndexesFound(XElemBuilder.targetHeaders))
            {
                if (targetHeaders[0].Equals(String.Empty))
                {
                    InitialaizeAccountsWindow();                                     // To get the value of bank account from user
                }
                try
                {
                    XElement cust = new XElement(Config.XmlFileRoot,
                    from fields in source
                    where !fields[0].Equals(string.Empty) & !fields[1].Equals(string.Empty)
                    select new XElement(Config.TransactionField, new XAttribute(Config.AuftragsKontoField, targetHeaders[0].Equals(String.Empty) ? BankAccount : fields[(int)targetHeaders[0]]),  // Auftragskonto
                        new XElement(Config.BuchungstagField, fields[(int)targetHeaders[1]]),                                                       // Buchungstag   
                        new XElement(Config.WertDatumField, Config.ExchYearDay(fields[(int)targetHeaders[2]]), new XAttribute("type", "date")),     // Valutadatum
                        new XElement(Config.BuchungsTextField, fields[(int)targetHeaders[3]]),                                                      // Buchungstext
                        new XElement(Config.VerwendZweckField, fields[(int)targetHeaders[4]]),                                                      // Verwendungszweck   
                        new XElement(Config.BeguenstigterField, fields[(int)targetHeaders[5]].Equals(string.Empty) ? "---" : fields[(int)targetHeaders[5]]),          // Begünstigter
                        new XElement(Config.KontonummerField, String.Empty),                                                                        // Kontonummer   
                        new XElement(Config.BLZField, String.Empty),                                                                                // BLZ   
                        new XElement(Config.BetragField,                                                                                            // Betrag       
                             GetAdoptedDouble(fields[(int)targetHeaders[8]]),
                        new XAttribute(Config.WaehrungField, " Euro"), new XAttribute("type", "double"))                                             // Währung
                        )
                    );
                    return cust;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Unable to parse.", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }
            }
            else if (Successor != null)
            {
                return Successor.BuildXElement(source);
            }
            else
            {
                MessageBox.Show(String.Format(" Only {0} of required header(s) is(are) resolved. \r\n The file was processed, but no new data is appended. \r\n The update of headers dictionary is required.", XElemBuilder.targetHeaders.Count));
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
        const uint completeNumberOfHeaders = 10U;
        public X10ElemBuilder(string[] headers)
        {
            this.keyIndexes = new List<uint>() { Settings.Default.ContributorAccFieldIndex, Settings.Default.PaymentDateFieldIndex,
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
            if (XElemBuilder.targetHeaders.Count >= completeNumberOfHeaders && IsAllKeyIndexesFound(XElemBuilder.targetHeaders))
            {
                try
                {
                    XElement cust = new XElement(Config.XmlFileRoot,
                       from fields in source
                       where !fields[1].Equals(string.Empty) & !fields[2].Equals(string.Empty)
                       select new XElement(Config.TransactionField, new XAttribute(Config.AuftragsKontoField, fields[(int)targetHeaders[0]]),    // Auftragskonto
                           new XElement(Config.BuchungstagField, Config.ExchYearDay(fields[(int)targetHeaders[1]]), new XAttribute("type", "date")), // Buchungstag   
                           new XElement(Config.WertDatumField, Config.ExchYearDay(fields[(int)targetHeaders[2]]), new XAttribute("type", "date")),  // Valutadatum
                           new XElement(Config.BuchungsTextField, fields[(int)targetHeaders[3]]),                                               // Buchungstext
                           new XElement(Config.VerwendZweckField, fields[(int)targetHeaders[4]]),                                               // Verwendungszweck   
                           new XElement(Config.BeguenstigterField, fields[(int)targetHeaders[5]].Equals(string.Empty) ? "---" : fields[(int)targetHeaders[5]]),      // Begünstigter
                           new XElement(Config.KontonummerField, fields[(int)targetHeaders[6]]),                                               // Kontonummer   
                           new XElement(Config.BLZField, fields[(int)targetHeaders[7]]),                                               // BLZ   
                           new XElement(Config.BetragField,                                                           // Betrag       
                                GetAdoptedDouble(fields[(int)targetHeaders[8]]),
                                   new XAttribute(Config.WaehrungField, fields[(int)targetHeaders[9]]), new XAttribute("type", "double"))   // Währung
                           )
                       );
                    return cust;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Unable to parse.", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }

            }
            WindowWarning dialog = new WindowWarning();
            dialog.labelMessage.Content = " Not all acting fields are found!\r\n Only basic functionality is availible.\r\n Press: \r\n Ok - To proceed; \r\n Stop - To cancel databank update. ";
            dialog.buttonOk.Click += delegate { dialog.Close(); };
            dialog.buttonStop.Click += delegate { dialog.Close(); Successor = null; };
            dialog.ShowDialog();

            if (Successor != null)
                return Successor.BuildXElement(source);
            return null;
        }
    }
}
