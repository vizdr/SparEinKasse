using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic.FileIO;

namespace CategoryFormatter
{
    public class FormatterCSVCategories
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
        private readonly Encoding csvSSKAEncoding;
        private List<Tuple<KeyValuePair<int, string> /*category*/,
            HashSet<string> /*beneficiary*/, HashSet<string> /* reasonForPayment*/ , HashSet<string> /*bookingText*/>> categoryContexts;
        private List<Tuple<long /* row number */, KeyValuePair<int /* category ID */, string /* Category */>>> categoryInfo;
        private List<string> lines;

        public static readonly string notFoundCategory = "NonCategorized";
        public static readonly int notFoundCategoryID = 0;
        public const string defaultPath2Categorization = @"../../../../../Categorization.csv";
        public string PathToCategories => pathToCategories;
        public int GetCountOfAvailibleCategories => categoryContexts.Count();
        public FormatterCSVCategories(string pathToCategoriesCSV = defaultPath2Categorization, string pathToInputCSV = @"../../../../../MT940_Test_umsatz.csv", string pathToOutputCSV = @"../../../../../testOutput.csv",
            string categoryId = "CategoryID", string category = "Category",
            string delimiter = ";", int indexBeneficiaryField = 5, int indexReasonForPaymentField = 4, int indexBookingTextField = 3)
        {
            pathToCategories = pathToCategoriesCSV;
            this.pathToInputCSV = pathToInputCSV;
            this.pathToOutputCSV = pathToOutputCSV;

            categoryContexts = new List<Tuple<KeyValuePair<int, string>, HashSet<string>, HashSet<string>, HashSet<string>>>(30);
            categoryInfo = new List<Tuple<long /* row number */, KeyValuePair<int /* category ID */, string /* Category */>>>();
            collectedTokensFromRow = new List<string>();

            this.categoryId = categoryId;
            this.category = category;
            this.delimiter = delimiter;
            this.indexBeneficiaryField = indexBeneficiaryField;
            this.indexReasonForPaymentField = indexReasonForPaymentField;
            this.indexBookingTextField = indexBookingTextField;
            try
            {
#if NETFRAMEWORK
                // Windows-1252 is available by default in .NET Framework
#else
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
#endif
                csvSSKAEncoding = Encoding.GetEncoding(1252);
                lines = File.ReadAllLines(pathToInputCSV, csvSSKAEncoding).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Input CSV File for raw lines not found. {0,-120}", ex.Message);
            }
        }

        public void GetCategoriesAndKeywordsFromFile(bool updateFlag = false)
        {
            int currentCategory = default;
            if (categoryContexts.Count > 0 && updateFlag)
            {
                categoryContexts.Clear();
            }
            else if (categoryContexts.Count == 0)           
            {
            }
            else 
            {
                return;
            }
            
            try
            {
                using (TextFieldParser parser = new TextFieldParser(pathToCategories))
                {
                    List<string> recognizedCategories = new List<string>(30);
                    recognizedCategories.Add(notFoundCategory);
                    parser.TextFieldType = FieldType.Delimited;
                    parser.SetDelimiters(delimiter);
                    parser.ReadLine(); // Skip header                    
                    categoryContexts.Add(Tuple.Create(new KeyValuePair<int, string>(currentCategory, notFoundCategory), new HashSet<string>(), new HashSet<string>(), new HashSet<string>()));

                    while (!parser.EndOfData)
                    {
                        //Process row
                        string[] fields = parser.ReadFields();
                        if ((fields != null))
                        {
                            string receivedCategory = fields[0]?.Trim();
                            if (!recognizedCategories.Contains(receivedCategory ?? notFoundCategory))
                            {
                                KeyValuePair<int, string> newCategory = new KeyValuePair<int, string>(++currentCategory, fields[0].Trim());
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


                }
                for (int i = 0; i <= currentCategory; i++)
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
            if (!isCategoryDetermined)
            {
                categoryInfo.Add(Tuple.Create(rowNumber + 1, new KeyValuePair<int, string>(notFoundCategoryID, notFoundCategory)));
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
                    string[] tokens = splitThird.Split(new char[2] { '/', '/' });
                    tokensList.AddRange(tokens);
                }
            }

            List<string> formatedTokens = new List<string>(tokensList.Count());
            tokensList.ForEach(fld => formatedTokens.Add(fld.ToLower().Trim()));
            return formatedTokens.ToArray();
        }

        public void FormCSVOutput()
        {
            if ((lines != null) && (lines[0] != null))
            {
                int rowNumber = 1;
                string[] headers = lines[0].Split( delimiter.ToCharArray() );
                if (!headers.ToList().Contains(category))
                {
                    lines[0] += delimiter + categoryId + delimiter + category;
                    lines.Skip(1).ToList().ForEach(line =>
                    {
                        lines[rowNumber] += delimiter + categoryInfo[rowNumber - 1].Item2.Key + delimiter + categoryInfo[rowNumber - 1].Item2.Value;
                        rowNumber++;
                    });
                }
                else
                {
                    lines.Skip(1).ToList().ForEach(line =>
                    {
                        lines[rowNumber] = lines[rowNumber].Remove(lines[rowNumber].Length - 2); // Remove last delimiters
                        lines[rowNumber] += delimiter + categoryInfo[rowNumber - 1].Item2.Key + delimiter + categoryInfo[rowNumber - 1].Item2.Value;
                        rowNumber++;
                    });
                }
                
               
            }

            //write the new content
            if (File.Exists(pathToOutputCSV))
            {
                try
                {
                    File.WriteAllLines(pathToOutputCSV, lines, csvSSKAEncoding);
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
                using (TextFieldParser parser = new TextFieldParser(pathToInputCSV))
                {

                    parser.TextFieldType = FieldType.Delimited;
                    parser.SetDelimiters(delimiter);
                    parser.ReadLine(); // Skip first line with headers
                    while (!parser.EndOfData)
                    {
                        //Process row of input file
                        try
                        {
                            rowInputCSV = parser.ReadFields();

                            if (rowInputCSV != null)
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

            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine("Input CSV file with path {0,30} is not found. Exception: {1,-120}", pathToInputCSV, ex.Message);
            }

        }

        /// <summary>
        /// Re-applies category matching for a single transaction's text fields.
        /// Call GetCategoriesAndKeywordsFromFile() once before using this.
        /// </summary>
        public KeyValuePair<int, string> MatchCategory(
            string beneficiary, string reasonForPayment, string bookingText)
        {
            var tokens = new List<string>();
            tokens.AddRange(Tokenize(beneficiary));
            tokens.AddRange(Tokenize(reasonForPayment));
            tokens.AddRange(Tokenize(bookingText));

            foreach (var token in tokens)
                foreach (var ctx in categoryContexts)
                    if (ctx.Item2.Contains(token) || ctx.Item3.Contains(token) || ctx.Item4.Contains(token))
                        return ctx.Item1;

            return new KeyValuePair<int, string>(notFoundCategoryID, notFoundCategory);
        }
    }
}
