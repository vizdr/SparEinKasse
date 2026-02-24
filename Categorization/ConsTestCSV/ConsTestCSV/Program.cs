using System;
using CategoryFormatter;

namespace ConsTestCSV
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Debug launched to test of parse logic for discovery of Categories from .csv file");
            FormatterCSVCategories worker = new FormatterCSVCategories();
            worker.GetCategoriesAndKeywordsFromFile();
            worker.ProcessCSVInput();
            worker.FormCSVOutput();
        }
    }
}
