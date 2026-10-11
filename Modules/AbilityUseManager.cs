using System;
using BHR.Modules.Rpc;

namespace BHR.Modules;

public static class AbilityUseManager
{
    private static readonly Dictionary<byte, float> AbilityUseLimit = [];

    public static void Initialize()
    {
        AbilityUseLimit.Clear();
    }

    public static float GetAbilityUseLimit(this PlayerControl pc) => GetAbilityUseLimit((IPlayerControl)(ModdedPlayerControl)pc);
    public static float GetAbilityUseLimit(this IPlayerControl pc) => AbilityUseLimit.GetValueOrDefault(pc.PlayerId, float.NaN);
    public static float GetAbilityUseLimit(this byte playerId) => AbilityUseLimit.GetValueOrDefault(playerId, float.NaN);

    public static void RpcRemoveAbilityUse(this PlayerControl pc, bool log = true) => RpcRemoveAbilityUse((IPlayerControl)(ModdedPlayerControl)pc, log);
    public static void RpcRemoveAbilityUse(this IPlayerControl pc, bool log = true, bool rpc = true)
    {
        float current = pc.GetAbilityUseLimit();
        if (float.IsNaN(current) || current <= 0f) return;
        pc.SetAbilityUseLimit(current - 1, rpc, log);
    }

    public static void RpcIncreaseAbilityUseLimitBy(this PlayerControl pc, float get, bool log = true) => RpcIncreaseAbilityUseLimitBy((IPlayerControl)(ModdedPlayerControl)pc, get, log);
    public static void RpcIncreaseAbilityUseLimitBy(this IPlayerControl pc, float get, bool log = true, bool rpc = true)
    {
        float current = pc.GetAbilityUseLimit();
        if (float.IsNaN(current)) return;
        pc.SetAbilityUseLimit(current + get, rpc, log);
    }


    public static void SetAbilityUseLimit(this PlayerControl pc, float limit, bool rpc = true, bool log = true) => SetAbilityUseLimit((IPlayerControl)(ModdedPlayerControl)pc, limit, rpc, log);
    public static void SetAbilityUseLimit(this IPlayerControl pc, float limit, bool rpc = true, bool log = true) => pc.PlayerId.SetAbilityUseLimit(limit, rpc, log);

    public static void SetAbilityUseLimit(this byte playerId, float limit, bool rpc = true, bool log = true)
    {
        limit = (float)Math.Round(limit, 1);

        if (float.IsNaN(limit) || limit is < 0f or > 1000f || (AbilityUseLimit.TryGetValue(playerId, out var beforeLimit) && Math.Abs(beforeLimit - limit) < 0.01f)) return;

        AbilityUseLimit[playerId] = limit;
        if (!rpc && !log) return;

        ApplyGameSideEffects(playerId, limit, rpc, log);
    }

    private static void ApplyGameSideEffects(byte playerId, float limit, bool rpc, bool log)
    {
        var player = playerId.GetPlayer();
        if (AmongUsClient.Instance.AmHost && player.IsNonHostModdedClient() && rpc)
        {
            LegacyRpcSenders.SendSyncAbilityUseLimit(ModdedPlayerControl.LocalPlayer.NetId, playerId, limit);
        }

        Utils.NotifyRoles(SpecifySeer: player, ForceLoop: false);
        if (log) Logger.Info($" {player.GetNameWithRole()} => {limit}", "SetAbilityUseLimit");
    }

    public static bool CanAbilityLimitBeManip(this PlayerControl pc) => CanAbilityLimitBeManip((IPlayerControl)(ModdedPlayerControl)pc);
    public static bool CanAbilityLimitBeManip(this IPlayerControl pc)
    {
        return pc.GetCustomRole() switch
        {
            CustomRoles.Lich or 
            CustomRoles.SoulCollector or 
            CustomRoles.Benefactor or 
            CustomRoles.Berserker or 
            CustomRoles.Keeper or 
            CustomRoles.Collector or 
            CustomRoles.Doomsayer or 
            CustomRoles.Maverick or 
            CustomRoles.Pirate or 
            CustomRoles.Pixie or 
            CustomRoles.PunchingBag or 
            CustomRoles.Seeker or 
            CustomRoles.Taskinator or 
            CustomRoles.Vector or 
            CustomRoles.Vulture
                => false,

            _ => true
        };
    }
}
