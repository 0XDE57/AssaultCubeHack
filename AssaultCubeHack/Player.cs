using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssaultCubeHack {
    class Player {
        private int pointerPlayer;

        public string name;
        private Vector3 position,velocity;
        private float yaw, pitch, roll;
        private int health, healthMax;
        private int ammo, ammoClip;

        public int Health {
            get { return health; }
            set { Memory.Write<int>(pointerPlayer + Offsets.health, value); }
        }

        public Vector3 Position {
            get { return position; }
            set { Memory.WriteVector3(pointerPlayer + Offsets.position, value); }
        }

        public Vector3 Velocity {
            get { return velocity; }
            set { Memory.WriteVector3(pointerPlayer + Offsets.velocity, value); }
        }

        public float Yaw {
            get { return yaw; }
            set { Memory.Write<float>(pointerPlayer + Offsets.yaw, value); }
        }

        public float Pitch {
            get { return pitch; }
            set { Memory.Write<float>(pointerPlayer + Offsets.pitch, value); }
        }

        public int Ammo {
            get { return ammo; }
            set {
                Int32 pa1 = Memory.Read<Int32>(pointerPlayer + Offsets.a1);
                Int32 pa2 = Memory.Read<Int32>(pa1 + Offsets.a2);
                Memory.Write<int>(pa2 + Offsets.ammo, value);
            }
        }

        public int AmmoClip {
            get { return ammoClip; }
            set {
                Int32 pa1 = Memory.Read<Int32>(pointerPlayer + Offsets.a1);
                Int32 pa2 = Memory.Read<Int32>(pa1 + Offsets.a2);
                Memory.Write<int>(pa2 + Offsets.ammoClip, value);
            }
        }



        public Player(int pointerPlayer) {
            this.pointerPlayer = pointerPlayer;

            name = Memory.ReadString2(pointerPlayer + Offsets.name, 17).Remove(0, 1);
            health = Memory.Read<int>(pointerPlayer + Offsets.health);
            position = Memory.ReadVector3(pointerPlayer + Offsets.position);
            velocity = Memory.ReadVector3(pointerPlayer + Offsets.velocity);
            yaw = Memory.Read<float>(pointerPlayer + Offsets.yaw);
            pitch = Memory.Read<float>(pointerPlayer + Offsets.pitch);

            Int32 pa1 = Memory.Read<Int32>(pointerPlayer + Offsets.a1);
            Int32 pa2 = Memory.Read<Int32>(pa1 + Offsets.a2);
            ammo = Memory.Read<int>(pa2 + Offsets.ammo);
            ammoClip = Memory.Read<int>(pa2 + Offsets.ammoClip);
        }
    }
}
