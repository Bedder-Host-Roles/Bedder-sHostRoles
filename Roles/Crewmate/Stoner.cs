using AmongUs.GameOptions;
using System;
using BHR.Modules;
using static BHR.Options;
using static BHR.Translator;
using static BHR.Utils;
using UnityEngine;

namespace BHR.Roles.Crewmate;

internal class Stoner : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Stoner;
    private const int Id = 33100;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateInvestigative;
    //==================================================================\\

    private static OptionItem PlantGrowthTime;
    private static OptionItem JointUses;
    private static OptionItem JointCooldown;

    // 0 uses = plant is still growing; configured uses = plant is ready.
    private static readonly Dictionary<byte, long> PlantStartTime = [];
    private static readonly HashSet<byte> PlantReady = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Stoner);

        PlantGrowthTime = FloatOptionItem.Create(
            Id + 10,
            "StonerPlantGrowthTime",
            new(5f, 300f, 5f),
            60f,
            TabGroup.CrewmateRoles,
            false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Stoner])
            .SetValueFormat(OptionFormat.Seconds);

        JointUses = IntegerOptionItem.Create(
            Id + 11,
            "StonerJointUses",
            new(1, 10, 1),
            3,
            TabGroup.CrewmateRoles,
            false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Stoner])
            .SetValueFormat(OptionFormat.Times);

        JointCooldown = FloatOptionItem.Create(
            Id + 12,
            "StonerJointCooldown",
            new(0f, 10f, 0.5f),
            1f,
            TabGroup.CrewmateRoles,
            false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Stoner])
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void Init()
    {
        PlantStartTime.Clear();
        PlantReady.Clear();
    }

    public override void Add(byte playerId)
    {
        PlantStartTime[playerId] = Utils.TimeStamp;
        PlantReady.Remove(playerId);

        // No joint is available until the plant finishes growing. This is
        // local state on each client; the host separately owns the authoritative
        // value and synchronizes it to modded clients.
        playerId.SetAbilityUseLimit(0, rpc: false, log: false);

        // Vanilla clients cannot run BHR's HUD patches. Keep their vanilla
        // ability button hidden by desyncing them to Crewmate until the plant
        // is ready; the host switches only that client's role to Shapeshifter
        // when the Seer action becomes available.
        var stoner = Utils.GetPlayerById(playerId);
        if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost
            && stoner != null && !stoner.IsModded())
        {
            stoner.RpcSetRoleDesync(RoleTypes.Crewmate, stoner.OwnerId);
        }
    }

    public override void Remove(byte playerId)
    {
        PlantStartTime.Remove(playerId);
        PlantReady.Remove(playerId);
    }

    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime, int timerLowLoad)
    {
        if (player == null || !player.IsAlive()) return;

        // The host tracks every Stoner. A non-host client also tracks its own
        // local Stoner so its HUD can enable the button at the growth deadline;
        // it must not attempt to authoritatively update another player's state.
        bool isHost = AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost;
        if (!isHost && player.PlayerId != PlayerControl.LocalPlayer?.PlayerId) return;
        if (!GameStates.IsInTask) return;
        if (!PlantStartTime.TryGetValue(player.PlayerId, out var startTime)) return;
        if (PlantReady.Contains(player.PlayerId)) return;

        if (nowTime - startTime < PlantGrowthTime.GetInt()) return;

        PlantReady.Add(player.PlayerId);
        player.SetAbilityUseLimit(
            JointUses.GetInt(),
            rpc: isHost,
            log: isHost);

        if (isHost)
        {
            // The vanilla Stoner receives the actual target-selecting button
            // only after the plant has finished growing.
            if (!player.IsModded())
                player.RpcSetRoleDesync(RoleTypes.Shapeshifter, player.OwnerId);
            player.RpcResetAbilityCooldown();
        }

        if (player.PlayerId == PlayerControl.LocalPlayer?.PlayerId)
        {
            Utils.FlashColor(new Color(0.20f, 1f, 0.25f, 0.45f), 0.75f);
            HudManager.Instance.ShowPopUp(GetString("StonerPlantReady"));
        }
    }

    private static bool IsPlantReady(PlayerControl player)
    {
        if (player == null) return false;

        // Host tracks the authoritative state in PlantReady. Clients learn
        // that the plant is ready through the existing ability-use sync.
        return PlantReady.Contains(player.PlayerId) || player.GetAbilityUseLimit() > 0f;
    }

    private static string GetPlantGrowingMessage(PlayerControl player)
    {
        if (player == null || !PlantStartTime.TryGetValue(player.PlayerId, out var startTime))
            return string.Format(GetString("StonerPlantGrowing"), 0);

        float growthTime = MathF.Max(PlantGrowthTime.GetFloat(), 1f);
        float elapsed = Math.Clamp(Utils.TimeStamp - startTime, 0f, growthTime);
        int percent = (int)MathF.Round((elapsed / growthTime) * 100f);
        return string.Format(GetString("StonerPlantGrowing"), percent);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        // The vanilla shapeshifter target picker is used as the Seer target
        // selector. The actual transformation is rejected in OnCheckShapeshift.
        AURoleOptions.ShapeshifterCooldown = Math.Max(1f, JointCooldown.GetFloat());
        AURoleOptions.ShapeshifterDuration = 1f;
    }

    // Stoner never receives a lethal kill button. Its only active ability is
    // the target-selecting Seer button supplied by the shapeshifter base role.
    public override bool CanUseKillButton(PlayerControl player) => false;

    public override bool OnCheckShapeshift(
        PlayerControl shapeshifter,
        PlayerControl target,
        ref bool resetCooldown,
        ref bool shouldAnimate)
    {
        resetCooldown = false;
        shouldAnimate = false;

        if (shapeshifter == null || target == null || target.PlayerId == shapeshifter.PlayerId
            || !shapeshifter.IsAlive() || !target.IsAlive() || !GameStates.IsInTask)
            return false;

        if (!IsPlantReady(shapeshifter))
        {
            string growingMessage = GetPlantGrowingMessage(shapeshifter);
            if (shapeshifter.PlayerId == PlayerControl.LocalPlayer?.PlayerId)
                HudManager.Instance?.ShowPopUp(growingMessage);
            else if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost && !shapeshifter.IsModded())
                Utils.SendMessage(growingMessage, shapeshifter.PlayerId);
            return false;
        }

        if (shapeshifter.GetAbilityUseLimit() < 1f)
        {
            if (shapeshifter.PlayerId == PlayerControl.LocalPlayer?.PlayerId)
                HudManager.Instance?.ShowPopUp("You have no Seer uses remaining.");
            else if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost && !shapeshifter.IsModded())
                Utils.SendMessage("You have no Seer uses remaining.", shapeshifter.PlayerId);
            return false;
        }

        // Consume the charge on the authoritative host. Ability-use sync is
        // sent only to BHR clients by the existing AbilityUseManager.
        if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
        {
            shapeshifter.RpcRemoveAbilityUse();
            if (shapeshifter.GetAbilityUseLimit() <= 0f && !shapeshifter.IsModded())
                shapeshifter.RpcSetRoleDesync(RoleTypes.Crewmate, shapeshifter.OwnerId);
        }

        resetCooldown = true;
        bool isImpostorAffiliation = target.GetCustomRole().IsImpostorTeamV3();
        string result = isImpostorAffiliation
            ? GetString("StonerResultImpostor")
            : GetString("StonerResultSafe");

        if (shapeshifter.PlayerId == PlayerControl.LocalPlayer?.PlayerId)
        {
            var resultColor = isImpostorAffiliation
                ? new Color(1f, 0.10f, 0.10f, 0.50f)
                : new Color(0.20f, 1f, 0.25f, 0.50f);
            Utils.FlashColor(resultColor, 0.65f);
            HudManager.Instance?.ShowPopUp(result);
        }
        else if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost && !shapeshifter.IsModded())
        {
            Utils.SendMessage(result, shapeshifter.PlayerId);
        }

        // Never allow the vanilla transformation to complete.
        return false;
    }

    public override void SetKillCooldown(byte id)
    {
        Main.AllPlayerKillCooldown[id] = 300f;
    }

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        var player = Utils.GetPlayerById(playerId);
        bool canUse = player != null && player.IsAlive()
            && IsPlantReady(player) && player.GetAbilityUseLimit() > 0f;
        hud.AbilityButton.OverrideText(GetString("StonerButtonText"));
        hud.AbilityButton.ToggleVisible(canUse);
    }

    public override Sprite GetAbilityButtonSprite(PlayerControl player, bool shapeshifting)
        => CustomButton.Get("Examine");

    public override string GetLowerText(PlayerControl seer, PlayerControl target = null, bool isForMeeting = false, bool isForHud = false)
    {
        if (seer == null || seer.PlayerId != target?.PlayerId || isForMeeting)
            return string.Empty;

        if (IsPlantReady(seer))
        {
            return ColorString(
                GetRoleColor(CustomRoles.Stoner),
                string.Format(GetString("StonerPlantReadyLower"), (int)MathF.Max(seer.GetAbilityUseLimit(), 0)));
        }

        if (!PlantStartTime.TryGetValue(seer.PlayerId, out var startTime))
            return string.Empty;

        float growthTime = MathF.Max(PlantGrowthTime.GetFloat(), 1f);
        float elapsed = Math.Clamp(Utils.TimeStamp - startTime, 0f, growthTime);
        int percent = (int)MathF.Round((elapsed / growthTime) * 100f);

        return ColorString(
            GetRoleColor(CustomRoles.Stoner),
            string.Format(GetString("StonerPlantGrowing"), percent));
    }
}
