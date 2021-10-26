using System.Collections.Generic;
using ShaderPlayground.Core.Util;

namespace ShaderPlayground.Core.Compilers.Zstd
{
    internal sealed class ZstdCompiler : IShaderCompiler
    {
        public string Name { get; } = CompilerNames.ZstdCompress;
        public string DisplayName { get; } = "Zstandard";
        public string Url { get; } = "http://zstd.net";
        public string Description { get; } = "Zstandard is a real-time compression algorithm, providing high compression ratios.";

        public string[] InputLanguages { get; } = { LanguageNames.Dxbc, LanguageNames.Dxil, LanguageNames.Smolv, LanguageNames.SpirV, LanguageNames.Markv };

        public ShaderCompilerParameter[] Parameters { get; } =
        {
            CommonParameters.CreateVersionParameter("zstd"),
            new ShaderCompilerParameter("CompressionLevel", "Compression level", ShaderCompilerParameterType.ComboBox, CompressionLevelOptions, "3"),
            CommonParameters.CreateOutputParameter(new[] { LanguageNames.Zstd })
        };

        private static readonly string[] CompressionLevelOptions =
        {
            "1",
            "2",
            "3",
            "4",
            "5",
            "6",
            "7",
            "8",
            "9",
            "10",
            "11",
            "12",
            "13",
            "14",
            "15",
            "16",
            "17",
            "18",
            "19"
        };

        public ShaderCompilerResult Compile(ShaderCode shaderCode, ShaderCompilerArguments arguments, List<ShaderCompilerArguments> previousCompilerArguments)
        {
            var outputLanguage = arguments.GetString(CommonParameters.OutputLanguageParameterName);

            using (var tempFile = TempFile.FromShaderCode(shaderCode))
            {
                var outputPath = $"{tempFile.FilePath}.out";

                ProcessHelper.Run(
                    CommonParameters.GetBinaryPath("zstd", arguments, "zstd.exe"),
                    $"-{arguments.GetString("CompressionLevel")} -v \"{tempFile.FilePath}\" -o \"{outputPath}\"",
                    out var _,
                    out var stdError);

                var binaryOutput = FileHelper.ReadAllBytesIfExists(outputPath);

                FileHelper.DeleteIfExists(outputPath);

                return new ShaderCompilerResult(
                    true,
                    new ShaderCode(outputLanguage, binaryOutput),
                    null,
                    new ShaderCompilerOutput("Output", null, stdError));
            }
        }
    }
}
