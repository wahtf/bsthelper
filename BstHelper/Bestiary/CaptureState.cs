using FFXIVClientStructs.FFXIV.Client.Game;

namespace BstHelper.Bestiary;

public static unsafe class CaptureState
{
    public static bool Ready
    {
        get
        {
            var manager = XBMManager.Instance();
            return manager != null && manager->State == XBMManager.DataState.Received;
        }
    }

    public static bool IsCaptured(uint number)
    {
        var manager = XBMManager.Instance();
        return manager != null && manager->IsPetUnlocked(number);
    }

    public static int CapturedCount
    {
        get
        {
            var manager = XBMManager.Instance();
            return manager == null ? 0 : manager->NumUnlockedPets;
        }
    }

    public static int PlayerLevel => Plugin.PlayerState.IsLoaded ? Plugin.PlayerState.Level : 0;

    public static bool IsCatchableNow(Beast beast)
        => Ready && !IsCaptured(beast.Number) && PlayerLevel > 0 && beast.Level <= PlayerLevel;
}
