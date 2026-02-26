using Microsoft.Data.SqlClient;

namespace BackendPrueba.Api.Data
{
    public class DbConnectionFactory
    {
        private readonly IConfiguration _config;

        public DbConnectionFactory(IConfiguration config)
        {
            _config = config;
        }

        public async Task<SqlConnection> CreateOpenConnectionAsync()
        {
            var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();
            return conn;
        }
    }
}
