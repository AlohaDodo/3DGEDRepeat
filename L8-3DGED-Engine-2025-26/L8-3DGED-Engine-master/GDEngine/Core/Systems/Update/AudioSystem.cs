#nullable enable
using GDEngine.Core.Audio;
using GDEngine.Core.Collections;
using GDEngine.Core.Components;
using GDEngine.Core.Entities;
using GDEngine.Core.Enums;
using GDEngine.Core.Events;
using GDEngine.Core.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace GDEngine.Core.Audio
{
    /// <summary>
    /// Simple per-channel volume mixer with time-based fades.
    /// </summary>
    /// <see cref="AudioMixer.AudioChannel"/>
    public sealed class AudioMixer
    {
        #region Enums
        public enum AudioChannel : sbyte
        {
            Master = 0,
            Music = 1,
            Sfx = 2,
            Ui = 3
        }
        #endregion

        #region Static Fields
        #endregion

        #region Fields
        private readonly ChannelState[] _channels;
        #endregion

        #region Properties
        #endregion

        #region Constructors
        public AudioMixer()
        {
            _channels = new ChannelState[4];
            for (int i = 0; i < _channels.Length; i++)
            {
                _channels[i] = new ChannelState
                {
                    Volume = 1f,
                    Target = 1f,
                    FadeSpeed = 0f
                };
            }
        }
        #endregion

        #region Methods
        public float GetVolume(AudioChannel channel)
        {
            int index = (int)channel;
            return _channels[index].Volume;
        }

        public void SetVolume(AudioChannel channel, float volume)
        {
            int index = (int)channel;
            float v = MathHelper.Clamp(volume, 0f, 1f);
            ChannelState state = _channels[index];
            state.Volume = v;
            state.Target = v;
            state.FadeSpeed = 0f;
        }

        public void FadeTo(AudioChannel channel, float targetVolume, float durationSeconds)
        {
            int index = (int)channel;
            float t = MathHelper.Clamp(targetVolume, 0f, 1f);
            ChannelState state = _channels[index];

            state.Target = t;

            if (durationSeconds <= 0f)
            {
                state.Volume = t;
                state.FadeSpeed = 0f;
                return;
            }

            float distance = t - state.Volume;
            state.FadeSpeed = distance / durationSeconds;
        }

        public void Update(float deltaTime)
        {
            if (deltaTime <= 0f)
                return;

            for (int i = 0; i < _channels.Length; i++)
            {
                ChannelState state = _channels[i];
                if (state.FadeSpeed == 0f)
                    continue;

                float v = state.Volume + state.FadeSpeed * deltaTime;

                if ((state.FadeSpeed > 0f && v >= state.Target)
                    || (state.FadeSpeed < 0f && v <= state.Target))
                {
                    v = state.Target;
                    state.FadeSpeed = 0f;
                }

                state.Volume = MathHelper.Clamp(v, 0f, 1f);
            }
        }

        public float GetEffectiveVolume(AudioChannel channel, float localVolume)
        {
            float master = _channels[(int)AudioChannel.Master].Volume;
            float ch = _channels[(int)channel].Volume;
            float v = localVolume;
            if (v < 0f)
                v = 0f;
            if (v > 1f)
                v = 1f;

            return master * ch * v;
        }
        #endregion

        #region Lifecycle Methods
        #endregion

        #region Housekeeping Methods
        public override string ToString()
        {
            return "AudioMixer(Master=" + _channels[(int)AudioChannel.Master].Volume.ToString("0.00")
                + ", Music=" + _channels[(int)AudioChannel.Music].Volume.ToString("0.00")
                + ", Sfx=" + _channels[(int)AudioChannel.Sfx].Volume.ToString("0.00")
                + ", Ui=" + _channels[(int)AudioChannel.Ui].Volume.ToString("0.00") + ")";
        }
        #endregion

        #region Nested Types
        private sealed class ChannelState
        {
            public float Volume;
            public float Target;
            public float FadeSpeed;
        }
        #endregion
    }
}

