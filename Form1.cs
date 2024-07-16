using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using static System.Windows.Forms.LinkLabel;

namespace dev.sakurakooi.BattlefieldAntiAFK {
    public partial class Form1 : Form {
        public Form1() {
            InitializeComponent();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e) {
            if (checkBox1.Checked) {
                timer1.Enabled = true;
                timer1_Tick(null, null);
            } else {
                timer1.Enabled = false;
            }
        }

        [DllImport("user32.dll", EntryPoint = "keybd_event")]
        public static extern void Keybd_Event(WinVK bVk, uint bScan, uint dwFlags, uint dwExtraInfo);

        [DllImport("user32.dll")]
        public static extern uint MapVirtualKey(WinVK uCode, uint uMapType);

        [DllImport("user32.dll")]
        public static extern int SetForegroundWindow(IntPtr hwnd);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();


        [StructLayout(LayoutKind.Sequential)]
        public struct LASTINPUTINFO {
            [MarshalAs(UnmanagedType.U4)] public UInt32 cbSize;
            [MarshalAs(UnmanagedType.U4)] public UInt32 dwTime;
        }

        [DllImport("user32.dll")]
        public static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        private void timer1_Tick(object sender, EventArgs e) {
            var hwnd = new Func<IntPtr>(delegate() {
                var processes = Process.GetProcessesByName("bf1");
                foreach (var process in processes) {
                    if (process.MainWindowTitle.Equals("Battlefield™ 1")) {
                        return process.MainWindowHandle;
                    }
                }

                return default;
            }).Invoke();

            if (hwnd == default) {
                label1.BeginInvoke(new Action(() => { label1.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " 未找到BF1窗口"; }));
                return;
            }

            var currentForeground = GetForegroundWindow();

            if (currentForeground == hwnd) { // In game -> check idle
                if (!checkIdleTime()) { // Check system no any input for 15s
                    label1.BeginInvoke(new Action(() => { label1.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " 正在游戏中"; }));
                    return;
                }
            } // Outside game -> always do Anti-AFK

            SetForegroundWindow(hwnd);
            Thread.Sleep(2000);

            Keybd_Event(WinVK.TAB, MapVirtualKey(WinVK.TAB, 0), 0, 0);
            Thread.Sleep(200);
            Keybd_Event(WinVK.TAB, MapVirtualKey(WinVK.TAB, 0), 2, 0);
            label1.BeginInvoke(new Action(() => { label1.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " 模拟输入成功"; }));

            Thread.Sleep(500);
            if (currentForeground != hwnd) { // Restore foreground window
                SetForegroundWindow(currentForeground);
            }
        }

        private bool checkIdleTime() {
            uint idleTime = 0;
            LASTINPUTINFO lastInputInfo = new LASTINPUTINFO();
            lastInputInfo.cbSize = (uint) Marshal.SizeOf(lastInputInfo);
            lastInputInfo.dwTime = 0;

            uint envTicks = (uint) Environment.TickCount;

            if (GetLastInputInfo(ref lastInputInfo)) {
                uint lastInputTick = lastInputInfo.dwTime;
                idleTime = envTicks - lastInputTick;
            }

            idleTime /= 1000;
            return idleTime <= 15;
        }
    }
}