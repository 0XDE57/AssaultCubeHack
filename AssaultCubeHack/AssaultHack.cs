using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Utilities;

namespace AssaultCubeHack {
    public partial class AssaultHack : Form {

        //graphics to draw on screen
        Graphics graphics;
        //color used for transparency. anything draw in same color will not show up.
        Color colorTransparencyKey = Color.Black;

        //game objects
        Player self;
        List<Player> players = new List<Player>();

        //keyboard commands
        GlobalKeyboardHook gkh = new GlobalKeyboardHook();
        const Keys keyAim = Keys.CapsLock;
        bool aim = false;

        public AssaultHack() {         
            InitializeComponent();
         
            this.WindowState = FormWindowState.Maximized; //maximize window
            this.TopMost = true; //set window on top of all others
            this.FormBorderStyle = FormBorderStyle.None; //remove form controls
            picBoxOverlay.Dock = DockStyle.Fill; //fill window with picturebox graphics
            picBoxOverlay.BackColor = colorTransparencyKey;
        }

        /// <summary>
        /// Set form to fully transparent.
        /// https://msdn.microsoft.com/en-us/library/windows/desktop/ff700543%28v=vs.85%29.aspx
        /// </summary>
        protected override CreateParams CreateParams {
            get {
                new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand();
                CreateParams CP = base.CreateParams;
                int WS_EX_TRANSPARENT = 0x00000020;
                CP.ExStyle = CP.ExStyle | WS_EX_TRANSPARENT;
                return CP;
            }
        }

        public void ClearScreen() {
            //fill screen with chosen transparent color
            graphics.FillRectangle(new SolidBrush(colorTransparencyKey), new Rectangle(0,0,this.Width, this.Height));
        }

        private void AssaultHack_Load(object sender, EventArgs e) {
            //initialize graphics
            graphics = picBoxOverlay.CreateGraphics();

            //taget process
            Process process;

            //try to find game
            if (Memory.GetProcessesByName("ac_client", out process)) {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Process found: " + process.Id + ": " + process.ProcessName);       
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Attaching...");

                //Attach to process
                try {
                    IntPtr handle = Memory.OpenProcess(process.Id);
                    Console.WriteLine("Attached Handle: " + handle);
                } catch (Exception ex) {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Attach failed: " + ex.Message);
                    Console.ReadKey(true);
                    return;
                }
            } else {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Process not found.");
                Console.ReadKey(true);
                return;
            }
           

            //start thread for playing with memory
            Thread t = new Thread(UpdateHack);
            t.IsBackground = false;
            t.Start();


            //set up keyboard hooking
            gkh.HookedKeys.Add(keyAim);
            gkh.KeyDown += new KeyEventHandler(KeyDownEvent);
            gkh.KeyUp += new KeyEventHandler(KeyUpEvent);
        }

        /// <summary>
        /// Key event when a hooked key is pressed.
        /// </summary>
        private void KeyDownEvent(object sender, KeyEventArgs e) {
            aim = (e.KeyCode == keyAim);

            //e.Handled = true;//prevent other programs from processing key
        }

        /// <summary>
        /// Key event when a hooked key is pressed.
        /// </summary>
        private void KeyUpEvent(object sender, KeyEventArgs e) {
            if (e.KeyCode == keyAim) {
                aim = false;
            }

            //e.Handled = true;//prevent other programs from processing key
        }


        public void UpdateHack() {

            //update loop
            while (true) {
                //test graphics
                graphics.DrawString("Working!!!", new Font("Comic Sans MS", 20), Brushes.Blue, 20, 20);
                graphics.DrawEllipse(Pens.Red, this.Width / 2 - 50, this.Height / 2 - 50, 100, 100);
                graphics.DrawLine(Pens.Green, 0, 0, this.Width, this.Height);
                graphics.DrawLine(Pens.Green, 0, this.Height, this.Width, 0);

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
