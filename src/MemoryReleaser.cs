using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace MemEase
{
    internal static class MemoryReleaser
    {
        private const long MinimumWorkingSetBytes = 24L * 1024L * 1024L;
        private const int MaxProcessesToTrim = 64;
        private const int SettleMilliseconds = 400;

        private static readonly HashSet<string> ExcludedProcesses =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "system",
                "idle",
                "registry",
                "memory compression",
                "secure system",
                "smss",
                "csrss",
                "wininit",
                "winlogon",
                "services",
                "lsass",
                "lsm",
                "dwm",
                "fontdrvhost",
                "audiodg",
                "sihost",
                "ctfmon",
                "explorer",
                "startmenuexperiencehost",
                "searchhost",
                "shellexperiencehost",
                "textinputhost",
                "securityhealthservice",
                "securityhealthsystray",
                "msmpeng",
                "nissrv",
                "hipsdaemon",
                "hipstray",
                "sangforudprotectex",
                "applicationframehost",
                "translucenttb",
                "wallpaper32",
                "wallpaper64",
                "memease"
            };

        public static ReleaseResult Release(int excludedProcessId)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            MemorySnapshot before = MemorySnapshot.Capture();

            int trimmed = 0;
            int inaccessible = 0;
            List<Candidate> candidates = CollectCandidates(excludedProcessId);
            candidates.Sort(delegate(Candidate a, Candidate b)
            {
                return b.WorkingSet.CompareTo(a.WorkingSet);
            });

            int count = candidates.Count;
            if (count > MaxProcessesToTrim) count = MaxProcessesToTrim;

            for (int i = 0; i < count; i++)
            {
                Candidate candidate = candidates[i];
                IntPtr handle = NativeMethods.OpenProcess(
                    NativeMethods.PROCESS_QUERY_LIMITED_INFORMATION |
                    NativeMethods.PROCESS_SET_QUOTA,
                    false,
                    candidate.ProcessId);

                if (handle == IntPtr.Zero)
                {
                    inaccessible++;
                    continue;
                }

                try
                {
                    if (NativeMethods.EmptyWorkingSet(handle)) trimmed++;
                    else inaccessible++;
                }
                finally
                {
                    NativeMethods.CloseHandle(handle);
                }
            }

            if (trimmed > 0) Thread.Sleep(SettleMilliseconds);

            MemorySnapshot after = MemorySnapshot.Capture();
            stopwatch.Stop();
            return new ReleaseResult(before, after, trimmed, inaccessible, stopwatch.Elapsed);
        }

        private static List<Candidate> CollectCandidates(int excludedProcessId)
        {
            List<Candidate> candidates = new List<Candidate>();
            int currentProcessId;
            int currentSessionId;
            using (Process current = Process.GetCurrentProcess())
            {
                currentProcessId = current.Id;
                currentSessionId = current.SessionId;
            }
            int foregroundProcessId = GetForegroundProcessId();

            Process[] processes;
            try
            {
                processes = Process.GetProcesses();
            }
            catch
            {
                return candidates;
            }

            for (int i = 0; i < processes.Length; i++)
            {
                Process process = processes[i];
                try
                {
                    int processId = process.Id;
                    if (processId <= 4 || processId == currentProcessId) continue;
                    if (processId == foregroundProcessId) continue;
                    if (processId == excludedProcessId) continue;
                    if (process.SessionId != currentSessionId) continue;

                    string name = process.ProcessName;
                    if (ExcludedProcesses.Contains(name)) continue;

                    long workingSet = process.WorkingSet64;
                    if (workingSet < MinimumWorkingSetBytes) continue;

                    candidates.Add(new Candidate(processId, workingSet));
                }
                catch
                {
                    // A process can disappear between enumeration and inspection.
                }
                finally
                {
                    process.Dispose();
                }
            }
            return candidates;
        }

        internal static int GetForegroundProcessId()
        {
            try
            {
                IntPtr window = NativeMethods.GetForegroundWindow();
                if (window == IntPtr.Zero) return 0;
                uint processId;
                NativeMethods.GetWindowThreadProcessId(window, out processId);
                return (int)processId;
            }
            catch
            {
                return 0;
            }
        }

        private sealed class Candidate
        {
            public readonly int ProcessId;
            public readonly long WorkingSet;

            public Candidate(int processId, long workingSet)
            {
                ProcessId = processId;
                WorkingSet = workingSet;
            }
        }
    }
}
