using AmongUs.GameOptions;

namespace BHR.Roles.Vanilla;

internal class JudgeBHR : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.JudgeBHR;
    private const int Id = 32800;
    public override CustomRoles ThisRoleBase => CustomRoles.Judge;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateVanilla;
    //==================================================================\\

    private static OptionItem JudgeTaskRequirementPercentage;

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.JudgeBHR);
        JudgeTaskRequirementPercentage = FloatOptionItem.Create(Id + 2, GeneralOption.JudgeBase_JudgeTaskRequirementPercentage, new(0, 100, 5), 50, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.JudgeBHR])
            .SetValueFormat(OptionFormat.Percent);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.JudgeTaskRequirementPercentage = JudgeTaskRequirementPercentage.GetFloat();
    }
}
