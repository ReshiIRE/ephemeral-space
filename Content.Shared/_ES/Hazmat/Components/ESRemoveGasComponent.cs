using Content.Shared.Atmos;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._ES.Hazmat.Components;

// Allows entity to remove the gasses specified
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class ESRemoveGasComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextClean = TimeSpan.Zero;

    [DataField]
    [AutoNetworkedField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    [DataField]
    public List<Gas> GasesToRemove = new()
    {
        Gas.Miasma
    };

    [DataField]
    public float ScrubRate = 5400.0f;
}
