using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reader_Excell.OOP
{
    internal class inventoryadjustment
    {
        private string TxnLineID;
        private string TimeCreated;
        private string ClassRef_ListID;
        private string ClassRef_FullName;
        private string RefNumber;

        public string TxnLineID1 { get => TxnLineID; set => TxnLineID = value; }
        public string TimeCreated1 { get => TimeCreated; set => TimeCreated = value; }
        public string ClassRef_ListID1 { get => ClassRef_ListID; set => ClassRef_ListID = value; }
        public string ClassRef_FullName1 { get => ClassRef_FullName; set => ClassRef_FullName = value; }
        public string RefNumber1 { get => RefNumber; set => RefNumber = value; }
    }
}
