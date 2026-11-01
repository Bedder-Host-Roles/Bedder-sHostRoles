using AmongUs.Data;
using AmongUs.GameOptions;
using AmongUs.InnerNet.GameDataMessages;
using Hazel;
using InnerNet;
using System;
using System.Text.RegularExpressions;
using BHR.Modules;
using BHR.Modules.Rpc;
using BHR.Patches;
using BHR.Roles.AddOns.Common;
using BHR.Roles.Core.AssignManager;
using BHR.Roles.Crewmate;
using BHR.Roles.Neutral;
using BHR.Test;
using UnityEngine;
using static BHR.Translator;

namespace BHR;

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameJoined))]
internal static class OnGameJoinedPatch
{
    public static bool JoiningGame;

    public static void Postfix(AmongUsClient __instance)
    {
        JoiningGame = true;
        while (!Options.IsLoaded) System.Threading.Tasks.Task.Delay(1);
        Logger.Info($"{__instance.GameId} Joining room - Room code: {GameCode.IntToGameName(AmongUsClient.Instance.GameId) ?? string.Empty}", "OnGameJoined");

        Main.IsHostVersionCheating = false;
        Main.playerVersion = [];
        SoundManager.Instance.ChangeAmbienceVolume(DataManager.Settings.Audio.AmbienceVolume);

        Main.HostClientId = AmongUsClient.Instance.HostId;
        if (!DebugModeManager.AmDebugger && Main.VersionCheat.Value)
            Main.VersionCheat.Value = false;

        RpcUtils.queuedReliableMessage.Clear();
        RpcUtils.queuedUnreliableMessage.Clear();

        ChatUpdatePatch.DoBlockChat = false;
        Main.CurrentServerIsVanilla = GameStates.IsVanillaServer && !GameStates.IsLocalGame;
        GameStates.InGame = false;
        ErrorText.Instance.Clear();
        EAC.Init();
        Main.AllClientRealNames.Clear();
        FixedUpdateInNormalGamePatch.RoleTextCache.Clear();

        if (AmongUsClient.Instance.AmHost) // Execute the following only on the Host
        {
            EndGameManagerPatch.IsRestarting = false;
            if (!RehostManager.IsAutoRehostDone)
            {
                AmongUsClient.Instance.ChangeGamePublic(RehostManager.ShouldPublic);
                RehostManager.IsAutoRehostDone = true;
            }

            Main.HostRealName = DataManager.Player.Customization.Name;
            if (!Main.AllClientRealNames.ContainsKey(__instance.ClientId))
            {
                Main.AllClientRealNames.Add(__instance.ClientId, DataManager.Player.Customization.Name);
            }

            GameStartManagerPatch.GameStartManagerUpdatePatch.exitTimer = -1;
            Main.DoBlockNameChange = false;
            RoleAssign.SetRoles = [];
            GhostRoleAssign.forceRole = [];
            EAC.DeNum = new();
            Main.AllPlayerNames.Clear();
            Main.PlayerQuitTimes.Clear();
            KickPlayerPatch.AttemptedKickPlayerList = [];

            switch (GameOptionsManager.Instance.CurrentGameOptions.GameMode)
            {
                case GameModes.NormalFools:
                case GameModes.Normal:
                    Logger.Info(" Is Normal Game", "Game Mode");

                    if (Main.NormalOptions.KillCooldown == 0f)
                        Main.NormalOptions.KillCooldown = Main.LastKillCooldown.Value;

                    AURoleOptions.SetOpt(Main.NormalOptions.CastFast<IGameOptions>());

                    if (AURoleOptions.ShapeshifterCooldown == 0f)
                        AURoleOptions.ShapeshifterCooldown = Main.LastShapeshifterCooldown.Value;

                    if (AURoleOptions.GuardianAngelCooldown == 0f)
                        AURoleOptions.GuardianAngelCooldown = Main.LastGuardianAngelCooldown.Value;

                    // If custom Gamemode is HideNSeekBHR in normal game, set Standard
                    if (Options.CurrentGameMode == CustomGameMode.HidenSeekBHR)
                    {
                        // Select Standard
                        Options.GameMode.SetValue(0);
                    }
                    break;

                case GameModes.SeekFools:
                case GameModes.HideNSeek:
                    Logger.Info(" Is Hide & Seek", "Game Mode");

                    // If custom Gamemode is Standard/FFA/Speedrun in H&S game, set HideNSeekBHR
                    if (Options.CurrentGameMode != CustomGameMode.HidenSeekBHR)
                    {
                        // Select HideNSeekBHR
                        Options.GameMode.SetValue(3);
                    }
                    break;

                case GameModes.None:
                    Logger.Info(" Is None", "Game Mode");
                    break;

                default:
                    Logger.Info(" No found", "Game Mode");
                    break;
            }

            _ = new LateTask(() =>
            {
                JoiningGame = false;
            }, 1f, "OnGameJoinedPatch");
        }

        _ = new LateTask(() =>
        {
            try
            {
                if (!GameStates.IsOnlineGame) return;
                if (!GameStates.IsModHost)
                    RPC.RpcRequestRetryVersionCheck();
                if (BanManager.CheckEACList(EOSManager.Instance.FriendCode, BanManager.GetHashedPuid(EOSManager.Instance.ProductUserId)) && GameStates.IsOnlineGame)
                {
                    AmongUsClient.Instance.ExitGame(DisconnectReasons.Banned);
                    SceneChanger.ChangeScene("MainMenu");
                    return;
                }

                var client = AmongUsClient.Instance.GetClientFromCharacter(PlayerControl.LocalPlayer);
                var host = AmongUsClient.Instance.GetHost();

                // if (!GameStates.IsVanillaServer)
                // {
                //     RPC.RpcSetFriendCode(EOSManager.Instance.FriendCode);
                // }

                Logger.Info($"{client.PlayerName.RemoveHtmlTags()}(ClientID:{client.Id}/FriendCode:{client.FriendCode}/HashPuid:{client.GetHashedPuid()}/Platform:{client.PlatformData.Platform}) finished join room", "Session: OnGameJoined");
                Logger.Info($"{host.PlayerName.RemoveHtmlTags()}(ClientID:{host.Id}/FriendCode:{host.FriendCode}/HashPuid:{host.GetHashedPuid()}/Platform:{host.PlatformData.Platform}) is the host", "Session: OnGameJoined");
            }
            catch
            {
                Logger.Error("Error while trying to log local client data.", "OnGameJoinedPatch");
                _ = new LateTask(() =>
                {
                    try
                    {
                        if (!GameStates.IsOnlineGame && !GameStates.IsLocalGame) return;
                        var client = AmongUsClient.Instance.GetClientFromCharacter(PlayerControl.LocalPlayer);
                        var host = AmongUsClient.Instance.GetHost();
                        Logger.Info($"{client.PlayerName.RemoveHtmlTags()}(ClientID:{client.Id}/FriendCode:{client.FriendCode}/HashPuid:{client.GetHashedPuid()}/Platform:{client.PlatformData.Platform}) finished join room", "Session: OnGameJoined Retry");
                        Logger.Info($"{host.PlayerName.RemoveHtmlTags()}(ClientID:{host.Id}/FriendCode:{host.FriendCode}/HashPuid:{host.GetHashedPuid()}/Platform:{host.PlatformData.Platform}) is the host", "Session: OnGameJoined Retry");
                    }
                    catch { }
                    ;
                }, 1.5f, "Retry Log Local Client");
            }
        }, 0.7f, "OnGameJoinedPatch");
    }
}
[HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.DisconnectInternal))]
class DisconnectInternalPatch
{
    public static void Prefix(InnerNetClient __instance, DisconnectReasons reason, string stringReason)
    {
        GameStates.InGame = false;
        Logger.Info($"Disconnect (Reason:{reason}:{stringReason}, ping:{__instance.Ping})", "Reason Disconnect");
        RehostManager.OnDisconnectInternal(reason);

        if (reason == DisconnectReasons.Kicked)
        {
            ExperimentManager.OnKicked();
        }
        else if (reason == DisconnectReasons.ClientTimeout)
        {
            ExperimentManager.OnTimedOut();
        }
        else
        {
            ExperimentManager.OnDisconnected();
        }
    }
}
[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerJoined))]
public static class OnPlayerJoinedPatch
{
    public static bool IsDisconnected(this ClientData client)
    {
        foreach (ClientData clientData in AmongUsClient.Instance.allClients)
        {
            if (clientData.Id == client.Id)
                return false;
        }

        return true;
    }
    public static bool HasInvalidFriendCode(string friendcode)
    {
        if (string.IsNullOrEmpty(friendcode))
        {
            return true;
        }

        if (friendcode.Length < 7) // #1234 is 5 chars, and its impossible for a friend code to only have 3
        {
            return true;
        }

        if (friendcode.Count(c => c == '#') != 1)
        {
            return true;
        }

        string pattern = @"[\W\d]";
        if (Regex.IsMatch(friendcode[..friendcode.IndexOf("#")], pattern))
        {
            return true;
        }

        return false;
    }
    public static void Postfix(/*AmongUsClient __instance,*/ [HarmonyArgument(0)] ClientData client)
    {
        Logger.Info($"{client.PlayerName}(ClientID:{client.Id}/FriendCode:{client.FriendCode}/HashPuid:{client.GetHashedPuid()}/Platform:{client.PlatformData.Platform}) Joining room", "Session: OnPlayerJoined");

        Main.AssignRolesIsStarted = false;

        _ = new LateTask(() =>
        {
            try
            {
                if (!AmongUsClient.Instance.AmHost) return;
                if ((!client.IsDisconnected() && client.Character.Data.IsIncomplete) || ((client.Character.Data.DefaultOutfit.ColorId < 0 || Palette.PlayerColors.Length <= client.Character.Data.DefaultOutfit.ColorId) && Main.AllPlayerControls.Count <= 15))
                {
                    Logger.SendInGame(GetString("Error.InvalidColor") + $" {client.Id}/{client.PlayerName}", Color.yellow);
                    AmongUsClient.Instance.KickPlayer(client.Id, false);
                    Logger.Info($"Kicked client {client.Id}/{client.PlayerName} since its PlayerControl was not spawned in time.", "OnPlayerJoinedPatchPostfix");
                    return;
                }

                if (client.Character != null && client.Character.Data != null && (client.Character.Data.DefaultOutfit.ColorId < 0 || Palette.PlayerColors.Length <= client.Character.Data.DefaultOutfit.ColorId) && Main.AllPlayerControls.Count >= 17)
                    Rainbow.ChangeColor(client.Character);
            }
            catch { }
        }, 4.5f, "Green Bean Kick LateTask", false);


        if (AmongUsClient.Instance.AmHost && HasInvalidFriendCode(client.FriendCode) && Options.KickPlayerFriendCodeInvalid.GetBool() && !GameStates.IsLocalGame)
        {
            if (!Options.TempBanPlayerFriendCodeInvalid.GetBool())
            {
                AmongUsClient.Instance.KickPlayer(client.Id, false);
                Logger.SendInGame(string.Format(GetString("Message.KickedByInvalidFriendCode"), client.PlayerName));
                Logger.Info($"Kicked a player {client?.PlayerName} because of invalid friend code", "Kick");
            }
            else
            {
                if (!BanManager.TempBanWhiteList.Contains(client.GetHashedPuid()))
                    BanManager.TempBanWhiteList.Add(client.GetHashedPuid());
                AmongUsClient.Instance.KickPlayer(client.Id, true);
                Logger.SendInGame(string.Format(GetString("Message.TempBannedByInvalidFriendCode"), client.PlayerName));
                Logger.Info($"TempBanned a player {client?.PlayerName} because of invalid friend code", "Temp Ban");
            }
        }

        if (AmongUsClient.Instance.AmHost && Options.AllowOnlyWhiteList.GetBool() && !BanManager.CheckAllowList(client?.FriendCode) && !GameStates.IsLocalGame)
        {
            AmongUsClient.Instance.KickPlayer(client.Id, false);
            Logger.SendInGame(string.Format(GetString("Message.KickedByWhiteList"), client.PlayerName));
            Logger.Warn($"Kicked player {client?.PlayerName}, because friendcode: {client?.FriendCode} is not in WhiteList.txt", "Kick");
        }

        Platforms platform = client.PlatformData.Platform;
        if (AmongUsClient.Instance.AmHost && Options.KickOtherPlatformPlayer.GetBool() && platform != Platforms.Unknown && !GameStates.IsLocalGame)
        {
            if ((platform == Platforms.Android && Options.OptKickAndroidPlayer.GetBool()) ||
                (platform == Platforms.IPhone && Options.OptKickIphonePlayer.GetBool()) ||
                (platform == Platforms.Xbox && Options.OptKickXboxPlayer.GetBool()) ||
                (platform == Platforms.Playstation && Options.OptKickPlayStationPlayer.GetBool()) ||
                (platform == Platforms.Switch && Options.OptKickNintendoPlayer.GetBool()))
            {
                if (Options.WhiteListNoKick.GetBool() && BanManager.CheckAllowList(client?.FriendCode) && !GameStates.IsLocalGame)
                {
                    Logger.SendInGame(string.Format(GetString("MsgWhiteListNoKick"), client?.PlayerName));
                    Logger.Info($"{client?.PlayerName} should be kicked because platform, but is in whitelist so they will not be kicked", "Other Platform Kick");
                }
                else
                {
                    string msg = string.Format(GetString("MsgKickOtherPlatformPlayer"), client?.PlayerName, platform.ToString());
                    AmongUsClient.Instance.KickPlayer(client.Id, false);
                    Logger.SendInGame(msg);
                    Logger.Info(msg, "Other Platform Kick");
                }
            }
        }
        if (DestroyableSingleton<FriendsListManager>.Instance.IsPlayerBlockedUsername(client.FriendCode) && AmongUsClient.Instance.AmHost)
        {
            AmongUsClient.Instance.KickPlayer(client.Id, true);
            Logger.Info($"Ban Player => {client?.PlayerName}({client.FriendCode}) has been banned.", "BAN");
        }
        BanManager.CheckBanPlayer(client);

        if (AmongUsClient.Instance.AmHost)
        {
            if (Main.SayStartTimes.ContainsKey(client.Id)) Main.SayStartTimes.Remove(client.Id);
            if (Main.SayBanwordsTimes.ContainsKey(client.Id)) Main.SayBanwordsTimes.Remove(client.Id);
            //if (Main.newLobby && Options.ShareLobby.GetBool()) Cloud.ShareLobby();

            if (client.GetHashedPuid() != "" && Options.TempBanPlayersWhoKeepQuitting.GetBool()
                && !BanManager.CheckAllowList(client.FriendCode) && !GameStates.IsLocalGame)
            {
                if (Main.PlayerQuitTimes.ContainsKey(client.GetHashedPuid()))
                {
                    if (Main.PlayerQuitTimes[client.GetHashedPuid()] >= Options.QuitTimesTillTempBan.GetInt())
                    {
                        if (!BanManager.TempBanWhiteList.Contains(client.GetHashedPuid()))
                            BanManager.TempBanWhiteList.Add(client.GetHashedPuid());
                        AmongUsClient.Instance.KickPlayer(client.Id, true);
                        Logger.SendInGame(string.Format(GetString("Message.TempBannedForSpamQuitting"), client.PlayerName));
                        Logger.Info($"Temp Ban Player => {client?.PlayerName}({client.FriendCode}) has been temp banned.", "BAN");
                    }
                }
            }
        }
    }
}
[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerLeft))]
class OnPlayerLeftPatch
{
    public static byte LeftPlayerId = byte.MaxValue;
    public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ClientData data, [HarmonyArgument(1)] DisconnectReasons reason)
    {
        try
        {
            if (AmongUsClient.Instance.AmHost && GameStates.IsInGame && data != null && data.Character != null)
            {
                if (data.Character.Is(CustomRoles.Lovers) && data.Character.IsAlive())
                {
                    Lovers.OnPartnerLeft(data.Character.PlayerId);
                }

                Spiritualist.RemoveTarget(data.Character.PlayerId);

                PlayerState state = Main.PlayerStates[data.Character.PlayerId];
                if (state.deathReason == PlayerState.DeathReason.etc) state.deathReason = PlayerState.DeathReason.Disconnected;

                if (!state.IsDead) state.SetDead();

                // Utils.AfterPlayerDeathTasks(data.Character, GameStates.IsMeeting, true);

                NameNotifyManager.Notifies.Remove(data.Character.PlayerId);
                data.Character.RpcSetName(data.Character.GetRealName(true));
                PlayerGameOptionsSender.RemoveSender(data.Character);
            }

            // Additional description of the reason for disconnection
            switch (reason)
            {
                case DisconnectReasons.Hacking:
                    Logger.SendInGame(string.Format(GetString("PlayerLeftByAU-Anticheat"), data?.PlayerName), Color.yellow);
                    break;
            }

            Logger.Info($"{data?.PlayerName} - (ClientID: {data?.Id} / FriendCode: {data?.FriendCode}) - Disconnected: {reason}, Ping: ({AmongUsClient.Instance.Ping})", "Session");

            if (AmongUsClient.Instance.AmHost)
            {
                Main.SayStartTimes.Remove(__instance.ClientId);
                Main.SayBanwordsTimes.Remove(__instance.ClientId);
                Main.playerVersion.Remove(data?.Character?.PlayerId ?? byte.MaxValue);

                if (data != null && data.Character != null)
                {
                    uint netid = data.Character.NetId;

                    LateTask.New(() =>
                    {
                        if (GameStates.IsOnlineGame)
                        {
                            var message = new DespawnGameDataMessage(netid);
                            AmongUsClient.Instance.LateBroadcastReliableMessage(message.CastFast<IGameDataMessage>());
                        }

                    }, 2.5f, "Repeat Despawn", false);
                }
            }

            Utils.CountAlivePlayers(true);
        }
        catch (NullReferenceException) { }
        catch (Exception ex) { Logger.Error(ex.ToString(), "OnPlayerLeftPatch.Postfix"); }
        finally
        {
            if (!GameStates.IsLobby && GameStates.IsInTask && !ExileController.Instance)
                Utils.NotifyRoles(ForceLoop: true);
        }
    }
}
[HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.Spawn))]
class InnerNetClientSpawnPatch
{
    public static void Postfix([HarmonyArgument(1)] int ownerId, [HarmonyArgument(2)] SpawnFlags flags)
    {
        if (!AmongUsClient.Instance.AmHost || flags != SpawnFlags.IsClientCharacter) return;

        ClientData client = Utils.GetClientById(ownerId);

        Logger.Msg($"Spawn player data: ID {client?.Character?.PlayerId}: {client?.PlayerName}", "CreatePlayer");

        if (client == null || client.Character == null // client is null
                           || client.ColorId < 0 || Palette.PlayerColors.Length <= client.ColorId) // invalid client color
            Logger.Warn("client is null or client have invalid color", "TrySyncAndSendMessage");
        else
        {
            LateTask.New(() => OptionItem.SyncAllOptions(client.Id), 3f, "Sync All Options For New Player");

            // The official server disconnects the host (Hacking) ~15s after lobby creation unless a SetName RPC has been sent.
            if (client.Character.AmOwner && GameStates.IsOnlineGame)
            {
                LateTask.New(() =>
                {
                    var host = PlayerControl.LocalPlayer;
                    if (host == null || !GameStates.IsLobby) return;
                    Logger.Info($"Sending host SetName: {host.Data.PlayerName}", "HostSetName");
                    host.RpcSetName(host.Data.PlayerName);
                }, 3.5f, "Host SetName");
            }

            LateTask.New(() =>
            {
                if (Main.OverrideWelcomeMsg != "")
                    Utils.SendMessage(Main.OverrideWelcomeMsg, client.Character.PlayerId, sendOption: SendOption.None);
                else
                    TemplateManager.SendTemplate("welcome", client.Character.PlayerId, true, sendOption: SendOption.None);
            }, 3f, "Welcome Message");

            LateTask.New(() =>
            {
                if (client == null || client.Character == null)
                {
                    Logger.Warn("client is null", "Spawn.RPCRequestRetryVersionCheck");
                    return;
                }

                LegacyRpcSenders.SendRequestRetryVersionCheck(PlayerControl.LocalPlayer.NetId, client.Id);
            }, 3f, "RPC Request Retry Version Check");

            if (GameStates.IsOnlineGame && !client.Character.IsHost())
            {
                LateTask.New(() =>
                {
                    if (GameStates.IsLobby && client.Character != null && LobbyBehaviour.Instance != null && GameStates.IsVanillaServer)
                    {
                        // Only for vanilla
                        if (!client.Character.IsModded())
                        {
                            // MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(LobbyBehaviour.Instance.NetId, (byte)RpcCalls.LobbyTimeExpiring, SendOption.Reliable, client.Id);
                            // writer.WritePacked((int)GameStartManagerPatch.timer);
                            // writer.Write(false);
                            // AmongUsClient.Instance.FinishRpcImmediately(writer);
                        }
                        // Non-host modded client
                        else
                        {
                            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncLobbyTimer, SendOption.Reliable, client.Id);
                            writer.Write((int)GameStartManagerPatch.timer);
                            AmongUsClient.Instance.FinishRpcImmediately(writer);
                        }
                    }
                }, IRandom.Instance.Next(7, 13), "Sync Lobby Timer RPC");
            }
        }

