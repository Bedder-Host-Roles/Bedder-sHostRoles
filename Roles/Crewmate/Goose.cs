using AmongUs.GameOptions;
using Hazel;
using BHR.Modules.Rpc;
using BHR.Roles.Core;
using BHR.Roles.Impostor;
using BHR.Roles.Neutral;
using UnityEngine;
using static BHR.Options;
using static BHR.Translator;

namespace BHR.Roles.Crewmate;

internal class Goose : RoleBase
{
    public override CustomRoles Role => CustomRoles.Goose;

    private const int Id = 33000;

    public static bool HasEnabled =>
        CustomRoleManager.HasEnabled(CustomRoles.Goose);

    public override CustomRoles ThisRoleBase =>
        CustomRoles.Shapeshifter;

    public override Custom_RoleType ThisRoleType =>
        Custom_RoleType.CrewmatePower;

    public static OptionItem OptionAbductTimerLimit;
    public static OptionItem OptionSpeedDuringDrag;
    public static OptionItem OptionVictimCanUseAbilities;
    public static OptionItem CanBeGuessed;
    public static OptionItem Cooldown;

    public PlayerControl AbductVictim;

    private float AbductTimer;
    private float AbductTimerLimit;
    private bool StopCount;
    private float SpeedDuringDrag;
    private float DefaultSpeed;

    public bool VictimCanUseAbilities { get; private set; }

    internal PlayerControl GoosePlayer;

    public override bool IsEnable =>
        GoosePlayer != null;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(
            Id,
            TabGroup.CrewmateRoles,
            CustomRoles.Goose);

