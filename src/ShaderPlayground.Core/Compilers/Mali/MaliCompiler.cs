using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ShaderPlayground.Core.Util;

namespace ShaderPlayground.Core.Compilers.Mali
{
    internal sealed class MaliCompiler : IShaderCompiler
    {
        static MaliCompiler()
        {
            var coreOptions = new List<string>();

            foreach (var versionDirectory in Directory.GetDirectories(Path.Combine(AppContext.BaseDirectory, "Binaries", "mali")))
            {
                var maliscPath = Path.Combine(versionDirectory, "malisc.exe");
                var maliocPath = Path.Combine(versionDirectory, "malioc.exe");

                var isMalisc = File.Exists(maliscPath);

                ProcessHelper.Run(
                    isMalisc ? maliscPath : maliocPath,
                    "--list",
                    out var stdOutput,
                    out var _);

                // Extract cores from output.
                var coreRegex = isMalisc
                    ? new Regex(@"\s+(Mali-[a-zA-Z0-9]+) <")
                    : new Regex(@"(Mali-[a-zA-Z0-9]+) \(");
                var matches = coreRegex.Matches(stdOutput);

                coreOptions.AddRange(matches
                    .Cast<Match>()
                    .Select(x => x.Groups[1].Value));
            }

            CoreOptions = 
                coreOptions
                .Distinct()
                .OrderBy(x => x)
                .ToArray();
        }

        public string Name { get; } = CompilerNames.Mali;
        public string DisplayName { get; } = "Mali offline compiler";
        public string Url { get; } = "https://developer.arm.com/products/software-development-tools/graphics-development-tools/mali-offline-compiler";
        public string Description { get; } = "ARM Mali offline compiler";

        public string[] InputLanguages { get; } = { LanguageNames.Glsl, LanguageNames.SpirV };

        public ShaderCompilerParameter[] Parameters { get; } = new[]
        {
            CommonParameters.CreateVersionParameter("mali"),
            CommonParameters.GlslShaderStage,
            new ShaderCompilerParameter("EntryPoint", "Entry point", ShaderCompilerParameterType.TextBox, defaultValue: "main", description: "Ignored when input language is GLSL.", filter: new ParameterFilter(CommonParameters.VersionParameterName, "6.2.0")),
            new ShaderCompilerParameter("Core", "Core", ShaderCompilerParameterType.ComboBox, CoreOptions, "Mali-G78"),
            new ShaderCompilerParameter("API", "API", ShaderCompilerParameterType.ComboBox, ApiOptions, "vulkan")
        };

        private static readonly string[] CoreOptions;

        private static readonly string[] ApiOptions =
        {
            "opengles",
            "vulkan",
        };

        public ShaderCompilerResult Compile(ShaderCode shaderCode, ShaderCompilerArguments arguments, List<ShaderCompilerArguments> previousCompilerArguments)
        {
            var stage = GetStageFlag(arguments.GetString("ShaderStage"));
            var core = arguments.GetString("Core");

            var maliscBinaryPath = CommonParameters.GetBinaryPath("mali", arguments, "malisc.exe");
            var maliocBinaryPath = CommonParameters.GetBinaryPath("mali", arguments, "malioc.exe");
            var isMalisc = File.Exists(maliscBinaryPath);

            var args = string.Empty;
            if (shaderCode.Language == LanguageNames.SpirV)
            {
                args += "--spirv";

                if (isMalisc)
                {
                    var spirVEntryPoint = arguments.GetString("EntryPoint");
                    args += $" --spirv_entrypoint_name {spirVEntryPoint}";
                }
            }

            if (!isMalisc)
            {
                args += $" --{arguments.GetString("API")}";
            }

            using (var tempFile = TempFile.FromShaderCode(shaderCode))
            {
                ProcessHelper.Run(
                    isMalisc ? maliscBinaryPath : maliocBinaryPath,
                    $"{stage} -c {core} {args} {tempFile.FilePath}",
                    out var stdOutput,
                    out var stdError);

                return new ShaderCompilerResult(
                    true,
                    null,
                    !string.IsNullOrWhiteSpace(stdError) ? (int?)1 : null, 
                    new ShaderCompilerOutput("Output", null, stdOutput),
                    new ShaderCompilerOutput("Errors", null, stdError));
            }
        }

        private static string GetStageFlag(string shaderStage)
        {
            switch (shaderStage)
            {
                case "vert":
                    return "--vertex";

                case "tesc":
                    return "--tessellation_control";

                case "tese":
                    return "--tessellation_evaluation";

                case "geom":
                    return "--geometry";

                case "frag":
                    return "--fragment";

                case "comp":
                    return "--compute";

                default:
                    throw new ArgumentOutOfRangeException(nameof(shaderStage));
            }
        }
    }
}