        if (client != null && client.Character != null) Main.GuessNumber[client.Character.PlayerId] = [-1, 7];

        if (Main.OverrideWelcomeMsg == string.Empty && Main.PlayerStates.Count > 0 && client != null && Main.clientIdList.Contains(client.Id))
        {
            if (Options.AutoDisplayKillLog.GetBool())
            {
                LateTask.New(() =>
                {
                    if (!AmongUsClient.Instance.IsGameStarted && client.Character != null)
                    {
                        Main.isChatCommand = true;
                        Utils.ShowKillLog(client.Character.PlayerId);
                    }
                }, 1f, "DisplayKillLog");
            }

            // if (Options.AutoDisplayLastAddOns.GetBool())
            // {
            //     _ = new LateTask(() =>
            //     {
            //         if (!AmongUsClient.Instance.IsGameStarted && client.Character != null)
            //         {
            //             Main.isChatCommand = true;
            //             Utils.ShowLastAddOns(client.Character.PlayerId);
            //         }
            //     }, 1.1f, "DisplayLastAddOns");
            // }

            if (Options.AutoDisplayLastRoles.GetBool())
            {
                _ = new LateTask(() =>
                {
                    if (!AmongUsClient.Instance.IsGameStarted && client.Character != null)
                    {
                        Main.isChatCommand = true;
                        Utils.ShowLastRoles(client.Character.PlayerId);
                    }
                }, 1.2f, "DisplayLastRoles");
            }

            if (Options.AutoDisplayLastResult.GetBool())
            {
                _ = new LateTask(() =>
                {
                    if (!AmongUsClient.Instance.IsGameStarted && client.Character != null)
                    {
                        Main.isChatCommand = true;
                        Utils.ShowLastResult(client.Character.PlayerId);
                    }
                }, 1.3f, "DisplayLastResult");
            }

            // if (PlayerControl.LocalPlayer.FriendCode.GetDevUser().Up && Options.EnableUpMode.GetBool())
            // {
            //     LateTask.New(() =>
            //     {
            //         if (!AmongUsClient.Instance.IsGameStarted && client.Character != null)
            //         {
            //             Main.IsChatCommand = true;
            //             Utils.SendMessage($"{GetString("Message.YTPlanNotice")} {PlayerControl.LocalPlayer.FriendCode.GetDevUser().UpName}", client.Character.PlayerId);
            //         }
            //     }, 1.4f, "DisplayUpWarnning");
            // }
        }
    }
}

