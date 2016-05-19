
namespace AssaultCubeHack {
    class Offsets {
        /** baseGame is specific address in memory that all the pointers are based off
         * A pointer is read like:  baseGame + ptrMyObject
         * A variable is read like: ptrMyObject + myVariable
         * Pointers to locations are prefaced with ptr
         */

        //base memory address to read
        public const int baseGame = 0x0050F4E8;


        //pointer to players
        //ptrPlayerEnitity -> variableOffset
        public const int ptrPlayerEntity = 0x0C;
        public const int ptrPlayerArray = 0x10;
        public const int numplayers = 0x18; //ptrPlayerArray size
        //player variables
        public const int name = 0x224;
        public const int team = 0x32C;
        public const int position = 0x34;
        public const int velocity = 0x10;
        public const int yaw = 0x40;
        public const int pitch = 0x44;
        public const int health = 0xF8;


        //weapon pointers
        //ptrPlayerEntity -> ptrCurrentWeapon -> ptrWeapon -> variableOffset
        public const int ptrCurrentWeapon = 0x378;
        public const int ptrWeapon = 0x10;
        //weapon variables
        public const int ammo = 0x28;
        public const int ammoClip = 0x0;
        public const int delayTime = 0x50;
        
    }
}
