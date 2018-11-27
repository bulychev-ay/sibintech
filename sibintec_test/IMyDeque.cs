
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sibintec_test
{
    interface IMyDeque<T>
    {
        void EnqueueFirst(T item);
        void EnqueueLast(T item);
        T DequeueFirst();
        T DequeueLast();
        T PeekFirst();
        T PeekLast();
        bool Contains(T item);
    }
}