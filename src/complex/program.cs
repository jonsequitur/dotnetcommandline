using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Globalization;
using System.Threading.Tasks;

namespace CSE.Samples.CommandLine
{
    public sealed class ComplexApp
    {
        /// <summary>
        /// Main entry point
        /// </summary>
        /// <param name="args">Command Line Parms</param>
        public static async Task<int> Main(string[] args)
        {
            // merge environment variables into command line for parsing
            var cmd = MergeEnvVarIntoCommandArgs(args);

            // build the System.CommandLine.RootCommand
            RootCommand root = BuildRootCommand();

            // handle version
            // ignores all other parameters
            if (cmd.Contains("--version"))
            {
                Console.WriteLine(CSE.SemanticVersion.Version);
                return 0;
            }

            // parse the command line
            ParseResult parse = root.Parse(cmd.ToArray());

            // display errors and help
            if (parse.Errors.Count > 0)
            {
                return await root.InvokeAsync(cmd.ToArray()).ConfigureAwait(false);
            }

            // load the config from the ParseResult
            Config config = Config.LoadFromCommandLine(parse);

            if (config.DryRun)
            {
                return DoDryRun(config);
            }

            // run the app
            return ComplexApp.Run(config);
        }

        /// <summary>
        /// Combine env vars and command line values
        /// </summary>
        /// <param name="args">command line args</param>
        /// <returns>string</returns>
        public static List<string> MergeEnvVarIntoCommandArgs(string[] args)
        {
            if (args == null)
            {
                args = Array.Empty<string>();
            }

            // convert array to list
            List<string> cmd = new List<string>(args);

            // environment variable to command line mapping
            Dictionary<string, string> dict = EnvKeys.EnvVarToCommandLineDictionary();

            // check each env var and add to command line if not exists
            foreach (string s in dict.Keys)
            {
                string v = Environment.GetEnvironmentVariable(s);

                // if env var exists
                if (!string.IsNullOrEmpty(v))
                {
                    bool found = false;

                    // split into an array
                    string[] alias = dict[v].Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    // check each command line param
                    foreach(string k in alias)
                    {
                        if (cmd.Contains(k.Trim()))
                        {
                            found = true;
                            break;
                        }
                    }

                    // add if doesn't exist
                    if (!found)
                    {
                        cmd.Add(dict[s]);
                        cmd.Add(v);
                    }
                }
            }

            // files is a list<string>
            if (!cmd.Contains("--files") && !cmd.Contains("-f"))
            {
                if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(EnvKeys.Files)))
                {
                    string[] files = Environment.GetEnvironmentVariable(EnvKeys.Files).Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    if (files.Length > 0)
                    {
                        cmd.Add("--files");

                        foreach (string f in files)
                        {
                            cmd.Add(f.Trim());
                        }
                    }
                }
            }

            // Telemetry uses two env vars but one command line
            // TODO - we should change the command line to use two parameters for simplicity
            if (!cmd.Contains("--telemetry"))
            {
                string tk = Environment.GetEnvironmentVariable(EnvKeys.TelemetryKey);
                string tn = Environment.GetEnvironmentVariable(EnvKeys.TelemetryName);

                // this will throw an error during parse if only one is set
                if (!string.IsNullOrEmpty(tk) || !string.IsNullOrEmpty(tn))
                {
                    cmd.Add("--telemetry");
                    if (!string.IsNullOrEmpty(tn)) cmd.Add(tn);
                    if (!string.IsNullOrEmpty(tk)) cmd.Add(tk);
                }
            }

