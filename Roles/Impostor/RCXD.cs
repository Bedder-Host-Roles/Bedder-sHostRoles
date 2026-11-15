using System.Linq;
using AmongUs.GameOptions;
using AmongUs.InnerNet.GameDataMessages;
using BHR.Modules;
using BHR.Roles.Core;
using UnityEngine;

namespace BHR.Roles.Impostor;

/// <summary>
/// RC-XD remains an Impostor-base custom role. Its deploy and detonate inputs
/// are handled by BHR; no Shapeshifter transformation is used.
/// </summary>
internal sealed class RCXD : RoleBase
{
    public override CustomRoles Role => CustomRoles.RCXD;
    private const int Id = 91000;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorKilling;

    private static OptionItem DetonationTime;
    private static OptionItem ExplosionRadius;
    private static readonly Dictionary<byte, RCXDCar> ActiveCars = [];
    private static readonly Dictionary<byte, RCXDBody> ParkedBodies = [];
    private float remaining;
    private bool deployed;
    private bool cameraDisabled;
    private bool cameraFollowingCar;
    private Vector2 deploymentOrigin;

    public bool IsDeployed => deployed;

    public override Sprite GetAbilityButtonSprite(PlayerControl player, bool shapeshifting)
        => CustomButton.Get("Timer");

