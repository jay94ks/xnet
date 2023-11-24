using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XnetStreams.Internals.Packets
{
    /// <summary>
    /// Stream Open request.
    /// </summary>
    internal class PKT_OPEN : Xnet.BasicPacket
    {
        /// <summary>
        /// Trace Id.
        /// </summary>
        public Guid TraceId { get; set; }

        /// <summary>
        /// Opening timeout.
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// Read timeout if required.
        /// </summary>
        public int ReadTimeout { get; set; } = -1;

        /// <summary>
        /// Read timeout if required.
        /// </summary>
        public int WriteTimeout { get; set; } = -1;

        /// <summary>
        /// Path to the resource.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Open Mode.
        /// </summary>
        public FileMode Mode { get; set; }
        
        /// <summary>
        /// Access Mode.
        /// </summary>
        public FileAccess Access { get; set; }

        /// <summary>
        /// Sharing Mode.
        /// </summary>
        public FileShare Share { get; set; }

        /// <summary>
        /// Extras to pass if required.
        /// </summary>
        public JObject Extras { get; set; }

        /// <inheritdoc/>
        public override Task ExecuteAsync(Xnet Connection)
            => StreamExtender.Get(Connection).OpenAsync(Connection, this);

        /// <inheritdoc/>
        protected override void Decode(BinaryReader Reader)
        {
            TraceId = new Guid(Reader.ReadBytes(16));
            Timeout = Reader.Read7BitEncodedInt();
            ReadTimeout = Reader.Read7BitEncodedInt();
            WriteTimeout = Reader.Read7BitEncodedInt();
            Path = Reader.ReadString();

            Mode = (FileMode)Reader.ReadByte();
            Access = (FileAccess)Reader.ReadByte();
            Share = (FileShare)Reader.ReadByte();

            Extras = DecodeOptions(Reader);
        }

        /// <inheritdoc/>
        protected override void Encode(BinaryWriter Writer)
        {
            Writer.Write(TraceId.ToByteArray());
            Writer.Write7BitEncodedInt(Timeout);
            Writer.Write7BitEncodedInt(ReadTimeout);
            Writer.Write7BitEncodedInt(WriteTimeout);
            Writer.Write(Path ?? string.Empty);

            Writer.Write((byte)Mode);
            Writer.Write((byte)Access);
            Writer.Write((byte)Share);

            if (Extras is null)
            {
                Writer.Write7BitEncodedInt(0);
                return;
            }

            var Bson = EncodeOptions(Extras);

            Writer.Write7BitEncodedInt(Bson.Length);
            Writer.Write(Bson);
        }

        /// <summary>
        /// Encode the options object.
        /// </summary>
        /// <returns></returns>
        internal static byte[] EncodeOptions(JObject Extras)
        {
            using var Stream = new MemoryStream();
            using (var Bson = new BsonDataWriter(Stream))
            {
                JsonSerializer
                    .CreateDefault()
                    .Serialize(Bson, Extras);
            }

            return Stream.ToArray();
        }

        /// <summary>
        /// Decode the options object.
        /// </summary>
        /// <param name="Reader"></param>
        /// <returns></returns>
        internal static JObject DecodeOptions(BinaryReader Reader)
        {
            var Len = Reader.Read7BitEncodedInt();
            if (Len <= 0)
                return null;

            using var Stream = new MemoryStream(Reader.ReadBytes(Len), false);
            using (var Bson = new BsonDataReader(Stream))
            {
                return JsonSerializer
                    .CreateDefault()
                    .Deserialize<JObject>(Bson);
            }
        }

    }
}
