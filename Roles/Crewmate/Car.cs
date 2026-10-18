using AmongUs.GameOptions;
using System.Collections;
using BHR.Modules;
using BHR.Roles.Core;
using UnityEngine;
using static BHR.Options;

namespace BHR.Roles.Crewmate;

internal class Car : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Car;
    private const int Id = 32900;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateBasic;
    //==================================================================\\

    private static OptionItem PropelDistance;

    private bool IsSkeld;
    private int Count;
    private HashSet<byte> CurrentlyPropelling = [];
    private Vector2 LastPosition;
    private byte CarId;

    public static string Name =>
        "<voffset=7em><alpha=#00>.</alpha></voffset><size=150%><line-height=97%><cspace=0.16em>" +
        "<#0000>W</color><mark=#628d85>WWWWW</mark><#0000>WW</color>\n" +
        "<mark=#586874>WW</mark><mark=#547a96>WW</mark><mark=#6894b6>WW</mark><mark=#586874>W</mark><#0000>W</color>\n" +
        "<mark=#586874>WWW</mark><mark=#547a96>WW</mark><mark=#586874>WW</mark><mark=#f5ee2e>W</mark>\n" +
        "<mark=#000000>W</mark><mark=#0d233f>W</mark><mark=#586874>WWW</mark><mark=#f5ee2e>W</mark><mark=#586874>W</mark><mark=#517a9a>W</mark>\n" +
        "<mark=#000000>WW</mark><#0000>W</color><mark=#000000>W</mark><mark=#0d233f>W</mark><mark=#586874>W</mark><mark=#517a9a>W</mark><#0000>W\n" +
        "WWW</color><mark=#000000>WW</mark><#0000>WWW";

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Car);

        PropelDistance = FloatOptionItem.Create(
                Id + 2,
                "CarPropelDistance",
                new(1f, 3f, 0.5f),
                3f,
                TabGroup.CrewmateRoles,
                false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Car]);
    }

    public override void Init()
    {
        IsSkeld = GameStates.SkeldIsActive || GameStates.DleksIsActive;
        Count = 0;
        CurrentlyPropelling.Clear();
        LastPosition = Vector2.zero;
        CarId = byte.MaxValue;
    }

    public override void Add(byte playerId)
    {
        CarId = playerId;
        Count = 0;
        CurrentlyPropelling.Clear();

        var pc = Utils.GetPlayerById(playerId);
        if (pc != null)
            LastPosition = pc.GetCustomPosition();
    }

    public override void Remove(byte playerId)
    {
        CurrentlyPropelling.Clear();
        Count = 0;
        CarId = byte.MaxValue;
    }

    public override void OnFixedUpdate(PlayerControl pc, bool lowLoad, long nowTime, int timerLowLoad)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (pc == null || !pc.IsAlive() || !GameStates.IsInTask || ExileController.Instance) return;

        // The EHR implementation checks movement every 5 fixed updates.
        if (Count++ < 5) return;
        Count = 0;

        Vector2 pos = pc.GetCustomPosition();
        if (Vector2.Distance(pos, LastPosition) <= 0.1f) return;

        Direction direction = pos.x < LastPosition.x
            ? pos.y < LastPosition.y
                ? Direction.DownLeft
                : pos.y > LastPosition.y
                    ? Direction.UpLeft
                    : Direction.Left
            : pos.x > LastPosition.x
                ? pos.y < LastPosition.y
                    ? Direction.DownRight
                    : pos.y > LastPosition.y
                        ? Direction.UpRight
                        : Direction.Right
                : pos.y < LastPosition.y
                    ? Direction.Down
                    : pos.y > LastPosition.y
                        ? Direction.Up
                        : Direction.Left;

        LastPosition = pos;

        PlayerControl target = Main.EnumerateAlivePlayerControls()
            .Where(x => x.PlayerId != pc.PlayerId)
            .OrderBy(x => Vector2.Distance(x.GetCustomPosition(), pos))
            .FirstOrDefault();

        if (target != null &&
            Vector2.Distance(target.GetCustomPosition(), pos) <= 1.2f &&
            CurrentlyPropelling.Add(target.PlayerId))
        {
            Main.Instance.StartCoroutine(Propel(pc, target, direction));
        }
    }

    private IEnumerator Propel(PlayerControl car, PlayerControl target, Direction direction)
    {
        if (car.AmOwner)
        {
            // The original EHR version completed its Driving Test Failed achievement here.
            // TOHE's supplied source does not contain that EHR achievement API.
        }

        float oldSpeed = Main.AllPlayerSpeed[target.PlayerId];
        Main.AllPlayerSpeed[target.PlayerId] = Main.MinSpeed;
        target.MarkDirtySettings();

        Vector2 pos = car.GetCustomPosition();

        const float unit = 0.15f;

        Vector2 addVector = direction switch
        {
            Direction.Left => new(-unit, 0),
            Direction.UpLeft => new(-unit, unit),
            Direction.Up => new(0, unit),
            Direction.UpRight => new(unit, unit),
            Direction.Right => new(unit, 0),
            Direction.DownRight => new(unit, -unit),
            Direction.Down => new(0, -unit),
            Direction.DownLeft => new(-unit, -unit),
            _ => Vector2.zero
        };

        float distance = Mathf.Min(PropelDistance.GetFloat(), 3f);
        Collider2D collider = target.Collider;

        // Keep the target at the last known valid position. We test the
        // NEXT position before teleporting so the Car cannot push players
        // through walls or into unplayable/out-of-bounds space.
        Vector2 safePos = target.GetCustomPosition();

        for (Vector2 newPos = safePos;
             Vector2.Distance(pos, newPos) <= distance && GameStates.IsInTask;
             newPos += addVector)
        {
            Vector2 nextPos = newPos + addVector;

            // Map-independent boundary check.
            // Test the candidate position before teleporting. This uses the
            // game's ShipOnlyMask rather than hard-coded coordinates or a
            // Skeld-only room check, so the same protection works on every map.
            if (PhysicsHelpers.AnythingBetween(
                    collider,
                    collider.bounds.center,
                    nextPos,
                    Constants.ShipOnlyMask,
                    false))
                break;

            // Also make sure the target's collider would not overlap
            // ship-only geometry at the candidate position.
            Vector2 originalPosition = target.GetCustomPosition();
            target.transform.position = nextPos;

            bool invalidPosition = Physics2D.OverlapCircle(
                collider.bounds.center,
                Mathf.Max(collider.bounds.extents.x, collider.bounds.extents.y),
                Constants.ShipOnlyMask) != null;

            target.transform.position = originalPosition;

            if (invalidPosition)
                break;

            safePos = nextPos;
            target.RpcTeleport(safePos, sendInfoInLogs: false);
            yield return new WaitForSecondsRealtime(0.05f);
        }

        Main.AllPlayerSpeed[target.PlayerId] = oldSpeed;
        target.MarkDirtySettings();
        CurrentlyPropelling.Remove(target.PlayerId);
    }

    private enum Direction
    {
        Left,
        UpLeft,
        Up,
        UpRight,
        Right,
        DownRight,
        Down,
        DownLeft
    }
}
