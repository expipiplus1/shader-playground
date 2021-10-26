using System.Collections.Generic;
using ShaderPlayground.Core.Util;

namespace ShaderPlayground.Core.Compilers.Smolv
{
    internal sealed class SmolvToSpirvCompiler : IShaderCompiler
    {
        public string Name { get; } = CompilerNames.SmolvToSpirv;
        public string DisplayName { get; } = "SMOL-V (decode)";
        public string Url { get; } = "https://github.com/aras-p/smol-v";
        public string Description { get; } = "SMOL-V encodes Vulkan/Khronos SPIR-V format programs into a form that is \"smoller\", and is more compressible.";

        public string[] InputLanguages { get; } = { LanguageNames.Smolv };

        public ShaderCompilerParameter[] Parameters { get; } =
        {
            CommonParameters.CreateVersionParameter("smol-v"),
            CommonParameters.CreateOutputParameter(new[] { LanguageNames.SpirV })
        };

        public ShaderCompilerResult Compile(ShaderCode shaderCode, ShaderCompilerArguments arguments, List<ShaderCompilerArguments> previousCompilerArguments)
        {
            var outputLanguage = arguments.GetString(CommonParameters.OutputLanguageParameterName);

            using (var tempFile = TempFile.FromShaderCode(shaderCode))
            {
                var outputPath = $"{tempFile.FilePath}.out";

                ProcessHelper.Run(
                    CommonParameters.GetBinaryPath("smol-v", arguments, "ShaderPlayground.Shims.Smolv.exe"),
                    $"\"{tempFile.FilePath}\" 1 0 \"{outputPath}\"",
                    out var stdOutput,
                    out var _);

                var binaryOutput = FileHelper.ReadAllBytesIfExists(outputPath);

                FileHelper.DeleteIfExists(outputPath);

                var hasCompilationError = string.IsNullOrEmpty(stdOutput);

                return new ShaderCompilerResult(
                    !hasCompilationError,
                    !hasCompilationError ? new ShaderCode(outputLanguage, binaryOutput) : null,
                    hasCompilationError ? (int?)1 : null,
                    new ShaderCompilerOutput("Stats", null, stdOutput));
            }
        }
    }
}
