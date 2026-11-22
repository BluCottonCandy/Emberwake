using System.Linq;
using Content.Client.Inventory;
using Content.Server.Station.Systems;
using Content.Shared._Emberwake.Medieval;
using Content.Shared.GameTicking;
using Content.Shared.Inventory;
using Content.Shared.Paper;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Stacks;
using Content.Shared.Storage;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Emberwake;

[TestFixture]
public sealed class EquipmentHotbarTest
{
    [Test]
    public async Task TownJobsStartWithTheirFilledMoneyPouch()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true });
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        var entities = server.EntMan;
        var spawning = server.System<StationSpawningSystem>();
        var inventory = server.System<InventorySystem>();
        var prototypes = server.ResolveDependency<IPrototypeManager>();
        EntityUid merchant = default, merchantPouch = default;

        await server.WaitAssertion(() =>
        {
            var jobs = prototypes.EnumeratePrototypes<JobPrototype>()
                .Where(job => job.ID.StartsWith("CP14") && job.StartingGear != null).ToArray();
            Assert.That(jobs.Select(job => job.ID), Does.Contain("CP14Adventurer"));
            Assert.That(jobs.Select(job => job.ID), Does.Contain("CP14Merchant"));

            foreach (var job in jobs)
            {
                var profile = HumanoidCharacterProfile.DefaultWithSpecies("CP14Human");
                var wearer = spawning.SpawnPlayerMob(map.GridCoords, job.ID, profile, null);
                var joined = new PlayerSpawnCompleteEvent(wearer, pair.Player!, job.ID, true, true, 1,
                    map.Grid.Owner, profile);
                entities.EventBus.RaiseLocalEvent(wearer, joined, true);
                Assert.That(inventory.TryGetSlotEntity(wearer, "passport", out _), job.ID);
                Assert.That(inventory.TryGetSlotEntity(wearer, "pocket1", out var pouch), job.ID);
                Assert.That(entities.GetComponent<MetaDataComponent>(pouch!.Value).EntityPrototype?.ID,
                    Is.EqualTo("CP14WalletFilledTest"), job.ID);
                var coins = entities.GetComponent<StorageComponent>(pouch.Value).Container.ContainedEntities
                    .Select(entities.GetComponent<StackComponent>).ToArray();
                Assert.That(coins.Where(s => s.StackTypeId == "CP14SilverCoin").Sum(s => s.Count), Is.EqualTo(7), job.ID);
                Assert.That(coins.Where(s => s.StackTypeId == "CP14CopperCoin").Sum(s => s.Count), Is.EqualTo(30), job.ID);
                Assert.That(inventory.CanEquip(wearer, pouch.Value, "pocket2", out _), job.ID);

                if (job.ID == "CP14Merchant")
                {
                    merchant = wearer;
                    merchantPouch = pouch.Value;
                    server.PlayerMan.SetAttachedEntity(pair.Player!, merchant);
                }
                else
                    entities.DeleteEntity(wearer);
            }
        });

        await pair.RunTicksSync(10);
        await pair.Client.WaitAssertion(() =>
        {
            var clientInventory = pair.Client.System<ClientInventorySystem>();
            var wearer = pair.ToClientUid(merchant);
            Assert.That(clientInventory.TryGetSlotEntity(wearer, "pocket1", out var visiblePouch));
            Assert.That(visiblePouch, Is.EqualTo(pair.ToClientUid(merchantPouch)));
            clientInventory.UIInventoryStorageActivate("pocket1");
        });
        await pair.RunTicksSync(10);
        await server.WaitAssertion(() =>
            Assert.That(server.System<SharedUserInterfaceSystem>().IsUiOpen(merchantPouch, StorageComponent.StorageUiKey.Key, merchant)));

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task BothSlingsAcceptTheArmoryAndLeaveRoomForTravelGear()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true });
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        var inventory = server.System<InventorySystem>();
        var armory = pair.GetPrototypesWithComponent<MedievalWeaponVisualsComponent>();

        await server.WaitAssertion(() =>
        {
            EntityUid Spawn(string id) => server.EntMan.SpawnEntity(id, map.GridCoords);

            foreach (var race in new[] { "CP14Human", "CP14Carcat" })
            {
                var slots = server.ResolveDependency<IPrototypeManager>().Index<InventoryTemplatePrototype>(race).Slots;
                Assert.That(slots.Where(s => s.SlotGroup == "SecondHotbar").Select(s => s.Name),
                    Is.EqualTo(new[] { "passport", "keys", "back", "neck" }));
                Assert.That(slots.Where(s => s.SlotGroup == "MainHotbar").Select(s => s.Name),
                    Is.EqualTo(new[] { "pocket1", "pocket2", "weaponBack" }));

                var wearer = Spawn(race == "CP14Human" ? "CP14MobHuman" : "CP14MobCarcat");
                void Equip(string id, string slot)
                {
                    var item = Spawn(id);
                    Assert.That(inventory.TryEquip(wearer, item, slot, silent: true), $"{id} in {slot}");
                    Assert.That(inventory.TryGetSlotEntity(wearer, slot, out var stowed) && stowed == item);
                }

                Equip("CP14ClothingPantsBrown", "pants");
                Equip("CP14WalletFilledMerchant", "belt");
                Equip("CP14KeyRingMerchant", "keys");
                Equip("CP14ClothingBackBackpackT1", "back");
                Equip("CP14CopperCoin", "pocket1");
                Equip("CP14SilverCoin", "pocket2");

                var profile = HumanoidCharacterProfile.DefaultWithSpecies(race);
                var joined = new PlayerSpawnCompleteEvent(wearer, pair.Player!, "CP14Merchant", false, true, 1,
                    map.Grid.Owner, profile);
                server.EntMan.EventBus.RaiseLocalEvent(wearer, joined, true);
                Assert.That(inventory.TryGetSlotEntity(wearer, "passport", out var passport));
                Assert.That(server.EntMan.GetComponent<PaperComponent>(passport!.Value).Content, Does.Contain(profile.Name));

                var weaponIds = armory.Select(x => x.Item1.ID).Concat(new[]
                {
                    "CP14WeaponDaggerIron", "CP14WeaponSwordIron", "CP14BowCombat", "CP14WeaponTowerShieldWooden",
                    "CP14MagicGuardElectroStaff", "CP14ClothingBeltQuiverArrows", "CP14ClothingBeltQuiver", "ClothingBeltQuiver",
                });
                Assert.That(armory.Count, Is.GreaterThanOrEqualTo(141));
                foreach (var id in weaponIds)
                {
                    var item = Spawn(id);
                    foreach (var slot in new[] { "neck", "weaponBack" })
                        Assert.That(inventory.CanEquip(wearer, item, slot, out _), $"{race}: {id} in {slot}");
                    server.EntMan.DeleteEntity(item);
                }

                var wallet = Spawn("CP14Wallet");
                foreach (var slot in new[] { "passport", "neck", "weaponBack" })
                    Assert.That(inventory.CanEquip(wearer, wallet, slot, out _), Is.False, slot);

                Equip("CP14ClothingBeltQuiverArrows", "neck");
                Equip("EWMedievalIronRoundShield", "weaponBack");
                Assert.That(inventory.TryUnequip(wearer, "neck", silent: true));
                Assert.That(inventory.TryUnequip(wearer, "weaponBack", silent: true));
                Equip("EWMedievalLongsword", "neck");
                Equip("CP14ClothingBeltQuiverArrows", "weaponBack");
            }
        });

        await pair.CleanReturnAsync();
    }
}
