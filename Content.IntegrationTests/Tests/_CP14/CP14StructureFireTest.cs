using System.Linq;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._CP14;

[TestFixture]
public sealed class CP14StructureFireTest
{
    [Test]
    public async Task BurningWallFrameLeavesPlanks()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        var entities = server.ResolveDependency<IEntityManager>();
        var prototypes = server.ResolveDependency<IPrototypeManager>();
        EntityUid frame = default;

        await server.WaitPost(() =>
        {
            frame = entities.SpawnEntity("CP14WallFrameWooden", map.GridCoords);
            var heat = prototypes.Index<DamageTypePrototype>("Heat");
            entities.System<DamageableSystem>().TryChangeDamage(frame, new DamageSpecifier(heat, 49), true);
            entities.System<FlammableSystem>().SetFireStacks(frame, 6, ignite: true);
        });

        await server.WaitRunTicks(120);
        await server.WaitAssertion(() =>
        {
            Assert.That(entities.EntityExists(frame), Is.False);
            var nearby = entities.System<EntityLookupSystem>().GetEntitiesInRange(map.GridCoords, 3);
            Assert.That(nearby.Any(uid =>
                entities.GetComponent<MetaDataComponent>(uid).EntityPrototype?.ID == "CP14WoodenPlanks1"), Is.True);
        });

        await pair.CleanReturnAsync();
    }
}
