﻿//inventoryadjustmentlinedetail.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reader_Excell.OOP
{
    internal class inventoryadjustmentlinedetail
    {
        private string TxTLineID;
        private string ItemRef_ListID;
        private string ItemRef_FullName;
        private decimal QuantityDifference;
        private string IDKEY;
        private int SeqNum;

        public string TxTLineID1 { get => TxTLineID; set => TxTLineID = value; }
        public string ItemRef_ListID1 { get => ItemRef_ListID; set => ItemRef_ListID = value; }
        public string ItemRef_FullName1 { get => ItemRef_FullName; set => ItemRef_FullName = value; }
        public string IDKEY1 { get => IDKEY; set => IDKEY = value; }
        public decimal QuantityDifference1 { get => QuantityDifference; set => QuantityDifference = value; }
        public int SeqNum1 { get => SeqNum; set => SeqNum = value; }
    }
}
