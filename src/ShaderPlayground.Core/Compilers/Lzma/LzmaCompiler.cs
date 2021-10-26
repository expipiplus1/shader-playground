using System.Collections.Generic;
using ShaderPlayground.Core.Util;

namespace ShaderPlayground.Core.Compilers.Lzma
{
    internal sealed class LzmaCompiler : IShaderCompiler
    {
        public string Name { get; } = CompilerNames.LzmaCompress;
        public string DisplayName { get; } = "LZMA";
        public string Url { get; } = "https://www.7-zip.org/sdk.html";
        public string Description { get; } = "The Lempel–Ziv–Markov chain algorithm (LZMA) is an algorithm used to perform lossless data compression.";

        public string[] InputLanguages { get; } = { LanguageNames.Dxbc, LanguageNames.Dxil, LanguageNames.Smolv, LanguageNames.SpirV, LanguageNames.Markv };

        public ShaderCompilerParameter[] Parameters { get; } =
        {
            CommonParameters.CreateVersionParameter("lzma"),
            new ShaderCompilerParameter("CompressionLevel", "Compression level", ShaderCompilerParameterType.ComboBox, CompressionLevelOptions, "5"),
            CommonParameters.CreateOutputParameter(new[] { LanguageNames.Lzma })
        };

        private static readonly string[] CompressionLevelOptions =
        {
            "0",
            "1",
            "2",
            "3",
            "4",
            "5",
            "6"
        };

        public ShaderCompilerResult Compile(ShaderCode shaderCode, ShaderCompilerArguments arguments, List<ShaderCompilerArguments> previousCompilerArguments)
        {
            var outputLanguage = arguments.GetString(CommonParameters.OutputLanguageParameterName);

            using (var tempFile = TempFile.FromShaderCode(shaderCode))
            {
                var outputPath = $"{tempFile.FilePath}.out";

                var compressionLevelArgs = string.Empty;
                switch (arguments.GetString("CompressionLevel"))
                {
                    case "0":
                        compressionLevelArgs = "-a0 -d14 -fb32";
                        break;

                    case "1":
                        compressionLevelArgs = "-a0 -d16 -fb32";
                        break;

                    case "2":
                        compressionLevelArgs = "-a0 -d18 -fb32";
                        break;

                    case "3":
                        compressionLevelArgs = "-a0 -d20 -fb32";
                        break;

                    case "4":
                        compressionLevelArgs = "-a0 -d22 -fb32";
                        break;

                    case "5":
                        compressionLevelArgs = "-a1 -d24 -fb32";
                        break;

                    case "6":
                        compressionLevelArgs = "-a1 -d25 -fb32";
                        break;
                }

                ProcessHelper.Run(
                    CommonParameters.GetBinaryPath("lzma", arguments, "lzma.exe"),
                    $"e \"{tempFile.FilePath}\" \"{outputPath}\" {compressionLevelArgs}",
                    out var stdOutput,
                    out var _);

                var binaryOutput = FileHelper.ReadAllBytesIfExists(outputPath);

                FileHelper.DeleteIfExists(outputPath);

                return new ShaderCompilerResult(
                    true,
                    new ShaderCode(outputLanguage, binaryOutput),
                    null,
                    new ShaderCompilerOutput("Output", null, stdOutput));
            }
        }
    }
}
