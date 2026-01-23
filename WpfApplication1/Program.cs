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

namespace WpfApplication1
 {
class Program
    {
        [STAThread]
        public static void Main()
        {
            // создаем хост приложения
            var host = Host.CreateDefaultBuilder()
                // внедряем сервисы
                .ConfigureServices(services =>
                {
                    services.AddSingleton<App>();
                    services.AddSingleton<Window1>();
                    services.AddSingleton<BusinessLogicSSKA>();
                    services.AddTransient<AccountsLogic>();
                    services.AddTransient<CsvToXmlSSKA>();
                })
                .Build();
            // получаем сервис - объект класса App
            var app = host.Services.GetService<App>();
            // запускаем приложения
            app?.Run();
        }
    }
 }
