using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Prototypes;
using Content.Shared.DoAfter;
using Content.Shared.Chemistry.Components;
using Content.Shared._ES.Hazmat;
using Content.Shared._ES.Core.Timer.Components;

namespace Content.Shared._ES.Hazmat.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ESSharedSanitationChipSystem))]
public sealed partial class ESSanitationChipComponent : Component
{
    [DataField]
    public float MovementThreshold = 0.1f;

    [DataField(required: true), AutoNetworkedField]
    public TimeSpan DelayTime;

    [DataField(required: true), AutoNetworkedField]
    public TimeSpan TimeUntilGasSpawn;

    [DataField(required: true), AutoNetworkedField]
    public TimeSpan Duration;

    [DataField(required: true), AutoNetworkedField]
    public int SpreadAmount;

    [DataField, AutoNetworkedField]
    public EntProtoId<SmokeComponent> SmokePrototype = "FoamSterilization";

    [DataField, AutoNetworkedField]
    public Solution Solution = new();
}

// used to mark a vent, so that the gas vent can properly update it's sprite
[RegisterComponent, NetworkedComponent]
public sealed partial class ESVentAffectedBySanitationChipComponent : Component { }

[Serializable, NetSerializable]
public sealed partial class ESSanitationChipDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class ESSanitationEventData
{
    public TimeSpan TimeUntilGasSpawn;

    public TimeSpan Duration { get; set; }

    public int SpreadAmount { get; set; }

    public EntProtoId<SmokeComponent> SmokePrototype { get; set; }

    public Solution Solution { get; set; } = new();
}

[Serializable, NetSerializable]
public sealed partial class ESSanitationReleaseGasTimerEvent : ESEntityTimerEvent
{
    public ESSanitationEventData EventData;

    public ESSanitationReleaseGasTimerEvent(ESSanitationEventData data)
    {
        EventData = data;
    }
}

[Serializable, NetSerializable]
public sealed partial class ESSanitationStopGasTimerEvent : ESEntityTimerEvent;
