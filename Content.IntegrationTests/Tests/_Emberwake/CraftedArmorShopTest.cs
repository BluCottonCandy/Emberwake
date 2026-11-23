using System.Linq;
using Content.Client._CP14.Trading;
using Content.IntegrationTests.Tests.Interaction;
using Content.Server._CP14.Trading;
using Content.Server.Station.Events;
using Content.Shared._CP14.Trading.Components;
using Content.Shared.Station.Components;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Emberwake;

public sealed class CraftedArmorShopTest : InteractionTest
{
    [Test]
    public async Task BuyEachArmorPieceFromTheMerchantCatalogue()
    {
        var platform = await SpawnTarget("CP14TradingPlatformPublic");
        var coords = SEntMan.GetCoordinates(TargetCoords);
        await Server.WaitPost(() =>
        {
            var uid = ToServer(platform);
            SEntMan.EnsureComponent<CP14StationEconomyComponent>(uid);
            var station = SEntMan.EnsureComponent<StationDataComponent>(uid);
            var startMarket = new StationPostInitEvent((uid, station));
            SEntMan.EventBus.RaiseLocalEvent(uid, ref startMarket);
            SEntMan.EnsureComponent<CP14TradingReputationComponent>(SPlayer);
            var trading = Server.System<CP14TradingPlatformSystem>();
            trading.AddReputation(SPlayer, "Tailors", 0);
            trading.AddReputation(SPlayer, "HelmirChild", 1);
        });

        await SpawnEntity(("CP14PlatinumCoin", 8), coords);
        await RunSeconds(1);
        await Activate();
        await RunTicks(5);
        var window = GetWindow<CP14TradingPlatformWindow>();
        var search = window.FindControl<LineEdit>("Search");
        var stock = window.FindControl<BoxContainer>("Stock");
        var buy = window.FindControl<Button>("BuyButton");
        string[] products =
        [
            "EWClothingOuterWayfarerJack", "EWClothingHeadWayfarerCap",
            "EWClothingOuterRingwatchHauberk", "EWClothingHeadRingwatchCoif",
            "EWClothingOuterGatewardPlate", "EWClothingHeadGatewardSallet",
            "EWClothingOuterTideglassLamellar", "EWClothingHeadTideglassHelm",
        ];

        foreach (var product in products)
        {
            await Client.WaitAssertion(() =>
            {
                search.SetText(ProtoMan.Index<EntityPrototype>(product).Name, true);
                Assert.That(stock.Children.Count(row => row.Visible), Is.EqualTo(1), product);
                Assert.That(buy.Disabled, Is.False, product);
            });
            await ClickControl(buy);
            await RunSeconds(1.5f);
            await Server.WaitAssertion(() =>
            {
                var nearby = Server.System<EntityLookupSystem>().GetEntitiesInRange(coords, 2);
                Assert.That(nearby.Count(e => SEntMan.GetComponent<MetaDataComponent>(e).EntityPrototype?.ID == product),
                    Is.EqualTo(1), product);
            });
        }
    }
}
