using System;

namespace MemEase
{
    internal sealed class ReleaseResult
    {
        public readonly MemorySnapshot Before;
        public readonly MemorySnapshot After;
        public readonly int TrimmedCount;
        public readonly int InaccessibleCount;
        public readonly TimeSpan Duration;

        public ReleaseResult(
            MemorySnapshot before,
            MemorySnapshot after,
            int trimmedCount,
            int inaccessibleCount,
            TimeSpan duration)
        {
            Before = before;
            After = after;
            TrimmedCount = trimmedCount;
            InaccessibleCount = inaccessibleCount;
            Duration = duration;
        }

        public long FreedBytes
        {
            get
            {
                long freed = Before.UsedBytes - After.UsedBytes;
                return freed > 0 ? freed : 0;
            }
        }
    }

    internal static class ByteFormat
    {
        private const long Megabyte = 1024L * 1024L;
        private const long Gigabyte = 1024L * 1024L * 1024L;

        public static string Format(long bytes)
        {
            if (bytes >= Gigabyte)
            {
                return string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "{0:0.0} GB",
                    bytes / (double)Gigabyte);
            }
            return string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "{0:0} MB",
                bytes / (double)Megabyte);
        }
    }
}
