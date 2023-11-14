namespace XnetStreams
{
    /// <summary>
    /// Stream delegate helpers.
    /// </summary>
    public static class StreamDelegateHelpers
    {
        private class ConcatOperator
        {
            private readonly StreamDelegate m_Prev;
            private readonly StreamDelegate m_Next;

            /// <summary>
            /// Initialize a new <see cref="ConcatOperator"/> instance.
            /// </summary>
            /// <param name="Prev"></param>
            /// <param name="Next"></param>
            public ConcatOperator(StreamDelegate Prev, StreamDelegate Next)
            {
                m_Prev = Prev;
                m_Next = Next;
            }

            /// <summary>
            /// Invoke the delegate.
            /// </summary>
            /// <param name="Context"></param>
            /// <param name="Next"></param>
            /// <returns></returns>
            public Task InvokeAsync(StreamContext Context, Func<Task> Next)
            {
                Task NextAsync()
                {
                    return m_Next.Invoke(Context, Next);
                }

                return m_Prev.Invoke(Context, NextAsync);
            }
        }

        /// <summary>
        /// Concat all delegates into single delegate.
        /// </summary>
        /// <param name="Delegate"></param>
        /// <param name="Postpends"></param>
        /// <returns></returns>
        public static StreamDelegate Concat(this StreamDelegate Delegate, params StreamDelegate[] Postpends)
        {
            foreach (var Each in Postpends)
            {
                if (Each is null)
                    continue;

                if (Delegate is null)
                {
                    Delegate = Each;
                    continue;
                }

                Delegate = new ConcatOperator(Delegate, Each).InvokeAsync;
            }

            return Delegate;
        }
    }
}
