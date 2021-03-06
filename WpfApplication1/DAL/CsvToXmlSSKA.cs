﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using WpfApplication1.DAL;
using System.Windows;
using System.IO;
using WpfApplication1.Properties;
using System.Xml;
using System.Collections;

namespace WpfApplication1.DAL
{
    class CsvToXmlSSKA
    {
        public static string PathToStorageXmlFile { get; private set; }
        private static bool isExceptionUnhandled = false;
        public static XElement DataSource { get; private set; }
        private readonly object _fileLock = new object();
        public CsvToXmlSSKA()
        {

            PathToStorageXmlFile = Config.PathToXmlStorageFolder + @"\" + Settings.Default.StorageFileName;
            try
            {
                if (File.Exists(PathToStorageXmlFile))
                    DataSource = XElement.Load(PathToStorageXmlFile);
                else
                {
                    DataSource = XElement.Load(PathToFilesUtil.CreateSskaFolderAndFile());
                    MessageBox.Show("The data bank file is empty.", Config.AppName, MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, Config.AppName + ": Xml Storage File was not load!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            //finally
            //{
            //    DataSource = null;
            //}
        }

        public static bool isInputCsvFileAvailable()
        {
            string[] files = { };
            if (Directory.Exists(Config.PathToSskaDownloadsFolder))
                files = Directory.GetFiles(Config.PathToSskaDownloadsFolder, @"*.csv");
            bool res = !isExceptionUnhandled & files.Length > 0 ? true : false;
            return res;
        }

        public bool UpdateDataBank()
        {
            isExceptionUnhandled = false;
            int filesCounter = 0;
            if (isInputCsvFileAvailable())
            {
                while (isInputCsvFileAvailable())
                    if (TryUpdateSavedXml(PathToFilesUtil.GetNameInputCsvFile()))
                    {
                        PathToFilesUtil.MoveFileToArxiv();
                        filesCounter++;
                    }
            }
            else PathToFilesUtil.CreateSskaFolderAndFile();
            if (filesCounter > 0)
            {
                MessageBox.Show("Update is completed. It was processed " + filesCounter + (filesCounter == 1 ? " file." : " files."), Config.AppName, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("File(s) to update not found." , Config.AppName);
            }
            return (filesCounter > 0);
        }

        public bool TryUpdateSavedXml(string fileToReadPath)
        {
            IEnumerable<XElement> mergedElements = null;
            XElement savedXml = new XElement(Config.XmlFileRoot);
            try
            {
                lock (_fileLock)
                {
                    XElement newXml = this.GetXmlElem(fileToReadPath);
                    if (newXml != null && newXml.HasElements)
                    {
                        if (isInputCsvFileAvailable())
                            savedXml = XElement.Load(PathToStorageXmlFile);
                        if (savedXml.HasElements)
                        {
                            mergedElements = savedXml.Elements(Config.TransactionField).Union(newXml.Elements(Config.TransactionField), new SSKA_XmlEqualityComparer());
                        }                     
                        else
                        {
                            savedXml.AddFirst(newXml.Elements(Config.TransactionField));                      
                        }
                        File.WriteAllText(PathToStorageXmlFile, new XElement(Config.XmlFileRoot, mergedElements ?? savedXml.Elements()).ToString());
                        DataSource = XElement.Load(PathToStorageXmlFile);
                    }
                    else throw new XmlException("Nothing to update or datum missing.");
                }
                MessageBox.Show("SSKA analyzer: The data bank is being updated.", "SSKA analyzer", MessageBoxButton.OK, MessageBoxImage.Information);
                return true;
            }
            catch (XmlException e)
            {
                MessageBox.Show(e.Message, Config.AppName + ": Xml File was not load!", MessageBoxButton.OK, MessageBoxImage.Error);
                isExceptionUnhandled = true;
                return false;
            }

            catch (Exception e)
            {
                MessageBox.Show(e.Message, Config.AppName + ": Unable to form Xml File", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
        private string[] RefineStringsGetSeparator(ref string[] src)
        {
            if (src[0].StartsWith("\"") && src[0].EndsWith("\""))
            {
                for (int i = 0; i < src.GetLength(0); i++)
                {
                    src[i] = src[i].Substring(1);
                    src[i] = src[i].Remove(src[i].Length - 1);
                }
                return new string[] { "\";\"" };
            }
            return new string[] { ";" };
        }
        private List<string[]> FilterStrings(string[] src, string[] sep)
        {
            List<string[]> res = new List<string[]>();
            foreach (string el in src)
            {
                string[] testArr = el.Split(sep, StringSplitOptions.None);
                //if (testArr.GetLength(0) >= Config.CsvFields.GetLength(0) && !string.IsNullOrEmpty(testArr[0]) && !string.IsNullOrEmpty(testArr[3]))
                res.Add(testArr);
            }
            return res;
        }
        private string[] GetHeader(List<string[]> textFileStrings)
        {
            return textFileStrings.FirstOrDefault();
        }
        private List<string[]> RemoveHeader(List<string[]> textFileStrings)
        {
            return textFileStrings.Skip(1).ToList();
        }
        public XElement GetXmlElem(string pathToFileToRead)
        {
            XElement res;
            try
            {
                lock (_fileLock)
                {
                    string[] sources = File.ReadAllLines(pathToFileToRead, /*Encoding.Default*/ Encoding.GetEncoding(Config.EncodePage)).ToArray<string>();
                    string[] sep = RefineStringsGetSeparator(ref sources);
                    List<string[]> source = FilterStrings(sources, sep);
                    string[] headers = GetHeader(source);
                    source = RemoveHeader(source);
                    XElemBuilder XElemsBuilder = new X10ElemBuilder(headers);
                    res = XElemsBuilder.BuildXElement(source);
                    if (res != null)
                        return res; //XElemsBuilder.BuildXElement(source) ?? new XElement("Root");
                    else throw new XmlException("Update of Xml DataBank file failed. ");
                }
            }

            catch (IOException e)
            {
                MessageBox.Show(e.Message, Config.AppName + ": File is blocked!", MessageBoxButton.OK, MessageBoxImage.Error);
                isExceptionUnhandled = true;
            }
            catch (XmlException e)
            {
                MessageBox.Show(e.Message, "!!! " + Config.AppName + ": Unable to create XmlElement.", MessageBoxButton.OK, MessageBoxImage.Error);
                isExceptionUnhandled = true;
            }
            return null;
        }
    }
}
