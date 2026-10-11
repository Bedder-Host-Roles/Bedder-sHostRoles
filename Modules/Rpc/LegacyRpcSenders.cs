using AmongUs.GameOptions;
using AmongUs.InnerNet.GameDataMessages;
using AmongUs.QuickChat;
using Hazel;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Linq;
using static BHR.Options;
using static BHR.Translator;

namespace BHR.Modules.Rpc;

public static class LegacyRpcSenders
{
    private static CustomRpcSender StartCustom(string name, uint netId, CustomRPC rpcType, int targetClientId = -1)
    {
        var sender = CustomRpcSender.Create(name, SendOption.Reliable);
        sender.AutoStartRpc(netId, (byte)rpcType, targetClientId);
        return sender;
    }

    private static CustomRpcSender StartVanilla(string name, uint netId, RpcCalls rpcCall, int targetClientId = -1)
    {
        var sender = CustomRpcSender.Create(name, SendOption.Reliable);
        sender.AutoStartRpc(netId, rpcCall, targetClientId);
        return sender;
    }

    private static void EndAndSend(CustomRpcSender sender)
    {
        sender.EndRpc();
        sender.SendMessage();
    }

    public static void SendSyncSpeed(uint netId, byte playerId, float speed)
    {
        var sender = StartCustom(nameof(CustomRPC.SyncSpeedPlayer), netId, CustomRPC.SyncSpeedPlayer);
        sender.stream.Write(playerId);
        sender.stream.Write(speed);
        EndAndSend(sender);
    }

    public static void SendShowPopUp(uint netId, uint seerId, string message, string title)
    {
        var sender = StartCustom(nameof(CustomRPC.ShowPopUp), netId, CustomRPC.ShowPopUp);
        sender.stream.WritePacked(seerId);
        sender.stream.Write(message);
        sender.stream.Write(title);
        EndAndSend(sender);
    }

    public static void SendNotificationPopper(uint netId, int index, bool playSound)
    {
        var sender = StartCustom(nameof(CustomRPC.NotificationPopper), netId, CustomRPC.NotificationPopper);
        sender.stream.WritePacked(index);
        sender.stream.Write(playSound);
        EndAndSend(sender);
    }

    public static void SendAntiBlackout(uint netId, byte playerId, string reason, string sourceError)
    {
        var sender = StartCustom(nameof(CustomRPC.AntiBlackout), netId, CustomRPC.AntiBlackout);
        sender.stream.Write(playerId);
        sender.stream.Write(reason);
        sender.stream.Write(sourceError);
        EndAndSend(sender);
    }

    public static void SendArrow(uint netId, bool isTargetArrow, int index, byte playerId, byte? targetId, UnityEngine.Vector3? vector, int targetClientId = -1)
    {
        var sender = StartCustom(nameof(CustomRPC.Arrow), netId, CustomRPC.Arrow, targetClientId);
        sender.stream.Write(isTargetArrow);
        sender.stream.WritePacked(index);
        sender.stream.Write(playerId);

        if (isTargetArrow)
            sender.stream.Write(targetId ?? byte.MaxValue);
        else
            sender.stream.Write(vector.HasValue ? vector.Value : UnityEngine.Vector3.zero);

        EndAndSend(sender);
    }

    public static void SendSyncAllPlayerNames(uint netId)
    {
        var sender = StartCustom(nameof(CustomRPC.SyncAllPlayerNames), netId, CustomRPC.SyncAllPlayerNames);
        sender.stream.WritePacked(Main.AllPlayerNames.Count);
        foreach (var name in Main.AllPlayerNames)
        {
            sender.stream.Write(name.Key);
            sender.stream.Write(name.Value);
        }

        sender.stream.WritePacked(Main.AllClientRealNames.Count);
        foreach (var name in Main.AllClientRealNames)
        {
            sender.stream.Write(name.Key);
            sender.stream.Write(name.Value);
        }

        EndAndSend(sender);
    }

    public static void SendNameColorData(uint netId, byte playerId, byte targetId, string colorCode)
    {
        var sender = StartCustom(nameof(CustomRPC.SetNameColorData), netId, CustomRPC.SetNameColorData);
        sender.stream.Write(playerId);
        sender.stream.Write(targetId);
        sender.stream.Write(colorCode);
        EndAndSend(sender);
    }

