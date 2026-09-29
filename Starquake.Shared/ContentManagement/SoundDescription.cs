// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Structure;

namespace Starquake.ContentManagement;

public class SoundDescription
{
    public string FilePath;
    public SoundEffectInstanceType InstanceType;
    public bool KillOnScreenChange;
    public int MaxInstances;
    public string ResourceName;

    public SoundDescription(string resourceName, string filePath, SoundEffectInstanceType instanceType, int maxInstances, bool killOnScreenChange)
    {
        ResourceName = resourceName;
        FilePath = filePath;
        InstanceType = instanceType;
        MaxInstances = maxInstances;
        KillOnScreenChange = killOnScreenChange;
    }
}