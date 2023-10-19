using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace XnetInternals.Impls.Protocols
{
    internal class PingPong : Xnet.BasicPacketProvider<PingPong>, Xnet.ConnectionExtender, Xnet.PacketExtender
    {
        /// <summary>
        /// Singleton.
        /// </summary>
        public static readonly PingPong Instance = new();

        // --
        private readonly HashSet<State> m_States = new();
        private CancellationTokenSource m_TokenSource;
        
        /// <summary>
        /// Initialize a new <see cref="PingPong"/> instance.
        /// This is for hiding the default constructor.
        /// </summary>
        private PingPong()
        {
        }

        /// <summary>
        /// State.
        /// </summary>
        private class State
        {
            private static readonly object KEY = new();

            /// <summary>
            /// Get the state from Xnet instance.
            /// </summary>
            /// <param name="Xnet"></param>
            /// <returns></returns>
            public static State Get(Xnet Xnet)
            {
                if (Xnet.Items.TryGetValue(KEY, out var Temp))
                    return Temp as State;

                Xnet.Items.TryAdd(KEY, new State(Xnet));
                return Get(Xnet);
            }

            // --
            private DateTime m_LastPongTime;
            private DateTime m_LastEmitTime;
            private int m_Counter;

            /// <summary>
            /// Initialize a new <see cref="State"/> instance.
            /// </summary>
            /// <param name="Xnet"></param>
            public State(Xnet Xnet)
            {
                m_LastPongTime = DateTime.Now;
                m_LastEmitTime = DateTime.Now;
                this.Xnet = Xnet;
            }

            /// <summary>
            /// Xnet instance.
            /// </summary>
            public Xnet Xnet { get; }

            /// <summary>
            /// Update state asynchronously.
            /// </summary>
            /// <returns></returns>
            public async Task UpdateAsync()
            {
                // --> emit `ping` packet if no packets received in 5 seconds.
                var Now = DateTime.Now;
                var Terminate = false;
                lock (this)
                {   
                    if ((Now - m_LastPongTime).TotalSeconds < 5)
                        return;

                    // --> and the `ping` packet should have delay a second at least.
                    var Term = Now - m_LastEmitTime;
                    if (Term.TotalSeconds < 1)
                        return;

                    // --> terminate the connection if no packets received in 5 times.
                    if (m_Counter < 5)
                        m_Counter++;

                    else
                        Terminate = true;

                    m_LastEmitTime = DateTime.Now;
                }

                if (Terminate)
                {
                    Xnet.Dispose();
                    return;
                }

                await Xnet.EmitAsync(new PKT_PING(), Xnet.Closing);
            }

            /// <summary>
            /// Called when any packet received.
            /// </summary>
            public void DelayEmitter()
            {
                lock(this)
                {
                    m_LastPongTime = DateTime.Now;
                    m_LastEmitTime = DateTime.Now;
                    m_Counter = 0;
                }
            }
        }

        /// <inheritdoc/>
        protected override void MapTypes()
        {
            Map<PKT_PING>("xnet.ping");
            Map<PKT_PONG>("xnet.pong");
        }

        /// <inheritdoc/>
        public ValueTask OnConnectedAsync(Xnet Connection)
        {
            CancellationToken? Token = null;
            lock (m_States)
            {
                m_States.Add(State.Get(Connection));

                if (m_TokenSource is null)
                {
                    m_TokenSource = new CancellationTokenSource();
                    Token = m_TokenSource.Token;
                }
            }

            if (Token.HasValue)
                _ = RunLoopAsync(Token.Value);

            return ValueTask.CompletedTask;
        }

        /// <inheritdoc/>
        public ValueTask OnDisconnectedAsync(Xnet Connection)
        {
            CancellationTokenSource TokenSource = null;
            lock (m_States)
            {
                m_States.Remove(State.Get(Connection));
                if (m_States.Count <= 0)
                {
                    TokenSource = m_TokenSource;
                    m_TokenSource = null;
                }
            }

            TokenSource?.Cancel();
            TokenSource?.Dispose();

            return ValueTask.CompletedTask;
        }

        /// <summary>
        /// Run the connection ping loop.
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>
        private async Task RunLoopAsync(CancellationToken Token)
        {
            while (Token.IsCancellationRequested == false)
            {
                State[] States = null;
                
                lock (m_States)
                    States = m_States.ToArray();

                foreach(var State in States)
                {
                    if (Token.IsCancellationRequested)
                        break;

                    await State.UpdateAsync();
                }

                // --> update states in every 5 seconds.
                try { await Task.Delay(5 * 1000, Token); }
                catch
                {
                }
            }
        }

        /// <inheritdoc/>
        public Task ExecuteAsync(Xnet Connection, Xnet.Packet Packet, Func<Task> Next)
        {
            /* delay the `ping` emitter if any packet received. */
            State.Get(Connection).DelayEmitter();
            switch(Packet)
            {
                /* emit `pong` packet if `ping` packet received. */
                case PKT_PING: return Connection.EmitAsync(new PKT_PONG(), Connection.Closing);

                /* ignore `pong` packet. */
                case PKT_PONG: return Task.CompletedTask;
                default: return Next.Invoke();
            }
        }

        /// <summary>
        /// Ping packet.
        /// </summary>
        private class PKT_PING : Xnet.BasicPacket
        {
            /// <inheritdoc/>
            protected override void Encode(BinaryWriter Writer)
            {
            }

            /// <inheritdoc/>
            protected override void Decode(BinaryReader Reader)
            {
            }

            /// <inheritdoc/>
            public override Task ExecuteAsync(Xnet Connection) => Task.CompletedTask;

        }

        /// <summary>
        /// Pong packet.
        /// </summary>
        private class PKT_PONG : Xnet.BasicPacket
        {
            /// <inheritdoc/>
            protected override void Encode(BinaryWriter Writer)
            {
            }

            /// <inheritdoc/>
            protected override void Decode(BinaryReader Reader)
            {
            }

            /// <inheritdoc/>
            public override Task ExecuteAsync(Xnet Connection) => Task.CompletedTask;
        }
    }
}
