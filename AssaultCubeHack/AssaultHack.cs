using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using Utilities;

namespace AssaultCubeHack {
    class AssaultHack {

        //process to attach to
        Process process;

        //game objects
        Player self;
        List<Player> players = new List<Player>();

        //keyboard commands
        GlobalKeyboardHook gkh = new GlobalKeyboardHook();
        const Keys keyAim = Keys.CapsLock;
        bool aim = false;

        public AssaultHack(Process process) {
            this.process = process;

            //start thread for playing with memory
            Thread t = new Thread(Update);
            t.IsBackground = false;
            t.Start();

            //set up keyboard hooking
            gkh.HookedKeys.Add(keyAim);
            gkh.KeyDown += new KeyEventHandler(KeyDownEvent);
            gkh.KeyUp += new KeyEventHandler(KeyUpEvent);
            Application.Run();//required for hooking to register


        }

        /// <summary>
        /// Key event when a hooked key is pressed.
        /// </summary>
        private void KeyDownEvent(object sender, KeyEventArgs e)  {
            aim = (e.KeyCode == keyAim);

            //e.Handled = true;//prevent other programs from processing key
        }

        /// <summary>
        /// Key event when a hooked key is pressed.
        /// </summary>
        private void KeyUpEvent(object sender, KeyEventArgs e) {
            aim = !(e.KeyCode == keyAim);

            //e.Handled = true;//prevent other programs from processing key
        }


        public void Update() {
            //Attach to process
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Attaching...");
            try {
                IntPtr handle = Memory.OpenProcess(process.Id);
                Console.WriteLine("Attached Handle: " + handle);
            } catch (Exception e) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Attach failed: " + e.Message);
                Console.ReadKey(true);
                return;
            }

            //update loop
            while (true) {

                //read self
                int pointerPlayerSelf = Memory.Read<int>(Offsets.baseGame + Offsets.playerEntity);
                self = new Player(pointerPlayerSelf);

                //read players
                players.Clear();
                int numPlayers = Memory.Read<int>(Offsets.baseGame + Offsets.numplayers);
                int pointerPlayerArray = Memory.Read<int>(Offsets.baseGame + Offsets.playerArray);
                for (int i = 0; i < numPlayers - 1; i++) {
                    int pointerPlayer = Memory.Read<int>(pointerPlayerArray + (i + 1) * 0x4);
                    Player player = new Player(pointerPlayer);
                    players.Add(player);
                }


                //Test Hacks
                //self hacks, infinite health and ammo
                self.Health += 1;
                self.Ammo = 7331;
                self.AmmoClip = 999;
                //players.ForEach(p => p.Velocity = new Vector3(0, 0, 15));//send everyone to the ceiling
                //players.ForEach(p => p.Pitch = 90); //make everyone look up
                //players.ForEach(p => p.Position = new Vector3(130, 130, 10)); //set everyone to same spot
                //players.ForEach(p => p.Health = 0);//1 hit kills on anyone


                //aimbot
                if (aim) {
                    if (players.Count > 0) {
                        //find closest enemy player
                        Player target = players.Find(p => p.Team != self.Team && p.Health > 0);
                        if (target == null) {
                            target = players[0];
                        }                  
                        foreach (Player player in players) {
                            if (player.Team != self.Team && player.Health > 0 && player.Position.Distance(self.Position) < target.Position.Distance(self.Position))
                                target = player;
                        }                       

                        //calculate horizontal angle between enemy and player (yaw)
                        float dx = target.Position.X - self.Position.X;
                        float dy = target.Position.Y - self.Position.Y;
                        double angleYaw = Math.Atan2(dy, dx) * 180 / Math.PI;

                        //calculate verticle angle between enemy and player (pitch)
                        double distance = Math.Sqrt(dx * dx + dy * dy);
                        float dz = target.Position.Z - self.Position.Z;
                        double anglePitch = Math.Atan2(dz, distance) * 180 / Math.PI;

                        //set self angles to calculated angles
                        self.Yaw = (float)angleYaw + 90;
                        self.Pitch = (float)anglePitch;
                    }
                }
            
                Thread.Sleep(0);
            }
        }
    }
}