        OptionAbductTimerLimit = FloatOptionItem.Create(
                Id + 11,
                "GooseAbductTimerLimit",
                new(1f, 20f, 1f),
                10f,
                TabGroup.CrewmateRoles,
                false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Goose])
            .SetValueFormat(OptionFormat.Seconds);

        OptionSpeedDuringDrag = FloatOptionItem.Create(
                Id + 13,
                "GooseSpeedDuringDrag",
                new(0.1f, 3f, 0.1f),
                1f,
                TabGroup.CrewmateRoles,
                false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Goose])
            .SetValueFormat(OptionFormat.Multiplier);

        OptionVictimCanUseAbilities = BooleanOptionItem.Create(
                Id + 14,
                "GooseVictimCanUseAbilities",
                false,
                TabGroup.CrewmateRoles,
                false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Goose]);

        CanBeGuessed = BooleanOptionItem.Create(
                Id + 15,
                "CanBeGuessed",
                true,
                TabGroup.CrewmateRoles,
                false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Goose]);

        Cooldown = FloatOptionItem.Create(
                Id + 16,
                "AbilityCooldown",
                new(0f, 180f, 0.5f),
                15f,
                TabGroup.CrewmateRoles,
                false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Goose])
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void Init()
    {
        GoosePlayer = null;
        AbductVictim = null;
        AbductTimer = 255f;
        StopCount = false;
        DefaultSpeed = 1f;
    }

    public override void Add(byte playerId)
    {
        GoosePlayer = Utils.GetPlayerById(playerId);

        AbductTimerLimit = OptionAbductTimerLimit.GetFloat();
        SpeedDuringDrag = OptionSpeedDuringDrag.GetFloat();
        VictimCanUseAbilities = OptionVictimCanUseAbilities.GetBool();

        AbductTimer = 255f;
        StopCount = false;
        AbductVictim = null;

        if (GoosePlayer != null)
        {
            DefaultSpeed = Main.AllPlayerSpeed[playerId];
            GoosePlayer.MarkDirtySettings();
        }
    }

    public override void Remove(byte playerId)
    {
        RemoveVictim();
        GoosePlayer = null;
    }

    public override void SetKillCooldown(byte id)
    {
        Main.AllPlayerKillCooldown[id] = Cooldown.GetFloat();
    }

    public override bool CanUseKillButton(PlayerControl pc)
    {
        return pc != null && pc.IsAlive();
    }

    public override bool CanUseImpostorVentButton(PlayerControl pc)
    {
        return false;
    }

    public override bool CanUseSabotage(PlayerControl pc)
    {
        return false;
    }

    public override void ApplyGameOptions(
        IGameOptions opt,
        byte playerId)
    {
        opt.SetVision(false);

        AURoleOptions.ShapeshifterCooldown =
            AbductVictim != null
                ? AbductTimer
                : AbductTimerLimit;

        AURoleOptions.ShapeshifterDuration = 1f;
    }

    private void SendRPC()
    {
        if (!IsEnable)
            return;

        var writer = MessageWriter.Get(SendOption.Reliable);

        writer.Write(
            AbductVictim?.PlayerId ?? 255);

        LegacyRpcSenders.SendSyncRoleSkill(
                PlayerControl.LocalPlayer.NetId,
                GoosePlayer.NetId,
                writer);
    }

    public override void ReceiveRPC(
        MessageReader reader,
        PlayerControl pc)
    {
        var victim = reader.ReadByte();

        if (victim == 255)
        {
            AbductVictim = null;
            AbductTimer = 255f;
        }
        else
        {
            AbductVictim = Utils.GetPlayerById(victim);
            AbductTimer = AbductTimerLimit;
        }
    }

    private void AddVictim(PlayerControl target)
    {
        if (!IsEnable ||
            target == null ||
            !target.IsAlive())
            return;

        Main.PlayerStates[target.PlayerId]
            .CanUseMovingPlatform = _state.CanUseMovingPlatform = false;

        AbductVictim = target;
        AbductTimer = AbductTimerLimit;

        Main.AllPlayerSpeed[GoosePlayer.PlayerId] =
            SpeedDuringDrag;

        GoosePlayer.MarkDirtySettings();
        GoosePlayer.RpcResetAbilityCooldown();

        SendRPC();
    }

    private void RemoveVictim()
    {
        if (AbductVictim != null &&
            Main.PlayerStates.TryGetValue(
                AbductVictim.PlayerId,
                out var victimState))
        {
            victimState.CanUseMovingPlatform = true;
        }

        AbductVictim = null;
        AbductTimer = 255f;

        if (GoosePlayer == null)
            return;

        Main.AllPlayerSpeed[GoosePlayer.PlayerId] =
            DefaultSpeed;

        if (_state != null)
            _state.CanUseMovingPlatform = true;

        GoosePlayer.MarkDirtySettings();
        GoosePlayer.RpcResetAbilityCooldown();

        SendRPC();
    }

    public override bool OnCheckMurderAsKiller(
        PlayerControl killer,
        PlayerControl target)
    {
        Logger.Info(
            $"Goose action: killer={killer?.GetNameWithRole()?.RemoveHtmlTags()} target={target?.GetNameWithRole()?.RemoveHtmlTags()} victim={AbductVictim?.PlayerId.ToString() ?? "none"}",
            "Goose.OnCheckMurderAsKiller");

        if (!IsEnable || target == null)
        {
            Logger.Info(
                $"Goose action cancelled: IsEnable={IsEnable}, targetNull={target == null}",
                "Goose.OnCheckMurderAsKiller");
            return false;
        }

        if (AbductVictim != null)
        {
            Logger.Info(
                $"Goose releasing current victim {AbductVictim.PlayerId}",
                "Goose.OnCheckMurderAsKiller");
            RemoveVictim();
            return false;
        }

        if (Penguin.IsAbducted(target) || Penguin.IsAbductor(target))
        {
            Logger.Info(
                $"Goose action blocked because target is Penguin-abducted/abductor: {target.PlayerId}",
                "Goose.OnCheckMurderAsKiller");
            return false;
        }

        AddVictim(target);

        Logger.Info(
            $"Goose successfully started dragging target {target.PlayerId}",
            "Goose.OnCheckMurderAsKiller");

        return false;
    }

    public override bool OnCheckShapeshift(
        PlayerControl shapeshifter,
        PlayerControl target,
        ref bool resetCooldown,
        ref bool shouldAnimate)
    {
        resetCooldown = false;
        shouldAnimate = false;

        return false;
    }

    public override Sprite GetKillButtonSprite(
        PlayerControl player,
        bool shapeshifting)
    {
        return CustomButton.Get("Drag");
    }

    public override void SetAbilityButtonText(
        HudManager hud,
        byte playerId)
    {
        hud.KillButton?.OverrideText(
            GetString("GooseKillButtonText"));

        hud.AbilityButton?.OverrideText(
            GetString("GooseTimerText"));

        hud.AbilityButton?.ToggleVisible(
            AbductVictim != null);
    }

    public override void OnReportDeadBody(
        PlayerControl reporter,
        NetworkedPlayerInfo target)
    {
        StopCount = true;

        if (AbductVictim == null)
            return;

        RemoveVictim();
    }

    public override void AfterMeetingTasks()
    {
        if (GameStates.AirshipIsActive)
            return;

        RestartAbduct();
    }

    public void OnSpawnAirship()
    {
        RestartAbduct();
    }

    private void RestartAbduct()
    {
        if (!IsEnable)
            return;

        if (AbductVictim != null)
        {
            GoosePlayer?.MarkDirtySettings();
            GoosePlayer?.RpcResetAbilityCooldown();
        }

        StopCount = false;
    }

    public override void OnCoEnterVent(
        PlayerPhysics physics,
        int ventId)
    {
        if (AbductVictim != null)
        {
            _ = new LateTask(
                () =>
                {
                    physics?.RpcBootFromVent(ventId);
                },
                0.5f,
                $"Goose {physics.myPlayer?.PlayerId} - Boot From Vent");
        }
    }

    public override bool OnCoEnterVentOthers(
        PlayerPhysics physics,
        int ventId)
    {
        if (AbductVictim != null &&
            physics.myPlayer?.PlayerId == AbductVictim.PlayerId)
        {
            _ = new LateTask(
                () =>
                {
                    physics?.RpcBootFromVent(ventId);
                },
                0.5f,
                $"Goose AbductVictim {physics.myPlayer?.PlayerId} - Boot From Vent");

            return true;
        }

        return false;
    }

    public override void OnFixedUpdate(
        PlayerControl goose,
        bool lowLoad,
        long nowTime,
        int timerLowLoad)
    {
        if (!IsEnable)
            return;

        if (!StopCount)
            AbductTimer -= Time.fixedDeltaTime;

        if (AbductVictim != null)
        {
            if (!goose.IsAlive() ||
                !AbductVictim.IsAlive() ||
                Pelican.IsEaten(AbductVictim.PlayerId))
            {
                RemoveVictim();
                return;
            }

            if (AbductTimer <= 0f &&
                !goose.MyPhysics.Animations.IsPlayingAnyLadderAnimation() &&
                !goose.inMovingPlat &&
                !goose.onLadder)
            {
                RemoveVictim();
                return;
            }

            if (!AbductVictim.MyPhysics.Animations.IsPlayingAnyLadderAnimation() &&
                !AbductVictim.inMovingPlat &&
                !AbductVictim.onLadder)
            {
                Vector2 position = goose.transform.position;

                if (!goose.IsHost())
                {
                    AbductVictim.RpcTeleport(
                        position,
                        sendInfoInLogs: false);
                }
                else
                {
                    var victim = AbductVictim;

                    _ = new LateTask(
                        () =>
                        {
                            victim?.RpcTeleport(
                                position,
                                sendInfoInLogs: false);
                        },
                        0.25f,
                        "Goose Teleport",
                        shoudLog: false);
                }
            }
        }
        else if (AbductTimer <= 100f)
        {
            AbductTimer = 255f;
            goose.RpcResetAbilityCooldown();
        }
    }

    public override string GetSuffix(
        PlayerControl seer,
        PlayerControl target,
        bool isForMeeting = false)
    {
        if (GoosePlayer == null ||
            seer.PlayerId != GoosePlayer.PlayerId ||
            target.PlayerId != GoosePlayer.PlayerId ||
            AbductTimer > AbductTimerLimit)
        {
            return string.Empty;
        }

        return $"↘ {(int)(AbductTimer + 1f)}s";
    }
}
