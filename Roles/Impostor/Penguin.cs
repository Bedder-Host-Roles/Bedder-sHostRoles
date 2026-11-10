using AmongUs.GameOptions;
using Hazel;
using BHR.Modules.Rpc;
using BHR.Roles.Core;
using UnityEngine;
using BHR.Roles.Crewmate;
using static BHR.Options;
using static BHR.Translator;

// https://github.com/tukasa0001/TownOfHost/blob/main/Roles/Impostor/Penguin.cs
namespace BHR.Roles.Impostor;

internal class Penguin : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Penguin;
    private const int Id = 27500;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Penguin);
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorHindering;
    //==================================================================\\

    private static OptionItem OptionAbductTimerLimit;
    private static OptionItem OptionMeetingKill;
    private static OptionItem OptionVictimCanUseAbilities;

    public PlayerControl AbductVictim;
    private float AbductTimer;
    private float AbductTimerLimit;
    private bool stopCount;
    private bool MeetingKill;
    public bool VictimCanUseAbilities { get; private set; }

    // Measures to prevent an opponent who is being abducted from using kill/other abilities.
    // Goose uses the same shared check so the existing TOHE patches also cover Goose victims.
    public static bool IsVictim(PlayerControl pc)
    {
        if (pc == null) return false;

        foreach (var penguin in Utils.GetRoleBasesByType<Penguin>())
        {
            if (penguin == null || !penguin.IsEnable)
                continue;

            if (!penguin.VictimCanUseAbilities &&
                penguin.AbductVictim != null &&
                penguin.AbductVictim.PlayerId == pc.PlayerId)
                return true;
        }

        foreach (var goose in Utils.GetRoleBasesByType<Goose>())
        {
            if (goose == null || !goose.IsEnable)
                continue;

            if (!goose.VictimCanUseAbilities &&
                goose.AbductVictim != null &&
                goose.AbductVictim.PlayerId == pc.PlayerId)
                return true;
        }

        return false;
    }

    public static bool IsAbducted(PlayerControl pc)
    {
        if (pc == null) return false;

        foreach (var p in Utils.GetRoleBasesByType<Penguin>())
        {
            if (p == null || !p.IsEnable || p.AbductVictim == null)
                continue;

            if (p.AbductVictim.PlayerId == pc.PlayerId)
                return true;
        }

        foreach (var g in Utils.GetRoleBasesByType<Goose>())
        {
            if (g == null || !g.IsEnable || g.AbductVictim == null)
                continue;

            if (g.AbductVictim.PlayerId == pc.PlayerId)
                return true;
        }

        return false;
    }

    public static bool IsAbductor(PlayerControl pc)
    {
        if (pc == null) return false;

        foreach (var p in Utils.GetRoleBasesByType<Penguin>())
        {
            if (p == null || !p.IsEnable || p._Player == null)
                continue;

            if (p._Player.PlayerId == pc.PlayerId &&
                p.AbductVictim != null)
                return true;
        }

        foreach (var g in Utils.GetRoleBasesByType<Goose>())
        {
            if (g == null || !g.IsEnable || g.GoosePlayer == null)
                continue;

            if (g.AbductVictim != null &&
                g.GoosePlayer.PlayerId == pc.PlayerId)
                return true;
        }

        return false;
    }

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Penguin, 1);
        OptionAbductTimerLimit = FloatOptionItem.Create(Id + 11, "PenguinAbductTimerLimit", new(1f, 20f, 1f), 10f, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Penguin])
            .SetValueFormat(OptionFormat.Seconds);
        OptionMeetingKill = BooleanOptionItem.Create(Id + 13, "PenguinMeetingKill", true, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Penguin]);
        OptionVictimCanUseAbilities = BooleanOptionItem.Create(Id + 14, "PenguinVictimCanUseAbilities", false, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Penguin]);
    }

    public override void Add(byte playerId)
    {
        AbductTimerLimit = OptionAbductTimerLimit.GetFloat();
        MeetingKill = OptionMeetingKill.GetBool();
        VictimCanUseAbilities = OptionVictimCanUseAbilities.GetBool();

        AbductTimer = 255f;
        stopCount = false;
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = AbductVictim != null ? AbductTimer : AbductTimerLimit;
        AURoleOptions.ShapeshifterDuration = 1f;
    }

    private void SendRPC()
    {
        var writer = MessageWriter.Get(SendOption.Reliable);
        writer.Write(AbductVictim?.PlayerId ?? 255);
        LegacyRpcSenders.SendSyncRoleSkill(PlayerControl.LocalPlayer.NetId, _Player.NetId, writer);
    }

    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
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

    private void AddVictim(PlayerControl penguin, PlayerControl target)
    {
        if (penguin == null || target == null || _state == null)
            return;

        if (!Main.PlayerStates.TryGetValue(target.PlayerId, out var targetState))
            return;

        targetState.CanUseMovingPlatform = false;
        _state.CanUseMovingPlatform = false;
        AbductVictim = target;
        AbductTimer = AbductTimerLimit;
        penguin.MarkDirtySettings();
        penguin.RpcResetAbilityCooldown();
        SendRPC();
    }

    private void RemoveVictim()
    {
        if (AbductVictim != null)
        {
            if (Main.PlayerStates.TryGetValue(AbductVictim.PlayerId, out var victimState))
                victimState.CanUseMovingPlatform = true;

            AbductVictim = null;
        }

        AbductTimer = 255f;

        var penguin = _Player;
        if (penguin == null) return;

        if (_state != null)
            _state.CanUseMovingPlatform = true;

        penguin.MarkDirtySettings();
        penguin.RpcResetAbilityCooldown();
        SendRPC();
    }

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        bool doKill = true;

        if (AbductVictim != null)
        {
            if (target != AbductVictim && !target.IsTransformedNeutralApocalypse())
            {
                // During an abduction, only the abductee can be killed.
                killer?.RpcMurderPlayer(AbductVictim);
                killer?.ResetKillCooldown();
                doKill = false;
            }

            RemoveVictim();
        }
        else
        {
            doKill = false;
            if (IsAbducted(target) || IsAbductor(target)) return false;
            AddVictim(killer, target);
        }

        return doKill;
    }

    public override bool OnCheckShapeshift(PlayerControl shapeshifter, PlayerControl target, ref bool resetCooldown, ref bool shouldAnimate)
    {
        // not should shapeshifted
        resetCooldown = false;
        return false;
    }

    public override Sprite GetAbilityButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Timer");

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.KillButton?.OverrideText(AbductVictim != null ? GetString("KillButtonText") : GetString("PenguinKillButtonText"));
        hud.AbilityButton?.OverrideText(GetString("PenguinTimerText"));
        hud.AbilityButton?.ToggleVisible(AbductVictim != null);
    }

    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        stopCount = true;
        if (AbductVictim == null) return;

        // If you meet a meeting with time running out, kill it even if you're on a ladder.
        if (AbductVictim.IsTransformedNeutralApocalypse())
        {
            RemoveVictim();
            Logger.Info($"{AbductVictim.GetRealName()} is TNA, no meeting kill", "Penguin");
            return;
        }

        if (AbductTimer <= 0f)
            _Player?.RpcMurderPlayer(AbductVictim);

        if (MeetingKill)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            _Player?.RpcMurderPlayer(AbductVictim);
        }

        RemoveVictim();
    }

    public override void AfterMeetingTasks()
    {
        if (GameStates.AirshipIsActive) return;

        //Maps other than Airship
        RestartAbduct();
    }

    public void OnSpawnAirship()
    {
        RestartAbduct();
    }

    private void RestartAbduct()
    {
        if (!HasEnabled) return;

        if (AbductVictim != null)
        {
            _Player?.MarkDirtySettings();
            _Player?.RpcResetAbilityCooldown();
            stopCount = false;
        }
    }

    public override void OnCoEnterVent(PlayerPhysics physics, int ventId)
    {
        if (AbductVictim != null)
        {
            _ = new LateTask(() =>
            {
                physics?.RpcBootFromVent(ventId);
            }, 0.5f, $"Penguin {physics.myPlayer?.PlayerId} - Boot From Vent");
        }
    }

    public override bool OnCoEnterVentOthers(PlayerPhysics physics, int ventId)
    {
        if (AbductVictim != null)
        {
            if (physics.myPlayer.PlayerId == AbductVictim.PlayerId)
            {
                _ = new LateTask(() =>
                {
                    physics?.RpcBootFromVent(ventId);
                }, 0.5f, $"AbductVictim {physics.myPlayer?.PlayerId} - Boot From Vent");
                return true;
            }
        }

        return false;
    }

    public override void OnFixedUpdate(PlayerControl penguin, bool lowLoad, long nowTime, int timerLowLoad)
    {
        if (!stopCount)
            AbductTimer -= Time.fixedDeltaTime;

        if (AbductVictim != null)
        {
            if (!penguin.IsAlive() || !AbductVictim.IsAlive())
            {
                RemoveVictim();
                return;
            }

            if (AbductTimer <= 0f &&
                !penguin.MyPhysics.Animations.IsPlayingAnyLadderAnimation() &&
                !AbductVictim.IsTransformedNeutralApocalypse())
            {
                // Set IsDead to true first (prevents ladder chase)
                AbductVictim.Data.IsDead = true;
                AbductVictim.Data.MarkDirty();

                // If the penguin himself is on a ladder, kill him after getting off the ladder.
                if (!AbductVictim.MyPhysics.Animations.IsPlayingAnyLadderAnimation())
                {
                    var abductVictim = AbductVictim;

                    _ = new LateTask(() =>
                    {
                        var sId = abductVictim.NetTransform.lastSequenceId + 5;
                        //Host side
                        abductVictim.NetTransform.SnapTo(penguin.transform.position, (ushort)sId);
                        penguin.MurderPlayer(abductVictim, ExtendedPlayerControl.ResultFlags);

                        var sender = CustomRpcSender.Create("PenguinMurder");
                        {
                            sender.AutoStartRpc(abductVictim.NetTransform.NetId, (byte)RpcCalls.SnapTo);
                            {
                                NetHelpers.WriteVector2(penguin.transform.position, sender.stream);
                                sender.Write(abductVictim.NetTransform.lastSequenceId);
                            }
                            sender.EndRpc();

                            sender.AutoStartRpc(penguin.NetId, (byte)RpcCalls.MurderPlayer);
                            {
                                sender.WriteNetObject(abductVictim);
                                sender.Write((int)ExtendedPlayerControl.ResultFlags);
                            }
                            sender.EndRpc();
                        }

                        sender.SendMessage();
                    }, 0.3f, "PenguinMurder");

                    RemoveVictim();
                }
            }
            else if (AbductTimer <= 0f &&
                     !penguin.MyPhysics.Animations.IsPlayingAnyLadderAnimation() &&
                     AbductVictim.IsTransformedNeutralApocalypse())
            {
                // If the penguin himself is on a ladder, kill him after getting off the ladder.
                if (!AbductVictim.MyPhysics.Animations.IsPlayingAnyLadderAnimation())
                {
                    var abductVictim = AbductVictim;

                    _ = new LateTask(() =>
                    {
                        var sId = abductVictim.NetTransform.lastSequenceId + 5;
                        //Host side
                        abductVictim.NetTransform.SnapTo(penguin.transform.position, (ushort)sId);
                        penguin.MurderPlayer(abductVictim, ExtendedPlayerControl.ResultFlags);

                        var sender = CustomRpcSender.Create("PenguinMurder");
                        {
                            sender.AutoStartRpc(abductVictim.NetTransform.NetId, (byte)RpcCalls.SnapTo);
                            {
                                NetHelpers.WriteVector2(penguin.transform.position, sender.stream);
                                sender.Write(abductVictim.NetTransform.lastSequenceId);
                            }

                            sender.EndRpc();
                        }

                        sender.SendMessage();
                    }, 0.3f, "PenguinMurder");

                    RemoveVictim();
                }
            }
            // SnapToRPC does not work for players on top of the ladder, and only the host behaves differently, so teleporting is not done uniformly.
            else if (!AbductVictim.MyPhysics.Animations.IsPlayingAnyLadderAnimation())
            {
                var position = penguin.transform.position;

                if (!penguin.IsHost())
                {
                    AbductVictim.RpcTeleport(position, sendInfoInLogs: false);
                }
                else
                {
                    _ = new LateTask(() =>
                    {
                        AbductVictim?.RpcTeleport(position, sendInfoInLogs: false);
                    }, 0.25f, "Penguin Teleport ", shoudLog: false);
                }
            }
        }
        else if (AbductTimer <= 100f)
        {
            AbductTimer = 255f;
            penguin.RpcResetAbilityCooldown();
        }
    }
}
