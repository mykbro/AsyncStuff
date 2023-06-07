using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncStuff
{
    /*      
     * Just an async version of a "Barrier" similar to a ManualResetEvent 
     * 
     */
    public class AsyncBarrier : IDisposable
    {
        private readonly AsyncMonitor _monitor;
        private bool _open;

               
        public AsyncBarrier(bool open = false)
        {           
            _monitor = new AsyncMonitor();
            _open = open;
        }

        public void TryToPass()
        {
            _monitor.Enter();
            while (!_open)
            {
                _monitor.Wait();
            }
            _monitor.Exit();
        }

        public void Open()
        {
            _monitor.Enter();
            if (!_open)
            {
                _open = true;
                _monitor.NotifyAll();
            }
            _monitor.Exit();
        }

        public void Close()
        {
            _monitor.Enter();
            _open = false;
            _monitor.Exit();
        }

        public async Task TryToPassAsync()
        {
            _monitor.Enter();  //await _monitor.EnterAsync();
            while (!_open)
            {
                await _monitor.WaitAsync();
            }
            _monitor.Exit();
        }

        public void Dispose()
        {
            //care should be taken by the user to not use a disposed object
            _monitor.Dispose();
        }

        /*
        public async Task OpenAsync()
        {
           _queueLock.Wait()   //await _monitor.EnterAsync();
            if (!_open)
            {
                _open = true;
                _monitor.NotifyAll();  //await _monitor.NotifyAllAsync();
            }
           _queueLock.Release()
        }

        public async Task CloseAsync()
        {
            await _monitor.EnterAsync();
            _open = false;
           _queueLock.Release()
        }
        */
    }
}
