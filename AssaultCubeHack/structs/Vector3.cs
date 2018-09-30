
using System;

namespace AssaultCubeHack
{
  public class Vector3
  {
    public float x;
    public float y;
    public float z;

    public Vector3() { }

    public Vector3(float x, float y, float z)
    {
      this.x = x;
      this.y = y;
      this.z = z;
    }

    public float Distance(Vector3 vector)
    {
      float dx = vector.x - x;
      float dy = vector.y - y;
      float dz = vector.z - z;
      return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    public override string ToString()
    {
      return string.Format("{0}, {1}, {2}", Math.Round(x, 2), Math.Round(y, 2), Math.Round(z, 2));
    }
  }
}
