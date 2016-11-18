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

        //target process
        const string processName = "ac_client";
        private Process process;

        //initialize rendering stuff
        BufferedGraphics bufferedGraphics;
        BufferedGraphicsContext buffContext = new BufferedGraphicsContext();
        Font font = new Font(FontFamily.GenericMonospace, 15, FontStyle.Bold);
        //color used for transparency. anything drawn in same color will not show up.
        Color colorTransparencyKey = Color.Black;

        //threads for updating rendering
        private Thread overlayThread;
        private Thread windowPosThread;
        private volatile bool isRunning = false;

        //game objects
        Player self;
        List<Player> players = new List<Player>();

        //keyboard commands
        GlobalKeyboardHook gkh = new GlobalKeyboardHook();
        const Keys keyAim = Keys.CapsLock;
        bool aim = false;

        public AssaultHack() {
            InitializeComponent();

            //set up window and overlay properties for drawing on top of another process
            this.WindowState = FormWindowState.Maximized; //maximize window
            this.TopMost = true; //set window on top of all others
            this.FormBorderStyle = FormBorderStyle.None; //remove form controls
            picBoxOverlay.Dock = DockStyle.Fill; //fill window with picturebox graphics
            picBoxOverlay.BackColor = colorTransparencyKey; //set overlay to transparent color
            this.TransparencyKey = colorTransparencyKey; //set tranparency key
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


        private void AssaultHack_Load(object sender, EventArgs e) {
            //initialize graphics
            bufferedGraphics = buffContext.Allocate(picBoxOverlay.CreateGraphics(), ClientRectangle);

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


            //start thread flag
            isRunning = true;

            //start thread for playing with memory and drawing overlay
            overlayThread = new Thread(UpdateHack);
            overlayThread.IsBackground = false;
            overlayThread.Start();

            //start thread for positioning and sizing overlay on top of target process
            windowPosThread = new Thread(UpdateWindow);
            windowPosThread.IsBackground = false;
            windowPosThread.Start(this.Handle);


            //set up keyboard hooking
            gkh.HookedKeys.Add(keyAim);
            gkh.KeyDown += new KeyEventHandler(KeyDownEvent);
            gkh.KeyUp += new KeyEventHandler(KeyUpEvent);
        }

        /// <summary>
        /// Thread to make sure overlay is positioned over target process.
        /// Checks to make sure process is still running.
        /// </summary>
        /// <param name="handle">Handle of overlay form</param>
        private void UpdateWindow(object handle) {
            while (isRunning) {
                isRunning = Memory.IsProcessRunning(process);

                SetOverlayPosition((IntPtr)handle);
                Thread.Sleep(1000);
            }


        }

        /// <summary>
        /// Set window position and size to overlay target process.
        /// </summary>
        /// <param name="handle">Handle of overlay form(this form/self)</param>
        private void SetOverlayPosition(IntPtr handle) {

            //get window handle
            IntPtr targetHandle = Managed.FindWindow(null, process.MainWindowTitle);
            if (targetHandle == IntPtr.Zero)
                return;

            //get position and size of window
            RECT targetWindowPosition, targetWindowSize;
            Managed.GetWindowRect(targetHandle, out targetWindowPosition);
            Managed.GetClientRect(targetHandle, out targetWindowSize);

            //calculate width and height of full target window
            int width = targetWindowPosition.Right - targetWindowPosition.Left;
            int height = targetWindowPosition.Bottom - targetWindowPosition.Top;

            //check if window has borders
            int dwStyle = Managed.GetWindowLong(targetHandle, Managed.GWL_STYLE);
            if ((dwStyle & Managed.WS_BORDER) != 0) {
                //calculate inner window size without borders      
                int bWidth = targetWindowPosition.Right - targetWindowPosition.Left;
                int bHeight = targetWindowPosition.Bottom - targetWindowPosition.Top;

                width = targetWindowSize.Right - targetWindowSize.Left;
                height = targetWindowSize.Bottom - targetWindowSize.Top;

                int borderWidth = (bWidth - targetWindowSize.Right) / 2;
                int borderHeight = (bHeight - targetWindowSize.Bottom);
                borderHeight -= borderWidth; //remove bottom

                targetWindowPosition.Left += borderWidth;
                targetWindowPosition.Top += borderHeight;
            }

            //move and resize self window to match target window
            Managed.MoveWindow(handle, targetWindowPosition.Left, targetWindowPosition.Top, width, height, true);
        }

        /// <summary>
        /// Key event when a hooked key is pressed.
        /// </summary>
        private void KeyDownEvent(object sender, KeyEventArgs e) {
            aim = (e.KeyCode == keyAim);

            e.Handled = true;//prevent other programs from processing key
        }

        /// <summary>
        /// Key event when a hooked key is pressed.
        /// </summary>
        private void KeyUpEvent(object sender, KeyEventArgs e) {
            if (e.KeyCode == keyAim) {
                aim = false;
            }

            e.Handled = true;//prevent other programs from processing key
        }


        public void UpdateHack() {

            //update loop
            while (isRunning) {

                //read
                ReadGameMemory();


                //Test Hacks
                //self hacks, infinite health and ammo
                /*
                self.Health = 99999;
                self.weapon.Ammo = 7331;
                self.weapon.AmmoClip = 999;
                self.weapon.DelayTime = 0;//rapid fire*/

                //players.ForEach(p => p.Velocity = new Vector3(0, 0, 15));//send everyone to the ceiling
                //players.ForEach(p => p.Pitch = 90); //make everyone look up
                //players.ForEach(p => p.Position = new Vector3(130, 130, 10)); //set everyone to same spot
                //players.ForEach(p => p.Health = 0);//1 hit kills on anyone

                //aimbot
                UpdateAimbot();

                //draw
                Draw(bufferedGraphics.Graphics);


                Thread.Sleep(1);
            }
        }

        private void UpdateAimbot() {
            //if not aiming or no players, escape
            if (!aim || players.Count == 0) return;

            //find closest enemy player
            Player target = GetClosestEnemy();
            if (target == null) return;

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

        private Player GetClosestEnemy() {           
            Player target = players.Find(p => p.Team != self.Team && p.Health > 0);
            if (target == null) return null;

            foreach (Player player in players) {
                if (player.Team != self.Team && player.Health > 0 && player.Position.Distance(self.Position) < target.Position.Distance(self.Position))
                    target = player;
            }

            return target;
        }

        private void Draw(Graphics g) {
            //clear
            ClearScreen(g);

            //test draw player list
            
            int spacing = (int)font.Size + 1;
            int s = 0;
            //cant draw with alpha due to window transperency? (127 and below = translucent, 128 and above = opaque. no in inbetween) 
            //g.FillRectangle(new SolidBrush(Color.FromArgb(127, 255, 0, 0)), 20, 20, 150, spacing * players.Count);
            foreach (Player p in players) {
                g.DrawString(p.Name, font, p.Team == self.Team ? Brushes.Green : Brushes.Red, 20, 20 + (s * spacing));
                s++;
            }

            //render
            bufferedGraphics.Render();
        }

        private void ReadGameMemory() {
            //read self
            int ptrPlayerSelf = Memory.Read<int>(Offsets.baseGame + Offsets.ptrPlayerEntity);
            self = new Player(ptrPlayerSelf);

            //read players
            players.Clear();
            int numPlayers = Memory.Read<int>(Offsets.baseGame + Offsets.numplayers);
            int ptrPlayerArray = Memory.Read<int>(Offsets.baseGame + Offsets.ptrPlayerArray);
            for (int i = 0; i < numPlayers - 1; i++) {
                //due to the game's implimentation we ignore the first 4 bytes from the start of the array 
                //each pointer is 4 bytes in the array. pointer + (i + 1) * 0x04
                int ptrPlayer = Memory.Read<int>(ptrPlayerArray + (i + 1) * 0x04);
                Player player = new Player(ptrPlayer);
                players.Add(player);
            }
        }

        public void ClearScreen(Graphics g) {
            //fill screen with chosen transparent color
            g.FillRectangle(new SolidBrush(colorTransparencyKey), new Rectangle(0, 0, this.Width, this.Height));
        }

        private void AssaultHack_FormClosing(object sender, FormClosingEventArgs e) {
            //kill threads
            isRunning = false;

            //wait for threads to finish
            windowPosThread.Join();
            overlayThread.Join();

            //detach from process
            Memory.CloseProcess();

            Environment.Exit(0);
        }
    }
}
