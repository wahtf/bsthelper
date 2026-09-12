using Dalamud.Game.ClientState.Conditions;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace BstHelper.Travel;

public static unsafe class PlayerActions
{
    private const uint MountRouletteAction = 9;

    private const uint JumpAction = 2;

    private const uint DismountAction = 23;

    private const uint SprintAction = 4;

    public static bool Sprint()
    {
        var actionManager = ActionManager.Instance();
        if (actionManager == null || actionManager->GetActionStatus(ActionType.GeneralAction, SprintAction) != 0)
            return false;

        return actionManager->UseAction(ActionType.GeneralAction, SprintAction);
    }

    public static bool Mount(uint preferredMountId)
    {
        var actionManager = ActionManager.Instance();
        if (actionManager == null)
            return false;

        var playerState = PlayerState.Instance();
        if (preferredMountId != 0 && playerState != null && playerState->IsMountUnlocked(preferredMountId) &&
            actionManager->GetActionStatus(ActionType.Mount, preferredMountId) == 0)
        {
            return actionManager->UseAction(ActionType.Mount, preferredMountId);
        }

        if (actionManager->GetActionStatus(ActionType.GeneralAction, MountRouletteAction) == 0)
            return actionManager->UseAction(ActionType.GeneralAction, MountRouletteAction);

        return false;
    }

    public static bool TakeOff()
    {
        var actionManager = ActionManager.Instance();
        return actionManager != null && actionManager->UseAction(ActionType.GeneralAction, JumpAction);
    }

    public static bool Swimming => Plugin.Condition[ConditionFlag.Swimming];

    public static bool Diving => Plugin.Condition[ConditionFlag.Diving];

    public static bool InWater
    {
        get
        {
            if (Swimming || Diving)
                return true;

            if (Plugin.ObjectTable[0] is not { } me)
                return false;

            var chara = (Character*)me.Address;
            return chara != null && chara->MoveController.IsSwimming;
        }
    }

    public static bool Dismount()
    {
        var actionManager = ActionManager.Instance();
        if (actionManager == null || actionManager->GetActionStatus(ActionType.GeneralAction, DismountAction) != 0)
            return false;

        return actionManager->UseAction(ActionType.GeneralAction, DismountAction);
    }

    public static bool IsMounted
        => Plugin.Condition[ConditionFlag.Mounted] || Plugin.Condition[ConditionFlag.InFlight];

    public static bool TrySprint(float distanceLeft, string throttleKey, float floor = 40f)
    {
        if (distanceLeft < floor || Plugin.Condition[ConditionFlag.Mounted] || Plugin.Condition[ConditionFlag.InFlight])
            return false;

        if (!Throttle.Ready(throttleKey, 2000))
            return false;

        return Sprint();
    }

    public static bool InWorld
        => Plugin.ClientState.IsLoggedIn && Plugin.ObjectTable.LocalPlayer != null;

    public static bool Zoning
        => Plugin.Condition[ConditionFlag.BetweenAreas] || Plugin.Condition[ConditionFlag.BetweenAreas51];

    public static bool InCutscene
        => Plugin.Condition[ConditionFlag.OccupiedInCutSceneEvent] ||
           Plugin.Condition[ConditionFlag.WatchingCutscene] ||
           Plugin.Condition[ConditionFlag.WatchingCutscene78];

    public static bool InEvent
        => Plugin.Condition[ConditionFlag.OccupiedInCutSceneEvent] ||
           Plugin.Condition[ConditionFlag.WatchingCutscene78] ||
           Plugin.Condition[ConditionFlag.OccupiedInEvent] ||
           Plugin.Condition[ConditionFlag.Occupied];

    public static bool IsBusyWithAnimation()
        => Plugin.Condition[ConditionFlag.Mounting] || Plugin.Condition[ConditionFlag.Mounting71] ||
           Plugin.Condition[ConditionFlag.Jumping] || Plugin.Condition[ConditionFlag.Jumping61] ||
           Plugin.Condition[ConditionFlag.BetweenAreas] || Plugin.Condition[ConditionFlag.Casting];
}
