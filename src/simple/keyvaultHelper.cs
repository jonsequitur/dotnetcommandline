using System;
using System.Collections.Generic;

namespace CSE
{
    /// <summary>
    /// Static helper methods for working with Key Vault
    /// </summary>
    public sealed class KeyVaultHelper
    {
        /// <summary>
        /// Build the Key Vault URL from the name
        /// </summary>
        /// <param name="name">Key Vault Name</param>
        /// <returns>URL to Key Vault</returns>
        public static bool BuildKeyVaultConnectionString(string name, out string keyvaultConnection)
        {
            // validate name
            if (!ValidateName(name))
            {
                keyvaultConnection = null;
                return false;
            }

            keyvaultConnection = name.Trim();

            // build the URL
            if (!keyvaultConnection.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                keyvaultConnection = "https://" + keyvaultConnection;
            }

            if (!keyvaultConnection.EndsWith(".vault.azure.net/", StringComparison.OrdinalIgnoreCase) && !keyvaultConnection.EndsWith(".vault.azure.net", StringComparison.OrdinalIgnoreCase))
            {
                keyvaultConnection += ".vault.azure.net/";
            }

            if (!keyvaultConnection.EndsWith("/", StringComparison.OrdinalIgnoreCase))
            {
                keyvaultConnection += "/";
            }

            return true;
        }

        /// <summary>
        /// Validate the authentication type
        /// </summary>
        /// <param name="authType">string</param>
        /// <returns>bool</returns>
        public static bool ValidateAuthType(string authType)
        {
            // valid authentication types
            List<string> validAuthTypes = new List<string> { "MSI", "CLI", "VS" };

            // validate authType
            return !string.IsNullOrWhiteSpace(authType) && validAuthTypes.Contains(authType.ToUpperInvariant());
        }

        /// <summary>
        /// Validate the keyvault name
        /// </summary>
        /// <param name="name">string</param>
        /// <returns>bool</returns>
        public static bool ValidateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }
            name = name.Trim();

            if (name.Length < 3 || name.Length > 20)
            {
                return false;
            }

            return true;
        }
    }
}
