using System.Collections.ObjectModel;

namespace WpfApplication1
{
    interface IViewSettings
    {
        ObservableCollection<string> AuftragsKontoFields { set; get; }
        ObservableCollection<string> BuchungstagFields { set; get; }
        ObservableCollection<string> AppCultures { set; get; }
        ObservableCollection<string> WertDatumFields { set; get; }
        ObservableCollection<string> BuchungsTextFields { set; get; }
        ObservableCollection<string> VerwendZweckFields { set; get; }
        ObservableCollection<string> BeguenstigerFields { set; get; }
        ObservableCollection<string> KontonummerFields { set; get; }
        ObservableCollection<string> BLZFields { set; get; }
        ObservableCollection<string> BetragFields { set; get; }
        ObservableCollection<string> WaehrungFields { set; get; }
        ObservableCollection<string> EncodePages { set; get; }
    }
}
