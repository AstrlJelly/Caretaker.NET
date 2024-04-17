using System;

namespace CaretakerNET.Economy
{
    public class EconomyHandler
    {
        public class Item(long price, string name, string desc)
        {
            public long Price = price;
            public string Name = name;
            public string Desc = desc;
        }

        private static readonly Item[] shop = [
            new(100, "Rock", "big rock... hrnggg...")
        ];
    }
}