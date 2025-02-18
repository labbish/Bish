namespace Bish {

    internal class BishThreadPool {
        private const int MaxThreadCount = Program.MaxThreadCount;
        private int count = 0;

        public T? GetResult<T>(Func<T> func) {
            CheckThreadCount();
            count++;
            T? result = default;
            Exception? exception = null;
            Thread thread = new(() => {
                try {
                    result = func();
                }
                catch (Exception ex) {
                    exception = ex;
                }
            });
            thread.Start();
            thread.Join();
            count--;
            if (exception is not null) throw exception;
            return result;
        }

        private void CheckThreadCount() {
            if (count > MaxThreadCount)
                throw new BishThreadOverflowException($"Thread Overflow: Found {MaxThreadCount} Same Threads");
        }
    }

    internal class BishThreadOverflowException(string message) : Exception(message) { }
}