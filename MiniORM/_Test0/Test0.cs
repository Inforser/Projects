namespace _Test0
{
    using System;

    public class Test0
    {
        public static void Main()
        {
            DateTime a = new DateTime();

            object b = a;

            Console.WriteLine(b.GetType().Name);
        }
    }
}