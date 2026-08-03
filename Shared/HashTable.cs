using System.IO.Hashing;
using System.Runtime.CompilerServices;
using System.Text;

namespace Shared
{
    public unsafe class HashTable
    {
        private const int DefaultCapacity = 1024;
        private const int MaxCapacity = DefaultCapacity;
        private const double LoadFactor = 0.75;

        private Entry[] _entries;
        private int _count;
        private readonly bool _allowResize;

        public HashTable(int expectedCount = default, bool allowResize = true)
        {
            _allowResize = allowResize;

            var targetCapacity = (int)(expectedCount / LoadFactor);
            var capacity = NextPowerOf2(Math.Max(DefaultCapacity, targetCapacity));
            capacity = Math.Min(capacity, MaxCapacity);

            _entries = new Entry[capacity];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddOrUpdate(byte* namePtr, int nameLength, int temperature)
        {
            // Check if resize is needed before adding a new entry
            if (_allowResize && _count >= _entries.Length * LoadFactor)
            {
                var newCapacity = Math.Min(_entries.Length * 2, MaxCapacity);
                if(newCapacity > _entries.Length) Resize(newCapacity);
            }

            var hash = ComputeHash(namePtr, nameLength);
            var index = (uint)(hash & (uint)(_entries.Length - 1));

            while (true)
            {
                ref Entry entry = ref _entries[index];

                if (entry.Name == null)
                {
                    // Empty slot found, insert new entry
                    entry.Name = new byte[nameLength];
                    fixed (byte* destPtr = entry.Name)
                    {
                        Buffer.MemoryCopy(namePtr, destPtr, nameLength, nameLength);
                    }

                    entry.StationName = Encoding.UTF8.GetString(entry.Name);
                    entry.Hash = hash;
                    entry.Temperature = temperature;
                    entry.Min = temperature;
                    entry.Max = temperature;
                    entry.Sum = temperature;
                    entry.Count = 1;

                    _count++;
                    return;
                }

                // Not empty slot
                if(entry.Hash == hash && entry.Name.Length == nameLength)
                {
                    // Veryfi bytes match
                    fixed (byte* destPtr = entry.Name)
                    {
                        var match = true;
                        for(int i = 0; i < nameLength; i++)
                        {
                            if (namePtr[i] != destPtr[i])
                            {
                                match = false;
                                break;
                            }
                        }

                        // Update existing entry
                        if (match)
                        {
                            entry.Temperature = temperature;

                            // Min and Max is optimized by JIT to avoid branching
                            entry.Min = Math.Min(entry.Min, temperature);
                            entry.Max = Math.Max(entry.Max, temperature);
                            entry.Sum += temperature;
                            entry.Count++;
                            return;
                        }

                        // Linear probing: move to the next index
                        index = (index + 1) & (uint)(_entries.Length - 1);
                    }
                }
            }
        }

        // TODO: Add a method to retrieve an entry by name, if needed.
        // TODO: Add a method to remove an entry by name, if needed.

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Resize(int newCapacity)
        {
            var oldEntries = _entries;
            _entries = new Entry[newCapacity];
            _count = 0;

            // We need to rehash all existing entries into the new array
            foreach (var oldEntry in oldEntries)
            {
                if (oldEntry.Name != null)
                {
                    //Add(oldEntry);
                    fixed (byte* ptr = oldEntry.Name)
                    {
                        // Reinsert the entry into the new array
                        var hash = oldEntry.Hash;

                        // To make it work branchless, we can use bitwise AND with (newCapacity - 1) instead of modulo operation.
                        // This works because newCapacity is always a power of 2.
                        var index  = (uint)(hash & (uint)(newCapacity - 1));

                        while (true)
                        {
                            // With struct[] we can use ref to get a reference to the struct in the array, allowing us to
                            // modify it directly without copying.
                            ref Entry entry = ref _entries[index];

                            if (entry.Name == null)
                            {
                                entry = oldEntry;
                                _count++;
                                break;
                            }

                            // Linear probing: move to the next index
                            index = (index + 1) & (uint)(newCapacity - 1);
                        }
                    }
                }
            }
        }

        public IEnumerable<Entry> GetEntries()
        {
            foreach (var entry in _entries)
            {
                if (entry.Name != null)
                {
                    yield return entry;
                }
            }
        }

        // Branchless method to compute the next power of 2 for a given integer n.
        private static int NextPowerOf2(int n)
        {
            if (n <= 0)
                return 1;

            n--;
            n |= n >> 1;
            n |= n >> 2;
            n |= n >> 4;
            n |= n >> 8;
            n |= n >> 16;

            return n + 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong ComputeHash(byte* bytes, int length)
        {
            return XxHash3.HashToUInt64(new ReadOnlySpan<byte>(bytes, length));
        }
    }
}
