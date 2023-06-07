using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncStuff
{
    public class AsyncMonitor
    {
       
        //private readonly SemaphoreSlim _globalLock;
        //private readonly SemaphoreSlim _auxLock;          //used to evaluate the _globallock status before release (we can concurrently be exiting and waiting)
        //private readonly SemaphoreSlim _pendingWaitsLock;

        //we chose SpinLocks instead of Locks/Semaphores because we'll quickly enter/exit them. Even the monitor itself will be quickly entered/waitedOn/exited.
        private readonly SpinLock _globalLock;              //this is the lock associated with the monitor. Holding the monitor means holding this lock.       
        private readonly SpinLock _pendingWaitsLock;        //used to atomically evaluate and update the pending threads number in Wait() and Notify() methods
                                                            
        private readonly SemaphoreSlim _waitingSem;         //threads waiting on the monitor will wait on this semaphore
                                                            //here we need a true Semaphore because we can wait for a lot of time and we need to use WaitAsync
        private int _pendingWaits;                          //will keep track of the number of sleeping threads that we'll need to wake up
        private bool _disposed;                             //used for implementing IsDisposed checks (currently not implemented)

        public AsyncMonitor()
        {
            //_globalSem = new SemaphoreSlim(1);
            //_auxSem = new SemaphoreSlim(1);
            //_pendingWaitsSem = new SemaphoreSlim(1);
            _globalLock = new SpinLock();            
            _pendingWaitsLock = new SpinLock();
            _waitingSem = new SemaphoreSlim(0);
            _pendingWaits = 0;
        }

        public bool Taken
        {
            get { return _globalLock.CurrentCount == 0; }
        }

        public void Enter()
        {            
            _globalLock.Wait();
        }

        /*
        public async Task EnterAsync()
        {
            await _globalSem.WaitAsync();
        }
        */

        public void Exit()
        {
            /*
            _auxLock.Wait();
            if(this.Taken)
                _globalLock.Release();
            _auxLock.Release();
            */
            _globalLock.Release();  //releasing a non-taken lock is idempotent (we could also use a semaphore with a limit of 1)
        }

        public void Wait()
        {
            _pendingWaitsLock.Wait();
            _pendingWaits++;
            _pendingWaitsLock.Release();          

            this.Exit();    //we Exit if the monitor is taken
            _waitingSem.Wait();
            this.Enter();   //we retake the monitor once awaken
        }

        public async Task WaitAsync()
        {
            _pendingWaitsLock.Wait();                        //we use the synchronous version instead of await _pendingWaitsSem.WaitAsync();
            _pendingWaits++;
            _pendingWaitsLock.Release();
            //Interlocked.Increment(ref _pendingWaits);

            this.Exit();
            await _waitingSem.WaitAsync();
            this.Enter();   //we retake the monitor once awaken
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

        /*
        public async Task NotifyAsync()
        {
            await _pendingWaitsSem.WaitAsync();
            if (_pendingWaits > 0)
            {
                _waitingSem.Release();
                _pendingWaits--;
            }
            _pendingWaitsSem.Release();
        }
        */

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

        /*
        public async Task NotifyAllAsync()
        {
            await _pendingWaitsSem.WaitAsync();
            if (_pendingWaits > 0)
            {
                _waitingSem.Release(_pendingWaits);
                _pendingWaits = 0;
            }
            _pendingWaitsSem.Release();
        }
        */

        public void Dispose()
        {
            //We let the Monitor user Notify and Exit correctly.
            //Usually a Monitor will be hidden inside a shared object (like a BLockingQueue or a Barrier).
            //That class's object will also need a Dispose method (which will call this Dispose) and the owner of that object will know when it'll be safe to Dispose it.
            //Usually a shared object's lifetime will be the whole process duration so Disposing won't be necessary.
            //However if this won't be the case everything should be disposed to free the Semaphore's handles.
            //We don't need to create a finalizer (and ofc no need to suppress it) because the unmanaged resource is inside the SemaphoreSlim.
            _waitingSem.Dispose();
        }
        
    }
}
