using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading;
using System.Globalization;
using WpfApplication1.Properties;

namespace WpfApplication1
{
    /// <summary>
    /// Interaction logic for WindowFieldsDictionary.xaml
    /// </summary>
    public partial class WindowFieldsDictionary : Window, IViewSettings
    {
        private SettingsPresenter sPresenter;
        private ObservableCollection<string> auftragsKontoFields;
        private ObservableCollection<string> buchungstagFields;
        private ObservableCollection<string> wertDatumFields;
        private ObservableCollection<string> buchungsTextFields;
        private ObservableCollection<string> verwendZweckFields;
        private ObservableCollection<string> beguenstigerFields;
        private ObservableCollection<string> kontonummerFields;
        private ObservableCollection<string> bLZFields;
        private ObservableCollection<string> betragFields;
        private ObservableCollection<string> waehrungFields;
        private ObservableCollection<string> encodePages;
        private ObservableCollection<string> appCultures;
        private string currentCulture;

        public WindowFieldsDictionary()
        {
            InitializeComponent();
            currentCulture = Thread.CurrentThread.CurrentUICulture.Name;
            sPresenter = new SettingsPresenter(this);
           
            if (appCultures.Contains(currentCulture))
            {
                int oldIndex = appCultures.IndexOf(currentCulture);
                appCultures.Move(oldIndex, 0);
                sPresenter.UpdateSettings();
            }
            comboBox_Local.SelectedIndex = 0;
            listBoxCodepageInputCSV.SelectedIndex = 0; 
            
            foreach (UIElement c in LayoutRoot.Children)
            {
                if (c is GroupBox)
                {
                    if ((c as GroupBox).HasHeader)
                    {
                        ((c as GroupBox).Header as Button).Click += Button_Click;
                    }
                }
            }

            foreach (UIElement c in LayoutRoot.Children)
            {
                if (c is GroupBox)
                {
                    (c as GroupBox).PreviewMouseDoubleClick += ListBoxItem_DoubleClick;
                }
            }

            btn_SaveAndClose.Click += delegate { sPresenter.UpdateSettings(); sPresenter.InitialazeCulture(); };
            btn_CancelAndClose.Click += delegate { sPresenter.ReloadSettings(); };
            comboBox_Local.SelectionChanged += comboBoxLocal_Selected;
        }

        private void ListBoxItem_DoubleClick(object sender, RoutedEventArgs e)
        {
            string s = (string)((GroupBox)e.Source).Name;
            switch (s)
            {
                case "gpb_AuftragsKonto" :
                    if (listBoxAuftragsKontoField.SelectedIndex != 0)
                    AuftragsKontoFields.Remove(listBoxAuftragsKontoField.SelectedValue.ToString());
                    break;
                case "gpb_Buchungstag" :
                    if (listBoxBuchungsTextField.SelectedIndex != 0)
                    BuchungstagFields.Remove(listBoxBuchungsTextField.SelectedValue.ToString());
                    break;
                case "gpb_WertDatum" :
                    if (listBoxWertDatumField.SelectedIndex != 0)
                    WertDatumFields.Remove(listBoxWertDatumField.SelectedValue.ToString());
                    break;
                case "gpb_Buchungstext" :
                    if (listBoxBuchungsTextField.SelectedIndex != 0)
                    BuchungsTextFields.Remove(listBoxBuchungsTextField.SelectedValue.ToString());
                    break;
                case "gpb_Verwendungszweck" :
                    if (listBoxVerwendungszweckField.SelectedIndex != 0)
                    VerwendZweckFields.Remove(listBoxVerwendungszweckField.SelectedValue.ToString());
                    break;
                case "gpb_Beguenstigter" :
                    if (listBoxBeguenstigterField.SelectedIndex != 0)
                    BeguenstigerFields.Remove(listBoxBeguenstigterField.SelectedValue.ToString());
                    break;
                case "gpb_Kontonummer" :
                    if (listBoxKontonummerField.SelectedIndex != 0)
                    KontonummerFields.Remove(listBoxKontonummerField.SelectedValue.ToString());
                    break;
                case "gpb_BLZ" :
                    if (listBoxBLZField.SelectedIndex != 0)
                    BLZFields.Remove(listBoxBLZField.SelectedValue.ToString());
                    break;
                case "gpb_Betrag" :
                    if (listBoxBetragField.SelectedIndex != 0)
                    BetragFields.Remove(listBoxBetragField.SelectedValue.ToString());
                    break;
                case "gpb_Waehrung" :
                    if (listBoxWaehrungField.SelectedIndex != 0)
                    WaehrungFields.Remove(listBoxWaehrungField.SelectedValue.ToString());
                    break;
                case "gpb_CodePageCSV" :
                    if(listBoxCodepageInputCSV.SelectedIndex != 0)
                    SweapEncoding();
                    break;

            }

        }

