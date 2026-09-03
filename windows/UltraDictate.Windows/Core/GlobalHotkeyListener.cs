using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace UltraDictate.Windows.Core;

public class GlobalHotkeyListener : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;

    private const int VK_RCONTROL = 0xA3;
    private const int VK_RMENU = 0xA5; // Right Alt / AltGr

    private readonly LowLevelKeyboardProc _proc;
    private IntPtr _hookId = IntPtr.Zero;
    private bool _isHotkeyDown = false;

    public event Action? HotkeyDown;
    public event Action? HotkeyUp;

    public int TargetVkCode { get; set; } = VK_RCONTROL;

    public GlobalHotkeyListener()
    {
        _proc = HookCallback;
        _hookId = SetHook(_proc);
    }

    private IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule;
        return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
            GetModuleHandle(curModule?.ModuleName), 0);
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int vkCode = Marshal.ReadInt32(lParam);
            int flags = Marshal.ReadInt32(lParam, 8);
            bool isExtended = (flags & 0x01) != 0;

            bool isTargetKey = false;
            if (TargetVkCode == VK_RCONTROL && vkCode == 0x11 && isExtended)
            {
                isTargetKey = true;
            }
            else if (TargetVkCode == VK_RMENU && vkCode == 0x12 && isExtended)
            {
                isTargetKey = true;
            }
            else if (vkCode == TargetVkCode)
            {
                isTargetKey = true;
            }

            if (isTargetKey)
            {
                int msg = wParam.ToInt32();
                if ((msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN) && !_isHotkeyDown)
                {
                    _isHotkeyDown = true;
                    HotkeyDown?.Invoke();
                }
                else if ((msg == WM_KEYUP || msg == WM_SYSKEYUP) && _isHotkeyDown)
                {
                    _isHotkeyDown = false;
                    HotkeyUp?.Invoke();
                }
            }
        }
        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_hookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
        GC.SuppressFinalize(this);
    }

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);
}