    public static void SendShowChat(uint netId, int ownerId)
    {
        var sender = StartCustom(nameof(CustomRPC.ShowChat), netId, CustomRPC.ShowChat);
        sender.stream.WritePacked(ownerId);
        sender.stream.Write(true);
        EndAndSend(sender);
    }

    private static Il2CppStructArray<byte> BuildGameOptionsBytes(PlayerControl player)
    {
        var optionSender = PlayerGameOptionsSender.AllSenders.OfType<PlayerGameOptionsSender>()
            .FirstOrDefault(x => x.player.PlayerId == player.PlayerId);

        var options = optionSender == null
            ? GameManager.Instance.LogicOptions.currentGameOptions
            : optionSender.BuildGameOptions();

        return GameManager.Instance.LogicOptions.gameOptionsFactory.ToBytes(options, false);
    }

    private static void WriteOptionsDataFlag(MessageWriter stream, Il2CppStructArray<byte> optionArray)
    {
        byte logicOptionsIndex = (byte)GameManager.Instance.LogicComponents.IndexOf(GameManager.Instance.LogicOptions);

        stream.StartMessage((byte)GameDataTypes.DataFlag);
        stream.WritePacked(GameManager.Instance.NetId);
        stream.StartMessage(logicOptionsIndex);
        stream.WriteBytesAndSize(optionArray);
        stream.EndMessage();
        stream.EndMessage();
    }

    public static void SendGuardAndKill(PlayerControl player, PlayerControl target, int targetClientId)
    {
        var sender = CustomRpcSender.Create("RpcGuardAndKill", SendOption.Reliable);
        sender.StartMessage(targetClientId);

        WriteOptionsDataFlag(sender.stream, BuildGameOptionsBytes(player));

        sender.StartRpc(player.NetId, RpcCalls.MurderPlayer);
        sender.stream.WritePacked(target.NetId);
        sender.stream.Write((int)MurderResultFlags.FailedProtected);
        sender.EndRpc();

        sender.EndMessage();
        sender.SendMessage();
    }

    public static void SendGuardAndKillModded(PlayerControl player, PlayerControl target, float timer, int targetClientId)
    {
        var sender = CustomRpcSender.Create("RpcGuardAndKillModded", SendOption.Reliable);
        sender.AutoStartRpc(player.NetId, (byte)CustomRPC.PlayGuardAndKill, targetClientId)
            .WritePacked(target.NetId)
            .EndRpc();

        sender.AutoStartRpc(player.NetId, (byte)CustomRPC.SetKillTimer, targetClientId)
            .Write(timer)
            .EndRpc();

        sender.SendMessage();
    }

    public static void SendSyncGeneralOptions(uint netId, byte playerId, CustomRoles role, bool isDead, bool isDisconnected, PlayerState.DeathReason deathReason, float killCooldown, float speed)
    {
        var sender = StartCustom(nameof(CustomRPC.SyncGeneralOptions), netId, CustomRPC.SyncGeneralOptions);
        sender.stream.Write(playerId);
        sender.stream.WritePacked((int)role);
        sender.stream.Write(isDead);
        sender.stream.Write(isDisconnected);
        sender.stream.WritePacked((int)deathReason);
        sender.stream.Write(killCooldown);
        sender.stream.Write(speed);
        EndAndSend(sender);
    }

    private static byte GetNextSequenceId(uint netId, RpcCalls call)
    {
        if (AmongUsClient.Instance.allObjects.allObjectsFast.TryGetValue(netId, out var obj) && obj is PlayerControl player)
        {
            return (byte)(player.GetNextRpcSequenceId(call) + 10);
        }

        return 0;
    }

