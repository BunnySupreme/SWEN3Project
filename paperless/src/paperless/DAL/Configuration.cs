namespace Paperless.DAL
{
    public class Configuration
    {
        #region Properties
        public static string PostgresConnectionString => GetPostgresConnectionString();
        public static string MinioPassword => GetMinioPassword();
        #endregion

        #region Methods
        private static string GetMinioPassword()
        {
            return GetPasswordFromFile("MINIO_ROOT_PASSWORD_FILE", "minio_password.txt", "Minio");
        }

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
                string? testsPasswordPath = null;

                // Navigate up 4 levels for IDE path
                var current = baseDir;
                for (int i = 0; i < 4 && current != null; i++)
                {
                    current = current.Parent;
                }
                if (current != null)
                {
                    idePasswordPath = Path.Combine(current.FullName, "secrets", fileName);
                }

                // Navigate up 6 levels for tests path
                current = baseDir;
                for (int i = 0; i < 6 && current != null; i++)
                {
                    current = current.Parent;
                }
                if (current != null)
                {
                    testsPasswordPath = Path.Combine(current.FullName, "secrets", fileName);
                }

                if (File.Exists(passwordPath))
                {
                    password = File.ReadAllText(passwordPath).Trim();
                }
                else if (idePasswordPath != null && File.Exists(idePasswordPath))
                {
                    password = File.ReadAllText(idePasswordPath).Trim();
                }
                else if (testsPasswordPath != null && File.Exists(testsPasswordPath))
                {
                    password = File.ReadAllText(testsPasswordPath).Trim();
                }
                else
                {
                    throw new FileNotFoundException(
                        $"ERROR: {serviceName} password not found in environment variable or file.\n" +
                        $"  Environment Variable: {envVarName}\n" +
                        $"  passwordPath: {passwordPath}\n" +
                        $"  idePasswordPath: {idePasswordPath ?? "null"}\n" +
                        $"  testsPasswordPath: {testsPasswordPath ?? "null"}");
                }
            }

            return password!;
        }
        #endregion
    }
}