namespace GDEngine.Core.Audio
{
    /// <summary>
    /// Event to request a one-shot sound effect.
    /// </summary>
    /// <see cref="GDEngine.Core.Audio.AudioMixer.AudioChannel"/>
    public sealed class PlaySfxEvent
    {
        #region Properties
        public string Clip { get; }
        public float Volume { get; }
        public bool Spatial { get; }
        public Transform? Emitter { get; }
        #endregion

        #region Constructors
        public PlaySfxEvent(
            string clip,
            float volume = 1.0f,
            bool spatial = false,
            Transform? emitter = null)
        {
            Clip = clip;
            Volume = volume;
            Spatial = spatial;
            Emitter = emitter;
        }
        #endregion
    }

    /// <summary>
    /// Event to request all currently playing sound effects to stop.
    /// </summary>
    public sealed class StopAllSfxEvent
    {
    }

    /// <summary>
    /// Event to request background music playback (with optional fade-in).
    /// </summary>
    public sealed class PlayMusicEvent
    {
        #region Properties
        public string Clip { get; }
        public float Volume { get; }
        public float FadeInSeconds { get; }
        #endregion

        #region Constructors
        public PlayMusicEvent(string clip, float volume = 1.0f, float fadeInSeconds = 0.0f)
        {
            Clip = clip;
            Volume = volume;
            FadeInSeconds = fadeInSeconds;
        }
        #endregion
    }

    /// <summary>
    /// Event to stop background music (optional fade-out).
    /// </summary>
    public sealed class StopMusicEvent
    {
        #region Properties
        public float FadeOutSeconds { get; }
        #endregion

        #region Constructors
        public StopMusicEvent(float fadeOutSeconds = 0.0f)
        {
            FadeOutSeconds = fadeOutSeconds;
        }
        #endregion
    }

    /// <summary>
    /// Event to fade a mixer channel to a target volume over time.
    /// </summary>
    public sealed class FadeChannelEvent
    {
        #region Properties
        public AudioMixer.AudioChannel Channel { get; }
        public float TargetVolume { get; }
        public float DurationSeconds { get; }
        #endregion

        #region Constructors
        public FadeChannelEvent(
            GDEngine.Core.Audio.AudioMixer.AudioChannel channel,
            float targetVolume,
            float durationSeconds)
        {
            Channel = channel;
            TargetVolume = targetVolume;
            DurationSeconds = durationSeconds;
        }
        #endregion
    }
}

namespace GDEngine.Core.Systems
{
    /// <summary>
    /// Central audio controller for SFX, music and mixer.
    /// Integrates with <see cref="EventBus"/>, <see cref="OrchestrationSystem"/> and the ECS.
    /// </summary>
    /// <see cref="AudioMixer"/>
    /// <see cref="FrameLifecycle.Update"/>
    public sealed class AudioSystem : SystemBase, IDisposable
    {
        #region Static Fields
        #endregion

        #region Fields
        private readonly AudioMixer _mixer = new AudioMixer();
        private readonly List<SoundEffectInstance> _sfxInstances = new List<SoundEffectInstance>(64);

        private EngineContext? _context;
        private Scene? _scene;
        private ContentDictionary<SoundEffect>? _sounds;

        private readonly AudioListener _listener = new AudioListener();
        private readonly AudioEmitter _emitter = new AudioEmitter();

        private SoundEffectInstance? _musicCurrent;
        private SoundEffectInstance? _musicNext;

        private float _musicBaseVolume = 1f;
        private float _musicCrossFadeDuration;
        private float _musicCrossFadeElapsed;
        private bool _musicCrossFading;

        private IDisposable? _subPlaySfx;
        private IDisposable? _subStopAllSfx;
        private IDisposable? _subPlayMusic;
        private IDisposable? _subStopMusic;
        private IDisposable? _subFadeChannel;
        #endregion

