using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PerfIt.Castle.Interception.Tests
{
    
    public class SimpleClass
    {
        [PerfIt(categoryName: "Vashah", InstanceName = "foofoo")]
        public virtual void Delay(int ms = 180)
        {
            Thread.Sleep(ms);
        }

        [PerfIt(categoryName: "Vashah", InstanceName = "foofoo")]
        public virtual Task DelayAsync(int ms = 180)
        {
            return Task.Delay(ms);
        }

        [PerfIt(categoryName: "Vashah", InstanceName = "foofoo")]
        public virtual async Task DelayThrowAsync(int ms = 180)
        {
            await Task.Delay(ms);
            throw new Exception("There you go!");
        }

    }
}
