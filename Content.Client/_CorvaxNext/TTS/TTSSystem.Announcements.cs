using Content.Shared._CorvaxNext;
using Robust.Client.ResourceManagement;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Corvax.TTS;

public sealed partial class TTSSystem
{
    private static TimeSpan _endTime;

    private static bool _isPlaying = false;
    private static bool _isPlayingIntro = false;
    private static bool _isPlayingTTS = false;

    private static SoundSpecifier? _endSound = new SoundPathSpecifier("/Audio/_CorvaxNext/TTS/comms_end.ogg");
    private static SoundSpecifier? _startSound = new SoundPathSpecifier("/Audio/_CorvaxNext/TTS/comms_start.ogg");

    private static TimeSpan _startSoundLength;
    private static byte[]? _currentData;
    private static TimeSpan _startedAt;

    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_isPlaying)
            return;

        if (_isPlayingIntro)
        {
            if (_timing.CurTime > _startedAt + _startSoundLength - TimeSpan.FromSeconds(3.5))
            {
                _isPlayingIntro = false;

                PlayTTS();
            }

            return;
        }

        if (_isPlayingTTS)
        {
            if (_timing.CurTime > _endTime)
            {
                _isPlayingTTS = false;

                _audio.PlayGlobal(_endSound, Filter.Local(), true, AudioParams.Default.WithVolume(AdjustVolume(false) + 3));
            }

            return;
        }

        _isPlaying = false;
    }

    private void PlayTTS()
    {
        var filePath = new ResPath($"{_fileIdx++}.ogg");
        _contentRoot.AddOrUpdateFile(filePath, _currentData!);

        var audioResource = new AudioResource();
        audioResource.Load(IoCManager.Instance!, Prefix / filePath);

        var audioParams = AudioParams.Default
            .WithVolume(AdjustVolume(false) + 2);

        var soundSpecifier = new SoundPathSpecifier(Prefix / filePath);

        var noiseParams = AudioParams.Default
            .WithLoop(true);


        try // I really don't sure about method SetEffect so I just put it into try-catch
        {
            var mainSound = _audio.PlayGlobal(soundSpecifier, Filter.Local(), true, audioParams);

            _audio.SetEffect(mainSound!.Value.Entity, mainSound.Value.Component, "TTSCommunication");
        }
        catch (Exception ex)
        {
            _sawmill.Error($"Failed to apply audio effect: {ex}");
        }

        _endTime = _audio.GetAudioLength(_audio.ResolveSound(soundSpecifier)) + _timing.CurTime + TimeSpan.FromSeconds(0.5);
        _isPlayingTTS = true;

        _contentRoot.RemoveFile(filePath);
    }

    private void OnAnnounced(TTSAnnouncedEvent args)
    {
        if (_isPlaying)
            return;

        _sawmill.Verbose($"Play TTS audio {args.Data.Length} bytes from station announcement");

        _currentData = args.Data;

        _audio.PlayGlobal(_startSound, Filter.Local(), true, AudioParams.Default.WithVolume(AdjustVolume(false) + 3));
        _startedAt = _timing.CurTime;
        _isPlaying = true;
        _isPlayingIntro = true;
    }
}
