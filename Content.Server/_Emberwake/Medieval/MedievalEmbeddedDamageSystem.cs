using Content.Server.Damage.Components;
using Content.Shared.Damage;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Projectiles;
using Robust.Shared.Timing;

namespace Content.Server._Emberwake.Medieval;

/// <summary>Thrown blades continue to deal a fraction of their impact damage while embedded.</summary>
[RegisterComponent]
public sealed partial class MedievalEmbeddedDamageComponent : Component
{
    [DataField]
    public float Multiplier = 0.05f;
}

public sealed class MedievalEmbeddedDamageSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    private TimeSpan _nextTick;

    public override void Update(float frameTime)
    {
        if (_timing.CurTime < _nextTick)
            return;

        _nextTick = _timing.CurTime + TimeSpan.FromSeconds(1);
        var query = EntityQueryEnumerator<MedievalEmbeddedDamageComponent, EmbeddableProjectileComponent, DamageOtherOnHitComponent>();
        while (query.MoveNext(out _, out var effect, out var embed, out var impact))
        {
            if (embed.EmbeddedIntoUid is not { } target ||
                !TryComp<MobStateComponent>(target, out var mob) || mob.CurrentState == MobState.Dead)
                continue;

            _damage.TryChangeDamage(target, impact.Damage * effect.Multiplier);
        }
    }
}
