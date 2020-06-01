using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WpfApplication1.Properties;
using System.Collections.Specialized;
using System.Collections;

namespace WpfApplication1
{
    abstract public class CsvHeaderResolver
    {
        public CsvHeaderResolver(CsvTargetField target)
        {
            Target = target;
        }
        public CsvTargetField Target {get; protected set;}
        abstract public int FindTargetFieldIndex(string headerField); 
    }

    public class CsvTargetFieldResolver : CsvHeaderResolver
    {
        public CsvTargetFieldResolver(CsvTargetField input) : base(input) { }
        private static bool isReseted = false;
        public override int FindTargetFieldIndex(string headerField)
        {
            StringEnumerator targetEnumerator = Target.TargetFieldSynonims.GetEnumerator();
            while (targetEnumerator.MoveNext())
            {
                if (targetEnumerator.Current.Equals(headerField))
                    return Target.TargetFieldIndex;  // index in Settings
            }
            if (Target.Successor == null)
            {
                Target = CsvTargetFieldsChain.Instance;
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
                Target = Target.Successor;
                return FindTargetFieldIndex(headerField);
            } 
        }
    }

     public  class CsvTargetField
    {
         public CsvTargetField(int targetFieldIndex, StringCollection targetFieldSynonims)
         {
             TargetFieldIndex = targetFieldIndex;
             TargetFieldSynonims = targetFieldSynonims;
         }
        public CsvTargetField Successor { get;  set; }
        public int TargetFieldIndex { get;  private set; }
        public StringCollection TargetFieldSynonims { get; private set; }
        public string GetFieldNameByIndex(int key)
        {
            if (TargetFieldIndex == key)
                return TargetFieldSynonims[0];
            else if (Successor != null)
                return Successor.GetFieldNameByIndex(key);
            else return String.Empty;
        }
    }

     public class CsvTargetFieldsChain
     {
         private  readonly CsvTargetField firstTargetField;
         private static  CsvTargetFieldsChain instance;

         private CsvTargetFieldsChain() 
         {
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


         }

         public static CsvTargetField Instance
         { 
         get
             {
                 if (instance == null)
                     instance = new CsvTargetFieldsChain();
                 return instance.firstTargetField;            
             }
         }

     }



     

    
    



    

    
 


    


   
    

    

   

    

   

}
