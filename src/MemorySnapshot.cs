using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;

namespace MemEase
{
    internal sealed class MemorySnapshot
    {
        private const double Gigabyte = 1024.0 * 1024.0 * 1024.0;

        public readonly long TotalBytes;
        public readonly long AvailableBytes;
        public readonly int LoadPercent;

        public MemorySnapshot(long totalBytes, long availableBytes, int loadPercent)
        {
            TotalBytes = totalBytes;
            AvailableBytes = availableBytes;
            LoadPercent = loadPercent < 0 ? 0 : (loadPercent > 100 ? 100 : loadPercent);
        }

        public long UsedBytes
        {
            get
            {
                long used = TotalBytes - AvailableBytes;
                return used < 0 ? 0 : used;
            }
        }

        public double TotalGigabytes
        {
            get { return TotalBytes / Gigabyte; }
        }

        public string UsedText
        {
            get
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "{0:0.0} / {1:0.0} GB",
                    UsedBytes / Gigabyte,
                    TotalBytes / Gigabyte);
            }
        }

        public static MemorySnapshot Capture()
        {
            MEMORYSTATUSEX status = new MEMORYSTATUSEX();
            status.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            if (!NativeMethods.GlobalMemoryStatusEx(ref status))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            return new MemorySnapshot(
                status.ullTotalPhys > long.MaxValue ? long.MaxValue : (long)status.ullTotalPhys,
                status.ullAvailPhys > long.MaxValue ? long.MaxValue : (long)status.ullAvailPhys,
                (int)status.dwMemoryLoad);
        }
    }
}
