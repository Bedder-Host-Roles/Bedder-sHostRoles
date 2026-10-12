using Hazel;
using System;
using System.Text.RegularExpressions;
using BHR.Modules;
using BHR.Modules.ChatManager;
using BHR.Modules.Rpc;
using BHR.Patches;
using BHR.Roles.Coven;
using BHR.Roles.Double;
using BHR.Roles.Impostor;
using UnityEngine;
using static BHR.Translator;
using static BHR.Utils;
using BHR.Roles.Core;

namespace BHR.Roles.Crewmate;

internal class Prosecutor : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Prosecutor;
    private const int Id = 10700;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateKilling;
    //==================================================================\\

    public static OptionItem TrialLimitPerMeeting;
    private static OptionItem TrialLimitPerGame;
    private static OptionItem CanTrialMadmate;
    private static OptionItem CanTrialCharmed;
    private static OptionItem CanTrialSidekick;
    private static OptionItem CanTrialInfected;
    private static OptionItem CanTrialContagious;
    private static OptionItem CanTrialEnchanted;
    private static OptionItem CanTrialCrewKilling;
    private static OptionItem CanTrialNeutralB;
    private static OptionItem CanTrialNeutralP;
    private static OptionItem CanTrialNeutralK;
    private static OptionItem CanTrialNeutralE;
    private static OptionItem CanTrialNeutralC;
    private static OptionItem CanTrialNeutralA;
    private static OptionItem CanTrialCoven;
    private static OptionItem CanTrialAdmired;

    private static readonly Dictionary<byte, int> TrialLimitMeeting = [];
    private static readonly Dictionary<byte, int> TrialLimitGame = [];

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Prosecutor);
        TrialLimitPerMeeting = IntegerOptionItem.Create(Id + 10, "ProsecutorTrialLimitPerMeeting", new(1, 30, 1), 1, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Prosecutor])
            .SetValueFormat(OptionFormat.Times);
        TrialLimitPerGame = IntegerOptionItem.Create(Id + 25, "ProsecutorTrialLimitPerGame", new(1, 30, 1), 1, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Prosecutor])
            .SetValueFormat(OptionFormat.Times);
        CanTrialMadmate = BooleanOptionItem.Create(Id + 12, "ProsecutorCanTrialMadmate", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Prosecutor]);
        CanTrialCharmed = BooleanOptionItem.Create(Id + 16, "ProsecutorCanTrialCharmed", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Prosecutor]);
        CanTrialSidekick = BooleanOptionItem.Create(Id + 19, "ProsecutorCanTrialSidekick", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Prosecutor]);
        CanTrialInfected = BooleanOptionItem.Create(Id + 20, "ProsecutorCanTrialInfected", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Prosecutor]);
        CanTrialContagious = BooleanOptionItem.Create(Id + 21, "ProsecutorCanTrialContagious", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Prosecutor]);
        CanTrialEnchanted = BooleanOptionItem.Create(Id + 24, "ProsecutorCanTrialEnchanted", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Prosecutor]);
        CanTrialCrewKilling = BooleanOptionItem.Create(Id + 13, "ProsecutorCanTrialnCrewKilling", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Prosecutor]);
        CanTrialNeutralB = BooleanOptionItem.Create(Id + 14, "ProsecutorCanTrialNeutralB", false, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Prosecutor]);
        CanTrialNeutralP = BooleanOptionItem.Create(Id + 27, "ProsecutorCanTrialNeutralP", false, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Prosecutor]);
        CanTrialNeutralE = BooleanOptionItem.Create(Id + 17, "ProsecutorCanTrialNeutralE", false, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Prosecutor]);
        CanTrialNeutralC = BooleanOptionItem.Create(Id + 18, "ProsecutorCanTrialNeutralC", false, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Prosecutor]);
        CanTrialNeutralK = BooleanOptionItem.Create(Id + 15, "ProsecutorCanTrialNeutralK", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Prosecutor]);
        CanTrialNeutralA = BooleanOptionItem.Create(Id + 22, "ProsecutorCanTrialNeutralA", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Prosecutor]);
        CanTrialCoven = BooleanOptionItem.Create(Id + 23, "ProsecutorCanTrialCoven", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Prosecutor]);
        CanTrialAdmired = BooleanOptionItem.Create(Id + 26, "ProsecutorCanTrialAdmired", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Prosecutor]);
    }
    public override void Init()
    {
        TrialLimitMeeting.Clear();
        TrialLimitGame.Clear();
    }
    public override void Add(byte playerId)
    {
        TrialLimitMeeting[playerId] = TrialLimitPerMeeting.GetInt();
        TrialLimitGame[playerId] = TrialLimitPerGame.GetInt();
        playerId.SetAbilityUseLimit(TrialLimitPerGame.GetInt());
    }
    public override void Remove(byte playerId)
    {
        TrialLimitMeeting.Remove(playerId);
        TrialLimitGame.Remove(playerId);
    }
    public override void OnReportDeadBody(PlayerControl party, NetworkedPlayerInfo dinosaur)
    {
        if (!_Player) return;

        TrialLimitMeeting[_Player.PlayerId] = TrialLimitPerMeeting.GetInt();

        if (TrialLimitGame[_Player.PlayerId] <= TrialLimitPerMeeting.GetInt())
        {
            _Player.SetAbilityUseLimit(TrialLimitGame[_Player.PlayerId]);
        }
        else
        {
            _Player.SetAbilityUseLimit(TrialLimitPerMeeting.GetInt());
        }
    }
    public override void AfterMeetingTasks()
    {
        if (!_Player) return;
        _Player.SetAbilityUseLimit(TrialLimitGame[_Player.PlayerId]);
    }

    public static void TrialCommand(PlayerControl player, string commandKey, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            ChatCommands.RequestCommandProcessingFromHost(text, commandKey);
            return;
        }
        
        if (player.Is(CustomRoles.Prosecutor))
            TrialMsg(player, text);
        else if (player.Is(CustomRoles.Councillor))
            Councillor.MurderMsg(player, text);
    }

    public override bool OnJudge(PlayerControl pc, PlayerControl target)
    {
        Logger.Info($"{pc.GetNameWithRole()} try trial {target.GetNameWithRole()}", "Prosecutor");
        bool judgeSuicide = true;
        if (TrialLimitMeeting[pc.PlayerId] < 1)
        {
            pc.ShowInfoMessage(false, GetString("ProsecutorTrialMaxMeetingMsg"));
            return true;
        }
        if (pc.GetAbilityUseLimit() < 1)
        {
            pc.ShowInfoMessage(false, GetString("ProsecutorTrialMaxGameMsg"));
        }
        if (target.Is(CustomRoles.VoodooMaster) && VoodooMaster.Dolls[target.PlayerId].Count > 0)
        {
            target = GetPlayerById(VoodooMaster.Dolls[target.PlayerId].Where(x => GetPlayerById(x).IsAlive()).ToList().RandomElement());
            SendMessage(string.Format(GetString("VoodooMasterTargetInMeeting"), target.GetRealName()), Utils.GetPlayerListByRole(CustomRoles.VoodooMaster).First().PlayerId);
        }
        if (Jailer.IsTarget(target.PlayerId))
        {
            pc.ShowInfoMessage(false, GetString("CanNotTrialJailed"), ColorString(GetRoleColor(CustomRoles.Jailer), GetString("Jailer").ToUpper()));
            return true;
        }
        if (pc.PlayerId == target.PlayerId)
        {
            pc.ShowInfoMessage(false, GetString("Prosecutor_LaughToWhoTrialSelf"), ColorString(Color.cyan, GetString("MessageFromKPD")));
            goto SkipToPerform;
        }
        if (target.Is(CustomRoles.NiceMini) && Mini.Age < 18)
        {
            pc.ShowInfoMessage(false, GetString("GuessMini"));
            return true;
        }
        if (target.Is(CustomRoles.PunchingBag))
        {
            pc.ShowInfoMessage(false, GetString("EradicatePunchingBag"));
            return true;
        }

        if (target.Is(CustomRoles.Rebound))
        {
            Logger.Info($"{pc.GetNameWithRole()} judged {target.GetNameWithRole()}, judge sucide = true because target rebound", "ProsecutorTrialMsg");
            judgeSuicide = true;
        }
        else if (target.Is(CustomRoles.Onbound))
        {
            pc.ShowInfoMessage(false, GetString("GuessOnbound"));
            return true;
        }
        else if (target.Is(CustomRoles.Solsticer))
        {
            pc.ShowInfoMessage(false, GetString("GuessSolsticer"));
            return true;
        }
        else if (target.Is(CustomRoles.Admired) && !CanTrialAdmired.GetBool()) judgeSuicide = true;
        else if (target.IsTransformedNeutralApocalypse()) judgeSuicide = true;
        else if (Medic.IsProtected(target.PlayerId) && !Medic.GuesserIgnoreShield.GetBool())
        {
            pc.ShowInfoMessage(false, GetString("GuessShielded"));
            return true;
        }
        else if (Guardian.CannotBeKilled(target))
        {
            pc.ShowInfoMessage(false, GetString("GuessGuardianTask"));
            return true;
        }
        else if (pc.IsAnySubRole(x => x.IsConverted())) judgeSuicide = false;
        else if (target.Is(CustomRoles.Trickster)) judgeSuicide = true;
        else if (target.Is(CustomRoles.Rascal)) judgeSuicide = false;
        else if (target.Is(CustomRoles.Narc)) judgeSuicide = true;
        else if ((target.Is(CustomRoles.Sidekick) || target.Is(CustomRoles.Recruit)) && CanTrialSidekick.GetBool()) judgeSuicide = false;
        else if ((target.GetCustomRole().IsMadmate() || target.Is(CustomRoles.Madmate)) && CanTrialMadmate.GetBool()) judgeSuicide = false;
        else if (target.Is(CustomRoles.Infected) && CanTrialInfected.GetBool()) judgeSuicide = false;
        else if (target.Is(CustomRoles.Contagious) && CanTrialContagious.GetBool()) judgeSuicide = false;
        else if (target.Is(CustomRoles.Charmed) && CanTrialCharmed.GetBool()) judgeSuicide = false;
        else if (target.Is(CustomRoles.Enchanted) && CanTrialEnchanted.GetBool()) judgeSuicide = false;
        else if (target.GetCustomRole().IsCrewKiller() && CanTrialCrewKilling.GetBool()) judgeSuicide = false;
        else if (target.GetCustomRole().IsNK() && CanTrialNeutralK.GetBool()) judgeSuicide = false;
        else if (target.GetCustomRole().IsNB() && CanTrialNeutralB.GetBool()) judgeSuicide = false;
        else if (target.GetCustomRole().IsNP() && CanTrialNeutralP.GetBool()) judgeSuicide = false;
        else if (target.GetCustomRole().IsNE() && CanTrialNeutralE.GetBool()) judgeSuicide = false;
        else if (target.GetCustomRole().IsNC() && CanTrialNeutralC.GetBool()) judgeSuicide = false;
        else if (target.GetCustomRole().IsNA() && CanTrialNeutralA.GetBool()) judgeSuicide = false;
        else if (target.GetCustomRole().IsCoven() && CanTrialCoven.GetBool()) judgeSuicide = false;
        else if (target.GetCustomRole().IsImpostor()) judgeSuicide = false;
        else
        {
            Logger.Warn("Impossibe to reach here!", "ProsecutorTrial");
            judgeSuicide = true;
        }

    SkipToPerform:
        var dp = judgeSuicide ? pc : target;
        target = dp;

        string Name = dp.GetRealName();

        TrialLimitMeeting[pc.PlayerId]--;
        TrialLimitGame[pc.PlayerId]--;
        pc.RpcRemoveAbilityUse();

        if (!GameStates.IsProceeding)
            _ = new LateTask(() =>
            {
                dp.SetDeathReason(PlayerState.DeathReason.Trialed);
                dp.SetRealKiller(pc);
                GuessManager.RpcGuesserMurderPlayer(dp);

                Main.PlayersDiedInMeeting.Add(dp.PlayerId);
                MurderPlayerPatch.AfterPlayerDeathTasks(pc, dp, true);

                _ = new LateTask(() => { SendMessage(string.Format(GetString("Prosecutor_TrialKill"), Name), 255, ColorString(GetRoleColor(CustomRoles.Prosecutor), GetString("Prosecutor_TrialKillTitle")), true); }, 0.6f, "Guess Msg");

            }, 0.2f, "Trial Kill");
        
        return true;
    }

    public static bool TrialMsg(PlayerControl pc, string msg, bool isUI = false)
    {
        var originMsg = msg;

        if (!AmongUsClient.Instance.AmHost) return false;
        if (!GameStates.IsMeeting || pc == null || GameStates.IsExilling) return false;
        if (!pc.Is(CustomRoles.Prosecutor)) return false;

        msg = msg.ToLower().TrimStart().TrimEnd();

        if (!pc.IsAlive())
        {
            pc.ShowInfoMessage(isUI, GetString("ProsecutorDead"));
            return true;
        }

        if (!MsgToPlayerAndRole(msg, out byte targetId, out string error))
        {
            SendMessage(error, pc.PlayerId);
            return true;
        }
        var target = GetPlayerById(targetId);
        if (target != null)
        {
            pc.GetRoleClass().OnJudge(pc, target);
        }
        return true;
    }
    private static bool MsgToPlayerAndRole(string msg, out byte id, out string error)
    {
        if (msg.StartsWith("/")) msg = msg.Replace("/", string.Empty);

        Regex r = new("\\d+");
        MatchCollection mc = r.Matches(msg);
        string result = string.Empty;
        for (int i = 0; i < mc.Count; i++)
        {
            result += mc[i];
        }

        if (int.TryParse(result, out int num))
        {
            id = Convert.ToByte(num);
        }
        else
        {
            id = byte.MaxValue;
            error = GetString("Prosecutor_TrialHelp");
            return false;
        }

        PlayerControl target = GetPlayerById(id);
        if (target == null || target.Data.IsDead || !target.IsAlive())
        {
            error = GetString("Prosecutor_TrialNull");
            return false;
        }

        error = string.Empty;
        return true;
    }

    public override string NotifyPlayerName(PlayerControl seer, PlayerControl target, string TargetPlayerName = "", bool IsForMeeting = false)
        => IsForMeeting && seer.IsAlive() && target.IsAlive() ? ColorString(GetRoleColor(CustomRoles.Prosecutor), target.PlayerId.ToString()) + " " + TargetPlayerName : "";
    public override string PVANameText(PlayerVoteArea pva, PlayerControl seer, PlayerControl target)
        => seer.IsAlive() && target.IsAlive() ? ColorString(GetRoleColor(CustomRoles.Prosecutor), target.PlayerId.ToString()) + " " + pva.NameText.text : "";
}
