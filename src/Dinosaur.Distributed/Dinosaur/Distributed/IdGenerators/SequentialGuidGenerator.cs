using System;
using System.Security.Cryptography;

namespace Dinosaur.Distributed.IdGenerators
{
    internal class SequentialGuidGenerator : IGuidGenerator
    {
        private static readonly RandomNumberGenerator RandomNumberGenerator = RandomNumberGenerator.Create();
        private const long BaseTicks = 621355968000000000L;

        private readonly byte _nodeId;
        private readonly SequentialGuidType _sequentialGuidType;

        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="nodeId">节点Id</param>
        /// <param name="sequentialGuidType">排序类型</param>
        public SequentialGuidGenerator(byte nodeId, SequentialGuidType sequentialGuidType = SequentialGuidType.AsString)
        {
            _nodeId = nodeId;
            _sequentialGuidType = sequentialGuidType;
        }

        public Guid NewGuid() => Generate(_sequentialGuidType);

        public Guid NewGuid(SequentialGuidType guidType) => Generate(guidType);

        private Guid Generate(SequentialGuidType guidType)
        {
            var randomBytes = new byte[8];
            RandomNumberGenerator.GetBytes(randomBytes);
            long timestamp = DateTime.UtcNow.Ticks - BaseTicks;
            byte[] timestampBytes = BitConverter.GetBytes(timestamp);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(timestampBytes);
            }

            byte[] guidBytes = new byte[16];

            switch (guidType)
            {
                case SequentialGuidType.AsString:
                    Buffer.BlockCopy(timestampBytes, 1, guidBytes, 0, 7);
                    guidBytes[7] = _nodeId;
                    Buffer.BlockCopy(randomBytes, 0, guidBytes, 8, 8);
                    if (BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(guidBytes, 0, 4);
                        Array.Reverse(guidBytes, 4, 2);
                        Array.Reverse(guidBytes, 6, 2);
                    }
                    break;
                case SequentialGuidType.AtEnd:
                    Buffer.BlockCopy(randomBytes, 0, guidBytes, 0, 8);
                    guidBytes[8] = timestampBytes[7];
                    guidBytes[9] = _nodeId;
                    Buffer.BlockCopy(timestampBytes, 1, guidBytes, 10, 6);
                    break;
            }

            return new Guid(guidBytes);
        }
    }
}
