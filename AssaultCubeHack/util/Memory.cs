using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace AssaultCubeHack {

    class Memory {
        //handle of process we are attached to
        private IntPtr handle = IntPtr.Zero;

        public Memory(int processID) {
            handle = OpenProcess(processID);
        }

        public IntPtr GetHandle() {
            return handle;
        }

        /// <summary>
        /// Release / invalidate handle.
        /// </summary>
        public void CloseProcess() {
            NativeMethods.CloseHandle(handle);
        }


        /// <summary>
        /// Get handle to process with read and write permissions.
        /// </summary>
        /// <param name="pId">process ID</param>
        /// <returns>handle</returns>
        public static IntPtr OpenProcess(int pId) {
            return NativeMethods.OpenProcess(NativeMethods.PROCESS_VM_READ | NativeMethods.PROCESS_VM_WRITE | NativeMethods.PROCESS_VM_OPERATION, false, pId);
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



        #region read/write
        /// <summary>
        /// Write genertic type into memory at address.
        /// </summary>
        /// <returns>write succeeded</returns>
        public bool Write<T>(long address, T type) {
            //create byte array with size of type
            byte[] buffer = new byte[Marshal.SizeOf(typeof(T))];

            //allocate handle for buffer
            GCHandle gHandle = GCHandle.Alloc(type, GCHandleType.Pinned);
            //arrange data from unmanaged block of memory to structure of type T
            Marshal.Copy(gHandle.AddrOfPinnedObject(), buffer, 0, buffer.Length);
            gHandle.Free(); //release handle

            //change access permission so we can write into memory
            NativeMethods.VirtualProtectEx(handle, (IntPtr)address, (uint)buffer.Length, NativeMethods.PAGE_READWRITE, out uint oldProtect);

            //write buffer into memory
            return NativeMethods.WriteProcessMemory(handle, address, buffer, (uint)buffer.Length, out IntPtr ptrBytesWritten);
        }

        /// <summary>
        /// Reads memory of generic type at address.
        /// </summary>
        public T Read<T>(long address) {
            //create byte array with size of type
            byte[] buffer = new byte[Marshal.SizeOf(typeof(T))];

            //read memory
            NativeMethods.ReadProcessMemory(handle, address, buffer, (uint)buffer.Length, out IntPtr bytesRead);

            //allocate handle for buffer
            GCHandle gHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);//TODO: this line is crashing in Release but not Debug compile. why? AccessViolationException
            //arrange data from unmanaged block of memory to structure of type T
            T data = (T)Marshal.PtrToStructure(gHandle.AddrOfPinnedObject(), typeof(T));
            gHandle.Free(); //release handle

            return data;
        }

        public string ReadString(long baseAddress, ulong size) {
            //create buffer for string
            byte[] buffer = new byte[size];

            //read memory into buffer
            NativeMethods.ReadProcessMemory(handle, baseAddress, buffer, size, out IntPtr bytesRead);

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

        public string ReadString2(long baseAddress, ulong size) {
            //create buffer for string
            byte[] buffer = new byte[size];

            //read memory into buffer
            NativeMethods.ReadProcessMemory(handle, baseAddress, buffer, size, out IntPtr bytesRead);

            //encode bytes to ASCII
            return Encoding.ASCII.GetString(buffer);
        }

        /// <summary>
        /// Read 3 consecutive floats into x,y,z of a Vector
        /// </summary>
        public Vector3 ReadVector3(long baseAddress) {
            //3 floats contiguously in memory
            byte[] buffer = new byte[3*4];

            //read memory into buffer
            NativeMethods.ReadProcessMemory(handle, baseAddress, buffer, (ulong)buffer.Length, out IntPtr bytesRead);

            //convert bytes to floats
            Vector3 vec = new Vector3 {
                x = BitConverter.ToSingle(buffer, (0 * 4)),
                y = BitConverter.ToSingle(buffer, (1 * 4)),
                z = BitConverter.ToSingle(buffer, (2 * 4))
            };
            return vec;
        }

        /// <summary>
        /// Write 3 floats in vector(x,y,z) consecutively into memory at address
        /// </summary>
        public void WriteVector3(long baseAddress, Vector3 vec) {
            Write(baseAddress + 0, vec.x); //x
            Write(baseAddress + 4, vec.y); //y
            Write(baseAddress + 8, vec.z); //z
        }

        /// <summary>
        /// Reads 16 consecutive floats into a Matrix
        /// </summary>
        public Matrix ReadMatrix(long baseAddress) {
            //float matrix[16]; 16-value array laid out contiguously in memory       
            byte[] buffer = new byte[16*4];

            //read memory into buffer
            NativeMethods.ReadProcessMemory(handle, baseAddress, buffer, (ulong)buffer.Length, out IntPtr bytesRead);

            //convert bytes to floats
            Matrix mat = new Matrix {
                m11 = BitConverter.ToSingle(buffer, (0 * 4)),
                m12 = BitConverter.ToSingle(buffer, (1 * 4)),
                m13 = BitConverter.ToSingle(buffer, (2 * 4)),
                m14 = BitConverter.ToSingle(buffer, (3 * 4)),

                m21 = BitConverter.ToSingle(buffer, (4 * 4)),
                m22 = BitConverter.ToSingle(buffer, (5 * 4)),
                m23 = BitConverter.ToSingle(buffer, (6 * 4)),
                m24 = BitConverter.ToSingle(buffer, (7 * 4)),

                m31 = BitConverter.ToSingle(buffer, (8 * 4)),
                m32 = BitConverter.ToSingle(buffer, (9 * 4)),
                m33 = BitConverter.ToSingle(buffer, (10 * 4)),
                m34 = BitConverter.ToSingle(buffer, (11 * 4)),

                m41 = BitConverter.ToSingle(buffer, (12 * 4)),
                m42 = BitConverter.ToSingle(buffer, (13 * 4)),
                m43 = BitConverter.ToSingle(buffer, (14 * 4)),
                m44 = BitConverter.ToSingle(buffer, (15 * 4))
            };
            return mat;
        }       

        public static bool IsValid(long address) {
            return (address >= 0x10000 && address < 0x000F000000000000);
        }
        #endregion
    }
}