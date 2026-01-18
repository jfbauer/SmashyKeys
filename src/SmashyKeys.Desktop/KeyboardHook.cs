using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SmashyKeys;

/// <summary>
/// Low-level keyboard hook to intercept and block system shortcuts.
/// This prevents toddlers from accidentally escaping the app via Windows key, Alt+Tab, etc.
/// Note: Ctrl+Alt+Delete CANNOT be blocked (Windows security feature) - this is the parent's exit method.
/// </summary>
public class KeyboardHook : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;

    // Virtual key codes
    private const int VK_LWIN = 0x5B;
    private const int VK_RWIN = 0x5C;
    private const int VK_TAB = 0x09;
    private const int VK_ESCAPE = 0x1B;
    private const int VK_F4 = 0x73;
    private const int VK_LALT = 0xA4;
    private const int VK_RALT = 0xA5;
    private const int VK_LCONTROL = 0xA2;
    private const int VK_RCONTROL = 0xA3;
    private const int VK_LSHIFT = 0xA0;
    private const int VK_RSHIFT = 0xA1;
    private const int VK_Q = 0x51;

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

    private IntPtr _hookId = IntPtr.Zero;
    private readonly LowLevelKeyboardProc _proc;
    private bool _disposed;

    // Track secret exit combo: Ctrl+Shift+Q held for 2 seconds
    private DateTime? _secretComboStartTime;
    private const int SecretExitHoldSeconds = 2;

    public event Action<int>? KeyPressed;
    public event Action? SecretExitTriggered;

    public KeyboardHook()
    {
        _proc = HookCallback;
    }

    public void Install()
    {
        try
        {
            Logger.Log("KeyboardHook.Install starting...");
            using var curProcess = Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule!;
            Logger.Log($"Got module: {curModule.ModuleName}");
            _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(curModule.ModuleName!), 0);
            Logger.Log($"Hook installed, hookId: {_hookId}");
        }
        catch (Exception ex)
        {
            Logger.LogException("KeyboardHook.Install", ex);
            throw;
        }
    }

    public void Uninstall()
    {
        if (_hookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
    }

    private bool IsKeyDown(int vKey) => (GetAsyncKeyState(vKey) & 0x8000) != 0;

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        try
        {
            if (nCode >= 0)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                bool isKeyDown = wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN;
                Logger.Log($"HookCallback: nCode={nCode}, vkCode={vkCode}, isKeyDown={isKeyDown}");

            // Check for secret parent exit combo: Ctrl+Shift+Q
            bool ctrlDown = IsKeyDown(VK_LCONTROL) || IsKeyDown(VK_RCONTROL);
            bool shiftDown = IsKeyDown(VK_LSHIFT) || IsKeyDown(VK_RSHIFT);

            if (ctrlDown && shiftDown && vkCode == VK_Q && isKeyDown)
            {
                if (_secretComboStartTime == null)
                {
                    _secretComboStartTime = DateTime.Now;
                }
                else if ((DateTime.Now - _secretComboStartTime.Value).TotalSeconds >= SecretExitHoldSeconds)
                {
                    SecretExitTriggered?.Invoke();
                    return CallNextHookEx(_hookId, nCode, wParam, lParam);
                }
            }
            else if (vkCode == VK_Q && !isKeyDown)
            {
                _secretComboStartTime = null;
            }

            // Block these keys/combos
            bool shouldBlock = false;

            // Block Windows keys
            if (vkCode == VK_LWIN || vkCode == VK_RWIN)
            {
                shouldBlock = true;
            }

            // Block Alt+Tab
            bool altDown = IsKeyDown(VK_LALT) || IsKeyDown(VK_RALT);
            if (altDown && vkCode == VK_TAB)
            {
                shouldBlock = true;
            }

            // Block Alt+F4
            if (altDown && vkCode == VK_F4)
            {
                shouldBlock = true;
            }

            // Block Alt+Escape
            if (altDown && vkCode == VK_ESCAPE)
            {
                shouldBlock = true;
            }

            // Block Ctrl+Escape (Start menu)
            if (ctrlDown && vkCode == VK_ESCAPE)
            {
                shouldBlock = true;
            }

            // Block plain Escape
            if (vkCode == VK_ESCAPE)
            {
                shouldBlock = true;
            }

            // Fire key pressed event for visual effects (only on key down, not blocked system keys)
            if (isKeyDown && !shouldBlock)
            {
                Logger.Log($"Firing KeyPressed event for vkCode={vkCode}");
                KeyPressed?.Invoke(vkCode);
                Logger.Log("KeyPressed event completed");
            }
            else if (isKeyDown && shouldBlock && vkCode != VK_LWIN && vkCode != VK_RWIN)
            {
                // Still fire visual event for blocked keys (except Windows keys)
                Logger.Log($"Firing KeyPressed event (blocked key) for vkCode={vkCode}");
                KeyPressed?.Invoke(vkCode);
                Logger.Log("KeyPressed event (blocked key) completed");
            }

            if (shouldBlock)
            {
                Logger.Log($"Blocking key vkCode={vkCode}");
                return (IntPtr)1; // Block the key
            }
            }

            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }
        catch (Exception ex)
        {
            Logger.LogException("HookCallback", ex);
            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Uninstall();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
