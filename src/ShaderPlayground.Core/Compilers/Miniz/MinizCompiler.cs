using System.Collections.Generic;
using ShaderPlayground.Core.Util;

namespace ShaderPlayground.Core.Compilers.Miniz
{
    internal sealed class MinizCompiler : IShaderCompiler
    {
        public string Name { get; } = CompilerNames.MinizCompress;
        public string DisplayName { get; } = "zlib (miniz)";
        public string Url { get; } = "https://github.com/richgel999/miniz";
        public string Description { get; } = "Miniz is a lossless, high performance data compression library in a single source file that implements the zlib (RFC 1950) and Deflate (RFC 1951) compressed data format specification standards.";

        public string[] InputLanguages { get; } = { LanguageNames.Dxbc, LanguageNames.Dxil, LanguageNames.Smolv, LanguageNames.SpirV, LanguageNames.Markv };

        public ShaderCompilerParameter[] Parameters { get; } =
        {
            CommonParameters.CreateVersionParameter("miniz"),
            new ShaderCompilerParameter("CompressionLevel", "Compression level", ShaderCompilerParameterType.ComboBox, CompressionLevelOptions, "3"),
            CommonParameters.CreateOutputParameter(new[] { LanguageNames.Zlib })
        };

        private static readonly string[] CompressionLevelOptions =
        {
            "0",
            "1",
            "2",
            "3",
            "4",
            "5",
            "6",
            "7",
            "8",
            "9"
        };

        public ShaderCompilerResult Compile(ShaderCode shaderCode, ShaderCompilerArguments arguments, List<ShaderCompilerArguments> previousCompilerArguments)
        {
            var outputLanguage = arguments.GetString(CommonParameters.OutputLanguageParameterName);

            using (var tempFile = TempFile.FromShaderCode(shaderCode))
            {
                var outputPath = $"{tempFile.FilePath}.out";

                ProcessHelper.Run(
                    CommonParameters.GetBinaryPath("miniz", arguments, "ShaderPlayground.Shims.Miniz.exe"),
                    $"\"{tempFile.FilePath}\" {arguments.GetString("CompressionLevel")} \"{outputPath}\"",
                    out var _,
                    out var _);

                var binaryOutput = FileHelper.ReadAllBytesIfExists(outputPath);

                FileHelper.DeleteIfExists(outputPath);

                return new ShaderCompilerResult(
                    true,
                    new ShaderCode(outputLanguage, binaryOutput),
                    null,
                    new ShaderCompilerOutput("Output", null, $"Compressed {shaderCode.Binary.Length} bytes into {binaryOutput.Length} bytes"));
            }
        }
    }
}