        #region Properties
        /// <summary>
        /// Exposes the underlying mixer so callers can inspect channel volumes.
        /// </summary>
        public AudioMixer Mixer
        {
            get { return _mixer; }
        }
        #endregion

        #region Constructors
        public AudioSystem(ContentDictionary<SoundEffect> sounds)
          : this(sounds, 0)
        {

        }
        public AudioSystem(ContentDictionary<SoundEffect> sounds, int order = 0)
            : base(FrameLifecycle.Update, order)
        {
            _sounds = sounds;
        }
        #endregion

        #region Methods
        public void SetChannelVolume(AudioMixer.AudioChannel channel, float volume)
        {
            _mixer.SetVolume(channel, volume);
        }

        public float GetChannelVolume(AudioMixer.AudioChannel channel)
        {
            return _mixer.GetVolume(channel);
        }

        public void PlayOneShot(string clipId, float volume = 1f)
        {
            SoundEffect? effect = ResolveClip(clipId);
            if (effect == null)
                return;

            float v = _mixer.GetEffectiveVolume(AudioMixer.AudioChannel.Sfx, volume);
            SoundEffectInstance instance = effect.CreateInstance();
            instance.IsLooped = false;
            instance.Volume = v;
            instance.Play();

            _sfxInstances.Add(instance);
        }

        public void PlayOneShot3D(string clipId, Transform emitterTransform, float volume = 1f)
        {
            if (emitterTransform == null)
                return;

            SoundEffect? effect = ResolveClip(clipId);
            if (effect == null)
                return;

            float v = _mixer.GetEffectiveVolume(AudioMixer.AudioChannel.Sfx, volume);
            if (v <= 0f)
                return;

            SoundEffectInstance instance = effect.CreateInstance();
            instance.IsLooped = false;

            // Configure emitter from the source transform
            _emitter.Position = emitterTransform.Position;
            _emitter.Forward = emitterTransform.Forward;
            _emitter.Up = emitterTransform.Up;
            _emitter.Velocity = Vector3.Zero;

            // Optional: ensure listener has some sane defaults even if no camera yet
            // (Update() will overwrite these when an ActiveCamera exists)
            // _listener.Position, _listener.Forward, _listener.Up already set in Update

            // Apply 3D spatialisation based on listener + emitter
            instance.Apply3D(_listener, _emitter);

            // Volume still goes through the mixer
            instance.Volume = v;
            instance.Play();

            _sfxInstances.Add(instance);
        }


        public void StopAllSfx()
        {
            for (int i = 0; i < _sfxInstances.Count; i++)
            {
                SoundEffectInstance inst = _sfxInstances[i];
                try
                {
                    inst.Stop();
                    inst.Dispose();
                }
                catch
                {
                }
            }

            _sfxInstances.Clear();
        }

        public void PlayMusic(string clipId, float volume = 1f, float fadeInSeconds = 0f, bool loop = true)
        {
            SoundEffectInstance? instance = CreateMusicInstance(clipId, loop);
            if (instance == null)
                return;

            _musicBaseVolume = MathHelper.Clamp(volume, 0f, 1f);

            if (_musicCurrent == null || fadeInSeconds <= 0f)
            {
                if (_musicCurrent != null)
                {
                    _musicCurrent.Stop();
                    _musicCurrent.Dispose();
                }

                _musicCurrent = instance;
                _musicCrossFading = false;
                _musicCrossFadeElapsed = 0f;
                _musicCrossFadeDuration = 0f;

                float v = _mixer.GetEffectiveVolume(AudioMixer.AudioChannel.Music, _musicBaseVolume);
                _musicCurrent.Volume = v;
                _musicCurrent.Play();
                return;
            }

            if (_musicNext != null)
            {
                _musicNext.Stop();
                _musicNext.Dispose();
            }

            _musicNext = instance;
            _musicCrossFading = true;
            _musicCrossFadeElapsed = 0f;
            _musicCrossFadeDuration = fadeInSeconds;

            _musicNext.Volume = 0f;
            _musicNext.Play();
        }

