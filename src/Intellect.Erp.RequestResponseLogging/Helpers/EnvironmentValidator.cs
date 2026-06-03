namespace Intellect.Erp.RequestResponseLogging.Helpers
{
    /// <summary>
    /// Provides environment validation helpers for request/response logging.
    /// </summary>
    internal static class EnvironmentValidator
    {
        private static readonly string[] DisallowedEnvironments =
        {
            "Production",
            "Prod",
            "Live"
        };

        /// <summary>
        /// Returns true when the environment name is explicitly allowed.
        /// </summary>
        public static bool IsAllowedEnvironment(string environmentName, IReadOnlyCollection<string> allowedEnvironments)
        {
            if (string.IsNullOrWhiteSpace(environmentName) || allowedEnvironments is null)
            {
                return false;
            }

            return allowedEnvironments
                .Any(value => string.Equals(value, environmentName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Returns true when the environment name matches a disallowed production identifier.
        /// </summary>
        public static bool IsDisallowedEnvironment(string environmentName)
        {
            if (string.IsNullOrWhiteSpace(environmentName))
            {
                return false;
            }

            return DisallowedEnvironments
                .Any(value => string.Equals(value, environmentName, StringComparison.OrdinalIgnoreCase));
        }
    }
}
