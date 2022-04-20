using System;
using WpfApplication1.Properties;
using System.Collections.Specialized;
using WpfApplication1.DAL;

namespace WpfApplication1
{
    abstract public class CsvHeaderResolver
    {
        public CsvHeaderResolver(CsvTargetField targetCSVField)
        {
            TargetCSVField = targetCSVField;
        }
        public CsvTargetField TargetCSVField { get; protected set; }
        abstract public int FindTargetFieldIndex(string headerField);
    }

    public class CsvTargetFieldResolver : CsvHeaderResolver
    {
        public CsvTargetFieldResolver(CsvTargetField targetCSVField) : base(targetCSVField) { }
        private static bool isReseted = false;
        public override int FindTargetFieldIndex(string headerField)
        {
            // recursive search of index
            StringEnumerator targetEnumerator = TargetCSVField.TargetFieldSynonyms.GetEnumerator();
            while (targetEnumerator.MoveNext())
            {
                if (targetEnumerator.Current.Equals(headerField))
                {
                    return (int)TargetCSVField.TargetFieldIndex;  // index in Settings
                }
            }

            if (TargetCSVField.Successor == null)
            {
                TargetCSVField = CsvTargetFieldsChain.Instance.FirstTargetField;
                if (isReseted)
                {
                    isReseted = false;
                    return -1;
                }
                else
                {
                    isReseted = true;
                    return FindTargetFieldIndex(headerField);
                }
            }
            else
            {
                TargetCSVField = TargetCSVField.Successor;
                return FindTargetFieldIndex(headerField);
            }
        }
    }

    public class CsvTargetField
    {
        public CsvTargetField(uint targetFieldIndex, StringCollection targetFieldSynonyms)
        {
            TargetFieldIndex = targetFieldIndex;
            TargetFieldSynonyms = targetFieldSynonyms;
        }
        public CsvTargetField Successor { get; set; }
        public uint TargetFieldIndex { get; private set; }
        public StringCollection TargetFieldSynonyms { get; private set; }
        public string GetFieldNameByIndex(uint key)
        {
            if (TargetFieldIndex == key)
                return TargetFieldSynonyms[0];
            else if (Successor != null)
                return Successor.GetFieldNameByIndex(key);
            else return String.Empty;
        }
    }

    public class CsvTargetFieldsChain
    {
        private readonly CsvTargetField firstTargetField;
        private static CsvTargetFieldsChain instance;
        private StringCollection categoryId = new StringCollection();
        private StringCollection category = new StringCollection();
        private CsvTargetFieldsChain()
        {
            categoryId.Add(Config.CategoryIdField);
            category.Add(Config.CategoryField);

            firstTargetField = new CsvTargetField(Settings.Default.ContributorAccFieldIndex, Settings.Default.ContributorAccField);
            firstTargetField.Successor = new CsvTargetField(Settings.Default.PaymentDateFieldIndex, Settings.Default.PaymentDateField);
            firstTargetField.Successor.Successor = new CsvTargetField(Settings.Default.BankOperDateFieldIndex, Settings.Default.BankOperDateField);
            firstTargetField.Successor.Successor.Successor = new CsvTargetField(Settings.Default.BankOperTypeFieldIndex, Settings.Default.BankOperTypeField);
            firstTargetField.Successor.Successor.Successor.Successor = new CsvTargetField(Settings.Default.PaymentPurposeFieldIndex, Settings.Default.PaymentPurposeField);
            firstTargetField.Successor.Successor.Successor.Successor.Successor = new CsvTargetField(Settings.Default.BeneficiaryFieldIndex, Settings.Default.BeneficiaryField);
            firstTargetField.Successor.Successor.Successor.Successor.Successor.Successor =
                new CsvTargetField(Settings.Default.BeneficiaryAccFieldIndex, Settings.Default.BeneficiaryAccField);
            firstTargetField.Successor.Successor.Successor.Successor.Successor.Successor.Successor =
                new CsvTargetField(Settings.Default.IntBankCodeFieldIndex, Settings.Default.IntBankCodeField);
            firstTargetField.Successor.Successor.Successor.Successor.Successor.Successor.Successor.Successor =
                new CsvTargetField(Settings.Default.BankOperValueFieldIndex, Settings.Default.BankOperValueField);
            firstTargetField.Successor.Successor.Successor.Successor.Successor.Successor.Successor.Successor.Successor =
                new CsvTargetField(Settings.Default.CurrencyFieldIndex, Settings.Default.CurrencyField);
            firstTargetField.Successor.Successor.Successor.Successor.Successor.Successor.Successor.Successor.Successor.Successor =
                new CsvTargetField(Settings.Default.IBAN_FieldIndex, Settings.Default.IBAN_Field);
            firstTargetField.Successor.Successor.Successor.Successor.Successor.Successor.Successor.Successor.Successor.Successor.Successor =
                new CsvTargetField(Settings.Default.BIC_FieldIndex, Settings.Default.BIC_Field);
            firstTargetField.Successor.Successor.Successor.Successor.Successor.Successor.Successor.Successor.Successor.Successor.Successor.Successor =
                new CsvTargetField(Settings.Default.InfoFieldIndex, Settings.Default.InfoField);
            firstTargetField.Successor.Successor.Successor.Successor.Successor.Successor.Successor.Successor.Successor.Successor.Successor.Successor.Successor =
                new CsvTargetField(Settings.Default.CategoryIdFieldIndex, categoryId);
            firstTargetField.Successor.Successor.Successor.Successor.Successor.Successor.Successor.Successor.Successor.Successor.Successor.Successor.Successor.Successor =
                new CsvTargetField(Settings.Default.CategoryFieldIndex, category);
        }

        public static CsvTargetFieldsChain Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new CsvTargetFieldsChain();
                }
                return instance;
            }
        }

        public CsvTargetField FirstTargetField => firstTargetField;
       
    }






























}
