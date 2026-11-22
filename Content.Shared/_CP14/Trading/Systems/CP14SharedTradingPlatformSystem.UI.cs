using Content.Shared._CP14.Trading.Prototypes;

namespace Content.Shared._CP14.Trading.Systems;

public abstract partial class CP14SharedTradingPlatformSystem
{
    public string GetTradeDescription(CP14TradingPositionPrototype position)
    {
        if (position.Desc != null)
            return Loc.GetString(position.Desc);

        if (position.Service is null)
            return string.Empty;

        return position.Service.GetDesc(Proto);
    }

    public string GetTradeName(CP14TradingPositionPrototype position)
    {
        if (position.Name != null)
            return Loc.GetString(position.Name);

        if (position.Service is null)
            return string.Empty;

        return position.Service.GetName(Proto);
    }
}
