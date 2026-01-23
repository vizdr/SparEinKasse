using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            // Create application host
            var host = Host.CreateDefaultBuilder()
                // Register services with DI container
                .ConfigureServices(services =>
                {
                    // DTO singletons (order matters due to dependencies)
                    services.AddSingleton<FilterViewModel>();
                    services.AddSingleton<DataRequest>();
                    services.AddSingleton<ResponseModel>();

                    // DAL services
                    services.AddSingleton<AccountsLogic>();
                    services.AddSingleton<CsvToXmlSSKA>();

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

            // Get the App service and run
            var app = host.Services.GetService<App>();
            app?.Run();
        }
    }
}
