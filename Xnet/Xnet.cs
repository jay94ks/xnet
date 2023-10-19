using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using XnetInternals.Impls;
using XnetInternals.Sockets;

/// <summary>
/// Xnet.
/// </summary>
public sealed partial class Xnet : IDisposable
{
    private static readonly AsyncLocal<Xnet> CURRENT = new();
    private readonly SocketBase m_Socket;

    /// <summary>
    /// Current <see cref="Xnet"/> instance.
    /// </summary>
    public static Xnet Current => CURRENT.Value;

    /// <summary>
    /// Set the current instance and return previous. 
    /// </summary>
    /// <param name="Xnet"></param>
    /// <returns></returns>
    private static Xnet SetCurrent(Xnet Xnet)
    {
        var Prev = CURRENT.Value;
        CURRENT.Value = Xnet;
        return Prev;
    }

    /// <summary>
    /// Initialize a new <see cref="Xnet"/> instance.
    /// </summary>
    /// <param name="Socket"></param>
    private Xnet(IServiceProvider Services, Socket Socket, CachedImpls Impls, Options Options)
    {
        if (Options.IsSslRequired)
            m_Socket = new SocketTcpSsl(Socket, Options.OnSslAuthentication);

        else
            m_Socket = new SocketTcp(Socket);

        this.Impls = Impls;
        this.Services = Services;

        NetworkId = Options.NetworkId;
        IsServerMode = Options is ServerOptions;
        LocalEndpoint = Socket.LocalEndPoint as IPEndPoint;
        RemoteEndpoint = Socket.RemoteEndPoint as IPEndPoint;

        Connections = GetExtender<Collection>();
    }

    /// <summary>
    /// Cached implementations.
    /// </summary>
    private CachedImpls Impls { get; }

    /// <summary>
    /// Network Id.
    /// </summary>
    public Guid NetworkId { get; }

    /// <summary>
    /// Indicates whether the underlying connection is server mode or not.
    /// </summary>
    public bool IsServerMode { get; }

    /// <summary>
    /// Service provider.
    /// </summary>
    public IServiceProvider Services { get; }

    /// <summary>
    /// Connections.
    /// </summary>
    public Collection Connections { get; }

    /// <summary>
    /// Indicates whether the underlying connection is alive or not.
    /// </summary>
    public bool IsAlive => Closing.IsCancellationRequested == false;

    /// <summary>
    /// Triggered when the underlying connection is closing.
    /// </summary>
    public CancellationToken Closing => m_Socket.Closing;

    /// <summary>
    /// Local endpoint.
    /// </summary>
    public IPEndPoint LocalEndpoint { get; }

    /// <summary>
    /// Remote endpoint.
    /// </summary>
    public IPEndPoint RemoteEndpoint { get; }

    /// <summary>
    /// Items that shared between packets.
    /// </summary>
    public ConcurrentDictionary<object, object> Items { get; } = new();

    /// <inheritdoc/>
    public void Dispose() => m_Socket.Dispose();

    /// <summary>
    /// Run the connection loop.
    /// </summary>
    /// <param name="Services"></param>
    /// <param name="Socket"></param>
    /// <param name="Impls"></param>
    /// <param name="Options"></param>
    /// <param name="Token"></param>
    /// <returns></returns>
    private static async Task RunAsync(
        IServiceProvider Services, Socket Socket, CachedImpls Impls,
        Options Options, CancellationToken Token = default)
    {
        using var Scope = Services.CreateScope();
        using var Xnet = new Xnet(Scope.ServiceProvider, Socket, Impls, Options);
        var Prev = SetCurrent(Xnet);

        try
        {
            using (Token.Register(Xnet.Dispose, false))
                await Xnet.RunAsync();
        }

        finally
        {
            SetCurrent(Prev);
        }
    }

