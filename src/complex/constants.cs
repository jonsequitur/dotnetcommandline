using System.Collections.Generic;

namespace CSE.Samples.CommandLine
{
    /// <summary>
    /// Environment Variable Keys
    /// </summary>
    public sealed class EnvKeys
    {
        public const string RunLoop = "RUN_LOOP";
        public const string MaxConcurrent = "MAX_CONCURRENT";
        public const string MaxErrors = "MAX_ERRORS";
        public const string Sleep = "SLEEP";
        public const string Verbose = "VERBOSE";
        public const string Files = "FILES";
        public const string Random = "RANDOM";
        public const string Server = "SERVER";
        public const string Duration = "DURATION";
        public const string RequestTimeout = "TIMEOUT";
        public const string TelemetryKey = "TELEMETRY_KEY";
        public const string TelemetryName = "TELEMETRY_NAME";

        public static Dictionary<string, string> EnvVarToCommandLineDictionary()
        {
            return new Dictionary<string, string>
            {
                { Server, "--server -s" },
                { Sleep, "--sleep -l" },
                { Verbose, "--verbose -v" },
                { RunLoop, "--run-loop -r" },
                { Random, "--random" },
                { Duration, "--duration" },
                { RequestTimeout, "--timeout -t" },
                { MaxConcurrent, "--max-concurrent" },
                { MaxErrors, "--max-errors" }
            };
        }
    }
}
