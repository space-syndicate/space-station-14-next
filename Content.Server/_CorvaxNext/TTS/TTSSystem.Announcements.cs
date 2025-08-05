using Content.Server.Chat.Systems;
using Content.Server.Communications;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared._CorvaxNext;
using Content.Shared.Corvax.CCCVars;
using Content.Shared.Corvax.TTS;
using Content.Shared.Speech.Muting;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.Corvax.TTS;
public sealed partial class TTSSystem
{
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static bool _isPlaying;
    private static TimeSpan _sendTTSAt;

    private static byte[]? _soundDataToSend;
    private static Filter? _filterToSend;

    public override void Update(float frameTime)
    {
        if (!_isPlaying)
            return;

        if (_timing.CurTime < _sendTTSAt)
            return;

        if (_soundDataToSend is null || _filterToSend is null)
        {
            _isPlaying = false;
            return;
        }

        _isPlaying = false;

        foreach (var recipient in _filterToSend.Recipients) if (recipient.AttachedEntity.HasValue)
                RaiseNetworkEvent(new TTSAnnouncedEvent(_soundDataToSend!), _filterToSend!);

        _soundDataToSend = null;
        _filterToSend = null;
    }

    private void OnConsoleAnnouncement(ref CommunicationConsoleAnnouncementEvent ev)
    {
        if (!_cfg.GetCVar(CCCVars.TTSEnabled))
            return;

        if (_isPlaying)
            return;

        if (!TryComp<TTSComponent>(ev.Sender, out var ttsComp) || HasComp<MutedComponent>(ev.Sender))
            return;

        var voiceId = ttsComp.VoicePrototypeId;

        if (!_isEnabled ||
            ev.Text.Length > MaxMessageChars ||
            voiceId == null)
            return;

        if (!_prototypeManager.TryIndex<TTSVoicePrototype>(voiceId, out var protoVoice))
            return;

        if (ev.Component.Global)
            SendGlobalAnnouncement(ev.Text, protoVoice.Speaker);
        else
            SendStationAnnouncement(ev.Uid, ev.Text, protoVoice.Speaker);
    }

    async private void SendGlobalAnnouncement(string text, string voice)
    {
        SendAnnouncementTTS(Filter.Broadcast(), text, voice);
    }

    async private void SendStationAnnouncement(EntityUid consoleUid, string text, string voice)
    {
        var station = _stationSystem.GetOwningStation(consoleUid);

        if (station is null)
            return;

        if (!TryComp<StationDataComponent>(station, out var stationDataComp))
            return;

        SendAnnouncementTTS(_stationSystem.GetInStation(stationDataComp), text, voice, ChatSystem.CentComAnnouncementSound);
    }
    async private void SendAnnouncementTTS(Filter filter, string text, string voice, string announcementSound = ChatSystem.DefaultAnnouncementSound)
    {
        _filterToSend = filter;
        _isPlaying = true;
        _sendTTSAt = _timing.CurTime + _audio.GetAudioLength(_audio.ResolveSound(new SoundPathSpecifier(announcementSound)));

        _soundDataToSend = await GenerateTTS(text, voice);
    }

    public void SendTTSAdminAnnouncement(string text, string voice, string announcementSound = ChatSystem.DefaultAnnouncementSound)
    {
        if (_isPlaying)
            return;

        if (!_isEnabled ||
            text.Length > MaxMessageChars ||
            voice == "None")
            return;

        if (!_prototypeManager.TryIndex<TTSVoicePrototype>(voice, out var protoVoice))
            return;

        SendAnnouncementTTS(Filter.Broadcast(), text, protoVoice.Speaker, announcementSound);
    }
}
