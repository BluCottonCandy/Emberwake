#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.Shared._CP14.Workbench.Prototypes;
using Content.Shared._Emberwake.Medieval;
using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Wieldable;
using Content.Shared.Wieldable.Components;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests._Emberwake;

[TestFixture]
public sealed class MedievalArmoryTest
{
    [Test]
    public async Task ArmorAndAmmunitionCanBeCraftedAndSpawned()
    {
        await using var pair = await PoolManager.GetServerClient();
        var map = await pair.CreateTestMap();
        var em = pair.Server.EntMan;
        var protos = pair.Server.ResolveDependency<IPrototypeManager>();
        await pair.Server.WaitAssertion(() =>
        {
            var recipes = protos.EnumeratePrototypes<CP14WorkbenchRecipePrototype>()
                .Where(r => r.ID.StartsWith("EWClothing") || r.ID == "EWMedievalMusketBall").ToArray();
            Assert.That(recipes, Has.Length.GreaterThanOrEqualTo(36));
            foreach (var recipe in recipes)
            {
                var item = em.SpawnEntity(recipe.Result, map.GridCoords);
                Assert.That(recipe.Requirements, Is.Not.Empty, recipe.ID);
                if (recipe.ID.StartsWith("EWClothing"))
                    Assert.That(em.GetComponent<ClothingComponent>(item).Slots, Is.Not.EqualTo(SlotFlags.NONE), recipe.ID);
                else
                    Assert.That(em.HasComponent<CartridgeAmmoComponent>(item), recipe.ID);
                em.DeleteEntity(item);
            }
        });
        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task ChargedAndLoadedWeaponsRetainTheirModesWhenUnwielded()
    {
        await using var pair = await PoolManager.GetServerClient();
        var map = await pair.CreateTestMap();
        var em = pair.Server.EntMan;
        await pair.Server.WaitAssertion(() =>
        {
            var wearer = em.SpawnEntity("CP14MobHuman", map.GridCoords);
            var sword = em.SpawnEntity("EWMedievalSwordOfState", map.GridCoords);
            var hands = em.System<SharedHandsSystem>();
            var wield = em.System<SharedWieldableSystem>();
            var toggle = em.System<ItemToggleSystem>();
            var swordWield = em.GetComponent<WieldableComponent>(sword);
            var swordItem = em.GetComponent<ItemComponent>(sword);
            Assert.That(hands.TryPickup(wearer, sword));
            Assert.That(wield.TryWield(sword, swordWield, wearer));
            Assert.That(toggle.IsActivated(sword), Is.False);
            Assert.That(toggle.TryActivate(sword, wearer));
            Assert.That(swordItem.HeldPrefix, Is.EqualTo("blaze-wielded"));
            Assert.That(wield.TryUnwield(sword, swordWield, wearer));
            Assert.That(swordItem.HeldPrefix, Is.EqualTo("lit"));
            Assert.That(hands.TryDrop(wearer, sword));

            var bow = em.SpawnEntity("EWMedievalCrossbow", map.GridCoords);
            var bolt = em.SpawnEntity("CP14CrossboltIron", map.GridCoords);
            var bowItem = em.GetComponent<ItemComponent>(bow);
            Assert.That(hands.TryPickup(wearer, bow));
            var storage = em.System<SharedContainerSystem>();
            var chamber = storage.GetContainer(bow, "projectiles");
            Assert.That(storage.Insert(bolt, chamber), Is.False);
            em.System<CrossbowSystem>().SetSpanned(bow, em.GetComponent<CrossbowComponent>(bow), true);
            Assert.That(storage.Insert(bolt, chamber));
            Assert.That(bowItem.HeldPrefix, Is.EqualTo("loaded"));
            var bowWield = em.GetComponent<WieldableComponent>(bow);
            Assert.That(wield.TryWield(bow, bowWield, wearer));
            Assert.That(bowItem.HeldPrefix, Is.EqualTo("loaded-wielded"));
            Assert.That(wield.TryUnwield(bow, bowWield, wearer));
            Assert.That(bowItem.HeldPrefix, Is.EqualTo("loaded"));
            Assert.That(storage.Remove(bolt, chamber));
            Assert.That(bowItem.HeldPrefix, Is.Null);
        });
        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task SwitchingHandsWieldingAndSlungCarryReordersLayers()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true });
        var map = await pair.CreateTestMap();
        var server = pair.Server;
        var hands = server.System<SharedHandsSystem>();
        var inventory = server.System<InventorySystem>();
        var wield = server.System<SharedWieldableSystem>();
        var em = server.EntMan;
        EntityUid wearer = default, sword = default;
        await server.WaitPost(() =>
        {
            wearer = em.SpawnEntity("CP14MobHuman", map.GridCoords);
            sword = em.SpawnEntity("EWMedievalLongsword", map.GridCoords);
            server.PlayerMan.SetAttachedEntity(pair.Player!, wearer);
        });
        await pair.RunTicksSync(10);
        var clientWearer = pair.ToClientUid(wearer);

