using System;

namespace SafeExamApp {

    public class RepeatedTimer {
        private DateTime prev;
        private bool active;
        public event Action Elapsed;

        protected int interval;

        public void Start() {
            prev = DateTime.Now;
            active = true;
        }

        public void Stop() {
            active = false;
        }

        public RepeatedTimer(int interval) {
            this.interval = interval;
        }

        protected virtual void AfterElapsed() {

        }

        public void Poll() {
            if(!active)
                return;

            if((DateTime.Now - prev).TotalMilliseconds > interval) {
                prev = DateTime.Now;
                Elapsed?.Invoke();
                AfterElapsed();
            }
        }
    }

    class RepeatedRandomTimer : RepeatedTimer {
        private readonly int minInterval;
        private readonly int maxInterval;
        private readonly Random random;

        public RepeatedRandomTimer(int minInterval, int maxInterval) : base(minInterval) {
            this.minInterval = minInterval;
            this.maxInterval = maxInterval;
            random = new Random();
        }

        protected override void AfterElapsed() {
            interval = random.Next(minInterval, maxInterval);
        }
    }
}