    public static void SendSetOutfit(uint netId, uint playerInfoNetId, NetworkedPlayerInfo.PlayerOutfit outfit, bool setName, bool setNamePlate)
    {
        var sender = CustomRpcSender.Create("RpcSetOutfit", SendOption.Reliable);

        sender.AutoStartRpc(netId, RpcCalls.SetColor)
            .Write(playerInfoNetId)
            .Write((byte)outfit.ColorId)
            .EndRpc();

        if (setName)
        {
            sender.AutoStartRpc(netId, RpcCalls.SetName)
                .Write(playerInfoNetId)
                .Write(outfit.PlayerName)
                .EndRpc();
        }

        sender.AutoStartRpc(netId, RpcCalls.SetHatStr)
            .Write(outfit.HatId)
            .Write(GetNextSequenceId(netId, RpcCalls.SetHatStr))
            .EndRpc();

        sender.AutoStartRpc(netId, RpcCalls.SetPetStr)
            .Write(outfit.PetId)
            .Write(GetNextSequenceId(netId, RpcCalls.SetPetStr))
            .EndRpc();

        sender.AutoStartRpc(netId, RpcCalls.SetSkinStr)
            .Write(outfit.SkinId)
            .Write(GetNextSequenceId(netId, RpcCalls.SetSkinStr))
            .EndRpc();

        if (setNamePlate)
        {
            sender.AutoStartRpc(netId, RpcCalls.SetNamePlateStr)
                .Write(outfit.NamePlateId)
                .Write(GetNextSequenceId(netId, RpcCalls.SetNamePlateStr))
                .EndRpc();
        }

        sender.AutoStartRpc(netId, RpcCalls.SetVisorStr)
            .Write(outfit.VisorId)
            .Write(GetNextSequenceId(netId, RpcCalls.SetVisorStr))
            .EndRpc();

        sender.SendMessage();
    }

    public static void SendSyncFFAPlayer(uint netId, byte playerId, int score)
    {
        var sender = StartCustom(nameof(CustomRPC.SyncFFAPlayer), netId, CustomRPC.SyncFFAPlayer);
        sender.stream.Write(playerId);
        sender.stream.Write(score);
        EndAndSend(sender);
    }

    public static void SendSyncFFANameNotify(uint netId, string name)
    {
        var sender = StartCustom(nameof(CustomRPC.SyncFFANameNotify), netId, CustomRPC.SyncFFANameNotify);
        sender.stream.Write(name);
        EndAndSend(sender);
    }

    public static void SendSyncSpeedRunStates(uint netId, MessageWriter writer, int targetClientId = -1)
    {
        var sender = StartCustom(nameof(CustomRPC.SyncSpeedRunStates), netId, CustomRPC.SyncSpeedRunStates, targetClientId);
        sender.stream.Write(writer, false);
        writer.Recycle();
        EndAndSend(sender);
    }

    public static void SendSyncDeadPassedMeetingList(uint netId, HashSet<byte> deadList)
    {
        var sender = StartCustom(nameof(CustomRPC.SyncDeadPassedMeetingList), netId, CustomRPC.SyncDeadPassedMeetingList);
        sender.stream.WritePacked(deadList.Count);
        foreach (var dead in deadList)
            sender.stream.Write(dead);
        EndAndSend(sender);
    }

    public static void SendSetDeathReason(uint netId, byte playerId, PlayerState.DeathReason deathReason)
    {
        var sender = StartCustom(nameof(CustomRPC.SetDeathReason), netId, CustomRPC.SetDeathReason);
        sender.stream.Write(playerId);
        sender.stream.Write((int)deathReason);
        EndAndSend(sender);
    }

    public static void SendSyncAbilityUseLimit(uint netId, byte playerId, float limit)
    {
        var sender = StartCustom(nameof(CustomRPC.SyncAbilityUseLimit), netId, CustomRPC.SyncAbilityUseLimit);
        sender.stream.Write(playerId);
        sender.stream.Write(limit);
        EndAndSend(sender);
    }

    public static void SendSetCustomRole(uint netId, byte playerId, CustomRoles role, int targetClientId = -1)
    {
        var sender = StartCustom(nameof(CustomRPC.SetCustomRole), netId, CustomRPC.SetCustomRole, targetClientId);
        sender.stream.Write(playerId);
        sender.stream.WritePacked((int)role);
        EndAndSend(sender);
    }

    public static void SendSyncPlayerSetting(uint netId, byte playerId, CustomRoles role, int targetClientId = -1)
    {
        var sender = StartCustom(nameof(CustomRPC.SyncPlayerSetting), netId, CustomRPC.SyncPlayerSetting, targetClientId);
        sender.stream.Write(playerId);
        sender.stream.WritePacked((int)role);
        EndAndSend(sender);
    }

