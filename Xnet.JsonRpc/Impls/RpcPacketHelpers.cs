using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json.Linq;

namespace XnetInternals.Impls
{
    internal static class RpcPacketHelpers
    {
        /// <summary>
        /// Encode <see cref="JObject"/> to the BSON bytes.
        /// </summary>
        /// <param name="Json"></param>
        /// <returns></returns>
        public static byte[] ToBson(this JObject Json)
        {
            if (Json is null)
                return Array.Empty<byte>();

            using var Stream = new MemoryStream();
            using (var Writer = new BsonDataWriter(Stream))
            {
                JsonSerializer
                    .CreateDefault()
                    .Serialize(Writer, Json);
            }

            return Stream.ToArray();
        }

        /// <summary>
        /// Decode the BSON bytes to <see cref="JObject"/>.
        /// </summary>
        /// <param name="Bson"></param>
        /// <returns></returns>
        public static JObject ToJson(this byte[] Bson)
        {
            if (Bson is null || Bson.Length <= 0)
                return null;

            using var Stream = new MemoryStream(Bson, false);
            using (var Reader = new BsonDataReader(Stream))
            {
                return JsonSerializer
                    .CreateDefault()
                    .Deserialize<JObject>(Reader);
            }
        }
    }
}
