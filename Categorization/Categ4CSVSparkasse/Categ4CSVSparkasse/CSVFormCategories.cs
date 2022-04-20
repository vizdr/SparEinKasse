using Microsoft.VisualBasic.FileIO;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace Categ4CSVSparkasse
{
    public class CSVFormCategories
    {
        private readonly string pathToCategories;
        private readonly string pathToInputCSV;
        private readonly string pathToOutputCSV;
        private readonly string delimiter;
        private readonly int indexBeneficiaryField;
        private readonly int indexReasonForPaymentField;
        private readonly int indexBookingTextField;       
        private readonly string categoryId;
        private readonly string category;

        private string[] rowInputCSV;
        List<string> collectedTokensFromRow;
        private List<Tuple<KeyValuePair<int, string> /*category*/,
            HashSet<string> /*beneficiary*/, HashSet<string> /* reasonForPayment*/ , HashSet<string> /*bookingText*/>> categoryContexts;
        private static int categoryCounter;
        private List<Tuple<long /* row number */, KeyValuePair<int /* category ID */, string /* Category */>>> categoryInfo;
        private List<string> lines;
        public const string notFoundCategory = "NonCategorized";
        public CSVFormCategories(string pathToCategoriesCSV = @"../../../../../Categorization.csv", string pathToInputCSV = @"../../../../../MT940_Test_umsatz.csv", string pathToOutputCSV = @"../../../../../testOutput.csv",
            string categoryId = "CategoryID", string category = "Category",
            string delimiter = ";", int indexBeneficiaryField = 5, int indexReasonForPaymentField = 4, int indexBookingTextField = 3)
        {
            pathToCategories = pathToCategoriesCSV;
            this.pathToInputCSV = pathToInputCSV;
            this.pathToOutputCSV = pathToOutputCSV;

            categoryContexts = new List<Tuple<KeyValuePair<int, string>, HashSet<string>, HashSet<string>, HashSet<string>>>(30);
            categoryInfo = new();
            collectedTokensFromRow = new();
            
            this.categoryId = categoryId;
            this.category = category;           
            this.delimiter = delimiter;
            this.indexBeneficiaryField = indexBeneficiaryField;
            this.indexReasonForPaymentField = indexReasonForPaymentField;
            this.indexBookingTextField = indexBookingTextField;
            try
            {
                lines = File.ReadAllLines(pathToInputCSV).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Input CSV File for raw lines not found. {0,-120}", ex.Message);
            }            
        }

        public void GetCategoriesAndKeywordsFromFile()
        {
            try
            {
                using TextFieldParser parser = new(pathToCategories);
                List<string> recognizedCategories = new(30);
                recognizedCategories.Add(notFoundCategory);
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(delimiter);
                parser.ReadLine(); // Skip header
                int currentCategory = default;
                categoryContexts.Add(Tuple.Create(KeyValuePair.Create(currentCategory, notFoundCategory), new HashSet<string>(), new HashSet<string>(), new HashSet<string>()));

                while (!parser.EndOfData)
                {
                    //Process row
                    string[] fields = parser.ReadFields();
                    if ((fields is not null))
                    {
                        string receivedCategory = fields[0]?.Trim();
                        if (!recognizedCategories.Contains(receivedCategory ?? notFoundCategory))
                        {
                            currentCategory = ++categoryCounter;
                            KeyValuePair<int, string> newCategory = KeyValuePair.Create(currentCategory, fields[0].Trim());
                            categoryContexts.Add(Tuple.Create(newCategory, new HashSet<string>(), new HashSet<string>(), new HashSet<string>()));
                            recognizedCategories.Add(receivedCategory);
                        }

                        // Process fields
                        if (!string.IsNullOrWhiteSpace(fields[1]))
                        {
                            categoryContexts[currentCategory].Item2.Add(fields[1].ToLower().Trim()); // beneficiary
                        }
                        if (!string.IsNullOrWhiteSpace(fields[2]))
                        {
                            categoryContexts[currentCategory].Item3.Add(fields[2].ToLower().Trim());  // reasonForPayment
                        }
                        if (!string.IsNullOrWhiteSpace(fields[3]))
                        {
                            categoryContexts[currentCategory].Item4.Add(fields[3].ToLower().Trim()); // bookingText
                        }
                    }

                }
                for (int i = 0; i <= categoryCounter; i++)
                {
                    Console.WriteLine("{0,3}: {1,-25} | beneficiary: {2,-3} | reasonForPayment: {3,-3} | bookingText: {4,-3}",
                        i, categoryContexts[i].Item1.Value, categoryContexts[i].Item2.Count, categoryContexts[i].Item3.Count, categoryContexts[i].Item4.Count);
                }
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine("Categorization.csv with path {0,30} is not found. Exception: {1,-120}", pathToCategories, ex.Message);
            }
            
            
        }

        private void FormCategoryInfoForRow(string[] tokens, long rowNumber)
        {
            bool isCategoryDetermined = false;
            bool isFound = false;
            foreach (string token in tokens)
            {                
                foreach (var categoryConrext in categoryContexts)
                {
                    if (categoryConrext.Item2.Contains(token) || categoryConrext.Item3.Contains(token) || categoryConrext.Item4.Contains(token))
                    {
                        isCategoryDetermined = true;
                        var currentInfo = Tuple.Create(rowNumber, categoryConrext.Item1);
                        if (!categoryInfo.Contains(currentInfo))
                        {
                            categoryInfo.Add(currentInfo);
                        }
                        isFound = true;
                        break;
                    }                       
                }
                if (isFound)
                {
                    break;
                }
            }
            if(!isCategoryDetermined)
            {
                categoryInfo.Add(Tuple.Create(rowNumber + 1, KeyValuePair.Create(0, notFoundCategory)));
            }            
        }

        private string[] Tokenize(string field)
        {
            List<string> tokensList = new List<string>();

            string[] splitedFirst = field.Split('+');
            foreach (string splited in splitedFirst)
            {
                string[] splitedSecond = splited.Split(' ');
                foreach (string splitThird in splitedSecond)
                {
                    string[] tokens = splitThird.Split("//");
                    tokensList.AddRange(tokens);
                }
            }

            List<string> formatedTokens = new List<string>(tokensList.Count());
            tokensList.ForEach(field => formatedTokens.Add(field.ToLower().Trim()));
            return formatedTokens.ToArray();
        }

        public void FormCSVOutput()
        {
            if (lines is not null && lines[0] is not null)
            {
                lines[0] += delimiter + categoryId + delimiter + category;
                int rowNumber = 1;
                lines.Skip(1).ToList().ForEach(line =>
                {
                    lines[rowNumber] += delimiter + categoryInfo[rowNumber - 1].Item2.Key + delimiter + categoryInfo[rowNumber - 1].Item2.Value;
                    rowNumber++;
                });
            }
                                     
            //write the new content
            if(File.Exists(pathToOutputCSV))
            {
                try
                {
                    File.WriteAllLines(pathToOutputCSV, lines);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Raw lines are not valid. {0,-120}", ex.Message);
                }
            }
            else
            {
                Console.WriteLine("Output file: " + pathToOutputCSV + " is not found. Please create it and restart.");
            }
        }

        private void GetTokensFromFiels(string[] row)
        {
            collectedTokensFromRow.Clear();
            if (row.Length > 0)
            {
                collectedTokensFromRow.AddRange(Tokenize(row[indexBeneficiaryField]));
                collectedTokensFromRow.AddRange(Tokenize(row[indexReasonForPaymentField]));
                collectedTokensFromRow.AddRange(Tokenize(row[indexBookingTextField]));
            }
        }
        public void ProcessCSVInput()
        {
            categoryInfo.Clear();
            try
            {
                using TextFieldParser parser = new(pathToInputCSV);
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(delimiter);
                parser.ReadLine(); // Skip first line with headers
                while (!parser.EndOfData)
                {
                    //Process row of input file
                    try
                    {
                        rowInputCSV = parser.ReadFields();

                        if (rowInputCSV is not null)
                        {
                            GetTokensFromFiels(rowInputCSV);
                            FormCategoryInfoForRow(collectedTokensFromRow.ToArray(), parser.LineNumber - 2);
                        }
                    }
                    catch (MalformedLineException ex)
                    {
                        Console.WriteLine("InputCSVFile Line: " + ex.LineNumber + " Not valid and skipped." + parser.ErrorLine);
                        GetTokensFromFiels(new string[0]);
                        FormCategoryInfoForRow(collectedTokensFromRow.ToArray(), parser.ErrorLineNumber - 2);
                    }
                }
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine("Input CSV file with path {0,30} is not found. Exception: {1,-120}", pathToInputCSV, ex.Message);
            }
            
        }
    }
}
