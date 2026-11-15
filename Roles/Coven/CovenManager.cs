using AmongUs.GameOptions;
using Hazel;
using BHR.Modules.Rpc;
using static BHR.Options;
using static BHR.Translator;
using static BHR.Utils;

namespace BHR;
public abstract class CovenManager : RoleBase // NO, THIS IS NOT A ROLE
{
    public static byte necroHolder = byte.MaxValue;
    [Obfuscation(Exclude = true)]
    public enum VisOptionList
    {
        On,
        CovenPerRole
    }
    [Obfuscation(Exclude = true)]
    public enum VentOptionList
    {
        On,
        CovenPerRole
    }

    private static readonly Dictionary<CustomRoles, OptionItem> CovenImpVisOptions = [];
    private static readonly Dictionary<CustomRoles, OptionItem> CovenVentOptions = [];

    public static readonly Dictionary<byte, byte> necroVotes = [];
    public static void RunSetUpImpVisOptions(int Id)
    {
        foreach (var cov in CustomRolesHelper.AllRoles.Where(x => x.IsCoven()).ToArray())
        {
            SetUpImpVisOption(cov, Id, true, CovenImpVisMode);
            Id++;
        }
    }
    public static void RunSetUpVentOptions(int Id)
    {
        foreach (var cov in CustomRolesHelper.AllRoles.Where(x => x.IsCoven()).ToArray())
        {
            SetUpVentOption(cov, Id, true, CovenVentMode);
            Id++;
        }
    }
    private static void SetUpImpVisOption(CustomRoles role, int Id, bool defaultValue = true, OptionItem parent = null)
    {
        var roleName = GetRoleName(role);
        Dictionary<string, string> replacementDic = new() { { "%role%", ColorString(GetRoleColor(role), roleName) } };
        CovenImpVisOptions[role] = BooleanOptionItem.Create(Id, "%role%HasImpVis", defaultValue, TabGroup.CovenRoles, false).SetParent(parent);
        CovenImpVisOptions[role].ReplacementDictionary = replacementDic;
    }
    private static void SetUpVentOption(CustomRoles role, int Id, bool defaultValue = true, OptionItem parent = null)
    {
        var roleName = GetRoleName(role);
        Dictionary<string, string> replacementDic = new() { { "%role%", ColorString(GetRoleColor(role), roleName) } };
        CovenVentOptions[role] = BooleanOptionItem.Create(Id, "%role%CanVent", defaultValue, TabGroup.CovenRoles, false).SetParent(parent);
        CovenVentOptions[role].ReplacementDictionary = replacementDic;
    }
    private static void SendRPC(byte playerId)
    {
        RoleRpcs.SendNecronomicon(PlayerControl.LocalPlayer.NetId, playerId);
    }
    public static void ReceiveNecroRPC(MessageReader reader)
    {
        byte NecroId = reader.ReadByte();
        necroHolder = NecroId;
        Logger.Info($"{necroHolder.GetPlayer()?.GetRealName()} now has Necronomicon", "ReceiveNecroRPC");
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        if (!CovenHasImpVis.GetBool())
            opt.SetVision(false);
        else if (CovenImpVisMode.GetValue() == 0)
            opt.SetVision(true);
        else
        {
            CovenImpVisOptions.TryGetValue(GetPlayerById(playerId).GetCustomRole(), out var option);
            opt.SetVision(option.GetBool());
        }
    }
    public override bool CanUseImpostorVentButton(PlayerControl pc)
    {
        if (!CovenCanVent.GetBool())
            return false;
        else if (CovenVentMode.GetValue() == 0)
            return true;
        else
        {
            CovenVentOptions.TryGetValue(pc.GetCustomRole(), out var option);
            return option.GetBool();
        }
    }
    // Only living, actual Coven members may hold or receive the Necronomicon.
    // Keep this check shared by initial assignment, meeting votes and death hand-off.
    public static bool CanReceiveNecronomicon(PlayerControl player)
        => player != null
            && player.Data != null
            && player.IsAlive()
            && !player.Data.Disconnected
            && player.IsPlayerCoven()
            && !player.Is(CustomRoles.Summoned)
            && Main.PlayerStates.TryGetValue(player.PlayerId, out var state)
            && !state.IsRandomizer;

    public static void GiveNecronomicon()
    {
        var candidates = Main.EnumerateAlivePlayerControls()
            .Where(CanReceiveNecronomicon)
            .ToList();

        if (candidates.Count == 0)
        {
            necroHolder = byte.MaxValue;
            Logger.Warn("No eligible living Coven player is available for the Necronomicon.", "Coven");
            return;
        }

        GiveNecronomicon(candidates.RandomElement());
    }

    public static void GiveNecronomicon(byte target)
    {
        GiveNecronomicon(GetPlayerById(target));
    }

    public static void GiveNecronomicon(PlayerControl target)
    {
        if (!CanReceiveNecronomicon(target))
        {
            Logger.Warn($"Rejected Necronomicon transfer to invalid player id {target?.PlayerId.ToString() ?? "null"}.", "Coven");
            // If the current holder has become invalid, immediately choose a valid replacement.
            var currentHolder = GetPlayerById(necroHolder);
            if (!CanReceiveNecronomicon(currentHolder))
                GiveNecronomicon();
            return;
        }

        if (necroHolder == target.PlayerId)
            return;

        necroHolder = target.PlayerId;
        target.Notify(GetString("NecronomiconNotification"));
        SendRPC(necroHolder);
        Logger.Info($"Necronomicon transferred to {target.GetRealName()} ({target.PlayerId}).", "Coven");
    }

    public static void CheckNecroVotes()
    {
        if (necroVotes.Count == 0) return;

        try
        {
            // Discard votes for players who died, disconnected, changed teams, or cannot hold it.
            var validVotes = necroVotes
                .Where(vote => CanReceiveNecronomicon(GetPlayerById(vote.Key))
                    && CanReceiveNecronomicon(GetPlayerById(vote.Value)))
                .GroupBy(vote => vote.Value)
                .ToDictionary(group => group.Key, group => group.Count());

            if (validVotes.Count == 0)
            {
                Logger.Info("No valid Necronomicon votes were cast; keeping the current holder.", "Coven");
                return;
            }

            int highestVoteCount = validVotes.Values.Max();
            var winners = validVotes
                .Where(vote => vote.Value == highestVoteCount)
                .Select(vote => vote.Key)
                .ToList();

            // A tied vote does not arbitrarily move the Necronomicon.
            if (winners.Count != 1)
            {
                Logger.Info($"Necronomicon vote tied between {string.Join(", ", winners)}; keeping the current holder.", "Coven");
                return;
            }

            GiveNecronomicon(winners[0]);
        }
        finally
        {
            necroVotes.Clear();
        }
    }

    public static void NecronomiconCheck()
    {
        var holder = GetPlayerById(necroHolder);
        if (!CanReceiveNecronomicon(holder))
            GiveNecronomicon();
    }
    public static bool HasNecronomicon(PlayerControl pc) => necroHolder == pc.PlayerId;
    public static bool HasNecronomicon(byte playerId) => necroHolder == playerId;

}