    public static void SendProtectPlayer(uint netId, uint targetNetId, int colorId, int targetClientId = -1)
    {
        var sender = StartVanilla(nameof(RpcCalls.ProtectPlayer), netId, RpcCalls.ProtectPlayer, targetClientId);
        sender.stream.WritePacked(targetNetId);
        sender.stream.Write(colorId);
        EndAndSend(sender);
    }

    public static void SendPlaySound(uint netId, byte playerId, Sounds sound)
    {
        var sender = StartCustom(nameof(CustomRPC.PlaySound), netId, CustomRPC.PlaySound);
        sender.stream.Write(playerId);
        sender.stream.Write((byte)sound);
        EndAndSend(sender);
    }

    public static void SendSyncRoleSkill(uint netId, uint player, MessageWriter writer)
    {
        var sender = StartCustom(nameof(CustomRPC.SyncRoleSkill), netId, CustomRPC.SyncRoleSkill);
        sender.stream.WritePacked(player);
        sender.stream.Write(writer, false);
        writer.Recycle();
        EndAndSend(sender);
    }

    public static void SendCheckVanish(uint netId, int targetClientId = -1)
    {
        var sender = StartVanilla(nameof(RpcCalls.CheckVanish), netId, RpcCalls.CheckVanish, targetClientId);
        sender.stream.Write(0f);
        EndAndSend(sender);
    }

    public static void SendCheckAppear(uint netId, bool shouldAnimate, int targetClientId = -1)
    {
        var sender = StartVanilla(nameof(RpcCalls.CheckAppear), netId, RpcCalls.CheckAppear, targetClientId);
        sender.stream.Write(shouldAnimate);
        EndAndSend(sender);
    }

    public static void SendVanish(uint netId, int targetClientId = -1)
    {
        var sender = StartVanilla(nameof(RpcCalls.StartVanish), netId, RpcCalls.StartVanish, targetClientId);
        EndAndSend(sender);
    }

    public static void SendAppear(uint netId, bool shouldAnimate, int targetClientId = -1)
    {
        var sender = StartVanilla(nameof(RpcCalls.StartAppear), netId, RpcCalls.StartAppear, targetClientId);
        sender.stream.Write(shouldAnimate);
        EndAndSend(sender);
    }

    public static void SendRemoveSubRole(uint netId, byte playerId, CustomRoles addon)
    {
        var sender = StartCustom(nameof(CustomRPC.RemoveSubRole), netId, CustomRPC.RemoveSubRole);
        sender.stream.Write(playerId);
        sender.stream.Write((int)addon);
        EndAndSend(sender);
    }

    public static void SendSyncCustomSettingsSingle(uint netId, int id, int value)
    {
        var sender = StartCustom(nameof(CustomRPC.SyncCustomSettings), netId, CustomRPC.SyncCustomSettings);
        sender.stream.Write(true);
        sender.stream.WritePacked(id);
        sender.stream.WritePacked(value);
        EndAndSend(sender);
    }

    public static void SendSyncShieldPersonDiedFirst(uint netId, string firstDied, string firstDiedPrevious)
    {
        var sender = StartCustom(nameof(CustomRPC.SyncShieldPersonDiedFirst), netId, CustomRPC.SyncShieldPersonDiedFirst);
        sender.stream.Write(firstDied);
        sender.stream.Write(firstDiedPrevious);
        EndAndSend(sender);
    }

    public static void SendPlayCustomSound(uint netId, string soundName, float volume, float pitch, int targetClientId = -1)
    {
        var sender = StartCustom(nameof(CustomRPC.PlayCustomSound), netId, CustomRPC.PlayCustomSound, targetClientId);
        sender.stream.Write(soundName);
        sender.stream.Write(volume);
        sender.stream.Write(pitch);
        EndAndSend(sender);
    }

    public static void SendMurderPlayer(uint netId, uint targetNetId, MurderResultFlags flags, int targetClientId = -1)
    {
        var sender = StartVanilla(nameof(RpcCalls.MurderPlayer), netId, RpcCalls.MurderPlayer, targetClientId);
        sender.stream.WritePacked(targetNetId);
        sender.stream.Write((int)flags);
        EndAndSend(sender);
    }

