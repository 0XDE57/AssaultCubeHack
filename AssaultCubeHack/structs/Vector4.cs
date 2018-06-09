using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssaultCubeHack {
    public class Vector4 {
        public float x;
        public float y;
        public float z;
        public float w;

        public Vector4() { }

        public Vector4(float x, float y, float z, float w) {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public override string ToString() {
            return string.Format("{0}, {1}, {2}, {3}", x, y, z, w);
        }
    }
}
