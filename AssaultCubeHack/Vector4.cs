using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssaultCubeHack {
    public class Vector4 {
        public float X;
        public float Y;
        public float Z;
        public float W;

        public Vector4() { }

        public Vector4(float x, float y, float z, float w) {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public override string ToString() {
            return string.Format("{0} {1} {2} {3}", X, Y, Z, W);
        }
    }
}
