using Content.Client.Stylesheets;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;

namespace Content.Client.UserInterface.Systems.Chat.Controls;

public sealed class ChannelSelectorItemButton : Button
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    public readonly ChatSelectChannel Channel;

    public bool IsHidden => Parent == null;

    public ChannelSelectorItemButton(ChatSelectChannel selector)
    {
        IoCManager.InjectDependencies(this);

        Channel = selector;
        AddStyleClass(StyleNano.StyleClassChatChannelSelectorButton);

        Text = ChannelSelectorButton.ChannelSelectorName(selector);
        _cfg.OnValueChanged(CCVars.CultureLocale, _ => Text = ChannelSelectorButton.ChannelSelectorName(selector));

        var prefix = ChatUIController.ChannelPrefixes[selector];

        if (prefix != default)
        {
            var text = Loc.GetString("hud-chatbox-select-name-prefixed", ("name", Text), ("prefix", prefix));
            Text = text;
            _cfg.OnValueChanged(CCVars.CultureLocale, _ => Text = text);
        }

    }
}
