using System.Collections.Generic;
using ShaderPlayground.Core.Util;

namespace ShaderPlayground.Core.Compilers.SpirvTools
{
    internal sealed class SpirvOptCompiler : IShaderCompiler
    {
        public string Name { get; } = CompilerNames.SpirvOpt;
        public string DisplayName { get; } = "spirv-opt";
        public string Url { get; } = "https://github.com/KhronosGroup/SPIRV-Tools#optimizer";
        public string Description { get; } = "Processes a SPIR-V binary module, applying transformations in the specified order.";

        public string[] InputLanguages { get; } = { LanguageNames.SpirV };

        public ShaderCompilerParameter[] Parameters { get; } =
        {
            CommonParameters.CreateVersionParameter("spirv-tools"),
            new ShaderCompilerParameter("OptimizeForPerformance", "Optimize for performance", ShaderCompilerParameterType.CheckBox, defaultValue: "true"),
            new ShaderCompilerParameter("OptimizeForSize", "Optimize for size", ShaderCompilerParameterType.CheckBox),
            CommonParameters.CreateOutputParameter(new[] { LanguageNames.SpirV })
        };

        public ShaderCompilerResult Compile(ShaderCode shaderCode, ShaderCompilerArguments arguments, List<ShaderCompilerArguments> previousCompilerArguments)
        {
            var outputLanguage = arguments.GetString(CommonParameters.OutputLanguageParameterName);

            using (var tempFile = TempFile.FromShaderCode(shaderCode))
            {
                var outputPath = $"{tempFile.FilePath}.out";

                var options = string.Empty;
                if (arguments.GetBoolean("OptimizeForPerformance"))
                {
                    options += "-O ";
                }
                if (arguments.GetBoolean("OptimizeForSize"))
                {
                    options += "-Os ";
                }

                ProcessHelper.Run(
                    CommonParameters.GetBinaryPath("spirv-tools", arguments, "spirv-opt.exe"),
                    $"{options} \"{tempFile.FilePath}\" -o \"{outputPath}\"",
                    out var _,
                    out var _);

                var binaryOutput = FileHelper.ReadAllBytesIfExists(outputPath);

                var textOutputPath = $"{tempFile.FilePath}.txt";

                ProcessHelper.Run(
                    CommonParameters.GetBinaryPath("spirv-tools", arguments, "spirv-dis.exe"),
                    $"-o \"{textOutputPath}\" \"{outputPath}\"",
                    out var _,
                    out var _);

                FileHelper.DeleteIfExists(outputPath);

                var textOutput = FileHelper.ReadAllTextIfExists(textOutputPath);

                FileHelper.DeleteIfExists(textOutputPath);

                return new ShaderCompilerResult(
                    true,
                    new ShaderCode(outputLanguage, binaryOutput),
                    null,
                    new ShaderCompilerOutput("Output", outputLanguage, textOutput));
            }
        }
    }
}