    public static void SendExiled(uint netId, int targetClientId = -1)
    {
        var sender = StartVanilla(nameof(RpcCalls.Exiled), netId, RpcCalls.Exiled, targetClientId);
        EndAndSend(sender);
    }

    public static void SendQuickChatSpam()
    {
        var firstAlivePlayer = Main.EnumerateAlivePlayerControls().OrderBy(x => x.PlayerId).FirstOrDefault() ?? PlayerControl.LocalPlayer;
        var title = "<color=#aaaaff>" + GetString("DefaultSystemMessageTitle") + "</color>";
        var name = firstAlivePlayer?.Data?.PlayerName ?? "Error";

        firstAlivePlayer.Data.PlayerName = title;

        var sender = CustomRpcSender.Create("RpcQuickChatSpam", SendOption.Reliable);
        sender.StartMessage(-1);

        sender.StartRpc(firstAlivePlayer.NetId, RpcCalls.SetName);
        sender.stream.Write(firstAlivePlayer.Data.NetId);
        sender.stream.Write(title);
        sender.EndRpc();

        var quickChatSpamMode = (QuickChatSpamMode)UseQuickChatSpamCheat.GetInt();
        switch (quickChatSpamMode)
        {
            case QuickChatSpamMode.QuickChatSpam_Disabled:
                goto case QuickChatSpamMode.QuickChatSpam_Random20;
            case QuickChatSpamMode.QuickChatSpam_Random20:
            {
                var random = IRandom.Instance;
                var stringNamesValues = System.Enum.GetValues(typeof(StringNames)).Cast<StringNames>().ToArray();
                for (int i = 0; i < 21; i++)
                {
                    var randomString = stringNamesValues[random.Next(stringNamesValues.Length)];
                    var message = new RpcSendQuickChatMessage(firstAlivePlayer.NetId, new(QuickChatPhraseType.ComplexPhrase, randomString, 0, null));
                    message.Serialize(sender.stream);
                    DestroyableSingleton<HudManager>.Instance.Chat.AddChat(firstAlivePlayer, GetString(randomString), false);
                }
                break;
            }
            case QuickChatSpamMode.QuickChatSpam_How2PlayNormal:
            {
                foreach (var names in Main.how2playN)
                {
                    var message = new RpcSendQuickChatMessage(firstAlivePlayer.NetId, new(QuickChatPhraseType.ComplexPhrase, names, 0, null));
                    message.Serialize(sender.stream);
                    message.Serialize(sender.stream);
                    DestroyableSingleton<HudManager>.Instance.Chat.AddChat(firstAlivePlayer, GetString(names), false);
                    DestroyableSingleton<HudManager>.Instance.Chat.AddChat(firstAlivePlayer, GetString(names), false);
                }
                break;
            }
            case QuickChatSpamMode.QuickChatSpam_How2PlayHidenSeek:
            {
                foreach (var names in Main.how2playHnS)
                {
                    var message = new RpcSendQuickChatMessage(firstAlivePlayer.NetId, new(QuickChatPhraseType.ComplexPhrase, names, 0, null));
                    message.Serialize(sender.stream);
                    message.Serialize(sender.stream);
                    DestroyableSingleton<HudManager>.Instance.Chat.AddChat(firstAlivePlayer, GetString(names), false);
                    DestroyableSingleton<HudManager>.Instance.Chat.AddChat(firstAlivePlayer, GetString(names), false);
                }
                break;
            }
            case QuickChatSpamMode.QuickChatSpam_EzHacked:
            {
                foreach (var names in Main.how2playEzHacked)
                {
                    var message = new RpcSendQuickChatMessage(firstAlivePlayer.NetId, new(QuickChatPhraseType.ComplexPhrase, names, 0, null));
                    message.Serialize(sender.stream);
                    message.Serialize(sender.stream);
                    DestroyableSingleton<HudManager>.Instance.Chat.AddChat(firstAlivePlayer, GetString(names), false);
                    DestroyableSingleton<HudManager>.Instance.Chat.AddChat(firstAlivePlayer, GetString(names), false);
                }
                break;
            }
            case QuickChatSpamMode.QuickChatSpam_Empty:
            {
                var message = new RpcSendQuickChatMessage(firstAlivePlayer.NetId, new(QuickChatPhraseType.SimplePhrase, StringNames.None, 0, null));
                for (var i = 0; i < 21; i++)
                {
                    message.Serialize(sender.stream);
                    DestroyableSingleton<HudManager>.Instance.Chat.AddChat(firstAlivePlayer, GetString(StringNames.None), false);
                }
                break;
            }
        }

        firstAlivePlayer.Data.PlayerName = name;

        sender.StartRpc(firstAlivePlayer.NetId, RpcCalls.SetName);
        sender.stream.Write(firstAlivePlayer.Data.NetId);
        sender.stream.Write(name);
        sender.EndRpc();

        sender.EndMessage();
        sender.SendMessage();
    }

