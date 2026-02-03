using System;
using System.Linq;
using WpfApplication1.Properties;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;

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
            try
            {
                var coll = sender as ObservableCollection<string>;
                if (coll == null || coll.Count == 0)
                    return;
                // Keep settings collection in sync with view collection order
                Settings.Default.DelimiterCSVInput.Clear();
                foreach (var v in coll)
                    Settings.Default.DelimiterCSVInput.Add(v);
            }
            catch
            {
                // Do not propagate UI changes as exceptions
            }
        }

        public void InitialazeCulture()
        {
            DiagnosticLog.Log("SettingsPresenter", "InitialazeCulture called");

            // Log current AppCultures state
            var appCultures = Settings.Default.AppCultures;
            if (appCultures != null)
            {
                DiagnosticLog.Log("SettingsPresenter", $"AppCultures count: {appCultures.Count}");
                for (int i = 0; i < appCultures.Count; i++)
                {
                    DiagnosticLog.Log("SettingsPresenter", $"  AppCultures[{i}]: {appCultures[i]}");
                }
            }
            else
            {
                DiagnosticLog.Log("SettingsPresenter", "AppCultures is NULL!");
            }

            // Recreate the main window with the new culture
            var cultureName = Settings.Default.AppCultures[0];
            DiagnosticLog.Log("SettingsPresenter", $"Creating culture from: {cultureName}");
            var culture = CultureInfo.CreateSpecificCulture(cultureName);
            DiagnosticLog.Log("SettingsPresenter", $"Calling App.ChangeCulture with: {culture.Name}");
            App.ChangeCulture(culture);
            DiagnosticLog.Log("SettingsPresenter", "InitialazeCulture completed");
        }

        public void UpdateSettings()
        {
            DiagnosticLog.Log("SettingsPresenter", "UpdateSettings called - saving settings");
            Settings.Default.Save();
            DiagnosticLog.Log("SettingsPresenter", "Settings saved");
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
            DiagnosticLog.Log("SettingsPresenter", $"OnAppCultureSelected - Action: {e.Action}");
            try
            {
                var coll = sender as ObservableCollection<string>;
                if (coll == null || coll.Count == 0)
                {
                    DiagnosticLog.Log("SettingsPresenter", "Collection is null or empty, returning");
                    return;
                }

                DiagnosticLog.Log("SettingsPresenter", $"View collection count: {coll.Count}");
                for (int i = 0; i < coll.Count; i++)
                {
                    DiagnosticLog.Log("SettingsPresenter", $"  View[{i}]: {coll[i]}");
                }

                // Mirror the view collection into settings to avoid index errors
                DiagnosticLog.Log("SettingsPresenter", "Mirroring to Settings.Default.AppCultures...");
                Settings.Default.AppCultures.Clear();
                foreach (var v in coll)
                    Settings.Default.AppCultures.Add(v);

                DiagnosticLog.Log("SettingsPresenter", $"Settings.Default.AppCultures updated, first item: {Settings.Default.AppCultures[0]}");
            }
            catch (Exception ex)
            {
                DiagnosticLog.Log("SettingsPresenter", $"EXCEPTION in OnAppCultureSelected: {ex.Message}");
                // swallow - UI ordering should not crash the app
            }
        }
    }
}
