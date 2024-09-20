using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reader_Excell.OOP
{
    public class Referencetable
    {
        private int _id;
        private DateTime _dateTime;
        private int _inc_num;

        public int Id { get => _id; set => _id = value; }
        public DateTime DateTime { get => _dateTime; set => _dateTime = value; }
        public int Inc_num { get => _inc_num; set => _inc_num = value; }
    }
}
