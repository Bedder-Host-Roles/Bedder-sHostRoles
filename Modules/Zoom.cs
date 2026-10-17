using System;
using System.Collections.Generic;
using UnityEngine;

namespace BHR;

// Credit: https://github.com/tugaru1975/TownOfPlus/TOPmods/Zoom.cs
// Credit: https://github.com/Yumenopai/TownOfHost_Y
[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class Zoom
{
    private static bool ResetButtons = false;

    public static void Prefix()
    {
        // Do not run zoom logic while essential game objects are unavailable.
        if (PlayerControl.LocalPlayer == null ||
            Camera.main == null ||
            HudManager.Instance == null ||
            HudManager.Instance.UICamera == null)
        {
            return;
        }

        try
        {
            if ((GameStates.IsShip &&
                 !GameStates.IsMeeting &&
                 GameStates.IsCanMove &&
                 PlayerControl.LocalPlayer.Data != null &&
                 PlayerControl.LocalPlayer.Data.IsDead)
                || (GameStates.IsLobby && GameStates.IsCanMove))
            {
                if (Camera.main.orthographicSize > 3.0f)
                    ResetButtons = true;

                if (Input.mouseScrollDelta.y > 0 &&
                    Camera.main.orthographicSize > 3.0f)
                {
                    SetZoomSize(times: false);
                }

                if (Input.mouseScrollDelta.y < 0)
                {
                    if (GameStates.IsDead ||
                        GameStates.IsFreePlay ||
                        DebugModeManager.AmDebugger ||
                        GameStates.IsLobby ||
                        PlayerControl.LocalPlayer.FriendCode.GetDevUser().DeBug)
                    {
                        if (Camera.main.orthographicSize < 18.0f)
                            SetZoomSize(times: true);
                    }
                }

                Flag.NewFlag("Zoom");
            }
            else
            {
                Flag.Run(() =>
                {
                    SetZoomSize(reset: true);
                }, "Zoom");
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"Zoom update skipped during a game transition: {ex.Message}", "Zoom");
        }
    }

    private static void SetZoomSize(bool times = false, bool reset = false)
    {
        var camera = Camera.main;
        var hud = HudManager.Instance;

        if (camera == null || hud == null || hud.UICamera == null)
            return;

        var size = 1.5f;
        if (!times)
            size = 1f / size;

        try
        {
            if (reset)
            {
                camera.orthographicSize = 3.0f;
                hud.UICamera.orthographicSize = 3.0f;

                if (hud.Chat != null)
                    hud.Chat.transform.localScale = Vector3.one;

                if (GameStates.IsMeeting &&
                    MeetingHud.Instance != null)
                {
                    MeetingHud.Instance.transform.localScale = Vector3.one;
                }
            }
            else
            {
                camera.orthographicSize *= size;
                hud.UICamera.orthographicSize *= size;
            }

            var shadowQuad = DestroyableSingleton<HudManager>.Instance?.ShadowQuad;
            if (shadowQuad != null && PlayerControl.LocalPlayer != null)
            {
                shadowQuad.gameObject.SetActive(
                    (reset || Mathf.Approximately(camera.orthographicSize, 3.0f)) &&
                    PlayerControl.LocalPlayer.IsAlive());
            }

            if (ResetButtons)
            {
                if (Screen.height > 0)
                {
                    ResolutionManager.ResolutionChanged.Invoke(
                        (float)Screen.width / Screen.height,
                        Screen.width,
                        Screen.height,
                        Screen.fullScreen);
                }

                ResetButtons = false;
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"Zoom resize skipped: {ex.Message}", "Zoom");
        }
    }

    public static void OnFixedUpdate()
    {
        var camera = Camera.main;
        var player = PlayerControl.LocalPlayer;
        var shadowQuad = DestroyableSingleton<HudManager>.Instance?.ShadowQuad;

        if (camera == null || player == null || shadowQuad == null)
            return;

        shadowQuad.gameObject.SetActive(
            Mathf.Approximately(camera.orthographicSize, 3.0f) &&
            player.IsAlive());
    }
}

public static class Flag
{
    private static readonly List<string> OneTimeList = [];
    private static readonly List<string> FirstRunList = [];

    public static void Run(Action action, string type, bool firstrun = false)
    {
        if (OneTimeList.Contains(type) ||
            (firstrun && !FirstRunList.Contains(type)))
        {
            if (!FirstRunList.Contains(type))
                FirstRunList.Add(type);

            OneTimeList.Remove(type);
            action();
        }
    }

    public static void NewFlag(string type)
    {
        if (!OneTimeList.Contains(type))
            OneTimeList.Add(type);
    }

    public static void DeleteFlag(string type)
    {
        if (OneTimeList.Contains(type))
            OneTimeList.Remove(type);
    }
}
