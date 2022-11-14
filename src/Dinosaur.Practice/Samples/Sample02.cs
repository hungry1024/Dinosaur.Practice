using Dinosaur.Distributed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dinosaur.Practice.Samples
{
    public class Sample02 : ISample
    {
        private readonly IGuidGenerator _guidGenerator;

        public Sample02(IGuidGenerator guidGenerator)
        {
            _guidGenerator = guidGenerator;
        }

        public void Execute()
        {
            Console.WriteLine("AsString:");
            for(int i = 0; i < 10; i++)
            {
                Console.WriteLine(_guidGenerator.NewGuid());
            }

            Console.WriteLine("\nAsEnd:");
            var sqlGuids = new List<Guid>();
            for (int i = 0; i < 10; i++)
            {
                 sqlGuids.Add(_guidGenerator.NewGuid(SequentialGuidType.AtEnd));
            }

            sqlGuids.ForEach(g =>
            {
                Console.WriteLine("select cast('{0}' as uniqueidentifier) as Id union all", g);
            });

            int length = 1_000_000;
            var guidAsStrings = new Guid[length];
            var guidAtEnds = new Guid[length];
            Parallel.For(0, length, i =>
            {
                guidAsStrings[i] = _guidGenerator.NewGuid();
                guidAtEnds[i] = _guidGenerator.NewGuid(SequentialGuidType.AtEnd);
            });

            Console.WriteLine(guidAsStrings.Distinct().Count());
            Console.WriteLine(guidAtEnds.Distinct().Count());
        }

        public Task ExecuteAsync()
        {
            return Task.Run(() => Execute());
        }
    }
}
