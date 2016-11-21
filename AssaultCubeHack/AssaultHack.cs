using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
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
        private const string processName = "ac_client";
        private Process process;

        //initialize rendering stuff
        private BufferedGraphics bufferedGraphics;
        private BufferedGraphicsContext buffContext = new BufferedGraphicsContext();
        private Font font = new Font(FontFamily.GenericMonospace, 14, FontStyle.Bold);
        //color used for transparency. anything drawn in same color will not show up.
        private Color colorTransparencyKey = Color.Black;
        //can't use SolidBrush with alpha due to window transperency flag destroying alpha
        //(alpha of 127 and below = translucent, 128 and above = opaque)
        //so we use HatchBrush to kinda fake alpha by allowing the background(game) to come through the holes in the hatches
        private Brush hatchBrush = new HatchBrush(HatchStyle.Percent50, Color.DarkBlue);

        //threads for updating rendering
        private Thread overlayThread;
        private Thread windowPosThread;
        private volatile bool isRunning = false;

        //game objects
        private Player self;
        private List<Player> players = new List<Player>();
        private int numPlayers;

        //keyboard commands
        private GlobalKeyboardHook gkh = new GlobalKeyboardHook();
        private const Keys keyAim = Keys.CapsLock;
        private bool aim = false;

        public AssaultHack() {
            //Get permission for working with UnmanagedCode
            //https://msdn.microsoft.com/en-us/library/xc5yzfbx(v=vs.110)
            //https://msdn.microsoft.com/en-us/library/ff648663.aspx#c08618429_020
            try {
                SecurityPermission sp = new SecurityPermission(SecurityPermissionFlag.UnmanagedCode);
                sp.Demand();
            } catch (Exception ex) {
                Console.WriteLine("Demand for SecurityPermissionFlag.UnmanagedCode failed: " + ex.Message);
            }

            //try to attach to game
            AttachToGameProcess();

            InitializeComponent();
        }

        /// <summary>
        /// Allow window to be visually transparent / alpha blended.
        /// Allow mouse events to "fall-through" to the next (underlying) window.
        /// WS_EX_TRANSPARENT: Specifies that a window created with this style is to be transparent. 
        /// That is, any windows that are beneath the window are not obscured by the window. 
        /// A window created with this style receives WM_PAINT messages only after all 
        /// sibling windows beneath it have been updated.
        /// https://msdn.microsoft.com/en-us/library/windows/desktop/ff700543%28v=vs.85%29.aspx
        /// https://msdn.microsoft.com/en-us/library/aa251511(v=vs.60).aspx
        /// </summary>
        protected override CreateParams CreateParams {
            get {               
                CreateParams CP = base.CreateParams;
                int WS_EX_TRANSPARENT = 0x00000020;
                CP.ExStyle = CP.ExStyle | WS_EX_TRANSPARENT;
                return CP;
            }
        }


        private void AssaultHack_Load(object sender, EventArgs e) {          

            //set up window and overlay properties for drawing on top of another process
            this.WindowState = FormWindowState.Maximized; //maximize window
            this.TopMost = true; //set window on top of all others
            this.FormBorderStyle = FormBorderStyle.None; //remove form controls
            picBoxOverlay.Dock = DockStyle.Fill; //fill window with picturebox graphics
            picBoxOverlay.BackColor = colorTransparencyKey; //set overlay to transparent color
            this.TransparencyKey = colorTransparencyKey; //set tranparency key
        
            //initialize graphics
            bufferedGraphics = buffContext.Allocate(picBoxOverlay.CreateGraphics(), ClientRectangle);
            bufferedGraphics.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

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

        private void AttachToGameProcess() {
            bool success = false;
            do {
                //check if game is running
                if (Memory.GetProcessesByName(processName, out process)) {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine("Process found: " + process.Id + ": " + process.ProcessName);               
                    Console.WriteLine("Attaching...");

                    //attach to game process
                    try {
                        Console.ForegroundColor = ConsoleColor.Green;
                        IntPtr handle = Memory.OpenProcess(process.Id);
                        Console.WriteLine("Attached Handle: " + handle);
                        success = true;
                    } catch (Exception ex) {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Attach failed: " + ex.Message);
                        Console.ReadKey(true);
                    }
                } else {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Process not found. Start game then press any key to continue...");
                    Console.ReadKey(true);
                }
            } while (!success);
        }

        /// <summary>
        /// Thread to make sure overlay is positioned over target process.
        /// Checks to make sure process is still running.
        /// </summary>
        /// <param name="handle">Handle of overlay form</param>
        private void UpdateWindow(object handle) {
            while (isRunning) {
                //update flag, make sure game is still running
                isRunning = Memory.IsProcessRunning(process);
                
                //ensure we are on in focus and on top of game
                picBoxOverlay.BringToFront();     
                SetOverlayPosition((IntPtr)handle);

                //sleep for a bit, we don't need to move around constantly
                Thread.Sleep(100);
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
            //find first living enemy player
            Player target = players.Find(p => p.Team != self.Team && p.Health > 0);
            if (target == null) return null;
            //if a closer enemy is found, set them as target
            foreach (Player player in players) {
                if (player.Team != self.Team && player.Health > 0 && player.Position.Distance(self.Position) < target.Position.Distance(self.Position))
                    target = player;
            }

            return target;
        }

        private void ReadGameMemory() {
            //read self
            int ptrPlayerSelf = Memory.Read<int>(Offsets.baseGame + Offsets.ptrPlayerEntity);
            self = new Player(ptrPlayerSelf);
            
            //read players
            players.Clear();
            numPlayers = Memory.Read<int>(Offsets.baseGame + Offsets.numPlayers);
            int ptrPlayerArray = Memory.Read<int>(Offsets.baseGame + Offsets.ptrPlayerArray);
            for (int i = 0; i < numPlayers - 1; i++) {
                //due to the game's implimentation we ignore the first 4 bytes from the start of the array
                //each pointer is 4 bytes apart in the array
                //pointer to player = pointer to array + index of player * byte size
                int ptrPlayer = Memory.Read<int>(ptrPlayerArray + (i + 1) * 0x04);
                players.Add(new Player(ptrPlayer));
            }
        }

        private void Draw(Graphics g) {
            //clear
            ClearScreen(g);

 
            //show player count
            g.DrawString("Players: " + numPlayers, font, new SolidBrush(Color.Wheat), ClientSize.Width / 2, 20);

            //test draw player list
            if (players.Count > 0) {
                int spacing = (int)font.Size + 1;
                int s = 0;
                //add background to make text more visible
                g.FillRectangle(hatchBrush, 20, 20, 180, (spacing * players.Count) + (spacing / 2));
                foreach (Player p in players) {
                    Point pos = new Point(20, 20 + (s * spacing));
                    Brush color = p.Team == self.Team ? Brushes.Green : Brushes.Red;
                    DrawStringOutlined(g, p.Name, pos, font, color, Pens.DarkBlue);
                    //g.DrawString(p.Name, font, color, pos.X, pos.Y);
                    s++;
                }
            }

            //render
            bufferedGraphics.Render();
        }

        private static void DrawStringOutlined(Graphics g, string text, Point pos, Font font, Brush colorText, Pen colorOutline) {
            GraphicsPath path = new GraphicsPath();
            path.AddString(text,
                font.FontFamily, (int)font.Style, 
                g.DpiY * font.Size / 72, // convert to em size
                pos, new StringFormat());
            g.DrawPath(colorOutline, path);
            g.FillPath(colorText, path);
        }

        public void ClearScreen(Graphics g) {
            //fill screen with chosen transparent color (tranparency key)
            g.FillRectangle(new SolidBrush(colorTransparencyKey), ClientRectangle);
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
