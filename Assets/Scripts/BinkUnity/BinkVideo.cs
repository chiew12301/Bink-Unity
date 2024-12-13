using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System;
using System.IO;

namespace KC_Custom.Bink
{
    public class BinkVideo
    {
#if UNITY_64
        private const string binkDll = "bink2w64";
#else
        private const string binkDll = "bink2w32";
#endif
        public enum BinkOpenEnum : uint
        {
            BINK_OPEN_STREAM = 0
        }

        public enum BinkSurface
        {
            BINKSURFACE8 = 0, // haven't tested yet.
            BINKSURFACE24 = 1, // Normal 24bit
            BINKSURFACE24R = 2, // reversed order
            BINKSURFACE32 = 3, // normal 32bit
            BINKSURFACE32R = 4, // reversed order
            BINKSURFACE32A = 5, // normal with alpha
            BINKSURFACE32AR = 6, // reversed order with alpha
            BINKSURFACE4444 = 7, // 16bit with 4 channels. each 4 bit.
            BINKSURFACE565R = 8, // 16bit 565 reversed bitorder
            BINKSURFACE555R = 9, // 16bit 555 reversed bitorder
            BINKSURFACE565 = 10, // 16bit 565. Standard for Windows Bitmaps
            BINKSURFACE555 = 11, // 16bit 565
            BINKSURFACE5551 = 12, // 16bit 555+1bit alpha. Weird!
            BINKSURFACE16INT = 13, // 16bit strange interleaving ?!
            BINKSURFACE16INTR = 14 // ..and reversed. Who uses this ?
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TBink
        {
            public uint Width;
            public uint Height;
            public uint Frames;
            public uint CurrentFrame;
            public uint LastFrame;
            public uint FrameRate;
            public uint FrameRate2;
        }
        public struct SubStr
        {
            public int Start;
            public int End;
            public string Text;
        }

        public const string folder = "\\Video\\";

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr GetProcAddress(IntPtr hModule, string procName);
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr LoadLibrary(string lpszLib);
        [DllImport("kernel32.dll")]
        public static extern void RtlZeroMemory(IntPtr dst, int length);
        [DllImport(binkDll)]
        public static extern IntPtr BinkOpen(string filename, BinkOpenEnum flag);
        [DllImport(binkDll)]
        public static extern uint BinkDoFrame(IntPtr bink);
        [DllImport(binkDll)]
        public static extern uint BinkNextFrame(IntPtr bink);
        [DllImport(binkDll)]
        public static extern uint BinkCopyToBuffer(IntPtr bink, IntPtr dest, uint Pitch, uint Height, uint Xoffset, uint Yoffset, BinkSurface flag);
        [DllImport(binkDll)]
        public static extern uint BinkWait(IntPtr bink);
        [DllImport(binkDll)]
        public static extern void BinkClose(IntPtr bink);
        [DllImport(binkDll)]
        public static extern bool BinkSetSoundSystem(IntPtr SoundSystem, IntPtr NotifyCallback);
        [DllImport(binkDll)]
        public static extern uint BinkSetVolume(IntPtr bink, uint id, uint volume);
        [DllImport(binkDll)]
        public static extern uint BinkGoto(IntPtr bink, uint target, uint p3);

        public static IntPtr BinkOpenDirectSound()
        {
            IntPtr handle;
            handle = LoadLibrary(binkDll);
#if UNITY_64
            return GetProcAddress(handle, "BinkOpenDirectSound");
#else
            return GetProcAddress(handle, "_BinkOpenDirectSound@4");
#endif
        }
    }
}