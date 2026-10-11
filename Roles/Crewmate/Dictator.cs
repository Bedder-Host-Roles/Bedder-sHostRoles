using Hazel;
using System;
using BHR.Modules.ChatManager;
using BHR.Modules.Rpc;
using BHR.Patches;
using BHR.Roles.Core;
using UnityEngine;
using static BHR.Options;
using static BHR.Translator;
using static BHR.Utils;

namespace BHR.Roles.Crewmate;

internal class Dictator : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Dictator;
    private const int Id = 11600;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmatePower;
    //==================================================================\\
    public static OptionItem ChangeCommandToExpel;
    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Dictator);
        ChangeCommandToExpel = BooleanOptionItem.Create(Id + 10, "DictatorChangeCommandToExpel", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Dictator]);
    }

    public static bool CheckVotingForTarget(PlayerControl pc, PlayerVoteArea pva)
        => pc.Is(CustomRoles.Dictator) && pva.DidVote && pc.PlayerId != pva.VotedForId && pva.VotedForId < 253 && !pc.Data.IsDead && pc.IsAlive();

    public static void ExpelCommand(PlayerControl pc, string commandKey, string msg, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            ChatCommands.RequestCommandProcessingFromHost(msg, commandKey);
            return;
        }

        if (!GameStates.IsMeeting || pc == null || GameStates.IsExilling) return;
        if (!pc.IsAlive()) return;
        if (!pc.Is(CustomRoles.Dictator)) return;

        bool isUI = pc.IsModded();

        if (args.Length < 2 || !int.TryParse(args[1], out int targetid))
        {
            pc.ShowInfoMessage(isUI, GetString("Dictator.InvalidTarget"));
            return;
        }

        var target = GetPlayerById(targetid);
        pc.GetRoleClass().OnJudge(pc, target);
    }

    public override bool OnJudge(PlayerControl pc, PlayerControl target)
    {
        if (target == pc)
        {
            pc.ShowInfoMessage(pc.IsModded(), GetString("DictatorExpelSelf"));
            return false;
        }
        if (!target.IsAlive())
        {
            return false;
        }

        if (target.Is(CustomRoles.Solsticer))
        {
            pc.ShowInfoMessage(pc.IsModded(), GetString("ExpelSolsticer"));
            MeetingHud.Instance.RpcClearVoteDelay(pc.PlayerId);
            return false;
        }

        List<MeetingHud.VoterState> statesList = [];
        MeetingHud.VoterState[] states;

        statesList.Add(new()
        {
            VoterId = pc.PlayerId,
            VotedForId = target.PlayerId
        });
        states = [.. statesList];
        var exiled = target.Data;
        var isBlackOut = AntiBlackout.BlackOutIsActive;
        CheckForEndVotingPatch.TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Suicide, pc.PlayerId);
        ExileControllerWrapUpPatch.AntiBlackout_LastExiled = exiled;
        Main.LastVotedPlayerInfo = exiled;
        AntiBlackout.ExilePlayerId = exiled.PlayerId;
        if (AntiBlackout.BlackOutIsActive)
        {
            if (isBlackOut)
                MeetingHud.Instance.AntiBlackRpcVotingComplete(states, exiled, false);
            else
                MeetingHud.Instance.RpcVotingComplete(statesList.ToArray(), exiled, false, false, 0);
            if (exiled != null)
            {
                AntiBlackout.ShowExiledInfo = isBlackOut;
                CheckForEndVotingPatch.ConfirmEjections(exiled, isBlackOut);
                MeetingHud.Instance.RpcVotingComplete(statesList.ToArray(), null, true, false, 0);
                MeetingHud.Instance.RpcClose();
            }
        }
        else
        {
            MeetingHud.Instance.RpcVotingComplete(states, exiled, false, false, 0);

            if (exiled != null)
            {
                CheckForEndVotingPatch.ConfirmEjections(exiled);
            }
        }

        Logger.Info($"{target.GetNameWithRole()} expelled by Dictator", "Dictator");

        CheckForEndVotingPatch.CheckForDeathOnExile(PlayerState.DeathReason.Vote, target.PlayerId);

        Logger.Info("Dictatorial vote, forced closure of the meeting", "Special Phase");

        target.SetRealKiller(pc);

        return true;
    }
    
    public override string NotifyPlayerName(PlayerControl seer, PlayerControl target, string TargetPlayerName = "", bool IsForMeeting = false)
        => IsForMeeting && ChangeCommandToExpel.GetBool() ? ColorString(GetRoleColor(CustomRoles.Dictator), target.PlayerId.ToString()) + " " + TargetPlayerName : "";

    private static void SendDictatorRPC(byte playerId)
    {
        RoleRpcs.SendDictator(PlayerControl.LocalPlayer.NetId, playerId);
    }

    public static void OnReceiveDictatorRPC(MessageReader reader, PlayerControl pc)
    {
        byte pid = reader.ReadByte();
        if (pc.Is(CustomRoles.Dictator) && pc.IsAlive() && GameStates.IsVoting)
        {
            ExpelCommand(pc, "Command.Expel", $"/exp {pid}", ["/exp", $"{pid}"]);
        }
    }

    private void DictatorOnClick(byte playerId, MeetingHud __instance)
    {
        Logger.Msg($"Click: ID {playerId}", "Dictator UI");
        var pc = playerId.GetPlayer();
        if (pc == null || !pc.IsAlive() || !GameStates.IsVoting) return;

        if (AmongUsClient.Instance.AmHost) ExpelCommand(pc, "Command.Expel", $"/exp {playerId}", ["/exp", $"{playerId}"]);
        else SendDictatorRPC(playerId);

        CreateDictatorButton(__instance);
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    class StartMeetingPatch
    {
        public static void Postfix(MeetingHud __instance)
        {
            if (PlayerControl.LocalPlayer.GetRoleClass() is Dictator dictator)
                if (ChangeCommandToExpel.GetBool())
                    dictator.CreateDictatorButton(__instance);
        }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.OnDestroy))]
    class OnDestroyPatch
    {
        public static void Postfix(MeetingHud __instance)
        {
            foreach (var pva in __instance.playerStates)
            {
                if (pva?.transform?.Find("DictatorButton") != null)
                    UnityEngine.Object.Destroy(pva.transform.Find("DictatorButton").gameObject);
            }
        }
    }

    private void CreateDictatorButton(MeetingHud __instance)
    {
        foreach (var pva in __instance.playerStates)
        {
            if (pva.transform.Find("DictatorButton") != null) UnityEngine.Object.Destroy(pva.transform.Find("DictatorButton").gameObject);

            var pc = ((byte)pva.PlayerId).GetPlayer();
            var local = PlayerControl.LocalPlayer;
            if (pc == null || !pc.IsAlive()) continue;

            GameObject template = pva.Buttons.transform.Find("CancelButton").gameObject;
            GameObject targetBox = UnityEngine.Object.Instantiate(template, pva.transform);
            targetBox.name = "DictatorButton";
            targetBox.transform.localPosition = new Vector3(-0.35f, 0.03f, -1.31f);
            SpriteRenderer renderer = targetBox.GetComponent<SpriteRenderer>();
            PassiveButton button = targetBox.GetComponent<PassiveButton>();
            renderer.sprite = CustomButton.Get("JudgeIcon");

            button.OnClick.RemoveAllListeners();
            button.OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
            {
                DictatorOnClick(pva.PlayerId, __instance);
            }));
        }
    }
}
