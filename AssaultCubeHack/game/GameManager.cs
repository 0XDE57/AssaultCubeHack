using AssaultCubeHack.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssaultCubeHack.game {
    class GameManager {
        
        public Process GameProcess { get; private set; }
        public Memory GameMemory { get; private set; }
        public bool IsAttached { get; private set; }

        public Player Self { get; private set; }
        public List<Player> Players = new List<Player>();
        public int NumPlayers { get; private set; }
        public Matrix ViewMatrix { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        

        public GameManager(Process process) {
            GameProcess = process;
            GameMemory = new Memory(process.Id);
            
            if (GameMemory.GetHandle() == IntPtr.Zero) {
                IsAttached = false;
            } else {
                IsAttached = true;
            }
        }


        /// <summary>
        /// Read the memory of the game and save data into objects.
        /// </summary>
        public void ReadGameMemory() {
            //read self
            int ptrPlayerSelf = GameMemory.Read<int>(Offsets.baseGame + Offsets.ptrPlayerEntity);
            Self = new Player(GameMemory, ptrPlayerSelf);//todo: can we simply updtate the pointer, why create a new object?

            //read players
            Players.Clear();
            NumPlayers = GameMemory.Read<int>(Offsets.baseGame + Offsets.numPlayers);
            int ptrPlayerArray = GameMemory.Read<int>(Offsets.baseGame + Offsets.ptrPlayerArray);
            for (int i = 0; i < NumPlayers; i++) {
                //each pointer is 4 bytes apart in the array
                //pointer to player = pointer to array + index of player * byte size
                int ptrPlayer = GameMemory.Read<int>(ptrPlayerArray + (i * 0x04));
                Players.Add(new Player(GameMemory, ptrPlayer)); //todo: can we simply updtate the pointer, why create a new object?
            }

            //read view matrix
            ViewMatrix = GameMemory.ReadMatrix(Offsets.viewMatrix);
        }


        /// <summary>
        /// Aim at closest enemy.
        /// </summary>
        public void UpdateAimbot() {
            if (Players.Count == 0) return;

            //find closest enemy player
            //Player target = GetClosestEnemy();
            Player target = GetClosestEnemyToCrossHair();
            if (target == null) return;

            //calculate horizontal angle between enemy and player (yaw)
            float dx = target.PositionFoot.x - Self.PositionFoot.x;
            float dy = target.PositionFoot.y - Self.PositionFoot.y;
            double angleYaw = Math.Atan2(dy, dx) * 180 / Math.PI;

            //calculate verticle angle between enemy and player (pitch)
            double distance = Math.Sqrt(dx * dx + dy * dy);
            float dz = target.PositionFoot.z - Self.PositionFoot.z;
            double anglePitch = Math.Atan2(dz, distance) * 180 / Math.PI;

            //set self angles to calculated angles
            Self.Yaw = (float)angleYaw + 90;
            Self.Pitch = (float)anglePitch;

        }

        /// <summary>
        /// Get enemy nearest to player.
        /// </summary>
        public Player GetClosestEnemy() {
            //find first living enemy player
            Player target = Players.Find(p => p.Team != Self.Team && p.Health > 0);
            if (target == null) return null;

            //if a closer enemy is found, set them as target
            foreach (Player player in Players) {
                if (player.Team != Self.Team && player.Health > 0
                    && player.PositionFoot.Distance(Self.PositionFoot) < target.PositionFoot.Distance(Self.PositionFoot))
                    target = player;
            }

            return target;
        }

        /// <summary>
        /// Get enemy nearest to crosshair.
        /// </summary>
        public Player GetClosestEnemyToCrossHair() {
            //find first living enemy player in view
            Vector2 targetPos = new Vector2();
            Player target = Players.Find(p => p.Team != Self.Team && p.Health > 0
                && ViewMatrix.WorldToScreen(p.PositionHead, Width, Height, out targetPos));
            if (target == null) return null;

            //calculate distance to crosshair
            Vector2 crossHair = new Vector2(Width / 2, Height / 2);
            float dist = crossHair.Distance(targetPos);

            //find player closest to crosshair
            foreach (Player p in Players) {
                if (p.Team != Self.Team && p.Health > 0) {
                    if (ViewMatrix.WorldToScreen(p.PositionHead, Width, Height, out Vector2 headPos)) {
                        float newDist = crossHair.Distance(headPos);
                        if (newDist < dist) {
                            target = p;
                            dist = newDist;
                        }
                    }
                }
            }

            return target;
        }

        public void UpdateSize(int width, int height) {
            Width = width;
            Height = height;
        }

        public void Close() {
            GameMemory.CloseProcess();
        }

        internal void TestHacks() {
            //Test Hacks
            //self hacks, infinite health and ammo
            
            Self.Health = 99999;
            Self.weapon.Ammo = 7331;
            Self.weapon.AmmoClip = 999;
            Self.weapon.DelayTime = 0;//rapid fire
            
            /*
            Players.ForEach(p => p.Velocity = new Vector3(0, 0, 15));//send everyone to the ceiling
            Players.ForEach(p => p.Pitch = 90); //make everyone look up
            Players.ForEach(p => p.PositionFoot = new Vector3(130, 130, 10)); //set everyone to same spot
            Players.ForEach(p => p.Health = 0);//1 hit kills on anyone
            */
        }
    }

}