        async Task CheckLayers(string? key, string? retired = null)
        {
            await pair.RunTicksSync(10);
            await pair.Client.WaitAssertion(() =>
            {
                var sprite = pair.Client.EntMan.GetComponent<SpriteComponent>(clientWearer);
                var sprites = pair.Client.System<SpriteSystem>();
                Assert.That(sprites.LayerMapTryGet((clientWearer, sprite), HumanoidVisualLayers.Chest, out var body, false));
                if (key != null)
                {
                    Assert.That(sprites.LayerMapTryGet((clientWearer, sprite), key, out var front, false), key);
                    Assert.That(sprites.LayerMapTryGet((clientWearer, sprite), key + "-behind", out var rear, false), key);
                    Assert.That(front, Is.GreaterThan(body));
                    Assert.That(rear, Is.LessThan(body));
                }
                if (retired != null)
                    Assert.That(sprites.LayerMapTryGet((clientWearer, sprite), retired + "-behind", out _, false), Is.False);
            });
        }

        foreach (var side in new[] { HandLocation.Left, HandLocation.Right })
        {
            var name = $"inhand-{side.ToString().ToLowerInvariant()}";
            await server.WaitAssertion(() =>
            {
                var handle = em.GetComponent<HandsComponent>(wearer).Hands.First(h => h.Value.Location == side).Key;
                Assert.That(hands.TryPickup(wearer, sword, handle));
            });
            await CheckLayers(name);
            await server.WaitAssertion(() => Assert.That(wield.TryWield(sword, em.GetComponent<WieldableComponent>(sword), wearer)));
            await CheckLayers("wielded-" + name, name);
            await server.WaitAssertion(() => Assert.That(wield.TryUnwield(sword, em.GetComponent<WieldableComponent>(sword), wearer)));
            await CheckLayers(name, "wielded-" + name);
            await server.WaitAssertion(() => Assert.That(hands.TryDrop(wearer, sword)));
            await CheckLayers(null, name);
        }
        await server.WaitAssertion(() => Assert.That(inventory.TryEquip(wearer, sword, "back", silent: true)));
        await CheckLayers("back-0");
        await server.WaitAssertion(() => Assert.That(inventory.TryUnequip(wearer, "back", out _, silent: true)));
        await CheckLayers(null, "back-0");
        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task CarryArtAndRecipes()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true });
        var client = pair.Client;
        var weapons = pair.GetPrototypesWithComponent<MedievalWeaponVisualsComponent>();
        var cache = client.ResolveDependency<IResourceCache>();
        var protos = client.ResolveDependency<IPrototypeManager>();
        var em = client.EntMan;

        await client.WaitAssertion(() =>
        {
            var wearer = em.Spawn("CP14MobHumanDummy");
            var itemSystem = client.System<SharedItemSystem>();
            Assert.That(weapons.Count, Is.GreaterThanOrEqualTo(141));
            foreach (var (proto, _) in weapons)
            {
                Assert.That(protos.Index<CP14WorkbenchRecipePrototype>(proto.ID).Result.ToString(), Is.EqualTo(proto.ID));
                var uid = em.Spawn(proto.ID);
                var item = em.GetComponent<ItemComponent>(uid);
                var art = cache.GetResource<RSIResource>(new ResPath("/Textures") / item.RsiPath!).RSI;
                var prefixes = new List<string?> { null };
                var wielded = em.HasComponent<WieldableComponent>(uid);
                if (wielded)
                    prefixes.Add("wielded");
                if (em.TryGetComponent<MedievalLookComponent>(uid, out var toggle) && toggle.OnState != null)
                {
                    prefixes.Add(toggle.OnState);
                    if (wielded)
                        prefixes.Add($"{toggle.WieldedOnState ?? toggle.OnState}-wielded");
                }
                if (em.TryGetComponent<MedievalLoadedLookComponent>(uid, out var loaded))
                {
                    prefixes.Add(loaded.State);
                    if (wielded)
                        prefixes.Add($"{loaded.State}-wielded");
                }

                foreach (var prefix in prefixes)
                {
                    itemSystem.SetHeldPrefix(uid, prefix);
                    foreach (var hand in new[] { HandLocation.Left, HandLocation.Right })
                    {
                        var ev = new GetInhandVisualsEvent(wearer, hand);
                        em.EventBus.RaiseLocalEvent(uid, ev);
                        Assert.That(ev.Layers, Has.Count.EqualTo(2), $"{proto.ID} {prefix} {hand}");
                        foreach (var (_, layer) in ev.Layers)
                            Assert.That(art.TryGetState(layer.State!, out _), $"{proto.ID} {layer.State}");

                        Assert.That(ev.BehindBodyLayer, Is.EqualTo(ev.Layers[1].Item1));
                    }
                }

                var slots = em.TryGetComponent<ClothingComponent>(uid, out var clothing)
                    ? clothing.Slots : SlotFlags.NONE;
                foreach (var (flag, slot) in new[] { (SlotFlags.BACK, "back"), (SlotFlags.BELT, "belt"),
                             (SlotFlags.BELT, "belt2"), (SlotFlags.GLOVES, "gloves") })
                {
                    if (!slots.HasFlag(flag))
                        continue;
                    var ev = new GetEquipmentVisualsEvent(wearer, slot);
                    em.EventBus.RaiseLocalEvent(uid, ev);
                    Assert.That(ev.Layers, Has.Count.EqualTo(slot == "back" ? 2 : 1), $"{proto.ID} {slot}");
                    foreach (var (_, layer) in ev.Layers)
                        Assert.That(art.TryGetState(layer.State!, out _), $"{proto.ID} {slot} {layer.State}");
                    if (slot == "back")
                        Assert.That(ev.BehindBodyLayer, Is.EqualTo(ev.Layers[1].Item1));
                }
                em.DeleteEntity(uid);
            }
            em.DeleteEntity(wearer);
        });
        await pair.CleanReturnAsync();
    }
}
