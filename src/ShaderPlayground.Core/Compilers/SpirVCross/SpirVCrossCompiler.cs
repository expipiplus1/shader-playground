using System.Collections.Generic;
using ShaderPlayground.Core.Util;

namespace ShaderPlayground.Core.Compilers.SpirVCross
{
    internal sealed class SpirVCrossCompiler : IShaderCompiler
    {
        public string Name { get; } = CompilerNames.SpirVCross;
        public string DisplayName { get; } = "SPIRV-Cross";
        public string Url { get; } = "https://github.com/KhronosGroup/SPIRV-Cross";
        public string Description { get; } = "Khronos spirv-cross.exe";

        public string[] InputLanguages { get; } = { LanguageNames.SpirV };

        public ShaderCompilerParameter[] Parameters { get; } =
        {
            CommonParameters.CreateVersionParameter("spirv-cross"),
            //CommonParameters.GlslShaderStage,
            CommonParameters.HlslEntryPoint.WithDisplayName("Entry point override").WithDefaultValue(string.Empty), // TODO: Only visible when input language is HLSL?
            CommonParameters.ExtraOptionsParameter,
            CommonParameters.CreateOutputParameter(new[] { LanguageNames.Glsl, LanguageNames.Metal, LanguageNames.Hlsl, LanguageNames.Cpp }),
            new ShaderCompilerParameter("ShaderModel", "Shader model", ShaderCompilerParameterType.ComboBox, ShaderModelOptions, "50").WithFilter(CommonParameters.OutputLanguageParameterName, LanguageNames.Hlsl),
        };

        private static readonly string[] ShaderModelOptions =
        {
            "30",
            "40",
            "50",
            "51",
            "60"
        };

        public ShaderCompilerResult Compile(ShaderCode shaderCode, ShaderCompilerArguments arguments, List<ShaderCompilerArguments> previousCompilerArguments)
        {
            var args = arguments.GetString(CommonParameters.ExtraOptionsParameter.Name);

            var outputLanguage = arguments.GetString(CommonParameters.OutputLanguageParameterName);
            switch (outputLanguage)
            {
                case LanguageNames.Glsl:
                    args += ""; // TODO
                    break;

                case LanguageNames.Metal:
                    args += " --msl";
                    break;

                case LanguageNames.Hlsl:
                    args += " --hlsl";
                    args += $" --shader-model {arguments.GetString("ShaderModel")}";
                    break;

                case LanguageNames.Cpp:
                    args += " --cpp";
                    break;
            }

            var entryPointArg = string.Empty;
            var entryPoint = arguments.GetString("EntryPoint");
            if (!string.IsNullOrWhiteSpace(entryPoint))
            {
                entryPointArg = $" --entry {entryPoint}";
                args += entryPointArg;
            }

            using (var tempFile = TempFile.FromShaderCode(shaderCode))
            {
                var outputPath = $"{tempFile.FilePath}.out";

                ProcessHelper.Run(
                    CommonParameters.GetBinaryPath("spirv-cross", arguments, "spirv-cross.exe"),
                    $"--output \"{outputPath}\" \"{tempFile.FilePath}\" {args}",
                    out var _,
                    out var stdError);

                var hasCompilationErrors = !string.IsNullOrWhiteSpace(stdError);

                var textOutput = FileHelper.ReadAllTextIfExists(outputPath);

                FileHelper.DeleteIfExists(outputPath);

                string reflectionJson = null;
                if (!hasCompilationErrors)
                {
                    ProcessHelper.Run(
                        CommonParameters.GetBinaryPath("spirv-cross", arguments, "spirv-cross.exe"),
                        $"--output \"{outputPath}\" \"{tempFile.FilePath}\" --reflect {entryPointArg}",
                        out var _,
                        out var _);

                    reflectionJson = FileHelper.ReadAllTextIfExists(outputPath);

                    FileHelper.DeleteIfExists(outputPath);
                }

                return new ShaderCompilerResult(
                    !hasCompilationErrors,
                    new ShaderCode(outputLanguage, textOutput),
                    hasCompilationErrors ? (int?) 2 : null,
                    new ShaderCompilerOutput("Output", outputLanguage, textOutput),
                    new ShaderCompilerOutput("Reflection", "JSON", reflectionJson),
                    new ShaderCompilerOutput("Errors", null, hasCompilationErrors ? stdError : "<No compilation errors>"));
            }
        }
    }
}
