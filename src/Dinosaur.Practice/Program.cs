using Dinosaur.Distributed;
using Dinosaur.Practice.Samples;
using Microsoft.Extensions.DependencyInjection;


IServiceCollection services = new ServiceCollection();
services.AddDisnosaurDistributed(opts =>
{
    opts.NodeId = 1;
    opts.RedisConnectionString = "192.168.0.223:6379,defaultDatabase=5";
    opts.GuidType = SequentialGuidType.AsString;
    opts.SnowflakeIdOptions.StartTime = new DateTime(2022, 1, 1);
    opts.SnowflakeIdOptions.TimestampBitLength = 43;
    opts.SnowflakeIdOptions.NodeIdBitLength = 8;
});
services.AddTransient<ISample, Sample04>();

var serviceProvider = services.BuildServiceProvider();

var sample = serviceProvider.GetRequiredService<ISample>();

Console.WriteLine("开始 {0:yyyy-MM-dd HH:mm:ss.fff}\n", DateTime.Now);

//await sample.ExecuteAsync();

sample.Execute();

Console.WriteLine("\n按任意键退出...");
Console.ReadKey();