        public void StopMusic(float fadeOutSeconds = 0f)
        {
            if (_musicCurrent == null)
                return;

            if (fadeOutSeconds <= 0f)
            {
                _musicCurrent.Stop();
                _musicCurrent.Dispose();
                _musicCurrent = null;

                if (_musicNext != null)
                {
                    _musicNext.Stop();
                    _musicNext.Dispose();
                    _musicNext = null;
                }

                _musicCrossFading = false;
                _musicCrossFadeElapsed = 0f;
                _musicCrossFadeDuration = 0f;
                return;
            }

            _musicCrossFading = true;
            _musicCrossFadeElapsed = 0f;
            _musicCrossFadeDuration = fadeOutSeconds;

            if (_musicNext != null)
            {
                _musicNext.Stop();
                _musicNext.Dispose();
                _musicNext = null;
            }
        }
        #endregion

        #region Lifecycle Methods
        protected override void OnAdded()
        {
            _scene = Scene;
            _context = _scene != null ? _scene.Context : null;

            if (_context == null)
                return;

            EventBus? bus = _context.Events;
            if (bus == null)
                return;

            // Subscribe to all relevant events
            _subPlaySfx = bus.On<PlaySfxEvent>()
                .WithPriorityPreset(EventPriority.Gameplay)
                .Do(HandlePlaySfx);

            _subStopAllSfx = bus.On<StopAllSfxEvent>()
                .WithPriorityPreset(EventPriority.Gameplay)
                .Do(_ => StopAllSfx());

            _subPlayMusic = bus.On<PlayMusicEvent>()
                .WithPriorityPreset(EventPriority.Systems)
                .Do(HandlePlayMusic);

            _subStopMusic = bus.On<StopMusicEvent>()
                .WithPriorityPreset(EventPriority.Systems)
                .Do(HandleStopMusic);

            _subFadeChannel = bus.On<FadeChannelEvent>()
                .WithPriorityPreset(EventPriority.Systems)
                .Do(HandleFadeChannel);
        }

        protected override void OnRemoved()
        {
            Dispose();
        }
        public override void Update(float deltaTime)
        {
            // Update listener from active camera
            if (_scene != null && _scene.ActiveCamera != null)
            {
                Transform? camTransform = _scene.ActiveCamera.Transform;

                if (camTransform != null)
                {
                    _listener.Position = camTransform.Position;
                    _listener.Forward = camTransform.Forward;
                    _listener.Up = camTransform.Up;
                    _listener.Velocity = Vector3.Zero;
                }
            }

            _mixer.Update(deltaTime);
            UpdateMusic(deltaTime);

            for (int i = _sfxInstances.Count - 1; i >= 0; i--)
            {
                SoundEffectInstance inst = _sfxInstances[i];
                if (inst.State == SoundState.Stopped)
                {
                    try
                    {
                        inst.Dispose();
                    }
                    catch
                    {
                    }

                    _sfxInstances.RemoveAt(i);
                }
            }
        }

        #endregion

        #region Housekeeping Methods
        public void Dispose()
        {
            StopAllSfx();

            if (_musicCurrent != null)
            {
                try
                {
                    _musicCurrent.Stop();
                    _musicCurrent.Dispose();
                }
                catch
                {
                }

                _musicCurrent = null;
            }

            if (_musicNext != null)
            {
                try
                {
                    _musicNext.Stop();
                    _musicNext.Dispose();
                }
                catch
                {
                }

                _musicNext = null;
            }

            _subPlaySfx?.Dispose();
            _subStopAllSfx?.Dispose();
            _subPlayMusic?.Dispose();
            _subStopMusic?.Dispose();
            _subFadeChannel?.Dispose();
        }
        #endregion

