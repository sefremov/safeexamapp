using System;
using System.Collections.Generic;
using System.Text;

namespace SafeExamApp {
    class RepeatedTimer {

        private DateTime prev;
        private int interval;
        private bool active;

        public event Action Elapsed;

        public RepeatedTimer(int interval) {
            this.interval = interval;
        }

        public void Start() {
            prev = DateTime.Now;
            active = true;
        }

        public void Stop() {
            active = false;
        }

        public void Poll() {
            if(!active)
                return;

            if((DateTime.Now - prev).TotalMilliseconds > interval) {
                prev = DateTime.Now;
                Elapsed?.Invoke();
            }
        }
            
    }
}
