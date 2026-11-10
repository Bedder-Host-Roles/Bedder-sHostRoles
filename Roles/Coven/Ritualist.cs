using Hazel;
using System;
using System.Text.RegularExpressions;
using BHR.Modules.ChatManager;
using BHR.Patches;
using BHR.Roles.Crewmate;
using UnityEngine;
using static BHR.Options;
using static BHR.Translator;
using static BHR.Utils;

namespace BHR.Roles.Coven;

internal class Ritualist : CovenManager
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Ritualist;
    private const int Id = 30800;
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CovenPower;
    //==================================================================\\

    private static OptionItem MaxRitsPerRound;
    public static OptionItem EnchantedKnowsCoven;
    public static OptionItem EnchantedKnowsEnchanted;

    private static readonly Dictionary<byte, int> RitualLimit = [];
    private static readonly Dictionary<byte, List<byte>> EnchantedPlayers = [];

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.CovenRoles, CustomRoles.Ritualist, 1, zeroOne: false);
        MaxRitsPerRound = IntegerOptionItem.Create(Id + 10, "RitualistMaxRitsPerRound", new(1, 15, 1), 2, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Ritualist])
            .SetValueFormat(OptionFormat.Times);
        EnchantedKnowsCoven = BooleanOptionItem.Create(Id + 12, "RitualistEnchantedKnowsCoven", true, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Ritualist]);
        EnchantedKnowsEnchanted = BooleanOptionItem.Create(Id + 13, "RitualistEnchantedKnowsEnchanted", true, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Ritualist]);

    }
    public override void Init()
    {
        RitualLimit.Clear();
        EnchantedPlayers.Clear();
    }
    public override void Add(byte PlayerId)
    {
        EnchantedPlayers[PlayerId] = [];
        RitualLimit.Add(PlayerId, MaxRitsPerRound.GetInt());
    }
    public override bool CanUseKillButton(PlayerControl pc) => HasNecronomicon(pc);
    public override void OnReportDeadBody(PlayerControl hatsune, NetworkedPlayerInfo miku)
    {
        foreach (var pid in RitualLimit.Keys)
        {
            RitualLimit[pid] = MaxRitsPerRound.GetInt();
        }
    }
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (target.GetCustomRole().IsCovenTeam() && !(Main.PlayerStates[killer.PlayerId].IsRandomizer || Main.PlayerStates[target.PlayerId].IsRandomizer))
        {
            killer.Notify(GetString("CovenDontKillOtherCoven"));
            return false;
        }
        return true;
    }
    public override string NotifyPlayerName(PlayerControl seer, PlayerControl target, string TargetPlayerName = "", bool IsForMeeting = false)
        => IsForMeeting && seer.IsAlive() && target.IsAlive() ? ColorString(GetRoleColor(CustomRoles.Ritualist), target.PlayerId.ToString()) + " " + TargetPlayerName : "";
    public override string PVANameText(PlayerVoteArea pva, PlayerControl seer, PlayerControl target)
        => seer.IsAlive() && target.IsAlive() ? ColorString(GetRoleColor(CustomRoles.Ritualist), target.PlayerId.ToString()) + " " + pva.NameText.text : "";


    public static void RitualCommand(PlayerControl player, string commandKey, string text, string[] args)
    {
        Logger.Info(
            $"RitualCommand ENTERED | Text={text} | Key={commandKey} | " +
            $"Host={AmongUsClient.Instance.AmHost} | " +
            $"Player={(player == null ? "null" : player.PlayerId.ToString())}",
            "RitualistDebug"
        );

        if (!AmongUsClient.Instance.AmHost)
        {
            Logger.Info("Not host; forwarding command to host.", "RitualistDebug");
            ChatCommands.RequestCommandProcessingFromHost(text, commandKey);
            return;
        }

        if (!GameStates.IsMeeting || player == null || GameStates.IsExilling)
        {
            Logger.Warn("Rejected: not in a valid meeting state.", "RitualistDebug");
            return;
        }

        var actualRole = player.GetCustomRole();

        Logger.Info(
            $"Role check | ActualRole={actualRole} | " +
            $"IsRitualist={player.Is(CustomRoles.Ritualist)}",
            "RitualistDebug"
        );

        if (!player.Is(CustomRoles.Ritualist))
        {
            Logger.Warn("Rejected: player failed Ritualist role check.", "RitualistDebug");
            player.ShowInfoMessage(
                player.IsModded(),
                "Ritualist command rejected: role check failed."
            );
            return;
        }

        bool isUI = player.IsModded();


        var originMsg = text;

        if (!GameStates.IsMeeting || player == null || GameStates.IsExilling) return;
        if (!player.Is(CustomRoles.Ritualist)) return;

        if (!player.IsAlive())
        {
            player.ShowInfoMessage(isUI, GetString("GuessDead"));
            return;
        }

        if (!RitualLimit.TryGetValue(player.PlayerId, out int remainingRituals))
        {
            player.ShowInfoMessage(isUI, GetString("RitualistRitualMax"));
            return;
        }

        if (remainingRituals <= 0)
        {
            player.ShowInfoMessage(isUI, GetString("RitualistRitualMax"));
            return;
        }

        if (!GetTargetAndRoleFromCommand(string.Join(" ", args.Skip(1)), out byte targetId, out CustomRoles role, out string error))
        {
            player.ShowInfoMessage(isUI, error);
            return;
        }
        var target = GetPlayerById(targetId);
        if (role.IsAdditionRole())
        {
            player.ShowInfoMessage(isUI, GetString("RitualistGuessAddon"));
            return;
        }
        if (!target.Is(role))
        {
            RPC.PlaySoundRPC(Sounds.SabotageSound, player.PlayerId);
            player.ShowInfoMessage(isUI, GetString("RitualistRitualFail"));
            RitualLimit[player.PlayerId] = 0;
            return;
        }
        if (!target.CanBeRecruitedBy(player))
        {
            player.ShowInfoMessage(isUI, GetString("RitualistRitualImpossible"));
            return;
        }

        Logger.Info($"{player.GetNameWithRole()} enchant {target.GetNameWithRole()}", "Ritualist");

        RitualLimit[player.PlayerId]--;

        EnchantedPlayers[player.PlayerId].Add(target.PlayerId);
        RPC.PlaySoundRPC(Sounds.TaskUpdateSound, target.PlayerId);
        target.ShowInfoMessage(target.IsModded(), string.Format(GetString("RitualistConvertNotif"), CustomRoles.Ritualist.ToColoredString()));
        // SendMessage(string.Format(GetString("RitualistConvertNotif"), CustomRoles.Ritualist.ToColoredString()), target.PlayerId);
        RPC.PlaySoundRPC(Sounds.TaskComplete, player.PlayerId);
        player.ShowInfoMessage(isUI, string.Format(GetString("RitualistRitualSuccess"), target.GetRealName()));
        // SendMessage(string.Format(GetString("RitualistRitualSuccess"), target.GetRealName()), player.PlayerId);
    }
    public override void AfterMeetingTasks()
    {
        foreach (var rit in EnchantedPlayers.Keys)
        {
            var ritualist = GetPlayerById(rit);
            foreach (var pc in EnchantedPlayers[rit])
            {
                ConvertRole(ritualist, GetPlayerById(pc));
            }
            EnchantedPlayers[rit].Clear();
        }
    }
    public static void ConvertRole(PlayerControl killer, PlayerControl target)
    {
        var addon = killer.GetBetrayalAddon(true);
        if (target.CanBeRecruitedBy(killer))
        {
            Logger.Info("Set converted: " + target.GetNameWithRole().RemoveHtmlTags() + " to " + addon.ToString(), "Ritualist Assign");
            target.RpcSetCustomRole(addon);
            if (addon is CustomRoles.Admired)
            {
                Admirer.AdmiredList[killer.PlayerId].Add(target.PlayerId);
                Admirer.SendRPC(killer.PlayerId, target.PlayerId);
            }
        }
    }
    private static bool GetTargetAndRoleFromCommand(string msg, out byte id, out CustomRoles role, out string error)
    {
        // Expected format: /ritual <player id> <role name>.
        // Parse the ID and role separately; passing the full command to GetRoleByName
        // includes the command alias and prevents the role name from matching.
        var parts = msg.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var idIndex = parts.Length > 0 && parts[0].StartsWith("/")
            ? 1
            : 0;

        if (parts.Length <= idIndex ||
            !byte.TryParse(parts[idIndex], out id))
        {
            id = byte.MaxValue;
            role = new();
            error = GetString("RitualistCommandHelp");
            return false;
        }

        var target = GetPlayerById(id);
        if (target == null || target.Data == null || target.Data.IsDead || !target.IsAlive())
        {
            error = GetString("GuessNull");
            role = new();
            return false;
        }

        var roleName = string.Join(" ", parts.Skip(idIndex + 1));
        if (string.IsNullOrWhiteSpace(roleName) ||
            !ChatCommands.GetRoleByName(roleName, out role))
        {
            error = GetString("RitualistCommandHelp");
            role = new();
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static bool CanBeConverted(PlayerControl pc)
    {
        return pc != null && !(pc.GetCustomRole().IsCovenTeam() || pc.GetBetrayalAddon().IsCovenTeam()) && !pc.IsTransformedNeutralApocalypse() && !pc.Is(CustomRoles.Solsticer);
    }
}
