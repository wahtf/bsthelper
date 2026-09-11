using System;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Conditions;
using BstHelper.Travel;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace BstHelper.Bestiary;

public static unsafe class CaptureSync
{
    private static readonly TimeSpan Settle = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan Retry = TimeSpan.FromSeconds(20);
    private const int Asks = 3;
    private const string Addon = "XBMMonsterNotebook";

    private static ulong character;
    private static DateTime nextAsk;
    private static int asked;
    private static XBMManager.DataState seen;
    private static bool held;
    private static bool gaveUp;

    public static void Init()
    {
        Plugin.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, Addon, OnAddon);
        Plugin.AddonLifecycle.RegisterListener(AddonEvent.PostRefresh, Addon, OnAddon);
        Plugin.AddonLifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, Addon, OnAddon);
        Plugin.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, Addon, OnAddon);
    }

    public static void Dispose()
    {
        Plugin.AddonLifecycle.UnregisterListener(OnAddon);
    }

    private static void OnAddon(AddonEvent type, AddonArgs args)
    {
        var state = CaptureState.State;
        if (type == AddonEvent.PostRefresh || type == AddonEvent.PostRequestedUpdate)
            Plugin.Log.Debug($"[Bestiary] {Addon} {type}, state={state}, {CaptureState.CapturedCount}/{BeastTable.Total}");
        else
            Plugin.Log.Information($"[Bestiary] {Addon} {type}, state={state}");
    }

    public static void Update()
    {
        var who = Plugin.PlayerState.IsLoaded ? Plugin.PlayerState.ContentId : 0;
        if (who == 0)
        {
            Forget();
            return;
        }

        if (who != character)
            Adopt(who);

        Watch();

        if (CaptureState.Ready)
            return;

        if (asked >= Asks)
        {
            GiveUp();
            return;
        }

        var busy = Busy;
        Hold(busy);
        if (busy || DateTime.UtcNow < nextAsk)
            return;

        nextAsk = DateTime.UtcNow + Retry;
        if (Ask())
            asked++;
    }

    private static void Forget()
    {
        character = 0;
        asked = 0;
        seen = XBMManager.DataState.None;
        held = false;
        gaveUp = false;
    }

    private static void Adopt(ulong who)
    {
        character = who;
        asked = 0;
        nextAsk = DateTime.UtcNow + Settle;
        seen = CaptureState.State;
        held = false;
        gaveUp = false;
    }

    private static void Watch()
    {
        var state = CaptureState.State;
        if (state == seen)
            return;

        Plugin.Log.Information(state == XBMManager.DataState.Received
            ? $"[Bestiary] state {seen} -> {state}, {CaptureState.CapturedCount}/{BeastTable.Total}"
            : $"[Bestiary] state {seen} -> {state}");
        seen = state;
    }

    private static bool Ask()
    {
        var module = AgentModule.Instance();
        if (module == null)
        {
            Plugin.Log.Warning("[Bestiary] request skipped, no agent module");
            return false;
        }

        var agent = module->GetAgentByInternalId(AgentId.XBMMonsterNotebook);
        if (agent == null)
        {
            Plugin.Log.Warning($"[Bestiary] request skipped, agent {(uint)AgentId.XBMMonsterNotebook} missing");
            return false;
        }

        if (agent->IsAgentActive())
        {
            Plugin.Log.Debug("[Bestiary] request skipped, bestiary already open");
            return false;
        }

        Plugin.Log.Information($"[Bestiary] requesting list ({asked + 1}/{Asks})");
        agent->Show();
        agent->Hide();
        return true;
    }

    private static void GiveUp()
    {
        if (gaveUp)
            return;

        gaveUp = true;
        Plugin.Log.Warning($"[Bestiary] gave up after {Asks} requests, state={CaptureState.State}");
    }

    private static void Hold(bool busy)
    {
        if (held == busy)
            return;

        held = busy;
        if (busy)
            Plugin.Log.Debug("[Bestiary] request deferred, busy");
    }

    private static bool Busy
        => !PlayerActions.InWorld || PlayerActions.Zoning || PlayerActions.InEvent ||
           Plugin.Condition[ConditionFlag.InCombat];
}
