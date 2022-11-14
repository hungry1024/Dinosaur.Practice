using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dinosaur.Practice.Samples
{
    public interface ISample
    {
        void Execute();

        Task ExecuteAsync();
    }
}
