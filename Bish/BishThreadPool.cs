namespace Bish {

    internal class BishThreadPool {
        private const int MaxThreadCount = Program.MaxThreadCount;
        private readonly List<int> threads = [];

        public T? GetResult<T>(int hash, Func<T> func) {
            CheckThreadCount(hash);
            threads.Add(hash);
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
            threads.Remove(hash);
            if (exception is not null) throw exception;
            return result;
        }

        private void CheckThreadCount(int hash) {
            int count = threads.Where(thread => thread == hash).Count();
            if (count > MaxThreadCount)
                throw new BishThreadOverflowException($"Thread Overflow: Found {MaxThreadCount} Same Threads");
        }
    }

    internal class BishThreadOverflowException(string message) : Exception(message) { }
}