using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Utilities;

namespace AssaultCubeHack {

    class Program {

        static void Main(string[] args) {
            //taget process
            Process process;

            //try to find game
            if (Memory.GetProcessesByName("ac_client", out process)) {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Process found: " + process.Id + ": " + process.ProcessName);
                new AssaultHack(process);
            } else {
                Console.WriteLine("Process not found.");
            }
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey(true);
        }


    }

   
}
