using Content.Shared.Localizations;
using Robust.Client;

namespace Content.Client.Localization;

public sealed class ChangeLocaleOnConnectSystem : EntitySystem
{
    [Dependency] private readonly IBaseClient _client = default!;
    [Dependency] private readonly ContentLocalizationManager _contentLoc = default!;
    [Dependency] private readonly ILocalizationManager _loc = default!;

    public override void Initialize()
    {
        _client.PlayerJoinedServer += (sender, args) =>
        {
            _contentLoc.LoadLocalization();
        };
    }
}
