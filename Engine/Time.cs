using System.Diagnostics;

namespace Engine
{
    public class Time
    {
        public static Stopwatch stopwatch = Stopwatch.StartNew();
        public static float delta;
        public Time()
        {
            stopwatch.Start();
        }

        public long GetCurrentTime()
        {
            return stopwatch.ElapsedMilliseconds;
        }

        public float Delta(long t1, long t2)
        {
            delta = (float)t1 - t2;
            return delta;
        }
    }
}
