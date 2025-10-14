namespace paperless.DAL
{
    public class Configuration
    {
        #region Properties
        public static string PostgresConnectionString => GetPostgresConnectionString();
        #endregion

        #region Methods
        private static string GetPostgresConnectionString()
        {
            string passwordPath = Path.Combine(AppContext.BaseDirectory, "postgres_password.txt");
            try
            {
                string password = File.ReadAllText(passwordPath).Trim();
                return $"Host=postgres;Port=5432;Database=paperlessdb;Username=paperless;Password={password}";
            }
            catch (Exception ex)
            {
                // Add logging here
                throw new Exception($"ERROR: Could not find/read postgres password file. PasswordPath = {passwordPath}, {ex}");
            }
        }
        #endregion
    }
}