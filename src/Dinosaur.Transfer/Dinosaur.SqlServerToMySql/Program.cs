using Dinosaur.SqlServerToMySql.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Dinosaur.SqlServerToMySql
{
    class Program
    {
        static void Main(string[] args)
        {
            Settings.Initialize();

            var services = new ServiceCollection();
            ConfigureServices(services);
            using (ServiceProvider serviceProvider = services.BuildServiceProvider())
            {
                var app = serviceProvider.GetRequiredService<IStartup>();
                app.Run();
            }
            Console.WriteLine("\n按任意键退出 ...");
            Console.ReadKey();
        }

        private static void ConfigureServices(ServiceCollection services)
        {
            switch (Settings.Dbms)
            {
                case DbmsType.MySql:
                    {
                        services.AddTransient<ITransferDataSource>(sp => new MySqlDataSource(Settings.SourceConnectionString));
                        break;
                    }

                case DbmsType.SqlServer:
                    {
                        services.AddTransient<ITransferDataSource>(sp => new SqlServerDataSource(Settings.SourceConnectionString));
                        break;
                    }

                default:
                    throw new NotImplementedException();
            }

            services.AddTransient<MySqlStructure>();

            services.AddLogging(config =>
            {
                config.AddConfiguration(Settings.Configuration);
                config.AddConsole();
            });
            services.AddSingleton<IStartup, AppStartup>();
        }

    }
}
