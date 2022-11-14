using Dinosaur.Distributed;

namespace Dinosaur.Practice.Samples
{
    public class Sample03 : ISample
    {
        private readonly ISnowflakeIdGenerator _snowflakeIdGenerator;

        public Sample03(ISnowflakeIdGenerator snowflakeIdGenerator)
        {
            _snowflakeIdGenerator = snowflakeIdGenerator;
        }

        public void Execute()
        {
            for(int i = 0; i < 10; i++)
            {
                Console.WriteLine(_snowflakeIdGenerator.NewId());
            }

            int n = 1_000_000;
            var ids = new long[n];
            Parallel.For(0, n, i =>
            {
                ids[i] = _snowflakeIdGenerator.NewId();
            });

            Console.WriteLine(ids.Distinct().Count());
        }

        public Task ExecuteAsync()
        {
            return Task.Run(() => Execute());
        }
    }
}
