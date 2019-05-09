using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Musegician.Core
{
    /// <summary>
    /// Statically-sized ring buffer container.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RingBuffer<T> : IEnumerable<T>, ICollection<T>
    {
        private T[] values = null;
        private int headIndex = -1;

        public int Size => values.Length;
        public int Count { get; private set; } = 0;

        bool ICollection<T>.IsReadOnly => false;

        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                {
                    throw new IndexOutOfRangeException();
                }

                return values[(Size + headIndex - index) % Size];
            }

            set
            {
                if (index < 0 || index >= Count)
                {
                    throw new IndexOutOfRangeException();
                }

                values[(Size + headIndex - index) % Size] = value;
            }
        }

        /// <summary>
        /// Returns the head (the most recent) element.
        /// </summary>
        public T Head => this[0];

        /// <summary>
        /// Returns the element at the Tail (the oldest).
        /// </summary>
        public T Tail => this[Count - 1];

        /// <summary>
        /// Construct an empty ring buffer supporting bufferSize elements
        /// </summary>
        /// <param name="bufferSize"></param>
        public RingBuffer(int bufferSize)
        {
            if (bufferSize <= 0)
            {
                throw new ArgumentException($"Initialized a RingBuffer with size {bufferSize}.");
            }

            values = new T[bufferSize];
            Count = 0;
            headIndex = -1;
        }

        /// <summary>
        /// Copy the list into a new buffer, optionally specify size.
        /// </summary>
        /// <param name="values"></param>
        /// <param name="bufferSize"></param>
        public RingBuffer(IEnumerable<T> values, int bufferSize = -1)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            if (bufferSize == -1)
            {
                bufferSize = values.Count();
            }

            if (bufferSize <= 0)
            {
                throw new ArgumentException($"Attempted to initialize RingBuffer with size {bufferSize}.");
            }

            this.values = new T[bufferSize];

            Count = Math.Min(bufferSize, values.Count());
            headIndex = Count - 1;

            //Iterate over collection, up to availableCount, and add items
            //We add back to front because newer items go to higher indices in our buffer

            int i = -1;
            using (var e = values.GetEnumerator())
            {
                while (e.MoveNext() && ++i < Count)
                {
                    this.values[headIndex - i] = e.Current;
                }
            }
        }

        /// <summary>
        /// Add newValue to the end of the ringbuffer.
        /// Replaces the oldest member if at capacity.
        /// </summary>
        /// <param name="newValue"></param>
        public void Push(T newValue) => Add(newValue);

        /// <summary>
        /// Add newValue to the end of the ringbuffer.
        /// Replaces the oldest member if at capacity.
        /// </summary>
        /// <param name="newValue"></param>
        public void Add(T newValue)
        {
            Count = Math.Min(Count + 1, Size);
            headIndex = (headIndex + 1) % Size;
            values[headIndex] = newValue;
        }

        /// <summary>
        /// Add newValues to the end of the ringbuffer.
        /// Replaces the oldest members if at capacity.
        /// </summary>
        public void AddRange(IEnumerable<T> newValues)
        {
            Count = Math.Min(Count + newValues.Count(), Size);

            foreach (T newValue in newValues)
            {
                headIndex = (headIndex + 1) % Size;
                values[headIndex] = newValue;
            }
        }

        /// <summary>
        /// Clears the RingBuffer and fills it with optional <paramref name="count"/> default elements.
        /// If <paramref name="count"/> is -1, then the RingBuffer is completely filled.
        /// </summary>
        /// <param name="count">The number of default elements to provide.  If this is -1, the buffer is filled</param>
        public void ZeroOut(int count = -1)
        {
            if (count == -1)
            {
                count = Size;
            }

            if (count < 0 || count > Size)
            {
                throw new ArgumentException($"Called ZeroOut on RingBuffer with invalid count: {count}.",
                    paramName: nameof(count));
            }

            //Clear already sets the values to zero
            Clear();

            Count = count;
            headIndex = count - 1;
        }

        /// <summary>
        /// Clear the current items in the ring buffer.
        /// Doesn't resize or release buffer memory.
        /// Does release item handles.
        /// </summary>
        public void Clear()
        {
            Count = 0;
            headIndex = -1;

            for (int i = 0; i < Size; i++)
            {
                values[i] = default;
            }
        }

        /// <summary>
        /// Query the list for the argument value.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool Contains(T value) => GetIndex(value) != -1;

        /// <summary>
        /// Get the index of the argument value if it's present.  Otherwise returns -1.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public int GetIndex(T value)
        {
            for (int i = 0; i < Count; i++)
            {
                if (Comparer<T>.Default.Compare(this[i], value) == 0)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Removes the first element matching the argument value, if present, returns whether a value was removed.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool Remove(T value)
        {
            int index = GetIndex(value);

            if (index >= 0)
            {
                RemoveAt(index);
                return true;
            }

            return false;
        }


        /// <summary>
        /// Removes the item at index.
        /// </summary>
        /// <param name="index"></param>
        /// <exception cref="IndexOutOfRangeException">
        /// Throws System.IndexOutOfRangeException if the index exceeds the available count.
        /// </exception>
        public void RemoveAt(int index)
        {
            if (index >= Count)
            {
                throw new IndexOutOfRangeException();
            }

            if (index < (Count + 1) / 2)
            {
                //If the item we're removing is closer to the front, move items forward
                for (int i = index; i > 0; i--)
                {
                    this[i] = this[i - 1];
                }

                //Clear the copy of the last remaining element
                //We don't want to retain references to dead elements
                this[0] = default;

                //Cyclically decrement headIndex
                headIndex = (Size + headIndex - 1) % Size;
            }
            else
            {
                for (int i = index; i < Count - 1; i++)
                {
                    this[i] = this[i + 1];
                }

                //Clear the copy of the last remaining element
                //We don't want to retain references to dead elements
                this[Count - 1] = default;
            }

            --Count;

            if (Count == 0)
            {
                headIndex = -1;
            }
        }

        /// <summary>
        /// Removes and returns the item at the head (the newest).
        /// </summary>
        /// <returns></returns>
        public T Pop()
        {
            T temp = this[0];
            RemoveAt(0);
            return temp;
        }

        /// <summary>
        /// Removes and returns the item at the tail (the oldest).
        /// </summary>
        /// <returns></returns>
        public T PopBack()
        {
            T temp = this[Count - 1];
            RemoveAt(Count - 1);
            return temp;
        }

        /// <summary>
        /// Returns the element at the head (the newest).
        /// </summary>
        /// <returns>The element at the head</returns>
        public T PeekHead() => this[0];

        /// <summary>
        /// Returns the element at the tail (the oldest).
        /// </summary>
        /// <returns>The element at the tail</returns>
        public T PeekTail() => this[Count - 1];

        /// <summary>
        /// Returns the number of elements whose value match the argument.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public int CountElement(T value)
        {
            int count = 0;

            for (int i = 0; i < Count; i++)
            {
                if (Comparer<T>.Default.Compare(this[i], value) == 0)
                {
                    ++count;
                }
            }

            return count;
        }

        /// <summary>
        /// Copy the list to the dest array, using the destIndex as an offset to the destination.
        /// </summary>
        /// <param name="dest"></param>
        /// <param name="destIndex"></param>
        public void CopyTo(T[] dest, int destIndex)
        {
            // We use the naive method because we would have to reverse the order of elements anyway
            for (int i = 0; i < Size && i < (destIndex + dest.Length); i++)
            {
                dest[destIndex + i] = this[i];
            }
        }

        /// <summary>
        /// Resize the buffer of this list to support bufferSize elements.
        /// </summary>
        /// <param name="bufferSize"></param>
        public void Resize(int bufferSize)
        {
            if (bufferSize <= 0)
            {
                throw new ArgumentException($"Initialized a RingBuffer with size {bufferSize}.");
            }

            if (bufferSize == Size)
            {
                return;
            }

            int newItemCount = Math.Min(Count, bufferSize);
            int newHeadIndex = newItemCount - 1;

            T[] newValues = new T[bufferSize];

            for (int i = 0; i < newItemCount; i++)
            {
                newValues[newHeadIndex - i] = this[i];
            }

            values = newValues;
            headIndex = newHeadIndex;
            Count = newItemCount;
        }

        public RingBufferEnum<T> GetRingEnumerator() => new RingBufferEnum<T>(values, Count, headIndex);

        public IEnumerator<T> GetEnumerator() => GetRingEnumerator() as IEnumerator<T>;

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// RingBuffer Enumerator class to enable proper list navigation.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RingBufferEnum<T> : IEnumerator<T>
    {
        public T[] values = null;
        public int availableCount = 0;
        public int headIndex = 0;

        private int index = -1;

        public int Size => values.Length;


        public RingBufferEnum(T[] values, int availableCount, int headIndex)
        {
            this.values = values;
            this.availableCount = availableCount;
            this.headIndex = headIndex;
        }

        public bool MoveNext()
        {
            if (index == -1)
            {
                index = headIndex;
                return true;
            }

            //Avoiding Negative mod issues by adding the cycle length before mod
            index = (Size + index - 1) % Size;

            //We have reached the end of the list if the mod distance from our head to our index is
            //equal to our available count, or if we're pointing at the head again
            return (index != headIndex) &&
                ((headIndex - index + Size) % Size < availableCount);
        }

        public void Reset()
        {
            index = -1;
        }

        object IEnumerator.Current => Current;

        void IDisposable.Dispose() { }

        public T Current
        {
            get
            {
                try
                {
                    return values[index];
                }
                catch (IndexOutOfRangeException)
                {
                    throw new InvalidOperationException();
                }
            }
        }
    }

}