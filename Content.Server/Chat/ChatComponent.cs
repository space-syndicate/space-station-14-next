namespace Content.Server.Chat;

/// <summary>
/// Modifaer for entity to expand whisper radius 
/// </summary>
[RegisterComponent]
public sealed partial class ChatComponent : Component
{
    [DataField("whisperPersonalRange")]
    public int WhisperPersonalRange = 2;
}
