using Dinosaur.SqlServerToMySql.Entities;
using Microsoft.Extensions.Logging;

namespace Dinosaur.SqlServerToMySql
{
    public class AppStartup : IStartup
    {
        private readonly ILogger<AppStartup> _logger;
        private readonly MySqlStructure _structure;

        public AppStartup(ILogger<AppStartup> logger, MySqlStructure structure)
        {
            _logger = logger;
            _structure = structure;
        }

        public void Run()
        {
            _structure.Create();

            _structure.SetEngine(MySqlEngine.MYISAM);

            //_structure.Transfer();

            //_structure.CreateTrigger4FuncDdefault();

            //_structure.Verify();

            //_structure.CreateIndex();

            //_structure.CreateForeign();

            _logger.LogInformation("程序执行完成");
        }
    }

    public interface IStartup
    {
        void Run();
    }
}
