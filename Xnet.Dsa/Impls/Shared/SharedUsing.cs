using Secp256k1Net;
using System.Security.Cryptography;

namespace XnetDsa.Impls.Shared
{
    internal class SharedUsing
    {
        private static readonly AsyncLocal<SharedUsing> SHARED = new();
        private readonly List<object> m_Instances = new List<object>();

        /// <summary>
        /// Get the current <see cref="SharedUsing"/> instance.
        /// </summary>
        public static SharedUsing Current
        {
            get
            {
                if (SHARED.Value is null)
                    SHARED.Value = new SharedUsing();

                return SHARED.Value;
            }
        }

        /// <summary>
        /// Configure correct constructors for sharedusing objects.
        /// </summary>
        static SharedUsing()
        {
            SharedSha256.SetCtor(() => SHA256.Create());
            SharedSecp256k1.SetCtor(() => new Secp256k1());
        }

        /// <summary>
        /// Push an instance.
        /// </summary>
        /// <param name="Next"></param>
        public void Push(object Next)
        {
            m_Instances.Add(Next);
        }

        /// <summary>
        /// Pop an instance.
        /// </summary>
        /// <param name="Last"></param>
        /// <returns></returns>
        public bool Pop(object Which)
        {
            if (m_Instances.Count <= 0)
                return false;

            var Index = m_Instances.LastIndexOf(Which);
            if (Index < 0)
                return false;

            m_Instances.RemoveAt(Index);
            return true;
        }

        /// <summary>
        /// Get the last pushed instance by its type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Get<T>() where T : class
        {
            for (var i = m_Instances.Count - 1; i >= 0; --i)
            {
                if (m_Instances[i] is T Instance)
                    return Instance;
            }

            return null;
        }
    }

    internal class SharedUsing<T> : SharedUsing where T : class, IDisposable
    {
        private static readonly object SYNC = new();
        private static readonly Func<T> DEFAULT_CTOR;
        private static Func<T> CTOR = null;

        /// <summary>
        /// Initialize the static SharedUsing instance.
        /// </summary>
        static SharedUsing()
        {
            if (typeof(T).IsAbstract)
                return;

            var Ctor = typeof(T).GetConstructor(Type.EmptyTypes);
            if (Ctor != null)
                DEFAULT_CTOR = () => Ctor.Invoke(Array.Empty<object>()) as T;

            CTOR = DEFAULT_CTOR;
        }

        /// <summary>
        /// Set the constructor.
        /// </summary>
        /// <param name="Ctor"></param>
        public static void SetCtor(Func<T> Ctor)
        {
            lock (SYNC)
            {
                if (Ctor is null)
                    Ctor = DEFAULT_CTOR;

                CTOR = Ctor;
            }
        }

        /// <summary>
        /// Create a new instance of <typeparamref name="T"/>.
        /// </summary>
        /// <returns></returns>
        private static T Create()
        {
            Func<T> Ctor;
            lock (SYNC)
                Ctor = CTOR ?? DEFAULT_CTOR;

            return Ctor.Invoke();
        }

        /// <summary>
        /// Execute an action with async shared <typeparamref name="T"/> instance.
        /// </summary>
        /// <typeparam name="TReturn"></typeparam>
        /// <param name="Action"></param>
        /// <returns></returns>
        public static TReturn Exec<TReturn>(Func<T, TReturn> Action)
        {
            var Instance = Current.Get<T>();
            if (Instance != null)
                return Action.Invoke(Instance);

            using (Instance = Create())
            {
                Current.Push(Instance);

                try { return Action.Invoke(Instance); }
                finally
                {
                    Current.Pop(Instance);
                }
            }
        }

        /// <summary>
        /// Execute an action with async shared <typeparamref name="T"/> instance.
        /// </summary>
        /// <param name="Action"></param>
        /// <returns></returns>
        public static void Exec(Action<T> Action)
        {
            var Instance = Current.Get<T>();
            if (Instance != null)
            {
                Action.Invoke(Instance);
                return;
            }

            using (Instance = Create())
            {
                Current.Push(Instance);

                try { Action.Invoke(Instance); }
                finally
                {
                    Current.Pop(Instance);
                }
            }
        }
    }
}
