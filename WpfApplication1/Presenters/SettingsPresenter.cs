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
            Settings.Default.Save();
            _viewSettings = viewSettings;

            // Bind each view collection to its Settings StringCollection.
            // Standard fields use a generic sync handler (Add/Remove).
            _viewSettings.AuftragsKontoFields  = BindField(Settings.Default.ContributorAccField);
            _viewSettings.BuchungstagFields    = BindField(Settings.Default.PaymentDateField);
            _viewSettings.WertDatumFields      = BindField(Settings.Default.BankOperDateField);
            _viewSettings.BuchungsTextFields   = BindField(Settings.Default.BankOperTypeField);
            _viewSettings.VerwendZweckFields   = BindField(Settings.Default.PaymentPurposeField);
            _viewSettings.BeguenstigerFields   = BindField(Settings.Default.BeneficiaryField);
            _viewSettings.KontonummerFields    = BindField(Settings.Default.BeneficiaryAccField);
            _viewSettings.BLZFields            = BindField(Settings.Default.IntBankCodeField);
            _viewSettings.BetragFields         = BindField(Settings.Default.BankOperValueField);
            _viewSettings.WaehrungFields       = BindField(Settings.Default.CurrencyField);

            // Special handlers: these collections use full-mirror or swap logic
            _viewSettings.EncodePages = new ObservableCollection<string>(Settings.Default.CodePage.Cast<string>());
            _viewSettings.EncodePages.CollectionChanged += OnEncodingPageChanged;
            _viewSettings.AppCultures = new ObservableCollection<string>(Settings.Default.AppCultures.Cast<string>());
            _viewSettings.AppCultures.CollectionChanged += OnAppCultureSelected;
            _viewSettings.DelimitersCSV = new ObservableCollection<string>(Settings.Default.DelimiterCSVInput.Cast<string>());
            _viewSettings.DelimitersCSV.CollectionChanged += OnCSVDelimiterChanged;
        }

        /// <summary>
        /// Creates an ObservableCollection from a Settings StringCollection and wires up
        /// a handler that mirrors Add/Remove actions back to the Settings collection.
        /// </summary>
        private static ObservableCollection<string> BindField(StringCollection settingsCollection)
        {
            var viewCollection = new ObservableCollection<string>(settingsCollection.Cast<string>());
            viewCollection.CollectionChanged += (sender, e) =>
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        settingsCollection.Add(e.NewItems[0] as string);
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        settingsCollection.Remove(e.OldItems[0] as string);
                        break;
                }
            };
            return viewCollection;
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

        public void UpdateSettings()
        {
            DiagnosticLog.Log("SettingsPresenter", "UpdateSettings called - saving settings");
            Settings.Default.Save();
            DiagnosticLog.Log("SettingsPresenter", "Settings saved");
        }

        public void ReloadSettings()
        {
            Settings.Default.Reset();
        }

        private static void OnEncodingPageChanged(object sender, NotifyCollectionChangedEventArgs e)
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
