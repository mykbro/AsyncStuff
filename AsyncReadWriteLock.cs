using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncStuff
{
    /* 
     * This class implements a ReadWriteLock with asynchronous operations using the SemaphoreSlim .WaitAsync() method.     * 
     * The implementation is for the '3rd readers/writers problem' which is a fairer algorithm that does not priviledge neither writers nor readers:
     * 
     * In this implementation we have a pre-lock that the writer release only when it has acquired the real lock.
     * A reader instead try to check if he can acquire the pre-lock and if it can it immediately releases it.
     * This mechanism blocks the readers arrived after a writer, let the current readers finish, let the writer write and proceed. 
     * If a new writer arrives while a writer is WRITING and multiple readers are waiting (on NumReadersLock) it will have the same chance of access as the first reader as both are waiting on ObjectLock.
     * If a new writer arrives while a writer is WAITING it will wait on the PrenotationLock the same as the readers that arrives after it.
     *      
     */
    public class AsyncReadWriteLock : IDisposable
    {
        private readonly SemaphoreSlim _reservationSem;
        private readonly SemaphoreSlim _numReadersSem;
        private readonly SemaphoreSlim _resourceSem;
        private int _numReaders;

        public AsyncReadWriteLock()
        {
            _reservationSem = new SemaphoreSlim(1);
            _numReadersSem = new SemaphoreSlim(1);
            _resourceSem = new SemaphoreSlim(1);
            _numReaders = 0;
        }

        public void TryAcquireReadLock()
        {
            _reservationSem.Wait();         //reservation check for any pending write
            _reservationSem.Release();      //if check passed we immediately release the lock

            _numReadersSem.Wait();
            _numReaders++;
            if (_numReaders == 1)
                _resourceSem.Wait();
            _numReadersSem.Release();
        }

        public void TryReleaseReadLock()    //we can be blocked if any write is occuring (but not for reserved writes)
        {
            _numReadersSem.Wait();
            _numReaders--;
            if (_numReaders == 0)
                _resourceSem.Release();
            _numReadersSem.Release();
        }

        public void TryAcquireWriteLock()
        {
            _reservationSem.Wait();         //we first acquire the reservation to avoid starvation due to infinite readers
            _resourceSem.Wait();
            _reservationSem.Release();      //once we have the main lock we release the reservation
        }

        public void ReleaseWriteLock()
        {
            _resourceSem.Release();
        }

        public async Task TryAcquireReadLockAsync()
        {
            await _reservationSem.WaitAsync();      //reservation check for any pending write
            _reservationSem.Release();              //if check passed we immediately release the lock

            await _numReadersSem.WaitAsync();
            _numReaders++;
            if (_numReaders == 1)
                await _resourceSem.WaitAsync();
            _numReadersSem.Release();
        }

        public async Task TryReleaseReadLockAsync()    //we can be blocked if any write is occuring (but not for reserved writes)
        {
            await _numReadersSem.WaitAsync();
            _numReaders--;
            if (_numReaders == 0)
                _resourceSem.Release();
            _numReadersSem.Release();
        }

        public async Task TryAcquireWriteLockAsync()
        {
            await _reservationSem.WaitAsync();         //we first acquire the reservation to avoid starvation due to infinite readers
            await _resourceSem.WaitAsync();
            _reservationSem.Release();      //once we have the main lock we release the reservation
        }

        public void Dispose()
        {
            _numReadersSem.Dispose();
            _reservationSem.Dispose();
            _resourceSem.Dispose();
        }
    }
}
