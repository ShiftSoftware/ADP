using System.Security.Cryptography;
using System.Text;

namespace ShiftSoftware.ADP.SyncAgent
{
    public class CacheableCSVEngine<T> : FileHelpers.FileHelperEngine<T>, IDisposable
        where T : CacheableCSV
    {
        private readonly HashAlgorithm _algorithm;
        private bool _disposed = false;
        private readonly object _lock = new object();

        public CacheableCSVEngine() : this(Encoding.UTF8) { }

        public CacheableCSVEngine(Encoding encoding) : base(encoding)
        {
            _algorithm = SHA512.Create();
            AfterReadRecord += CacheableCSVEngine_AfterReadRecord;
        }

        private void CacheableCSVEngine_AfterReadRecord(FileHelpers.EngineBase engine, FileHelpers.Events.AfterReadEventArgs<T> e)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(CacheableCSVEngine<T>));

            // Ensure thread safety
            lock (_lock)
            {
                e.Record.id = BitConverter.ToString(_algorithm.ComputeHash(Encoding.UTF8.GetBytes(e.RecordLine))).Replace("-", string.Empty);
            }
        }

        // Implement IDisposable pattern to ensure _algorithm is disposed
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _algorithm?.Dispose();
                }
                _disposed = true;
            }
        }

        ~CacheableCSVEngine()
        {
            Dispose(false);
        }
    }
}