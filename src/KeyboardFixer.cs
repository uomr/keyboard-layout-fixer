using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace KeyboardFixer
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FixerApp());
        }
    }

    public class FixerApp : ApplicationContext
    {
        // ── Win32 ───────────────────────────────────────────────────────────
        delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        struct KBDLLHOOKSTRUCT { public uint vkCode, scanCode, flags, time; public IntPtr dwExtraInfo; }

        [StructLayout(LayoutKind.Sequential)]
        struct INPUT { public uint type; public INPUTUNION u; }

        [StructLayout(LayoutKind.Explicit)]
        struct INPUTUNION { [FieldOffset(0)] public KEYBDINPUT ki; }

        [StructLayout(LayoutKind.Sequential)]
        struct KEYBDINPUT
        {
            public ushort wVk, wScan;
            public uint dwFlags, time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll")] static extern IntPtr SetWindowsHookEx(int h, LowLevelKeyboardProc p, IntPtr m, uint t);
        [DllImport("user32.dll")] static extern bool UnhookWindowsHookEx(IntPtr h);
        [DllImport("user32.dll")] static extern IntPtr CallNextHookEx(IntPtr h, int n, IntPtr w, IntPtr l);
        [DllImport("kernel32.dll")] static extern IntPtr GetModuleHandle(string m);
        [DllImport("user32.dll")] static extern uint SendInput(uint n, INPUT[] p, int s);
        [DllImport("user32.dll")] static extern short GetKeyState(int k);
        [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr h);
        [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] static extern IntPtr GetFocus();
        [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        [DllImport("user32.dll")] static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);
        [DllImport("user32.dll", CharSet = CharSet.Auto)] static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll")] static extern uint GetCurrentThreadId();

        const uint WM_COPY = 0x0301;
        const uint WM_PASTE = 0x0302;

        static IntPtr GetFocusedControlHandle()
        {
            IntPtr activeWnd = GetForegroundWindow();
            if (activeWnd == IntPtr.Zero) return IntPtr.Zero;

            uint pid;
            uint activeThreadId = GetWindowThreadProcessId(activeWnd, out pid);
            uint currentThreadId = GetCurrentThreadId();

            if (activeThreadId != currentThreadId)
            {
                AttachThreadInput(currentThreadId, activeThreadId, true);
                IntPtr focusWnd = GetFocus();
                AttachThreadInput(currentThreadId, activeThreadId, false);
                return focusWnd != IntPtr.Zero ? focusWnd : activeWnd;
            }
            return activeWnd;
        }

        // keybd_event for releasing modifiers
        [DllImport("user32.dll")] static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        const int INPUT_KEYBOARD = 1;
        const uint KEYEVENTF_UNICODE = 0x0004;
        const uint KEYEVENTF_KEYUP = 0x0002;
        const int WH_KEYBOARD_LL = 13;
        const int WM_KEYDOWN = 0x0100;
        const int WM_SYSKEYDOWN = 0x0104;
        const byte VK_CTRL = 0x11;
        const byte VK_SHIFT = 0x10;
        const byte VK_ALT = 0x12;
        const byte VK_C = 0x43;
        const byte VK_V = 0x56;
        const byte VK_Z = 0x5A;
        const byte VK_X = 0x58;
        const byte VK_Q = 0x51;

        // ── Maps ────────────────────────────────────────────────────────────
        static readonly Dictionary<char, string> EN2AR = new Dictionary<char, string>();
        static readonly Dictionary<char, string> AR2EN = new Dictionary<char, string>();

        static void BuildMaps()
        {
            // Format: EN char -> AR string (multi-char allowed for ligatures in AR2EN key)
            string[] rows = new string[]
            {
                "`ذ","qض","wص","eث","rق","tف","yغ","uع","iه","oخ","pح","[ج","]د",
                "aش","sس","dي","fب","gل","hا","jت","kن","lم",";ك","'ط",
                "zئ","xء","cؤ","vر","bلا","nى","mة",",و",".ز","/ظ",
                // Shifted
                "~ّ","Qَ","Wً","Eُ","Rٌ","Tلإ","Yإ","U'","I÷","O×","P؛","{<","}>" ,
                "Aِ","Sٍ","D]","F[","Gلأ","Hأ","Jـ","K،","L/","::","\"\"",
                "Z~","Xْ","C}","V{","Bلآ","Nآ","M'","<,",">.","?؟"
            };

            foreach (string row in rows)
            {
                if (row.Length < 2) continue;
                char en = row[0];
                string ar = row.Substring(1);
                if (!EN2AR.ContainsKey(en)) EN2AR[en] = ar;
                if (ar.Length == 1 && !AR2EN.ContainsKey(ar[0]))
                    AR2EN[ar[0]] = en.ToString();
            }

            // Ligatures AR->EN
            AR2EN['ل'] = "g"; // fallback

            // Pass-through: space, digits, common symbols
            string passthru = " \t\r\n0123456789!@#$%^&*()-_=+|\\";
            foreach (char c in passthru)
            {
                if (!EN2AR.ContainsKey(c)) EN2AR[c] = c.ToString();
                if (!AR2EN.ContainsKey(c)) AR2EN[c] = c.ToString();
            }
        }

        static string ArToEnHandle(string text)
        {
            // Handle ligatures first
            text = text.Replace("لآ", "B").Replace("لإ", "T")
                       .Replace("لأ", "G").Replace("لا", "b");
            var sb = new StringBuilder();
            foreach (char c in text)
            {
                if (AR2EN.ContainsKey(c)) sb.Append(AR2EN[c]);
                else sb.Append(c);
            }
            return sb.ToString();
        }

        // Is this string an Arabic letter (0x0600–0x06FF range)?
        static bool IsArabicLetter(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            int code = (int)s[0];
            return code >= 0x0600 && code <= 0x06FF;
        }

        static string EnToArHandle(string text)
        {
            var sb = new StringBuilder();
            foreach (char c in text)
            {
                string result;
                if (EN2AR.ContainsKey(c))
                {
                    result = EN2AR[c];

                    // Smart Caps-Lock / accidental-Shift fix:
                    // If this uppercase letter maps to a NON-Arabic symbol (like I→÷, U→')
                    // but its lowercase counterpart maps to an Arabic LETTER (i→ه, u→ع),
                    // prefer the lowercase Arabic mapping.
                    if (char.IsUpper(c) && !IsArabicLetter(result))
                    {
                        char lower = char.ToLower(c);
                        if (EN2AR.ContainsKey(lower) && IsArabicLetter(EN2AR[lower]))
                            result = EN2AR[lower];
                    }
                }
                else
                {
                    result = c.ToString();
                }
                sb.Append(result);
            }
            return sb.ToString();
        }

        static string ConvertSmart(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            int en = 0, ar = 0;
            foreach (char c in text)
            {
                int code = (int)c;
                if ((code >= 65 && code <= 90) || (code >= 97 && code <= 122)) en++;
                else if (code >= 0x0600 && code <= 0x06FF) ar++;
            }
            return (ar >= en && ar > 0) ? ArToEnHandle(text) : EnToArHandle(text);
        }

        // ── SendInput Unicode ───────────────────────────────────────────────
        static void TypeUnicode(string text)
        {
            var list = new List<INPUT>();
            foreach (char c in text)
            {
                // Key down
                list.Add(new INPUT
                {
                    type = INPUT_KEYBOARD,
                    u = new INPUTUNION { ki = new KEYBDINPUT { wVk = 0, wScan = (ushort)c, dwFlags = KEYEVENTF_UNICODE } }
                });
                // Key up
                list.Add(new INPUT
                {
                    type = INPUT_KEYBOARD,
                    u = new INPUTUNION { ki = new KEYBDINPUT { wVk = 0, wScan = (ushort)c, dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP } }
                });
            }
            INPUT[] arr = list.ToArray();
            SendInput((uint)arr.Length, arr, Marshal.SizeOf(typeof(INPUT)));
        }

        // ── Release all modifier keys ───────────────────────────────────────
        static void ReleaseModifiers()
        {
            keybd_event(VK_ALT,   0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            keybd_event(VK_CTRL,  0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            keybd_event(VK_SHIFT, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }

        // ── Clipboard helpers (must be called on STA thread) ────────────────
        static string SafeGetClipboard()
        {
            try { return Clipboard.ContainsText() ? Clipboard.GetText() : ""; }
            catch { return ""; }
        }

        static void SafeSetClipboard(string text)
        {
            try
            {
                if (string.IsNullOrEmpty(text)) Clipboard.Clear();
                else Clipboard.SetText(text);
            }
            catch { }
        }

        // ── Main conversion logic ───────────────────────────────────────────
        volatile bool _busy = false;

        void DoConvert()
        {
            if (_busy) return;
            _busy = true;
            try
            {
                // 1. Release ALL modifier keys first
                ReleaseModifiers();
                Thread.Sleep(80);

                // Get focused target handle
                IntPtr hTarget = GetFocusedControlHandle();

                // 2. Backup existing clipboard
                string backup = SafeGetClipboard();
                Clipboard.Clear();

                // 3. Try Ctrl+C first
                keybd_event(VK_CTRL, 0, 0, UIntPtr.Zero);
                keybd_event(VK_C, 0, 0, UIntPtr.Zero);
                Thread.Sleep(35);
                keybd_event(VK_C, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                keybd_event(VK_CTRL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

                string selected = "";
                for (int i = 0; i < 15; i++)
                {
                    Thread.Sleep(25);
                    selected = SafeGetClipboard();
                    if (!string.IsNullOrEmpty(selected)) break;
                }

                // Fallback for legacy ERP controls: Send WM_COPY directly to focused window handle
                if (string.IsNullOrEmpty(selected) && hTarget != IntPtr.Zero)
                {
                    SendMessage(hTarget, WM_COPY, IntPtr.Zero, IntPtr.Zero);
                    Thread.Sleep(50);
                    selected = SafeGetClipboard();
                }

                if (string.IsNullOrEmpty(selected))
                {
                    SafeSetClipboard(backup);
                    return;
                }

                // 4. Convert the text
                string converted = ConvertSmart(selected);
                if (converted == selected)
                {
                    SafeSetClipboard(backup);
                    return;
                }

                // 5. Put converted text in clipboard
                SafeSetClipboard(converted);
                Thread.Sleep(40);

                // 6. Try Ctrl+V paste
                keybd_event(VK_CTRL, 0, 0, UIntPtr.Zero);
                keybd_event(VK_V, 0, 0, UIntPtr.Zero);
                Thread.Sleep(35);
                keybd_event(VK_V, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                keybd_event(VK_CTRL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

                // Fallback for legacy controls: Send WM_PASTE directly
                if (hTarget != IntPtr.Zero)
                {
                    Thread.Sleep(30);
                    SendMessage(hTarget, WM_PASTE, IntPtr.Zero, IntPtr.Zero);
                }

                // 7. Restore original clipboard
                Thread.Sleep(150);
                SafeSetClipboard(backup);

                // 8. Beep confirmation
                System.Media.SystemSounds.Asterisk.Play();
            }
            catch { }
            finally { _busy = false; }
        }

        // ── Hook ────────────────────────────────────────────────────────────
        IntPtr _hookId = IntPtr.Zero;
        LowLevelKeyboardProc _proc; // kept as field to prevent GC

        IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
            {
                var kb = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
                bool alt  = (GetKeyState(VK_ALT)   & 0x8000) != 0;
                bool ctrl = (GetKeyState(VK_CTRL)  & 0x8000) != 0;
                bool shft = (GetKeyState(VK_SHIFT) & 0x8000) != 0;

                bool trigger =
                    (alt  && kb.vkCode == VK_Z) ||           // Alt + Z
                    (ctrl && shft && kb.vkCode == VK_X) ||   // Ctrl + Shift + X
                    (ctrl && shft && kb.vkCode == VK_Q);     // Ctrl + Shift + Q

                if (trigger)
                {
                    // Spawn STA thread (clipboard requires STA)
                    Thread t = new Thread(() => DoConvert());
                    t.SetApartmentState(ApartmentState.STA);
                    t.IsBackground = true;
                    t.Start();
                    return (IntPtr)1; // consume key
                }
            }
            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        // ── Tray ────────────────────────────────────────────────────────────
        NotifyIcon _tray;

        public FixerApp()
        {
            BuildMaps();

            _proc = HookCallback;
            using (var p = System.Diagnostics.Process.GetCurrentProcess())
            using (var m = p.MainModule)
                _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(m.ModuleName), 0);

            // Auto-start with Windows via registry
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Run", true))
                    key.SetValue("KeyboardFixer", "\"" + Application.ExecutablePath + "\"");
            }
            catch { }

            _tray = new NotifyIcon
            {
                Visible = true,
                Icon = System.Drawing.SystemIcons.Application,
                Text = "مصحح الكيبورد يعمل — Alt+Z أو Ctrl+Shift+X"
            };

            var menu = new ContextMenuStrip();
            menu.Items.Add("⌨  Hotkeys: Alt+Z  /  Ctrl+Shift+X").Enabled = false;
            menu.Items.Add("-");
            menu.Items.Add("❌ إيقاف", null, (s, e) =>
            {
                UnhookWindowsHookEx(_hookId);
                _tray.Visible = false;
                Application.Exit();
            });
            _tray.ContextMenuStrip = menu;

            _tray.ShowBalloonTip(3000, "✅ مصحح الكيبورد يعمل",
                "ظلل أي نص واضغط Alt+Z لتحويل اللغة في أي تطبيق!", ToolTipIcon.Info);
        }
    }
}
