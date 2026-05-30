#pragma once

#ifdef OPENKEYNATIVE_EXPORTS
#define OPENKEY_NATIVE_API extern "C" __declspec(dllexport)
#else
#define OPENKEY_NATIVE_API extern "C" __declspec(dllimport)
#endif

OPENKEY_NATIVE_API int OpenKeyNative_Initialize();
OPENKEY_NATIVE_API int OpenKeyNative_StartHook();
OPENKEY_NATIVE_API void OpenKeyNative_StopHook();
OPENKEY_NATIVE_API int OpenKeyNative_ReloadSettings();
OPENKEY_NATIVE_API void OpenKeyNative_SetLanguage(int language);
OPENKEY_NATIVE_API int OpenKeyNative_GetLanguage();
OPENKEY_NATIVE_API int OpenKeyNative_IsHookRunning();

