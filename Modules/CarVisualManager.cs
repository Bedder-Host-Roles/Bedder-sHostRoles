using System.Collections.Generic;
using System.Linq;
using BHR.Roles.Crewmate;
using UnityEngine;

namespace BHR.Modules;

/// <summary>
/// Local-only visual replacement for the Car role. The real PlayerControl remains
/// untouched for movement, collisions, meetings and game logic.
/// </summary>
internal static class CarVisualManager
{
    private const string VisualName = "BHR_PocketCircuit_CarVisual";
    private const string SpriteResource = "BHR.Resources.Images.Pocket_Circuit.png";
    private const float PixelsPerUnit = 250f;

    private sealed class PlayerVisual
    {
        public GameObject Visual;
        public SpriteRenderer Renderer;
        public readonly Dictionary<SpriteRenderer, bool> OriginalRendererStates = new();
    }

    private static readonly Dictionary<byte, PlayerVisual> Visuals = new();
    private static Sprite CarSprite;

    public static void UpdatePlayer(PlayerControl player)
    {
        if (player == null || player.PlayerId >= 254) return;

        bool shouldShow = GameStates.IsInTask
            && !GameStates.IsEnded
            && player.IsAlive()
            && player.Is(CustomRoles.Car);

        if (!shouldShow)
        {
            Restore(player.PlayerId);
            return;
        }

        if (!Visuals.TryGetValue(player.PlayerId, out var visual) || visual.Visual == null)
        {
            visual = CreateVisual(player);
            if (visual == null) return;
            Visuals[player.PlayerId] = visual;
        }

        // Follow the real character locally. Do not alter PlayerControl or its collider.
        visual.Visual.transform.position = player.transform.position + new Vector3(0f, 0.02f, -0.01f);
        visual.Visual.transform.rotation = Quaternion.identity;
        visual.Renderer.enabled = true;
    }

    private static PlayerVisual CreateVisual(PlayerControl player)
    {
        if (CarSprite == null)
            CarSprite = Utils.LoadSprite(SpriteResource, PixelsPerUnit);
        if (CarSprite == null) return null;

        var visual = new PlayerVisual();
        SpriteRenderer bodyRenderer = null;
        foreach (var renderer in player.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (renderer == null || renderer.gameObject.name == VisualName) continue;
            if (bodyRenderer == null && renderer.enabled) bodyRenderer = renderer;
            visual.OriginalRendererStates[renderer] = renderer.enabled;
            renderer.enabled = false;
        }

        visual.Visual = new GameObject(VisualName);
        visual.Visual.transform.position = player.transform.position;
        visual.Renderer = visual.Visual.AddComponent<SpriteRenderer>();
        visual.Renderer.sprite = CarSprite;
        visual.Renderer.color = Color.white;
        if (bodyRenderer != null)
        {
            visual.Renderer.sortingLayerID = bodyRenderer.sortingLayerID;
            visual.Renderer.sortingOrder = bodyRenderer.sortingOrder;
        }
        else
        {
            visual.Renderer.sortingOrder = 1;
        }
        return visual;
    }

    private static void Restore(byte playerId)
    {
        if (!Visuals.TryGetValue(playerId, out var visual)) return;

        foreach (var pair in visual.OriginalRendererStates.ToArray())
        {
            if (pair.Key != null) pair.Key.enabled = pair.Value;
        }

        if (visual.Visual != null) Object.Destroy(visual.Visual);
        Visuals.Remove(playerId);
    }

    public static void Reset()
    {
        foreach (byte playerId in Visuals.Keys.ToArray()) Restore(playerId);
        Visuals.Clear();
    }
}
