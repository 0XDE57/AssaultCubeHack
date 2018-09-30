using AssaultCubeHack.Properties;
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

namespace AssaultCubeHack
{
  public partial class AssaultHack : Form
  {

    //target process
    private const string processName = "ac_client";
    private Process process;


    //threads for updating rendering
    private Thread overlayThread;
    private Thread windowPosThread;
    private volatile bool isRunning = false;


    //game objects
    private Player self;
    private List<Player> players = new List<Player>();
    private int numPlayers;
    private Matrix viewMatrix;
    private int gameWidth, gameHeight;


    //initialize rendering stuff
    private BufferedGraphics bufferedGraphics;
    private Font font = new Font(FontFamily.GenericMonospace, 14, FontStyle.Bold);
    //color used for transparency. anything drawn in same color will not show up.
    private Color colorTransparencyKey = Color.Black;
    //TODO: fix. play with flags and window attributes...
    //can't use SolidBrush with alpha due to window transperency key / flag destroying alpha?
    //(alpha of 127 and below = translucent, 128 and above = opaque)
    //so we use HatchBrush to kinda fake alpha by allowing the background(game) to come through the holes in the hatches
    private Brush hatchBrush = new HatchBrush(HatchStyle.Percent70, Color.FromArgb(128, 15, 15, 15));


    //keyboard commands
    private GlobalKeyboardHook gkh = new GlobalKeyboardHook();
    private bool aim = false;


    // runtime Settings
    private bool _showNames;
    private bool _showHealth;
    private bool _showNamesAsString;

    public AssaultHack()
    {
      _showNames = Settings.Default.ShowNick;
      _showHealth = Settings.Default.ShowHealth;
      _showNamesAsString = Settings.Default.ShowHealthAsString;
      InitializeComponent();

      //Get permission for working with UnmanagedCode
      //msdn.microsoft.com/en-us/library/xc5yzfbx(v=vs.110)
      //msdn.microsoft.com/en-us/library/ff648663.aspx#c08618429_020
      try
      {
        SecurityPermission sp = new SecurityPermission(SecurityPermissionFlag.UnmanagedCode);
        sp.Demand();
      }
      catch (Exception ex)
      {
        Console.WriteLine("Demand for SecurityPermissionFlag.UnmanagedCode failed: " + ex.Message);
      }
    }

    /// <summary>
    /// Allow window to be visually transparent / alpha blended.
    /// Allow mouse events to "fall-through" to the next (underlying) window.
    /// WS_EX_TRANSPARENT: Specifies that a window created with this style is to be transparent. 
    /// That is, any windows that are beneath the window are not obscured by the window. 
    /// A window created with this style receives WM_PAINT messages only after all 
    /// sibling windows beneath it have been updated.
    /// msdn.microsoft.com/en-us/library/windows/desktop/ff700543%28v=vs.85%29.aspx
    /// msdn.microsoft.com/en-us/library/aa251511(v=vs.60).aspx
    /// </summary>
    protected override CreateParams CreateParams
    {
      get
      {
        CreateParams CP = base.CreateParams;
        int WS_EX_TRANSPARENT = 0x00000020;
        CP.ExStyle = CP.ExStyle | WS_EX_TRANSPARENT;
        return CP;
      }
    }

    private void AssaultHack_Load(object sender, EventArgs e)
    {
      Visible = false;

      //try to attach to game
      AttachToGameProcess();
    }

    private void InitializeOverlayWindowAttributes()
    {
      //set up window and overlay properties for drawing on top of another window
      Visible = true;
      picBoxOverlay.Visible = true;
      TopMost = true; //set window on top of all others
      FormBorderStyle = FormBorderStyle.None; //remove form controls
      picBoxOverlay.Dock = DockStyle.Fill; //fill window with picturebox graphics
      picBoxOverlay.BackColor = colorTransparencyKey; //set overlay to transparent color
      TransparencyKey = colorTransparencyKey; //set tranparency key
      SetStyle(ControlStyles.SupportsTransparentBackColor, true);

      //Win32API.SetLayeredWindowAttributes(this.Handle, 0, 128, 2);


      //initialize graphics           
      BufferedGraphicsContext buffContext = new BufferedGraphicsContext();
      bufferedGraphics = buffContext.Allocate(picBoxOverlay.CreateGraphics(), ClientRectangle);
      bufferedGraphics.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
      bufferedGraphics.Graphics.CompositingQuality = CompositingQuality.HighQuality;

    }

