using System.Collections;
using System.Collections.Generic;
using Adrenak.UniMic;
using Adrenak.UniVoice;
using Adrenak.UniVoice.Inputs;
using Adrenak.UniVoice.Outputs;
using Adrenak.UniVoice.Samples;
using UnityEngine;
using UnityEngine.InputSystem;

public class VoiceController : MonoBehaviour
{
    public static VoiceController Instance { get; private set; }

    private MicVolumeFilter _micFilter;
    private UniMicInput _micInput;
    private Mic.Device _currentMicDevice;
    private float _outputVolume = 1f;
    private float _micVolume = 1f;

    public bool TurnMuted { get; set; }
    public bool IsLocalMuted => _micVolume == 0f || TurnMuted;
    public bool IsLocalSpeaking =>
        UniVoiceMirrorSetupSample.HasSetUp &&
        (UniVoiceMirrorSetupSample.ClientSession?.InputEnabled ?? false);

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(this); return; }
    }

    private void Start()
    {
        StartCoroutine(WaitAndSetup());
    }

    private void Update()
    {
        if (!UniVoiceMirrorSetupSample.HasSetUp) return;
        var session = UniVoiceMirrorSetupSample.ClientSession;
        if (session == null) return;

        bool keyPressed = Keyboard.current != null && Keyboard.current[VoiceButton.Bind].isPressed;
        session.InputEnabled = keyPressed && !IsLocalMuted;
    }

    public void SetOutputVolume(float volume)
    {
        _outputVolume = volume;
        ApplyOutputVolumeToAll();
    }

    public void SetMicVolume(float volume)
    {
        _micVolume = volume;
        if (_micFilter != null)
            _micFilter.Volume = volume;
    }

    public List<string> GetMicDeviceNames()
    {
        return new List<string>(Microphone.devices);
    }

    public int GetCurrentMicIndex()
    {
        if (_currentMicDevice == null) return 0;
        int idx = System.Array.IndexOf(Microphone.devices, _currentMicDevice.Name);
        return idx < 0 ? 0 : idx;
    }

    public void SetMicDevice(int index)
    {
        string[] deviceNames = Microphone.devices;
        if (index < 0 || index >= deviceNames.Length) return;

        var newDevice = Mic.AvailableDevices.Find(d => d.Name == deviceNames[index]);
        if (newDevice == null || newDevice == _currentMicDevice) return;

        _currentMicDevice?.StopRecording();
        newDevice.StartRecording(60);

        if (_micInput != null)
            _micInput.Device = newDevice;

        _currentMicDevice = newDevice;
        PlayerPrefs.SetInt("MicDeviceIndex", index);
    }

    // ── Output device ────────────────────────────────────────────────────────

    public List<string> GetOutputDeviceNames() => AudioOutputDevices.GetDeviceNames();

    public int GetCurrentOutputDeviceIndex() => AudioOutputDevices.GetCurrentDeviceIndex();

    public void SetOutputDevice(int index)
    {
        if (AudioOutputDevices.SetDevice(index))
            PlayerPrefs.SetInt("OutputDeviceIndex", index);
    }

    // ── Peer speaking ────────────────────────────────────────────────────────

    public bool IsPeerSpeaking(int voiceId)
    {
        if (!UniVoiceMirrorSetupSample.HasSetUp) return false;
        var session = UniVoiceMirrorSetupSample.ClientSession;
        if (session == null) return false;
        if (session.PeerOutputs.TryGetValue(voiceId, out var output) && output is StreamedAudioSourceOutput saso)
            return saso.Stream.IsPlaying;
        return false;
    }

    private IEnumerator WaitAndSetup()
    {
        yield return new WaitUntil(() =>
            UniVoiceMirrorSetupSample.HasSetUp &&
            UniVoiceMirrorSetupSample.ClientSession != null);

        var session = UniVoiceMirrorSetupSample.ClientSession;

        _micFilter = new MicVolumeFilter();
        session.InputFilters.Insert(0, _micFilter);

        _micInput = session.Input as UniMicInput;
        if (_micInput != null)
            _currentMicDevice = _micInput.Device;

        int savedMic = PlayerPrefs.GetInt("MicDeviceIndex", 0);
        if (savedMic > 0) SetMicDevice(savedMic);

        int savedOutput = PlayerPrefs.GetInt("OutputDeviceIndex", 0);
        if (savedOutput > 0) SetOutputDevice(savedOutput);

        session.Client.OnPeerJoined += _ => ApplyOutputVolumeToAll();
    }

    private void ApplyOutputVolumeToAll()
    {
        if (!UniVoiceMirrorSetupSample.HasSetUp) return;
        var session = UniVoiceMirrorSetupSample.ClientSession;
        if (session == null) return;

        foreach (var output in session.PeerOutputs.Values)
        {
            if (output is StreamedAudioSourceOutput saso)
                saso.Stream.UnityAudioSource.volume = _outputVolume;
        }
    }
}

class MicVolumeFilter : IAudioFilter
{
    public float Volume { get; set; } = 1f;

    public AudioFrame Run(AudioFrame frame)
    {
        if (Mathf.Approximately(Volume, 1f)) return frame;

        var floats = Utils.Bytes.BytesToFloats(frame.samples);
        for (int i = 0; i < floats.Length; i++)
            floats[i] *= Volume;
        frame.samples = Utils.Bytes.FloatsToBytes(floats);
        return frame;
    }
}
