
using System;

namespace AssaultCubeHack {
    public class Vector3 {
        public float X;
        public float Y;
        public float Z;

        public Vector3() { }

        public Vector3(float _X, float _Y, float _Z) { 
            X = _X; 
            Y = _Y; 
            Z = _Z; 
        }

        public override string ToString() {
            return string.Format("{0} {1} {2}", Math.Round(X, 2), Math.Round(Y, 2), Math.Round(Z, 2));
        }
    }
}
