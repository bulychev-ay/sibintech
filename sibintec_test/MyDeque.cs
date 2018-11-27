using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sibintec_test
{
    [Serializable]
    public class MyDeque<T> : IMyDeque<T>, IEnumerable<T>, ICloneable, IComparable
    {
        T[] items;
        int count;
        int head;
        int tail;

        // Конструктор, задаёт начальный размер массива для дека
        public MyDeque()
        {
            items = new T[1];
        }

        // Длина дека
        public int Count
        {
            get { return count; }
        }

        // Свойство. Проверка на пустоту
        public bool IsEmpty
        {
            get { return count == 0; }
        }

        // Проверка на то, есть ли такой item в деке
        public bool Contains(T item)
        {
            for (var i = head; i <= tail; i++)
            {
                if (items[i].Equals(item))
                    return true;
            }
            return false;
        }

        // Удаление элемента с начала
        public T DequeueFirst()
        {
            if (IsEmpty)
                throw new Exception("Дек пуст, удалять нечего");
            else
            {
                T item = items[head];
                Resize(-1, "head");
                --count;
                //items[head] = default(T);
                --tail;
                return item;
            }
        }

        // Удаление элемента с конца
        public T DequeueLast()
        {
            if (IsEmpty)
                throw new Exception("Дек пуст, удалять нечего");
            else
            {
                T item = items[tail];
                Resize(-1, "tail");
                --count;
                --tail;
                return item;
            }
        }

        // Добавление элемента в начало
        public void EnqueueFirst(T item)
        {
            if (IsEmpty)
            {
                head = 0;
                tail = head;
                //if (items.Count() == 0)
                //{
                //    items = new T[1];
                //}
                items[head] = item;
                count++;
            }
            else
            {

                Resize(1, "head");
                tail++;
                //for (var i = items.Count()-2; i > -1; i--)
                //{
                //    items[i + 1] = items[i];
                //}

                items[head] = item;
                
                count++;
            }
        }

        // Добавление элемента в конец
        public void EnqueueLast(T item)
        {
            if (IsEmpty)
            {
                head = 0;
                tail = head;
                //if (items.Count() == 0)
                //{
                //    items = new T[1];
                //}
                items[head] = item;
                //tail++;
                count++;
            }
            else
            {
                //if (++tail == items.Length)

                Resize(1, "tile");
                tail++;
                items[tail] = item;
                count++;
            }
        }

        // Узнать какой элемент в начале дека
        public T PeekFirst()
        {
            if (IsEmpty)
                throw new Exception("Дек пуст");
            return items[head];
        }

        // Узнать какой элемент в конце дека
        public T PeekLast()
        {
            if (IsEmpty)
                throw new Exception("Дек пуст");
            return items[tail];
        }

        // Метод изменения размера дека
        void Resize(int shift, string side)
        {
            int newArraySize = items.Length + shift;
            if (newArraySize < 1)
            {
                newArraySize = 1;
            }

            T[] tempItems = new T[newArraySize];
            int startIndex;
            int endIndex;
            if (shift < 0)
            {
                if (side == "head")
                {
                    startIndex = head - shift;
                    endIndex = tail;
                }
                else
                {
                    startIndex = head;
                    endIndex = tail + shift;
                }
                
            }
            else
            {
                startIndex = head;
                endIndex = tail;
            }

            for (var i = startIndex; i <= endIndex; i++)
            {
                if (side == "head")
                {
                    tempItems[i+shift] = items[i];
                }
                else
                {
                    tempItems[i] = items[i];
                }
            }

            items = tempItems;
        }

        // Очистка дека, возвращение к первоначальному состоянию
        public void Clear()
        {
            if (IsEmpty)
                throw new Exception("Дек пуст, его не за чем чистить");
            else
            {
                for (var i = head; i <= tail; i++)
                    items[i] = default(T);
                count = 0;
                T[] tempItems = new T[0];
                items = tempItems;
            }
        }

        // Реализация интерфейса IEnumerable в котором единственный метод GetEnumerator()
        IEnumerator IEnumerable.GetEnumerator()
        {
            for (int i = head; i <= tail; i++)
                yield return items[i];
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = head; i <= tail; i++)
                yield return items[i];
        }

        // Реализация интерфейса ICloneable в котором единственный метод Clone()
        // Неглубокого копирования будет достаточно
        // потому что в деке нет свойств непримитивных типов
        public object Clone()
        {
            return this.MemberwiseClone();
        }

        // Реализация метода CompareTo
        // метод сравнивает 2 дека по длине
        public int CompareTo(object obj)
        {
            MyDeque<T> md = obj as MyDeque<T>;
            return count.CompareTo(md.Count);
        }

    }
}