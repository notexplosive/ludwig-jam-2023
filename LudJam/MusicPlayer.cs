using System;
using ExplogineCore.Data;
using ExplogineMonoGame;
using ExTween;
using Microsoft.Xna.Framework.Audio;

namespace LudJam;

public class MusicPlayer
{
    private const float FullVolume = 1f;
    private const float NoVolume = 0f;
    private readonly SequenceTween _faderTween = new();
    private bool _firstTouch;
    private SoundEffectInstance _lowTrack = null!;
    private readonly TweenableFloat _lowVolumeTweenable = new();
    private SoundEffectInstance _mainTrack = null!;
    private readonly TweenableFloat _mainVolumeTweenable = new();
    public Wrapped<int> VolumeInt { get; set; } = new(10);
    public float Volume => VolumeInt.Value / 20f;

    public void FreshStart(bool isMain)
    {
        _faderTween.Clear();

        if (isMain)
        {
            _mainVolumeTweenable.Value = MusicPlayer.FullVolume;
            _lowVolumeTweenable.Value = MusicPlayer.NoVolume;
        }
        else
        {
            _mainVolumeTweenable.Value = MusicPlayer.NoVolume;
            _lowVolumeTweenable.Value = MusicPlayer.FullVolume;
        }

        RestartBoth();
    }

    private void RestartBoth()
    {
        _mainTrack.Stop();
        _lowTrack.Stop();

        _mainTrack.Play();
        _lowTrack.Play();
    }

    public void FadeToLow(float fadeTime = 0.5f)
    {
        if (IsFirstTouch())
        {
            FreshStart(false);
        }

        _faderTween.Clear();
        _faderTween.Add(
            new MultiplexTween()
                .AddChannel(_lowVolumeTweenable.TweenTo(MusicPlayer.FullVolume, fadeTime, Ease.Linear))
                .AddChannel(_mainVolumeTweenable.TweenTo(MusicPlayer.NoVolume, fadeTime, Ease.Linear))
        );
    }

    public void FadeToOff(float fadeTime = 0.5f)
    {
        _faderTween.Clear();
        _faderTween.Add(
            new MultiplexTween()
                .AddChannel(_lowVolumeTweenable.TweenTo(MusicPlayer.NoVolume, fadeTime, Ease.Linear))
                .AddChannel(_mainVolumeTweenable.TweenTo(MusicPlayer.NoVolume, fadeTime, Ease.Linear))
        );
    }

    public void FadeToMain(float fadeTime = 0.5f)
    {
        if (IsFirstTouch())
        {
            FreshStart(true);
        }

        _faderTween.Clear();
        _faderTween.Add(
            new MultiplexTween()
                .AddChannel(_lowVolumeTweenable.TweenTo(MusicPlayer.NoVolume, fadeTime, Ease.Linear))
                .AddChannel(_mainVolumeTweenable.TweenTo(MusicPlayer.FullVolume, fadeTime, Ease.Linear))
        );
    }

    public void UpdateTween(float dt)
    {
        _faderTween.Update(dt);
        _mainTrack.Volume = _mainVolumeTweenable * Volume;
        _lowTrack.Volume = _lowVolumeTweenable * Volume;
    }

    private bool IsFirstTouch()
    {
        var result = _firstTouch;
        _firstTouch = false;
        return result;
    }

    public void Initialize()
    {
        _firstTouch = true;

        _mainTrack = Client.Assets.GetSoundEffectInstance("cat/crashtroid-xenon");
        _lowTrack = Client.Assets.GetSoundEffectInstance("cat/crashtroid-xenon-low");

        _mainTrack.Volume = MusicPlayer.NoVolume;
        _lowTrack.Volume = MusicPlayer.NoVolume;

        _mainTrack.IsLooped = true;
        _lowTrack.IsLooped = true;
    }

    public void SetLowPercent(float pullPercent)
    {
        pullPercent = Math.Clamp(pullPercent, 0f, 1f);
        _mainVolumeTweenable.Value = 1 - pullPercent;
        _lowVolumeTweenable.Value = pullPercent;
    }
}
