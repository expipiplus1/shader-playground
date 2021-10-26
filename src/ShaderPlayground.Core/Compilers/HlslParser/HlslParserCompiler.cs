using System;
using System.Collections.Generic;
using ShaderPlayground.Core.Util;

namespace ShaderPlayground.Core.Compilers.HlslParser
{
    internal sealed class HlslParserCompiler : IShaderCompiler
    {
        public string Name { get; } = CompilerNames.HlslParser;
        public string DisplayName { get; } = "HLSLParser";
        public string Url { get; } = "https://github.com/Thekla/hlslparser";
        public string Description { get; } = "HLSL Parser and Translator for HLSL, GLSL, and MSL.";

        public string[] InputLanguages { get; } = { LanguageNames.Hlsl };

        public ShaderCompilerParameter[] Parameters { get; } =
        {
            CommonParameters.CreateVersionParameter("hlslparser"),
            new ShaderCompilerParameter("ShaderType", "Shader type", ShaderCompilerParameterType.ComboBox, ShaderTypeOptions, "Vertex"),
            CommonParameters.HlslEntryPoint,
            CommonParameters.CreateOutputParameter(new[] { LanguageNames.Hlsl, LanguageNames.Metal, LanguageNames.Glsl })
        };

        private static readonly string[] ShaderTypeOptions =
        {
            "Vertex",
            "Fragment"
        };

        public ShaderCompilerResult Compile(ShaderCode shaderCode, ShaderCompilerArguments arguments, List<ShaderCompilerArguments> previousCompilerArguments)
        {
            var outputLanguage = arguments.GetString(CommonParameters.OutputLanguageParameterName);

            using (var tempFile = TempFile.FromShaderCode(shaderCode))
            {
                var shaderType = arguments.GetString("ShaderType") == "Vertex"
                    ? "-vs"
                    : "-fs";

                string targetLanguage = null;
                switch (outputLanguage)
                {
                    case LanguageNames.Glsl:
                        targetLanguage = "-glsl";
                        break;

                    case LanguageNames.Hlsl:
                        targetLanguage = "-hlsl";
                        break;

                    case LanguageNames.Metal:
                        targetLanguage = "-metal";
                        break;

                    default:
                        throw new InvalidOperationException();
                }

                var entryPoint = arguments.GetString("EntryPoint");

                ProcessHelper.Run(
                    CommonParameters.GetBinaryPath("hlslparser", arguments, "hlslparser.exe"),
                    $"{shaderType} {targetLanguage} \"{tempFile.FilePath}\" {entryPoint}",
                    out var stdOutput,
                    out var _);

                var hasCompilationError = stdOutput.Contains("failed, aborting");

                return new ShaderCompilerResult(
                    !hasCompilationError,
                    !hasCompilationError ? new ShaderCode(outputLanguage, stdOutput) : null,
                    hasCompilationError ? (int?)1 : null,
                    new ShaderCompilerOutput("Output", outputLanguage, stdOutput));
            }
        }
    }
}
