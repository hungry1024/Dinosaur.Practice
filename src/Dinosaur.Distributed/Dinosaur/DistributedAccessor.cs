using Dinosaur.Distributed;
using Dinosaur.Distributed.IdGenerators;
using Dinosaur.Distributed.Infrastructure;
using System;

namespace Dinosaur
{
    public class DistributedAccessor
    {
        private readonly DistributedOptions options;
        private byte nodeId4Guid;

        private DistributedAccessor()
        {
            options = new DistributedOptions();
        }

        private static readonly DistributedAccessor instance = new DistributedAccessor();
        private static bool hasConfigured = false;
        private static readonly object lockobj = new object();

        private static ISnowflakeIdGenerator snowflakeIdGenerator = null;

        public static DistributedAccessor Configure(Action<DistributedOptions> action)
        {
            if (hasConfigured)
            {
                return instance;
            }

            lock (lockobj)
            {
                if (!hasConfigured)
                {
                    if (action == null)
                    {
                        throw new ArgumentNullException(nameof(action));
                    }

                    action(instance.options);

                    if (instance.options.NodeId > Math.Pow(2, instance.options.SnowflakeIdOptions.NodeIdBitLength) - 1)
                    {
                        throw new InvalidOperationException($"NodeIdBitLength={instance.options.SnowflakeIdOptions.NodeIdBitLength}允许最大的NodeId是{Math.Pow(2, instance.options.SnowflakeIdOptions.NodeIdBitLength) - 1}");
                    }

                    if (!string.IsNullOrEmpty(instance.options.RedisConnectionString))
                    {
                        RedisProxy.Initialize(instance.options.RedisConnectionString);
                    }

                    instance.nodeId4Guid = (byte)(instance.options.NodeId & byte.MaxValue);

                    snowflakeIdGenerator = new DinosaurSnowflakeGenerator(
                       startTime: instance.options.SnowflakeIdOptions.StartTime,
                       nodeId: instance.options.NodeId,
                       timestampBitLength: instance.options.SnowflakeIdOptions.TimestampBitLength,
                       nodeIdBitLength: instance.options.SnowflakeIdOptions.NodeIdBitLength);

                    hasConfigured = true;
                }
            }

            return instance;
        }

        public IGuidGenerator GetGuidGenerator()
        {
            return new SequentialGuidGenerator(nodeId4Guid, options.GuidType);
        }

        public ISerialIdGenerator GetSerialIdGenerator()
        {
            return new RedisSerialIdGenerator();
        }

        public ISnowflakeIdGenerator GetSnowflakeIdGenerator()
        {
            return snowflakeIdGenerator;
        }

    }
}
