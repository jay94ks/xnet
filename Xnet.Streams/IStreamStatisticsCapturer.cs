using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XnetStreams
{
    /// <summary>
    /// Streaming statistics capturer.
    /// </summary>
    public interface IStreamStatisticsCapturer : Xnet.Extender
    {
        /// <summary>
        /// Rx Kbps.
        /// </summary>
        double RxKbps { get; }

        /// <summary>
        /// Tx Kbps.
        /// </summary>
        double TxKbps { get; }

        /// <summary>
        /// Push size of RX bytes.
        /// </summary>
        /// <param name="Xnet"></param>
        /// <param name="RxKbytes"></param>
        void PushRx(Xnet Xnet, double RxKbytes);

        /// <summary>
        /// Push size of TX bytes.
        /// </summary>
        /// <param name="Xnet"></param>
        /// <param name="TxKbytes"></param>
        void PushTx(Xnet Xnet, double TxKbytes);
    }
}
