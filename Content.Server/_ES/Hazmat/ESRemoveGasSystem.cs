using Content.Server.Atmos.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Fluids.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Robust.Shared.Map.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Spawners;
using Robust.Shared.Prototypes;
using System.Linq;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Decals;
using Content.Server.Decals;
using System.Numerics;
using Robust.Shared.Timing;
using Content.Shared.Atmos;

using Content.Shared._ES.Hazmat.Components;

namespace Content.Server._ES.Hazmat;

public sealed partial class ESRemoveGasSystem : EntitySystem
{
    [Dependency] private AtmosphereSystem _atmosphere = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private DecalSystem _decal = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ESRemoveGasComponent, MapInitEvent>(OnMapInit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ESRemoveGasComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var removeGas, out var transform))
        {
            if (removeGas.NextClean > _timing.CurTime)
                continue;

            if (transform == null)
            {
                Log.Error("RemoveGas! Grid or transform component not found.");
                continue;
            }
            if (_atmosphere.GetTileMixture((uid, transform), true) is { } environment)
            {
                Scrub(frameTime, removeGas, environment);

                removeGas.NextClean += removeGas.UpdateInterval;
            }
        }
    }

    private void OnMapInit(Entity<ESRemoveGasComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextClean = _timing.CurTime + TimeSpan.FromSeconds(1);
    }

    private void Scrub(float timeDelta, ESRemoveGasComponent component, GasMixture tile)
    {
        var transferRate = component.ScrubRate * _atmosphere.PumpSpeedup();
        foreach (var gas in component.GasesToRemove)
        {
            var amountOfGas = tile.GetMoles(gas);
            var amountToReduceBy = timeDelta * transferRate;
            var adjustedAmountOfGas = MathF.Min(0f, amountOfGas - amountToReduceBy);
            tile.AdjustMoles(gas, adjustedAmountOfGas);
        }
    }
}
