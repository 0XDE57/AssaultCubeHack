using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace AssaultCubeHack {
    class Program {
        //Low level key hooking
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;
        private delegate IntPtr LowLevelKeyboardProc( int nCode, IntPtr wParam, IntPtr lParam);

        

        static void Main(string[] args) {
            //start thread for playing with memory
            Thread t = new Thread(Update);
            t.IsBackground = true;
            t.Start();

            //set keyboard hooking
            _hookID = SetHook(_proc);
            Application.Run();
            UnhookWindowsHookEx(_hookID);
        }
        

        public static void Update() {
            Player self;
            List<Player> players = new List<Player>();

            //foreach (Process p in Process.GetProcesses()) Console.WriteLine(p.Id + ": " + p.ProcessName);

            Process process;
            if (Memory.GetProcessesByName("ac_client", out process)) {
                Console.WriteLine("Process found: " + process.Id + ": " + process.ProcessName);
                Console.WriteLine("Attaching");
                IntPtr handle = Memory.OpenProcess(process.Id);
                Console.WriteLine("Handle: " + handle);
                Thread.Sleep(1000);
                while (true) {
                    Console.Clear();
                    //int game = Memory.Read<int>(offset_Game);
                    int pointerPlayerSelf = Memory.Read<int>(Offsets.baseGame + Offsets.playerEntity);
                    self = new Player(pointerPlayerSelf);
                    self.Health = 1337;
                    self.Ammo = 7331;
                    self.AmmoClip = 999;
                    
                    //int pointerPlayer = Memory.Read<int>(0x509b74);
                    Console.WriteLine("Health: " + self.Health);
                    Console.WriteLine("Position: " + self.Position);
                    Console.WriteLine("Velocity: " + self.Velocity);
                    Console.WriteLine("Yaw: " + self.Yaw);
                    Console.WriteLine("Pitch: " + self.Pitch);
                    Console.WriteLine("Ammo: " + self.Ammo + "/" + self.AmmoClip);


                    Console.WriteLine("-----------------");
                    int numPlayers = Memory.Read<int>(Offsets.baseGame + Offsets.numplayers);
                    int pointerPlayerArray = Memory.Read<int>(Offsets.baseGame + Offsets.playerArray);
                    for (int i = 0; i < numPlayers-1; i++) {
                        int pointerPlayer = Memory.Read<int>(pointerPlayerArray + (i+1) * 0x4);

                        Player player = new Player(pointerPlayer);
                       
                        Console.WriteLine(player.name + ": " + player.Position + " : " + player.Velocity);

                        //player.Velocity = new Vector3(0,0,5);//test, send everyone to the ceiling
                        
                    }

                    //test messing with player
                    //Memory.Write<float>(pointerPlayer + Offsets.yaw, 360);
                    //Memory.Write<Int32>(pointerPlayer + healthOffset, 100);
                    //p += 0.5f;
                    //Memory.Write<float>(pointerPlayer + yaw, p);
                    Thread.Sleep(100);
                }
            } else {
                Console.WriteLine("Process not found");
            }


            Console.ReadKey(true);
        }


        private static IntPtr SetHook(LowLevelKeyboardProc proc) {
            using (Process curProcess = Process.GetCurrentProcess()) {
                using (ProcessModule curModule = curProcess.MainModule) {
                    return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
                }
            }
        }

       

        private static IntPtr HookCallback(
            int nCode, IntPtr wParam, IntPtr lParam) {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN) {
                int vkCode = Marshal.ReadInt32(lParam);

                //Console.WriteLine((Keys)vkCode);

                if ((Keys)vkCode == Keys.PageUp) {
                    //Int32 pointerPlayer = Memory.Read<Int32>(playerEntity);
                    //Memory.WriteVector3(pointerPlayer + velocity, new Vector3(0, 0, 10));
                }
                if ((Keys)vkCode == Keys.PageDown) {
                    //Int32 pointerPlayer = Memory.Read<Int32>(playerEntity);
                    //Vector3 pos = Memory.ReadVector3(pointerPlayer + position);
                    //pos.X += 2;
                    //Memory.WriteVector3(pointerPlayer + position, pos);
                }

            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

    }

   
}
