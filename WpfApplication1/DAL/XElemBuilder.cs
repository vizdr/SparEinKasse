using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using WpfApplication1.Properties;
using System.Windows;
using System.Globalization;
using CategoryFormatter;
using WpfApplication1.BusinessLogic;

namespace WpfApplication1.DAL
{
    public abstract class XElemBuilder
    {
        protected List<uint>                    keyIndexes;
        protected static Dictionary<uint, uint> targetedHeaderFieldsIndexes;  // key - found index from Settings, value - index of the field in input hedaers, to be recognized 
        protected XElemBuilder                  Successor { get; set; }
        protected Dictionary<uint, uint> ResolveHeaders(string[] headers, CsvHeaderResolver fieldsResolver)
        {
            targetedHeaderFieldsIndexes = new Dictionary<uint, uint>();

            for (uint i = 0; i < headers.Length; i++)
            {
                int fieldIndex = fieldsResolver.FindTargetFieldIndex(headers[i]); // index of required(target) field in Settings, if not found: -1 
                if (fieldIndex >= 0)
                {
                    targetedHeaderFieldsIndexes.Add((uint)fieldIndex, i); // Key from Settings - to sort; Value from parameter: headers[i] 
                }
            }
            return targetedHeaderFieldsIndexes;
        }
        protected virtual bool           IsAllKeyIndexesFound(Dictionary<uint, uint> targetHeaders)
        {
            IEnumerable<uint> targetIndexes = targetHeaders.Keys;
            return keyIndexes.Except<uint>(targetIndexes).Count<uint>() == 0;
        }
        protected static string          GetCultureAdaptedDouble(object dec)
        {
            if (double.TryParse(dec.ToString(), NumberStyles.Any, CultureInfo.CurrentCulture, out double tmp))
            {
                return tmp.ToString(CultureInfo.InvariantCulture);
            }

            if (double.TryParse(dec.ToString(), NumberStyles.Any, Config.GetCSVCultureInfo(), out tmp))
            {
                return tmp.ToString(CultureInfo.InvariantCulture);
            }

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
        abstract public XElement         BuildXElement(List<string[]> source);
    }

    public class XBaseElemBuilder : XElemBuilder
    {
        AccountsLogic logicSSKA;
        public XBaseElemBuilder(AccountsLogic logicSSKA)
        {
            keyIndexes = new List<uint>() { Settings.Default.PaymentDateFieldIndex, Settings.Default.BankOperDateFieldIndex,
                                                                       Settings.Default.BankOperTypeFieldIndex, Settings.Default.PaymentPurposeFieldIndex,
                                                                       Settings.Default.BeneficiaryFieldIndex, Settings.Default.BankOperValueFieldIndex
                                                                      };
            this.logicSSKA = logicSSKA;
        }
        
