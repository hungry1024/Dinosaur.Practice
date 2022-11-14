using Dinosaur.Distributed;

namespace Dinosaur.Practice.Samples
{
    public class Sample01 : ISample
    {
        public void Execute()
        {
            Parallel.For(0, 10, i =>
            {
                RedisDistributedLock.Lock("HelloWorld", () =>
                {
                    Console.WriteLine("{0} 我在执行HelloWorld. {1:yyyy-MM-dd HH:mm:ss.fff}", i, DateTime.Now);
                    Task.Delay(5000).Wait();
                    Console.WriteLine("{0} 我执行HelloWorld完毕. {1:yyyy-MM-dd HH:mm:ss.fff}\n", i, DateTime.Now);
                });
            });
        }

        public Task ExecuteAsync()
        {
            Parallel.For(0, 10, async i =>
            {
                await RedisDistributedLock.LockAsync("HelloWorld", async () =>
                {
                    await Task.Delay(5000);
                    Console.WriteLine("我在执行HelloWorld. {0:yyyy-MM-dd HH:mm:ss.fff}", DateTime.Now);
                });
            });

            return Task.CompletedTask;
        }
    }
}
