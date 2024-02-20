public sealed partial class Xnet
{
    /// <summary>
    /// Statistics counter.
    /// </summary>
    public interface StatisticsCounter
    {
        /// <summary>
        /// RX bytes per seconds.
        /// </summary>
        double Rxbps { get; }

        /// <summary>
        /// TX bytes per seconds.
        /// </summary>
        double Txbps { get; }

        /// <summary>
        /// Accumulate RX and TX bytes.
        /// </summary>
        /// <param name="Rxbytes"></param>
        /// <param name="Txbytes"></param>
        void Accumulate(double? Rxbytes, double? Txbytes);

        /// <summary>
        /// Capture current value in single call.
        /// </summary>
        /// <param name="Txbps"></param>
        /// <param name="Rxbps"></param>
        void Capture(out double Txbps, out double Rxbps);
    }

    /// <summary>
    /// Default implementation of counter.
    /// </summary>
    private class DefaultStatisticsCounter : StatisticsCounter
    {
        private DateTime m_Start = DateTime.Now;
        private double m_Tx = 0, m_Rx = 0;

        /// <inheritdoc/>
        public double Txbps => GetValue(ref m_Tx);

        /// <inheritdoc/>
        public double Rxbps => GetValue(ref m_Rx);

        /// <summary>
        /// Get the speed value.
        /// </summary>
        /// <param name="Field"></param>
        /// <returns></returns>
        private double GetValue(ref double Field)
        {
            DateTime Start;
            double Value = 0.0;
            lock (this)
            {
                Start = m_Start;
                Value = Field;
            }

            var Sec = (DateTime.Now - Start).TotalSeconds;
            if (Sec <= 0)
                return 0;

            return Value / Sec;
        }

        /// <inheritdoc/>
        public void Accumulate(double? Tx = null, double? Rx = null)
        {
            if (!Tx.HasValue && !Rx.HasValue)
                return;

            var Now = DateTime.Now;
            lock (this)
            {
                var Diff = Now - m_Start;
                if (Diff.TotalSeconds >= 1)
                    m_Tx = m_Rx = 0;

                if (Tx.HasValue)
                    m_Tx += Tx.Value;

                if (Rx.HasValue)
                    m_Rx += Rx.Value;

                m_Start = Now;
            }
        }

        /// <inheritdoc/>
        public void Capture(out double Txbps, out double Rxbps)
        {
            DateTime Start;

            lock (this)
            {
                Start = m_Start;
                Txbps = m_Tx;
                Rxbps = m_Rx;
            }

            var Sec = (DateTime.Now - Start).TotalSeconds;
            if (Sec <= 0)
            {
                Txbps = Rxbps = 0;
                return;
            }

            Txbps /= Sec;
            Rxbps /= Sec;
        }
    }
}