    /// <summary>
    /// Try to attach to game.
    /// </summary>
    private void AttachToGameProcess()
    {
      Visible = false;
      int count = 0;
      bool success = false;
      do
      {
        //check if game is running
        if (Memory.GetProcessesByName(processName, out process))
        {
          Console.ForegroundColor = ConsoleColor.White;
          Console.Clear();
          Console.WriteLine("Process found: " + process.Id + ": " + process.ProcessName);
          Console.WriteLine("Attaching...");

          //try to attach to game process
          try
          {
            //success  
            IntPtr handle = Memory.OpenProcess(process.Id);
            if (handle != IntPtr.Zero)
            {
              success = true;
              Console.ForegroundColor = ConsoleColor.Green;
              Console.WriteLine("Attached Handle: " + handle);
            }
            else
            {
              Console.ForegroundColor = ConsoleColor.Red;
              Console.WriteLine("Could not attach");
            }
          }
          catch (Exception ex)
          {
            //fail
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Attach failed: " + ex.Message);
            Console.ReadKey(true);
          }
        }
        else
        {
          //process not found
          Console.ForegroundColor = ConsoleColor.Yellow;
          if (count++ == 0)
          {
            Console.Clear();
            Console.Write($"Waiting for {processName}");
          }
          else if (count < 10)
          {
            Console.Write(".");
          }
          else
          {
            count = 0;
          }
          Thread.Sleep(1000);
        }
      } while (!success);

      InitializeOverlayWindowAttributes();
      StartThreads();
    }

    private void StartThreads()
    {
      //start thread flag
      isRunning = true;

      //start thread for playing with memory and drawing overlay
      overlayThread = new Thread(UpdateHack);
      overlayThread.IsBackground = false;
      overlayThread.Start();

      //start thread for positioning and sizing overlay on top of target process
      windowPosThread = new Thread(UpdateWindow);
      windowPosThread.IsBackground = false;
      windowPosThread.Start(Handle);


      //set up low level keyboard hooking to recieve key events while not in focus
      if (Settings.Default.ActivateHotkeys)
      {
        gkh.HookedKeys.Add(Settings.Default.HotKeyHealthAsString);
        gkh.HookedKeys.Add(Settings.Default.HotKeyShowHealth);
        gkh.HookedKeys.Add(Settings.Default.HotKeyShowNick);
      }
      gkh.HookedKeys.Add(Settings.Default.AimKey);
      gkh.KeyDown += new KeyEventHandler(KeyDownEvent);
      gkh.KeyUp += new KeyEventHandler(KeyUpEvent);
    }

    /// <summary>
    /// Thread to make sure overlay is positioned over target process.
    /// Checks to make sure process is still running.
    /// </summary>
    /// <param name="handle">Handle of overlay form</param>
    private void UpdateWindow(object handle)
    {
      //update flag, make sure game is still running
      while (isRunning)
      {
        if (!Memory.IsProcessRunning(process))
        {
          isRunning = false;
          continue;
        }

        //ensure we are in focus and on top of game
        SetOverlayPosition((IntPtr)handle);

        //sleep for a bit, we don't need to move around constantly
        Thread.Sleep(200);
      }

      Console.ForegroundColor = ConsoleColor.White;
      Console.WriteLine("\nProcess " + processName + " ended.");
      BeginInvoke(new Action(() => AttachToGameProcess()));
    }

