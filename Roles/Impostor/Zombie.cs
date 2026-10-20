using AmongUs.GameOptions;
using System;

namespace BHR.Roles.Impostor;

internal class Zombie : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Zombie;
    private const int Id = 23900;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorKilling;
    //==================================================================\\

    private static OptionItem ZombieKillCooldown;
    private static OptionItem ZombieSpeedReduce;

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Zombie);
        ZombieKillCooldown = FloatOptionItem.Create(Id + 3, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 5f, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Zombie])
            .SetValueFormat(OptionFormat.Seconds);
        ZombieSpeedReduce = FloatOptionItem.Create(Id + 4, "ZombieSpeedReduce", new(0.0f, 1.0f, 0.1f), 0.1f, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Zombie])
            .SetValueFormat(OptionFormat.Multiplier);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        opt.SetFloat(FloatOptionNames.ImpostorLightMod, 0.2f);
    }

    public override void SetKillCooldown(byte id)
    {
        Main.AllPlayerKillCooldown[id] = ZombieKillCooldown.GetFloat();

        // Reduce speed without allowing Math.Clamp's max to go below min.
        float currentSpeed = Main.AllPlayerSpeed[id];
        float maxReduction = Math.Max(0f, currentSpeed - 0.5f);
        float reduction = Math.Clamp(
            ZombieSpeedReduce.GetFloat(),
            0f,
            maxReduction
        );

        Main.AllPlayerSpeed[id] = currentSpeed - reduction;
    }


    public static void CheckRealVotes(PlayerControl target, ref int VoteNum)
    {
        if (target.Is(CustomRoles.Zombie))
        {
            VoteNum = 0;
        }
    }
}
