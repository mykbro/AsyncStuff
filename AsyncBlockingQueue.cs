using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncStuff
{
    public class AsyncBlockingQueue<T> : IEnumerable<T>, IDisposable
    {
        private readonly Queue<T> _queue;
        //private readonly AsyncMonitor _monitor;
        private readonly SpinLock _queueLock;
        private readonly AsyncConditionVariable _queueNotEmpty;
        private bool _productionStopped;
        private int _count;
        private int _maxSize;

        public AsyncBlockingQueue()
        {
            _queue = new Queue<T>();
            _queueLock = new SpinLock();
            _queueNotEmpty = new AsyncConditionVariable();
            //_monitor = new AsyncMonitor();
            _count = 0;
            _maxSize = 0;
            _productionStopped = false;
        }

        public int Count
        {
            get
            {
                //_monitor.Enter();           //do we really need to take the monitor here ??
                int toReturn = _count;
                //_monitor.Exit();
                return toReturn;
            }
        }

        public bool ProductionStopped
        {
            get
            {
                //we don't need the monitor here either
                return _productionStopped;
            }
        }


        public void Clear()
        {
            _queueLock.Wait();
            _queue.Clear();
            _count = 0;
            _queueLock.Release();
        }

        public bool Contains(T item)
        {
            _queueLock.Wait();
            bool toReturn = _queue.Contains(item);
            _queueLock.Release();

            return toReturn;
        }

        public T Dequeue()
        {
            T toReturn;

            _queueLock.Wait();
            try
            {
                toReturn = _queue.Dequeue();
                _count--;
            }
            finally
            {
                _queueLock.Release();
            }
            return toReturn;
        }

        public T DequeueWhenAvailable()
        {
            T toReturn;

            _queueLock.Wait();
            while (_count == 0 && !_productionStopped)
            {   //we wait until count > 0 || production_stopped 
                _queueNotEmpty.Wait(_queueLock);
            }

            if (_count > 0)                              //if count > 0 all good even if production_stopped
            {
                toReturn = _queue.Dequeue();
                _count--;
                _queueLock.Release();
            }
            else                                        //else we're here because production_stopped && count == 0
            {
                _queueLock.Release();
                throw new InvalidOperationException();
            }

            return toReturn;
        }
        
        public async Task<T> DequeueWhenAvailableAsync()
        {
            T toReturn;

            _queueLock.Wait();
            while (_count == 0 && !_productionStopped)
            {   //we wait until count > 0 || production_stopped 
                await _queueNotEmpty.WaitAsync(_queueLock);
            }

            if (_count > 0)                              //if count > 0 we keep dequeing even if production_stopped
            {
                toReturn = _queue.Dequeue();
                _count--;
                _queueLock.Release();
            }
            else                                        //else we're here because production_stopped && count == 0
            {
                _queueLock.Release();
                throw new InvalidOperationException();
            }

            return toReturn;
        }

        public void Enqueue(T item)
        {
            _queueLock.Wait();
            _queue.Enqueue(item);
            _count++;
            if (_count > _maxSize)
                _maxSize = _count;
            //if (_count == 1)
            //_monitor.NotifyAll();            
            _queueNotEmpty.Notify();
            _queueLock.Release();
        }

        public T Peek()
        {

            T toReturn;
            _queueLock.Wait();
            try
            {
                toReturn = _queue.Peek();
            }
            finally
            {
                _queueLock.Release();
            }
            return toReturn;
        }

        public T PeekWhenAvailable()
        {
            T toReturn;

            _queueLock.Wait();
            while (_count == 0 && !_productionStopped)  //we wait until count > 0 || production_stopped 
                _queueNotEmpty.Wait(_queueLock);
            if (_count > 0)                              //if count > 0 all good even if production_stopped
            {
                toReturn = _queue.Peek();
                _queueLock.Release();
            }
            else                                        //else we're here because production_stopped && count == 0
            {
                _queueLock.Release();
                throw new InvalidOperationException();
            }

            return toReturn;
        }

        public void SignalMoreItemsAreComing()
        {
            _queueLock.Wait();
            _productionStopped = false;     // no need to Notify... we SLEEP while _production_stopped = false (and count == 0) and we WAKEUP when _prod_stopped = true
            _queueLock.Release();
        }

        public void SignalNoMoreItemsAvailable()
        {
            _queueLock.Wait();
            if (!_productionStopped)
            {
                _productionStopped = true;
                _queueNotEmpty.NotifyAll();
            }
            _queueLock.Release();
        }

        public IEnumerator<T> GetEnumerator()
        {
            _queueLock.Wait();
            var toReturn = _queue.GetEnumerator();
            _queueLock.Release();

            return toReturn;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public void Dispose()
        {
            _queueNotEmpty.Dispose();
        }
    }
}
