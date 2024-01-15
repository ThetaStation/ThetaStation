using Content.Shared.CCVar;
using Content.Shared.Chat;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;

namespace Content.Client.UserInterface.Systems.Chat.Controls;

public sealed class ChannelFilterCheckbox : CheckBox
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public readonly ChatChannel Channel;

    public bool IsHidden => Parent == null;

    public ChannelFilterCheckbox(ChatChannel channel)
    {
        IoCManager.InjectDependencies(this);

        Channel = channel;
        var messageId = $"hud-chatbox-channel-{Channel}";
        Text = Loc.GetString(messageId);
        _cfg.OnValueChanged(CCVars.CultureLocale, _ => Text = Loc.GetString(messageId));
    }

    private void UpdateText(int? unread)
    {
        var name = Loc.GetString($"hud-chatbox-channel-{Channel}");

        if (unread > 0)
            // todo: proper fluent stuff here.
            name += " (" + (unread > 9 ? "9+" : unread) + ")";

        Text = name;
    }

    public void UpdateUnreadCount(int? unread)
    {
        UpdateText(unread);
    }
}
