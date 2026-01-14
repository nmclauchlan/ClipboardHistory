using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace ClipboardHistory;

public class GlobalKeyboardHook
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;

    private IntPtr _hookId = IntPtr.Zero;
    private readonly LowLevelKeyboardProc _proc;

    public int TargetVirtualKey { get; set; } = 0x91; // VK_SCROLL (Scroll Lock)
    public ModifierKeys RequiredModifiers { get; set; } = ModifierKeys.None;

    public event EventHandler? HotkeyPressed;

    public GlobalKeyboardHook()
    {
        _proc = HookCallback;
    }

    public void Start()
    {
        _hookId = SetHook(_proc);
    }

    public void Stop()
    {
        if (_hookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
    }

    public void UpdateHotkey(int virtualKey, ModifierKeys modifiers)
    {
        TargetVirtualKey = virtualKey;
        RequiredModifiers = modifiers;
    }

    private IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule!;
        return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
        {
            int vkCode = Marshal.ReadInt32(lParam);

            if (vkCode == TargetVirtualKey && CheckModifiers())
            {
                HotkeyPressed?.Invoke(this, EventArgs.Empty);
            }
        }

        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    private bool CheckModifiers()
    {
        if (RequiredModifiers == ModifierKeys.None)
            return true;

        bool ctrlRequired = RequiredModifiers.HasFlag(ModifierKeys.Control);
        bool altRequired = RequiredModifiers.HasFlag(ModifierKeys.Alt);
        bool shiftRequired = RequiredModifiers.HasFlag(ModifierKeys.Shift);
        bool winRequired = RequiredModifiers.HasFlag(ModifierKeys.Windows);

        bool ctrlPressed = (GetAsyncKeyState(0x11) & 0x8000) != 0; // VK_CONTROL
        bool altPressed = (GetAsyncKeyState(0x12) & 0x8000) != 0;  // VK_MENU (Alt)
        bool shiftPressed = (GetAsyncKeyState(0x10) & 0x8000) != 0; // VK_SHIFT
        bool winPressed = (GetAsyncKeyState(0x5B) & 0x8000) != 0 || (GetAsyncKeyState(0x5C) & 0x8000) != 0; // VK_LWIN/VK_RWIN

        return ctrlRequired == ctrlPressed &&
               altRequired == altPressed &&
               shiftRequired == shiftPressed &&
               winRequired == winPressed;
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
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);
}
