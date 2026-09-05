using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UltraDictate.Windows.Core;

public static class TextInputService
{
    private const int INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const uint KEYEVENTF_UNICODE = 0x0004;

    public const byte VK_SHIFT = 0x10;
    public const byte VK_CONTROL = 0x11;
    public const byte VK_MENU = 0x12; // Alt
    public const byte VK_RETURN = 0x0D;
    public const byte VK_V = 0x56;
    public const byte VK_LWIN = 0x5B;
    public const byte VK_RWIN = 0x5C;
    public const byte VK_LCONTROL = 0xA2;
    public const byte VK_RCONTROL = 0xA3;
    public const byte VK_LMENU = 0xA4;
    public const byte VK_RMENU = 0xA5;

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    [DllImport("user32.dll")]
    private static extern bool BringWindowToTop(IntPtr hWnd);

    public static void ForceForegroundWindow(IntPtr targetHwnd)
    {
        if (targetHwnd == IntPtr.Zero) return;

        IntPtr currentForeground = GetForegroundWindow();
        if (currentForeground == targetHwnd) return;

        uint currentThreadId = GetCurrentThreadId();
        uint targetThreadId = GetWindowThreadProcessId(targetHwnd, out _);

        if (currentThreadId != targetThreadId && targetThreadId != 0)
        {
            AttachThreadInput(currentThreadId, targetThreadId, true);
            BringWindowToTop(targetHwnd);
            SetForegroundWindow(targetHwnd);
            AttachThreadInput(currentThreadId, targetThreadId, false);
        }
        else
        {
            BringWindowToTop(targetHwnd);
            SetForegroundWindow(targetHwnd);
        }
    }

    public static void ReleaseModifiers()
    {
        keybd_event(VK_RCONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        keybd_event(VK_LCONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        keybd_event(VK_RMENU, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        keybd_event(VK_LMENU, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        keybd_event(VK_MENU, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        keybd_event(VK_SHIFT, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        keybd_event(VK_LWIN, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        keybd_event(VK_RWIN, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    public static void InsertText(string text, string mode = "ClipboardPaste", IntPtr targetHwnd = default)
    {
        if (string.IsNullOrEmpty(text)) return;

        // 1. Ensure target window has focus
        if (targetHwnd != IntPtr.Zero)
        {
            ForceForegroundWindow(targetHwnd);
            Thread.Sleep(40);
        }

        // 2. Release physical modifiers so Ctrl/Alt are not stuck
        ReleaseModifiers();
        Thread.Sleep(35);

        // 3. Direct typing mode or fallback
        if (mode == "DirectTyping")
        {
            SendUnicodeString(text);
            return;
        }

        // 4. High-speed clipboard paste mode (Default)
        bool clipboardPasted = TryClipboardPaste(text);
        if (!clipboardPasted)
        {
            // Fallback to direct Unicode keystrokes if clipboard was locked
            SendUnicodeString(text);
        }
    }

    private static bool TryClipboardPaste(string text)
    {
        bool success = false;
        var thread = new Thread(() =>
        {
            IDataObject? previousData = null;
            try
            {
                // Retry opening clipboard up to 10 times if locked by another process
                for (int i = 0; i < 10; i++)
                {
                    try
                    {
                        if (Clipboard.ContainsText())
                        {
                            previousData = Clipboard.GetDataObject();
                        }
                        Clipboard.SetText(text);
                        success = true;
                        break;
                    }
                    catch
                    {
                        Thread.Sleep(20);
                    }
                }

                if (!success) return;

                // Micro-delay so Windows clipboard synchronization completes
                Thread.Sleep(25);

                // Synthesize Ctrl+V
                SendCtrlV();
                Thread.Sleep(45);

                // Detached task to restore original clipboard after target app processes paste
                if (previousData != null)
                {
                    Task.Run(async () =>
                    {
                        await Task.Delay(1200);
                        var restoreThread = new Thread(() =>
                        {
                            for (int retry = 0; retry < 5; retry++)
                            {
                                try
                                {
                                    Clipboard.SetDataObject(previousData, true);
                                    break;
                                }
                                catch
                                {
                                    Thread.Sleep(40);
                                }
                            }
                        });
                        restoreThread.SetApartmentState(ApartmentState.STA);
                        restoreThread.Start();
                    });
                }
            }
            catch
            {
                success = false;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join(1200);
        return success;
    }

    public static void SendUnicodeString(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        var inputs = new List<INPUT>();

        foreach (char c in text)
        {
            if (c == '\r') continue;

            if (c == '\n')
            {
                // Send Return key
                inputs.Add(new INPUT
                {
                    type = INPUT_KEYBOARD,
                    u = new InputUnion
                    {
                        ki = new KEYBDINPUT { wVk = VK_RETURN, wScan = 0, dwFlags = 0, time = 0, dwExtraInfo = IntPtr.Zero }
                    }
                });
                inputs.Add(new INPUT
                {
                    type = INPUT_KEYBOARD,
                    u = new InputUnion
                    {
                        ki = new KEYBDINPUT { wVk = VK_RETURN, wScan = 0, dwFlags = KEYEVENTF_KEYUP, time = 0, dwExtraInfo = IntPtr.Zero }
                    }
                });
                continue;
            }

            // Unicode keydown
            inputs.Add(new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = 0,
                        wScan = (ushort)c,
                        dwFlags = KEYEVENTF_UNICODE,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            });

            // Unicode keyup
            inputs.Add(new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = 0,
                        wScan = (ushort)c,
                        dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            });
        }

        if (inputs.Count > 0)
        {
            SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf(typeof(INPUT)));
        }
    }

    private static void SendCtrlV()
    {
        keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
        Thread.Sleep(15);
        keybd_event(VK_V, 0, 0, UIntPtr.Zero);
        Thread.Sleep(15);
        keybd_event(VK_V, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        Thread.Sleep(15);
        keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion u;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
}
