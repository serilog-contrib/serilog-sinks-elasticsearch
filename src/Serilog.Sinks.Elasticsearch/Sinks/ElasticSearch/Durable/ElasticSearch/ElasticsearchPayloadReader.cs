using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Serilog.Sinks.Elasticsearch.Durable
{
    /// <summary>
    /// 
    /// </summary>
    public class ElasticsearchPayloadReader: APayloadReader<List<string>>
    {
        private readonly string _pipelineName;
        private readonly string _typeName;
        private readonly Func<object, string> _serialize;
        private readonly Func<string, DateTime,string> _getIndexForEvent;
        private List<string> _payload;
        private int _count;
        private DateTime _date;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pipelineName"></param>
        /// <param name="typeName"></param>
        /// <param name="serialize"></param>
        /// <param name="getIndexForEvent"></param>
        public ElasticsearchPayloadReader(string pipelineName,string typeName, Func<object,string> serialize,Func<string,DateTime,string> getIndexForEvent)
        {
            _pipelineName = pipelineName;
            _typeName = typeName;
            _serialize = serialize;
            _getIndexForEvent = getIndexForEvent;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override List<string> GetNoPayload()
        {
            return new List<string>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filename"></param>
        protected override void InitPayLoad(string filename)
        {
            _payload = new List<string>();
            _count = 0;
            var lastToken = filename.Split('-').Last();

            // lastToken should be something like 20150218.json or 20150218_3.json now
            if (!lastToken.ToLowerInvariant().EndsWith(".json"))
            {
                throw new FormatException(string.Format("The file name '{0}' does not seem to follow the right file pattern - it must be named [whatever]-{{Date}}[_n].json", Path.GetFileName(filename)));
            }

            var dateString = lastToken.Substring(0, 8);
            _date = DateTime.ParseExact(dateString, "yyyyMMdd", CultureInfo.InvariantCulture);            
        }
       /// <summary>
       /// 
       /// </summary>
       /// <returns></returns>
        protected override List<string> FinishPayLoad()
        {
            return _payload;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nextLine"></param>
        protected override void AddToPayLoad(string nextLine)
        {
            var indexName = _getIndexForEvent(nextLine, _date);
            var action = default(object);

            if (string.IsNullOrWhiteSpace(_pipelineName))
            {
                action = new { index = new { _index = indexName, _type = _typeName, _id = _count + "_" + Guid.NewGuid() } };
            }
            else
            {
                action = new { index = new { _index = indexName, _type = _typeName, _id = _count + "_" + Guid.NewGuid(), pipeline = _pipelineName } };
            }

            var actionJson = _serialize(action);
            _payload.Add(actionJson);
            _payload.Add(nextLine);
            _count++;
        }
    }
}
