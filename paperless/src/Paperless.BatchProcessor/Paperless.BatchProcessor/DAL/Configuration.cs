namespace Paperless.BatchProcessor.DAL
{
    public class Configuration
    {
        #region Properties
        public static string PostgresConnectionString => GetPostgresConnectionString();
        #endregion

        #region Methods
        private static string GetPostgresConnectionString()
        {
            string password = GetPasswordFromFile("POSTGRES_PASSWORD_FILE", "postgres_password.txt", "Postgres");

            string host = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "localhost";
            string port = Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432";
            string database = Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "paperless";
            string username = Environment.GetEnvironmentVariable("POSTGRES_USER") ?? "paperless";

            return $"Host={host};Port={port};Database={database};Username={username};Password={password}";
        }

        private static string GetPasswordFromFile(string envVarName, string fileName, string serviceName)
        {
            string? password = null;

            // Try password from environment variable
            string? passwordFile = Environment.GetEnvironmentVariable(envVarName);
            if (!string.IsNullOrEmpty(passwordFile) && File.Exists(passwordFile))
            {
                password = File.ReadAllText(passwordFile).Trim();
            }

            // Else try password from local file
            if (string.IsNullOrEmpty(password))
            {
                string passwordPath = Path.Combine(AppContext.BaseDirectory, fileName);

                var baseDir = Directory.GetParent(AppContext.BaseDirectory);
                string? idePasswordPath = null;

                // Navigate up 5 levels for IDE path
                var current = baseDir;
                for (int i = 0; i < 5 && current != null; i++)
                {
                    current = current.Parent;
                }
                if (current != null)
                {
                    idePasswordPath = Path.Combine(current.FullName, "secrets", fileName);
                }
            }

            return password!;
        }
        #endregion
    }
}
