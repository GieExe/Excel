// ConnectionClass.cs
namespace Reader_Excell.Class
{
    public static class ConnectionClass
    {
        public static string GetConnectionString()
        {
            // Replace the following with your actual connection string
            return "Server=localhost;Database=abcdb;User Id=root;Password=Agentfive5!;Port=3306;Pooling=true;Max Pool Size=100";
        }
    }
}