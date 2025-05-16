using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

public static class HotkeyManager
{
    //MY HOTKEYS
    public const int TOGGLE_SHOW_HK = 1;
    public const int OPEN_CMD_HK = 2;
    public const int OPEN_BLUETOOTH_SHARE_HK = 3;

    private static Dictionary<int, Action> _hotkeyActions = new();
    //private static int _currentId = 0;
    private static IntPtr _windowHandle;

    // Modifier keys
    public const int MOD_ALT = 0x1;
    public const int MOD_CONTROL = 0x2;
    public const int MOD_SHIFT = 0x4;
    public const int MOD_WIN = 0x8;

    // WinAPI imports
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    public static void Init(Form form)
    {
        _windowHandle = form.Handle;
        Application.AddMessageFilter(new HotkeyMessageFilter());
    }

    public static int RegisterHotkey(Keys key, int modifiers, int id, Action callback)
    {
        if (!RegisterHotKey(_windowHandle, id, modifiers, (int)key))
            throw new InvalidOperationException("Hotkey registration failed.");
        _hotkeyActions[id] = callback;
        return id;
    }

    public static void UnregisterHotkey(int id)
    {
        UnregisterHotKey(_windowHandle, id);
        _hotkeyActions.Remove(id);
    }

    private class HotkeyMessageFilter : IMessageFilter
    {
        private const int WM_HOTKEY = 0x0312;

        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg == WM_HOTKEY && _hotkeyActions.TryGetValue((int)m.WParam, out var action))
            {
                action?.Invoke();
                return true;
            }
            return false;
        }
    }
}
