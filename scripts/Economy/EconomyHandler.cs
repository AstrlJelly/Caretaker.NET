using System;

namespace CaretakerNET.Economy
{
    public class EconomyHandler
    {
        public class Item(string name, string desc, float price)
        {
            public string Name = name;
            public string Desc = desc;
            public float Price = price;
        }
    }
}