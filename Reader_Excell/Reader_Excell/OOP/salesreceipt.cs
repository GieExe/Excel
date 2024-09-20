// SalesReceipt.cs
namespace Reader_Excel.OOP
{
    public class SalesReceipt
    {
        private string _txnID;
        private string _ClassRef_ListID;
        private string _ClassRef_FullName;
        private DateTime _txnDate;
        private string _refNumber;
        private decimal _subtotal;
        private decimal _totalAmount;
        private string _status;

        public string TxnID { get => _txnID; set => _txnID = value; }
        public string ClassRef_ListID { get => _ClassRef_ListID; set => _ClassRef_ListID = value; }
        public string ClassRef_FullName { get => _ClassRef_FullName; set => _ClassRef_FullName = value; }
        public DateTime TxnDate { get => _txnDate; set => _txnDate = value; }
        public string RefNumber { get => _refNumber; set => _refNumber = value; }
        public decimal Subtotal { get => _subtotal; set => _subtotal = value; }
        public decimal TotalAmount { get => _totalAmount; set => _totalAmount = value; }
        public string Status { get => _status; set => _status = value; }
      
    }
}
