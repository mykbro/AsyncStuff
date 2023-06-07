using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncStuff
{
    /*
     * Similar to the .NET class but without the possible context switch after a single spin. Will spin for the whole timeslice.
     */    

    public class SpinLock
    {
        private int _available;

        public SpinLock()
        {
            _available = 1;
        }

        public int CurrentCount
        {
            get { return _available; }
        }

        public void Wait()
        {
            //we wait while _available was 0 before the exchange
            //only one thread will see a 1 when available and after the evaluation will be set atomically to 0

            while (Interlocked.CompareExchange(ref _available, 0, 1) == 0)
            {
                //Thread.Sleep(0);      //using this here will yield to other equal priority READY threads but will cause more context switches
            }
        }

        public void Release()
        {
            //we could avoid the 0 check and always set 1 with .Exchange
            //we probably could also avoid the atomic operation but we would need to make _available volatile 
            Interlocked.CompareExchange(ref _available, 1, 0);
        }
    }
}
