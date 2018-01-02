using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace MusicPlayer.Core
{
    /// <summary>
    /// Partially pilfered from 
    /// https://bitbucket.org/josephcooney/learnwpf.com.samples/src/f2fb54ba7ecb/KeyboardHook/?at=default
    /// </summary>
    public class KeyboardHook : IDisposable
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private LowLevelKeyboardProc keyboardProc;
        private IntPtr hookId = IntPtr.Zero;

        const UInt32 SWP_NOSIZE = 0x0001;
        const UInt32 SWP_NOMOVE = 0x0002;
        const UInt32 SWP_SHOWWINDOW = 0x0040;

        private HashSet<Key> SelectedKeys;

        public KeyboardHook(ICollection<Key> keys)
        {
            keyboardProc = HookCallback;
            hookId = SetHook(keyboardProc);

            SelectedKeys = new HashSet<Key>(keys);
        }

        public void Dispose()
        {
            UnhookWindowsHookEx(hookId);
        }

        public event EventHandler<Key> RegisteredKeyPressed;

        public void OnRegisteredKeyPressed(Key key)
        {
            RegisteredKeyPressed?.Invoke(this, key);
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(
                    idHook: WH_KEYBOARD_LL,
                    lpfn: proc,
                    hMod: GetModuleHandle(curModule.ModuleName),
                    dwThreadId: 0);
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Key keyPressed = KeyInterop.KeyFromVirtualKey(vkCode);

                //Trace.WriteLine(keyPressed);
                if (SelectedKeys.Contains(keyPressed))
                {
                    //Trace.WriteLine("Triggering Keyboard Hook");
                    OnRegisteredKeyPressed(keyPressed);
                }
            }
            return CallNextHookEx(hookId, nCode, wParam, lParam);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(
            int idHook,
            LowLevelKeyboardProc lpfn,
            IntPtr hMod,
            uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(
            IntPtr hhk,
            int nCode,
            IntPtr wParam,
            IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

    }
}
