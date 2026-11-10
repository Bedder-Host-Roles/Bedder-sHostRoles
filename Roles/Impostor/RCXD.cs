using AmongUs.GameOptions;
using BHR.Modules;
using BHR.Roles.Core;
using UnityEngine;

namespace BHR.Roles.Impostor;

/// <summary>
/// RC-XD prototype. Activating the shapeshift ability deploys a networked car marker;
/// the host owns the countdown and resolves the explosion so all clients agree on kills.
/// </summary>
internal sealed class RCXD : RoleBase
{
    public override CustomRoles Role => CustomRoles.RCXD;
    private const int Id = 91000;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorKilling;

    private static OptionItem DetonationTime;
    private static OptionItem ExplosionRadius;
    private static readonly Dictionary<byte, RCXDCar> ActiveCars = [];
    private float remaining;
    private bool deployed;

    public override Sprite GetAbilityButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Timer");

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
        if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
        {
            foreach (var car in ActiveCars.Values.ToArray()) car.Despawn();
            ActiveCars.Clear();
        }
    }

    public override void Add(byte playerId)
    {
        remaining = 0f;
        deployed = false;
    }

    public override void Remove(byte playerId)
    {
        deployed = false;
        remaining = 0f;
        if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost && ActiveCars.Remove(playerId, out var car))
            car.Despawn();
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = 300f;
    public override bool CanUseKillButton(PlayerControl pc) => false;

    // This callback is invoked when the shapeshift ability is activated.
    public override void UnShapeShiftButton(PlayerControl player)
    {
        if (player == null || !player.IsAlive() || !GameStates.IsInTask || deployed) return;
        if (!AmongUsClient.Instance.AmHost) return;

        deployed = true;
        remaining = DetonationTime.GetFloat();
        var pos = player.GetCustomPosition();
        if (ActiveCars.Remove(player.PlayerId, out var previous)) previous.Despawn();
        ActiveCars[player.PlayerId] = new RCXDCar(pos);
        player.Visible = false;
        player.moveable = true;
    }

    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime, int timerLowLoad)
    {
        if (!AmongUsClient.Instance.AmHost || !deployed || player == null) return;
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
        if (restorePlayer)
        {
            player.Visible = true;
            player.moveable = true;
        }
    }

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
        => hud.AbilityButton.OverrideText(Translator.GetString("RCXDDeploy"));

    /// <summary>Uses BHR's existing vanilla-compatible custom network object transport.</summary>
    private sealed class RCXDCar : CustomNetObject
    {
        public void MoveTo(Vector2 position) => TP(position);

        public RCXDCar(Vector2 position)
        {
            // The proxy is synchronized through BHR's vanilla-compatible network-object path.
            // Native Pocket Circuit prefab integration is still required for the final visual.
            CreateNetObject("<size=120%><line-height=75%><#d9d9d9>▟<#ff3030>▙<br><#202020>▰<#d9d9d9>▰<#202020>▰<br><#ff3030>▟<#202020>▙</size>", position);
        }
    }
}
