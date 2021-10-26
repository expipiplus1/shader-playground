using System;
using System.Collections.Generic;
using ShaderPlayground.Core.Util;

namespace ShaderPlayground.Core.Compilers.Angle
{
    internal sealed class AngleCompiler : IShaderCompiler
    {
        public string Name { get; } = CompilerNames.Angle;
        public string DisplayName { get; } = "ANGLE";
        public string Url { get; } = "https://github.com/google/angle";
        public string Description { get; } = "Shader translator used by Google's ANGLE project.";

        public string[] InputLanguages { get; } = { LanguageNames.Glsl };

        public ShaderCompilerParameter[] Parameters { get; } =
        {
            CommonParameters.CreateVersionParameter("angle"),
            CommonParameters.CreateOutputParameter(new[] { LanguageNames.Glsl, LanguageNames.Hlsl })
        };

        public ShaderCompilerResult Compile(ShaderCode shaderCode, ShaderCompilerArguments arguments, List<ShaderCompilerArguments> previousCompilerArguments)
        {
            var outputLanguage = arguments.GetString(CommonParameters.OutputLanguageParameterName);

            using (var tempFile = TempFile.FromShaderCode(shaderCode))
            {
                var outputPath = $"{tempFile.FilePath}.out";
                var errorPath = $"{tempFile.FilePath}.error";

                string outputArg;
                switch (outputLanguage)
                {
                    case LanguageNames.Glsl:
                        outputArg = "e";
                        break;

                    case LanguageNames.Hlsl:
                        outputArg = "h11";
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }

                string RunCompiler(string arg)
                {
                    ProcessHelper.Run(
                        CommonParameters.GetBinaryPath("angle", arguments, "angle_shader_translator.exe"),
                        $"{arg} -b={outputArg} \"{tempFile.FilePath}\"",
                        out var result,
                        out var _);
                    return result;
                }

                var intermediateTree = RunCompiler("-i");
                var translatedCode = RunCompiler("-o");
                var metadata = RunCompiler("-u");

                var hasCompilationError = translatedCode.Contains("\nERROR: ");

                return new ShaderCompilerResult(
                    !hasCompilationError,
                    !hasCompilationError ? new ShaderCode(outputLanguage, translatedCode) : null,
                    hasCompilationError ? (int?) 0 : null,
                    new ShaderCompilerOutput("Output", outputLanguage, translatedCode),
                    new ShaderCompilerOutput("Intermediate tree", null, intermediateTree),
                    new ShaderCompilerOutput("Metadata", null, metadata));
            }
        }
    }
}
