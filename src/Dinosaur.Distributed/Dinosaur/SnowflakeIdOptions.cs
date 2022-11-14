using System;

namespace Dinosaur
{
    public class SnowflakeIdOptions
    {
        private int _timestampBitLength;
        private int _nodeIdBitLength;
        private DateTime _startTime;

        public DateTime StartTime 
        { 
            get => _startTime; 
            set => _startTime = value >= new DateTime(1970, 1, 1) && value <= DateTime.Now.Date ? value : new DateTime(2020, 1, 1); 
        }

        public int TimestampBitLength
        {
            get => _timestampBitLength;
            set => _timestampBitLength = value > 40 && value < 52 ? value : 41;
        }

        public int NodeIdBitLength 
        { 
            get => _nodeIdBitLength; 
            set => _nodeIdBitLength = value >= -1 && value < 11 ? value : 10; 
        }
    }
}