    /// <summary>
    /// Run the connection loop.
    /// </summary>
    /// <returns></returns>
    private async Task RunAsync()
    {
        if (await CheckNetworkIdAsync() == false)
        {
            Dispose();
            return;
        }

        try
        {
            await OnConnectedAsync();
            while (Closing.IsCancellationRequested == false)
            {
                var Lenb = await m_Socket.ReceiveAsync(sizeof(ushort));
                if (Lenb.Length <= 0)
                    break;

                var Len = Lenb[0] | (Lenb[1] << 8);
                if (Len <= 0)
                    continue;

                var Data = await m_Socket.ReceiveAsync(Len);
                if (Data.Length < 16)
                    break;

                var Packet = DecodeFrameToPacket(Data);
                if (Packet is null)
                    continue;

                await Impls.ExecuteWithPacketExtenders(this, Packet);
            }
        }

        finally
        {
            Dispose();
            await OnDisconnectedAsync();
        }
    }

    /// <summary>
    /// Decode frame to the packet.
    /// </summary>
    /// <param name="Frame"></param>
    /// <returns></returns>
    private Packet DecodeFrameToPacket(byte[] Frame)
    {
        using var Stream = new MemoryStream(Frame, false);
        using (var Reader = new BinaryReader(Stream, Encoding.UTF8, true))
            return Decode(Reader);
    }

    /// <summary>
    /// Check the network id asynchronously.
    /// </summary>
    /// <returns></returns>
    private async Task<bool> CheckNetworkIdAsync()
    {
        if (IsServerMode)
        {
            var Temp = await m_Socket.ReceiveAsync(16);
            if (Temp.Length != 16 || new Guid(Temp) != NetworkId)
                return false;

            return true;
        }

        return await m_Socket.SendAsync(NetworkId.ToByteArray());
    }

    /// <summary>
    /// Emit a packet to the remote host asynchronously.
    /// </summary>
    /// <param name="Packet"></param>
    /// <param name="Token"></param>
    /// <returns></returns>
    public async Task<bool> EmitAsync(Packet Packet, CancellationToken Token = default)
    {
        var Prev = SetCurrent(this);
        try
        {
            var Frame = EncodeFrameFromPacket(Packet);
            if (Frame is null)
                return false;

            return await m_Socket.SendAsync(Frame, Token);
        }

        finally
        {
            SetCurrent(Prev);
        }
    }

    /// <summary>
    /// Broadcast a packet to all connections except this connection asynchronously.
    /// </summary>
    /// <param name="Packet"></param>
    /// <param name="Token"></param>
    /// <returns></returns>
    public async Task<int> BroadcastAsync(Packet Packet, CancellationToken Token = default)
    {
        var Conns = Connections.FindAll(X => X != this);
        var Counter = 0;

        foreach (var Conn in Conns)
        {
            if (Token.IsCancellationRequested)
                break;

            if (await Conn.EmitAsync(Packet, Token))
                Counter++;
        }

        return Counter;
    }

    /// <summary>
    /// Encode frame from the packet.
    /// </summary>
    /// <param name="Packet"></param>
    /// <returns></returns>

    private byte[] EncodeFrameFromPacket(Packet Packet)
    {
        var Success = false;

        using var Stream = new MemoryStream();
        using (var Writer = new BinaryWriter(Stream, Encoding.UTF8, true))
            Success = Encode(Packet, Writer);

        if (Success)
        {
            var Frame = Stream.ToArray();
            var Length = Frame.Length;

            Array.Resize(ref Frame, Frame.Length + sizeof(ushort));
            Array.Copy(Frame, 0, Frame, sizeof(ushort), Length);

            Frame[0] = (byte)(Length & 0xff);
            Frame[1] = (byte)(Length >> 8);
            return Frame;
        }

        return null;
    }

    /// <summary>
    /// Called at begining of <see cref="RunAsync"/> method.
    /// </summary>
    /// <returns></returns>
    private async Task OnConnectedAsync()
    {
        if (Impls.BeforeConnectionLoop != null)
            await Impls.BeforeConnectionLoop(this);

        foreach (var Each in Impls.ConnectionExtenders)
            await Each.OnConnectedAsync(this);
    }

    /// <summary>
    /// Called at ending of <see cref="RunAsync"/> method.
    /// </summary>
    /// <returns></returns>
    private async Task OnDisconnectedAsync()
    {
        foreach (var Each in Impls.ConnectionExtenders)
            await Each.OnDisconnectedAsync(this);

        if (Impls.AfterConnectionLoop != null)
            await Impls.AfterConnectionLoop(this);
    }

}