# Audio Integration

## Overview

The `AudioSystem` provides centralized audio management with support for:
- One-shot sound effects (2D and 3D spatial)
- Looping background music with crossfade
- Per-channel volume mixing
- Event-driven playback

---

## Setup

### Creating the AudioSystem

```csharp
// Create sound dictionary
var sounds = new ContentDictionary<SoundEffect>(content);

// Load sounds with IDs
sounds.Add("footstep", "Audio/SFX/footstep");
sounds.Add("door_open", "Audio/SFX/door_open");
sounds.Add("pickup", "Audio/SFX/item_pickup");
sounds.Add("ambient", "Audio/Music/ambient_loop");
sounds.Add("tension", "Audio/Music/tension_loop");

// Create and add system
var audioSystem = new AudioSystem(sounds);
scene.Add(audioSystem);
```

---

## Playing Sound Effects

### Direct API

```csharp
var audio = scene.GetSystem<AudioSystem>();

// 2D sound (UI, global effects)
audio.PlayOneShot("pickup", volume: 0.8f);

// 3D spatial sound (positioned in world)
audio.PlayOneShot3D("footstep", transform, volume: 0.5f);
```

### Event-Based

```csharp
var events = scene.Context.Events;

// 2D sound
events.Publish(new PlaySfxEvent("door_open", volume: 1f));

// 3D spatial sound
events.Publish(new PlaySfxEvent(
    clip: "explosion",
    volume: 0.9f,
    spatial: true,
    emitter: enemyTransform
));
```

### Stopping All SFX

```csharp
audio.StopAllSfx();

// Or via event
events.Publish(new StopAllSfxEvent());
```

---

## Background Music

### Playing Music

```csharp
// Immediate start
audio.PlayMusic("ambient", volume: 0.7f);

// With fade-in
audio.PlayMusic("ambient", volume: 0.7f, fadeIn: 2f);

// Via event
events.Publish(new PlayMusicEvent(
    clip: "ambient",
    volume: 0.7f,
    fadeInSeconds: 2f
));
```

### Stopping Music

```csharp
// Immediate stop
audio.StopMusic();

// With fade-out
audio.StopMusic(fadeOut: 1.5f);

// Via event
events.Publish(new StopMusicEvent(fadeOutSeconds: 1.5f));
```

### Crossfading Music

When you call `PlayMusic` while music is already playing, the system crossfades:

```csharp
// Playing "ambient" music
audio.PlayMusic("ambient", 0.7f, fadeIn: 0f);

// Later: crossfade to "tension"
audio.PlayMusic("tension", 0.8f, fadeIn: 2f);
// "ambient" fades out while "tension" fades in over 2 seconds
```

---

## Volume Mixing

### Audio Channels

```csharp
public enum AudioChannel : sbyte
{
    Master = 0,  // Affects all audio
    Music = 1,   // Background music
    Sfx = 2,     // Sound effects
    Ui = 3       // UI sounds
}
```

### Setting Volume

```csharp
// Immediate volume change
audio.Mixer.SetVolume(AudioChannel.Master, 0.8f);
audio.Mixer.SetVolume(AudioChannel.Music, 0.5f);
audio.Mixer.SetVolume(AudioChannel.Sfx, 1f);

// Get current volume
float musicVol = audio.Mixer.GetVolume(AudioChannel.Music);
```

### Fading Volume

```csharp
// Fade music to 50% over 2 seconds
audio.Mixer.FadeTo(AudioChannel.Music, 0.5f, duration: 2f);

// Fade master to 0 (mute) over 1 second
audio.Mixer.FadeTo(AudioChannel.Master, 0f, duration: 1f);

// Via event
events.Publish(new FadeChannelEvent(
    channel: AudioChannel.Sfx,
    targetVolume: 0.3f,
    durationSeconds: 1.5f
));
```

### Effective Volume

Final volume = Master × Channel × Local

```csharp
// Master: 0.8, Music: 0.5, Local: 1.0
// Effective: 0.8 × 0.5 × 1.0 = 0.4

float effective = audio.Mixer.GetEffectiveVolume(AudioChannel.Music, localVolume: 1f);
```

---

## Spatial Audio (3D)

### How It Works

- The `AudioSystem` tracks a listener position from the active camera
- 3D sounds are positioned relative to this listener
- Sound volume attenuates with distance
- Stereo panning reflects left/right positioning

### Playing 3D Sounds

```csharp
// Pass the emitter's transform
audio.PlayOneShot3D("explosion", enemyTransform, volume: 1f);

// Via event
events.Publish(new PlaySfxEvent(
    clip: "machinery",
    volume: 0.7f,
    spatial: true,
    emitter: machineTransform
));
```

### Best Practices for 3D Audio

```csharp
// Use for sounds with clear physical sources
audio.PlayOneShot3D("footstep", characterTransform);
audio.PlayOneShot3D("door_creak", doorTransform);
audio.PlayOneShot3D("water_drip", fountainTransform);

// Use 2D for ambient/global sounds
audio.PlayOneShot("ui_click");
audio.PlayOneShot("heartbeat");
```

---

## Common Patterns

### Interaction Feedback

