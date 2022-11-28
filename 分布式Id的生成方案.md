# 分布式ID的生成方案

## 基于Redis的分布式锁

场景：

- 分布式部署、并发
- 有写操作
- 有竞争关系



核心：Redis SETNX 命令（**SET** if **N**ot e**X**ists），循环等待

核心代码，需要引入Redis客户端StackExchange.Redis

````c#
/// <summary>
/// 临界区
/// </summary>
/// <param name="key">键</param>
/// <param name="action">临界区的执行方法</param>
/// <param name="lockTimeout">独占超时</param>
/// <param name="getlockWaitTimeout">等待超时</param>
/// <exception cref="ArgumentNullException">key为空抛出异常</exception>
public static void Lock(string key, Action action, TimeSpan lockTimeout, TimeSpan getlockWaitTimeout)
{
    if (string.IsNullOrEmpty(key))
    {
        throw new ArgumentNullException(nameof(key));
    }

    RedisValue value = Environment.MachineName;

    RetryUntilTrue(() => redisDatabase.LockTake(KeyPrefix + key, value, lockTimeout), getlockWaitTimeout);
    try
    {
        action();
    }
    finally
    {
        redisDatabase.LockRelease(KeyPrefix + key, value);
    }
}

private static void RetryUntilTrue(Func<bool> action, TimeSpan timeOut)
{
    int num = 0;
    DateTime entryTime = DateTime.Now;
    var random = new Random();
    while (DateTime.Now - entryTime < timeOut)
    {
        num++;
        if (action())
        {
            return;
        }

        var milliseconds = random.Next((int)Math.Pow(n, 2), (int)Math.Pow(n + 1, 2) + 1);
        Thread.Sleep(milliseconds);
    }

    throw new TimeoutException($"Exceeded timeout of {timeOut}");
}
````



## 有序Guid

GUID 是一个 128 位整数， (16 字节) ，可在需要唯一标识符的所有计算机和网络中使用。 此类标识符的复制概率非常低。

7个字节时间戳，1个字节节点Id（取值范围0~0xFF），8个字节随机数

### 普通Guid

排序规则按照字节数组顺序，与字符串形式一样从左往右。

7个字节时间戳 + 1个字节节点Id + 8个字节随机数



### uniqueidentifier

仅限于SqlServer下的uniqueidentifier类型，对应c#的SqlGuid类型，排序规则根据字节数组下标如下：

```
{10, 11, 12, 13, 14, 15, 8, 9, 6, 7, 4, 5, 0, 1, 2, 3}
```

与普通Guid类似的规则，只是根据排序规则放置位置不同

8个字节随机数 + 时间戳[6] + 1个字节节点Id + 时间戳[0~5]



注：在SqlServer下不使用uniqueidentifier类型，但是使用Guid的字符串形式，则使用普通Guid的字符串形式。



### 代码片段

````c#
public class SequentialGuid
{
    private static readonly RandomNumberGenerator RandomNumberGenerator = RandomNumberGenerator.Create();
    private const long BaseTicks = 621355968000000000L;

    private readonly byte _nodeId;
    public SequentialGuidGenerator(byte nodeId)
    {
        _nodeId = nodeId;
    } 
    
    public Guid NewGuid(SequentialGuidType guidType)
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
            case SequentialGuidType.AsEnd:
                Buffer.BlockCopy(randomBytes, 0, guidBytes, 0, 8);
                guidBytes[8] = timestampBytes[7];
                guidBytes[9] = _nodeId;
                Buffer.BlockCopy(timestampBytes, 1, guidBytes, 10, 6);
                break;
        }

        return new Guid(guidBytes);
    }
}

/// <summary>
/// 有序Guid排序类型
/// </summary>
public enum SequentialGuidType
{
    /// <summary>
    /// 作为字符串排序，用于char(36)类型
    /// </summary>
    AsString,

    /// <summary>
    /// 末尾排序，适用于mssql的uniqueidentifier类型
    /// </summary>
    AtEnd
}
````



## 基于Redis的流水号

流水号一般用于单号，按月/按日顺序生成，主要使用Redis的INCR操作，代码片段：

````c#
using StackExchange.Redis;

class SerialNoGenerator
{
    private static readonly string CacheNamespace = "serial:";
    
    private readonly IDataBase _redisDatabase;
    public SerialNoGenerator(IDataBase redisDatabase)
    {
        _redisDatabase = redisDatabase;
    }

    public bool Dialback(string key, long value, ConditionWhen when, TimeSpan? expiry = null)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentNullException(nameof(key));
        }

        switch (when)
        {
            case ConditionWhen.NotExists:
                return _redisDatabase.StringSet(CacheNamespace + key, value, expiry ?? TimeSpan.FromDays(31), When.NotExists);
            case ConditionWhen.NotEqualToOldValue:
                var oldValue = _redisDatabase.StringGet(CacheNamespace + key);
                if (oldValue.HasValue && (long)oldValue == value)
                {
                    return true;
                }
                break;
        }

        return _redisDatabase.StringSet(CacheNamespace + key, value, expiry ?? TimeSpan.FromDays(31));
    }

    public long Increment(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentNullException(nameof(key));
        }

        return _redisDatabase.StringIncrement(CacheNamespace + key);
    }

}

enum ConditionWhen
{
    Always,
    NotExists,
    NotEqualToOldValue
}
````



## 雪花Id

8个字节的长整形类型，组成格式：

1位符号（固定为0）+  41位时间戳 + 10位节点Id + 12位顺序号。



变种，有一些系统并没有这么多需要节点，适当增加时间戳位数与顺序码位数，如：

1位符号（固定为0）+ n位时间戳（41<=n<=51）+ m位节点Id（0<=m<=10） + 63-n-m位顺序号。

建议顺序号保留12位，如 1 + 43 + 8 + 12。



另外很多系统的并发并不大，这可能导致顺序号总是0，可根据实际情况整改生成0~顺序号最大值之间的随机数。



由于每次获取系统时间，需要一定的代价，同时也能避免在运行过程中出现时间回拨带来重复，本次实例代码采用起始时间戳+计数器的毫秒数。



实例代码：

````c#
class SnowflakeGenerator
{
    private readonly long _timestampStart;
    private readonly int _timestampBitLength;

    private readonly long _nodeId;

    private readonly int _sequenceBitLength;
    private readonly long _sequenceLAndBase;

    private readonly Stopwatch _watch;

    private long _sequence = 0;
    private long _lastTime;

    private static readonly object syncObj = new object();

    public DinosaurSnowflakeGenerator(DateTime startTime, int nodeId = 0, int timestampBitLength = 41, int nodeIdBitLength = 10)
    {
        _timestampStart = (DateTime.UtcNow.Ticks - startTime.Ticks) / 10000L;

        _timestampBitLength = timestampBitLength;

        _sequenceBitLength = 63 - timestampBitLength - nodeIdBitLength;

        _sequenceLAndBase = (long)Math.Pow(2, 63 - timestampBitLength - nodeIdBitLength) - 1L;

        _nodeId = nodeId & (long)Math.Pow(2, nodeIdBitLength) - 1L;

        _watch = Stopwatch.StartNew();
    }

    public long GetIdOnlyTime(DateTime time)
    {
        var t = time.Ticks / 10000L - _timestampStart;

        return t << (63 - _timestampBitLength);
    }

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
                _sequence = 0;
                _lastTime = ms;
            }

            return (ms << (63 - _timestampBitLength)) | (_nodeId << _sequenceBitLength) | _sequence;
        }
    }

}
````

