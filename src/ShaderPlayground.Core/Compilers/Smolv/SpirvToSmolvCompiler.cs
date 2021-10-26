using System.Collections.Generic;
using ShaderPlayground.Core.Util;

namespace ShaderPlayground.Core.Compilers.Smolv
{
    internal sealed class SpirvToSmolvCompiler : IShaderCompiler
    {
        public string Name { get; } = CompilerNames.SpirvToSmolv;
        public string DisplayName { get; } = "SMOL-V (encode)";
        public string Url { get; } = "https://github.com/aras-p/smol-v";
        public string Description { get; } = "SMOL-V encodes Vulkan/Khronos SPIR-V format programs into a form that is \"smoller\", and is more compressible.";

        public string[] InputLanguages { get; } = { LanguageNames.SpirV };

        public ShaderCompilerParameter[] Parameters { get; } =
        {
            CommonParameters.CreateVersionParameter("smol-v"),
            new ShaderCompilerParameter("StripDebugInfo", "Strip debug info", ShaderCompilerParameterType.CheckBox),
            CommonParameters.CreateOutputParameter(new[] { LanguageNames.Smolv })
        };

        public ShaderCompilerResult Compile(ShaderCode shaderCode, ShaderCompilerArguments arguments, List<ShaderCompilerArguments> previousCompilerArguments)
        {
            var outputLanguage = arguments.GetString(CommonParameters.OutputLanguageParameterName);

            using (var tempFile = TempFile.FromShaderCode(shaderCode))
            {
                var outputPath = $"{tempFile.FilePath}.out";

                var encodeFlags = arguments.GetBoolean("StripDebugInfo") ? 1 : 0;

                ProcessHelper.Run(
                    CommonParameters.GetBinaryPath("smol-v", arguments, "ShaderPlayground.Shims.Smolv.exe"),
                    $"\"{tempFile.FilePath}\" 0 {encodeFlags} \"{outputPath}\"",
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
