using System;
using System.Runtime.InteropServices;
using UnityEngine;

public static class IosAudioSessionLogger
{
#if UNITY_IOS && !UNITY_EDITOR
	private static bool s_Logged;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	private static void Register()
	{
		AkSoundEngineInitialization.Instance.initializationDelegate += LogOnWwiseInitialized;

		if (AkSoundEngine.IsInitialized())
		{
			LogOnce("Wwise already initialized");
		}
	}

	private static void LogOnWwiseInitialized()
	{
		LogOnce("Wwise initialized");
	}

	private static void LogOnce(string reason)
	{
		if (s_Logged)
		{
			return;
		}

		s_Logged = true;
		var info = IosAudioSessionNative.GetAudioSessionInfo();
		if (!string.IsNullOrEmpty(info))
		{
			Debug.Log($"[AudioSession] {reason}: {info}");
		}
		else
		{
			Debug.Log($"[AudioSession] {reason}: <unavailable>");
		}
	}
#endif
}

internal static class IosAudioSessionNative
{
#if UNITY_IOS && !UNITY_EDITOR
	[DllImport("__Internal")]
	private static extern IntPtr WwiseUnity_GetAudioSessionInfo();

	[DllImport("__Internal")]
	private static extern void WwiseUnity_FreeCString(IntPtr str);

	public static string GetAudioSessionInfo()
	{
		var ptr = WwiseUnity_GetAudioSessionInfo();
		if (ptr == IntPtr.Zero)
		{
			return null;
		}

		try
		{
			return Marshal.PtrToStringAnsi(ptr);
		}
		finally
		{
			WwiseUnity_FreeCString(ptr);
		}
	}
#else
	public static string GetAudioSessionInfo()
	{
		return null;
	}
#endif
}
