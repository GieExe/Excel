using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reader_Excell.OOP
{
    internal class ingredientsref
    {
        private int _ID;
        private DateTime _DATE;
        private int _INC_NUM;

        public int ID { get => _ID; set => _ID = value; }
        public DateTime DATE { get => _DATE; set => _DATE = value; }
        public int INC_NUM { get => _INC_NUM; set => _INC_NUM = value; }
    }
}
