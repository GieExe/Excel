using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reader_Excell.OOP
{
    internal class ingredients_table
    {
        private string _ingredient_id;
        private decimal _qty;
        private string _iteminventory_id;
        private string _FullName;

        public string Ingredient_id { get => _ingredient_id; set => _ingredient_id = value; }
        public decimal Qty { get => _qty; set => _qty = value; }
        public string Iteminventory_id { get => _iteminventory_id; set => _iteminventory_id = value; }
        public string FullName { get => _FullName; set => _FullName = value; }
    }
}
