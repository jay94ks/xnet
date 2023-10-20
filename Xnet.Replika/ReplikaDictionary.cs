using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XnetDsa;

namespace XnetReplika
{
    /// <summary>
    /// Replika BSON dictionary.
    /// </summary>
    /// <typeparam name="TItem"></typeparam>
    public class ReplikaBsonDictionary<TItem> : IDisposable
    {
        private readonly Dictionary<Guid, TItem> m_CachedItems;

        private Guid[] m_CachedKeys;
        private bool m_Disposed = false;

        /// <summary>
        /// Initialize a new <see cref="ReplikaBsonDictionary{TItem}"/> instance.
        /// This will use overlay view.
        /// </summary>
        /// <param name="Manager"></param>
        /// <param name="Key"></param>
        public ReplikaBsonDictionary(IReplikaManager Manager, Guid Key, JsonSerializerSettings Settings = null) : this(Manager.Overlay[Key], Settings) { }

        /// <summary>
        /// Initialize a new <see cref="ReplikaBsonDictionary{TItem}"/> instance.
        /// This will use remote view, if `<paramref name="Manager"/>` has no authority, the dictionary will be read-only.
        /// </summary>
        /// <param name="Manager"></param>
        /// <param name="Owner"></param>
        /// <param name="Key"></param>
        public ReplikaBsonDictionary(IReplikaManager Manager, DsaPubKey Owner, Guid Key, JsonSerializerSettings Settings = null)
            : this(Manager.Get(Owner, Key), Settings) { }

        /// <summary>
        /// Initialize a new <see cref="ReplikaBsonDictionary{TItem}"/> instance.
        /// </summary>
        /// <param name="Dictionary"></param>
        public ReplikaBsonDictionary(IReplikaDictionary Dictionary, JsonSerializerSettings Settings = null)
        {
            m_CachedItems = new Dictionary<Guid, TItem>();

            if (Settings is null)
            {
                var Defaults = JsonConvert.DefaultSettings;
                if (Defaults is null)
                    Settings = new JsonSerializerSettings();

                else
                    Settings = Defaults.Invoke();
            }

            this.Dictionary = Dictionary;
            this.Settings = Settings;

            Dictionary.Changed += OnDataChanged;
        }

        /// <summary>
        /// Settings.
        /// </summary>
        public JsonSerializerSettings Settings { get; } 

        /// <summary>
        /// Serialize <paramref name="Item"/> to BSON using <see cref="Settings"/>.
        /// </summary>
        /// <param name="Item"></param>
        /// <returns></returns>
        private byte[] Serialize(TItem Item)
        {
            if (Item is null)
                return null;

            using var Stream = new MemoryStream();
            using(var Writer = new BsonDataWriter(Stream))
            {
                JsonSerializer
                    .Create(Settings)
                    .Serialize(Writer, Item);
            }

            return Stream.ToArray();
        }

        /// <summary>
        /// Deserialize <typeparamref name="TItem"/> from BSON using <see cref="Settings"/>.
        /// </summary>
        /// <param name="Bson"></param>
        /// <returns></returns>
        private TItem Deserialize(byte[] Bson)
        {
            if (Bson is null)
                return default;

            using var Stream = new MemoryStream(Bson, false);
            using (var Reader = new BsonDataReader(Stream))
            {
                return JsonSerializer
                    .Create(Settings)
                    .Deserialize<TItem>(Reader);
            }
        }

        /// <summary>
        /// Called when data changed.
        /// </summary>
        /// <param name="Dictionary"></param>
        /// <param name="ItemKey"></param>
        private void OnDataChanged(IReplikaDictionary Dictionary, Guid ItemKey)
        {
            Dictionary.TryGet(ItemKey, out var Data);
            lock (m_CachedItems)
            {
                if (m_Disposed)
                    return;

                if (Data is null)
                {
                    m_CachedKeys = null;
                    m_CachedItems.Remove(ItemKey);
                }

                else
                {
                    if (m_CachedItems.ContainsKey(ItemKey) == false)
                        m_CachedKeys = null;

                    m_CachedItems[ItemKey] = Deserialize(Data);
                }
            }
        }

        /// <summary>
        /// Indicates whether the local key has authority or not.
        /// </summary>
        public bool HasAuthority => Dictionary.HasAuthority;

        /// <summary>
        /// <see cref="IReplikaDictionary"/> instance.
        /// </summary>
        public IReplikaDictionary Dictionary { get; }

        /// <summary>
        /// Get keys which set on the dictionary. Note that, this refers only cached keys.
        /// So, this can be delayed then accessing <see cref="IReplikaDictionary"/> instance directly..
        /// </summary>
        public IEnumerable<Guid> Keys
        {
            get
            {
                lock (m_CachedItems)
                {
                    if (m_CachedKeys != null)
                        return m_CachedKeys;

                    return m_CachedKeys
                        = m_CachedItems.Keys.ToArray();
                }
            }
        }

        /// <summary>
        /// Try to get an item by its key.
        /// </summary>
        /// <param name="ItemKey"></param>
        /// <param name="Value"></param>
        /// <returns></returns>
        public bool TryGetValue(Guid ItemKey, out TItem Value)
        {
            lock(m_CachedItems)
            {
                if (m_Disposed)
                {
                    Value = default;
                    return false;
                }

                if (m_CachedItems.TryGetValue(ItemKey, out Value))
                    return true;
            }

            if (Dictionary.TryGet(ItemKey, out var Data))
            {
                Value = Deserialize(Data);

                lock(m_CachedItems)
                {
                    if (m_CachedItems.TryAdd(ItemKey, Value))
                        m_CachedKeys = null;
                }

                return true;
            }

            Value = default;
            return false;
        }

        /// <summary>
        /// Set an item by its key.
        /// </summary>
        /// <param name="ItemKey"></param>
        /// <param name="Value"></param>
        /// <returns></returns>
        public bool TrySet(Guid ItemKey, TItem Value)
        {
            var Data = Serialize(Value);
            lock (m_CachedItems)
            {
                if (m_Disposed)
                    return false;

                if (m_CachedItems.TryGetValue(ItemKey, out var Older) &&
                    Older is IEquatable<TItem> Left &&
                    Value is IEquatable<TItem> Right &&
                    Left.Equals(Right))
                {
                    return true;
                }

                if (HasAuthority)
                    m_CachedItems[ItemKey] = Value;
            }

            if (Dictionary.Set(ItemKey, Data))
                return true;

            return false;
        }

        /// <summary>
        /// Set an item by its key asynchronously.
        /// </summary>
        /// <param name="ItemKey"></param>
        /// <param name="Value"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public async Task<bool> TrySetAsync(Guid ItemKey, TItem Value, CancellationToken Token = default)
        {
            var Data = Serialize(Value);
            lock (m_CachedItems)
            {
                if (m_Disposed)
                    return false;

                if (m_CachedItems.TryGetValue(ItemKey, out var Older) &&
                    Older is IEquatable<TItem> Left &&
                    Value is IEquatable<TItem> Right &&
                    Left.Equals(Right))
                {
                    return true;
                }

                if (HasAuthority)
                    m_CachedItems[ItemKey] = Value;
            }

            return await Dictionary.SetAsync(ItemKey, Data, Token);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            lock(m_CachedItems)
            {
                if (m_Disposed)
                    return;

                m_Disposed = true;
                m_CachedItems.Clear();
            }

            Dictionary.Changed -= OnDataChanged;
        }
    }
}
