// SalesReceiptLineDetail.cs
namespace Reader_Excel.OOP
{
    public class SalesReceiptLineDetail
    {
        private string _txnLineID;
        private string _itemRefListID;
        private string _itemRefFullName;
        private string _description;
        private int _quantity;
        private decimal _rate;
        private decimal _amount;
        private string _idKey; // Foreign key to TxnID from SalesReceipt table
        private string _InventorySiteRef_ListID;
        private string _InventorySiteRef_FullName;

        public string TxnLineID { get => _txnLineID; set => _txnLineID = value; }
        public string ItemRefListID { get => _itemRefListID; set => _itemRefListID = value; }
        public string ItemRefFullName { get => _itemRefFullName; set => _itemRefFullName = value; }
        public string Description { get => _description; set => _description = value; }
        public int Quantity { get => _quantity; set => _quantity = value; }
        public decimal Rate { get => _rate; set => _rate = value; }
        public decimal Amount { get => _amount; set => _amount = value; }
        public string IdKey { get => _idKey; set => _idKey = value; }
        public string InventorySiteRef_ListID { get => _InventorySiteRef_ListID; set => _InventorySiteRef_ListID = value; }
        public string InventorySiteRef_FullName { get => _InventorySiteRef_FullName; set => _InventorySiteRef_FullName = value; }
    }
}
