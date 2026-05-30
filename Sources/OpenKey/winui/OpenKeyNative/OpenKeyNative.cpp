#include "pch.h"
#include "OpenKeyNative.h"

#include <imm.h>

#include <algorithm>
#include <string>
#include <vector>

#include "..\..\engine\Engine.h"

#pragma comment(lib, "imm32")

namespace {
constexpr const wchar_t* RegistryPath = L"SOFTWARE\\TuyenMai\\OpenKey";
constexpr ULONG_PTR InjectedInputMarker = 1;
constexpr int EmptyHotKey = 0xFE0000FE;

constexpr unsigned int MaskShift = 0x01;
constexpr unsigned int MaskControl = 0x02;
constexpr unsigned int MaskAlt = 0x04;
constexpr unsigned int MaskCapital = 0x08;
constexpr unsigned int MaskNumLock = 0x10;
constexpr unsigned int MaskWin = 0x20;
constexpr unsigned int MaskScroll = 0x40;

#define OTHER_CONTROL_KEY ((_flag & MaskAlt) || (_flag & MaskControl))
#define DYNA_DATA(macro, pos) (macro ? _hookData->macroData[pos] : _hookData->charData[pos])

HMODULE _module = nullptr;
HHOOK _keyboardHook = nullptr;
vKeyHookState* _hookData = nullptr;

std::vector<Uint16> _syncKey;
std::vector<Uint16> _newCharString;
INPUT _backspaceEvent[2] = {};
INPUT _keyEvent[2] = {};

unsigned int _flag = 0;
unsigned int _lastFlag = 0;
bool _isFlagKey = false;
bool _hasJustUsedHotKey = false;
Uint16 _keycode = 0;

int ReadRegistryInt(const wchar_t* name, int defaultValue)
{
    HKEY key = nullptr;
    if (RegOpenKeyExW(HKEY_CURRENT_USER, RegistryPath, 0, KEY_READ, &key) != ERROR_SUCCESS) {
        return defaultValue;
    }

    DWORD type = REG_DWORD;
    DWORD value = 0;
    DWORD valueSize = sizeof(value);
    const LONG result = RegQueryValueExW(key, name, nullptr, &type, reinterpret_cast<BYTE*>(&value), &valueSize);
    RegCloseKey(key);
    return result == ERROR_SUCCESS ? static_cast<int>(value) : defaultValue;
}

std::vector<Byte> ReadRegistryBinary(const wchar_t* name)
{
    HKEY key = nullptr;
    if (RegOpenKeyExW(HKEY_CURRENT_USER, RegistryPath, 0, KEY_READ, &key) != ERROR_SUCCESS) {
        return {};
    }

    DWORD type = REG_BINARY;
    DWORD valueSize = 0;
    if (RegQueryValueExW(key, name, nullptr, &type, nullptr, &valueSize) != ERROR_SUCCESS || valueSize == 0) {
        RegCloseKey(key);
        return {};
    }

    std::vector<Byte> value(valueSize);
    const LONG result = RegQueryValueExW(key, name, nullptr, &type, value.data(), &valueSize);
    RegCloseKey(key);
    if (result != ERROR_SUCCESS) {
        return {};
    }

    return value;
}

void InitializeInputState()
{
    _flag = 0;
    if (GetKeyState(VK_LSHIFT) < 0 || GetKeyState(VK_RSHIFT) < 0) {
        _flag |= MaskShift;
    }
    if (GetKeyState(VK_LCONTROL) < 0 || GetKeyState(VK_RCONTROL) < 0) {
        _flag |= MaskControl;
    }
    if (GetKeyState(VK_LMENU) < 0 || GetKeyState(VK_RMENU) < 0) {
        _flag |= MaskAlt;
    }
    if (GetKeyState(VK_LWIN) < 0 || GetKeyState(VK_RWIN) < 0) {
        _flag |= MaskWin;
    }
    if (GetKeyState(VK_NUMLOCK) < 0) {
        _flag |= MaskNumLock;
    }
    if (GetKeyState(VK_CAPITAL) == 1) {
        _flag |= MaskCapital;
    }
    if (GetKeyState(VK_SCROLL) < 0) {
        _flag |= MaskScroll;
    }
    _lastFlag = _flag;
}

void PrepareKeyEvent(INPUT& input, Uint16 keycode, bool isPress, DWORD flag = 0)
{
    input.type = INPUT_KEYBOARD;
    input.ki.dwFlags = isPress ? flag : flag | KEYEVENTF_KEYUP;
    input.ki.wVk = keycode;
    input.ki.wScan = 0;
    input.ki.time = 0;
    input.ki.dwExtraInfo = InjectedInputMarker;
}

void PrepareUnicodeEvent(INPUT& input, Uint16 unicode, bool isPress)
{
    input.type = INPUT_KEYBOARD;
    input.ki.wVk = 0;
    input.ki.wScan = unicode;
    input.ki.time = 0;
    input.ki.dwFlags = (isPress ? 0 : KEYEVENTF_KEYUP) | KEYEVENTF_UNICODE;
    input.ki.dwExtraInfo = InjectedInputMarker;
}

void SendKeyCode(Uint32 data);

void SendBackspace()
{
    SendInput(2, _backspaceEvent, sizeof(INPUT));
    if (IS_DOUBLE_CODE(vCodeTable) && !_syncKey.empty()) {
        if (_syncKey.back() > 1) {
            SendInput(2, _backspaceEvent, sizeof(INPUT));
        }
        _syncKey.pop_back();
    }
}

void InsertKeyLength(Uint8 len)
{
    _syncKey.push_back(len);
}

void SendPureCharacter(Uint16 ch)
{
    if (ch < 128) {
        SendKeyCode(ch);
        return;
    }

    PrepareUnicodeEvent(_keyEvent[0], ch, true);
    PrepareUnicodeEvent(_keyEvent[1], ch, false);
    SendInput(2, _keyEvent, sizeof(INPUT));
    if (IS_DOUBLE_CODE(vCodeTable)) {
        InsertKeyLength(1);
    }
}

void SendKeyCode(Uint32 data)
{
    Uint16 newChar = static_cast<Uint16>(data);
    if (!(data & CHAR_CODE_MASK)) {
        if (IS_DOUBLE_CODE(vCodeTable)) {
            InsertKeyLength(1);
        }

        newChar = keyCodeToCharacter(data);
        if (newChar == 0) {
            newChar = static_cast<Uint16>(data);
            PrepareKeyEvent(_keyEvent[0], newChar, true);
            PrepareKeyEvent(_keyEvent[1], newChar, false);
            SendInput(2, _keyEvent, sizeof(INPUT));
        } else {
            PrepareUnicodeEvent(_keyEvent[0], newChar, true);
            PrepareUnicodeEvent(_keyEvent[1], newChar, false);
            SendInput(2, _keyEvent, sizeof(INPUT));
        }
        return;
    }

    if (vCodeTable == 0) {
        PrepareUnicodeEvent(_keyEvent[0], newChar, true);
        PrepareUnicodeEvent(_keyEvent[1], newChar, false);
        SendInput(2, _keyEvent, sizeof(INPUT));
    } else if (vCodeTable == 1 || vCodeTable == 2 || vCodeTable == 4) {
        Uint16 highChar = HIBYTE(newChar);
        newChar = LOBYTE(newChar);

        PrepareUnicodeEvent(_keyEvent[0], newChar, true);
        PrepareUnicodeEvent(_keyEvent[1], newChar, false);
        SendInput(2, _keyEvent, sizeof(INPUT));

        if (highChar > 32) {
            if (vCodeTable == 2) {
                InsertKeyLength(2);
            }
            PrepareUnicodeEvent(_keyEvent[0], highChar, true);
            PrepareUnicodeEvent(_keyEvent[1], highChar, false);
            SendInput(2, _keyEvent, sizeof(INPUT));
        } else if (vCodeTable == 2) {
            InsertKeyLength(1);
        }
    } else if (vCodeTable == 3) {
        const Uint16 highChar = newChar >> 13;
        newChar &= 0x1FFF;
        InsertKeyLength(highChar > 0 ? 2 : 1);
        PrepareUnicodeEvent(_keyEvent[0], newChar, true);
        PrepareUnicodeEvent(_keyEvent[1], newChar, false);
        SendInput(2, _keyEvent, sizeof(INPUT));
        if (highChar > 0) {
            PrepareUnicodeEvent(_keyEvent[0], _unicodeCompoundMark[highChar - 1], true);
            PrepareUnicodeEvent(_keyEvent[1], _unicodeCompoundMark[highChar - 1], false);
            SendInput(2, _keyEvent, sizeof(INPUT));
        }
    }
}

void SendOutputCharacters(bool dataFromMacro = false)
{
    const int count = dataFromMacro ? static_cast<int>(_hookData->macroData.size()) : _hookData->newCharCount;
    for (int index = dataFromMacro ? 0 : count - 1; dataFromMacro ? index < count : index >= 0; dataFromMacro ? index++ : index--) {
        const Uint32 data = DYNA_DATA(dataFromMacro, index);
        if (data & PURE_CHARACTER_MASK) {
            SendPureCharacter(static_cast<Uint16>(data));
        } else {
            SendKeyCode(data);
        }
    }

    if (_hookData->code == vRestore || _hookData->code == vRestoreAndStartNewSession) {
        SendKeyCode(_keycode | ((_flag & MaskCapital) || (_flag & MaskShift) ? CAPS_MASK : 0));
    }
    if (_hookData->code == vRestoreAndStartNewSession) {
        startNewSession();
    }
}

void HandleMacro()
{
    for (int index = 0; index < _hookData->backspaceCount; index++) {
        SendBackspace();
    }
    SendOutputCharacters(true);
    SendKeyCode(_keycode | (_flag & MaskShift ? CAPS_MASK : 0));
}

bool SetModifierMask(Uint16 vkCode)
{
    if (GetKeyState(VK_CAPITAL) == 1) {
        _flag |= MaskCapital;
    } else {
        _flag &= ~MaskCapital;
    }

    if (vkCode == VK_LSHIFT || vkCode == VK_RSHIFT) {
        _flag |= MaskShift;
    } else if (vkCode == VK_LCONTROL || vkCode == VK_RCONTROL) {
        _flag |= MaskControl;
    } else if (vkCode == VK_LMENU || vkCode == VK_RMENU) {
        _flag |= MaskAlt;
    } else if (vkCode == VK_LWIN || vkCode == VK_RWIN) {
        _flag |= MaskWin;
    } else if (vkCode == VK_NUMLOCK) {
        _flag |= MaskNumLock;
    } else if (vkCode == VK_SCROLL) {
        _flag |= MaskScroll;
    } else {
        _isFlagKey = false;
        return false;
    }

    _isFlagKey = true;
    return true;
}

bool UnsetModifierMask(Uint16 vkCode)
{
    if (GetKeyState(VK_CAPITAL) == 1) {
        _flag |= MaskCapital;
    } else {
        _flag &= ~MaskCapital;
    }

    if (vkCode == VK_LSHIFT || vkCode == VK_RSHIFT) {
        _flag &= ~MaskShift;
    } else if (vkCode == VK_LCONTROL || vkCode == VK_RCONTROL) {
        _flag &= ~MaskControl;
    } else if (vkCode == VK_LMENU || vkCode == VK_RMENU) {
        _flag &= ~MaskAlt;
    } else if (vkCode == VK_LWIN || vkCode == VK_RWIN) {
        _flag &= ~MaskWin;
    } else if (vkCode == VK_NUMLOCK) {
        _flag &= ~MaskNumLock;
    } else if (vkCode == VK_SCROLL) {
        _flag &= ~MaskScroll;
    } else {
        _isFlagKey = false;
        return false;
    }

    _isFlagKey = true;
    return true;
}

bool CheckHotKey(int hotKeyData, bool checkKeyCode = true)
{
    if ((hotKeyData & (~0x8000)) == EmptyHotKey) {
        return false;
    }
    if (HAS_CONTROL(hotKeyData) ^ GET_BOOL(_lastFlag & MaskControl)) {
        return false;
    }
    if (HAS_OPTION(hotKeyData) ^ GET_BOOL(_lastFlag & MaskAlt)) {
        return false;
    }
    if (HAS_COMMAND(hotKeyData) ^ GET_BOOL(_lastFlag & MaskWin)) {
        return false;
    }
    if (HAS_SHIFT(hotKeyData) ^ GET_BOOL(_lastFlag & MaskShift)) {
        return false;
    }
    return !checkKeyCode || GET_SWITCH_KEY(hotKeyData) == _keycode;
}

void SwitchLanguage()
{
    vLanguage = vLanguage == 0 ? 1 : 0;
    if (HAS_BEEP(vSwitchKeyStatus)) {
        MessageBeep(MB_OK);
    }
    startNewSession();
}

bool IsImeOpen()
{
    HWND hwnd = GetForegroundWindow();
    HWND ime = ImmGetDefaultIMEWnd(hwnd);
    return ime != nullptr && SendMessageW(ime, WM_IME_CONTROL, 0x0005, 0) != 0;
}

LRESULT CALLBACK KeyboardHookProcess(int nCode, WPARAM wParam, LPARAM lParam)
{
    if (nCode < 0) {
        return CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
    }

    auto* keyboardData = reinterpret_cast<KBDLLHOOKSTRUCT*>(lParam);
    if (keyboardData->dwExtraInfo == InjectedInputMarker || IsImeOpen()) {
        return CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
    }

    if (wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN) {
        SetModifierMask(static_cast<Uint16>(keyboardData->vkCode));
    } else if (wParam == WM_KEYUP || wParam == WM_SYSKEYUP) {
        UnsetModifierMask(static_cast<Uint16>(keyboardData->vkCode));
    }

    if (!_isFlagKey && wParam != WM_KEYUP && wParam != WM_SYSKEYUP) {
        _keycode = static_cast<Uint16>(keyboardData->vkCode);
    }

    if ((wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN) && !_isFlagKey && _keycode != 0) {
        if (GET_SWITCH_KEY(vSwitchKeyStatus) != _keycode) {
            _lastFlag = 0;
        } else if (CheckHotKey(vSwitchKeyStatus, GET_SWITCH_KEY(vSwitchKeyStatus) != 0xFE)) {
            SwitchLanguage();
            _hasJustUsedHotKey = true;
            _keycode = 0;
            return -1;
        }
        _hasJustUsedHotKey = _lastFlag != 0;
    } else if (_isFlagKey) {
        if (_lastFlag == 0 || _lastFlag < _flag) {
            _lastFlag = _flag;
        } else if (_lastFlag > _flag) {
            if (CheckHotKey(vSwitchKeyStatus, GET_SWITCH_KEY(vSwitchKeyStatus) != 0xFE)) {
                SwitchLanguage();
                _hasJustUsedHotKey = true;
            }
            if (vTempOffSpelling && !_hasJustUsedHotKey && (_lastFlag & MaskControl)) {
                vTempOffSpellChecking();
            }
            if (vTempOffOpenKey && !_hasJustUsedHotKey && (_lastFlag & MaskAlt)) {
                vTempOffEngine();
            }
            _lastFlag = _flag;
            _hasJustUsedHotKey = false;
        }
        _keycode = 0;
        return CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
    }

    if (vLanguage == 0) {
        if (vUseMacro && vUseMacroInEnglishMode && (wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN)) {
            vEnglishMode(vKeyEventState::KeyDown, _keycode, (_flag & MaskShift) || (_flag & MaskCapital), OTHER_CONTROL_KEY);
            if (_hookData->code == vReplaceMaro) {
                HandleMacro();
                return 0;
            }
        }
        return CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
    }

    if (wParam != WM_KEYDOWN && wParam != WM_SYSKEYDOWN) {
        return CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
    }

    vKeyHandleEvent(
        vKeyEvent::Keyboard,
        vKeyEventState::KeyDown,
        _keycode,
        (_flag & MaskShift && _flag & MaskCapital) ? 0 : (_flag & MaskShift ? 1 : (_flag & MaskCapital ? 2 : 0)),
        OTHER_CONTROL_KEY);

    if (_hookData->code == vDoNothing) {
        if (IS_DOUBLE_CODE(vCodeTable)) {
            if (_hookData->extCode == 1) {
                _syncKey.clear();
            } else if (_hookData->extCode == 2 && !_syncKey.empty()) {
                if (_syncKey.back() > 1 && (vCodeTable == 2 || vCodeTable == 3)) {
                    SendInput(2, _backspaceEvent, sizeof(INPUT));
                }
                _syncKey.pop_back();
            } else if (_hookData->extCode == 3) {
                InsertKeyLength(1);
            }
        }
        return CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
    }

    if (_hookData->code == vWillProcess || _hookData->code == vRestore || _hookData->code == vRestoreAndStartNewSession) {
        for (int index = 0; index < _hookData->backspaceCount && _hookData->backspaceCount < MAX_BUFF; index++) {
            SendBackspace();
        }
        SendOutputCharacters(false);
    } else if (_hookData->code == vReplaceMaro) {
        HandleMacro();
    }

    return -1;
}

void PrepareBackspaceEvents()
{
    PrepareKeyEvent(_backspaceEvent[0], VK_BACK, true);
    PrepareKeyEvent(_backspaceEvent[1], VK_BACK, false);
}
} // namespace

