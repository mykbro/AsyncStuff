using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncStuff
{
    /*  
     *   Like a Monitor but with an external lock.
     *   More similar to C++ condition variables.
     *   
     *   A thread can then wait on multiple CVs all "belonging" to the same external lock.
     *   This also "solves" the problem with the sometimes akward choice between Pulse and PulseAll.
     *   Sometimes we may sleep waiting for multiple different conditions. With multiple CV (one for each condition probably) we can granularly use Pulse or PulseAll only on the appropriate CV and wake up the 'correct' number of threads.
     *   With a Monitor object, which means a single CV, we would have to wakeup many or all of the threads for nothing because we would have no granularity. 
     */

    public class AsyncConditionVariable : IDisposable
    {
        private readonly SpinLock _pendingWaitsLock;
        private readonly SemaphoreSlim _waitingSem;
        private int _pendingWaits;

        public AsyncConditionVariable()
        {
            _pendingWaitsLock = new SpinLock();
            _waitingSem = new SemaphoreSlim(0);
            _pendingWaits = 0;
        }

        public void Wait(SpinLock cvLock)
        {
            _pendingWaitsLock.Wait();
            _pendingWaits++;
            _pendingWaitsLock.Release();

            cvLock.Release();
            _waitingSem.Wait();
            cvLock.Wait();   //we retake the lock once awaken
        }

        public async Task WaitAsync(SpinLock cvLock)
        {
            _pendingWaitsLock.Wait();
            _pendingWaits++;
            _pendingWaitsLock.Release();

            cvLock.Release();
            await _waitingSem.WaitAsync();
            cvLock.Wait();   //we retake the lock once awaken
        }

        public void Notify()
        {
            _pendingWaitsLock.Wait();
            if (_pendingWaits > 0)
            {
                _waitingSem.Release();
                _pendingWaits--;
            }
            _pendingWaitsLock.Release();
        }

        public void NotifyAll()
        {
            _pendingWaitsLock.Wait();
            if (_pendingWaits > 0)
            {
                _waitingSem.Release(_pendingWaits);
                _pendingWaits = 0;
            }
            _pendingWaitsLock.Release();
        }

        public void Dispose()
        {
            _waitingSem.Dispose();
        }
    }
}
