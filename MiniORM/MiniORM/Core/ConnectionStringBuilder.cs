namespace MiniORM.Core
{
    using System.Data.SqlClient;

    public class ConnectionStringBuilder
    {
        private SqlConnectionStringBuilder builder;

        public ConnectionStringBuilder(string databaseName)
        {
            this.builder = new SqlConnectionStringBuilder
            {
                ["Data Source"] = "(local)",
                ["Integrated Security"] = true,
                ["Connect Timeout"] = 1000,
                ["Trusted_Connection"] = true,
                ["Initial Catalog"] = databaseName
            };

            this.ConnectionString = this.builder.ToString();
        }

        public string ConnectionString { get; }
    }
}
