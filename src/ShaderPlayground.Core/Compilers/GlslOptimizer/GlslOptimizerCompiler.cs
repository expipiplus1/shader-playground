using System.Collections.Generic;
using ShaderPlayground.Core.Util;

namespace ShaderPlayground.Core.Compilers.GlslOptimizer
{
    internal sealed class GlslOptimizerCompiler : IShaderCompiler
    {
        public string Name { get; } = CompilerNames.GlslOptimizer;
        public string DisplayName { get; } = "GLSL optimizer";
        public string Url { get; } = "https://github.com/aras-p/glsl-optimizer";
        public string Description { get; } = "A C++ library that takes GLSL shaders, does some GPU-independent optimizations on them and outputs GLSL or Metal source back.";

        public string[] InputLanguages { get; } = { LanguageNames.Glsl };

        public ShaderCompilerParameter[] Parameters { get; } =
        {
            CommonParameters.CreateVersionParameter("glsl-optimizer"),
            new ShaderCompilerParameter("ShaderType", "Shader type", ShaderCompilerParameterType.ComboBox, ShaderTypeOptions, "Vertex"),
            CommonParameters.CreateOutputParameter(new[] { LanguageNames.Glsl, LanguageNames.Metal })
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
                var outputPath = $"{tempFile.FilePath}.out";
                var errorPath = $"{tempFile.FilePath}.error";

                var targetVersion = outputLanguage == LanguageNames.Metal
                    ? 3 // kGlslTargetMetal
                    : 0; // kGlslTargetOpenGL

                var shaderType = arguments.GetString("ShaderType") == "Vertex"
                    ? 0 // kGlslOptShaderVertex
                    : 1; // kGlslOptShaderFragment

                ProcessHelper.Run(
                    CommonParameters.GetBinaryPath("glsl-optimizer", arguments, "ShaderPlayground.Shims.GlslOptimizer.exe"),
                    $"\"{tempFile.FilePath}\" {targetVersion} {shaderType} \"{outputPath}\" \"{errorPath}\"",
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
                    hasCompilationError ? (int?) 1 : null,
                    new ShaderCompilerOutput("Output", outputLanguage, textOutput),
                    new ShaderCompilerOutput("Build errors", null, errorOutput));
            }
        }
    }
}
