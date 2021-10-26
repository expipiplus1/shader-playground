using System.Collections.Generic;
using ShaderPlayground.Core.Util;

namespace ShaderPlayground.Core.Compilers.Yariv
{
    internal sealed class YarivToSpirvCompiler : IShaderCompiler
    {
        public string Name { get; } = CompilerNames.YarivToSpirv;
        public string DisplayName { get; } = "YARI-V (decode)";
        public string Url { get; } = "https://github.com/sheredom/yari-v";
        public string Description { get; } = "YARI-V is an alternative encoding of the SPIR-V standard that seeks to reduce the bytes required to encode shaders/kernels. In its default mode, it will losslessly convert a SPIR-V shader module into a corresponding YARI-V encoding with an average compression ratio of 34.42%.";

        public string[] InputLanguages { get; } = { LanguageNames.Yariv };

        public ShaderCompilerParameter[] Parameters { get; } =
        {
            CommonParameters.CreateVersionParameter("yari-v"),
            CommonParameters.CreateOutputParameter(new[] { LanguageNames.SpirV })
        };

        public ShaderCompilerResult Compile(ShaderCode shaderCode, ShaderCompilerArguments arguments, List<ShaderCompilerArguments> previousCompilerArguments)
        {
            var outputLanguage = arguments.GetString(CommonParameters.OutputLanguageParameterName);

            using (var tempFile = TempFile.FromShaderCode(shaderCode))
            {
                var outputPath = $"{tempFile.FilePath}.out";

                ProcessHelper.Run(
                    CommonParameters.GetBinaryPath("yari-v", arguments, "ShaderPlayground.Shims.Yariv.exe"),
                    $"\"{tempFile.FilePath}\" 1 0 \"{outputPath}\"",
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
