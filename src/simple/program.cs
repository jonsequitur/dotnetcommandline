using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Threading.Tasks;

namespace CSE.Samples.CommandLine.Simple
{
    public sealed class App
    {
        /// <summary>
        /// Main entry point
        /// 
        /// Configure and run the web server
        /// </summary>
        /// <param name="args">command line args</param>
        public static async Task<int> Main(string[] args)
        {
            // combine environment variables and command line args
            List<string> cmd = MergeEnvVarIntoCommandArgs(args);

            // build the System.CommandLine.RootCommand
            RootCommand root = BuildRootCommand();

            // handle version
            // ignores all other parameters
            if (cmd.Contains("--version"))
            {
                Console.WriteLine(CSE.SemanticVersion.Version);
                return 0;
            }

            // run the app
            root.Handler = CommandHandler.Create<string, string, bool>(RunApp);
            return await root.InvokeAsync(cmd.ToArray()).ConfigureAwait(false);
        }

        /// <summary>
        /// Combine env vars and command line values
        /// </summary>
        /// <param name="args">command line args</param>
        /// <returns>string</returns>
        public static List<string> MergeEnvVarIntoCommandArgs(string[] args)
        {
            const string KeyVaultName = "KEYVAULT_NAME";
            const string AuthType = "AUTH_TYPE";

            if (args == null)
            {
                args = Array.Empty<string>();
            }

            List<string> cmd = new List<string>(args);

            // read the environment variables
            string kv = Environment.GetEnvironmentVariable(KeyVaultName);
            string auth = Environment.GetEnvironmentVariable(AuthType);

            // add command line param if not present
            // command line options override environment variables
            if (!string.IsNullOrEmpty(kv) && !cmd.Contains("--keyvault-name") && !cmd.Contains("-k"))
            {
                cmd.Add("--keyvault-name");
                cmd.Add(kv);
            }

            // add --auth-type value or default
            if (!cmd.Contains("--auth-type") && !cmd.Contains("-a"))
            {
                cmd.Add("--auth-type");
                cmd.Add(string.IsNullOrEmpty(auth) ? "MSI" : auth);
            }

            // return the list
            return cmd;
        }

        /// <summary>
        /// Build the RootCommand for parsing
        /// </summary>
        /// <returns>RootCommand</returns>
        public static RootCommand BuildRootCommand()
        {
            RootCommand root = new RootCommand
            {
                Name = "simple-app",
                Description = "A simple app",
                TreatUnmatchedTokensAsErrors = true
            };

            // add options
            Option optKv = new Option(new string[] { "-k", "--keyvault-name" }, "The name or URL of the Azure Keyvault")
            {
                Argument = new Argument<string>(),
                Required = true
            };

            optKv.AddValidator(v =>
            {
                if (v.Tokens == null ||
                v.Tokens.Count != 1 ||
                !KeyVaultHelper.ValidateName(v.Tokens[0].Value))
                {
                    return "--keyvault-name must be 3-20 characters [a-z][0-9]";
                }

                return string.Empty;
            });

            Option optAuth = new Option(new string[] { "-a", "--auth-type" }, "Authentication type - MSI CLI VS")
            {
                Argument = new Argument<string>(() => "MSI")
            };

            optAuth.AddValidator(v =>
            {
                const string errorMessage = "--auth-type must be MSI CLI or VS";

                if (v.Tokens == null)
                {
                    return errorMessage;
                }

                // use default value
                if (v.Tokens.Count != 1 && v.Option.Argument.HasDefaultValue)
                {
                    return string.Empty;
                }

                // validate using helper
                if (v.Tokens.Count != 1 || !KeyVaultHelper.ValidateAuthType(v.Tokens[0].Value))
                {
                    return errorMessage;
                }

                return string.Empty;
            });

            // add the options
            root.AddOption(optKv);
            root.AddOption(optAuth);
            root.AddOption(new Option(new string[] { "-d", "--dry-run" }, "Validates configuration"));

            return root;
        }

        /// <summary>
        /// Run the app
        /// </summary>
        /// <param name="keyvaultName">Keyvault Name</param>
        /// <param name="authType">Authentication Type</param>
        /// <param name="dryRun">Dry Run flag</param>
        /// <returns>int</returns>
        public static int RunApp(string keyvaultName, string authType, bool dryRun)
        {
            // convert name to URL
            KeyVaultHelper.BuildKeyVaultConnectionString(keyvaultName, out string kvUrl);

            // don't start the web server
            if (dryRun)
            {
                Console.WriteLine("dry run");
                Console.WriteLine($"\tKeyvault   {kvUrl}");
                Console.WriteLine($"\tAuth Type  {authType?.ToUpperInvariant()}");

                // exit success
                return 0;
            }

            // run the app and exit success
            Console.WriteLine("Web server starting ...");
            Console.WriteLine($"\tKeyvault   {kvUrl}");
            Console.WriteLine($"\tAuth Type  {authType?.ToUpperInvariant()}");
            Console.WriteLine("Exiting ...");
            return 0;
        }
    }
}
