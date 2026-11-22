using Content.Client.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._CP14;

[TestFixture]
public sealed class CP14CandleVisualsTest
{
    [Test]
    public async Task ReceivingAnAlreadyLitCandleCreatesAndRemovesItsLight()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true });
        var map = await pair.CreateTestMap();
        var server = pair.Server;
        var client = pair.Client;
        EntityUid serverCandle = default;

        await server.WaitPost(() =>
        {
            var player = server.EntMan.SpawnEntity("CP14MobHuman", map.GridCoords);
            server.PlayerMan.SetAttachedEntity(pair.Player!, player);
            serverCandle = server.EntMan.SpawnEntity("CP14Candle", map.GridCoords);
            server.System<FlammableSystem>().Ignite(serverCandle, serverCandle);
        });
        await pair.RunTicksSync(10);

        var candle = pair.ToClientUid(serverCandle);
        EntityUid light = default;
        await client.WaitAssertion(() =>
        {
            client.System<AppearanceSystem>().FrameUpdate(0);
            var visuals = client.EntMan.GetComponent<FireVisualsComponent>(candle);
            Assert.That(visuals.LightEntity, Is.Not.Null);
            light = visuals.LightEntity!.Value;
            Assert.That(client.EntMan.HasComponent<PointLightComponent>(light));
            Assert.That(client.EntMan.GetComponent<TransformComponent>(light).ParentUid, Is.EqualTo(candle));
        });

        await server.WaitPost(() => server.System<FlammableSystem>().Extinguish(serverCandle));
        await pair.RunTicksSync(10);
        await client.WaitAssertion(() =>
        {
            client.System<AppearanceSystem>().FrameUpdate(0);
            Assert.That(client.EntMan.GetComponent<FireVisualsComponent>(candle).LightEntity, Is.Null);
            Assert.That(client.EntMan.EntityExists(light), Is.False);
        });
        await pair.CleanReturnAsync();
    }
}
