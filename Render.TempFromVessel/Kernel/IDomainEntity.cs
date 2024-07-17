using System;

namespace Render.TempFromVessel.Kernel
{
    public interface IDomainEntity
    {
        Guid Id { get; }
    }
}