using System.Collections.Generic;
using ShaderPlayground.Core.Util;

namespace ShaderPlayground.Core.Compilers.HlslCc
{
    internal sealed class HlslCcCompiler : IShaderCompiler
    {
        public string Name { get; } = CompilerNames.HlslCc;
        public string DisplayName { get; } = "HLSLcc";
        public string Url { get; } = "https://github.com/Unity-Technologies/HLSLcc";
        public string Description { get; } = "Unity Technologies' DirectX shader bytecode cross compiler";

        public string[] InputLanguages { get; } = { LanguageNames.Dxbc };

        public ShaderCompilerParameter[] Parameters { get; } =
        {
            CommonParameters.CreateVersionParameter("hlslcc"),
            CommonParameters.CreateOutputParameter(new[] { LanguageNames.Glsl, LanguageNames.Metal })
        };

        public ShaderCompilerResult Compile(ShaderCode shaderCode, ShaderCompilerArguments arguments, List<ShaderCompilerArguments> previousCompilerArguments)
        {
            var outputLanguage = arguments.GetString(CommonParameters.OutputLanguageParameterName);

            using (var tempFile = TempFile.FromShaderCode(shaderCode))
            {
                var outputPath = $"{tempFile.FilePath}.out";

                var lang = outputLanguage == LanguageNames.Metal
                    ? 14 // LANG_METAL
                    : 0; // LANG_DEFAULT

                ProcessHelper.Run(
                    CommonParameters.GetBinaryPath("hlslcc", arguments, "ShaderPlayground.Shims.HLSLcc.exe"),
                    $"\"{tempFile.FilePath}\" {lang} \"{outputPath}\"",
                    out var _,
                    out var _);

                var textOutput = FileHelper.ReadAllTextIfExists(outputPath);

                FileHelper.DeleteIfExists(outputPath);

                return new ShaderCompilerResult(
                    true,
                    new ShaderCode(outputLanguage, textOutput),
                    null,
                    new ShaderCompilerOutput("Output", outputLanguage, textOutput));
            }
        }
    }
}
