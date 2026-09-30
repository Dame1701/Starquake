// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Structure;

namespace Starquake.ContentManagement;

public class SoundDescription
{
    public readonly string FilePath;
    public readonly SoundEffectInstanceType InstanceType;
    public readonly bool KillOnScreenChange;
    public readonly string ResourceName;
    public int MaxInstances;

    public SoundDescription(string resourceName, string filePath, SoundEffectInstanceType instanceType, int maxInstances, bool killOnScreenChange)
    {
        ResourceName = resourceName;
        FilePath = filePath;
        InstanceType = instanceType;
        MaxInstances = maxInstances;
        KillOnScreenChange = killOnScreenChange;
    }
}
