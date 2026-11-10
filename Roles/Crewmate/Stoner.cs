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
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
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

        // No joint is available until the plant finishes growing.
        playerId.SetAbilityUseLimit(0);
    }

    public override void Remove(byte playerId)
    {
        PlantStartTime.Remove(playerId);
        PlantReady.Remove(playerId);
    }

    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime, int timerLowLoad)
    {
        // The host owns plant completion so clients cannot independently unlock
        // the ability at a different time.
        if (!AmongUsClient.Instance.AmHost) return;
        if (player == null || !player.IsAlive()) return;
        if (!GameStates.IsInTask) return;
        if (!PlantStartTime.TryGetValue(player.PlayerId, out var startTime)) return;
        if (PlantReady.Contains(player.PlayerId)) return;

        if (nowTime - startTime < PlantGrowthTime.GetInt()) return;

        PlantReady.Add(player.PlayerId);
        player.SetAbilityUseLimit(JointUses.GetInt());

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

    public override bool CanUseKillButton(PlayerControl player)
    {
        if (player == null || !player.IsAlive()) return false;
        if (!IsPlantReady(player)) return false;

        return player.GetAbilityUseLimit() >= 1f;
    }

    public override bool ForcedCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null) return false;
        if (killer.PlayerId == target.PlayerId) return false;
        if (!killer.IsAlive() || !target.IsAlive()) return false;
        if (!IsPlantReady(killer)) return false;
        if (killer.GetAbilityUseLimit() < 1f) return false;

        // The Kill button is only being used as the target-selection button.
        // Never allow the normal murder to happen.
        killer.RpcRemoveAbilityUse();

        var targetRole = target.GetCustomRole();
        bool isImpostorAffiliation = targetRole.IsImpostorTeamV3();

        // Only the local Stoner should see the result. This is important when
        // the host is processing a non-host Stoner's target request.
        if (killer.PlayerId == PlayerControl.LocalPlayer?.PlayerId)
        {
            var resultColor = isImpostorAffiliation
                ? new Color(1f, 0.10f, 0.10f, 0.50f)
                : new Color(0.20f, 1f, 0.25f, 0.50f);

            Utils.FlashColor(resultColor, 0.65f);

            string result = isImpostorAffiliation
                ? GetString("StonerResultImpostor")
                : GetString("StonerResultSafe");

            HudManager.Instance.ShowPopUp(result);
        }

        killer.ResetKillCooldown();
        killer.SetKillCooldown();

        return false;
    }

    public override void SetKillCooldown(byte id)
    {
        Main.AllPlayerKillCooldown[id] = JointCooldown.GetFloat();
    }

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.KillButton.OverrideText(GetString("StonerButtonText"));
    }

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