            // return list
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
                Name = "complex-app",
                Description = "Validate API responses",
                TreatUnmatchedTokensAsErrors = true
            };

            // create options with validators
            Option serverOption = new Option(new string[] { "-s", "--server" }, "Server to test") { Argument = new Argument<string>(), Required = true };
            serverOption.AddValidator(v =>
            {
                const string errorMessage = "--server must be 3 - 20 characters [a-z][0-9]";

                if (v.Tokens == null ||
                v.Tokens.Count != 1 ||
                v.Tokens[0].Value == null ||
                v.Tokens[0].Value.Length < 3 ||
                v.Tokens[0].Value.Length > 20)
                {
                    return errorMessage;
                }

                return string.Empty;
            });

            Option filesOption = new Option(new string[] { "-f", "--files" }, "List of files to test") { Argument = new Argument<List<string>>(() => new List<string> { "baseline.json" }) };
            filesOption.AddValidator(v =>
            {
                string ret = string.Empty;

                if (v.Tokens.Count != 0)
                {
                    foreach (Token t in v.Tokens)
                    {
                        if (!CheckFileExists(t.Value))
                        {
                            if (string.IsNullOrEmpty(ret))
                            {
                                ret = "File not found: " + t.Value;
                            }
                            else
                            {
                                ret = ret.Replace("File ", "Files ", StringComparison.Ordinal);
                                ret += ", " + t.Value;
                            }
                        }
                    }
                }

                return ret;
            });

            Option timeoutOption = new Option(new string[] { "-t", "--timeout" }, "Request timeout (seconds)") { Argument = new Argument<int>(() => 30) };
            timeoutOption.AddValidator(ValidateIntGTEZero);

            Option sleepOption = new Option(new string[] { "-l", "--sleep" }, "Sleep (ms) between each request") { Argument = new Argument<int>() };
            sleepOption.AddValidator(ValidateIntGTEZero);

            Option durationOption = new Option(new string[] { "--duration" }, "Test duration (seconds)  (requires --run-loop)") { Argument = new Argument<int>(() => 0) };
            durationOption.AddValidator(ValidateIntGTEZero);

            Option maxConcurrentOption = new Option(new string[] { "--max-concurrent" }, "Max concurrent requests") { Argument = new Argument<int>(() => 100) };
            maxConcurrentOption.AddValidator(ValidateIntGTEZero);

            Option maxErrorsOption = new Option(new string[] { "--max-errors" }, "Max validation errors") { Argument = new Argument<int>(() => 10) };
            maxErrorsOption.AddValidator(ValidateIntGTEZero);

            Option telemetryOption = new Option(new string[] { "--telemetry" }, "App Insights name and key") { Argument = new Argument<List<string>>() };
            telemetryOption.AddValidator(v =>
            {
                if (v.Tokens.Count != 2)
                {
                    return "--telemetry requires appName and appKey parameters";
                }

                return string.Empty;
            });

            root.AddOption(serverOption);
            root.AddOption(filesOption);
            root.AddOption(sleepOption);
            root.AddOption(new Option(new string[] { "-v", "--verbose" }, "Display verbose results") { Argument = new Argument<bool>() });
            root.AddOption(new Option(new string[] { "-r", "--run-loop" }, "Run test in an infinite loop") { Argument = new Argument<bool>(() => false) });
            root.AddOption(new Option(new string[] { "--random" }, "Run requests randomly (requires --run-loop)") { Argument = new Argument<bool>(() => false) });
            root.AddOption(durationOption);
            root.AddOption(timeoutOption);
            root.AddOption(maxConcurrentOption);
            root.AddOption(maxErrorsOption);
            root.AddOption(telemetryOption);
            root.AddOption(new Option(new string[] { "-d", "--dry-run" }, "Validates configuration") { Argument = new Argument<bool>() });

            // these require access to --run-loop so are added at the root level
            root.AddValidator(ValidateDuration);
            root.AddValidator(ValidateRandom);

            return root;
        }

        // validate --duration based on --run-loop
        static string ValidateDuration(CommandResult result)
        {
            OptionResult runLoop = null;
            OptionResult duration = null;

            foreach (OptionResult c in result.Children)
            {
                if (c.Symbol.Name == "duration")
                {
                    duration = c;
                }
                else if (c.Symbol.Name == "run-loop")
                {
                    runLoop = c;
                }
            }

            if (runLoop == null || runLoop.Token == null || (runLoop.Tokens.Count > 0 && !bool.Parse(runLoop.Tokens[0].Value)))
            {
                if (duration != null && duration.Tokens != null && duration.Tokens.Count > 0 && int.TryParse(duration.Tokens[0].Value, out int d) && d > 0)
                {
                    return "--run-loop must be true to use --duration";
                }
            }

            return string.Empty;
        }

        // validate --random based on --run-loop
        static string ValidateRandom(CommandResult result)
        {
            OptionResult runLoop = null;
            OptionResult random = null;


            foreach (OptionResult c in result.Children)
            {
                if (c.Symbol.Name == "run-loop")
                {
                    runLoop = c;
                }
                else if (c.Symbol.Name == "random")
                {
                    random = c;
                }
            }

            if (runLoop == null || runLoop.Token == null || (runLoop.Tokens.Count > 0 && !bool.Parse(runLoop.Tokens[0].Value)))
            {
                if (random != null && random.Token != null && random.Tokens != null && (random.Tokens.Count == 0 || bool.Parse(random.Tokens[0].Value)))
                {
                    return "--run-loop must be true to use --random";
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// CommandLine validator for integer >= 0
        /// </summary>
        /// <param name="result">OptionResult</param>
        /// <returns>error message</returns>
        static string ValidateIntGTEZero(OptionResult result)
        {
            // nothing to validate
            if (result.Symbol == null || result.Token == null)
            {
                return string.Empty;
            }

            string errorMessage = result.Symbol.Name + " must be an integer >= 0";

            if (result.Tokens == null || result.Tokens.Count != 1)
            {
                return errorMessage;
            }

            // system.commandline will handle the parsing error
            if (!int.TryParse(result.Tokens[0].Value, out int i))
            {
                return string.Empty;
            }

            return i < 0 ? errorMessage : string.Empty;
        }

        /// <summary>
        /// Run the "app"
        /// </summary>
        /// <param name="config">Config</param>
        /// <returns>non-zero on error</returns>
        public static int Run(Config config)
        {
            // run the "app"
            Console.WriteLine("Starting app ...");
            Console.WriteLine("Shutting down ...");

            // return success
            return 0;
        }

        static int DoDryRun(Config config)
        {
            // display the config
            Console.WriteLine("dry run");
            Console.WriteLine($"   Server          {config.Server}");
            Console.WriteLine($"   Files (count)   {config.FileList.Count}");
            Console.WriteLine($"   Run Loop        {config.RunLoop}");
            Console.WriteLine($"   Sleep           {config.SleepMs}");
            Console.WriteLine($"   Duration        {config.Duration}");
            Console.WriteLine($"   Max Concurrent  {config.MaxConcurrentRequests}");
            Console.WriteLine($"   Max Errors      {config.MaxErrors}");
            Console.WriteLine($"   Random          {config.Random}");
            Console.WriteLine($"   Timeout         {config.Timeout}");
            Console.WriteLine($"   Verbose         {config.Verbose}");
            Console.WriteLine($"   Telemetry App   {config.TelemetryName}");
            Console.WriteLine($"   Telemetry Key   {config.TelemetryKey}");

            return 0;
        }

        /// <summary>
        /// Check to see if the file exists in testFiles directory
        /// </summary>
        /// <param name="name">file name</param>
        /// <returns>bool</returns>
        public static bool CheckFileExists(string name)
        {
            return !string.IsNullOrWhiteSpace(name) && System.IO.File.Exists("testFiles/" + name.Trim());
        }
    }
}
