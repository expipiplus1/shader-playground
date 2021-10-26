using System.Collections.Generic;
using ShaderPlayground.Core.Util;

namespace ShaderPlayground.Core.Compilers.Glslang
{
    internal sealed class GlslangCompiler : IShaderCompiler
    {
        public string Name { get; } = CompilerNames.Glslang;
        public string DisplayName { get; } = "glslang";
        public string Url { get; } = "https://github.com/KhronosGroup/glslang";
        public string Description { get; } = "Khronos glslangvalidator.exe";

        public string[] InputLanguages { get; } = 
        {
            LanguageNames.Glsl,
            LanguageNames.Hlsl
        };

        public ShaderCompilerParameter[] Parameters { get; } =
        {
            CommonParameters.CreateVersionParameter("glslang"),
            new ShaderCompilerParameter("ShaderStage", "Shader stage", ShaderCompilerParameterType.ComboBox, ShaderStageOptions, defaultValue: "frag"),
            new ShaderCompilerParameter("Target", "Target", ShaderCompilerParameterType.ComboBox, TargetOptions, TargetVulkan1_0),
            CommonParameters.HlslEntryPoint.WithFilter(CommonParameters.InputLanguageParameterName, LanguageNames.Hlsl),
            CommonParameters.CreateOutputParameter(new[] { LanguageNames.SpirV })
        };

        private static readonly string[] ShaderStageOptions =
        {
            "vert",
            "tesc",
            "tese",
            "geom",
            "frag",
            "comp",
            "mesh",
            "task",
            "rgen",
            "rint",
            "rahit",
            "rchit",
            "rmiss",
            "rcall",
        };

        private const string TargetVulkan1_0 = "Vulkan 1.0";
        private const string TargetVulkan1_1 = "Vulkan 1.1";
        private const string TargetOpenGL = "OpenGL";
        private const string TargetSpirV1_0 = "spirv1.0";
        private const string TargetSpirV1_1 = "spirv1.1";
        private const string TargetSpirV1_2 = "spirv1.2";
        private const string TargetSpirV1_3 = "spirv1.3";
        private const string TargetSpirV1_4 = "spirv1.4";
        private const string TargetSpirV1_5 = "spirv1.5";

        private static readonly string[] TargetOptions =
        {
            TargetVulkan1_0,
            TargetVulkan1_1,
            TargetOpenGL,
            TargetSpirV1_0,
            TargetSpirV1_1,
            TargetSpirV1_2,
            TargetSpirV1_3,
            TargetSpirV1_4,
            TargetSpirV1_5,
        };

        public ShaderCompilerResult Compile(ShaderCode shaderCode, ShaderCompilerArguments arguments, List<ShaderCompilerArguments> previousCompilerArguments)
        {
            var stage = arguments.GetString("ShaderStage");

            var target = arguments.GetString("Target");
            var targetOption = string.Empty;
            switch (target)
            {
                case TargetVulkan1_0:
                    targetOption = "--target-env vulkan1.0";
                    break;

                case TargetVulkan1_1:
                    targetOption = "--target-env vulkan1.1";
                    break;

                case TargetOpenGL:
                    targetOption = "--target-env opengl";
                    break;

                case TargetSpirV1_0:
                    targetOption = "--target-env spirv1.0 -V";
                    break;

                case TargetSpirV1_1:
                    targetOption = "--target-env spirv1.1 -V";
                    break;

                case TargetSpirV1_2:
                    targetOption = "--target-env spirv1.2 -V";
                    break;

                case TargetSpirV1_3:
                    targetOption = "--target-env spirv1.3 -V";
                    break;

                case TargetSpirV1_4:
                    targetOption = "--target-env spirv1.4 -V";
                    break;

                case TargetSpirV1_5:
                    targetOption = "--target-env spirv1.5 -V";
                    break;
            }

            if (shaderCode.Language == LanguageNames.Hlsl)
            {
                targetOption += " -D";
                targetOption += $" -e {arguments.GetString("EntryPoint")}";
            }

            using (var tempFile = TempFile.FromShaderCode(shaderCode))
            {
                var binaryPath = $"{tempFile.FilePath}.o";

                var args = targetOption + $" -o \"{binaryPath}\" --auto-map-locations";

                var validationErrors = RunGlslValidator(arguments, stage, tempFile, args);
                var spirv = RunGlslValidator(arguments, stage, tempFile, args + " --spirv-dis");
                var ast = RunGlslValidator(arguments, stage, tempFile, args + " -i");

                var binaryOutput = FileHelper.ReadAllBytesIfExists(binaryPath);

                FileHelper.DeleteIfExists(binaryPath);

                var hasValidationErrors = !string.IsNullOrWhiteSpace(validationErrors);

                return new ShaderCompilerResult(
                    !hasValidationErrors,
                    new ShaderCode(LanguageNames.SpirV, binaryOutput),
                    hasValidationErrors ? 2 : (int?) null,
                    new ShaderCompilerOutput("Disassembly", LanguageNames.SpirV, spirv),
                    new ShaderCompilerOutput("AST", null, ast),
                    new ShaderCompilerOutput("Validation", null, hasValidationErrors ? validationErrors : "<No validation errors>"));
            }
        }

        private static string RunGlslValidator(ShaderCompilerArguments arguments, string stage, string codeFilePath, string args)
        {
            ProcessHelper.Run(
                CommonParameters.GetBinaryPath("glslang", arguments, "glslangValidator.exe"),
                $"-S {stage} -d {args} \"{codeFilePath}\"",
                out var stdOutput,
                out var stdError);

            // First line is always full path of input file - remove that.
            string RemoveCodeFilePath(string value)
            {
                return (value != null && value.StartsWith(codeFilePath))
                    ? value.Substring(codeFilePath.Length).Trim()
                    : value;
            }

            stdOutput = RemoveCodeFilePath(stdOutput);
            stdError = RemoveCodeFilePath(stdError);

            if (!string.IsNullOrWhiteSpace(stdError))
            {
                return stdError;
            }

            if (!string.IsNullOrWhiteSpace(stdOutput))
            {
                return stdOutput;
            }

            return null;
        }
    }
}
