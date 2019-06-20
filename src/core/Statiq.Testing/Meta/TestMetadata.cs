﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Statiq.Common.Meta;
using Statiq.Testing.Execution;

namespace Statiq.Testing.Meta
{
    /// <summary>
    /// A test implementation of <see cref="IMetadata"/>.
    /// </summary>
    public class TestMetadata : IMetadata, IDictionary<string, object>, ITypeConversions
    {
        private readonly Dictionary<string, object> _dictionary;

        public TestMetadata()
        {
            _dictionary = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        }

        public TestMetadata(IDictionary<string, object> initialMetadata)
        {
            _dictionary = new Dictionary<string, object>(initialMetadata, StringComparer.OrdinalIgnoreCase);
        }

        /// <inhertdoc />
        public bool ContainsKey(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }
            return _dictionary.ContainsKey(key);
        }

        /// <inhertdoc />
        public void Add(string key, object value) => _dictionary.Add(key, value);

        /// <inhertdoc />
        public bool Remove(string key) => _dictionary.Remove(key);

        object IDictionary<string, object>.this[string key]
        {
            get { return _dictionary[key]; }
            set { _dictionary[key] = value; }
        }

        /// <inhertdoc />
        public bool TryGetRaw(string key, out object value) => _dictionary.TryGetValue(key, out value);

        /// <inheritdoc />
        public bool TryGetValue<T>(string key, out T value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }
            value = default(T);
            if (!_dictionary.TryGetValue(key, out object rawValue))
            {
                return false;
            }
            rawValue = GetValue(rawValue);

            // Check if there's a test-specific conversion
            if (TypeConversions.TryGetValue((rawValue?.GetType() ?? typeof(object), typeof(T)), out Func<object, object> typeConversion))
            {
                value = (T)typeConversion(rawValue);
            }
            else
            {
                // Default conversion is just to cast
                value = (T)rawValue;
            }

            return true;
        }

        /// <inheritdoc />
        public bool TryGetValue(string key, out object value) => TryGetValue<object>(key, out value);

        public Dictionary<(Type Value, Type Result), Func<object, object>> TypeConversions { get; } =
            new Dictionary<(Type Value, Type Result), Func<object, object>>(TestExecutionContext.DefaultTypeConversions);

        public void AddTypeConversion<T, TResult>(Func<T, TResult> typeConversion) => TypeConversions.Add((typeof(T), typeof(TResult)), x => typeConversion((T)x));

        /// <inhertdoc />
        public IMetadata GetMetadata(params string[] keys) => new TestMetadata(keys.Where(ContainsKey).ToDictionary(x => x, x => this[x]));

        /// <inhertdoc />
        public object this[string key]
        {
            get
            {
                if (key == null)
                {
                    throw new ArgumentNullException(nameof(key));
                }
                object value;
                if (!TryGetValue(key, out value))
                {
                    throw new KeyNotFoundException();
                }
                return value;
            }
            set
            {
                _dictionary[key] = value;
            }
        }

        /// <inhertdoc />
        public IEnumerable<string> Keys => _dictionary.Keys;

        ICollection<object> IDictionary<string, object>.Values => _dictionary.Values;

        ICollection<string> IDictionary<string, object>.Keys => _dictionary.Keys;

        /// <inhertdoc />
        public IEnumerable<object> Values => _dictionary.Select(x => GetValue(x.Value));

        /// <inhertdoc />
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => _dictionary.Select(GetItem).GetEnumerator();

        /// <inhertdoc />
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inhertdoc />
        public void Add(KeyValuePair<string, object> item) => ((IDictionary<string, object>)_dictionary).Add(item);

        /// <inhertdoc />
        public void Clear() => _dictionary.Clear();

        /// <inhertdoc />
        public bool Contains(KeyValuePair<string, object> item) => _dictionary.Contains(item);

        /// <inhertdoc />
        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex) => ((IDictionary<string, object>)_dictionary).CopyTo(array, arrayIndex);

        /// <inhertdoc />
        public bool Remove(KeyValuePair<string, object> item) => ((IDictionary<string, object>)_dictionary).Remove(item);

        /// <inhertdoc />
        public int Count => _dictionary.Count;

        public bool IsReadOnly => ((IDictionary<string, object>)_dictionary).IsReadOnly;

        /// <summary>
        /// This resolves the metadata value by recursively expanding IMetadataValue.
        /// </summary>
        private object GetValue(object originalValue)
        {
            IMetadataValue metadataValue = originalValue as IMetadataValue;
            return metadataValue != null ? GetValue(metadataValue.Get(this)) : originalValue;
        }

        /// <summary>
        /// This resolves the metadata value by expanding IMetadataValue.
        /// </summary>
        private KeyValuePair<string, object> GetItem(KeyValuePair<string, object> item)
        {
            IMetadataValue metadataValue = item.Value as IMetadataValue;
            return metadataValue != null ? new KeyValuePair<string, object>(item.Key, GetValue(metadataValue.Get(this))) : item;
        }
    }
}