```csharp
public class InteractableObject : Component
{
    private AudioSystem? _audio;
    
    protected override void Start()
    {
        _audio = GameObject.Scene.GetSystem<AudioSystem>();
    }
    
    public void OnExamine()
    {
        _audio?.PlayOneShot3D("examine", Transform);
    }
    
    public void OnCollect()
    {
        _audio?.PlayOneShot("pickup", volume: 0.8f);
    }
    
    public void OnActivate()
    {
        _audio?.PlayOneShot3D("activate", Transform);
    }
}
```

### Ambient Sound Zones

```csharp
public class AmbientZone : Component
{
    public string AmbientTrack { get; set; }
    public float FadeTime { get; set; } = 2f;
    
    private EventBus? _events;
    
    protected override void Start()
    {
        _events = GameObject.Scene.Context.Events;
    }
    
    public void OnPlayerEnter()
    {
        _events?.Publish(new PlayMusicEvent(
            AmbientTrack, 
            volume: 0.6f, 
            fadeInSeconds: FadeTime
        ));
    }
    
    public void OnPlayerExit()
    {
        _events?.Publish(new FadeChannelEvent(
            AudioChannel.Music,
            targetVolume: 0f,
            durationSeconds: FadeTime
        ));
    }
}
```

### Self-Talk / Voice Lines

```csharp
public class PlayerVoice : Component
{
    private AudioSystem? _audio;
    
    protected override void Start()
    {
        _audio = GameObject.Scene.GetSystem<AudioSystem>();
    }
    
    public void Say(string lineId)
    {
        // Play voice line as 2D (always clear)
        _audio?.PlayOneShot(lineId, volume: 1f);
    }
}

// Usage
playerVoice.Say("vo_find_key");
playerVoice.Say("vo_door_locked");
```

### Dynamic Music

```csharp
public class MusicManager : Component
{
    private EventBus? _events;
    private string _currentTrack = "";
    
    protected override void Start()
    {
        _events = GameObject.Scene.Context.Events;
        
        // Subscribe to game state
        _events.On<GameStateChangedEvent>()
            .Do(OnGameStateChanged);
    }
    
    private void OnGameStateChanged(GameStateChangedEvent evt)
    {
        switch (evt.NewState)
        {
            case GameOutcomeState.InProgress:
                TransitionTo("music_explore");
                break;
            case GameOutcomeState.Won:
                TransitionTo("music_victory");
                break;
            case GameOutcomeState.Lost:
                TransitionTo("music_defeat");
                break;
        }
    }
    
    private void TransitionTo(string track)
    {
        if (_currentTrack == track) return;
        _currentTrack = track;
        
        _events?.Publish(new PlayMusicEvent(track, 0.7f, fadeInSeconds: 1.5f));
    }
}
```

### Audio Settings Menu

```csharp
public void OnMusicSliderChanged(float value)
{
    audio.Mixer.SetVolume(AudioChannel.Music, value);
    
    // Optional: Play preview sound
    if (value > 0)
        audio.PlayOneShot("music_preview", volume: 0.3f);
}

public void OnSfxSliderChanged(float value)
{
    audio.Mixer.SetVolume(AudioChannel.Sfx, value);
    
    // Play test sound
    audio.PlayOneShot("test_beep", volume: 0.5f);
}

public void OnMasterSliderChanged(float value)
{
    audio.Mixer.SetVolume(AudioChannel.Master, value);
}
```

---

## Audio Event Reference

| Event | Properties | Description |
|-------|------------|-------------|
| `PlaySfxEvent` | Clip, Volume, Spatial, Emitter | Play sound effect |
| `StopAllSfxEvent` | (none) | Stop all playing SFX |
| `PlayMusicEvent` | Clip, Volume, FadeInSeconds | Start music |
| `StopMusicEvent` | FadeOutSeconds | Stop music |
| `FadeChannelEvent` | Channel, TargetVolume, Duration | Fade channel volume |

---

## Tips

### Organize Sound IDs

```csharp
public static class SoundIds
{
    // SFX
    public const string Footstep = "footstep";
    public const string DoorOpen = "door_open";
    public const string DoorClose = "door_close";
    public const string Pickup = "pickup";
    public const string Examine = "examine";
    
    // Music
    public const string Ambient = "music_ambient";
    public const string Tension = "music_tension";
    public const string Victory = "music_victory";
    
    // Voice
    public const string VoiceKeyFound = "vo_key_found";
    public const string VoiceDoorLocked = "vo_door_locked";
}

// Usage
audio.PlayOneShot(SoundIds.DoorOpen);
```

### Avoid Sound Spam

```csharp
private float _lastFootstepTime;
private const float FootstepCooldown = 0.3f;

private void PlayFootstep()
{
    if (Time.TimeSinceStartupSecs - _lastFootstepTime < FootstepCooldown)
        return;
    
    _lastFootstepTime = Time.TimeSinceStartupSecs;
    _audio.PlayOneShot3D("footstep", Transform);
}
```

### Volume Guidelines

| Sound Type | Suggested Volume |
|------------|------------------|
| UI clicks | 0.3 - 0.5 |
| Footsteps | 0.4 - 0.6 |
| Ambient SFX | 0.3 - 0.5 |
| Important actions | 0.7 - 0.9 |
| Voice lines | 0.9 - 1.0 |
| Background music | 0.5 - 0.7 |
