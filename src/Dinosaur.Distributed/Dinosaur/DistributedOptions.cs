using Dinosaur.Distributed;
using System;

namespace Dinosaur
{
    public class DistributedOptions
    {
        private int _nodeId;

        public string RedisConnectionString { get; set; }

        public int NodeId 
        { 
            get => _nodeId;
            set
            {
                if(value < 0)
                {
                    _nodeId = Math.Abs(value);
                }
                else if(value > 1023)
                {
                    _nodeId = value % 1023;
                }
                else
                {
                    _nodeId = value;
                }
            }
        }

        public SequentialGuidType GuidType { get; set; }

        public SnowflakeIdOptions SnowflakeIdOptions { get; set; }

        public DistributedOptions()
        {
            NodeId = 0;

            GuidType = SequentialGuidType.AsString;

            SnowflakeIdOptions = new SnowflakeIdOptions()
            {
                StartTime = new DateTime(2022, 1, 1),
                TimestampBitLength = 41,
                NodeIdBitLength = 10
            };
        }
    }
}
