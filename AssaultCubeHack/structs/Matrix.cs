using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssaultCubeHack {
    class Matrix {
        //Memory layout of data will affect order of matrix. 
        //DirectX: Usualy Row-Major
        //OpenGL: Usualy Column-Major

        //             X,   Y,   Z,   W
        public float m11, m12, m13, m14; //00, 01, 02, 03
        public float m21, m22, m23, m24; //04, 05, 06, 07
        public float m31, m32, m33, m34; //08, 09, 10, 11
        public float m41, m42, m43, m44; //12, 13, 14, 15


        /// <summary>
        /// Project a 3D position in world to a 2D position on the screen.
        /// </summary>
        /// <param name="worldPos">object's 3D position in world</param>
        /// <param name="width">screen width</param>
        /// <param name="height">screen height</param>
        /// <param name="screenPos">object's 2D position on screen</param>
        /// <returns>true if object is visible, false otherwise</returns>
        public bool WorldToScreen(Vector3 worldPos, int width, int height, out Vector2 screenPos) {

            //multiply vector against matrix
            float screenX = (m11 * worldPos.x) + (m21 * worldPos.y) + (m31 * worldPos.z) + m41;
            float screenY = (m12 * worldPos.x) + (m22 * worldPos.y) + (m32 * worldPos.z) + m42;
            float screenW = (m14 * worldPos.x) + (m24 * worldPos.y) + (m34 * worldPos.z) + m44;

            //camera position (eye level/middle of screen)
            float camX = width / 2f;
            float camY = height / 2f;

            //convert to homogeneous position
            float x = camX + (camX * screenX / screenW);
            float y = camY - (camY * screenY / screenW);
            screenPos = new Vector2(x, y);

            //check if object is behind camera / off screen (not visible)
            //w = z where z is relative to the camera 
            return (screenW > 0.001f);
        }


        public override string ToString() {
            //display matrix cleanly in a grid
            return String.Format(
                "{0,8}{1,8}{2,8}{3,8}\n" +
                "{4,8}{5,8}{6,8}{7,8}\n" +
                "{8,8}{9,8}{10,8}{11,8}\n" +
                "{12,8}{13,8}{14,8}{15,8}",
                Math.Round(m11, 2), Math.Round(m12, 2), Math.Round(m13, 2), Math.Round(m14, 2),
                Math.Round(m21, 2), Math.Round(m22, 2), Math.Round(m23, 2), Math.Round(m24, 2),
                Math.Round(m31, 2), Math.Round(m32, 2), Math.Round(m33, 2), Math.Round(m34, 2),
                Math.Round(m41, 2), Math.Round(m42, 2), Math.Round(m43, 2), Math.Round(m44, 2));
        }
    }
}
