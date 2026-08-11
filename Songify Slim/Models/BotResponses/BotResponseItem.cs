using System;

namespace Songify_Slim.Models.BotResponses;

/// <summary>
/// One editable bot-response row in the Bot Responses settings UI.
/// </summary>
public sealed class BotResponseItem
{
    public BotResponseItem(
        string id,
        string titleResourceKey,
        string defaultText,
        Func<string> get,
        Action<string> set)
    {
        Id = id;
        TitleResourceKey = titleResourceKey;
        DefaultText = defaultText;
        Get = get ?? throw new ArgumentNullException(nameof(get));
        Set = set ?? throw new ArgumentNullException(nameof(set));
    }

    public string Id { get; }
    public string TitleResourceKey { get; }
    public string DefaultText { get; }
    public Func<string> Get { get; }
    public Action<string> Set { get; }
}
