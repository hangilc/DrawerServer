using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Drawing.Printing;

namespace DrawerServer
{
    static class Win32
    {
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern IntPtr GlobalLock(IntPtr handle);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern bool GlobalUnlock(IntPtr handle);

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Auto)]
        public struct DEVMODE
        {
            private const int CCHDEVICENAME = 32;
            private const int CCHFORMNAME = 32;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
            public string dmDeviceName;
            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;

            public short dmOrientation;
            public short dmPaperSize;
            public short dmPaperLength;
            public short dmPaperWidth;
            public short dmScale;
            public short dmCopies;
            public short dmDefaultSource;
            public short dmPrintQuality;

            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHFORMNAME)]
            public string dmFormName;
            public short dmLogPixels;
            public int dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;
            public int dmICMMethod;
            public int dmICMIntent;
            public int dmMediaType;
            public int dmDitherType;
            public int dmReserved1;
            public int dmReserved2;
            public int dmPanningWidth;
            public int dmPanningHeight;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class DEVNAMES
        {
            public short wDriverOffset;
            public short wDeviceOffset;
            public short wOutputOffset;
            public short wDefault;
        }

        public static byte[] CopyDevmode(PrinterSettings settings)
        {
            IntPtr hdevmode = settings.GetHdevmode();
            IntPtr pdevmode = Win32.GlobalLock(hdevmode);
            Win32.DEVMODE devmode = (Win32.DEVMODE)Marshal.PtrToStructure(pdevmode, typeof(Win32.DEVMODE));
            int totalSize = devmode.dmSize + devmode.dmDriverExtra;
            byte[] devmodeData = new byte[totalSize];
            Marshal.Copy(pdevmode, devmodeData, 0, totalSize);
            Win32.GlobalUnlock(hdevmode);
            return devmodeData;
        }

        public static void SetDevmode(PrinterSettings settings, byte[] devmodeData)
        {
            IntPtr buf = Marshal.AllocHGlobal(devmodeData.Length);
            Marshal.Copy(devmodeData, 0, buf, devmodeData.Length);
            Win32.DEVMODE devmode2 = (Win32.DEVMODE)Marshal.PtrToStructure(buf, typeof(Win32.DEVMODE));
            settings.PrinterName = devmode2.dmDeviceName;
            settings.SetHdevmode(buf);
            Marshal.FreeHGlobal(buf);
        }

        public static DEVMODE ParseDevmode(byte[] devmodeData)
        {
            GCHandle handle = GCHandle.Alloc(devmodeData, GCHandleType.Pinned);
            DEVMODE devmode = (DEVMODE)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(DEVMODE));
            handle.Free();
            return devmode;
        }

        public static byte[] CopyDevnames(PrinterSettings settings)
        {
            IntPtr hdevnames = settings.GetHdevnames();
            IntPtr pdevnames = Win32.GlobalLock(hdevnames);
            Win32.DEVNAMES devnames = (Win32.DEVNAMES)Marshal.PtrToStructure(pdevnames, typeof(Win32.DEVNAMES));
            int charSize = Marshal.SystemDefaultCharSize;
            string outputName = Marshal.PtrToStringAuto(pdevnames + devnames.wOutputOffset * charSize);
            int devnamesSize = (devnames.wOutputOffset + outputName.Length + 1) * charSize;
            byte[] devnamesData = new byte[devnamesSize];
            Marshal.Copy(pdevnames, devnamesData, 0, devnamesSize);
            Win32.GlobalUnlock(hdevnames);
            return devnamesData;
        }

        public static void SetDevnames(PrinterSettings settings, byte[] devnamesData)
        {
            IntPtr buf = Marshal.AllocHGlobal(devnamesData.Length);
            Marshal.Copy(devnamesData, 0, buf, devnamesData.Length);
            settings.SetHdevnames(buf);
            Marshal.FreeHGlobal(buf);
        }

        public static void ParseDevnames(byte[] data, out string driver, out string device, out string output)
        {
            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            IntPtr ptr = handle.AddrOfPinnedObject();
            int charSize = Marshal.SystemDefaultCharSize;
            DEVNAMES devnames = (DEVNAMES)Marshal.PtrToStructure(ptr, typeof(DEVNAMES));
            driver = Marshal.PtrToStringAuto(ptr + devnames.wDriverOffset * charSize);
            device = Marshal.PtrToStringAuto(ptr + devnames.wDeviceOffset * charSize);
            output = Marshal.PtrToStringAuto(ptr + devnames.wOutputOffset * charSize);
            handle.Free();
        }
    }

}
