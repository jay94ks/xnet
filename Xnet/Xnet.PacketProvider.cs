using System.Diagnostics;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Text;
using static Xnet;

public sealed partial class Xnet
{
    /// <summary>
    /// Packet provider interface.
    /// </summary>
    public interface PacketProvider
    {
        /// <summary>
        /// Get the packet's type id.
        /// If <see cref="Guid.Empty"/> returned, it means that not supported.
        /// </summary>
        /// <param name="Packet"></param>
        /// <returns></returns>
        Guid GetTypeId(Packet Packet);

        /// <summary>
        /// Encode the packet from the binary reader.
        /// </summary>
        /// <param name="Packet"></param>
        /// <param name="Writer"></param>
        /// <returns></returns>
        bool Encode(Packet Packet, BinaryWriter Writer);

        /// <summary>
        /// Decode the packet from the binary reader.
        /// </summary>
        /// <param name="TypeId"></param>
        /// <param name="Reader"></param>
        /// <returns></returns>
        Packet Decode(Guid TypeId, BinaryReader Reader);
    }

    /// <summary>
    /// Encode a packet to the binary writer.
    /// </summary>
    /// <param name="Packet"></param>
    /// <param name="Writer"></param>
    /// <returns></returns>
    public bool Encode(Packet Packet, BinaryWriter Writer)
    {
        foreach (var Each in Impls.PacketProviders)
        {
            var TypeId = Each.GetTypeId(Packet);
            if (TypeId == Guid.Empty)
                continue;

            Writer.Write(TypeId.ToByteArray());
            return Each.Encode(Packet, Writer);
        }

        return false;
    }

    /// <summary>
    /// Decode a packet from the binary reader.
    /// </summary>
    /// <param name="Reader"></param>
    /// <returns></returns>
    public Packet Decode(BinaryReader Reader)
    {
        var Packet = null as Packet;
        var TypeId = new Guid(Reader.ReadBytes(16));
        foreach (var Each in Impls.PacketProviders)
        {
            if ((Packet = Each.Decode(TypeId, Reader)) is null)
                continue;

            break;
        }

        return Packet;
    }

    /// <summary>
    /// Basic packet provider.
    /// </summary>
    public abstract class BasicPacketProvider<TSelf> : PacketProvider where TSelf : BasicPacketProvider<TSelf>
    {
        private readonly Dictionary<Guid, Func<BasicPacket>> m_Ctors = new();
        private readonly Dictionary<Type, Guid> m_Guids = new();

        /// <summary>
        /// Initialize a new <see cref="BasicPacketProvider{TSelf}"/> instance.
        /// </summary>
        public BasicPacketProvider() => MapTypes();

        /// <summary>
        /// Called to map packet types.
        /// </summary>
        protected abstract void MapTypes();

        /// <summary>
        /// Map the specified packet type.
        /// And generate type id by hashing the its name.
        /// </summary>
        /// <typeparam name="TPacket"></typeparam>
        /// <returns></returns>
        protected TSelf Map<TPacket>(string Name = null) where TPacket : BasicPacket, new()
        {
            if (string.IsNullOrWhiteSpace(Name))
                Name = typeof(TPacket).Name;

            var TypeId = MakePacketId(Name);

            m_Ctors[TypeId] = () => new TPacket();
            m_Guids[typeof(TPacket)] = TypeId;
            return this as TSelf;
        }

        /// <summary>
        /// Map packet types that scanned from <paramref name="Assembly"/>.
        /// </summary>
        /// <param name="Assembly"></param>
        /// <param name="Kinds"></param>
        /// <returns></returns>
        protected TSelf MapFrom(Assembly Assembly, params string[] Kinds)
        {
            MapFromInternal(Assembly.GetTypes(), Kinds);
            return this as TSelf;
        }

        /// <summary>
        /// Map packet types.
        /// </summary>
        /// <param name="Types"></param>
        /// <returns></returns>
        protected TSelf MapFrom(params Type[] Types)
        {
            MapFromInternal(Types, Array.Empty<string>());
            return this as TSelf;
        }

        /// <summary>
        /// Map packet types.
        /// </summary>
        /// <param name="Types"></param>
        /// <param name="Kinds"></param>
        private void MapFromInternal(IEnumerable<Type> Types, string[] Kinds)
        {
            foreach (var Type in Types)
            {
                if (Type.IsAbstract || Type.IsAssignableTo(typeof(BasicPacket)) == false)
                    continue;

                var Attr = Type.GetCustomAttribute<BasicPacketAttribute>();
                if (Attr is null)
                    continue;

                var Name = Type.Name;
                if (string.IsNullOrWhiteSpace(Attr.Name) == false)
                    Name = Attr.Name;

                var Ctor = Type.GetConstructor(Type.EmptyTypes);
                if (Ctor is null)
                {
                    continue;
                }

                if (Kinds is null || Kinds.Length <= 0 || Kinds.Contains(Attr.Kind ?? string.Empty))
                {
                    var TypeId = MakePacketId(Name);
                    m_Ctors[TypeId] = () => Ctor.Invoke(Array.Empty<object>()) as BasicPacket;
                    m_Guids[Type] = TypeId;
                }
            }
        }

        /// <inheritdoc/>
        public Guid GetTypeId(Packet Packet)
        {
            m_Guids.TryGetValue(Packet.GetType(), out var TypeId);
            return TypeId;
        }

        /// <inheritdoc/>
        public Packet Decode(Guid TypeId, BinaryReader Reader)
        {
            if (m_Ctors.TryGetValue(TypeId, out var Ctor) == false || Ctor is null)
                return null;

            var Packet = Ctor.Invoke();
            if (Debugger.IsAttached)
                Packet.CallDeserialize(Reader);

            else
            {
                try { Packet.CallDeserialize(Reader); }
                catch
                {
                    return null;
                }
            }

            return Packet;
        }

        /// <inheritdoc/>
        public bool Encode(Packet Packet, BinaryWriter Writer)
        {
            if (Packet is not BasicPacket Basic)
                return false;

            if (Debugger.IsAttached)
                Basic.CallSerialize(Writer);

            else
            {
                try
                {
                    Basic.CallSerialize(Writer);
                    return true;
                }

                catch { }
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Basic packet provider impl.
    /// </summary>
    internal class BasicPacketProviderImpl : BasicPacketProvider<BasicPacketProviderImpl>
    {
        private readonly Assembly m_Assembly;
        private readonly string[] m_Kinds;

        /// <summary>
        /// Initialize a new <see cref="BasicPacketProviderImpl"/> instance.
        /// </summary>
        /// <param name="Assembly"></param>
        /// <param name="Kinds"></param>
        public BasicPacketProviderImpl(Assembly Assembly, string[] Kinds)
        {
            m_Assembly = Assembly;
            m_Kinds = Kinds;
        }

        /// <inheritdoc/>
        protected override void MapTypes() => MapFrom(m_Assembly, m_Kinds);
    }
}