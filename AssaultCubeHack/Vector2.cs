using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssaultCubeHack {
    class Vector2 {

        public float x;
        public float y;

        public Vector2() {}

        public Vector2(float x, float y) {
            this.x = x;
            this.y = y;
        }

        public float Distance(Vector2 vector) {
            float dx = vector.x - x;
            float dy = vector.y - y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        public override string ToString() {
            return string.Format("{0}, {1}", Math.Round(x, 2), Math.Round(y, 2));
        }
    }
}
