using Dinosaur.Distributed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dinosaur.Practice.Samples
{
    public class Sample04 : ISample
    {
        private readonly ISerialIdGenerator _serialIdGenerator;

        public Sample04(ISerialIdGenerator serialIdGenerator) 
        {
            _serialIdGenerator = serialIdGenerator;
        }

        public void Execute()
        {
            string key = $"HW{DateTime.Now:yyyyMM}";

            _serialIdGenerator.Dialback(key, 10, ConditionWhen.NotExists, TimeSpan.FromDays(31));

            int length = 100_000;

            var nos = new long[length];

            Parallel.For(0, length, i =>
            {
                nos[i] = _serialIdGenerator.Increment(key);
            });

            Console.WriteLine(nos.Distinct().Count());
        }

        public async Task ExecuteAsync()
        {
            string key = $"HW{DateTime.Now:yyyyMM}";

            await _serialIdGenerator.DialbackAsync("HW202211", 10, ConditionWhen.NotExists, TimeSpan.FromDays(31));

            int length = 100_000;

            var nos = new long[length];

            Parallel.For(0, length, async i =>
            {
                nos[i] = await _serialIdGenerator.IncrementAsync(key);
            });

            Console.WriteLine(nos.Distinct().Count());

        }
    }
}
