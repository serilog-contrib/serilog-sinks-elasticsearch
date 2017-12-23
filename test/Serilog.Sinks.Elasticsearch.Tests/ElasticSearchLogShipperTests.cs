using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Serilog.Sinks.Elasticsearch.Tests
{
    public class ElasticSearchLogShipperTests
    {
        //To enable tests :
        // * Create public key token for Testproject, make Serilog.Sinks.Elasticsearch internals visible to testproject & scope ElasticsearchLogShipper to internal.
        // * or make ElasticsearchLogShipper (temporarily) public.

        //[Fact]
        //public void ElasticsearchLogShipper_TryReadLineShouldReadWithBOM()
        //{
        //    bool withBom = true;

        //    string testLine1 = "test 1";
        //    string testLine2 = "test 2";

        //    using (MemoryStream s = new MemoryStream())
        //    using (StreamWriter sw = new StreamWriter(s, new UTF8Encoding(withBom)))
        //    {
        //        sw.WriteLine(testLine1);
        //        sw.WriteLine(testLine2);
        //        sw.Flush();

        //        string nextLine;
        //        long nextStart = 0;

        //        ElasticsearchLogShipper.TryReadLine(s,ref nextStart, out nextLine);

        //        Assert.Equal(Encoding.UTF8.GetByteCount(testLine1) + +Encoding.UTF8.GetByteCount(Environment.NewLine) + 3, nextStart);
        //        Assert.Equal(testLine1, nextLine);

        //        ElasticsearchLogShipper.TryReadLine(s, ref nextStart, out nextLine);
        //        Assert.Equal(testLine2, nextLine);
        //    }
        //}

        //[Fact]
        //public void ElasticsearchLogShipper_TryReadLineShouldReadWithoutBOM()
        //{
        //    bool withBom = false;

        //    string testLine1 = "test 1";
        //    string testLine2 = "test 2";
        //    Encoding.UTF8.GetBytes(testLine1);

        //    using (MemoryStream s = new MemoryStream())
        //    using (StreamWriter sw = new StreamWriter(s, new UTF8Encoding(withBom)))
        //    {
        //        sw.WriteLine(testLine1);
        //        sw.WriteLine(testLine2);
        //        sw.Flush();

        //        string nextLine;
        //        long nextStart = 0;

        //        ElasticsearchLogShipper.TryReadLine(s, ref nextStart, out nextLine);

        //        Assert.Equal(Encoding.UTF8.GetByteCount(testLine1) + Encoding.UTF8.GetByteCount(Environment.NewLine), nextStart);
        //        Assert.Equal(testLine1, nextLine);

        //        ElasticsearchLogShipper.TryReadLine(s, ref nextStart, out nextLine);
        //        Assert.Equal(testLine2, nextLine);

        //    }
        //}
    }
}
