/*
 Copyright 2024 Huawei Technologies Co.,Ltd.

 Licensed to the Apache Software Foundation (ASF) under one
 or more contributor license agreements.  See the NOTICE file
 distributed with this work for additional information
 regarding copyright ownership.  The ASF licenses this file
 to you under the Apache LICENSE, Version 2.0 (the
 "LICENSE"); you may not use this file except in compliance
 with the LICENSE.  You may obtain a copy of the LICENSE at

     http://www.apache.org/licenses/LICENSE-2.0

 Unless required by applicable law or agreed to in writing,
 software distributed under the LICENSE is distributed on an
 "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 KIND, either express or implied.  See the LICENSE for the
 specific language governing permissions and limitations
 under the LICENSE.
*/

using HuaweiCloud.SDK.LTS.HTTP;
using System.Collections.Generic;
using System.Threading;

namespace HuaweiCloud.SDK.LTS.Model
{
    public class Batch
    {
        private ReaderWriterLockSlim _rwLock;
        private string _groupId;

        private string _streamId;

        // Log的分组，一个batch对应一个logGroup，一个logGroup中的logs，groupID和streamId相同
        private LogGroup _logGroup;

        // loggroup中logs的条数
        private int _logGroupCount;

        // logGroup中所有数据的字节大小
        private long _totalDataSize;
        private int _attemptCount;
        private long _baseRetryBackoffMs;
        private long _nextRetryMs;
        private long _maxRetryIntervalInMs;
        private List<CallBack> _callBackList;
        private long _createTimeMs;
        private int _maxRetryTimes;
        private Result _result;
        private int _maxReservedAttempts;

        public long MaxRetryIntervalInMs
        {
            get => _maxRetryIntervalInMs;
            set => _maxRetryIntervalInMs = value;
        }

        public long TotalDataSize
        {
            get => _totalDataSize;
            set => _totalDataSize = value;
        }

        public ReaderWriterLockSlim RwLock
        {
            get => _rwLock;
            set => _rwLock = value;
        }

        public LogGroup LogGroup
        {
            get => _logGroup;
            set => _logGroup = value;
        }

        public int LogGroupCount
        {
            get
            {
                try
                {
                    _rwLock.EnterReadLock();
                    return _logGroup.Logs == null ? 0 : _logGroup.Logs.Count;
                }
                finally
                {
                    _rwLock.ExitReadLock();
                }
            }
        }

        public int AttemptCount
        {
            get => _attemptCount;
            set => _attemptCount = value;
        }

        public long BaseRetryBackoffMs
        {
            get => _baseRetryBackoffMs;
            set => _baseRetryBackoffMs = value;
        }

        public long NextRetryMs
        {
            get => _nextRetryMs;
            set => _nextRetryMs = value;
        }

        public List<CallBack> CallBackList
        {
            get => _callBackList;
            set => _callBackList = value;
        }

        public long CreateTimeMs
        {
            get => _createTimeMs;
            set => _createTimeMs = value;
        }

        public int MaxRetryTimes
        {
            get => _maxRetryTimes;
            set => _maxRetryTimes = value;
        }

        public string GroupId
        {
            get
            {
                try
                {
                    _rwLock.EnterReadLock();
                    return _groupId;
                }
                finally
                {
                    _rwLock.ExitReadLock();
                }
            }
            set => _groupId = value;
        }

        public string StreamId
        {
            get
            {
                try
                {
                    _rwLock.EnterReadLock();
                    return _streamId;
                }
                finally
                {
                    _rwLock.ExitReadLock();
                }
            }
            set => _streamId = value;
        }

        public Result Result
        {
            get => _result;
            set => _result = value;
        }

        public int MaxReservedAttempts
        {
            get => _maxReservedAttempts;
            set => _maxReservedAttempts = value;
        }

        public void AddLogToLogGroup(Log log)
        {
            try
            {
                _rwLock.EnterWriteLock();
                _logGroup.Logs.Add(log);
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
        }

        public void AddLogToLogGroup(List<Log> logs)
        {
            try
            {
                _rwLock.EnterWriteLock();
                _logGroup.Logs.AddRange(logs);
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
        }

        public void AddProducerBatchCallBack(CallBack callBack)
        {
            try
            {
                _rwLock.EnterWriteLock();
                _callBackList.Add(callBack);
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
        }
        
        public static Batch InitProducerBatch(Log logData, CallBack callBack, string groupId, string streamId,
            Config config)
        {
            var logs = new List<Log> { logData };
            var logGroup = new LogGroup { Logs = logs };
            var currentTime = Common.GetTimeMs();

            return ProducerBatch(callBack, groupId, streamId, config, logGroup, currentTime);
        }

        public static Batch InitProducerBatch(List<Log> logList, CallBack callBack, string groupId, string streamId,
            Config config)
        {
            var logs = new List<Log>();
            logs.AddRange(logList);
            var logGroup = new LogGroup { Logs = logs };
            var currentTime = Common.GetTimeMs();

            return ProducerBatch(callBack, groupId, streamId, config, logGroup, currentTime);
        }

        private static Batch ProducerBatch(CallBack callBack, string groupId, string streamId, Config config,
            LogGroup logGroup,
            long currentTime)
        {
            var producerBatch = new Batch
            {
                LogGroup = logGroup,
                AttemptCount = 0,
                _logGroupCount = 0,
                MaxRetryIntervalInMs = config.MaxRetryBackoffMs,
                CallBackList = new List<CallBack>(),
                CreateTimeMs = currentTime,
                MaxRetryTimes = config.Retries,
                BaseRetryBackoffMs = config.BaseRetryBackoffMs,
                GroupId = groupId,
                StreamId = streamId,
                Result = new Result(),
                MaxReservedAttempts = config.MaxReservedAttempts,
                RwLock = new ReaderWriterLockSlim()
            };
            producerBatch.TotalDataSize = producerBatch.LogGroup.Size();
            if (callBack != null)
            {
                producerBatch.CallBackList.Add(callBack);
            }

            return producerBatch;
        }
    }
}