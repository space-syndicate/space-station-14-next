using Content.Server.Speech.EntitySystems;
using Content.Shared.Whitelist;

namespace Content.Server.Speech.Components;

[RegisterComponent]
[Access(typeof(ParrotSpeechSystem))]
public sealed partial class ParrotSpeechComponent : Component
{
    /// <summary>
    /// The maximum number of words the parrot can learn per phrase.
    /// Phrases are 1 to MaxPhraseLength words in length.
    /// </summary>
    [DataField]
    public int MaximumPhraseLength = 7;

    [DataField]
    public int MaximumPhraseCount = 20;

    [DataField]
    public int MinimumWait = 60; // 1 minutes

    [DataField]
    public int MaximumWait = 120; // 2 minutes

    /// <summary>
    /// The probability that a parrot will learn from something an overheard phrase.
    /// </summary>
    [DataField]
    public float LearnChance = 0.2f;

    [DataField]
    public EntityWhitelist Blacklist { get; private set; } = new();

    [DataField]
    public TimeSpan? NextUtterance;

    [DataField(readOnly: true)]
    public List<string> LearnedPhrases = new();
}
