using System.Collections.Generic;
using System.Text;
using System.Threading;
using HuaweiCloud.SDK.LTS.Model;


namespace HuaweiCloud.SDK.LTS
{
    public class LogAccumulator
    {
        public ReaderWriterLockSlim RwLock { get; set; }
        public Dictionary<string, Batch> LogGroupData { get; set; }
        public Config ProducerConfig { get; set; }

        public IO_Worker IoWorker { get; set; }

        private bool _shutDownFlag;

        public bool ShutDownFlag
        {
            get
            {
                lock (this)
                {
                    return _shutDownFlag;
                }
            }
            set
            {
                lock (this)
                {
                    _shutDownFlag = value;
                }
            }
        }

        public IO_ThreadPool IoThreadPool { get; set; }
        public Producer LogProducer { get; set; }

        public LogAccumulator(Config config, IO_Worker ioWorker, IO_ThreadPool threadPool,
            Producer producer)
        {
            RwLock = new ReaderWriterLockSlim();
            LogGroupData = new Dictionary<string, Batch>();
            ProducerConfig = config;
            IoWorker = ioWorker;
            _shutDownFlag = false;
            IoThreadPool = threadPool;
            LogProducer = producer;
        }


        public Error AddLogToProducerBatch(string groupId, string streamId, Log logData, CallBack callBack)
        {
            if (ShutDownFlag)
            {
                return new Error
                    { Message = "Producer has started and shut down and cannot write to new logs", ErrorCode = "1234" };
            }

            var key = GetKeyString(groupId, streamId);
            try
            {
                RwLock.EnterWriteLock();
                if (LogGroupData.ContainsKey(key))
                {
                    var batch = LogGroupData[key];
                    long logSize = Common.GetLogSizeCalculate(logData);
                    batch.TotalDataSize += logSize;
                    LogProducer.ProducerLogGroupSize += logSize;
                    AddOrSendProducerBatch(key, groupId, streamId, batch, logData, callBack);
                }
                else
                {
                    CreateNewProducerBatch(logData, callBack, key, groupId, streamId);
                }
            }
            finally
            {
                RwLock.ExitWriteLock();
            }

            return null;
        }

        public Error AddLogToProducerBatch(string groupId, string streamId, List<Log> logList, CallBack callBack)
        {
            if (ShutDownFlag)
            {
                return new Error
                    { Message = "Producer has started and shut down and cannot write to new logs", ErrorCode = "1234" };
            }

            var key = GetKeyString(groupId, streamId);
            try
            {
                RwLock.EnterWriteLock();
                if (LogGroupData.ContainsKey(key))
                {
                    var batch = LogGroupData[key];
                    long logListSize = Common.GetLogListSize(logList);
                    batch.TotalDataSize += logListSize;
                    LogProducer.ProducerLogGroupSize += logListSize;
                    AddOrSendProducerBatch(key, groupId, streamId, batch, logList, callBack);
                }
                else
                {
                    CreateNewProducerBatch(logList, callBack, key, groupId, streamId);
                }
            }
            finally
            {
                RwLock.ExitWriteLock();
            }

            return null;
        }

        private string GetKeyString(string groupId, string streamId)
        {
            var key = new StringBuilder();
            key.Append(groupId);
            key.Append(Common.Delimiter);
            key.Append(streamId);
            key.Append(Common.Delimiter);
            return key.ToString();
        }

        private void AddOrSendProducerBatch(string key, string groupId, string streamId, Batch producerBatch, Log log,
            CallBack callBack)
        {
            var totalDataCount = producerBatch.LogGroupCount + 1;
            // batch中日志大小超过MaxBatchSize && batch中日志大小< 5242880 && batch中日志条数 <= MaxBatchCount
            // 直接发送
            if (producerBatch.TotalDataSize > this.ProducerConfig.MaxBatchSize && producerBatch.TotalDataSize < 5242880
                                                                               && totalDataCount <=
                                                                               this.ProducerConfig.MaxBatchCount)
            {
                producerBatch.AddLogToLogGroup(log);
                if (callBack != null)
                {
                    producerBatch.AddProducerBatchCallBack(callBack);
                }

                InnerSendToServer(key, producerBatch);
            }
            // batch中日志大小和条数都没有满足 配置，则log加入batch中先缓存
            else if (producerBatch.TotalDataSize <= this.ProducerConfig.MaxBatchSize &&
                     totalDataCount <= this.ProducerConfig.MaxBatchCount)
            {
                producerBatch.AddLogToLogGroup(log);
                if (callBack != null)
                {
                    producerBatch.AddProducerBatchCallBack(callBack);
                }
            }
            else
            {
                InnerSendToServer(key, producerBatch);
                CreateNewProducerBatch(log, callBack, key, groupId, streamId);
            }
        }

        private void AddOrSendProducerBatch(string key, string groupId, string streamId, Batch producerBatch,
            List<Log> logList, CallBack callBack)
        {
            var totalDataCount = producerBatch.LogGroupCount + 1;
            if (producerBatch.TotalDataSize > this.ProducerConfig.MaxBatchSize && producerBatch.TotalDataSize < 5242880
                                                                               && totalDataCount <=
                                                                               this.ProducerConfig.MaxBatchCount)
            {
                producerBatch.AddLogToLogGroup(logList);
                if (callBack != null)
                {
                    producerBatch.AddProducerBatchCallBack(callBack);
                }

                InnerSendToServer(key, producerBatch);
            }
            else if (producerBatch.TotalDataSize <= this.ProducerConfig.MaxBatchSize &&
                     totalDataCount <= this.ProducerConfig.MaxBatchCount)
            {
                producerBatch.AddLogToLogGroup(logList);
                if (callBack != null)
                {
                    producerBatch.AddProducerBatchCallBack(callBack);
                }
            }
            else
            {
                InnerSendToServer(key, producerBatch);
                CreateNewProducerBatch(logList, callBack, key, groupId, streamId);
            }
        }

        private void InnerSendToServer(string key, Batch producerBatch)
        {
            this.IoThreadPool.AddTask(producerBatch);
            LogGroupData.Remove(key);
        }

        private void CreateNewProducerBatch(Log log, CallBack callBack, string key, string groupId, string streamId)
        {
            var newBatch = Batch.InitProducerBatch(log, callBack, groupId, streamId, ProducerConfig);
            LogGroupData[key] = newBatch;
        }

        private void CreateNewProducerBatch(List<Log> logList, CallBack callBack, string key, string groupId,
            string streamId)
        {
            var newBatch = Batch.InitProducerBatch(logList, callBack, groupId, streamId, ProducerConfig);
            LogGroupData[key] = newBatch;
        }
    }
}