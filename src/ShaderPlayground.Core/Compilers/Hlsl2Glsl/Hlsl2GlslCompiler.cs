using System;
using System.Collections.Generic;
using ShaderPlayground.Core.Util;

namespace ShaderPlayground.Core.Compilers.Hlsl2Glsl
{
    internal sealed class Hlsl2GlslCompiler : IShaderCompiler
    {
        public string Name { get; } = CompilerNames.Hlsl2Glsl;
        public string DisplayName { get; } = "hlsl2glslfork";
        public string Url { get; } = "https://github.com/aras-p/hlsl2glslfork";
        public string Description { get; } = "HLSL to GLSL language translator based on ATI's HLSL2GLSL. Previously used in Unity.";

        public string[] InputLanguages { get; } = { LanguageNames.Hlsl };

        public ShaderCompilerParameter[] Parameters { get; } =
        {
            CommonParameters.CreateVersionParameter("hlsl2glsl"),
            new ShaderCompilerParameter("ShaderType", "Shader type", ShaderCompilerParameterType.ComboBox, ShaderTypeOptions, "Vertex"),
            new ShaderCompilerParameter("TargetVersion", "Target version", ShaderCompilerParameterType.ComboBox, TargetVersionOptions, "GLSL 140"),
            CommonParameters.HlslEntryPoint,
            CommonParameters.CreateOutputParameter(new[] { LanguageNames.Glsl })
        };

        private static readonly string[] ShaderTypeOptions =
        {
            "Vertex",
            "Fragment"
        };

        private static readonly string[] TargetVersionOptions =
        {
            "GLSL ES 100", // ETargetGLSL_ES_100
            "GLSL 110",    // ETargetGLSL_110
            "GLSL 120",    // ETargetGLSL_120
            "GLSL 140",    // ETargetGLSL_140
            "GLSL ES 300"  // ETargetGLSL_ES_300
        };

        public ShaderCompilerResult Compile(ShaderCode shaderCode, ShaderCompilerArguments arguments, List<ShaderCompilerArguments> previousCompilerArguments)
        {
            var outputLanguage = arguments.GetString(CommonParameters.OutputLanguageParameterName);

            using (var tempFile = TempFile.FromShaderCode(shaderCode))
            {
                var outputPath = $"{tempFile.FilePath}.out";
                var errorPath = $"{tempFile.FilePath}.error";

                var shaderType = arguments.GetString("ShaderType") == "Vertex"
                    ? 0 // EShLangVertex
                    : 1; // EShLangFragment

                var targetVersion = Array.IndexOf(TargetVersionOptions, arguments.GetString("TargetVersion"));

                var entryPoint = arguments.GetString("EntryPoint");

                ProcessHelper.Run(
                    CommonParameters.GetBinaryPath("hlsl2glsl", arguments, "ShaderPlayground.Shims.Hlsl2Glsl.exe"),
                    $"\"{tempFile.FilePath}\" {shaderType} {targetVersion} {entryPoint} \"{outputPath}\" \"{errorPath}\"",
                    out var _,
                    out var _);

                var textOutput = FileHelper.ReadAllTextIfExists(outputPath);
                var errorOutput = FileHelper.ReadAllTextIfExists(errorPath);

                FileHelper.DeleteIfExists(outputPath);
                FileHelper.DeleteIfExists(errorPath);

                var hasCompilationError = !string.IsNullOrEmpty(errorOutput);

                return new ShaderCompilerResult(
                    !hasCompilationError,
                    !hasCompilationError ? new ShaderCode(outputLanguage, textOutput) : null,
                    hasCompilationError ? (int?)1 : null,
                    new ShaderCompilerOutput("Output", outputLanguage, textOutput),
                    new ShaderCompilerOutput("Build errors", null, errorOutput));
            }
        }
    }
}
