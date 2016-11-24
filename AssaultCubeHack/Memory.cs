using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace AssaultCubeHack {

    /// <summary>
    /// PInvoke (Platform Invoke) Signatures for calls to external Win32 and other unmanaged APIs from managed code.
    /// http://www.pinvoke.net
    /// http://winapi.freetechsecrets.com/
    /// </summary>
    class Win32API {
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

        //user32 imports
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);      

        [DllImport("user32.dll")]
        public static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern bool MoveWindow(IntPtr handle, int x, int y, int width, int height, bool redraw);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        public static extern short GetKeyState(int KeyStates);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        //dwmapi imports
        [DllImport("dwmapi.dll")]
        public static extern void DwmExtendFrameIntoClientArea(IntPtr hWnd, ref int[] pMargins);

        //kernal32 imports
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
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT {
        public int Left;   // x position of upper-left corner
        public int Top;    // y position of upper-left corner
        public int Right;  // x position of lower-right corner
        public int Bottom; // y position of lower-right corner
    }

    class Memory {
        //handle of process we are attached to
        private static IntPtr handle = IntPtr.Zero;

        /// <summary>
        /// Get handle to process with read and write permissions.
        /// </summary>
        /// <param name="pId">process ID</param>
        /// <returns>handle</returns>
        public static IntPtr OpenProcess(int pId) {
            handle = Win32API.OpenProcess(Win32API.PROCESS_VM_READ | Win32API.PROCESS_VM_WRITE | Win32API.PROCESS_VM_OPERATION, false, pId);
            return handle;
        }

        public static IntPtr GetHandle() {
            return handle;
        }

        /// <summary>
        /// Release / invalidate handle.
        /// </summary>
        public static void CloseProcess() {
            Win32API.CloseHandle(handle);
        }

        public static bool GetProcessesByName(string pName, out Process process) {
            Process[] pList = Process.GetProcessesByName(pName);
            process = pList.Length > 0 ? pList[0] : null;
            return process != null;
        }

        public static bool IsProcessRunning(Process process) {
            foreach (Process p in Process.GetProcesses()) {
                if (p.ProcessName == process.ProcessName)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Write genertic type into memory at address.
        /// </summary>
        /// <returns>write succeeded</returns>
        public static bool Write<T>(Int64 address, T t) {
            //create byte array with size of type
            Byte[] buffer = new Byte[Marshal.SizeOf(typeof(T))];

            //allocate handle for buffer
            GCHandle gHandle = GCHandle.Alloc(t, GCHandleType.Pinned);
            //arrange data from unmanaged block of memory to structure of type T
            Marshal.Copy(gHandle.AddrOfPinnedObject(), buffer, 0, buffer.Length);
            gHandle.Free(); //release handle

            //change access permission so we can write into memory
            uint oldProtect;
            Win32API.VirtualProtectEx(handle, (IntPtr)address, (uint)buffer.Length, Win32API.PAGE_READWRITE, out oldProtect);

            //write buffer into memory
            IntPtr ptrBytesWritten;
            return Win32API.WriteProcessMemory(handle, address, buffer, (uint)buffer.Length, out ptrBytesWritten);
        }

        /// <summary>
        /// Reads memory of generic type at address.
        /// </summary>
        public static T Read<T>(Int64 address) {
            //create byte array with size of type
            byte[] buffer = new byte[Marshal.SizeOf(typeof(T))];

            //read memory
            IntPtr bytesRead;
            Win32API.ReadProcessMemory(handle, address, buffer, (uint)buffer.Length, out bytesRead);

            //allocate handle for buffer
            GCHandle gHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            //arrange data from unmanaged block of memory to structure of type T
            T data = (T)Marshal.PtrToStructure(gHandle.AddrOfPinnedObject(), typeof(T));
            gHandle.Free(); //release handle

            return data;
        }

        public static string ReadString(Int64 baseAddress, UInt64 size) {
            //create buffer for string
            byte[] buffer = new byte[size];

            //read memory into buffer
            IntPtr bytesRead;
            Win32API.ReadProcessMemory(handle, baseAddress, buffer, size, out bytesRead);

            //encode bytes to ascii
            for (int i = 0; i < buffer.Length; i++) {
                if (buffer[i] == 0) {
                    byte[] tmpBuffer = new byte[i];
                    Buffer.BlockCopy(buffer, 0, tmpBuffer, 0, i);
                    return Encoding.ASCII.GetString(tmpBuffer);
                }
            }
            return Encoding.ASCII.GetString(buffer);
        }

        public static string ReadString2(Int64 baseAddress, UInt64 size) {
            //create buffer for string
            byte[] buffer = new byte[size];

            //read memory into buffer
            IntPtr bytesRead;
            Win32API.ReadProcessMemory(handle, baseAddress, buffer, size, out bytesRead);

            //encode bytes to ASCII
            return Encoding.ASCII.GetString(buffer);
        }

        /// <summary>
        /// Read 3 consecutive floats into x,y,z of a Vector
        /// </summary>
        public static Vector3 ReadVector3(Int64 baseAddress) {
            //3 floats contiguously in memory
            byte[] buffer = new byte[3*4];

            //read memory into buffer
            IntPtr bytesRead;
            Win32API.ReadProcessMemory(handle, baseAddress, buffer, (ulong)buffer.Length, out bytesRead);

            //convert bytes to floats
            Vector3 tmp = new Vector3();
            tmp.X = BitConverter.ToSingle(buffer, (0 * 4));
            tmp.Y = BitConverter.ToSingle(buffer, (1 * 4));
            tmp.Z = BitConverter.ToSingle(buffer, (2 * 4));
            return tmp;
        }

        /// <summary>
        /// Write 3 floats in vector(x,y,z) consecutively into memory at address
        /// </summary>
        public static void WriteVector3(Int64 baseAddress, Vector3 vec) {
            Write<float>(baseAddress + 0, vec.X); //x
            Write<float>(baseAddress + 4, vec.Y); //y
            Write<float>(baseAddress + 8, vec.Z); //z
        }

        /// <summary>
        /// Reads 16 consecutive floats into a Matrix
        /// </summary>
        public static Matrix ReadMatrix(Int64 baseAddress) {
            //float matrix[16]; 16-value array laid out contiguously in memory       
            byte[] buffer = new byte[16*4];

            //read memory into buffer
            IntPtr bytesRead;
            Win32API.ReadProcessMemory(handle, baseAddress, buffer, (ulong)buffer.Length, out bytesRead);

            //convert bytes to floats
            Matrix tmp = new Matrix();
            tmp.m11 = BitConverter.ToSingle(buffer, (0 * 4));
            tmp.m12 = BitConverter.ToSingle(buffer, (1 * 4));
            tmp.m13 = BitConverter.ToSingle(buffer, (2 * 4));
            tmp.m14 = BitConverter.ToSingle(buffer, (3 * 4));

            tmp.m21 = BitConverter.ToSingle(buffer, (4 * 4));
            tmp.m22 = BitConverter.ToSingle(buffer, (5 * 4));
            tmp.m23 = BitConverter.ToSingle(buffer, (6 * 4));
            tmp.m24 = BitConverter.ToSingle(buffer, (7 * 4));

            tmp.m31 = BitConverter.ToSingle(buffer, (8 * 4));
            tmp.m32 = BitConverter.ToSingle(buffer, (9 * 4));
            tmp.m33 = BitConverter.ToSingle(buffer, (10 * 4));
            tmp.m34 = BitConverter.ToSingle(buffer, (11 * 4));

            tmp.m41 = BitConverter.ToSingle(buffer, (12 * 4));
            tmp.m42 = BitConverter.ToSingle(buffer, (13 * 4));
            tmp.m43 = BitConverter.ToSingle(buffer, (14 * 4));
            tmp.m44 = BitConverter.ToSingle(buffer, (15 * 4));
            return tmp;
        }       

        public static bool IsValid(Int64 address) {
            return (address >= 0x10000 && address < 0x000F000000000000);
        }
    }
}