    public static void SendKillFlash(uint netId, uint seerId, bool doKillSound)
    {
        var sender = StartCustom(nameof(CustomRPC.KillFlash), netId, CustomRPC.KillFlash);
        sender.stream.WritePacked(seerId);
        sender.stream.Write(doKillSound);
        EndAndSend(sender);
    }

    public static void SendSetRealKiller(uint netId, byte playerId, byte killerId)
    {
        var sender = StartCustom(nameof(CustomRPC.SetRealKiller), netId, CustomRPC.SetRealKiller);
        sender.stream.Write(playerId);
        sender.stream.Write(killerId);
        EndAndSend(sender);
    }

    public static void SendRequestRetryVersionCheck(uint netId, int targetClientId = -1)
    {
        var sender = StartCustom(nameof(CustomRPC.RequestRetryVersionCheck), netId, CustomRPC.RequestRetryVersionCheck, targetClientId);
        EndAndSend(sender);
    }

    public static void SendEndGame(uint netId, CustomWinner winnerTeam, HashSet<AdditionalWinners> additionalWinnerTeams, HashSet<CustomRoles> winnerRoles, HashSet<byte> winnerIds)
    {
        var sender = StartCustom(nameof(CustomRPC.EndGame), netId, CustomRPC.EndGame);
        sender.stream.WritePacked((int)winnerTeam);

        sender.stream.WritePacked(additionalWinnerTeams.Count);
        foreach (var wt in additionalWinnerTeams)
            sender.stream.WritePacked((int)wt);

        sender.stream.WritePacked(winnerRoles.Count);
        foreach (var wr in winnerRoles)
            sender.stream.WritePacked((int)wr);

        sender.stream.WritePacked(winnerIds.Count);
        foreach (var id in winnerIds)
            sender.stream.Write(id);

        EndAndSend(sender);
    }

    public static void SendSetKillTimer(uint netId, float timer, int targetClientId = -1)
    {
        var sender = StartCustom(nameof(CustomRPC.SetKillTimer), netId, CustomRPC.SetKillTimer, targetClientId);
        sender.stream.Write(timer);
        EndAndSend(sender);
    }

    public static void SendSyncLobbyTimerVanilla(uint netId, int timer, bool flag, int targetClientId = -1)
    {
        var sender = StartVanilla(nameof(RpcCalls.LobbyTimeExpiring), netId, RpcCalls.LobbyTimeExpiring, targetClientId);
        sender.stream.WritePacked(timer);
        sender.stream.Write(flag);
        EndAndSend(sender);
    }

    public static void SendSyncLobbyTimerModded(uint netId, int timer, int targetClientId = -1)
    {
        var sender = StartCustom(nameof(CustomRPC.SyncLobbyTimer), netId, CustomRPC.SyncLobbyTimer, targetClientId);
        sender.stream.WritePacked(timer);
        EndAndSend(sender);
    }

    public static void SendSetRoleGrouped(List<(PlayerControl, RoleTypes)> playerRoles, int targetClientId)
    {
        if (playerRoles == null || playerRoles.Count == 0) return;

        var sender = CustomRpcSender.Create("RpcSetRoleGrouped", SendOption.Reliable);
        foreach (var (player, role) in playerRoles)
        {
            sender.AutoStartRpc(player.NetId, RpcCalls.SetRole, targetClientId)
                .Write((ushort)role)
                .Write(true)
                .EndRpc();
        }
        sender.SendMessage();
    }
}
