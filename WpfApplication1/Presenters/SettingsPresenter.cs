using System.Linq;
using WpfApplication1.Properties;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System;

namespace WpfApplication1
{
    class SettingsPresenter
    {
        private IViewSettings _viewSettings;
        public SettingsPresenter(IViewSettings viewSettings)
        {
            //Settings.Default.Upgrade();
            //Settings.Default.Reload();
            Settings.Default.Save();
            _viewSettings = viewSettings;
            _viewSettings.AuftragsKontoFields = new ObservableCollection<string>(Settings.Default.ContributorAccField.Cast<string>());
            _viewSettings.AuftragsKontoFields.CollectionChanged += OnAuftragsKontoFieldChanged;
            _viewSettings.BuchungstagFields = new ObservableCollection<string>(Settings.Default.PaymentDateField.Cast<string>());
            _viewSettings.BuchungstagFields.CollectionChanged += OnBuchungstagFieldChanged;
            _viewSettings.WertDatumFields = new ObservableCollection<string>(Settings.Default.BankOperDateField.Cast<string>());
            _viewSettings.WertDatumFields.CollectionChanged += OnWertDatumFieldChanged;
            _viewSettings.BuchungsTextFields = new ObservableCollection<string>(Settings.Default.BankOperTypeField.Cast<string>());
            _viewSettings.BuchungsTextFields.CollectionChanged += OnBuchungsTextFielChanged;
            _viewSettings.VerwendZweckFields = new ObservableCollection<string>(Settings.Default.PaymentPurposeField.Cast<string>());
            _viewSettings.VerwendZweckFields.CollectionChanged += OnVerwendZweckFieldChanged;
            _viewSettings.BeguenstigerFields = new ObservableCollection<string>(Settings.Default.BeneficiaryField.Cast<string>());
            _viewSettings.BeguenstigerFields.CollectionChanged += OnBeguenstigerFieldChanged;
            _viewSettings.KontonummerFields = new ObservableCollection<string>(Settings.Default.BeneficiaryAccField.Cast<string>());
            _viewSettings.KontonummerFields.CollectionChanged += OnKontonummerFieldChanged;
            _viewSettings.BLZFields = new ObservableCollection<string>(Settings.Default.IntBankCodeField.Cast<string>());
            _viewSettings.BLZFields.CollectionChanged += OnBLZFieldChanged;
            _viewSettings.BetragFields = new ObservableCollection<string>(Settings.Default.BankOperValueField.Cast<string>());
            _viewSettings.BetragFields.CollectionChanged += OnBetragFieldChanged;
            _viewSettings.WaehrungFields = new ObservableCollection<string>(Settings.Default.CurrencyField.Cast<string>());
            _viewSettings.WaehrungFields.CollectionChanged += OnWaehrungFieldChanged;
            _viewSettings.EncodePages = new ObservableCollection<string>(Settings.Default.CodePage.Cast<string>());
            _viewSettings.EncodePages.CollectionChanged += OnMyEncodingPageChanged;
            _viewSettings.AppCultures = new ObservableCollection<string>(Settings.Default.AppCultures.Cast<string>());
            _viewSettings.AppCultures.CollectionChanged += OnAppCultureSelected;
            _viewSettings.DelimitersCSV = new ObservableCollection<string>(Settings.Default.DelimiterCSVInput.Cast<string>());
            _viewSettings.DelimitersCSV.CollectionChanged += OnCSVDelimiterChanged;
        }

        private void OnCSVDelimiterChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewStartingIndex != e.OldStartingIndex)
            {
                Settings.Default.DelimiterCSVInput.RemoveAt(e.OldStartingIndex);
                Settings.Default.DelimiterCSVInput.Insert(0, (sender as ObservableCollection<string>)[e.NewStartingIndex]);
            }
        }

        public void InitialazeCulture()
        {
            App.ChangeCulture(CultureInfo.CreateSpecificCulture(Settings.Default.AppCultures[0]));
        }

        public void UpdateSettings()
        {
            Settings.Default.Save();
            //InitialazeCulture();
        }

        public void ReloadSettings()
        {
            Settings.Default.Reset();
        }

        private static void OnAuftragsKontoFieldChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    Settings.Default.ContributorAccField.Add(e.NewItems[0] as string);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    Settings.Default.ContributorAccField.Remove(e.OldItems[0] as string);
                    break;
            default:
                    break;
            }
        }

        private static void OnBuchungstagFieldChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    Settings.Default.PaymentDateField.Add(e.NewItems[0] as string);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    Settings.Default.PaymentDateField.Remove(e.OldItems[0] as string);
                    break;
            default:
                    break;
            }
        }

        private static void OnWertDatumFieldChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    Settings.Default.BankOperDateField.Add(e.NewItems[0] as string);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    Settings.Default.BankOperDateField.Remove(e.OldItems[0] as string);
                    break;
                default:
                    break;
            }
        }

        private static void OnBuchungsTextFielChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    Settings.Default.BankOperTypeField.Add(e.NewItems[0] as string);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    Settings.Default.BankOperTypeField.Remove(e.OldItems[0] as string);
                    break;
                default:
                    break;
            }
        }

        private static void OnVerwendZweckFieldChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    Settings.Default.PaymentPurposeField.Add(e.NewItems[0] as string);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    Settings.Default.PaymentPurposeField.Remove(e.OldItems[0] as string);
                    break;
                default:
                    break;
            }
        }

        private static void OnBeguenstigerFieldChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    Settings.Default.BeneficiaryField.Add(e.NewItems[0] as string);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    Settings.Default.BeneficiaryField.Remove(e.OldItems[0] as string);
                    break;
            default:
                    break;
            }
        }

        private static void OnKontonummerFieldChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    Settings.Default.BeneficiaryAccField.Add(e.NewItems[0] as string);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    Settings.Default.BeneficiaryAccField.Remove(e.OldItems[0] as string);
                    break;
            default:
                    break;
            }
        }

        private static void OnBLZFieldChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    Settings.Default.IntBankCodeField.Add(e.NewItems[0] as string);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    Settings.Default.IntBankCodeField.Remove(e.OldItems[0] as string);
                    break;
            default:
                    break;
            }
        }

        private static void OnBetragFieldChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    Settings.Default.BankOperValueField.Add(e.NewItems[0] as string);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    Settings.Default.BankOperValueField.Remove(e.OldItems[0] as string);
                    break;
            default:
                    break;
            }
        }

        private static void OnWaehrungFieldChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    Settings.Default.CurrencyField.Add(e.NewItems[0] as string);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    Settings.Default.CurrencyField.Remove(e.OldItems[0] as string);
                    break;
            default:
                    break;
            }
        }

        private static void OnMyEncodingPageChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Settings.Default.CodePage.RemoveAt(e.NewStartingIndex);
            Settings.Default.CodePage.Insert(e.OldStartingIndex, (sender as ObservableCollection<string>)[e.OldStartingIndex]);
        }

        private void OnAppCultureSelected(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewStartingIndex != e.OldStartingIndex)
            {
                Settings.Default.AppCultures.RemoveAt(e.OldStartingIndex);
                Settings.Default.AppCultures.Insert(0, (sender as ObservableCollection<string>)[e.NewStartingIndex]);
            }
        }
    }
}
