using System;
using System.IO;
using System.Linq;
using NuGet.Versioning;

namespace ShaderPlayground.Core
{
    internal static class CommonParameters
    {
        private static readonly string[] ShaderStageOptions =
        {
            "vert",
            "tesc",
            "tese",
            "geom",
            "frag",
            "comp"
        };

        public static readonly ShaderCompilerParameter GlslShaderStage = new ShaderCompilerParameter(
            "ShaderStage", 
            "Shader stage", 
            ShaderCompilerParameterType.ComboBox, 
            ShaderStageOptions, 
            defaultValue: "frag");

        public static readonly ShaderCompilerParameter HlslEntryPoint = new ShaderCompilerParameter(
            "EntryPoint",
            "Entry point",
            ShaderCompilerParameterType.TextBox,
            defaultValue: "PSMain");

        public static readonly ShaderCompilerParameter ExtraOptionsParameter = new ShaderCompilerParameter(
            "ExtraOptions",
            "Extra options",
            ShaderCompilerParameterType.TextBox,
            defaultValue: "",
            description: "Arbitrary command-line options");

        public const string OutputLanguageParameterName = "OutputLanguage";

        public static ShaderCompilerParameter CreateOutputParameter(string[] languages)
        {
            return new ShaderCompilerParameter(
                OutputLanguageParameterName,
                "Output format",
                ShaderCompilerParameterType.ComboBox,
                languages,
                languages[0]);
        }

        public const string InputLanguageParameterName = "__InputLanguage";

        public const string VersionParameterName = "Version";

        public static ShaderCompilerParameter CreateVersionParameter(string binaryFolderName)
        {
            var fullBinaryFolderName = Path.Combine(AppContext.BaseDirectory, "Binaries", binaryFolderName);

            var versionDirectories = Directory
                .GetDirectories(fullBinaryFolderName)
                .Select(x => new DirectoryInfo(x));

            var versions = versionDirectories
                .Select(x => x.Name)
                .ToArray();

            // When version directories follow semantic versioning rules (which they mostly do),
            // then sort them by semantic version.
            Array.Sort(versions, (x, y) =>
            {
                x = x.TrimStart('v');
                y = y.TrimStart('v');

                x = x == "beta" ? "1.0-beta" : x;
                y = y == "beta" ? "1.0-beta" : y;

                if (SemanticVersion.TryParse(x, out var semanticX))
                {
                    if (SemanticVersion.TryParse(y, out var semanticY))
                    {
                        return semanticX.CompareTo(semanticY);
                    }
                }

                return x.CompareTo(y);
            });

            var trunkDescription = string.Empty;
            var trunkVersion = versionDirectories.FirstOrDefault(x => x.Name == "trunk");
            if (trunkVersion != null)
            {
                var trunkVersionLastUpdated = trunkVersion.EnumerateFiles().Max(x => x.LastWriteTimeUtc);
                trunkDescription = $"Updated from trunk on {trunkVersionLastUpdated.ToString("yyyy-MM-dd")}";
            }

            return new ShaderCompilerParameter(
                VersionParameterName,
                "Version",
                ShaderCompilerParameterType.ComboBox,
                versions,
                versions.Last(),
                trunkDescription);
        }

        public static string GetBinaryPath(string binaryFolderName, ShaderCompilerArguments arguments)
        {
            return GetBinaryPath(binaryFolderName, arguments.GetString(VersionParameterName));
        }

        public static string GetBinaryPath(string binaryFolderName, string version)
        {
            return Path.Combine(
                AppContext.BaseDirectory,
                "Binaries",
                binaryFolderName,
                version);
        }

        public static string GetBinaryPath(string binaryFolderName, ShaderCompilerArguments arguments, string executableFileName)
        {
            return Path.Combine(
                GetBinaryPath(binaryFolderName, arguments),
                executableFileName);
        }

        public static string GetBinaryPath(string binaryFolderName, string version, string executableFileName)
        {
            return Path.Combine(
                GetBinaryPath(binaryFolderName, version),
                executableFileName);
        }
    }
}