        #region Methods (private)
        private SoundEffect? ResolveClip(string clipId)
        {
            if (string.IsNullOrWhiteSpace(clipId))
                return null;

            if (_sounds == null)
                return null;

            SoundEffect? s;
            if (_sounds.TryGet(clipId, out s) && s != null)
                return s;

            return null;
        }


        private SoundEffectInstance? CreateMusicInstance(string clipId, bool loop)
        {
            SoundEffect? effect = ResolveClip(clipId);
            if (effect == null)
                return null;

            SoundEffectInstance instance = effect.CreateInstance();
            instance.IsLooped = loop;
            return instance;
        }

        private void UpdateMusic(float deltaTime)
        {
            if (_musicCurrent == null && _musicNext == null)
                return;

            if (!_musicCrossFading)
            {
                if (_musicCurrent != null)
                {
                    float v = _mixer.GetEffectiveVolume(AudioMixer.AudioChannel.Music, _musicBaseVolume);
                    _musicCurrent.Volume = v;
                }

                return;
            }

            if (_musicCrossFadeDuration <= 0f)
            {
                CompleteMusicCrossFade();
                return;
            }

            _musicCrossFadeElapsed += deltaTime;
            float t = _musicCrossFadeElapsed / _musicCrossFadeDuration;
            if (t < 0f)
                t = 0f;
            if (t > 1f)
                t = 1f;

            if (_musicNext != null && _musicCurrent != null)
            {
                float vCurrent = _mixer.GetEffectiveVolume(AudioMixer.AudioChannel.Music, _musicBaseVolume * (1f - t));
                float vNext = _mixer.GetEffectiveVolume(AudioMixer.AudioChannel.Music, _musicBaseVolume * t);

                _musicCurrent.Volume = vCurrent;
                _musicNext.Volume = vNext;

                if (t >= 1f)
                    CompleteMusicCrossFade();
            }
            else if (_musicCurrent != null)
            {
                float v = _mixer.GetEffectiveVolume(AudioMixer.AudioChannel.Music, _musicBaseVolume * (1f - t));
                _musicCurrent.Volume = v;

                if (t >= 1f)
                {
                    _musicCurrent.Stop();
                    _musicCurrent.Dispose();
                    _musicCurrent = null;

                    _musicCrossFading = false;
                    _musicCrossFadeElapsed = 0f;
                    _musicCrossFadeDuration = 0f;
                }
            }
            else
            {
                _musicCrossFading = false;
                _musicCrossFadeElapsed = 0f;
                _musicCrossFadeDuration = 0f;
            }
        }

        private void CompleteMusicCrossFade()
        {
            if (_musicCurrent != null && _musicNext != null)
            {
                _musicCurrent.Stop();
                _musicCurrent.Dispose();
                _musicCurrent = _musicNext;
                _musicNext = null;

                float v = _mixer.GetEffectiveVolume(AudioMixer.AudioChannel.Music, _musicBaseVolume);
                _musicCurrent.Volume = v;
            }

            _musicCrossFading = false;
            _musicCrossFadeElapsed = 0f;
            _musicCrossFadeDuration = 0f;
        }

        private void HandlePlaySfx(PlaySfxEvent evt)
        {
            if (evt.Spatial && evt.Emitter != null)
                PlayOneShot3D(evt.Clip, evt.Emitter, evt.Volume);
            else
                PlayOneShot(evt.Clip, evt.Volume);
        }

        private void HandlePlayMusic(PlayMusicEvent evt)
        {
            PlayMusic(evt.Clip, evt.Volume, evt.FadeInSeconds, true);
        }

        private void HandleStopMusic(StopMusicEvent evt)
        {
            StopMusic(evt.FadeOutSeconds);
        }

        private void HandleFadeChannel(FadeChannelEvent evt)
        {
            _mixer.FadeTo(evt.Channel, evt.TargetVolume, evt.DurationSeconds);
        }
        #endregion
    }
}
