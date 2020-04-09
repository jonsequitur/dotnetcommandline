using System;
using System.Collections.Generic;
using System.CommandLine.Parsing;
using System.Globalization;

namespace CSE.Samples.CommandLine
{
    /// <summary>
    /// Web Validation Test Configuration
    /// </summary>
    public class Config
    {
        public string Server { get; set; }
        public List<string> FileList { get; } = new List<string>();
        public bool RunLoop { get; set; }
        public int SleepMs { get; set; }
        public int Duration { get; set; }
        public bool Random { get; set; }
        public bool Verbose { get; set; }
        public int Timeout { get; set; }
        public string TelemetryKey { get; set; }
        public string TelemetryName { get; set; }
        public int MaxConcurrentRequests { get; set; }
        public int MaxErrors { get; set; }
        public bool DryRun { get; set; }

        /// <summary>
        /// Load config values from CommandLine.ParseResult
        /// </summary>
        /// <param name="parse">ParseResult</param>
        /// <returns>Config</returns>
        public static Config LoadFromCommandLine(ParseResult parse)
        {
            if (parse == null)
            {
                throw new ArgumentNullException(nameof(parse));
            }

            Config config = new Config
            {
                Server = (string)parse.ValueForOption("server")
            };

            // make it easier to pass server value
            if (!config.Server.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                if (config.Server.StartsWith("localhost", StringComparison.OrdinalIgnoreCase) ||
                    config.Server.StartsWith("127.0.0.1", StringComparison.OrdinalIgnoreCase))
                {
                    config.Server = "http://" + config.Server;
                }
                else
                {
                    config.Server = string.Format(CultureInfo.InvariantCulture, $"https://{config.Server}.azurewebsites.net");
                }
            }

            if (parse.ValueForOption("files") != null)
            {
                config.FileList.AddRange((List<string>)parse.ValueForOption("files"));
            }
            else
            {
                // add default file
                config.FileList.Add("baseline.json");
            }

            if (parse.ValueForOption("telemetry") != null)
            {
                if (parse.ValueForOption("telemetry") is List<string> tel && tel.Count == 2)
                {
                    config.TelemetryName = tel[0];
                    config.TelemetryKey = tel[1];
                }
            }

            if (parse.ValueForOption("run-loop") != null)
            {
                config.RunLoop = (bool)parse.ValueForOption("run-loop");
            }
            else
            {
                // default value
                config.RunLoop = false;
            }

            if (parse.ValueForOption("dry-run") != null)
            {
                config.DryRun = (bool)parse.ValueForOption("dry-run");
            }
            else
            {
                // default value
                config.DryRun = false;
            }

            if (parse.ValueForOption("verbose") != null)
            {
                config.Verbose = (bool)parse.ValueForOption("verbose");
            }
            else
            {
                // default value
                config.Verbose = !config.RunLoop;
            }

            if (parse.ValueForOption("random") != null)
            {
                config.Random = (bool)parse.ValueForOption("random");
            }

            if (parse.ValueForOption("sleep") != null)
            {
                config.SleepMs = (int)parse.ValueForOption("sleep");
            }
            else
            {
                // default value
                config.SleepMs = config.RunLoop ? 1000 : 0;
            }

            if (parse.ValueForOption("duration") != null)
            {
                config.Duration = (int)parse.ValueForOption("duration");
            }
            else
            {
                // default value
                config.Duration = 0;
            }

            if (parse.ValueForOption("timeout") != null)
            {
                config.Timeout = (int)parse.ValueForOption("timeout");
            }
            else
            {
                // default value
                config.Timeout = 30;
            }

            if (parse.ValueForOption("max-errors") != null)
            {
                config.MaxErrors = (int)parse.ValueForOption("max-errors");
            }
            else
            {
                // default value
                config.MaxErrors = 10;
            }

            if (parse.ValueForOption("max-concurrent-requests") != null)
            {
                config.MaxConcurrentRequests = (int)parse.ValueForOption("max-concurrent-requests");
            }
            else
            {
                // default value
                config.MaxConcurrentRequests = 100;
            }

            return config;
        }
    }
}
