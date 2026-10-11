using AmongUs.Data;
using Discord;
using InnerNet;
using System;

namespace BHR.Patches
{
    // Originally from Town of Us Rewritten, by Det
    [HarmonyPatch(typeof(ActivityManager), nameof(ActivityManager.UpdateActivity))]
    public class DiscordRPC
    {
        private static string lobbycode = "";
        private static string region = "";
        public static void Prefix([HarmonyArgument(0)] Activity activity)
        {
            if (activity == null) return;

            var details = $"BHR v{Main.PluginDisplayVersion}";
            activity.Details = details;

            activity.Assets = new ActivityAssets
            {
                LargeImage = "",
                SmallImage = ""
            };

            try
            {
                if (activity.State != "In Menus")
                {
                    if (!DataManager.Settings.Gameplay.StreamerMode)
                    {
                        int maxSize = GameOptionsManager.Instance.CurrentGameOptions.MaxPlayers;
                        if (GameStates.IsLobby)
                        {
                            lobbycode = GameCode.IntToGameName(AmongUsClient.Instance.GameId);
                            region = Utils.GetRegionName();
                        }

                        if (lobbycode != "" && region != "")
                        {
                            if (GameStates.IsNormalGame)
                                details = $"BHR - {lobbycode} ({region})";

                            else if (GameStates.IsHideNSeek)
                                details = $"BHR Hide & Seek - {lobbycode} ({region})";
                        }

                        activity.Details = details;
                    }
                    else
                    {
                        if (GameStates.IsNormalGame)
                            details = $"BHR v{Main.PluginDisplayVersion}";

                        else if (GameStates.IsHideNSeek)
                            details = $"BHR v{Main.PluginDisplayVersion} - Hide & Seek";

                        else details = $"BHR v{Main.PluginDisplayVersion}";

                        activity.Details = details;
                    }
                }
            }

            catch (ArgumentException ex)
            {
                Logger.Error("Error in updating discord rpc", "DiscordPatch");
                Logger.Exception(ex, "DiscordPatch");
                details = $"BHR v{Main.PluginDisplayVersion}";
                activity.Details = details;
            }
        }
    }
}