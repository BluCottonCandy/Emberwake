using System.Linq;
using Content.Client._CP14.Trading;
using Content.IntegrationTests.Tests.Interaction;
using Content.Server._CP14.Trading;
using Content.Shared._CP14.Trading;
using Content.Shared._CP14.Trading.Components;
using Content.Shared.Stacks;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._CP14;

public sealed class CP14MerchantCatalogueTest : InteractionTest
{
    [Test]
    public async Task BrowseAndPurchaseFromPublicPlatform()
    {
        var platform = await SpawnTarget("CP14TradingPlatformPublic");
        var coords = SEntMan.GetCoordinates(TargetCoords);
        await Server.WaitPost(() =>
        {
            var market = SEntMan.EnsureComponent<CP14StationEconomyComponent>(ToServer(platform));
            market.Pricing["CP14BaseWrench"] = 7;
            market.Pricing["CP14WeaponPickaxeIron"] = 9;
            SEntMan.Dirty(ToServer(platform), market);
            SEntMan.EnsureComponent<CP14TradingReputationComponent>(SPlayer);
            Server.System<CP14TradingPlatformSystem>().AddReputation(SPlayer, "DwarfMining", 0);
        });
        await RunTicks(5);
        await Activate();
        await RunTicks(5);
        var window = GetWindow<CP14TradingPlatformWindow>();
        var stock = window.FindControl<BoxContainer>("Stock");
        var search = window.FindControl<LineEdit>("Search");
        var buy = window.FindControl<Button>("BuyButton");
        var affordable = window.FindControl<CheckBox>("AvailableOnly");
        int VisibleItems() => stock.Children.Count(row => row.Visible);

        await Client.WaitAssertion(() =>
        {
            var allItems = VisibleItems();
            Assert.That(allItems, Is.Positive);
            search.SetText("dWaRf StEeL", true);
            Assert.That(VisibleItems(), Is.InRange(1, allItems - 1));
            search.SetText(string.Empty, true);
            Assert.That(VisibleItems(), Is.EqualTo(allItems));
            search.SetText("no matching item anywhere", true);
            Assert.That(VisibleItems(), Is.Zero);
            Assert.That(buy.Disabled);
            search.SetText("  WrEnCh  ", true);
            Assert.That(VisibleItems(), Is.EqualTo(1));
            Assert.That(buy.Disabled);
        });

        var coins = await SpawnEntity(("CP14SilverCoin", 1), coords);
        await RunSeconds(1);
        await Client.WaitAssertion(() => Assert.That(buy.Disabled, "A 10.5 copper purchase needs 11 copper."));
        await Server.WaitPost(() => Stack.SetCount(coins, 2));
        await RunSeconds(1);
        await Client.WaitAssertion(() =>
        {
            Assert.That(buy.Disabled, Is.False);
            search.SetText("iron pickaxe", true);
            Assert.That(VisibleItems(), Is.EqualTo(1));
            Assert.That(buy.Disabled, "A funded order still requires supplier reputation.");
        });
        await ClickControl(affordable);
        Assert.That(VisibleItems(), Is.Zero);
        await Client.WaitPost(() => search.SetText("wrench", true));
        Assert.That(VisibleItems(), Is.EqualTo(1));
        await ClickControl(buy);
        await RunSeconds(1);
        await Server.WaitAssertion(() =>
        {
            var nearby = Server.System<EntityLookupSystem>().GetEntitiesInRange(coords, 2);
            Assert.That(nearby.Count(e => SEntMan.GetComponent<MetaDataComponent>(e).EntityPrototype?.ID == "CP14BaseWrench"), Is.EqualTo(1));
            Assert.That(SEntMan.Deleted(coins));
            Assert.That(nearby.Where(e => SEntMan.TryGetComponent<StackComponent>(e, out var stack)
                    && stack.StackTypeId == "CP14CopperCoin")
                .Sum(e => SEntMan.GetComponent<StackComponent>(e).Count), Is.EqualTo(9));
        });
    }
}
