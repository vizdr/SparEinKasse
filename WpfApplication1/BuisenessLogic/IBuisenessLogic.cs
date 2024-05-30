using WpfApplication1.DTO;

namespace WpfApplication1
{
    interface IBuisenessLogic
    {
        void UpdateData();
        void UpdateDataModel();
        void FilterData();
        void UpdateViewData();
        void FinalizeBL();
       // FilterViewModel Filter { get; set; }
        DataRequest Request { get; }
        ResponseModel ResponseModel { get; }
    }
}
