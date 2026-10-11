using BHR;
using BHR.Modules;
using Hazel;
using Moq;

namespace BHR.Tests.Modules;

public class ExtendedPlayerControlTests
{
    [Theory]
    [InlineData(0, -27f, 3.3f)]
    [InlineData(1, -11.4f, 8.2f)]
    [InlineData(2, 42.6f, -19.9f)]
    [InlineData(3, 27f, 3.3f)]
    [InlineData(4, -16.8f, -6.2f)]
    [InlineData(5, 10.2f, 18.1f)]
    public void GetBlackRoomCoordinates_ReturnsExpectedPositionForMap(int mapId, float expectedX, float expectedY)
    {
        var (X, Y) = ExtendedPlayerControl.GetBlackRoomCoordinates(mapId);

        Assert.Equal(expectedX, X);
        Assert.Equal(expectedY, Y);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(6)]
    public void GetBlackRoomCoordinates_ForUnsupportedMap_ThrowsNotImplementedException(int mapId)
    {
        Assert.Throws<NotImplementedException>(() => ExtendedPlayerControl.GetBlackRoomCoordinates(mapId));
    }

    [Theory]
    [InlineData(CustomRoles.Pestilence, true)]
    [InlineData(CustomRoles.War, true)]
    [InlineData(CustomRoles.Death, true)]
    [InlineData(CustomRoles.Famine, true)]
    [InlineData(CustomRoles.Crewmate, false)]
    public void IsTransformedNeutralApocalypse_UsesTheIPlayerControlRole(CustomRoles role, bool expected)
    {
        var player = CreatePlayer(role);

        Assert.Equal(expected, player.Object.IsTransformedNeutralApocalypse());
        player.Verify(instance => instance.GetCustomRole(), Times.Once);
    }

    [Fact]
    public void Is_MainRole_ReturnsTrueWhenMatching()
    {
        var player = CreatePlayer(CustomRoles.Sheriff);

        Assert.True(player.Object.Is(CustomRoles.Sheriff));
        Assert.False(player.Object.Is(CustomRoles.Engineer));
    }

    [Fact]
    public void Is_SubRole_ChecksSubRoles()
    {
        var player = CreatePlayer(CustomRoles.Crewmate, subRoles: new List<CustomRoles> { CustomRoles.Madmate, CustomRoles.Torch });

        Assert.True(player.Object.Is(CustomRoles.Madmate));
        Assert.True(player.Object.Is(CustomRoles.Torch));
        Assert.False(player.Object.Is(CustomRoles.Bait));
    }

    [Fact]
    public void Is_CountTypes_ReturnsTrueWhenMatching()
    {
        var player = CreatePlayer(CustomRoles.Crewmate, countTypes: CountTypes.Crew);

        Assert.True(player.Object.Is(CountTypes.Crew));
        Assert.False(player.Object.Is(CountTypes.Impostor));
    }

    [Fact]
    public void IsAnySubRole_EvaluatesPredicateAgainstSubRoles()
    {
        var player = CreatePlayer(CustomRoles.Crewmate, subRoles: new List<CustomRoles> { CustomRoles.Madmate, CustomRoles.Torch });

        Assert.True(player.Object.IsAnySubRole(r => r == CustomRoles.Torch));
        Assert.False(player.Object.IsAnySubRole(r => r == CustomRoles.Lovers));
    }

    [Theory]
    [InlineData(CustomRoles.Madmate, true)]
    [InlineData(CustomRoles.Charmed, true)]
    [InlineData(CustomRoles.Infected, true)]
    [InlineData(CustomRoles.Contagious, true)]
    [InlineData(CustomRoles.Egoist, true)]
    [InlineData(CustomRoles.Enchanted, true)]
    [InlineData(CustomRoles.Crewmate, false)]
    public void IsNonCrewSheriff_IdentifiesConvertedOrSubRoles(CustomRoles subRole, bool expected)
    {
        var player = subRole == CustomRoles.Crewmate 
            ? CreatePlayer(CustomRoles.Crewmate) 
            : CreatePlayer(CustomRoles.Crewmate, subRoles: new List<CustomRoles> { subRole });

        Assert.Equal(expected, player.Object.IsNonCrewSheriff());
    }