    public override Sprite GetKillButtonSprite(PlayerControl player, bool shapeshifting)
        => deployed ? CustomButton.Get("MeetingKillButton") : null;

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.RCXD);
        DetonationTime = FloatOptionItem.Create(Id + 2, "RCXDDetonationTime", new(5f, 60f, 5f), 20f, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.RCXD]).SetValueFormat(OptionFormat.Seconds);
        ExplosionRadius = FloatOptionItem.Create(Id + 3, "RCXDExplosionRadius", new(0.5f, 5f, 0.5f), 2f, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.RCXD]).SetValueFormat(OptionFormat.Multiplier);
    }

    public override void Init()
    {
        remaining = 0f;
        deployed = false;
        cameraDisabled = false;
        cameraFollowingCar = false;
        deploymentOrigin = Vector2.zero;
        if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
        {
            foreach (var car in ActiveCars.Values.ToArray()) car.Despawn();
            foreach (var body in ParkedBodies.Values.ToArray()) body.Despawn();
            ActiveCars.Clear();
            ParkedBodies.Clear();
        }
    }

    public override void Add(byte playerId)
    {
        remaining = 0f;
        deployed = false;
        cameraDisabled = false;
        cameraFollowingCar = false;
        deploymentOrigin = Vector2.zero;
    }

    public override void Remove(byte playerId)
    {
        bool wasDeployed = deployed;
        deployed = false;
        remaining = 0f;
        if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
        {
            var owner = Utils.GetPlayerById(playerId);
            if (wasDeployed && owner != null)
            {
                owner.NetTransform.RpcSnapTo(deploymentOrigin);
                owner.Visible = true;
                owner.moveable = true;
            }
            if (ActiveCars.Remove(playerId, out var car)) car.Despawn();
            if (ParkedBodies.Remove(playerId, out var body)) body.Despawn();
        }
        if (PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.PlayerId == playerId)
        {
            PlayerControl.LocalPlayer.Visible = true;
            PlayerControl.LocalPlayer.moveable = true;
        }
        RestoreLocalCamera();
        cameraFollowingCar = false;
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = 300f;
    public override bool CanUseKillButton(PlayerControl pc) => deployed;

    // Impostor-base roles do not receive a role ability button by default. BHR
    // keeps the existing AbilityButton visible and routes its click here.
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        if (hud == null || PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.PlayerId != playerId) return;
        hud.AbilityButton.OverrideText(Translator.GetString("RCXDDeploy"));
        hud.AbilityButton.ToggleVisible(!deployed && GameStates.IsInTask);
        if (deployed)
        {
            hud.KillButton.OverrideText(Translator.GetString("KillButtonText"));
            hud.KillButton.ToggleVisible(GameStates.IsInTask);
        }
    }

    public override bool OnAbilityButtonClick(PlayerControl player)
    {
        if (player == null || player != PlayerControl.LocalPlayer || !player.IsAlive() || !GameStates.IsInTask || deployed)
            return true;

        // Set local state immediately so the owner can switch to the car camera
        // and use Kill while the host processes the deploy request.
        deployed = true;
        player.Visible = false;
        player.moveable = true;
        if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
            HostDeploy(player);
        else
            player.CheckMurder(player); // vanilla CheckMurder RPC; host intercepts the self-target safely
        return true;
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        // Deliberately do not configure or invoke the Shapeshifter ability.
    }

    public void HostDeploy(PlayerControl player)
    {
        if (player == null || !player.IsAlive() || !GameStates.IsInTask || deployed && ActiveCars.ContainsKey(player.PlayerId)) return;
        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return;

        deployed = true;
        remaining = DetonationTime.GetFloat();
        var pos = player.GetCustomPosition();
        deploymentOrigin = pos;

        // Keep a stationary, outfit-matched copy of the player at the launch point.
        // The real player is hidden and moved to steer the TextMesh RC-XD.
        if (ActiveCars.Remove(player.PlayerId, out var previous)) previous.Despawn();
        if (ParkedBodies.Remove(player.PlayerId, out var previousBody)) previousBody.Despawn();
        var outfit = player.Data?.Outfits?[PlayerOutfitType.Default];
        if (outfit != null) ParkedBodies[player.PlayerId] = new RCXDBody(pos, outfit);
        ActiveCars[player.PlayerId] = new RCXDCar(pos);
        player.Visible = false;
        player.moveable = true;
    }

    public void HostDetonate(PlayerControl player)
    {
        if (player == null || !deployed || AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return;
        Detonate(player);
    }

    public void SetNetworkState(PlayerControl owner, bool isDeployed)
    {
        deployed = isDeployed;
        if (owner != null && owner == PlayerControl.LocalPlayer)
        {
            owner.Visible = !isDeployed;
            owner.moveable = true;
        }
        if (!isDeployed)
        {
            remaining = 0f;
            RestoreLocalCamera();
        }
    }

    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime, int timerLowLoad)
    {
        if (player == null) return;

        if (player == PlayerControl.LocalPlayer && deployed)
            UpdateLocalCamera(player);

        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost || !deployed) return;
        if (!GameStates.IsInTask || GameStates.IsEnded || !player.IsAlive())
        {
            EndDeployment(player, restorePlayer: true);
            return;
        }

        if (ActiveCars.TryGetValue(player.PlayerId, out var activeCar))
            activeCar.MoveTo(player.GetCustomPosition());

        remaining -= Time.fixedDeltaTime;
        if (remaining <= 0f)
            Detonate(player);
    }

    private void UpdateLocalCamera(PlayerControl owner)
    {
        if (HudManager.Instance == null || HudManager.Instance.PlayerCam == null) return;

        // Fake-player custom objects use PlayerId 254. The car's pixel-art name
        // begins with this distinctive character sequence; the parked body uses
        // PlayerId 253 and therefore cannot be mistaken for the moving car.
        PlayerControl proxy = null;
        foreach (var pc in PlayerControl.AllPlayerControls)
        {
            if (pc == null || !pc.notRealPlayer || pc.PlayerId != 254
                || pc.Data == null || pc.Data.PlayerName == null || !pc.Data.PlayerName.Contains("▟"))
                continue;

            proxy = pc;
            break;
        }

        if (proxy == null)
        {
            // The host despawns the RC-XD after it explodes. That is the signal
            // for a non-host owner to restore their local player and camera.
            if (cameraFollowingCar)
            {
                owner.Visible = true;
                owner.moveable = true;
                deployed = false;
                remaining = 0f;
                RestoreLocalCamera();
                cameraFollowingCar = false;
            }
            return;
        }

        var cam = HudManager.Instance.PlayerCam;
        if (!cameraDisabled)
        {
            cam.enabled = false;
            cameraDisabled = true;
        }
        cameraFollowingCar = true;
        var current = cam.transform.position;
        cam.transform.position = new Vector3(proxy.transform.position.x, proxy.transform.position.y, current.z);
    }

    private void RestoreLocalCamera()
    {
        if (!cameraDisabled) return;
        if (HudManager.Instance != null && HudManager.Instance.PlayerCam != null)
        {
            HudManager.Instance.PlayerCam.enabled = true;
            HudManager.Instance.PlayerCam.SnapToTarget();
        }
        cameraDisabled = false;
        cameraFollowingCar = false;
    }

    private void Detonate(PlayerControl owner)
    {
        if (!AmongUsClient.Instance.AmHost || owner == null) return;
        Vector2 blastPosition = owner.GetCustomPosition();
        if (ActiveCars.TryGetValue(owner.PlayerId, out var car))
        {
            blastPosition = car.Position;
            car.Despawn();
            ActiveCars.Remove(owner.PlayerId);
        }

        _ = new Explosion(5f, 0.5f, blastPosition);
        foreach (var target in Main.EnumerateAlivePlayerControls().ToArray())
        {
            if (target == null || target.notRealPlayer || target.PlayerId == owner.PlayerId || target.inVent) continue;
            if (target.Is(Custom_Team.Impostor)) continue;
            if (Vector2.Distance(target.GetCustomPosition(), blastPosition) > ExplosionRadius.GetFloat()) continue;
            target.SetDeathReason(PlayerState.DeathReason.Bombed);
            target.SetRealKiller(owner);
            target.RpcMurderPlayer(target);
        }
        EndDeployment(owner, restorePlayer: true);
    }

    private void EndDeployment(PlayerControl player, bool restorePlayer)
    {
        deployed = false;
        remaining = 0f;
        if (ActiveCars.Remove(player.PlayerId, out var car)) car.Despawn();
        if (ParkedBodies.Remove(player.PlayerId, out var body)) body.Despawn();
        if (restorePlayer)
        {
            // Return the real player to the exact position where the RC-XD was
            // deployed before handing the camera back to the normal player cam.
            player.NetTransform.RpcSnapTo(deploymentOrigin);
            player.Visible = true;
            player.moveable = true;
        }
        if (player == PlayerControl.LocalPlayer)
            RestoreLocalCamera();
    }

    public static bool HandleKillButtonClick(PlayerControl player)
    {
        if (player == null || player.GetRoleClass() is not RCXD role || !role.IsDeployed) return false;
        if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
            role.HostDetonate(player);
        else
            player.CheckMurder(player); // host turns the self-target into detonation, never a real kill
        return true;
    }

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (!deployed) return true;
        HostDetonate(killer);
        return false;
    }

    /// <summary>TextMesh-based pixel-art remote control car proxy.</summary>
    private sealed class RCXDCar : CustomNetObject
    {
        public void MoveTo(Vector2 position) => TP(position);

        public RCXDCar(Vector2 position)
        {
            CreateNetObject("<size=130%><line-height=72%><#d0d0d0>　▟<#ff3030>▙　<br><#202020>▟<#d0d0d0>▰<#202020>▰<#d0d0d0>▰<#202020>▙<br><#202020>▔<#ff3030>▔<#202020>▔<#ff3030>▔</size>", position);
        }
    }

    /// <summary>Stationary visible player-outfit proxy left at the deployment point.</summary>
    private sealed class RCXDBody : CustomNetObject
    {
        public RCXDBody(Vector2 position, NetworkedPlayerInfo.PlayerOutfit outfit)
        {
            // visible=true assigns the dedicated visible fake-player ID (253),
            // while the supplied outfit makes the parked proxy look like its owner.
            CreateNetObject(string.Empty, position, visible: true, pOutfit: outfit);
        }
    }
}

[HarmonyPatch(typeof(KillButton), nameof(KillButton.DoClick))]
internal static class RCXDKillButtonPatch
{
    public static bool Prefix()
    {
        var player = PlayerControl.LocalPlayer;
        var button = HudManager.Instance != null ? HudManager.Instance.KillButton : null;
        Logger.Info(
            $"[KillButton] DoClick received: player={(player != null ? player.GetRealName() : "<null>")}; " +
            $"role={(player != null ? player.GetCustomRole().ToString() : "<none>")}; " +
            $"target={(button != null && button.currentTarget != null ? button.currentTarget.GetRealName() : "<none>")}; " +
            $"visible={(button != null && button.gameObject.activeInHierarchy)}; " +
            $"canKill={(player != null && player.CanUseKillButton())}",
            "KillButton");
        return !RCXD.HandleKillButtonClick(player);
    }
}
