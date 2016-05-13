using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssaultCubeHack {
    class Player {
        public string name;

        public Vector3 position,velocity;
        public float yaw,pitch,roll;

        public int health, healthMax;

        public int ammo, ammoClip;

        public Player(int pointerPlayer) {
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