    [Fact]
    public void IsNonCrewSheriff_WhenConvertedRoleIsTheMainRole_ReturnsFalse()
    {
        var player = CreatePlayer(CustomRoles.Madmate);

        Assert.False(player.Object.IsNonCrewSheriff());
    }

    [Theory]
    [InlineData(true, true, true, true)]
    [InlineData(false, true, true, false)]
    [InlineData(true, false, true, false)]
    [InlineData(true, true, false, false)]
    [InlineData(false, false, false, false)]
    public void CanPerformInGameHostAction_RequiresHostGameAndPlayer(bool isHost, bool isInGame, bool hasPlayer,
        bool expected)
    {
        Assert.Equal(expected, ExtendedPlayerControl.CanPerformInGameHostAction(isHost, isInGame, hasPlayer));
    }

    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(42, 42, true)]
    [InlineData(0, 1, false)]
    [InlineData(-1, 0, false)]
    public void IsLocalClient_ReturnsTrueOnlyForMatchingClientIds(int currentClientId, int targetClientId,
        bool expected)
    {
        Assert.Equal(expected, ExtendedPlayerControl.IsLocalClient(currentClientId, targetClientId));
    }

    [Theory]
    [InlineData(CustomRoles.LastImpostor)]
    [InlineData(CustomRoles.Madmate)]
    [InlineData(CustomRoles.Charmed)]
    [InlineData(CustomRoles.Recruit)]
    [InlineData(CustomRoles.Admired)]
    [InlineData(CustomRoles.Soulless)]
    [InlineData(CustomRoles.Lovers)]
    [InlineData(CustomRoles.Infected)]
    [InlineData(CustomRoles.Enchanted)]
    [InlineData(CustomRoles.Contagious)]
    [InlineData(CustomRoles.Narc)]
    public void ShouldBeDisplayed_ForHiddenAddOns_ReturnsFalse(CustomRoles subRole)
    {
        Assert.False(subRole.ShouldBeDisplayed());
    }

    [Theory]
    [InlineData(CustomRoles.Bait)]
    [InlineData(CustomRoles.Torch)]
    [InlineData(CustomRoles.Crewmate)]
    public void ShouldBeDisplayed_ForVisibleRoles_ReturnsTrue(CustomRoles subRole)
    {
        Assert.True(subRole.ShouldBeDisplayed());
    }

    [Fact]
    public void ShouldBeDisplayed_MatchesTheCompleteHiddenRoleSet()
    {
        var hiddenRoles = new HashSet<CustomRoles>
        {
            CustomRoles.LastImpostor,
            CustomRoles.Madmate,
            CustomRoles.Charmed,
            CustomRoles.Recruit,
            CustomRoles.Admired,
            CustomRoles.Soulless,
            CustomRoles.Lovers,
            CustomRoles.Infected,
            CustomRoles.Enchanted,
            CustomRoles.Contagious,
            CustomRoles.Narc
        };

        foreach (var role in Enum.GetValues<CustomRoles>())
        {
            Assert.Equal(!hiddenRoles.Contains(role), role.ShouldBeDisplayed());
        }
    }

    [Fact]
    public void IPlayerControlPredicates_WithNullPlayer_ReturnFalse()
    {
        IPlayerControl? player = null;

        Assert.False(player.Is(CustomRoles.Crewmate));
        Assert.False(player.Is(CountTypes.Crew));
        Assert.False(player.IsAnySubRole(_ => true));
        Assert.False(player.IsNonCrewSheriff());
    }

    [Fact]
    public void GetConflictedAddOns_ReturnsOnlyRolesRejectedByCompatibilityPredicate()
    {
        var addOns = new List<CustomRoles> { CustomRoles.Bait, CustomRoles.Torch, CustomRoles.Bait };

        var conflictedAddOns = ExtendedPlayerControl.GetConflictedAddOns(addOns,
            addOn => addOn != CustomRoles.Bait);

        Assert.Equal(new[] { CustomRoles.Bait, CustomRoles.Bait }, conflictedAddOns);
        Assert.Equal(new[] { CustomRoles.Bait, CustomRoles.Torch, CustomRoles.Bait }, addOns);
    }

    [Fact]
    public void GetConflictedAddOns_WhenEveryAddOnIsCompatible_ReturnsEmptyList()
    {
        var conflictedAddOns = ExtendedPlayerControl.GetConflictedAddOns(
            new[] { CustomRoles.Bait, CustomRoles.Torch }, _ => true);

        Assert.Empty(conflictedAddOns);
    }

    [Fact]
    public void TryGetTeleportSendOption_WhenRateLimitBypassIsInactive_UsesReliableWithoutMutatingBuffer()
    {
        var bufferTimes = new Dictionary<byte, int>();
        var teleportBuffers = new Dictionary<byte, int> { [4] = 10 };

        var canTeleport = ExtendedPlayerControl.TryGetTeleportSendOption(false, true, 4, bufferTimes, teleportBuffers, out var sendOption);

        Assert.True(canTeleport);
        Assert.Equal(SendOption.Reliable, sendOption);
        Assert.Equal(10, teleportBuffers[4]);
    }

    [Fact]
    public void TryGetTeleportSendOption_WhenBypassIsDisabled_UsesReliableWithoutReadingBufferTime()
    {
        var teleportBuffers = new Dictionary<byte, int> { [4] = 10 };

        var canTeleport = ExtendedPlayerControl.TryGetTeleportSendOption(true, false, 4,
            new Dictionary<byte, int>(), teleportBuffers, out var sendOption);

        Assert.True(canTeleport);
        Assert.Equal(SendOption.Reliable, sendOption);
        Assert.Equal(10, teleportBuffers[4]);
    }

    [Fact]
    public void TryGetTeleportSendOption_WhenBufferTimeIsMissing_RejectsTeleport()
    {
        var canTeleport = ExtendedPlayerControl.TryGetTeleportSendOption(true, true, 4,
            new Dictionary<byte, int>(), new Dictionary<byte, int>(), out var sendOption);

        Assert.False(canTeleport);
        Assert.Equal(SendOption.Reliable, sendOption);
    }

    [Theory]
    [InlineData(10, 10, SendOption.None)]
    [InlineData(16, 10, SendOption.Reliable)]
    public void TryGetTeleportSendOption_UpdatesOrRateLimitsByElapsedBufferTime(int bufferTime, int previousBufferTime, SendOption expectedSendOption)
    {
        var teleportBuffers = new Dictionary<byte, int> { [4] = previousBufferTime };

        var canTeleport = ExtendedPlayerControl.TryGetTeleportSendOption(true, true, 4,
            new Dictionary<byte, int> { [4] = bufferTime }, teleportBuffers, out var sendOption);

        Assert.True(canTeleport);
        Assert.Equal(expectedSendOption, sendOption);
        Assert.Equal(expectedSendOption == SendOption.Reliable ? bufferTime : previousBufferTime, teleportBuffers[4]);
    }

    [Fact]
    public void TryGetTeleportSendOption_ForFirstTeleport_RecordsBufferTime()
    {
        var teleportBuffers = new Dictionary<byte, int>();

        var canTeleport = ExtendedPlayerControl.TryGetTeleportSendOption(true, true, 4,
            new Dictionary<byte, int> { [4] = 10 }, teleportBuffers, out var sendOption);

        Assert.True(canTeleport);
        Assert.Equal(SendOption.Reliable, sendOption);
        Assert.Equal(10, teleportBuffers[4]);
    }

    [Fact]
    public void CanBeTeleported_WhenAllConditionsAreClear_ReturnsTrue()
    {
        Assert.True(ExtendedPlayerControl.CanBeTeleported(false, false, true, false, false, false, false, false, false, false, false));
    }

    [Theory]
    [InlineData(true, false, true, false, false, false, false, false, false, false, false)]
    [InlineData(false, true, true, false, false, false, false, false, false, false, false)]
    [InlineData(false, false, false, false, false, false, false, false, false, false, false)]
    [InlineData(false, false, true, true, false, false, false, false, false, false, false)]
    [InlineData(false, false, true, false, true, false, false, false, false, false, false)]
    [InlineData(false, false, true, false, false, true, false, false, false, false, false)]
    [InlineData(false, false, true, false, false, false, true, false, false, false, false)]
    [InlineData(false, false, true, false, false, false, false, true, false, false, false)]
    [InlineData(false, false, true, false, false, false, false, false, true, false, false)]
    [InlineData(false, false, true, false, false, false, false, false, false, true, false)]
    [InlineData(false, false, true, false, false, false, false, false, false, false, true)]
    public void CanBeTeleported_WhenAnyBlockingConditionIsTrue_ReturnsFalse(bool hasNoPlayerData, bool isMeetingStarted,
        bool isAlive, bool isInVent, bool isWalkingToVent, bool isUsingMovingPlatform, bool isPlayingVentAnimation,
        bool isOnLadder, bool isPlayingLadderAnimation, bool isEaten, bool isBlasted)
    {
        Assert.False(ExtendedPlayerControl.CanBeTeleported(hasNoPlayerData, isMeetingStarted, isAlive, isInVent,
            isWalkingToVent, isUsingMovingPlatform, isPlayingVentAnimation, isOnLadder, isPlayingLadderAnimation,
            isEaten, isBlasted));
    }

    [Fact]
    public void GetVisiblePlayerId_WithoutStolenId_ReturnsPlayerId()
    {
        Assert.Equal(4, ExtendedPlayerControl.GetVisiblePlayerId(4, null));
    }

    [Fact]
    public void GetVisiblePlayerId_WithStolenId_ReturnsStolenId()
    {
        Assert.Equal(12, ExtendedPlayerControl.GetVisiblePlayerId(4, 12));
        Assert.Equal(0, ExtendedPlayerControl.GetVisiblePlayerId(255, 0));
    }

    [Theory]
    [InlineData(true, false, false, false, false, false, false)]
    [InlineData(false, true, false, false, false, false, false)]
    [InlineData(false, false, true, false, false, false, false)]
    [InlineData(false, false, false, true, true, false, false)]
    [InlineData(false, false, false, false, false, false, true)]
    public void CanKnowDeathReason_WhenAuthorizedForDeadTarget_ReturnsTrue(bool everyoneCanSee, bool isDoctor,
        bool isAutopsy, bool isGhost, bool ghostsCanSee, bool isTargetAlive, bool isGravestone)
    {
        Assert.True(ExtendedPlayerControl.CanKnowDeathReason(everyoneCanSee, isDoctor, isAutopsy, isGhost,
            ghostsCanSee, isTargetAlive, isGravestone));
    }

    [Theory]
    [InlineData(false, false, false, false, false, false, false)]
    [InlineData(false, false, false, true, false, false, false)]
    [InlineData(true, false, false, false, false, true, false)]
    [InlineData(false, false, false, false, false, true, true)]
    public void CanKnowDeathReason_WhenUnauthorizedOrTargetAlive_ReturnsFalse(bool everyoneCanSee, bool isDoctor,
        bool isAutopsy, bool isGhost, bool ghostsCanSee, bool isTargetAlive, bool isGravestone)
    {
        Assert.False(ExtendedPlayerControl.CanKnowDeathReason(everyoneCanSee, isDoctor, isAutopsy, isGhost,
            ghostsCanSee, isTargetAlive, isGravestone));
    }

    [Theory]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(false, false, false)]
    public void CanKnowDeadTeam_RequiresNecroviewAndDeadTarget(bool isNecroview, bool isTargetAlive, bool expected)
    {
        Assert.Equal(expected, ExtendedPlayerControl.CanKnowDeadTeam(isNecroview, isTargetAlive));
    }

    [Theory]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(false, false, false)]
    public void CanKnowLivingTeam_RequiresVisionaryAndDeadTarget(bool isVisionary, bool isTargetAlive, bool expected)
    {
        Assert.Equal(expected, ExtendedPlayerControl.CanKnowLivingTeam(isVisionary, isTargetAlive));
    }

    [Theory]
    [InlineData(CustomRoles.Councillor)]
    [InlineData(CustomRoles.Inspector)]
    [InlineData(CustomRoles.Prosecutor)]
    [InlineData(CustomRoles.Retributionist)]
    [InlineData(CustomRoles.Starspawn)]
    [InlineData(CustomRoles.Swapper)]
    public void UsesJudgeAbilityAsTrigger_ForModdedExceptionRoles_ReturnsFalseWithoutCallingFallback(CustomRoles role)
    {
        var fallbackCalled = false;

        var result = ExtendedPlayerControl.UsesJudgeAbilityAsTrigger(true, role, _ =>
        {
            fallbackCalled = true;
            return true;
        });

        Assert.False(result);
        Assert.False(fallbackCalled);
    }

    [Fact]
    public void UsesJudgeAbilityAsTrigger_ForNonExceptionRole_UsesFallback()
    {
        var result = ExtendedPlayerControl.UsesJudgeAbilityAsTrigger(true, CustomRoles.Crewmate, _ => true);

        Assert.True(result);
    }

    [Fact]
    public void UsesJudgeAbilityAsTrigger_ForUnmoddedExceptionRole_UsesFallback()
    {
        var result = ExtendedPlayerControl.UsesJudgeAbilityAsTrigger(false, CustomRoles.Councillor, _ => true);

        Assert.True(result);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(false, true)]
    public void ShouldSetRealKiller_OnlyRetainsExistingKillerWhenRequested(bool hasExistingKiller, bool doNotOverride)
    {
        var existingKillerTime = hasExistingKiller ? DateTime.UnixEpoch : DateTime.MinValue;

        var result = ExtendedPlayerControl.ShouldSetRealKiller(existingKillerTime, doNotOverride);

        Assert.Equal(!hasExistingKiller || !doNotOverride, result);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, true)]
    public void IsExiled_ReturnsTrueDuringGameOrAfterVote(bool isInGame, bool wasVotedOut, bool expected)
    {
        Assert.Equal(expected, ExtendedPlayerControl.IsExiled(isInGame, wasVotedOut));
    }

    [Theory]
    [InlineData(true, -1, 1, 0)]
    [InlineData(true, 0, 1, 0)]
    [InlineData(true, 1, 0, 1)]
    [InlineData(true, 2, 0, 2)]
    [InlineData(true, 3, 0, 2)]
    [InlineData(false, 2, -1, 0)]
    [InlineData(false, 0, 1, 1)]
    [InlineData(false, 0, 3, 2)]
    public void GetKillDistanceIndex_ClampsSelectedValue(bool overrideValue, int newValue, int configuredValue,
        int expectedIndex)
    {
        Assert.Equal(expectedIndex, ExtendedPlayerControl.GetKillDistanceIndex(overrideValue, newValue, configuredValue));
    }

    [Theory]
    [InlineData("Player", "Player")]
    [InlineData("<color=red>Player", "<red>Player")]
    [InlineData("color=color=Player", "Player")]
    public void SanitizePlayerName_RemovesColorAttributesTag(string name, string expectedName)
    {
        Assert.Equal(expectedName, ExtendedPlayerControl.SanitizePlayerName(name));
    }

    [Theory]
    [InlineData(CustomRoles.Crewmate, true)]
    [InlineData((CustomRoles)499, true)]
    [InlineData(CustomRoles.NotAssigned, false)]
    [InlineData(CustomRoles.Bait, false)]
    public void IsMainRoleAssignment_UsesNotAssignedAsTheBoundary(CustomRoles role, bool expected)
    {
        Assert.Equal(expected, ExtendedPlayerControl.IsMainRoleAssignment(role));
    }

    [Theory]
    [InlineData(0, SystemTypes.Reactor)]
    [InlineData(1, SystemTypes.Reactor)]
    [InlineData(2, SystemTypes.Laboratory)]
    [InlineData(3, SystemTypes.Reactor)]
    [InlineData(4, SystemTypes.HeliSabotage)]
    [InlineData(5, SystemTypes.Reactor)]
    [InlineData(99, SystemTypes.Reactor)]
    public void GetBlackScreenRepairSystemType_UsesMapSpecificRepairSystem(int mapId, SystemTypes expectedSystemType)
    {
        Assert.Equal(expectedSystemType, ExtendedPlayerControl.GetBlackScreenRepairSystemType(mapId));
    }

    [Theory]
    [InlineData(true, false, false, true)]
    [InlineData(false, false, false, false)]
    [InlineData(true, true, false, false)]
    [InlineData(true, false, true, false)]
    public void ShouldActivateObserverGuardAnimation_RequiresEnabledNonObserverAfterFirstMeeting(bool hasObserver,
        bool isForObserver, bool isFirstMeeting, bool expected)
    {
        Assert.Equal(expected, ExtendedPlayerControl.ShouldActivateObserverGuardAnimation(hasObserver, isForObserver,
            isFirstMeeting));
    }

    [Fact]
    public void ShouldDeferBlackScreenFix_WhenAllConditionsAreClear_ReturnsFalse()
    {
        Assert.False(ExtendedPlayerControl.ShouldDeferBlackScreenFix(false, false, false, false, false, false, true));
    }

    [Theory]
    [InlineData(true, false, false, false, false, false, true)]
    [InlineData(false, true, false, false, false, false, true)]
    [InlineData(false, false, true, false, false, false, true)]
    [InlineData(false, false, false, true, false, false, true)]
    [InlineData(false, false, false, false, true, false, true)]
    [InlineData(false, false, false, false, false, true, true)]
    [InlineData(false, false, false, false, false, false, false)]
    public void ShouldDeferBlackScreenFix_WhenAnyBlockingConditionIsPresent_ReturnsTrue(bool isMeeting,
        bool hasExileController, bool isSkippingTasks, bool isInVent, bool isUsingMovingPlatform, bool isOnLadder,
        bool hasDeadGhost)
    {
        Assert.True(ExtendedPlayerControl.ShouldDeferBlackScreenFix(isMeeting, hasExileController, isSkippingTasks,
            isInVent, isUsingMovingPlatform, isOnLadder, hasDeadGhost));
    }

    private static Mock<IPlayerControl> CreatePlayer(CustomRoles role, List<CustomRoles>? subRoles = null, CountTypes countTypes = CountTypes.Crew)
    {
        return CreatePlayer(1, role, subRoles, countTypes);
    }

    private static Mock<IPlayerControl> CreatePlayer(byte playerId, CustomRoles role, List<CustomRoles>? subRoles = null, CountTypes countTypes = CountTypes.Crew)
    {
        var player = new Mock<IPlayerControl>();
        player.SetupGet(instance => instance.PlayerId).Returns(playerId);
        player.Setup(instance => instance.GetCustomRole()).Returns(role);
        player.Setup(instance => instance.GetCustomSubRoles()).Returns(subRoles ?? new List<CustomRoles>());
        player.Setup(instance => instance.GetCountTypes()).Returns(countTypes);

        return player;
    }
}