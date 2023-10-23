using System.Security.Cryptography;

namespace XnetBuckets.Impls.Packets
{
    /// <summary>
    /// Bucket item.
    /// </summary>
    [Xnet.BasicPacket(Name = "xnet.bucket.item", Kind = "xnet.bucket")]
    internal class BKT_ITEM : Xnet.BasicPacket
    {
        /// <summary>
        /// Make an bucket item's id.
        /// </summary>
        /// <param name="Item"></param>
        /// <returns></returns>
        public static Guid MakeItemId(IBucketItem Item)
        {
            using var Md = MD5.Create();
            using var Stream = new MemoryStream();
            using(var Writer = new BinaryWriter(Stream))
                Item.Serialize(Writer);

            Stream.Position = 0;
            return new Guid(Md.ComputeHash(Stream));
        }

        /// <summary>
        /// Make an bucket item's id.
        /// </summary>
        /// <param name="Data"></param>
        /// <returns></returns>
        public static Guid MakeItemId(ArraySegment<byte> Data)
        {
            if (Data.Array is null)
                Data = Array.Empty<byte>();

            using var Md = MD5.Create();
            return new Guid(Md.ComputeHash(
                Data.Array, Data.Offset, Data.Count));
        }

        /// <summary>
        /// Bucket Id.
        /// </summary>
        public Guid BucketId { get; set; }

        /// <summary>
        /// Item Id.
        /// </summary>
        public Guid ItemId { get; set; }
        
        /// <summary>
        /// Time to live.
        /// </summary>
        public int Ttl { get; set; }

        /// <summary>
        /// Bucket Data.
        /// </summary>
        public byte[] Data { get; set; }

        /// <inheritdoc/>
        public override Task ExecuteAsync(Xnet Connection)
            => BucketManager.Get(Connection).BucketItem(Connection, this);

        /// <inheritdoc/>
        protected override void Encode(BinaryWriter Writer)
        {
            Writer.Write(BucketId.ToByteArray());
            Writer.Write(ItemId.ToByteArray());
            Writer.Write7BitEncodedInt(Ttl);

            var Data = this.Data ?? Array.Empty<byte>();
            Writer.Write7BitEncodedInt(Data.Length);
            Writer.Write(Data);
        }

        /// <inheritdoc/>
        protected override void Decode(BinaryReader Reader)
        {
            BucketId = new Guid(Reader.ReadBytes(16));
            ItemId = new Guid(Reader.ReadBytes(16));
            Ttl = Reader.Read7BitEncodedInt();

            var Len = Reader.Read7BitEncodedInt();
            if (Len > 0)
                Data = Reader.ReadBytes(Len);

            else
                Data = Array.Empty<byte>();
        }
    }
}
