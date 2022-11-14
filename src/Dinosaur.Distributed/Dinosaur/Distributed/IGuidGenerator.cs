using System;

namespace Dinosaur.Distributed
{
    public interface IGuidGenerator
    {
        Guid NewGuid();

        Guid NewGuid(SequentialGuidType guidType);
    }
}
