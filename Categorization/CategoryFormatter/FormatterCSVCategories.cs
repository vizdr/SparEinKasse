using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

        private readonly Encoding csvSSKAEncoding;
        private List<Tuple<KeyValuePair<int, string> /*category*/,
            HashSet<string> /*beneficiary*/, HashSet<string> /* reasonForPayment*/ , HashSet<string> /*bookingText*/>> categoryContexts;
        private List<KeyValuePair<int /* category ID */, string /* Category */>> categoryInfo;
        private List<string> lines;

        public static readonly string notFoundCategory = "NonCategorized";
        public static readonly int notFoundCategoryID = 0;
        public const string defaultPath2Categorization = @"../../../../../../Categorization.csv";
        public string PathToCategories => pathToCategories;
        public int GetCountOfAvailibleCategories => categoryContexts.Count();

        public FormatterCSVCategories(string pathToCategoriesCSV = defaultPath2Categorization, string pathToInputCSV = @"../../../../../../MT940_Test_umsatz.csv", string pathToOutputCSV = @"../../../../../../testOutput.csv",
            string categoryId = "CategoryID", string category = "Category",
            string delimiter = ";", int indexBeneficiaryField = 5, int indexReasonForPaymentField = 4, int indexBookingTextField = 3)
        {
            pathToCategories = pathToCategoriesCSV;
            this.pathToInputCSV = pathToInputCSV;
            this.pathToOutputCSV = pathToOutputCSV;

            categoryContexts = new List<Tuple<KeyValuePair<int, string>, HashSet<string>, HashSet<string>, HashSet<string>>>(30);
            categoryInfo = new List<KeyValuePair<int, string>>();

            this.categoryId = categoryId;
            this.category = category;
            this.delimiter = delimiter;
            this.indexBeneficiaryField = indexBeneficiaryField;
            this.indexReasonForPaymentField = indexReasonForPaymentField;
            this.indexBookingTextField = indexBookingTextField;
            try
            {
                // Robustly obtain code page 1252 (Windows-1252). On some runtimes (e.g. .NET Core)
                // the code pages provider must be registered. On others (e.g. full .NET Framework)
                // this is not necessary. Attempt registration, ignore if not available,
                // then try to get the encoding and fall back to Encoding.Default if needed.
                try
                {
                    // Try registering the provider if available (no-op on runtimes that don't need it).
                    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                }
                catch
                {
                    // Ignore: registration may not be necessary or available on this runtime.
                }

                try
                {
                    csvSSKAEncoding = Encoding.GetEncoding(1252);
                }
                catch
                {
                    try
                    {
                        // Try by name as a fallback
                        csvSSKAEncoding = Encoding.GetEncoding("windows-1252");
                    }
                    catch
                    {
                        // Final fallback to system default to avoid throwing here.
                        csvSSKAEncoding = Encoding.Default;
                    }
                }

                lines = File.ReadAllLines(pathToInputCSV, csvSSKAEncoding).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Input CSV File for raw lines or Encoding error. {0,-120}", ex.Message);
            }
        }

        public void GetCategoriesAndKeywordsFromFile(bool updateFlag = false)
        {
            if (categoryContexts.Count > 0)
            {
                if (!updateFlag) return;
                categoryContexts.Clear();
            }

            try
            {
                int currentCategory = default;
                using (TextFieldParser parser = new TextFieldParser(pathToCategories, csvSSKAEncoding))
                {
                    HashSet<string> recognizedCategories = new HashSet<string>();
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
                                categoryContexts[currentCategory].Item2.Add(fields[1].ToLowerInvariant().Trim()); // beneficiary
                            }
                            if (!string.IsNullOrWhiteSpace(fields[2]))
                            {
                                categoryContexts[currentCategory].Item3.Add(fields[2].ToLowerInvariant().Trim());  // reasonForPayment
                            }
                            if (!string.IsNullOrWhiteSpace(fields[3]))
                            {
                                categoryContexts[currentCategory].Item4.Add(fields[3].ToLowerInvariant().Trim()); // bookingText
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

        private void FormCategoryInfoForRow(string normalizedString, string[] tokens)
        {
            string combined = normalizedString ?? string.Empty;
            foreach (var categoryContext in categoryContexts)
            {
                if (KeywordSetMatches(categoryContext.Item2, tokens, combined) ||
                    KeywordSetMatches(categoryContext.Item3, tokens, combined) ||
                    KeywordSetMatches(categoryContext.Item4, tokens, combined))
                {
                    categoryInfo.Add(categoryContext.Item1);
                    return;
                }
            }
            categoryInfo.Add(new KeyValuePair<int, string>(notFoundCategoryID, notFoundCategory));
        }

        // Hilfsmethode: Prüft, ob irgendein Keyword aus 'set' in den Tokens oder der kombinierten Zeichenkette vorkommt.
        // Ein-Wort-Keywords werden gegen die token-Liste gesucht, mehrwortige Keywords (enthalten Leerzeichen)
        // werden als Substring in der kombinierten normalisierten Zeichenkette geprüft.
        private bool KeywordSetMatches(HashSet<string> set, string[] tokens, string combinedLower)
        {
            if (set == null || set.Count == 0)
                return false;

            var tokenSet = new HashSet<string>(tokens ?? Array.Empty<string>());

            foreach (var kw in set)
            {
                if (string.IsNullOrWhiteSpace(kw))
                    continue;

                string keyword = kw.Trim().ToLowerInvariant();

                if (keyword.Contains(" "))
                {
                    if (!string.IsNullOrEmpty(combinedLower) && combinedLower.Contains(keyword))
                        return true;
                }
                else
                {
                    if (tokenSet.Contains(keyword))
                        return true;
                }
            }

            return false;
        }

        private string[] Tokenize(string field)
        {
            if (string.IsNullOrWhiteSpace(field))
                return Array.Empty<string>();

            List<string> tokensList = new List<string>();

            string[] splitedFirst = field.Split('+');
            foreach (string splited in splitedFirst)
            {
                string[] splitedSecond = splited.Split(' ');
                foreach (string splitThird in splitedSecond)
                {
                    // correct delimiter: once '/'
                    string[] tokens = splitThird.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                    tokensList.AddRange(tokens);
                }
            }

            List<string> formatedTokens = new List<string>(tokensList.Count());
            tokensList.ForEach(fld =>
            {
                if (!string.IsNullOrWhiteSpace(fld))
                    formatedTokens.Add(fld.ToLowerInvariant().Trim());
            });
            return formatedTokens.ToArray();
        }

        public void FormCSVOutput()
        {
            if (lines == null || lines.Count == 0)
                return;

            int rowNumber = 1;
            string[] headers = lines[0].Split(delimiter.ToCharArray());
            if (!headers.ToList().Contains(category))
            {
                lines[0] += delimiter + categoryId + delimiter + category;
                lines.Skip(1).ToList().ForEach(line =>
                {
                    lines[rowNumber] += delimiter + categoryInfo[rowNumber - 1].Key + delimiter + categoryInfo[rowNumber - 1].Value;
                    rowNumber++;
                });
            }
            else
            {
                lines.Skip(1).ToList().ForEach(line =>
                {
                    // Strip the last two delimiter-separated fields (old CategoryID;Category) before appending updated values.
                    int lastDelim = lines[rowNumber].LastIndexOf(delimiter);
                    int prevDelim = lastDelim > 0 ? lines[rowNumber].LastIndexOf(delimiter, lastDelim - 1) : -1;
                    lines[rowNumber] = prevDelim >= 0 ? lines[rowNumber].Substring(0, prevDelim) : lines[rowNumber];
                    lines[rowNumber] += delimiter + categoryInfo[rowNumber - 1].Key + delimiter + categoryInfo[rowNumber - 1].Value;
                    rowNumber++;
                });
            }

            try
            {
                File.WriteAllLines(pathToOutputCSV, lines, csvSSKAEncoding);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Output CSV write failed. {0,-120}", ex.Message);
            }
        }

        private (string normalized, string[] tokens) CollectTokensFromFields(string[] row)
        {
            string beneficiary = string.Empty;
            string reason = string.Empty;
            string booking = string.Empty;

            if (row != null && row.Length > 0)
            {
                if (indexBeneficiaryField >= 0 && indexBeneficiaryField < row.Length)
                    beneficiary = row[indexBeneficiaryField] ?? string.Empty;
                if (indexReasonForPaymentField >= 0 && indexReasonForPaymentField < row.Length)
                    reason = row[indexReasonForPaymentField] ?? string.Empty;
                if (indexBookingTextField >= 0 && indexBookingTextField < row.Length)
                    booking = row[indexBookingTextField] ?? string.Empty;
            }

            string normalized = (beneficiary + "***" + reason + "***" + booking).ToLowerInvariant().Trim();

            var tokensList = new List<string>();
            tokensList.AddRange(Tokenize(beneficiary));
            tokensList.AddRange(Tokenize(reason));
            tokensList.AddRange(Tokenize(booking));
            return (normalized, tokensList.ToArray());
        }

        public void ProcessCSVInput()
        {
            categoryInfo.Clear();
            try
            {
                using (TextFieldParser parser = new TextFieldParser(pathToInputCSV, csvSSKAEncoding))
                {
                    parser.TextFieldType = FieldType.Delimited;
                    parser.SetDelimiters(delimiter);
                    parser.ReadLine(); // Skip first line with headers
                    while (!parser.EndOfData)
                    {
                        try
                        {
                            string[] rowInputCSV = parser.ReadFields();
                            if (rowInputCSV != null)
                            {
                                var (normalized, tokens) = CollectTokensFromFields(rowInputCSV);
                                FormCategoryInfoForRow(normalized, tokens);
                            }
                        }
                        catch (MalformedLineException ex)
                        {
                            Console.WriteLine("InputCSVFile Line: " + ex.LineNumber + " Not valid and skipped." + parser.ErrorLine);
                            var (normalized, tokens) = CollectTokensFromFields(new string[0]);
                            FormCategoryInfoForRow(normalized, tokens);
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
            var tokensList = new List<string>();
            tokensList.AddRange(Tokenize(beneficiary));
            tokensList.AddRange(Tokenize(reasonForPayment));
            tokensList.AddRange(Tokenize(bookingText));
            string[] tokens = tokensList.ToArray();

            string combined = (beneficiary + "***" + reasonForPayment + "***" + bookingText).ToLowerInvariant().Trim();

            foreach (var ctx in categoryContexts)
            {
                if (KeywordSetMatches(ctx.Item2, tokens, combined) ||
                    KeywordSetMatches(ctx.Item3, tokens, combined) ||
                    KeywordSetMatches(ctx.Item4, tokens, combined))
                    return ctx.Item1;
            }
            return new KeyValuePair<int, string>(notFoundCategoryID, notFoundCategory);
        }
    }
}
