using System.Numerics;
using Content.Client.Clothing;
using Content.Client.Items.Systems;
using Content.Shared._Emberwake.Medieval;
using Content.Shared.Clothing;
using Content.Shared.Hands;

namespace Content.Client._Emberwake.Medieval;

public sealed class MedievalWeaponVisualsSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MedievalWeaponVisualsComponent, GetInhandVisualsEvent>(OnHeld, after: [typeof(ItemSystem)]);
        SubscribeLocalEvent<MedievalWeaponVisualsComponent, GetEquipmentVisualsEvent>(OnWorn, after: [typeof(ClientClothingSystem)]);
    }

    private static void OnHeld(EntityUid uid, MedievalWeaponVisualsComponent comp, GetInhandVisualsEvent args)
    {
        if (args.Layers.Count == 0)
            return;

        var (key, face) = args.Layers[0];
        face.Scale = new Vector2(comp.Scale);
        args.BehindBodyLayer = $"{key}-behind";
        args.Layers.Add((args.BehindBodyLayer, RearLayer(face)));
    }

    private static void OnWorn(EntityUid uid, MedievalWeaponVisualsComponent comp, GetEquipmentVisualsEvent args)
    {
        if (args.Layers.Count == 0)
            return;

        var (key, face) = args.Layers[0];
        face.Scale = new Vector2(comp.Scale);
        if (args.Slot != "back")
            return;

        args.BehindBodyLayer = $"{key}-behind";
        args.Layers.Add((args.BehindBodyLayer, RearLayer(face)));
    }

    private static PrototypeLayerData RearLayer(PrototypeLayerData face) => new()
    {
        RsiPath = face.RsiPath,
        State = $"{face.State}-behind",
        Scale = face.Scale,
    };
}
