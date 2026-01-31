using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using WpfApplication1;
using WpfApplication1.BusinessLogic;
using WpfApplication1.DAL;
using WpfApplication1.DTO;

namespace WpfApplication1
{
    class Program
    {
        [STAThread]
        public static void Main()
        {
            // Set culture BEFORE creating any services that parse dates
            // This ensures DateTime.Parse() in BusinessLogicSSKA uses correct format
            CultureInfo CI = (CultureInfo)Thread.CurrentThread.CurrentCulture.Clone();
            CI.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
            Thread.CurrentThread.CurrentCulture = CI;
            // Create application host
            var host = Host.CreateDefaultBuilder()
                // Register services with DI container
                .ConfigureServices(services =>
                {
                    // DTO singletons (order matters due to dependencies)
                    services.AddSingleton<FilterViewModel>();
                    services.AddSingleton<DataRequest>();
                    services.AddSingleton<ResponseModel>();

                    // DAL services - CsvToXmlSSKA implements IDataSourceProvider
                    services.AddSingleton<CsvToXmlSSKA>();
                    services.AddSingleton<IDataSourceProvider>(sp => sp.GetRequiredService<CsvToXmlSSKA>());
                    services.AddSingleton<AccountsLogic>(sp =>
                        new AccountsLogic(sp.GetRequiredService<IDataSourceProvider>()));

                    // Business Logic
                    services.AddSingleton<IBusinessLogic, BusinessLogicSSKA>();
                    services.AddSingleton<BusinessLogicSSKA>();

                    // Presenters
                    services.AddTransient<ChartsPresenter>();

                    // UI
                    services.AddSingleton<Window1>();
                    services.AddSingleton<App>();
                })
                .Build();

            // Wire up circular dependency (CsvToXmlSSKA <-> AccountsLogic)
            var csvToXml = host.Services.GetRequiredService<CsvToXmlSSKA>();
            csvToXml.AccountsLogic = host.Services.GetRequiredService<AccountsLogic>();

            // Get the App service and run
            var app = host.Services.GetService<App>();
            app?.Run();
        }
    }
}
