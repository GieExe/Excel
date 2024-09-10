// ItemInventory.cs
namespace Reader_Excel
{
    public class ItemInventory
    {
        private string _ListID;
        private string _Name;
        private string _FullName;
        private string _SalesDesc;
        private string _AssetAccountRef_ListID;


        public string ListID { get => _ListID; set => _ListID = value; }
        public string Name { get => _Name; set => _Name = value; }
        public string FullName { get => _FullName; set => _FullName = value; }
        public string SalesDesc { get => _SalesDesc; set => _SalesDesc = value; }
        public string AssetAccountRef_ListID { get => _AssetAccountRef_ListID; set => _AssetAccountRef_ListID = value; }
    }
}
