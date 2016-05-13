using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace AssaultCubeHack {
    class Managed {
        // READ FLAGS
        public static uint PROCESS_VM_READ = 0x0010;
        public static uint PROCESS_VM_WRITE = 0x0020;
        public static uint PROCESS_VM_OPERATION = 0x0008;
        public static uint PAGE_READWRITE = 0x0004;

        // WINDOW FLAGS
        public static uint WS_BORDER = 0x800000;
        public static int GWL_STYLE = (-16);

        // KEYS
        public const int KEY_PRESSED = 0x8000;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        public static extern short GetKeyState(int KeyStates);

        [DllImport("dwmapi.dll")]
        public static extern void DwmExtendFrameIntoClientArea(IntPtr hWnd, ref int[] pMargins);

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(UInt32 dwAccess, bool inherit, int pid);

        [DllImport("kernel32.dll")]
        public static extern bool CloseHandle(IntPtr handle);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadProcessMemory(IntPtr hProcess, Int64 lpBaseAddress, [In, Out] byte[] lpBuffer, UInt64 dwSize, out IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        public static extern bool WriteProcessMemory(IntPtr hProcess, Int64 lpBaseAddress, [In, Out] byte[] lpBuffer, UInt64 dwSize, out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool VirtualProtectEx(IntPtr hProcess, IntPtr lpAddress, UInt32 dwSize, uint flNewProtect, out uint lpflOldProtect);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        public static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("User32.dll")]
        public static extern bool MoveWindow(IntPtr handle, int x, int y, int width, int height, bool redraw);

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT {
        public int Left;        // x position of upper-left corner
        public int Top;         // y position of upper-left corner
        public int Right;       // x position of lower-right corner
        public int Bottom;      // y position of lower-right corner
    }

    class Memory {

        public static bool GetProcessesByName(string pName, out Process process) {
            Process[] pList = Process.GetProcessesByName(pName);
            process = pList.Length > 0 ? pList[0] : null;
            return process != null;
        }

        private static IntPtr handle = IntPtr.Zero;

        public static IntPtr OpenProcess(int pId) {
            handle = Managed.OpenProcess(Managed.PROCESS_VM_READ | Managed.PROCESS_VM_WRITE | Managed.PROCESS_VM_OPERATION, false, pId);
            return handle;
        }

        public static IntPtr GetHandle() {
            return handle;
        }

        public static void CloseProcess() {
            Managed.CloseHandle(handle);
        }

        public static bool Write<T>(Int64 address, T t) {
            Byte[] Buffer = new Byte[Marshal.SizeOf(typeof(T))];

            GCHandle gHandle = GCHandle.Alloc(t, GCHandleType.Pinned);
            Marshal.Copy(gHandle.AddrOfPinnedObject(), Buffer, 0, Buffer.Length);
            gHandle.Free();

            uint oldProtect;
            Managed.VirtualProtectEx(handle, (IntPtr)address, (uint)Buffer.Length, Managed.PAGE_READWRITE, out oldProtect);
            IntPtr ptrBytesWritten;
            return Managed.WriteProcessMemory(handle, address, Buffer, (uint)Buffer.Length, out ptrBytesWritten);
        }

        /// <summary>
        /// Generic memory reader. Reads memory at address of process.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="address">Memoty address to access</param>
        /// <returns>memory read from process</returns>
        public static T Read<T>(Int64 address) {
            //create byte array with size of type
            byte[] buffer = new byte[Marshal.SizeOf(typeof(T))];

            //read memory
            IntPtr ByteRead;
            Managed.ReadProcessMemory(handle, address, buffer, (uint)buffer.Length, out ByteRead);

            //get structure from buffer
            GCHandle gHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            T data = (T)Marshal.PtrToStructure(gHandle.AddrOfPinnedObject(), typeof(T));
            gHandle.Free();

            return data;
        }

        /*
        public static Int64 ReadInt64(Int64 baseAddress) {
            byte[] buffer = new byte[8];
            IntPtr byteRead;
            Managed.ReadProcessMemory(handle, baseAddress, buffer, 8, out byteRead);
            return BitConverter.ToInt64(buffer, 0);
        }

        public static Int32 ReadInt32(Int64 baseAddress) {
            byte[] buffer = new byte[4];
            IntPtr byteRead;
            Managed.ReadProcessMemory(handle, baseAddress, buffer, 4, out byteRead);
            return BitConverter.ToInt32(buffer, 0);
        }

        public static float ReadFloat(Int64 baseAddress) {
            byte[] buffer = new byte[sizeof(float)];
            IntPtr byteRead;
            Managed.ReadProcessMemory(handle, baseAddress, buffer, sizeof(float), out byteRead);
            return BitConverter.ToSingle(buffer, 0);
        }

        public static byte ReadByte(Int64 baseAddress) {
            byte[] buffer = new byte[sizeof(byte)];
            IntPtr byteRead;
            Managed.ReadProcessMemory(handle, baseAddress, buffer, sizeof(byte), out byteRead);
            return buffer[0];
        }
        */

        public static string ReadString(Int64 baseAddress, UInt64 size) {
            byte[] buffer = new byte[size];
            IntPtr bytesRead;

            Managed.ReadProcessMemory(handle, baseAddress, buffer, size, out bytesRead);

            for (int i = 0; i < buffer.Length; i++) {
                if (buffer[i] == 0) {
                    byte[] _buffer = new byte[i];
                    Buffer.BlockCopy(buffer, 0, _buffer, 0, i);
                    return Encoding.ASCII.GetString(_buffer);
                }
            }
            return Encoding.ASCII.GetString(buffer);
        }

        public static string ReadString2(Int64 baseAddress, UInt64 size) {
            byte[] buffer = new byte[size];
            IntPtr bytesRead;

            Managed.ReadProcessMemory(handle, baseAddress, buffer, size, out bytesRead);
            return Encoding.ASCII.GetString(buffer);
        }

        
        public static Vector3 ReadVector3(Int64 baseAddress) {
            Vector3 tmp = new Vector3();

            byte[] buffer = new byte[12];
            IntPtr byteRead;

            Managed.ReadProcessMemory(handle, baseAddress, buffer, 12, out byteRead);
            tmp.X = BitConverter.ToSingle(buffer, (0 * 4));
            tmp.Y = BitConverter.ToSingle(buffer, (1 * 4));
            tmp.Z = BitConverter.ToSingle(buffer, (2 * 4));
            return tmp;
        }

        public static void WriteVector3(Int64 baseAddress, Vector3 vec) {
            Write<float>(baseAddress, vec.X); //x
            Write<float>(baseAddress + 4, vec.Y);//y
            Write<float>(baseAddress + 8, vec.Z);//z
        }

        
        public static Matrix ReadMatrix(Int64 baseAddress) {
            Matrix tmp = new Matrix();

            byte[] buffer = new byte[64];
            IntPtr byteRead;

            Managed.ReadProcessMemory(handle, baseAddress, buffer, 64, out byteRead);

            tmp.M11 = BitConverter.ToSingle(buffer, (0 * 4));
            tmp.M12 = BitConverter.ToSingle(buffer, (1 * 4));
            tmp.M13 = BitConverter.ToSingle(buffer, (2 * 4));
            tmp.M14 = BitConverter.ToSingle(buffer, (3 * 4));

            tmp.M21 = BitConverter.ToSingle(buffer, (4 * 4));
            tmp.M22 = BitConverter.ToSingle(buffer, (5 * 4));
            tmp.M23 = BitConverter.ToSingle(buffer, (6 * 4));
            tmp.M24 = BitConverter.ToSingle(buffer, (7 * 4));

            tmp.M31 = BitConverter.ToSingle(buffer, (8 * 4));
            tmp.M32 = BitConverter.ToSingle(buffer, (9 * 4));
            tmp.M33 = BitConverter.ToSingle(buffer, (10 * 4));
            tmp.M34 = BitConverter.ToSingle(buffer, (11 * 4));

            tmp.M41 = BitConverter.ToSingle(buffer, (12 * 4));
            tmp.M42 = BitConverter.ToSingle(buffer, (13 * 4));
            tmp.M43 = BitConverter.ToSingle(buffer, (14 * 4));
            tmp.M44 = BitConverter.ToSingle(buffer, (15 * 4));
            return tmp;
        }       

        public static bool IsValid(Int64 address) {
            return (address >= 0x10000 && address < 0x000F000000000000);
        }
    }
}