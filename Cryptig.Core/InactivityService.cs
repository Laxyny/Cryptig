using System;
using System.Runtime.InteropServices;

namespace Cryptig.Core
{
    public class InactivityService
    {
        private readonly TimeSpan _timeout;

        public InactivityService(TimeSpan timeout)
        {
            _timeout = timeout;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        [DllImport("user32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        private static TimeSpan GetIdleTime()
        {
            var info = new LASTINPUTINFO { cbSize = (uint)Marshal.SizeOf<LASTINPUTINFO>() };
            if (!GetLastInputInfo(ref info))
                return TimeSpan.Zero;
            uint idleMillis = (uint)Environment.TickCount - info.dwTime;
            return TimeSpan.FromMilliseconds(idleMillis);
        }

        public bool IsExpired()
        {
            return GetIdleTime() > _timeout;
        }
    }
}