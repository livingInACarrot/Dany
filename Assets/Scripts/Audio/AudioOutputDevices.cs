using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// Windows-only: enumerates audio render endpoints and sets the system default.
/// Unity's audio engine picks up the new default after AudioSettings.Reset().
/// </summary>
public static class AudioOutputDevices
{
    // ─── COM interfaces ───────────────────────────────────────────────────────

    [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceEnumerator
    {
        int EnumAudioEndpoints(int dataFlow, int dwStateMask,
            out IMMDeviceCollection ppDevices);
        int GetDefaultAudioEndpoint(int dataFlow, int role,
            out IMMDevice ppEndpoint);
        int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string pwstrId,
            out IMMDevice ppDevice);
        int RegisterEndpointNotificationCallback(IntPtr pClient);
        int UnregisterEndpointNotificationCallback(IntPtr pClient);
    }

    [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    private class MMDeviceEnumeratorComObject { }

    [ComImport, Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceCollection
    {
        int GetCount(out uint pcDevices);
        int Item(uint nDevice, out IMMDevice ppDevice);
    }

    [ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDevice
    {
        int Activate(ref Guid iid, int dwClsCtx, IntPtr pActivationParams,
            out IntPtr ppInterface);
        int OpenPropertyStore(int stgmAccess, out IPropertyStore ppProperties);
        int GetId([MarshalAs(UnmanagedType.LPWStr)] out string ppstrId);
        int GetState(out int pdwState);
    }

    [ComImport, Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPropertyStore
    {
        int GetCount(out uint cProps);
        int GetAt(uint iProp, out PropertyKey pkey);
        int GetValue(ref PropertyKey key, out PropVariant pv);
        int SetValue(ref PropertyKey key, ref PropVariant propvar);
        int Commit();
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PropertyKey
    {
        public Guid fmtid;
        public int pid;
        public static readonly PropertyKey DeviceFriendlyName =
            new() { fmtid = new Guid("a45c254e-df1c-4efd-8020-67d146a850e0"), pid = 14 };
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct PropVariant
    {
        [FieldOffset(0)]  public short  vt;
        [FieldOffset(8)]  public IntPtr ptr;
        [FieldOffset(8)]  public long   hVal;
    }

    // Undocumented PolicyConfig (stable since Vista, works on Win10/11)
    [ComImport, Guid("870AF99C-171D-4F9E-AF0D-E63DF40C2BC9")]
    private class CPolicyConfigClient { }

    [ComImport, Guid("F8679F50-850A-41CF-9C72-430F290290C8"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPolicyConfig
    {
        int GetMixFormat(string deviceName, IntPtr ppFormat);
        int GetDeviceFormat(string deviceName, bool bDefault, IntPtr ppFormat);
        int ResetDeviceFormat(string deviceName);
        int SetDeviceFormat(string deviceName, IntPtr pEndpointFormat, IntPtr mixFormat);
        int GetProcessingPeriod(string deviceName, bool bDefault,
            IntPtr pmftDefaultPeriod, IntPtr pmftMinimumPeriod);
        int SetProcessingPeriod(string deviceName, IntPtr pmftPeriod);
        int GetShareMode(string deviceName, IntPtr pMode);
        int SetShareMode(string deviceName, IntPtr mode);
        int GetPropertyValue(string deviceName, bool bFxStore,
            IntPtr key, IntPtr pv);
        int SetPropertyValue(string deviceName, bool bFxStore,
            IntPtr key, IntPtr pv);
        int SetDefaultEndpoint(
            [MarshalAs(UnmanagedType.LPWStr)] string deviceId,
            int role);
        int SetEndpointVisibility(string deviceName, bool bVisible);
    }

    // ─── State ────────────────────────────────────────────────────────────────

    private static readonly List<(string Name, string Id)> _devices = new();

    // ─── Public API ───────────────────────────────────────────────────────────

    /// <summary>Returns friendly names of all active render endpoints.
    /// Index 0 is always "Default" (no change).</summary>
    public static List<string> GetDeviceNames()
    {
        _devices.Clear();
        _devices.Add(("Default", ""));   // index 0 = don't change

        try
        {
            var enumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorComObject();
            enumerator.EnumAudioEndpoints(
                dataFlow: 0,              // eRender
                dwStateMask: 0x00000001,  // DEVICE_STATE_ACTIVE
                ppDevices: out var collection);

            collection.GetCount(out uint count);
            for (uint i = 0; i < count; i++)
            {
                collection.Item(i, out IMMDevice device);
                device.GetId(out string id);
                string name = GetFriendlyName(device) ?? id;
                _devices.Add((name, id));
                Marshal.ReleaseComObject(device);
            }

            Marshal.ReleaseComObject(collection);
            Marshal.ReleaseComObject(enumerator);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AudioOutputDevices] Enumeration failed: {ex.Message}");
        }

        var names = new List<string>(_devices.Count);
        foreach (var d in _devices) names.Add(d.Name);
        return names;
    }

    /// <summary>Returns index of the current system default render endpoint
    /// (0 if not found).</summary>
    public static int GetCurrentDeviceIndex()
    {
        try
        {
            var enumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorComObject();
            enumerator.GetDefaultAudioEndpoint(
                dataFlow: 0, role: 1,     // eRender, eMultimedia
                ppEndpoint: out IMMDevice def);
            def.GetId(out string defaultId);
            Marshal.ReleaseComObject(def);
            Marshal.ReleaseComObject(enumerator);

            for (int i = 1; i < _devices.Count; i++)
                if (_devices[i].Id == defaultId) return i;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AudioOutputDevices] GetCurrentDevice failed: {ex.Message}");
        }
        return 0;
    }

    /// <summary>Sets the system default audio output to <paramref name="index"/>
    /// and restarts Unity's audio engine.</summary>
    public static bool SetDevice(int index)
    {
        if (index <= 0 || index >= _devices.Count) return true; // "Default" selected

        string deviceId = _devices[index].Id;
        try
        {
            var policy = (IPolicyConfig)new CPolicyConfigClient();
            // Set for Console (0), Multimedia (1) and Communications (2)
            policy.SetDefaultEndpoint(deviceId, 0);
            policy.SetDefaultEndpoint(deviceId, 1);
            policy.SetDefaultEndpoint(deviceId, 2);
            Marshal.ReleaseComObject(policy);

            // Tell Unity to re-open audio with the new default
            AudioSettings.Reset(AudioSettings.GetConfiguration());
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AudioOutputDevices] SetDevice failed: {ex.Message}");
            return false;
        }
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static string GetFriendlyName(IMMDevice device)
    {
        try
        {
            device.OpenPropertyStore(0, out IPropertyStore store); // STGM_READ
            var key = PropertyKey.DeviceFriendlyName;
            store.GetValue(ref key, out PropVariant pv);
            string name = Marshal.PtrToStringUni(pv.ptr);
            Marshal.FreeCoTaskMem(pv.ptr);
            Marshal.ReleaseComObject(store);
            return name;
        }
        catch { return null; }
    }
}
