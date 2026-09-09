using Content.Shared.Chemistry.Components;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Timing;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Tag;
using Content.Shared.Popups;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.Charges.Components;
using Content.Shared.Tools.Systems;
using Content.Shared._ES.Core.Timer;
using Content.Shared.Maps;
using Content.Shared.Trigger;
using Content.Shared.Trigger.Components.Effects;
using Content.Shared.Administration.Logs;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Spreader;
using Content.Server.Power.EntitySystems;
using Content.Server.Atmos.Monitor.Components;
using Content.Server._ES.Announcements;
using Content.Server.Pinpointer;
using Content.Server.Atmos.Monitor.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System.Linq;

using Content.Shared._ES.Hazmat.Components;
using Content.Shared._ES.Hazmat;

namespace Content.Server._ES.Hazmat;

public sealed partial class ESSanitationChipSystem : ESSharedSanitationChipSystem
{
    [Dependency] private MapSystem _map = default!;
    [Dependency] private SmokeSystem _smoke = default!;
    [Dependency] private TransformSystem _transform = default!;
    [Dependency] private TurfSystem _turf = default!;
    [Dependency] private SpreaderSystem _spreader = default!;
    [Dependency] private ESAnnouncementSystem _announcement = default!;
    [Dependency] private WeldableSystem _weldable = default!;
    [Dependency] private NavMapSystem _navMap = default!;
    [Dependency] private SharedChargesSystem _charges = default!;
    [Dependency] private PowerReceiverSystem _powerReceiver = default!;
    [Dependency] private AtmosDeviceNetworkSystem _atmosDeviceNetwork = default!;
    [Dependency] private ESEntityTimerSystem _entityTimer = default!;

    public const string Prepare = "sanitation_system_prepare_gas";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ESSanitationChipComponent, ESSanitationChipDoAfterEvent>(OnSanitationChipDoAfter);
    }

    // API
    public void ReleaseGasFromTarget(EntityUid target, ESSanitationEventData args)
    {
        var xform = CompOrNull<TransformComponent>(target);
        if (xform == null)
            return;

        // then spawn the foam out of those vents locations
        var mapCoords = _transform.GetMapCoordinates(target, xform);
        if (!_map.TryFindGridAt(mapCoords, out var gridUid, out var gridComp) ||
            !_map.TryGetTileRef(gridUid, gridComp, xform.Coordinates, out var tileRef) ||
            tileRef.Tile.IsEmpty)
        {
            return;
        }

        if (_turf.IsSpace(tileRef))
            return;

        var coords = _map.MapToGrid(gridUid, mapCoords);
        var smoke = Spawn(args.SmokePrototype, coords.SnapToGrid());
        var smokeComp = EnsureComp<SmokeComponent>(smoke);
        _smoke.StartSmoke(smoke, args.Solution.Clone(), (float)args.Duration.TotalSeconds, args.SpreadAmount, smokeComp);
    }

    private void OnSanitationChipDoAfter(Entity<ESSanitationChipComponent> chip, ref ESSanitationChipDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

        args.Handled |= TryUseSanitationChip(chip, args.Args.Target.Value);
    }

    private bool TryUseSanitationChip(Entity<ESSanitationChipComponent> chip, EntityUid target)
    {
        if (!_charges.TryUseCharge(chip.Owner))
            return false;

        if (!TryComp<AirAlarmComponent>(target, out var airAlarm))
            return false;

        var seconds = chip.Comp.TimeUntilGasSpawn.TotalSeconds.ToString();

        var location = FormattedMessage.RemoveMarkupPermissive(_navMap.GetNearestBeaconString(target));

        _announcement.DispatchRoundAnnouncement(Loc.GetString("es-sanitation-chip-announcement", ("seconds", seconds), ("area", location)),
            Loc.GetString("es-station-event-announcer"),
            announcementSound: new SoundPathSpecifier("/Audio/_ES/Announcements/attention_medium.ogg"),
            colorOverride: Color.LightGoldenrodYellow,
            important: true);

        var addresses = airAlarm.VentData.Keys.ToList();

        var eventData = new ESSanitationEventData
        {
            TimeUntilGasSpawn = chip.Comp.TimeUntilGasSpawn,
            Duration = chip.Comp.Duration,
            SpreadAmount = chip.Comp.SpreadAmount,
            SmokePrototype = chip.Comp.SmokePrototype,
            Solution = chip.Comp.Solution
        };

        foreach (var address in addresses)
        {
            _atmosDeviceNetwork.SanitationPrepare(target, address, eventData);
        }

        return true;
    }
}