    /// <summary>
    /// Set window position and size to overlay target process.
    /// </summary>
    /// <param name="overlayHandle">Handle of overlay form(this form/self)</param>
    private void SetOverlayPosition(IntPtr overlayHandle)
    {

      //get window handle
      IntPtr gameProcessHandle = process.MainWindowHandle;
      if (gameProcessHandle == IntPtr.Zero)
        return;

      //get position and size of window
      NativeMethods.RECT targetWindowPosition, targetWindowSize;
      if (!NativeMethods.GetWindowRect(gameProcessHandle, out targetWindowPosition))
        return;
      if (!NativeMethods.GetClientRect(gameProcessHandle, out targetWindowSize))
        return;

      //calculate width and height of full target window
      int width = targetWindowPosition.Right - targetWindowPosition.Left;
      int height = targetWindowPosition.Bottom - targetWindowPosition.Top;

      //check if window has borders
      int dwStyle = NativeMethods.GetWindowLong(gameProcessHandle, NativeMethods.GWL_STYLE);
      if ((dwStyle & NativeMethods.WS_BORDER) != 0)
      {
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
      NativeMethods.MoveWindow(overlayHandle, targetWindowPosition.Left, targetWindowPosition.Top, width, height, true);

      //use hWndInsertAfter force AssualtCube behind overlay
      NativeMethods.SetWindowPos(gameProcessHandle, overlayHandle, 0, 0, 0, 0, NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE);


      //save window size for ESP WorldToScreen translation
      gameWidth = width;
      gameHeight = height;
    }

    /// <summary>
    /// Key event when a hooked key is pressed.
    /// </summary>
    private void KeyDownEvent(object sender, KeyEventArgs e)
    {
      aim = (e.KeyCode == Settings.Default.AimKey);

      e.Handled = true;//prevent other programs from processing key
    }

    /// <summary>
    /// Key event when a hooked key is released.
    /// </summary>
    private void KeyUpEvent(object sender, KeyEventArgs e)
    {
      if (e.KeyCode == Settings.Default.AimKey)
      {
        aim = false;
      }
      else if(e.KeyCode == Settings.Default.HotKeyShowNick)
      {
        _showNames = !_showNames;
      }
      else if (e.KeyCode == Settings.Default.HotKeyShowHealth)
      {
        _showHealth = !_showHealth;
      }
      else if(e.KeyCode == Settings.Default.HotKeyHealthAsString)
      {
        _showNamesAsString = !_showNamesAsString;
      }

      e.Handled = true;//prevent other programs from processing key
    }


    /// <summary>
    /// Main update thread. 
    /// Read and write game memory, draw on screen.
    /// </summary>
    public void UpdateHack()
    {

      //update loop
      while (isRunning)
      {

        //read
        ReadGameMemory();


        //Test Hacks
        //self hacks, infinite health and ammo
        /*
        self.Health = 99999;
        self.weapon.Ammo = 7331;
        self.weapon.AmmoClip = 999;
        self.weapon.DelayTime = 0;//rapid fire
        */

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

      //cleanup
      Memory.CloseProcess();
      bufferedGraphics.Dispose();
      bufferedGraphics = null;

    }

    /// <summary>
    /// Aim at closest enemy.
    /// </summary>
    private void UpdateAimbot()
    {
      //if not aiming or no players, escape
      if (!aim || players.Count == 0) return;

      //find closest enemy player
      //Player target = GetClosestEnemy();
      Player target = GetClosestEnemyToCrossHair();
      if (target == null) return;

      //calculate horizontal angle between enemy and player (yaw)
      float dx = target.PositionFoot.x - self.PositionFoot.x;
      float dy = target.PositionFoot.y - self.PositionFoot.y;
      double angleYaw = Math.Atan2(dy, dx) * 180 / Math.PI;

      //calculate verticle angle between enemy and player (pitch)
      double distance = Math.Sqrt(dx * dx + dy * dy);
      float dz = target.PositionFoot.z - self.PositionFoot.z;
      double anglePitch = Math.Atan2(dz, distance) * 180 / Math.PI;

      //set self angles to calculated angles
      self.Yaw = (float)angleYaw + 90;
      self.Pitch = (float)anglePitch;

    }

    /// <summary>
    /// Get enemy nearest to player.
    /// </summary>
    private Player GetClosestEnemy()
    {
      //find first living enemy player
      Player target = players.Find(p => p.Team != self.Team && p.Health > 0);
      if (target == null) return null;

      //if a closer enemy is found, set them as target
      foreach (Player player in players)
      {
        if (player.Team != self.Team && player.Health > 0
            && player.PositionFoot.Distance(self.PositionFoot) < target.PositionFoot.Distance(self.PositionFoot))
          target = player;
      }

      return target;
    }

    /// <summary>
    /// Get enemy nearest to crosshair.
    /// </summary>
    private Player GetClosestEnemyToCrossHair()
    {
      //find first living enemy player in view
      Vector2 targetPos = new Vector2();
      Player target = players.Find(p => p.Team != self.Team && p.Health > 0
          && viewMatrix.WorldToScreen(p.PositionHead, gameWidth, gameHeight, out targetPos));
      if (target == null) return null;

      //calculate distance to crosshair
      Vector2 crossHair = new Vector2(gameWidth / 2, gameHeight / 2);
      float dist = crossHair.Distance(targetPos);

      //find player closest to crosshair
      foreach (Player p in players)
      {
        if (p.Team != self.Team && p.Health > 0)
        {
          Vector2 headPos;
          if (viewMatrix.WorldToScreen(p.PositionHead, gameWidth, gameHeight, out headPos))
          {
            float newDist = crossHair.Distance(headPos);
            if (newDist < dist)
            {
              target = p;
              dist = newDist;
            }
          }
        }
      }

      return target;
    }

    /// <summary>
    /// Read the memory of the game and save data into objects.
    /// </summary>
    private void ReadGameMemory()
    {
      if (!isRunning) return;

      //read self
      int ptrPlayerSelf = Memory.Read<int>(Offsets.baseGame + Offsets.ptrPlayerEntity);
      self = new Player(ptrPlayerSelf);

      //read players
      players.Clear();
      numPlayers = Memory.Read<int>(Offsets.baseGame + Offsets.numPlayers);
      int ptrPlayerArray = Memory.Read<int>(Offsets.baseGame + Offsets.ptrPlayerArray);
      for (int i = 0; i < numPlayers; i++)
      {
        //each pointer is 4 bytes apart in the array
        //pointer to player = pointer to array + index of player * byte size
        int ptrPlayer = Memory.Read<int>(ptrPlayerArray + (i * 0x04));
        players.Add(new Player(ptrPlayer));
      }

      //read view matrix
      viewMatrix = Memory.ReadMatrix(Offsets.viewMatrix);
    }


    private void Draw(Graphics g)
    {
      //clear
      ClearScreen(g);

      //show player count
      g.DrawString("Players: " + numPlayers, font, new SolidBrush(Color.Wheat), ClientSize.Width / 2, 10);

      //debug show view matrix
      //g.DrawString(viewMatrix.ToString(), font, Brushes.White, new Point(300, 30));
      //g.DrawString(gameWidth + "," + gameHeight, font, Brushes.White, new Point());

      //g.FillRectangle(new SolidBrush(Color.FromArgb(128, 255, 0, 0)), 30, 30, 300, 300);

      //draw esp(wall hack)
      foreach (Player p in players)
      {
        if (p.Health <= 0) continue;

        int offset = 20;
        Pen color = p.Team == self.Team ? Pens.Green : Pens.Red;
        Vector2 headPos, footPos;
        if (viewMatrix.WorldToScreen(p.PositionHead, gameWidth, gameHeight, out headPos) &&
            viewMatrix.WorldToScreen(p.PositionFoot, gameWidth, gameHeight, out footPos))
        {
          float height = Math.Abs(headPos.y - footPos.y);
          float width = height / 2;
          g.DrawRectangle(color, headPos.x - width / 2, headPos.y - offset, width, height + offset);
          if (_showNames)
          {
            g.DrawString(p.Name, font, new SolidBrush(Color.Wheat), headPos.x - width / 2, headPos.y - offset);
            if (_showHealth)
            {
              g.DrawString(_showNamesAsString?p.Health.ToString():healthBars(p.Health), font, (p.Health == 100 ? new SolidBrush(Color.Green) : (p.Health >= 50 ? new SolidBrush(Color.Yellow) : new SolidBrush(Color.Red))), headPos.x - width / 2, (headPos.y - offset) + font.Height);
            }
          }
          else if (_showHealth)
          {
            g.DrawString(_showNamesAsString ? p.Health.ToString() : healthBars(p.Health), font, (p.Health == 100 ? new SolidBrush(Color.Green) : (p.Health >= 50 ? new SolidBrush(Color.Yellow) : new SolidBrush(Color.Red))), headPos.x - width / 2, headPos.y - offset);
          }
        }

      }

      bool drawPlayerList = false;
      if (drawPlayerList)
      {
        //test draw player list
        if (players.Count > 0)
        {
          int spacing = (int)font.Size + 1;
          int s = 0;
          //add background to make text more visible
          g.FillRectangle(hatchBrush, 20, 20, 180, (spacing * players.Count) + (spacing / 2));
          foreach (Player p in players)
          {

            Point pos = new Point(20, 20 + (s * spacing));
            Brush color = p.Team == self.Team ? Brushes.Green : Brushes.Red;
            DrawStringOutlined(g, p.Name, pos, font, color, Pens.DarkBlue);
            //g.DrawString(p.Name, font, color, pos.X, pos.Y);
            //g.DrawOutline();
            //DrawOutline(g)
            s++;
          }
        }
      }

      //render
      bufferedGraphics.Render();
    }

    private string healthBars(int health)
    {
      int result = (health / 10);
      string bars = result>0?"":"|";
      for (int i = 0; i < result; i++)
      {
        bars += "|";
      }
      return bars;
    }

    private static void DrawStringOutlined(Graphics g, string text, Point pos, Font font, Brush colorText, Pen colorOutline)
    {
      GraphicsPath path = new GraphicsPath();
      path.AddString(text,
          font.FontFamily, (int)font.Style,
          g.DpiY * font.Size / 72, // convert to em size
          pos, new StringFormat());
      g.DrawPath(colorOutline, path);
      g.FillPath(colorText, path);
    }

    private void ClearScreen(Graphics g)
    {
      //fill screen with chosen transparent color (tranparency key)
      g.FillRectangle(new SolidBrush(colorTransparencyKey), ClientRectangle);
    }

    private void AssaultHack_FormClosing(object sender, FormClosingEventArgs e)
    {
      //kill threads
      isRunning = false;

      //wait for threads to finish
      windowPosThread.Join(2000);
      overlayThread.Join(2000);

      //detach from process
      Memory.CloseProcess();

      Environment.Exit(0);
    }
  }
}
