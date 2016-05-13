using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryTest {
    class Offsets {
        //memory addresses
        public const int baseGame = 0x0050F4E8;

        public const int playerEntity = 0x0C;
        public const int playerArray = 0x10;

        public const int name = 0x224;
        public const int position = 0x34;
        public const int velocity = 0x10;
        public const int yaw = 0x40;
        public const int pitch = 0x44;
        public const int health = 0xF8;
        public const int numplayers = 0x18;

        public const int a1 = 0x378;
        public const int a2 = 0x10;
        public const int ammo = 0x28;
        public const int ammoCap = 0x0;
    }
}
