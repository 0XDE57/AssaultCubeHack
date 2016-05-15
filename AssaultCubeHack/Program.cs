using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace AssaultCubeHack {
    class Program {

        static Player self;
        static List<Player> players = new List<Player>();

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
                    
                    /*
                    Console.WriteLine("Health: " + self.Health);
                    Console.WriteLine("Position: " + self.Position);
                    Console.WriteLine("Velocity: " + self.Velocity);
                    Console.WriteLine("Yaw: " + self.Yaw);
                    Console.WriteLine("Pitch: " + self.Pitch);
                    Console.WriteLine("Ammo: " + self.Ammo + "/" + self.AmmoClip);
                    */

                    players.Clear();
                    int numPlayers = Memory.Read<int>(Offsets.baseGame + Offsets.numplayers);
                    int pointerPlayerArray = Memory.Read<int>(Offsets.baseGame + Offsets.playerArray);
                    for (int i = 0; i < numPlayers-1; i++) {
                        int pointerPlayer = Memory.Read<int>(pointerPlayerArray + (i+1) * 0x4);
                        Player player = new Player(pointerPlayer);
                        players.Add(player);                    
                    }



                    //Console.WriteLine("-----------------");
                    foreach (Player p in players) {

                        //Console.WriteLine(p.Name + ": " + Math.Round(p.Position.Distance(self.Position)));
                        //Console.WriteLine(p.Name + ": " + p.Position);
                        //p.Velocity = new Vector3(0,0,5);//test, send everyone to the ceiling
                        //p.Position = new Vector3(130, 130, 10);
                        //p.Pitch = 90; //make everyone look up
                        //player.Yaw = 0;
                        //p.Health = 1000;
                    }


                    //test look at first player
                    if (players.Count > 0) {
                        //players.Find(p => p.Position.Distance(self.Position) > 15);
                        //double distance = players[0].Position.Distance(self.Position);
                        Player target = players[0];
                        foreach (Player player in players) {
                            if (player.Team != self.Team && player.Health > 0 && player.Position.Distance(self.Position) < target.Position.Distance(self.Position)) {
                                target = player;
                            }
                        }

                        //calculate horizontal angle between enemy and player (yaw)
                        float dx = target.Position.X - self.Position.X;
                        float dy = target.Position.Y - self.Position.Y;
                        double angleYaw = Math.Atan2(dy, dx) * 180 / Math.PI;

                        //calculate verticle angle between enemy and player (pitch)
                        double distance = Math.Sqrt(dx * dx + dy * dy);
                        float dz = target.Position.Z - self.Position.Z;
                        double anglePitch = Math.Atan2(dz, distance) * 180 / Math.PI;

                        //set angles to calculated angles
                        //self.Yaw = (float)angleYaw + 90;
                        //self.Pitch = (float)anglePitch;


                        //Console.Clear();
                        //Console.WriteLine(players[0].Name + ": " + self.Pitch + "-" + anglePitch + " - " + dz);
                    }

                    //Thread.Sleep(50);
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
                if ((Keys)vkCode == Keys.CapsLock) {
                    
                }


                if ((Keys)vkCode == Keys.PageUp) {
                    //send your player to ceiling
                    self.Velocity = new Vector3(0, 0, 10);
                }

                if ((Keys)vkCode == Keys.NumPad6) {
                    foreach(Player p in players) {
                        //p.Velocity = new Vector3(0, 0, 5);//test, send everyone to the ceiling
                        //p.Health = 0;
                        p.Position = new Vector3(130, 130, 10);
                    }
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
