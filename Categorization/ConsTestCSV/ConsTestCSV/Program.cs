using System;
using Categ4CSVSparkasse;

namespace ConsTestCSV
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello CSV Categories");
            CSVFormCategories worker = new();
            worker.GetCategoriesAndKeywordsFromFile();
            worker.ProcessCSVInput();
            worker.FormCSVOutput();
        }
    }
}
