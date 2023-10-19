namespace XnetDsa.Impls.Protocols
{
    /// <summary>
    /// DSA remote state.
    /// </summary>
    internal class DsaRemoteState
    {
        private static readonly object KEY = new();
        private TaskCompletionSource<bool> m_Ready = new();

        /// <summary>
        /// Get the <see cref="DsaRemoteState"/> from the current connection.
        /// </summary>
        /// <param name="Xnet"></param>
        /// <returns></returns>
        public static DsaRemoteState FromXnet(Xnet Xnet)
        {
            if (Xnet.Items.TryGetValue(KEY, out var Temp) == true)
                return Temp as DsaRemoteState;

            Xnet.Items.TryAdd(KEY, new DsaRemoteState());
            return FromXnet(Xnet);
        }

        /// <summary>
        /// DSA public key.
        /// </summary>
        public DsaPubKey PubKey { get; set; }

        /// <summary>
        /// DSA requested public key.
        /// </summary>
        public DsaPubKey RequestedPubKey { get; set; }

        /// <summary>
        /// DSA required digest.
        /// </summary>
        public DsaDigest RequiredDigest { get; set; }

        /// <summary>
        /// Called to reset ready task.
        /// </summary>
        public void Initiated()
        {
            TaskCompletionSource<bool> Previous;
            lock (this)
            {
                if ((Previous = m_Ready).Task.IsCompleted == false)
                    return;

                m_Ready = new TaskCompletionSource<bool>();
            }

            Previous?.TrySetResult(false);
        }

        /// <summary>
        /// Called to set ready.
        /// </summary>
        /// <param name="Success"></param>
        public void Completed(bool Success)
        {
            TaskCompletionSource<bool> Previous;
            lock (this) Previous = m_Ready;
            Previous?.TrySetResult(Success);
        }
    }
}
