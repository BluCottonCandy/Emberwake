namespace Content.Shared._Emberwake.Medieval;

/// <summary>Directional artwork for a weapon carried close to the body.</summary>
[RegisterComponent]
public sealed partial class MedievalWeaponVisualsComponent : Component
{
    [DataField]
    public float Scale = 0.5f;
}
