using System;
using CategoryFormatter;

namespace ConsTestCSV
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello CSV Categories");
            FormatterCSVCategories worker = new();
            worker.GetCategoriesAndKeywordsFromFile();
            worker.ProcessCSVInput();
            worker.FormCSVOutput();
        }
    }
}
