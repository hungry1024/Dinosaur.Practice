using Dinosaur;
using Dinosaur.Distributed;
using Dinosaur.Distributed.IdGenerators;
using Dinosaur.Distributed.Infrastructure;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DisnosaurDistributedServiceCollectionExtensions
    {
        public static IServiceCollection AddDisnosaurDistributed(this IServiceCollection services, Action<DistributedOptions> action)
        {
            if(action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            var opts = new DistributedOptions();

            action(opts);

            if(opts.NodeId > Math.Pow(2, opts.SnowflakeIdOptions.NodeIdBitLength) - 1)
            {
                throw new InvalidOperationException($"NodeIdBitLength={opts.SnowflakeIdOptions.NodeIdBitLength}允许最大的NodeId是{Math.Pow(2, opts.SnowflakeIdOptions.NodeIdBitLength) - 1}");
            }

            if (!string.IsNullOrEmpty(opts.RedisConnectionString))
            {
                RedisProxy.Initialize(opts.RedisConnectionString);

                services.AddTransient<ISerialIdGenerator, RedisSerialIdGenerator>();
            }

            services.AddSingleton<ISnowflakeIdGenerator>(new DinosaurSnowflakeGenerator(
               startTime: opts.SnowflakeIdOptions.StartTime, 
               nodeId: opts.NodeId, 
               timestampBitLength: opts.SnowflakeIdOptions.TimestampBitLength, 
               nodeIdBitLength: opts.SnowflakeIdOptions.NodeIdBitLength));

            var nid = (byte)(opts.NodeId & byte.MaxValue);

            services.AddTransient<IGuidGenerator>(sp => new SequentialGuidGenerator(nid, opts.GuidType));

            return services;
        }
    }
}
