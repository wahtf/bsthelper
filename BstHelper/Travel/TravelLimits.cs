using System;

namespace BstHelper.Travel;

public static class TravelLimits
{
    public const int MaxTeleportAttempts = 3;
    public const int MaxMountAttempts = 3;
    public const int MaxTakeoffAttempts = 20;
    public const int MaxDismountAttempts = 12;

    public static readonly TimeSpan TeleportRetryInterval = TimeSpan.FromSeconds(8);
    public static readonly TimeSpan MountRetryInterval = TimeSpan.FromSeconds(1.5);
    public static readonly TimeSpan TakeoffRetryInterval = TimeSpan.FromSeconds(0.3);
}
