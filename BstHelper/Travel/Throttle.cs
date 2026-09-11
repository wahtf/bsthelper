using System;
using System.Collections.Generic;

namespace BstHelper.Travel;

internal static class Throttle
{
    private static readonly Dictionary<string, DateTime> Next = new();

    public static bool Ready(string key, int milliseconds)
    {
        var now = DateTime.Now;
        if (Next.TryGetValue(key, out var at) && now < at)
            return false;

        Next[key] = now.AddMilliseconds(milliseconds);
        return true;
    }
}
