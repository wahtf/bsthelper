using System;

namespace BstHelper.Travel.Ipc;

internal static class Gate
{
    public static TReturn Call<TReturn>(Func<TReturn> call, TReturn fallback, string name)
    {
        try
        {
            return call();
        }
        catch (Exception e)
        {
            Plugin.Log.Verbose($"[Ipc] {name} failed: {e.Message}");
            return fallback;
        }
    }

    public static void Call(Action call, string name)
    {
        try
        {
            call();
        }
        catch (Exception e)
        {
            Plugin.Log.Verbose($"[Ipc] {name} failed: {e.Message}");
        }
    }
}
