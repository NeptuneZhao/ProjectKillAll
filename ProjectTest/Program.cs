using System;
using System.Collections.Generic;

namespace ProjectTest
{
    internal class Program
    {
        public static void Main()
        {
            List<int> list = [1, 2, 3, 4, 5];
            int j = 0;
            foreach (int i in list)
            {
                Console.WriteLine(j++);
            }
            Console.ReadKey();
        }
    }
}