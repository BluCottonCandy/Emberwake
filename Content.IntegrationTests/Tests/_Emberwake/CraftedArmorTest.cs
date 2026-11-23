using System.Linq;
using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Cargo.Systems;
using Content.Shared._CP14.Workbench;
using Content.Shared._CP14.Workbench.Prototypes;
using Content.Shared.Inventory;
using Content.Shared.Stacks;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Emberwake;

public sealed class CraftedArmorTest : InteractionTest
{
    protected override string PlayerPrototype => "CP14MobHuman";

    [TestCase("CP14WorkbenchLeather", "EWClothingOuterWayfarerJack", "EWClothingHeadWayfarerCap",
        new[] { "CP14Leather", "CP14String", "CP14IronBuckle" }, new[] { 8, 6, 1 }, TestName = "CraftAndWearWayfarerLeather")]
    [TestCase("CP14WorkbenchAnvil", "EWClothingOuterRingwatchHauberk", "EWClothingHeadRingwatchCoif",
        new[] { "CP14IronBar", "CP14Cloth", "CP14Leather" }, new[] { 8, 3, 1 }, TestName = "CraftAndWearRingwatchMail")]
    [TestCase("CP14WorkbenchAnvil", "EWClothingOuterGatewardPlate", "EWClothingHeadGatewardSallet",
        new[] { "CP14IronBar", "CP14BrassBar", "CP14Leather" }, new[] { 12, 2, 3 }, TestName = "CraftAndWearGatewardPlate")]
    [TestCase("CP14WorkbenchAnvil", "EWClothingOuterTideglassLamellar", "EWClothingHeadTideglassHelm",
        new[] { "CP14MithrilBar", "CP14Leather", "CP14Cloth" }, new[] { 7, 3, 1 }, TestName = "CraftAndWearTideglassLamellar")]
    public async Task CraftAndWearArmorSet(string bench, string body, string head, string[] materials, int[] quantities)
    {
        await AddAtmosphere();
        await SpawnTarget(bench);
        var coords = SEntMan.GetCoordinates(TargetCoords);
        foreach (var (material, quantity) in materials.Zip(quantities))
        {
            var stackSize = ProtoMan.TryIndex<StackPrototype>(material, out var stack) ? stack.MaxCount ?? quantity : 1;
            for (var remaining = quantity; remaining > 0; remaining -= stackSize)
                await SpawnEntity((material, Math.Min(remaining, stackSize)), coords);
        }
        await RunTicks(5);
        await Activate();
        await RunTicks(5);

        foreach (var (item, slot) in new[] { (body, "outerClothing"), (head, "head") })
        {
            await Client.WaitAssertion(() =>
            {
                Assert.That(CUiSys.TryGetUiState<CP14WorkbenchUiRecipesState>(CEntMan.GetEntity(Target!.Value), CP14WorkbenchUiKey.Key, out var state));
                Assert.That(state!.Recipes.Any(entry => entry.ProtoId.Id == item && entry.Craftable), $"{bench} should offer {item} with the supplied ingredients.");
            });
            await SendBui(CP14WorkbenchUiKey.Key, new CP14WorkbenchUiCraftMessage(item));
            await AwaitDoAfters();
            await Server.WaitAssertion(() =>
            {
                var nearby = Server.System<EntityLookupSystem>().GetEntitiesInRange(coords, 2);
                var result = nearby.Single(uid => SEntMan.GetComponent<MetaDataComponent>(uid).EntityPrototype?.ID == item);
                var ingredientCost = ProtoMan.Index<CP14WorkbenchRecipePrototype>(item).Requirements.Sum(req => req.GetPrice(SEntMan, ProtoMan));
                Assert.That(Server.System<PricingSystem>().GetPrice(result), Is.InRange(ingredientCost, ingredientCost * 2));
                Assert.That(Server.System<InventorySystem>().TryEquip(SPlayer, result, slot, silent: true), item);
            });
            await RunTicks(5);
            await Client.WaitAssertion(() =>
                Assert.That(Client.System<SpriteSystem>().LayerMapTryGet(CPlayer, $"{slot}-0", out _, false), item));
        }
    }
}
