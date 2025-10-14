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
            string? password = null;

            // Try password from environment variable
            string? passwordFile = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD_FILE");
            if (!string.IsNullOrEmpty(passwordFile) && File.Exists(passwordFile))
            {
                password = File.ReadAllText(passwordFile).Trim();
            }

            // Else try password from local file
            if (string.IsNullOrEmpty(password))
            {
                string passwordPath = Path.Combine(AppContext.BaseDirectory, "postgres_password.txt");
                string idePasswordPath = Path.Combine(Directory.GetParent(AppContext.BaseDirectory)!.Parent!.Parent!.Parent!.Parent!.FullName, "secrets", "postgres_password.txt");

                if (File.Exists(passwordPath))
                {
                    password = File.ReadAllText(passwordPath).Trim();
                }
                else if (File.Exists(idePasswordPath))
                {
                    password = File.ReadAllText(idePasswordPath).Trim();
                }
                else
                {
                    throw new FileNotFoundException("ERROR: Postgres password not found in environment variable or file.");
                }
            }

            // Get rest of connection details from environment variables or hard-coded defaults
            string host = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "paperless-postgres";
            string port = Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432";
            string database = Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "paperless";
            string username = Environment.GetEnvironmentVariable("POSTGRES_USER") ?? "paperless";

            return $"Host={host};Port={port};Database={database};Username={username};Password={password}";
        }
        #endregion
    }
}