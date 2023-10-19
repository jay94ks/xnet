using System.Security.Cryptography;
using System.Text;

public sealed partial class Xnet
{
    /// <summary>
    /// Make Id from the specified name.
    /// </summary>
    /// <param name="Name"></param>
    /// <returns></returns>
    private static Guid MakePacketId(string Name)
    {
        using var Md = MD5.Create();
        var Temp = Encoding.UTF8.GetBytes(
            $"xnet.packet: {Name ?? string.Empty}.");

        return new Guid(Md.ComputeHash(Temp));
    }

    /// <summary>
    /// Packet interface.
    /// </summary>
    public interface Packet
    {
        /// <summary>
        /// Execute the packet asynchronously.
        /// </summary>
        /// <param name="Connection"></param>
        /// <returns></returns>
        Task ExecuteAsync(Xnet Connection);
    }

    /// <summary>
    /// Basic Packet.
    /// </summary>
    public abstract class BasicPacket : Packet
    {
        /// <inheritdoc/>
        public abstract Task ExecuteAsync(Xnet Connection);

        /// <summary>
        /// Encode the <see cref="BasicPacket"/> into <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="Writer"></param>
        protected abstract void Encode(BinaryWriter Writer);

        /// <summary>
        /// Decode the <see cref="BasicPacket"/> from <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="Reader"></param>
        protected abstract void Decode(BinaryReader Reader);

        // --
        internal void CallSerialize(BinaryWriter Writer) => Encode(Writer);
        internal void CallDeserialize(BinaryReader Reader) => Decode(Reader);
    }

    /// <summary>
    /// Basic Packet attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class BasicPacketAttribute : Attribute
    {
        /// <summary>
        /// for generating type id by hashing the its name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Kind name to filter packet types.
        /// </summary>
        public string Kind { get; set; }
    }
}