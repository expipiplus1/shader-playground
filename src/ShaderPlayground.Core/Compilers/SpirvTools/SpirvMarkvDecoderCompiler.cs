using System.Collections.Generic;
using ShaderPlayground.Core.Util;

namespace ShaderPlayground.Core.Compilers.SpirvTools
{
    internal sealed class SpirvMarkvDecoderCompiler : IShaderCompiler
    {
        public string Name { get; } = CompilerNames.SpirvMarkvDecoder;
        public string DisplayName { get; } = "spirv-markv (decode)";
        public string Url { get; } = "https://github.com/KhronosGroup/SPIRV-Tools/blob/master/source/comp/markv_codec.cpp";
        public string Description { get; } = "Decodes a SPIR-V binary from a MARK-V binary.";

        public string[] InputLanguages { get; } = { LanguageNames.Markv };

        public ShaderCompilerParameter[] Parameters { get; } =
        {
            CommonParameters.CreateVersionParameter("spirv-tools-legacy"),
            new ShaderCompilerParameter("Model", "Model", ShaderCompilerParameterType.ComboBox, ModelOptions, defaultValue: "shader_lite"),
            CommonParameters.CreateOutputParameter(new[] { LanguageNames.SpirV })
        };

        private static readonly string[] ModelOptions =
        {
            "shader_lite",
            "shader_mid",
            "shader_max"
        };

        public ShaderCompilerResult Compile(ShaderCode shaderCode, ShaderCompilerArguments arguments, List<ShaderCompilerArguments> previousCompilerArguments)
        {
            var outputLanguage = arguments.GetString(CommonParameters.OutputLanguageParameterName);

            using (var tempFile = TempFile.FromShaderCode(shaderCode))
            {
                var outputPath = $"{tempFile.FilePath}.out";

                ProcessHelper.Run(
                    CommonParameters.GetBinaryPath("spirv-tools-legacy", arguments, "spirv-markv.exe"),
                    $"d --comments --model={arguments.GetString("Model")} -o \"{outputPath}\" \"{tempFile.FilePath}\"",
                    out var stdOutput,
                    out var stdError);

                var binaryOutput = FileHelper.ReadAllBytesIfExists(outputPath);

                FileHelper.DeleteIfExists(outputPath);

                return new ShaderCompilerResult(
                    true,
                    new ShaderCode(outputLanguage, binaryOutput),
                    null,
                    new ShaderCompilerOutput("Output", outputLanguage, stdError));
            }
        }
    }
}
