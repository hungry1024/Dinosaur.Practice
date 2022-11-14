using System;
using System.Diagnostics;
using System.Threading;

namespace Dinosaur.Distributed.IdGenerators
{
    internal class DinosaurSnowflakeGenerator : ISnowflakeIdGenerator
    {
        private readonly long _timestampStart;
        private readonly int _timestampBitLength;

        private readonly long _nodeId;
        private readonly long _nodeIdLAndBase;

        private readonly int _sequenceBitLength;
        private readonly long _sequenceLAndBase;

        private readonly Stopwatch _watch;

        private long _sequence = 0;
        private long _lastTime;

        public DinosaurSnowflakeGenerator(DateTime startTime, int nodeId = 0, int timestampBitLength = 41, int nodeIdBitLength = 10)
        {
            _timestampStart = (DateTime.UtcNow.Ticks - startTime.Ticks) / 10000L;

            _timestampBitLength = timestampBitLength;

            _sequenceBitLength = 63 - timestampBitLength - nodeIdBitLength;

            _nodeIdLAndBase = (long)Math.Pow(2, nodeIdBitLength) - 1L;

            _sequenceLAndBase = (long)Math.Pow(2, 63 - timestampBitLength - nodeIdBitLength) - 1L;

            _nodeId = nodeId & _nodeIdLAndBase;

            _watch = Stopwatch.StartNew();
        }

        public long GetIdOnlyTime(DateTime time)
        {
            var t = time.Ticks / 10000L - _timestampStart;

            return t << (63 - _timestampBitLength);
        }

        private static readonly object syncObj = new object();

        public long NewId()
        {
            lock (syncObj)
            {
                var ms = _watch.ElapsedMilliseconds + _timestampStart;

                if (ms < _lastTime) ms = _lastTime;

                if (_lastTime == ms)
                {
                    if (++_sequence > _sequenceLAndBase)
                    {
                        _sequence = 0;
                        SpinWait.SpinUntil(() =>
                        {
                            ms = _watch.ElapsedMilliseconds + _timestampStart;
                            return _lastTime != ms;
                        });
                        _lastTime = ms;
                    }
                }
                else
                {
                    _sequence= 0;
                    _lastTime = ms;
                }            

                return (ms << (63 - _timestampBitLength)) | (_nodeId << _sequenceBitLength) | _sequence;
            }
        }

        public long NewId(DateTime time)
        {
            long ms = time.Ticks / 10000L - _timestampStart;

            var seq = Interlocked.Increment(ref _sequence) & _sequenceLAndBase;

            return (ms << (63 - _timestampBitLength)) | (_nodeId << _sequenceBitLength) | seq;
        }

        public (DateTime time, int nodeId, int sequence) Parse(long id)
        {
            var time = new DateTime((_timestampStart + (id >> (63 - _timestampBitLength))) * 10000L);

            var nodeId = (int)((id >> _sequenceBitLength) & _nodeIdLAndBase);

            var sequence = (int)(id & _sequenceLAndBase);

            return (time, nodeId, sequence);
        }
    }
}
