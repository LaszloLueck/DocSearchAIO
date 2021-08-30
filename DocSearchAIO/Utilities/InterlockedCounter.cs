using System.Threading;

namespace DocSearchAIO.Utilities
{
    public sealed class InterlockedCounter
    {
        private int _current = 0;

        public void Increment()
        {
            Interlocked.Increment(ref _current);
        }

        public void Add(int value)
        {
            Interlocked.Add(ref _current, value);
        }

        public int Current()
        {
            return _current;
        }

        public void Reset()
        {
            _current = 0;
        }
    }
}