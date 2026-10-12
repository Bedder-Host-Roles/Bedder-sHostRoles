
using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;

namespace BHR.Modules;

public interface IPlayerControl
{
    byte PlayerId { get; }
    CustomRoles GetCustomRole();
    List<CustomRoles> GetCustomSubRoles();
    CountTypes GetCountTypes();
    static PlayerControl LocalPlayer { get; }
}

public sealed class ModdedPlayerControl(PlayerControl player) : IPlayerControl
{
    private readonly PlayerControl player = player;

    public byte PlayerId => player?.PlayerId ?? byte.MaxValue;
    public CustomRoles GetCustomRole() => player == null ? CustomRoles.Crewmate : player.GetCustomRole();
    public List<CustomRoles> GetCustomSubRoles() => player == null ? [CustomRoles.NotAssigned] : player.GetCustomSubRoles();
    public CountTypes GetCountTypes() => player == null ? CountTypes.None : player.GetCountTypes();
    public static PlayerControl LocalPlayer => PlayerControl.LocalPlayer;


    public static implicit operator PlayerControl(ModdedPlayerControl mpc) => mpc?.player;
    public static implicit operator ModdedPlayerControl(PlayerControl pc) => pc == null ? null : new(pc);
}