int vLanguage = 1;
int vInputType = 0;
int vFreeMark = 0;
int vCodeTable = 0;
int vSwitchKeyStatus = 0x7A000206;
int vCheckSpelling = 1;
int vUseModernOrthography = 0;
int vQuickTelex = 0;
int vRestoreIfWrongSpelling = 1;
int vFixRecommendBrowser = 1;
int vUseMacro = 1;
int vUseMacroInEnglishMode = 0;
int vAutoCapsMacro = 0;
int vUseSmartSwitchKey = 0;
int vUpperCaseFirstChar = 0;
int vTempOffSpelling = 0;
int vAllowConsonantZFWJ = 0;
int vQuickStartConsonant = 0;
int vQuickEndConsonant = 0;
int vRememberCode = 0;
int vOtherLanguage = 0;
int vTempOffOpenKey = 0;
int vSendKeyStepByStep = 1;
int vSupportMetroApp = 0;
int vFixChromiumBrowser = 0;

OPENKEY_NATIVE_API int OpenKeyNative_ReloadSettings()
{
    vLanguage = ReadRegistryInt(L"vLanguage", 1);
    vInputType = ReadRegistryInt(L"vInputType", 0);
    vFreeMark = 0;
    vCodeTable = ReadRegistryInt(L"vCodeTable", 0);
    vCheckSpelling = ReadRegistryInt(L"vCheckSpelling", 1);
    vUseModernOrthography = ReadRegistryInt(L"vUseModernOrthography", 0);
    vQuickTelex = ReadRegistryInt(L"vQuickTelex", 0);
    vSwitchKeyStatus = ReadRegistryInt(L"vSwitchKeyStatus", 0x7A000206);
    vRestoreIfWrongSpelling = ReadRegistryInt(L"vRestoreIfWrongSpelling", 1);
    vFixRecommendBrowser = ReadRegistryInt(L"vFixRecommendBrowser", 1);
    vUseMacro = ReadRegistryInt(L"vUseMacro", 1);
    vUseMacroInEnglishMode = ReadRegistryInt(L"vUseMacroInEnglishMode", 0);
    vAutoCapsMacro = ReadRegistryInt(L"vAutoCapsMacro", 0);
    vSendKeyStepByStep = ReadRegistryInt(L"vSendKeyStepByStep", 1);
    vUseSmartSwitchKey = ReadRegistryInt(L"vUseSmartSwitchKey", 0);
    vUpperCaseFirstChar = ReadRegistryInt(L"vUpperCaseFirstChar", 0);
    vAllowConsonantZFWJ = ReadRegistryInt(L"vAllowConsonantZFWJ", 0);
    vTempOffSpelling = ReadRegistryInt(L"vTempOffSpelling", 0);
    vQuickStartConsonant = ReadRegistryInt(L"vQuickStartConsonant", 0);
    vQuickEndConsonant = ReadRegistryInt(L"vQuickEndConsonant", 0);
    vSupportMetroApp = ReadRegistryInt(L"vSupportMetroApp", 0);
    vRememberCode = ReadRegistryInt(L"vRememberCode", 0);
    vOtherLanguage = ReadRegistryInt(L"vOtherLanguage", 0);
    vTempOffOpenKey = ReadRegistryInt(L"vTempOffOpenKey", 0);
    vFixChromiumBrowser = ReadRegistryInt(L"vFixChromiumBrowser", 0);

    const auto macroData = ReadRegistryBinary(L"macroData");
    initMacroMap(macroData.empty() ? nullptr : macroData.data(), static_cast<int>(macroData.size()));

    const auto smartSwitchData = ReadRegistryBinary(L"smartSwitchKey");
    initSmartSwitchKey(smartSwitchData.empty() ? nullptr : smartSwitchData.data(), static_cast<int>(smartSwitchData.size()));
    return 1;
}

