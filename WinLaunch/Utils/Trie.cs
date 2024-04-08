using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace KTrie
{
    /// <summary>
    /// Defines a key/value pair that can be set or retrieved from <see cref="StringTrie{TValue}"/>.
    /// </summary>
    public struct StringEntry<TValue>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StringEntry{TValue}"/> structure with the specified key and value.
        /// </summary>
        /// <param name="key">The <see cref="string"/> object defined in each key/value pair.</param>
        /// <param name="value">The definition associated with key.</param>
        public StringEntry(string key, TValue value)
        {
            Key = key;
            Value = value;
        }

        /// <summary>
        /// Gets the key in the key/value pair.
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// Gets the value in the key/value pair.
        /// </summary>
        public TValue Value { get; }
    }

    public class TrieSet<T> : ICollection<IEnumerable<T>>
    {
        private readonly IEqualityComparer<T> _comparer;

        private readonly TrieNode _root;

        public TrieSet() : this(EqualityComparer<T>.Default)
        {

        }

        public TrieSet(IEqualityComparer<T> comparer)
        {
            _comparer = comparer;
            _root = new TrieNode(default, comparer);
        }

        public int Count { get; private set; }

        bool ICollection<IEnumerable<T>>.IsReadOnly => false;

        public IEnumerator<IEnumerable<T>> GetEnumerator() =>
            GetAllNodes(_root).Select(GetFullKey).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Add(IEnumerable<T> key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            var node = _root;

            // ReSharper disable once PossibleMultipleEnumeration
            foreach (var item in key)
            {
                node = AddItem(node, item);
            }

            if (node.IsTerminal)
            {
                throw new ArgumentException($"An element with the same key already exists: '{key}'", nameof(key));
            }

            node.IsTerminal = true;

            // ReSharper disable once PossibleMultipleEnumeration
            node.Item = key;
            Count++;
        }

        public void AddRange(IEnumerable<IEnumerable<T>> collection)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));

            foreach (var item in collection)
            {
                Add(item);
            }
        }

        public void Clear()
        {
            _root.Children.Clear();
            Count = 0;
        }

        public bool Contains(IEnumerable<T> item)
        {
            var node = GetNode(item);

            return node != null && node.IsTerminal;
        }

        public void CopyTo(IEnumerable<T>[] array, int arrayIndex) => Array.Copy(GetAllNodes(_root).Select(GetFullKey).ToArray(), 0, array, arrayIndex, Count);

        public bool Remove(IEnumerable<T> key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            var node = GetNode(key);

            if (node == null)
            {
                return false;
            }

            if (!node.IsTerminal)
            {
                return false;
            }

            RemoveNode(node);

            return true;
        }

        /// <summary>
        /// Gets an item by key.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="item">Output item.</param>
        /// <returns>true if trie contains an element with the specified key; otherwise, false.</returns>
        public bool TryGetItem(IEnumerable<T> key, out IEnumerable<T> item)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            var node = GetNode(key);
            item = null;

            if (node == null)
            {
                return false;
            }

            if (!node.IsTerminal)
            {
                return false;
            }

            item = node.Item;

            return true;
        }

        internal bool TryGetNode(IEnumerable<T> key, out TrieNode node)
        {
            node = GetNode(key);

            if (node == null)
            {
                return false;
            }

            if (!node.IsTerminal)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets items by key prefix.
        /// </summary>
        /// <param name="prefix">Key prefix.</param>
        /// <returns>Collection of <see cref="T"/> items.</returns>
        public IEnumerable<IEnumerable<T>> GetByPrefix(IEnumerable<T> prefix)
        {
            if (prefix == null) throw new ArgumentNullException(nameof(prefix));

            var node = _root;

            foreach (var item in prefix)
            {
                if (!node.Children.TryGetValue(item, out node))
                {
                    return Enumerable.Empty<IEnumerable<T>>();
                }
            }

            return GetByPrefix(node);
        }

        private static IEnumerable<TrieNode> GetAllNodes(TrieNode node)
        {
            foreach (var child in node.Children)
            {
                if (child.Value.IsTerminal)
                {
                    yield return child.Value;
                }

                foreach (var item in GetAllNodes(child.Value))
                {
                    if (item.IsTerminal)
                    {
                        yield return item;
                    }
                }
            }
        }

        private static IEnumerable<IEnumerable<T>> GetByPrefix(TrieNode node)
        {
            var stack = new Stack<TrieNode>();
            var current = node;

            while (stack.Count > 0 || current != null)
            {
                if (current != null)
                {
                    if (current.IsTerminal)
                    {
                        yield return GetFullKey(current);
                    }

                    using (var enumerator = current.Children.GetEnumerator())
                    {
                        current = enumerator.MoveNext() ? enumerator.Current.Value : null;

                        while (enumerator.MoveNext())
                        {
                            stack.Push(enumerator.Current.Value);
                        }
                    }
                }
                else
                {
                    current = stack.Pop();
                }
            }
        }

        private static IEnumerable<T> GetFullKey(TrieNode node)
        {
            //var stack = new Stack<T>();
            //stack.Push(node.Key);

            //var n = node;

            //while ((n = n.Parent) != _root)
            //{
            //    stack.Push(n.Key);
            //}

            //return stack;

            return node.Item;
        }

        private TrieNode GetNode(IEnumerable<T> key)
        {
            var node = _root;

            foreach (var item in key)
            {
                if (!node.Children.TryGetValue(item, out node))
                {
                    return null;
                }
            }

            return node;
        }

        private void RemoveNode(TrieNode node)
        {
            Remove(node);
            Count--;
        }

        private TrieNode AddItem(TrieNode node, T key)
        {
            if (!node.Children.TryGetValue(key, out var child))
            {
                child = new TrieNode(key, _comparer)
                {
                    Parent = node
                };

                node.Children.Add(key, child);
            }

            return child;
        }

        private void Remove(TrieNode node, T key)
        {
            foreach (var trieNode in node.Children)
            {
                if (_comparer.Equals(key, trieNode.Key))
                {
                    node.Children.Remove(trieNode);

                    return;
                }
            }
        }

        private void Remove(TrieNode node)
        {
            while (true)
            {
                node.IsTerminal = false;

                if (node.Children.Count == 0 && node.Parent != null)
                {
                    Remove(node.Parent, node.Key);

                    if (!node.Parent.IsTerminal)
                    {
                        node = node.Parent;
                        continue;
                    }
                }

                break;
            }
        }

        internal sealed class TrieNode
        {
            public TrieNode(T key, IEqualityComparer<T> comparer)
            {
                Key = key;
                Children = new Dictionary<T, TrieNode>(comparer);
            }

            public bool IsTerminal { get; set; }

            public T Key { get; }

            public IEnumerable<T> Item { get; set; }

            public IDictionary<T, TrieNode> Children { get; }

            public TrieNode Parent { get; set; }
        }
    }

    /// <summary>
    /// Defines a key/value pair that can be set or retrieved from <see cref="StringTrie{TValue}"/>.
    /// </summary>
    public struct TrieEntry<TKey, TValue>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StringEntry{TValue}"/> structure with the specified key and value.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The definition associated with key.</param>
        public TrieEntry(IEnumerable<TKey> key, TValue value)
        {
            Key = key;
            Value = value;
        }

        /// <summary>
        /// Gets the key in the key/value pair.
        /// </summary>
        public IEnumerable<TKey> Key { get; }

        /// <summary>
        /// Gets the value in the key/value pair.
        /// </summary>
        public TValue Value { get; }
    }

    /// <summary>
    /// Implementation of trie data structure.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the trie.</typeparam>
    /// <typeparam name="TValue">The type of values in the trie.</typeparam>
    public class Trie<TKey, TValue> : IDictionary<IEnumerable<TKey>, TValue>
    {
        private readonly TrieSet<TKey> _trie;

        /// <summary>
        /// Initializes a new instance of the <see cref="Trie{TKey,TValue}"/>.
        /// </summary>
        public Trie() : this(EqualityComparer<TKey>.Default)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Trie{TKey,TValue}"/>.
        /// </summary>
        /// <param name="comparer">Comparer.</param>
        public Trie(IEqualityComparer<TKey> comparer)
        {
            _trie = new TrieSet<TKey>(comparer);
        }

        public int Count => _trie.Count;

        bool ICollection<KeyValuePair<IEnumerable<TKey>, TValue>>.IsReadOnly => false;

        public ICollection<IEnumerable<TKey>> Keys => _trie.ToList();

        public ICollection<TValue> Values => _trie.Cast<TrieEntryPrivate>().Select(te => te.Value).ToArray();

        public TValue this[IEnumerable<TKey> key]
        {
            get
            {
                if (TryGetValue(key, out var val))
                {
                    return val;
                }

                throw new KeyNotFoundException($"The given key was not present in the trie.");
            }
            set
            {
                // ReSharper disable once PossibleMultipleEnumeration
                var result = _trie.TryGetItem(key, out var trieEntry);

                if (result)
                {
                    ((TrieEntryPrivate)trieEntry).Value = value;
                }
                else
                {
                    // ReSharper disable once PossibleMultipleEnumeration
                    Add(key, value);
                }
            }
        }

        /// <summary>
        /// Gets items by key prefix.
        /// </summary>
        /// <param name="prefix">Key prefix.</param>
        /// <returns>Collection of <see cref="TrieEntry{TKey, TValue}"/> items which have key which starts from specified <see cref="prefix"/>.</returns>
        public IEnumerable<TrieEntry<TKey, TValue>> GetByPrefix(IEnumerable<TKey> prefix) =>
            _trie.GetByPrefix(prefix).Cast<TrieEntryPrivate>().Select(i => new TrieEntry<TKey, TValue>(i, i.Value));


        public IEnumerator<KeyValuePair<IEnumerable<TKey>, TValue>> GetEnumerator() =>
            _trie.Cast<TrieEntryPrivate>().Select(i => new KeyValuePair<IEnumerable<TKey>, TValue>(i, i.Value)).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        void ICollection<KeyValuePair<IEnumerable<TKey>, TValue>>.Add(KeyValuePair<IEnumerable<TKey>, TValue> item) =>
            Add(item.Key, item.Value);

        public void Clear() => _trie.Clear();


        bool ICollection<KeyValuePair<IEnumerable<TKey>, TValue>>.Contains(KeyValuePair<IEnumerable<TKey>, TValue> item)
        {
            var result = _trie.TryGetItem(item.Key, out var trieEntry);

            if (result)
            {
                var value = ((TrieEntryPrivate)trieEntry).Value;

                if (EqualityComparer<TValue>.Default.Equals(item.Value, value))
                {
                    return true;
                }
            }

            return false;
        }

        public void CopyTo(KeyValuePair<IEnumerable<TKey>, TValue>[] array, int arrayIndex) =>
            Array.Copy(_trie.Cast<TrieEntryPrivate>().Select(i => new KeyValuePair<IEnumerable<TKey>, TValue>(i, i.Value)).ToArray(), 0, array, arrayIndex, Count);

        bool ICollection<KeyValuePair<IEnumerable<TKey>, TValue>>.Remove(KeyValuePair<IEnumerable<TKey>, TValue> item)
        {
            var result = _trie.TryGetItem(item.Key, out var trieEntry);

            if (result)
            {
                var value = ((TrieEntryPrivate)trieEntry).Value;

                if (EqualityComparer<TValue>.Default.Equals(item.Value, value))
                {
                    return Remove(item.Key);
                }
            }

            return false;
        }

        public bool ContainsKey(IEnumerable<TKey> key) => _trie.Contains(key);

        public void Add(IEnumerable<TKey> key, TValue value) =>
            _trie.Add(new TrieEntryPrivate(key) { Value = value });

        public bool Remove(IEnumerable<TKey> key) => _trie.Remove(key);

        public bool TryGetValue(IEnumerable<TKey> key, out TValue value)
        {
            var result = _trie.TryGetItem(key, out var trieEntry);

            value = result ? ((TrieEntryPrivate)trieEntry).Value : default;

            return result;
        }

        private sealed class TrieEntryPrivate : IEnumerable<TKey>
        {
            public TrieEntryPrivate(IEnumerable<TKey> key)
            {
                Key = key;
            }

            private IEnumerable<TKey> Key { get; }

            public TValue Value { get; set; }

            public IEnumerator<TKey> GetEnumerator() => Key.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }

    /// <summary>
    /// Implementation of trie data structure.
    /// </summary>
    /// <typeparam name="TValue">The type of values in the trie.</typeparam>
    public class StringTrie<TValue> : IDictionary<string, TValue>
    {
        private readonly Trie<char, TValue> _trie;

        /// <summary>
        /// Initializes a new instance of the <see cref="StringTrie{TValue}"/>.
        /// </summary>
        /// <param name="comparer">Comparer.</param>
        public StringTrie(IEqualityComparer<char> comparer)
        {
            _trie = new Trie<char, TValue>(comparer);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StringTrie{TValue}"/>.
        /// </summary>
        public StringTrie() : this(EqualityComparer<char>.Default)
        {
        }

        /// <summary>
        /// Gets the number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </summary>
        /// <returns>
        /// The number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </returns>
        public int Count => _trie.Count;

        /// <summary>
        /// Gets an <see cref="T:System.Collections.Generic.ICollection`1"/> containing the keys of the <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.Generic.ICollection`1"/> containing the keys of the object that implements <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </returns>
        public ICollection<string> Keys => _trie.Keys.Select(i => new string(i.ToArray())).ToArray();

        /// <summary>
        /// Gets an <see cref="T:System.Collections.Generic.ICollection`1"/> containing the values in the <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.Generic.ICollection`1"/> containing the values in the object that implements <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </returns>
        public ICollection<TValue> Values => _trie.Values.ToArray();

        bool ICollection<KeyValuePair<string, TValue>>.IsReadOnly => false;

        /// <summary>
        /// Gets or sets the element with the specified key.
        /// </summary>
        /// <returns>
        /// The element with the specified key.
        /// </returns>
        /// <param name="key">The key of the element to get or set.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is null.</exception>
        /// <exception cref="T:System.Collections.Generic.KeyNotFoundException">The property is retrieved and <paramref name="key"/> is not found.</exception>
        public TValue this[string key]
        {
            get => _trie[key];

            set => _trie[key] = value;
        }

        /// <summary>
        /// Adds an element with the provided charKey and value to the <see cref="StringTrie{TValue}"/>.
        /// </summary>
        /// <param name="key">The object to use as the charKey of the element to add.</param>
        /// <param name="value">The object to use as the value of the element to add.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is null.</exception>
        /// <exception cref="T:System.ArgumentException">An element with the same charKey already exists in the <see cref="StringTrie{TValue}"/>.</exception>
        public void Add(string key, TValue value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            _trie.Add(key, value);
        }

        /// <summary>
        /// Adds the elements of the specified collection to the <see cref="StringTrie{TValue}"/>.
        /// </summary>
        /// <param name="collection">The collection whose elements should be added to the <see cref="StringTrie{TValue}"/>. The items should have unique keys.</param>
        /// <exception cref="T:System.ArgumentException">An element with the same charKey already exists in the <see cref="StringTrie{TValue}"/>.</exception>
        public void AddRange(IEnumerable<StringEntry<TValue>> collection)
        {
            foreach (var item in collection)
            {
                _trie.Add(item.Key, item.Value);
            }
        }

        /// <summary>
        /// Removes all items from the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </summary>
        /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only. </exception>
        public void Clear() => _trie.Clear();

        /// <summary>
        /// Determines whether the <see cref="T:System.Collections.Generic.IDictionary`2"/> contains an element with the specified charKey.
        /// </summary>
        /// <returns>
        /// true if the <see cref="T:System.Collections.Generic.IDictionary`2"/> contains an element with the charKey; otherwise, false.
        /// </returns>
        /// <param name="key">The charKey to locate in the <see cref="T:System.Collections.Generic.IDictionary`2"/>.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is null.</exception>
        public bool ContainsKey(string key) => _trie.ContainsKey(key);

        /// <summary>
        /// Gets items by key prefix.
        /// </summary>
        /// <param name="prefix">Key prefix.</param>
        /// <returns>Collection of <see cref="StringEntry{TValue}"/> items which have key with specified key.</returns>
        public IEnumerable<StringEntry<TValue>> GetByPrefix(string prefix) =>
            _trie.GetByPrefix(prefix).Select(i => new StringEntry<TValue>(new string(i.Key.ToArray()), i.Value));

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>1</filterpriority>
        public IEnumerator<KeyValuePair<string, TValue>> GetEnumerator() =>
            _trie.Select(i => new KeyValuePair<string, TValue>(new string(i.Key.ToArray()), i.Value)).GetEnumerator();

        /// <summary>
        /// Removes the element with the specified charKey from the <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </summary>
        /// <returns>
        /// true if the element is successfully removed; otherwise, false.  This method also returns false if <paramref name="key"/> was not found in the original <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </returns>
        /// <param name="key">The charKey of the element to remove.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is null.</exception>
        /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.IDictionary`2"/> is read-only.</exception>
        public bool Remove(string key) => _trie.Remove(key);

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <returns>
        /// true if the object that implements <see cref="T:System.Collections.Generic.IDictionary`2"/> contains an element with the specified key; otherwise, false.
        /// </returns>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="value">When this method returns, the value associated with the specified key, if the key is found; otherwise, the default value for the type of the <paramref name="value"/> parameter. This parameter is passed uninitialized.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is null.</exception>
        public bool TryGetValue(string key, out TValue value) => _trie.TryGetValue(key, out value);

        void ICollection<KeyValuePair<string, TValue>>.Add(KeyValuePair<string, TValue> item) =>
            Add(item.Key, item.Value);

        bool ICollection<KeyValuePair<string, TValue>>.Contains(KeyValuePair<string, TValue> item) =>
            ((IDictionary<IEnumerable<char>, TValue>)_trie).Contains(new KeyValuePair<IEnumerable<char>, TValue>(item.Key, item.Value));

        public void CopyTo(KeyValuePair<string, TValue>[] array, int arrayIndex) =>
            Array.Copy(_trie.Select(i => new KeyValuePair<string, TValue>(new string(i.Key.ToArray()), i.Value)).ToArray(), 0, array, arrayIndex, Count);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        bool ICollection<KeyValuePair<string, TValue>>.Remove(KeyValuePair<string, TValue> item) =>
            ((IDictionary<IEnumerable<char>, TValue>)_trie).Remove(new KeyValuePair<IEnumerable<char>, TValue>(item.Key, item.Value));
    }
}