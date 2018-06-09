using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssaultCubeHack {
    class Player {
        private int pointerPlayer;

        public string Name { get { return Memory.ReadString2(pointerPlayer + Offsets.name, 17).Remove(0, 1); } }

        public int Team { get { return Memory.Read<int>(pointerPlayer + Offsets.team); } }

        public int Health {
            get { return Memory.Read<int>(pointerPlayer + Offsets.health); }
            set { Memory.Write<int>(pointerPlayer + Offsets.health, value); }
        }

        public Vector3 PositionHead {
            get { return Memory.ReadVector3(pointerPlayer + Offsets.headPos); }
            set { Memory.WriteVector3(pointerPlayer + Offsets.headPos, value); }
        }

        public Vector3 PositionFoot {
            get { return Memory.ReadVector3(pointerPlayer + Offsets.footPos); }
            set { Memory.WriteVector3(pointerPlayer + Offsets.footPos, value); }
        }

        public Vector3 Velocity {
            get { return Memory.ReadVector3(pointerPlayer + Offsets.velocity); }
            set { Memory.WriteVector3(pointerPlayer + Offsets.velocity, value); }
        }

        public float Yaw {
            get { return Memory.Read<float>(pointerPlayer + Offsets.yaw); }
            set { Memory.Write(pointerPlayer + Offsets.yaw, value); }
        }

        public float Pitch {
            get { return Memory.Read<float>(pointerPlayer + Offsets.pitch); }
            set { Memory.Write(pointerPlayer + Offsets.pitch, value); }
        }

        public Weapon weapon;

        public Player(int pointerPlayer) {
            this.pointerPlayer = pointerPlayer;
            weapon = new Weapon(pointerPlayer);
        }
    }

    /// <summary>
    /// Weapon object that player is holding.
    /// </summary>
    class Weapon {

        private int pointerWeapon;

        public int Ammo {
            get { return Memory.Read<int>(pointerWeapon + Offsets.ammo); }
            set { Memory.Write<int>(pointerWeapon + Offsets.ammo, value); }
        }

        public int AmmoClip {
            get { return Memory.Read<int>(pointerWeapon + Offsets.ammoClip); }
            set { Memory.Write<int>(pointerWeapon + Offsets.ammoClip, value); }
        }

        public int DelayTime {
            get { return Memory.Read<int>(pointerWeapon + Offsets.delayTime); }
            set { Memory.Write<int>(pointerWeapon + Offsets.delayTime, value); }
        }

        public Weapon(int pointerPlayer) {
            int pointerCurrentWeapon = Memory.Read<Int32>(pointerPlayer + Offsets.ptrCurrentWeapon);
            pointerWeapon = Memory.Read<Int32>(pointerCurrentWeapon + Offsets.ptrWeapon);
        }
    }
}