        private void SweapEncoding()
        {
            EncodePages.Move(0, listBoxCodepageInputCSV.SelectedIndex);         
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string s = (string)((Button)e.OriginalSource).Name;
            switch (s)
            {
                case "btn_Auftragskonto" :
                    AuftragsKontoFields.Add(txtBox_AddFieldValue.Text);
                     
                    break;
                case "btn_Buchungstag":
                    BuchungstagFields.Add(txtBox_AddFieldValue.Text);
                    listBoxBuchungstagField.ItemsSource = BuchungstagFields;
                    break;
                case "btn_WertDatum":
                    WertDatumFields.Add(txtBox_AddFieldValue.Text);
                    break;
                case "btn_Buchungstext":
                    BuchungsTextFields.Add(txtBox_AddFieldValue.Text);
                    break;
                case "btn_Verwendungszweck":
                    VerwendZweckFields.Add(txtBox_AddFieldValue.Text);
                    break;
                case "btn_Beguenstigter":
                    BeguenstigerFields.Add(txtBox_AddFieldValue.Text);
                    break;
                case "btn_Kontonummer":
                    KontonummerFields.Add(txtBox_AddFieldValue.Text);
                    break;
                case "btn_BLZ":
                    BLZFields.Add(txtBox_AddFieldValue.Text);
                    break;
                case "btn_Betrag":
                    BetragFields.Add(txtBox_AddFieldValue.Text);
                    break;
                case "btn_Waehrung":
                    WaehrungFields.Add(txtBox_AddFieldValue.Text);
                    break;
                case "btn_CodePageCSV":
                    if (listBoxCodepageInputCSV.SelectedIndex != -1 && listBoxCodepageInputCSV.SelectedIndex != 0)
                        SweapEncoding();
                    break;
            }
        }

        private  void comboBoxLocal_Selected(object sender, RoutedEventArgs e)
        {
            int oldIndex = (e.OriginalSource as ComboBox).SelectedIndex;
            appCultures.Move(oldIndex, 0);
        }


        #region IViewSettings Members

        public ObservableCollection<string> AuftragsKontoFields
        {
            get
            {
                return auftragsKontoFields;
            }
            set
            {
                auftragsKontoFields = value;
                listBoxAuftragsKontoField.ItemsSource = auftragsKontoFields;
            }
        }

        public ObservableCollection<string> BuchungstagFields
        {
            get
            {
                return buchungstagFields;
            }
            set
            {
                buchungstagFields = value;
                listBoxBuchungstagField.ItemsSource = buchungstagFields;
            }
        }

        public ObservableCollection<string> WertDatumFields
        {
            get
            {
                return wertDatumFields;
            }
            set
            {
                wertDatumFields = value;
                listBoxWertDatumField.ItemsSource = wertDatumFields;
            }
        }

        public ObservableCollection<string> BuchungsTextFields
        {
            get
            {
               return buchungsTextFields;
            }
            set
            {
                buchungsTextFields = value;
                listBoxBuchungsTextField.ItemsSource = buchungsTextFields;
            }
        }

        public ObservableCollection<string> VerwendZweckFields
        {
            get
            {
                return verwendZweckFields;
            }
            set
            {
                verwendZweckFields = value;
                listBoxVerwendungszweckField.ItemsSource = verwendZweckFields;
            }
        }

        public ObservableCollection<string> BeguenstigerFields
        {
            get
            {
                return beguenstigerFields;
            }
            set
            {
                beguenstigerFields = value;
                listBoxBeguenstigterField.ItemsSource = beguenstigerFields;
            }
        }

        public ObservableCollection<string> KontonummerFields
        {
            get
            {
                return kontonummerFields;
            }
            set
            {
                kontonummerFields = value;
                listBoxKontonummerField.ItemsSource = kontonummerFields;
            }
        }

        public ObservableCollection<string> BLZFields
        {
            get
            {
                return bLZFields;
            }
            set
            {
                bLZFields = value;
                listBoxBLZField.ItemsSource = bLZFields;
            }
        }

        public ObservableCollection<string> BetragFields
        {
            get
            {
                return betragFields;
            }
            set
            {
                betragFields = value;
                listBoxBetragField.ItemsSource = betragFields;
            }
        }

        public ObservableCollection<string> WaehrungFields
        {
            get
            {
                return waehrungFields;
            }
            set
            {
                waehrungFields = value;
                listBoxWaehrungField.ItemsSource = waehrungFields;
            }
        }

        public ObservableCollection<string> EncodePages
        {
            get
            {
                return encodePages;
            }
            set
            {
                encodePages = value;
                listBoxCodepageInputCSV.ItemsSource = encodePages;
            }
        }

        public ObservableCollection<string> AppCultures
        {
            get
            {
                return appCultures;
            }
            set
            {
                appCultures = value;
                comboBox_Local.ItemsSource = appCultures;
            }
        }
     
        #endregion
    }
}
