using Xunit;
using BHR.Modules;
using Moq;

namespace BHR.Tests.Modules;
public class AbilityUseManagerTests
{

    public AbilityUseManagerTests()
    {
        AbilityUseManager.Initialize();
    }

    [Fact]
    public void Initialize_ClearsAbilityUseLimits()
    {
        AbilityUseManager.SetAbilityUseLimit(0, 0.5f, rpc: false, log: false);
        AbilityUseManager.Initialize();
        Assert.Equal(float.NaN, AbilityUseManager.GetAbilityUseLimit(0));
    }

    [Fact]
    public void GetAbilityUseLimit_ForUnknownPlayer_ReturnsNaN()
    {
        Assert.True(float.IsNaN(AbilityUseManager.GetAbilityUseLimit(42)));
    }

    [Theory]
    [InlineData(0f, 0f)]
    [InlineData(0.04f, 0f)]
    [InlineData(0.05f, 0.1f)]
    [InlineData(10.34f, 10.3f)]
    [InlineData(10.35f, 10.4f)]
    [InlineData(1000f, 1000f)]
    public void SetAbilityUseLimit_StoresValueRoundedToOneDecimal(float limit, float expected)
    {
        AbilityUseManager.SetAbilityUseLimit(42, limit, rpc: false, log: false);

        Assert.Equal(expected, AbilityUseManager.GetAbilityUseLimit(42));
    }

    [Theory]
    [InlineData(-0.1f)]
    [InlineData(1000.1f)]
    [InlineData(float.NaN)]
    public void SetAbilityUseLimit_WithInvalidValue_PreservesPreviousValue(float limit)
    {
        AbilityUseManager.SetAbilityUseLimit(42, 5f, rpc: false, log: false);

        AbilityUseManager.SetAbilityUseLimit(42, limit, rpc: false, log: false);

        Assert.Equal(5f, AbilityUseManager.GetAbilityUseLimit(42));
    }

    [Fact]
    public void SetAbilityUseLimit_StoresLimitsIndependentlyPerPlayer()
    {
        AbilityUseManager.SetAbilityUseLimit(1, 2f, rpc: false, log: false);
        AbilityUseManager.SetAbilityUseLimit(2, 3f, rpc: false, log: false);

        Assert.Equal(2f, AbilityUseManager.GetAbilityUseLimit(1));
        Assert.Equal(3f, AbilityUseManager.GetAbilityUseLimit(2));
    }

    [Fact]
    public void IPlayerControlAbilityUseLimitOverloads_UseThePlayersId()
    {
        var player = CreatePlayer(42, CustomRoles.Crewmate);

        player.Object.SetAbilityUseLimit(3.5f, rpc: false, log: false);

        Assert.Equal(3.5f, player.Object.GetAbilityUseLimit());
    }

    [Fact]
    public void RpcRemoveAbilityUse_WithPositiveLimit_DecreasesLimit()
    {
        var player = CreatePlayer(42, CustomRoles.Crewmate);
        player.Object.SetAbilityUseLimit(2.5f, rpc: false, log: false);

        player.Object.RpcRemoveAbilityUse(log: false, rpc: false);

        Assert.Equal(1.5f, player.Object.GetAbilityUseLimit());
    }

    [Theory]
    [InlineData(0f)]
    public void RpcRemoveAbilityUse_WithNonPositiveLimit_DoesNotChangeLimit(float limit)
    {
        var player = CreatePlayer(42, CustomRoles.Crewmate);
        player.Object.SetAbilityUseLimit(limit, rpc: false, log: false);

        player.Object.RpcRemoveAbilityUse(log: false, rpc: false);

        Assert.Equal(limit, player.Object.GetAbilityUseLimit());
    }

    [Fact]
    public void RpcRemoveAbilityUse_WithoutLimit_DoesNotCreateLimit()
    {
        var player = CreatePlayer(42, CustomRoles.Crewmate);

        player.Object.RpcRemoveAbilityUse(log: false, rpc: false);

        Assert.True(float.IsNaN(player.Object.GetAbilityUseLimit()));
    }

    [Fact]
    public void RpcIncreaseAbilityUseLimitBy_WithLimit_IncreasesLimit()
    {
        var player = CreatePlayer(42, CustomRoles.Crewmate);
        player.Object.SetAbilityUseLimit(2.5f, rpc: false, log: false);

        player.Object.RpcIncreaseAbilityUseLimitBy(1.25f, log: false, rpc: false);

        Assert.Equal(3.8f, player.Object.GetAbilityUseLimit());
    }

    [Fact]
    public void RpcIncreaseAbilityUseLimitBy_WithoutLimit_DoesNotCreateLimit()
    {
        var player = CreatePlayer(42, CustomRoles.Crewmate);

        player.Object.RpcIncreaseAbilityUseLimitBy(1f, log: false, rpc: false);

        Assert.True(float.IsNaN(player.Object.GetAbilityUseLimit()));
    }

    [Theory]
    [InlineData(CustomRoles.Lich)]
    [InlineData(CustomRoles.SoulCollector)]
    [InlineData(CustomRoles.Benefactor)]
    [InlineData(CustomRoles.Berserker)]
    [InlineData(CustomRoles.Keeper)]
    [InlineData(CustomRoles.Collector)]
    [InlineData(CustomRoles.Doomsayer)]
    [InlineData(CustomRoles.Maverick)]
    [InlineData(CustomRoles.Pirate)]
    [InlineData(CustomRoles.Pixie)]
    [InlineData(CustomRoles.PunchingBag)]
    [InlineData(CustomRoles.Seeker)]
    [InlineData(CustomRoles.Taskinator)]
    [InlineData(CustomRoles.Vector)]
    [InlineData(CustomRoles.Vulture)]
    public void CanAbilityLimitBeManip_WithExcludedRole_ReturnsFalse(CustomRoles role)
    {
        var player = CreatePlayer(42, role);

        Assert.False(player.Object.CanAbilityLimitBeManip());
    }

    [Fact]
    public void CanAbilityLimitBeManip_WithEligibleRole_ReturnsTrue()
    {
        var player = CreatePlayer(42, CustomRoles.Crewmate);

        Assert.True(player.Object.CanAbilityLimitBeManip());
    }

    private static Mock<IPlayerControl> CreatePlayer(byte playerId, CustomRoles role)
    {
        var player = new Mock<IPlayerControl>();
        player.SetupGet(instance => instance.PlayerId).Returns(playerId);
        player.Setup(instance => instance.GetCustomRole()).Returns(role);
        return player;
    }
}