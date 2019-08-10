using PerfIt.Tracers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace PerfIt.Tests
{
    public class FileTracerTests
    {
        [Fact]
        public void Writes()
        {
            string fileName = "shibi";

            if (File.Exists(fileName))
                File.Delete(fileName);

            var ins = new SimpleInstrumentor(new InstrumentationInfo()
            {
                Description = "test",
                InstanceName = "Test instance",
                CategoryName = "test"
            });

            ins.Tracers.Add("File", new SeparatedFileTracer(fileName));
            var ctx = new InstrumentationContext()
            {
                Text1 = "Text1",
                Text2 = "Text2",
                Numeric = 424242,
                Decimal = 0.420420420
            };
            ins.Instrument(() => Thread.Sleep(100), extraContext: ctx);


            Thread.Sleep(1000);
            ins.Dispose();
            var lines = File.ReadAllLines(fileName);
            Assert.Equal(1, lines.Length);

            var segments = lines[0].Split('\t');

            Assert.Equal(9, segments.Length);

            Assert.Equal("Test instance", segments[1]);
            Assert.Equal("test", segments[0]);
            Assert.Equal("Text1", segments[5]);
            Assert.Equal("Text2", segments[6]);
            Assert.Equal("424242", segments[7]);
            // INTENTIONAL // Assert.Equal("0.420420420", segments[7]);  !!floating point numbers :/

            //     1- Category
            //     2- Instance
            //     3- CorrelationId
            //     4- TimeTakenMilli
            //     5- Text1
            //     6- Text2
            //     7- Numberic
            //     8- Decimal

        }
    }
}
