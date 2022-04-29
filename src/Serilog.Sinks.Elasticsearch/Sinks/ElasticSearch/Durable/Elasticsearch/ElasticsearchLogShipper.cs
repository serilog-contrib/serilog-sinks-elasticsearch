using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Sinks.Elasticsearch.Durable;

namespace Serilog.Sinks.Elasticsearch.Durable
{
    /// <summary>
    /// 
    /// </summary>
    public class ElasticsearchLogShipper : LogShipper<List<string>>
    {
        private readonly Action _registerTemplateIfNeeded;
        bool _didRegisterTemplateIfNeeded = false;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bufferBaseFilename"></param>
        /// <param name="batchPostingLimit"></param>
        /// <param name="period"></param>
        /// <param name="eventBodyLimitBytes"></param>
        /// <param name="levelControlSwitch"></param>
        /// <param name="logClient"></param>
        /// <param name="payloadReader"></param>
        /// <param name="retainedInvalidPayloadsLimitBytes"></param>
        /// <param name="bufferSizeLimitBytes"></param>
        /// <param name="registerTemplateIfNeeded"></param>
        /// <param name="rollingInterval"></param>
        public ElasticsearchLogShipper(string bufferBaseFilename, int batchPostingLimit, TimeSpan period,
            long? eventBodyLimitBytes, LoggingLevelSwitch levelControlSwitch, ILogClient<List<string>> logClient,
            IPayloadReader<List<string>> payloadReader, long? retainedInvalidPayloadsLimitBytes,
            long? bufferSizeLimitBytes, Action registerTemplateIfNeeded, RollingInterval rollingInterval)
            : base(bufferBaseFilename, batchPostingLimit, period, eventBodyLimitBytes, levelControlSwitch, logClient, 
                payloadReader, retainedInvalidPayloadsLimitBytes, bufferSizeLimitBytes, rollingInterval)
        {
            _registerTemplateIfNeeded = registerTemplateIfNeeded;                        
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected override async Task OnTick()
        {
            bool success = true;
            try
            {
                if (!_didRegisterTemplateIfNeeded)
                {
                    if (_registerTemplateIfNeeded != null)
                    {
                        _registerTemplateIfNeeded();
                        _didRegisterTemplateIfNeeded = true;
                    }
                }
            }
            catch (Exception ex)
            {
                SelfLog.WriteLine("Exception while emitting periodic batch from {0}: {1}", this, ex);
                _connectionSchedule.MarkFailure();
                success = false;
            }
            if (success)
                await base.OnTick();
        }
    }
}
