using System;
using System.Globalization;

namespace CSE
{
    /// <summary>
    /// Semantic Versioning
    /// </summary>
    public sealed class SemanticVersion
    {
        // cache the assembly version
        static string _version = string.Empty;

        public static string Version
        {
            get
            {
                if (string.IsNullOrEmpty(_version))
                {
                    // use reflection to get the assembly version
                    string file = System.Reflection.Assembly.GetExecutingAssembly().Location;
                    DateTime dt = System.IO.File.GetCreationTime(file);
                    System.Version aVer = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

                    // use major.minor and the build date as the semver
                    _version = "Version: " + string.Format(CultureInfo.InvariantCulture, $"{aVer.Major}.{aVer.Minor}.{aVer.Build}+{dt.ToString("MMdd.HHmm", CultureInfo.InvariantCulture)}");
                }

                return _version;
            }
        }
    }
}