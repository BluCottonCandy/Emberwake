using Content.IntegrationTests.Tests.Interaction;
using Content.Shared._CP14.Currency;

namespace Content.IntegrationTests.Tests._CP14;

public sealed class CP14CashConverterTest : InteractionTest
{
    [Test]
    public async Task DepositsCreditCoinStacksAndLeaveOtherItemsIntact()
    {
        var converter = await SpawnTarget("CP14CashConverter");
        var silver = await PlaceInHands("CP14SilverCoin", 5);
        await Interact();
        AssertDeleted(silver);
        Assert.That(Comp<CP14CurrencyConverterComponent>(converter).Balance, Is.EqualTo(50));

        var copper = await PlaceInHands("CP14CopperCoin", 1);
        await Interact();
        AssertDeleted(copper);
        Assert.That(Comp<CP14CurrencyConverterComponent>(converter).Balance, Is.EqualTo(51));

        var otherItem = await PlaceInHands("FoodEgg");
        await Interact();
        AssertExists(otherItem);
        Assert.That(Comp<CP14CurrencyConverterComponent>(converter).Balance, Is.EqualTo(51));
    }
}
