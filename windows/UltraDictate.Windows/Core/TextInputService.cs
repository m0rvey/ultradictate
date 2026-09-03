using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace UltraDictate.Windows.Core;

public static class TextInputService
{
    private const int INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const ushort VK_CONTROL = 0x11;
    private const ushort VK_V = 0x56;

    public static void InsertText(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        // Perform clipboard transaction:
        // 1. Save current clipboard
        // 2. Set transcribed text
        // 3. Synthesize Ctrl+V
        // 4. Restore original clipboard
        var thread = new Thread(() =>
        {
            IDataObject? previousData = null;
            try
            {
                if (Clipboard.ContainsText())
                {
                    previousData = Clipboard.GetDataObject();
                }

                Clipboard.SetText(text);
                SendCtrlV();

                Thread.Sleep(80); // Allow target window to read clipboard

                if (previousData != null)
                {
                    Clipboard.SetDataObject(previousData, true);
                }
            }
            catch
            {
                // Fallback direct typing
                SendKeys.SendWait(text);
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join(1000);
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
