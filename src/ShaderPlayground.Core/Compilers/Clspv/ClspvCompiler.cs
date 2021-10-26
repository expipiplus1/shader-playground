using System;
using System.Collections.Generic;
using ShaderPlayground.Core.Util;

namespace ShaderPlayground.Core.Compilers.Clspv
{
    internal sealed class ClspvCompiler : IShaderCompiler
    {
        public string Name { get; } = CompilerNames.Clspv;
        public string DisplayName { get; } = "Clspv";
        public string Url { get; } = "https://github.com/google/clspv";
        public string Description { get; } = "Clspv is a prototype compiler for a subset of OpenCL C to Vulkan compute shaders.";

        public string[] InputLanguages { get; } = { LanguageNames.OpenCLC };

        public ShaderCompilerParameter[] Parameters { get; } =
        {
            CommonParameters.CreateVersionParameter("clspv"),
            CommonParameters.ExtraOptionsParameter,
            CommonParameters.CreateOutputParameter(new[] { LanguageNames.SpirV }),
        };

        public ShaderCompilerResult Compile(ShaderCode shaderCode, ShaderCompilerArguments arguments, List<ShaderCompilerArguments> previousCompilerArguments)
        {
            using (var tempFile = TempFile.FromShaderCode(shaderCode))
            {
                var outputPath = $"{tempFile.FilePath}.spv";

                ProcessHelper.Run(
                    CommonParameters.GetBinaryPath("clspv", arguments, "clspv.exe"),
                    $"{arguments.GetString(CommonParameters.ExtraOptionsParameter.Name)} \"{tempFile.FilePath}\" -o \"{outputPath}\"",
                    out var stdOutput,
                    out var stdError);

                var binaryOutput = FileHelper.ReadAllBytesIfExists(outputPath);

                var hasCompilationError = binaryOutput == null;

                var textOutput = "";
                if (!hasCompilationError)
                {
                    var textOutputPath = $"{tempFile.FilePath}.txt";

                    ProcessHelper.Run(
                        CommonParameters.GetBinaryPath("spirv-tools", arguments, "spirv-dis.exe"),
                        $"-o \"{textOutputPath}\" \"{outputPath}\"",
                        out var _,
                        out var _);

                    textOutput = FileHelper.ReadAllTextIfExists(textOutputPath);

                    FileHelper.DeleteIfExists(textOutputPath);
                }

                if (!string.IsNullOrWhiteSpace(stdOutput))
                {
                    stdError += Environment.NewLine + stdOutput;
                }

                FileHelper.DeleteIfExists(outputPath);

                return new ShaderCompilerResult(
                    !hasCompilationError,
                    !hasCompilationError ? new ShaderCode(LanguageNames.SpirV, binaryOutput) : null,
                    hasCompilationError ? (int?) 1 : null,
                    new ShaderCompilerOutput("Assembly", LanguageNames.SpirV, textOutput),
                    new ShaderCompilerOutput("Output", null, stdError));
            }
        }
    }
}
