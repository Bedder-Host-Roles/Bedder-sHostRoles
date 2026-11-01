using AmongUs.GameOptions;
using BHR.Roles.Core;
using BHR.Modules;
using UnityEngine;
using static BHR.Options;
using static BHR.Translator;
using static BHR.Utils;

namespace BHR.Roles._Ghosts_.Crewmate;

/// <summary>
/// BHR adaptation of the vanilla Influencer ghost role.
///
/// IMPORTANT: This file implements the role's host-side targeting, cooldown,
/// limited sends, and randomized pictogram payload. BHR does not currently
/// expose the vanilla Influencer's image-picker UI or its private sticker
/// transport, so this implementation uses temporary recipient notifications
/// containing pictograms instead of pretending to send the game's image assets.
/// A dedicated client UI/RPC is required for exact vanilla parity.
/// </summary>
internal sealed class Influencer : RoleBase
{
    public override CustomRoles Role => CustomRoles.Influencer;
    private const int Id = 28100;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Influencer);
    public override CustomRoles ThisRoleBase => CustomRoles.GuardianAngel;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateGhosts;

    private static OptionItem AbilityCooldown;
    private static OptionItem MaxMessages;
    private static OptionItem ImagesPerMessage;

    // Deliberately ambiguous pictograms, matching the vanilla role's clue-based
    // design. They are not player identities and never encode the killer.
    private static readonly string[] Pictograms =
    [
        "📍", "👀", "🚨", "❓", "❗", "🧭", "🔍", "🕒", "🚪", "🩸",
        "🛡️", "⚡", "🧪", "🗝️", "📣", "🌙", "☀️", "🔥", "❄️", "💨",
        "🍄", "🍎", "🍌", "🍕", "🎭", "🎯", "🎲", "🎈", "🎁", "📦",
        "🚀", "🛸", "🧩", "🔔", "🔒", "🔓", "🧯", "🪤", "🧹", "🪫",
        "🐾", "🐟", "🦀", "🦴", "🌵", "🌈", "🌧️", "🌪️", "⭐", "❤️",
        "💔", "💡", "📷", "🎵", "🪙", "🧸", "🪞", "🪄", "🧿", "🪐"
    ];


    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Influencer);

        AbilityCooldown = FloatOptionItem.Create(
            Id + 10,
            GeneralOption.AbilityCooldown,
            new(2.5f, 120f, 2.5f),
            30f,
            TabGroup.CrewmateRoles,
            false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Influencer])
            .SetValueFormat(OptionFormat.Seconds);

        MaxMessages = IntegerOptionItem.Create(
            Id + 11,
            "InfluencerMaxMessages",
            new(1, 10, 1),
            3,
            TabGroup.CrewmateRoles,
            false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Influencer])
            .SetValueFormat(OptionFormat.Times);

        ImagesPerMessage = IntegerOptionItem.Create(
            Id + 12,
            "InfluencerImagesPerMessage",
            new(1, 3, 1),
            3,
            TabGroup.CrewmateRoles,
            false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Influencer])
            .SetValueFormat(OptionFormat.Times);
    }

    public override void Init() { }

    public override void Add(byte playerId)
    {
        // Each successful protect interaction sends one pictogram message.
        playerId.SetAbilityUseLimit(MaxMessages.GetInt());
    }

    public override void Remove(byte playerId) { }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.GuardianAngelCooldown = AbilityCooldown.GetFloat();
        AURoleOptions.ProtectionDurationSeconds = 0f;
    }

    public override bool OnCheckProtect(PlayerControl influencer, PlayerControl target)
    {
        if (influencer == null || target == null || influencer.PlayerId != _Player?.PlayerId)
            return false;

        if (influencer.IsAlive())
        {
            influencer.Notify(GetString("InfluencerMustBeDead"));
            return false;
        }

        if (!target.IsAlive() || target.PlayerId == influencer.PlayerId)
        {
            influencer.Notify(GetString("InfluencerInvalidRecipient"));
            return false;
        }

        if (influencer.GetAbilityUseLimit() <= 0)
        {
            influencer.Notify(GetString("InfluencerNoMessagesLeft"));
            return false;
        }

        var clueCount = Mathf.Clamp(ImagesPerMessage.GetInt(), 1, 3);
        var clues = PickRandomPictograms(clueCount);
        var payload = $"📸 {GetString("InfluencerMessageTitle")}\n{string.Join("   ", clues)}";

        // Notify is delivered to the selected living recipient only.
        target.Notify(payload);
        influencer.RpcRemoveAbilityUse();
        influencer.RpcResetAbilityCooldown();

        Logger.Info(
            $"Influencer {influencer.PlayerId} sent {clues.Length} pictograms to player {target.PlayerId}; " +
            $"remaining messages: {influencer.GetAbilityUseLimit() - 1}",
            "Influencer");

        return false;
    }

    private static string[] PickRandomPictograms(int count)
    {
        // Fisher-Yates partial shuffle guarantees no duplicate pictograms in
        // one message and uses BHR's configured random provider.
        var pool = Pictograms.ToList();
        var result = new string[Mathf.Clamp(count, 1, 3)];

        for (var i = 0; i < result.Length; i++)
        {
            var index = IRandom.Instance.Next(i, pool.Count);
            (pool[i], pool[index]) = (pool[index], pool[i]);
            result[i] = pool[i];
        }

        return result;
    }
}
