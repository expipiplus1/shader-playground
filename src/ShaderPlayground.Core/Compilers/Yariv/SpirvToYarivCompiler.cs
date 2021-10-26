using System.Collections.Generic;
using ShaderPlayground.Core.Util;

namespace ShaderPlayground.Core.Compilers.Yariv
{
    internal sealed class SpirvToYarivCompiler : IShaderCompiler
    {
        public string Name { get; } = CompilerNames.SpirvToYariv;
        public string DisplayName { get; } = "YARI-V (encode)";
        public string Url { get; } = "https://github.com/sheredom/yari-v";
        public string Description { get; } = "YARI-V is an alternative encoding of the SPIR-V standard that seeks to reduce the bytes required to encode shaders/kernels. In its default mode, it will losslessly convert a SPIR-V shader module into a corresponding YARI-V encoding with an average compression ratio of 34.42%.";

        public string[] InputLanguages { get; } = { LanguageNames.SpirV };

        public ShaderCompilerParameter[] Parameters { get; } =
        {
            CommonParameters.CreateVersionParameter("yari-v"),
            new ShaderCompilerParameter("Strip", "Strip non-essential information", ShaderCompilerParameterType.CheckBox),
            CommonParameters.CreateOutputParameter(new[] { LanguageNames.Yariv })
        };

        public ShaderCompilerResult Compile(ShaderCode shaderCode, ShaderCompilerArguments arguments, List<ShaderCompilerArguments> previousCompilerArguments)
        {
            var outputLanguage = arguments.GetString(CommonParameters.OutputLanguageParameterName);

            using (var tempFile = TempFile.FromShaderCode(shaderCode))
            {
                var outputPath = $"{tempFile.FilePath}.out";

                var encodeFlags = arguments.GetBoolean("Strip") ? 1 : 0;

                ProcessHelper.Run(
                    CommonParameters.GetBinaryPath("yari-v", arguments, "ShaderPlayground.Shims.Yariv.exe"),
                    $"\"{tempFile.FilePath}\" 0 {encodeFlags} \"{outputPath}\"",
                    out var stdOutput,
                    out var _);

                var binaryOutput = FileHelper.ReadAllBytesIfExists(outputPath);

                FileHelper.DeleteIfExists(outputPath);

                var hasCompilationError = string.IsNullOrEmpty(stdOutput);

                return new ShaderCompilerResult(
                    !hasCompilationError,
                    !hasCompilationError ? new ShaderCode(outputLanguage, binaryOutput) : null,
                    hasCompilationError ? (int?)1 : null);
            }
        }
    }
}
