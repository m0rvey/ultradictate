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
    private const ushort VK_CONTROL = 0x11;
    private const ushort VK_RETURN = 0x0D;
    private const ushort VK_V = 0x56;

    public static void InsertText(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        // If text is short or moderately sized (< 250 chars), direct Unicode typing is
        // instantaneous, 100% reliable, never collides with clipboard, and works with Russian / code.
        if (text.Length < 250)
        {
            SendUnicodeString(text);
            return;
        }

        // For large text blocks, attempt high-speed clipboard paste with safe retry & restore
        bool clipboardPasted = TryClipboardPaste(text);
        if (!clipboardPasted)
        {
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
                // Retry opening clipboard up to 8 times if locked by another app
                for (int i = 0; i < 8; i++)
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

                // Release physical Ctrl if held, then synthesize Ctrl+V
                SendCtrlV();

                // Detached task to restore original clipboard after target app consumes it
                if (previousData != null)
                {
                    Task.Run(async () =>
                    {
                        await Task.Delay(350);
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
                                    Thread.Sleep(30);
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
        thread.Join(800);
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
        var inputs = new INPUT[4];

        // Ctrl down
        inputs[0].type = INPUT_KEYBOARD;
        inputs[0].u.ki.wVk = VK_CONTROL;

        // V down
        inputs[1].type = INPUT_KEYBOARD;
        inputs[1].u.ki.wVk = VK_V;

        // V up
        inputs[2].type = INPUT_KEYBOARD;
        inputs[2].u.ki.wVk = VK_V;
        inputs[2].u.ki.dwFlags = KEYEVENTF_KEYUP;

        // Ctrl up
        inputs[3].type = INPUT_KEYBOARD;
        inputs[3].u.ki.wVk = VK_CONTROL;
        inputs[3].u.ki.dwFlags = KEYEVENTF_KEYUP;

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
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
