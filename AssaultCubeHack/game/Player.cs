using AssaultCubeHack.game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssaultCubeHack {
    class Player {

        private Memory memory;
        private int pointerPlayer;
        public Weapon weapon;

        public Player(Memory gameMemory, int pointerPlayer) {
            memory = gameMemory;
            this.pointerPlayer = pointerPlayer;
            weapon = new Weapon(gameMemory, pointerPlayer);
        }

        public string Name { get { return memory.ReadString2(pointerPlayer + Offsets.name, 17).Remove(0, 1); } }

        public int Team { get { return memory.Read<int>(pointerPlayer + Offsets.team); } }

        public int Health {
            get { return memory.Read<int>(pointerPlayer + Offsets.health); }
            set { memory.Write<int>(pointerPlayer + Offsets.health, value); }
        }

        public Vector3 PositionHead {
            get { return memory.ReadVector3(pointerPlayer + Offsets.headPos); }
            set { memory.WriteVector3(pointerPlayer + Offsets.headPos, value); }
        }

        public Vector3 PositionFoot {
            get { return memory.ReadVector3(pointerPlayer + Offsets.footPos); }
            set { memory.WriteVector3(pointerPlayer + Offsets.footPos, value); }
        }

        public Vector3 Velocity {
            get { return memory.ReadVector3(pointerPlayer + Offsets.velocity); }
            set { memory.WriteVector3(pointerPlayer + Offsets.velocity, value); }
        }

        public float Yaw {
            get { return memory.Read<float>(pointerPlayer + Offsets.yaw); }
            set { memory.Write(pointerPlayer + Offsets.yaw, value); }
        }

        public float Pitch {
            get { return memory.Read<float>(pointerPlayer + Offsets.pitch); }
            set { memory.Write(pointerPlayer + Offsets.pitch, value); }
        }

    }

    /// <summary>
    /// Weapon object that player is holding.
    /// </summary>
    class Weapon {
        private Memory memory;
        private int pointerWeapon;

        public Weapon(Memory gameMemory, int pointerPlayer) {
            memory = gameMemory;
            int pointerCurrentWeapon = memory.Read<int>(pointerPlayer + Offsets.ptrCurrentWeapon);
            pointerWeapon = memory.Read<int>(pointerCurrentWeapon + Offsets.ptrWeapon);
        }

        public int Ammo {
            get { return memory.Read<int>(pointerWeapon + Offsets.ammo); }
            set { memory.Write<int>(pointerWeapon + Offsets.ammo, value); }
        }

        public int AmmoClip {
            get { return memory.Read<int>(pointerWeapon + Offsets.ammoClip); }
            set { memory.Write<int>(pointerWeapon + Offsets.ammoClip, value); }
        }

        public int DelayTime {
            get { return memory.Read<int>(pointerWeapon + Offsets.delayTime); }
            set { memory.Write<int>(pointerWeapon + Offsets.delayTime, value); }
        }
        
    }
}
