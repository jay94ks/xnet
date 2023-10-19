using Newtonsoft.Json.Linq;

namespace XnetInternals.Impls.Packets
{
    [Xnet.BasicPacket(Name = "xnet.rpc.response", Kind = "xnet.rpc")]
    internal class RPC_RESPONSE : Xnet.BasicPacket
    {
        /// <summary>
        /// Request Id.
        /// </summary>
        public Guid ReqId { get; set; }

        /// <summary>
        /// Error or not.<br />
        /// 1. null: no such action exists.<br />
        /// 2. true: error with reason.<br />
        /// 3. false: success.
        /// </summary>
        public bool? Error { get; set; }

        /// <summary>
        /// Reason.
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// Result.
        /// </summary>
        public JObject Result { get; set; }

        /// <inheritdoc/>
        protected override void Encode(BinaryWriter Writer)
        {
            Writer.Write(ReqId.ToByteArray());
            Writer.Write(Reason ?? string.Empty);

            if (Error == null)
                Writer.Write((byte) 'N');

            else if (Error.Value)
                Writer.Write((byte) 'S');

            else
                Writer.Write((byte) 'E');


            var Bson = Result.ToBson();
            Writer.Write7BitEncodedInt(Bson.Length);
            Writer.Write(Bson);
        }

        /// <inheritdoc/>
        protected override void Decode(BinaryReader Reader)
        {
            ReqId = new Guid(Reader.ReadBytes(16));
            Reason = Reader.ReadString();

            var State = Reader.ReadByte();
            switch (State)
            {
                case (byte)'S': Error = true; break;
                case (byte)'E': Error = false; break;
                default: Error = null; break;
            }

            var Bson = Reader.Read7BitEncodedInt();
            Result = Reader.ReadBytes(Bson).ToJson();
        }

        /// <inheritdoc/>
        public override Task ExecuteAsync(Xnet Connection)
        {
            RpcExtender.Instance.OnResponse(this);
            return Task.CompletedTask;
        }
    }
}
