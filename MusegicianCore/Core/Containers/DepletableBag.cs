using System;
using System.Collections;
using System.Collections.Generic;
using TASAgency.Math;

namespace TASAgency.Collections.Generic
{
    /// <summary>
    /// An unstable-sort set structure that acts as a select-without-replace bag.
    /// </summary>
    [Serializable]
    public sealed class DepletableBag<T> : IDepletable<T>
    {
        private readonly List<T> values;
        private readonly Random _randomizer = null;

        Random Randomizer => _randomizer ?? ThreadSafeRandom.Rand;

        public DepletableBag(Random randomizer = null)
        {
            values = new List<T>();
            Count = 0;

            _randomizer = randomizer;
        }

        public DepletableBag(
            IEnumerable<T> values,
            bool autoRefill = false,
            Random randomizer = null)
        {
            this.values = new List<T>(values);
            AutoRefill = autoRefill;
            Count = this.values.Count;

            _randomizer = randomizer;
        }

        #region IDepletable<T>

        public bool AutoRefill { get; set; }
        public int TotalCount => values.Count;

        public T PopNext()
        {
            if (Count == 0)
            {
                if (AutoRefill)
                {
                    Count = values.Count;
                }
                else
                {
                    Console.WriteLine("Bag is empty and you tried to pull out an element.");
                    return default;
                }
            }

            int index = Randomizer.Next(0, Count);
            T temp = values[index];

            //Swap the position of the highest available value with the randomly chosen index.
            values[index] = values[Count - 1];
            values[Count - 1] = temp;

            //Decrement Count. Now that we have swapped those values,
            //all unusedValues will be below unusedCount.
            Count--;

            //return our chosen value.
            return temp;
        }

        public bool TryPopNext(out T value)
        {
            if (Count == 0)
            {
                if (AutoRefill)
                {
                    Count = values.Count;
                }
                else
                {
                    value = default;
                    return false;
                }
            }

            int index = Randomizer.Next(0, Count);
            value = values[index];

            //Swap the position of the highest available value with the randomly chosen index.
            values[index] = values[Count - 1];
            values[Count - 1] = value;

            //Decrement Count. Now that we have swapped those values,
            //all unusedValues will be below unusedCount.
            Count--;

            //return our chosen value.
            return true;
        }

        /// <summary>
        /// Fills the bag back up.
        /// </summary>
        public void Reset()
        {
            Count = values.Count;
        }

        public bool DepleteValue(T value)
        {
            const int NOT_FOUND = -1;
            int index = NOT_FOUND;

            for (int i = 0; i < Count; i++)
            {
                if (values[i].Equals(value))
                {
                    index = i;
                    break;
                }
            }

            if (index == NOT_FOUND)
            {
                return false;
            }

            T temp = values[index];

            values[index] = values[Count - 1];
            values[Count - 1] = temp;

            Count--;

            return true;
        }

        public bool DepleteAllValue(T value)
        {
            bool success = false;

            while (DepleteValue(value))
            {
                success = true;
            }

            return success;
        }

        public bool ReplenishValue(T value)
        {
            const int NOT_FOUND = -1;
            int index = NOT_FOUND;

            for (int i = Count; i < TotalCount; i++)
            {
                if (values[i].Equals(value))
                {
                    index = i;
                    break;
                }
            }

            if (index == NOT_FOUND)
            {
                return false;
            }

            T temp = values[index];

            values[index] = values[Count];
            values[Count] = temp;

            Count++;

            return true;
        }

        public bool ReplenishAllValue(T value)
        {
            bool success = false;

            while (ReplenishValue(value))
            {
                success = true;
            }

            return success;
        }

        public bool ContainsAnywhere(T value) => values.Contains(value);
        public IList<T> GetAvailable() => values.GetRange(0, Count);

        public void CopyAllTo(T[] array, int arrayIndex)
        {
            values.CopyTo(
                index: 0,
                array: array,
                arrayIndex: arrayIndex,
                count: System.Math.Min(values.Count, array.Length - arrayIndex));
        }

        public void AddRange(IEnumerable<T> values)
        {
            foreach (T value in values)
            {
                Add(value);
            }
        }

        #endregion IDepletable<T>
        #region ICollection<T>

        public int Count { get; private set; } = 0;

        bool ICollection<T>.IsReadOnly => false;

        public void Add(T value)
        {
            if (Count < values.Count)
            {
                values.Add(values[Count]);
                values[Count] = value;
            }
            else
            {
                values.Add(value);
            }
            Count++;
        }

        public void Clear()
        {
            values.Clear();
            Count = 0;
        }

        public bool Contains(T value)
        {
            for (int i = 0; i < Count; i++)
            {
                if (values[i].Equals(value))
                {
                    return true;
                }
            }

            return false;
        }

        public void CopyTo(T[] dest, int destIndex)
        {
            values.CopyTo(
                index: 0,
                array: dest,
                arrayIndex: destIndex,
                count: System.Math.Min(Count, dest.Length - destIndex));
        }

        public bool Remove(T item)
        {
            int index = values.IndexOf(item);

            if (index > -1)
            {
                values.RemoveAt(index);

                if (index < Count)
                {
                    Count--;
                }

            }

            return index != -1;
        }

        #endregion ICollection<T>
        #region IEnumerable<T>

        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)values).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<T>)values).GetEnumerator();

        #endregion IEnumerable<T>
    }

}