using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncStuff
{
    public class SpinSemaphore
    {
        private int _count;

        public SpinSemaphore(int initialCount)
        {
            if (initialCount < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCount));

            _count = initialCount;
        }

        public int CurrentCount
        {
            get { return _count; }
        }

        public void Wait()
        {
            bool decrementSuccesful = false;

            while (!decrementSuccesful)
            {
                //we "freeze" the value of _count in this variable
                int countWhenEntering = _count;
                
                //if the fixed value was > 0  and _count did not change in the meantime we decrement _count and exit the loop
                decrementSuccesful = (countWhenEntering > 0) && Interlocked.CompareExchange(ref _count, countWhenEntering - 1, countWhenEntering) == countWhenEntering;
                
                /* commented out to prevent context switches
                 * 
                    //if we failed we yield before retrying
                    if (!decrementSuccesful)
                        Thread.Sleep(0);
                */
            }
        }

        public void Release(int howMuch = 1)
        {
            if (howMuch <= 0)
                throw new ArgumentOutOfRangeException(nameof(howMuch));

            Interlocked.Add(ref _count, howMuch);
        }
    }
}