        public override XElement BuildXElement(List<string[]> source)
        {
            if ((XElemBuilder.targetedHeaderFieldsIndexes.Count + 1) >= keyIndexes.Count && IsAllKeyIndexesFound(XElemBuilder.targetedHeaderFieldsIndexes))
            {
                if (targetedHeaderFieldsIndexes[0].Equals(String.Empty))
                {
                    InitialaizeAccountsWindow();                                     // Try to get the value of bank account from user
                }
                try
                {
                    bool isSourceCAMT = source.Count() == Config.CountCAMTFields;
                    XElement cust = new XElement(Config.XmlFileRoot,
                    from fields in source
                    where !fields[0].Equals(string.Empty) & !fields[1].Equals(string.Empty) && isSourceCAMT ? !fields[16].Contains(Config.Preliminar) : !fields[10].Contains(Config.Preliminar)
                    // index of targrtHeaders from Settings
                    select new XElement(Config.TransactionField, new XAttribute(Config.AuftragsKontoField, targetedHeaderFieldsIndexes[0].Equals(String.Empty) ? AccountsLogic.BankAccount : fields[(int)targetedHeaderFieldsIndexes[0]]),  // Auftragskonto
                        new XElement(Config.BuchungstagField, fields[(int)targetedHeaderFieldsIndexes[1]]),                                                                                                     // Buchungstag   
                        new XElement(Config.WertDatumField, Config.ExchLeapYearDay(fields[(int)targetedHeaderFieldsIndexes[2]]), new XAttribute("type", "date")),                                                   // Valutadatum
                        new XElement(Config.BuchungsTextField, fields[(int)targetedHeaderFieldsIndexes[3]]),                                                                                                    // Buchungstext
                        new XElement(Config.VerwendZweckField, fields[(int)targetedHeaderFieldsIndexes[4]]),                                                                                                    // Verwendungszweck   
                        new XElement(Config.BeguenstigterField, fields[(int)targetedHeaderFieldsIndexes[5]].Equals(string.Empty) ? "---" : fields[(int)targetedHeaderFieldsIndexes[5]]),                                      // Begünstigter
                        new XElement(Config.KontonummerField, String.Empty),                                                                                                                      // Kontonummer   
                        new XElement(Config.BLZField, String.Empty),                                                                                                                              // BLZ   
                        new XElement(Config.BetragField,                                                                                                                                          // Betrag       
                             GetCultureAdaptedDouble(fields[(int)targetedHeaderFieldsIndexes[8]]),
                        new XAttribute(Config.WaehrungField, " Euro"), new XAttribute("type", "double")),                                                                                          // Währung
                        new XElement(Config.CategoryIdField, String.IsNullOrEmpty(fields[(int)targetedHeaderFieldsIndexes[13]]) ? FormatterCSVCategories.notFoundCategoryID.ToString() : fields[(int)targetedHeaderFieldsIndexes[13]]),                                                                                                      // CategoryID
                        new XElement(Config.CategoryField, String.IsNullOrEmpty(fields[(int)targetedHeaderFieldsIndexes[14]]) ? FormatterCSVCategories.notFoundCategory : fields[(int)targetedHeaderFieldsIndexes[14]])                                                                                                         // Category
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
                MessageBox.Show(String.Format(" Only {0} of required header(s) is(are) resolved. \r\n The file was processed, but no new data is appended. \r\n The update of headers dictionary is required.", XElemBuilder.targetedHeaderFieldsIndexes.Count));
                return null;
            }
        }
        private void InitialaizeAccountsWindow()
        {
            WindowAccount window = new WindowAccount(logicSSKA);
            window.Activate();
            window.ShowDialog();
        }
    }

    public class X10ElemBuilder : XElemBuilder
    {
        const uint completeNumberOfHeaders = 12U; // 10 listed and additionaly categoryID and Category headers
        public X10ElemBuilder(string[] headers, AccountsLogic logicSSKA)
        {
            this.keyIndexes = new List<uint>() { Settings.Default.ContributorAccFieldIndex, Settings.Default.PaymentDateFieldIndex,
                Settings.Default.BankOperDateFieldIndex, Settings.Default.BankOperTypeFieldIndex,
                Settings.Default.PaymentPurposeFieldIndex, Settings.Default.BeneficiaryFieldIndex,
                Settings.Default.BeneficiaryAccFieldIndex, Settings.Default.IntBankCodeFieldIndex,
                Settings.Default.BankOperValueFieldIndex, Settings.Default.CurrencyFieldIndex
            };
            XElemBuilder.targetedHeaderFieldsIndexes = ResolveHeaders(headers, new CsvTargetFieldResolver(CsvTargetFieldsChain.Instance.FirstTargetField));
            Successor = new XBaseElemBuilder(logicSSKA);
        }
        public override XElement BuildXElement(List<string[]> source)
        {
            if (XElemBuilder.targetedHeaderFieldsIndexes.Count >= completeNumberOfHeaders && IsAllKeyIndexesFound(XElemBuilder.targetedHeaderFieldsIndexes))
            {
                try
                {
                    bool isSourceCAMT = source.Count() == Config.CountCAMTFields;
                    XElement cust = new XElement(Config.XmlFileRoot,
                       from fields in source
                       where !fields[1].Equals(string.Empty) & !fields[2].Equals(string.Empty) && isSourceCAMT ? !fields[16].Contains(Config.Preliminar) : !fields[10].Contains(Config.Preliminar)                                                  // indexes of targetHeaders from Settings
                       select new XElement(Config.TransactionField, new XAttribute(Config.AuftragsKontoField, fields[(int)targetedHeaderFieldsIndexes[0]]),                        // Auftragskonto
                           new XElement(Config.BuchungstagField, Config.ExchLeapYearDay(fields[(int)targetedHeaderFieldsIndexes[1]]), new XAttribute("type", "date")),                 // Buchungstag   
                           new XElement(Config.WertDatumField, Config.ExchLeapYearDay(fields[(int)targetedHeaderFieldsIndexes[2]]), new XAttribute("type", "date")),                   // Valutadatum
                           new XElement(Config.BuchungsTextField, fields[(int)targetedHeaderFieldsIndexes[3]]),                                                                    // Buchungstext
                           new XElement(Config.VerwendZweckField, fields[(int)targetedHeaderFieldsIndexes[4]]),                                                                    // Verwendungszweck   
                           new XElement(Config.BeguenstigterField, fields[(int)targetedHeaderFieldsIndexes[5]].Equals(string.Empty) ? "---" : fields[(int)targetedHeaderFieldsIndexes[5]]),      // Begünstigter
                           new XElement(Config.KontonummerField, fields[(int)targetedHeaderFieldsIndexes[6]]),                                                                     // Kontonummer   
                           new XElement(Config.BLZField, fields[(int)targetedHeaderFieldsIndexes[7]]),                                                                             // BLZ   
                           new XElement(Config.BetragField,                                                                                                          // Betrag       
                                GetCultureAdaptedDouble(fields[(int)targetedHeaderFieldsIndexes[8]]),
                                   new XAttribute(Config.WaehrungField, fields[(int)targetedHeaderFieldsIndexes[9]]), new XAttribute("type", "double")),                            // Währung
                            new XElement(Config.CategoryIdField, String.IsNullOrEmpty(fields[(int)targetedHeaderFieldsIndexes[13]]) ? FormatterCSVCategories.notFoundCategoryID.ToString() : fields[(int)targetedHeaderFieldsIndexes[13]]),                               // CategoryID                                                                                               
                            new XElement(Config.CategoryField, String.IsNullOrEmpty(fields[(int)targetedHeaderFieldsIndexes[14]]) ? FormatterCSVCategories.notFoundCategory : fields[(int)targetedHeaderFieldsIndexes[14]])                                                                        // Category
                           )
                       );
                    return cust;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Unable to parse.", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }
            } // End of if ( XElemBuilder.targetHeaders.Count >= completeNumberOfHeaders && IsAllKeyIndexesFound(XElemBuilder.targetHeaders)

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