OPENKEY_NATIVE_API int OpenKeyNative_Initialize()
{
    OpenKeyNative_ReloadSettings();
    _hookData = static_cast<vKeyHookState*>(vKeyInit());
    PrepareBackspaceEvents();
    InitializeInputState();
    return _hookData != nullptr ? 1 : 0;
}

OPENKEY_NATIVE_API int OpenKeyNative_StartHook()
{
    if (_keyboardHook != nullptr) {
        return 1;
    }
    if (_hookData == nullptr && !OpenKeyNative_Initialize()) {
        return 0;
    }

    _keyboardHook = SetWindowsHookExW(WH_KEYBOARD_LL, KeyboardHookProcess, _module, 0);
    return _keyboardHook != nullptr ? 1 : 0;
}

OPENKEY_NATIVE_API void OpenKeyNative_StopHook()
{
    if (_keyboardHook != nullptr) {
        UnhookWindowsHookEx(_keyboardHook);
        _keyboardHook = nullptr;
    }
}

OPENKEY_NATIVE_API void OpenKeyNative_SetLanguage(int language)
{
    vLanguage = language ? 1 : 0;
    vTempOffEngine(false);
    startNewSession();
}

OPENKEY_NATIVE_API int OpenKeyNative_GetLanguage()
{
    return vLanguage;
}

OPENKEY_NATIVE_API int OpenKeyNative_IsHookRunning()
{
    return _keyboardHook != nullptr ? 1 : 0;
}

BOOL APIENTRY DllMain(HMODULE hModule, DWORD reason, LPVOID)
{
    if (reason == DLL_PROCESS_ATTACH) {
        _module = hModule;
        DisableThreadLibraryCalls(hModule);
    } else if (reason == DLL_PROCESS_DETACH) {
        OpenKeyNative_StopHook();
    }
    return TRUE;
}
