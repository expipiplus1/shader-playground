using System;
using System.Collections.Generic;
using System.Text;
using ShaderPlayground.Core.Util;

namespace ShaderPlayground.Core.Compilers.Naga
{
    internal sealed class NagaCompiler : IShaderCompiler
    {
        public string Name { get; } = CompilerNames.Naga;
        public string DisplayName { get; } = "Naga";
        public string Url { get; } = "https://github.com/gfx-rs/naga";
        public string Description { get; } = "The shader translation library for the needs of wgpu and gfx-rs projects.";

        public string[] InputLanguages { get; } = { LanguageNames.SpirV, LanguageNames.Wgsl, LanguageNames.Glsl };

        public ShaderCompilerParameter[] Parameters { get; } =
        {
            CommonParameters.CreateVersionParameter("naga"),
            new ShaderCompilerParameter("ShaderStage", "Shader stage", ShaderCompilerParameterType.ComboBox, ShaderStageOptions, defaultValue: "frag", filter: new ParameterFilter(CommonParameters.InputLanguageParameterName, LanguageNames.Glsl)),
            CommonParameters.CreateOutputParameter(new[] { LanguageNames.SpirV, LanguageNames.Wgsl, LanguageNames.Metal, LanguageNames.Hlsl, LanguageNames.Glsl }),
        };

        private static readonly string[] ShaderStageOptions =
        {
            "vert",
            "frag",
            "comp"
        };

        public ShaderCompilerResult Compile(ShaderCode shaderCode, ShaderCompilerArguments arguments, List<ShaderCompilerArguments> previousCompilerArguments)
        {
            var shaderStage = arguments.GetString(CommonParameters.GlslShaderStage.Name);

            var inputFileExtension = shaderCode.Language switch
            {
                LanguageNames.SpirV => ".spv",
                LanguageNames.Wgsl => ".wgsl",
                LanguageNames.Glsl => $".{shaderStage}",
                _ => throw new InvalidOperationException()
            };

            using var tempFile = TempFile.FromShaderCode(shaderCode, inputFileExtension);

            var outputLanguage = arguments.GetString(CommonParameters.OutputLanguageParameterName);

            var outputFileExtension = outputLanguage switch
            {
                LanguageNames.SpirV => ".spv",
                LanguageNames.Wgsl => ".wgsl",
                LanguageNames.Glsl => $".{shaderStage}",
                LanguageNames.Metal => ".metal",
                LanguageNames.Hlsl => ".hlsl",
                _ => throw new InvalidOperationException()
            };

            var outputPath = $"{tempFile.FilePath}{outputFileExtension}";

            ProcessHelper.Run(
                CommonParameters.GetBinaryPath("naga", arguments, "naga.exe"),
                $"\"{tempFile.FilePath}\" \"{outputPath}\"",
                out var stdOutput,
                out var stdError,
                Encoding.UTF8);

            ShaderCode pipeableCode;
            bool hasCompilationError;
            string textOutput;
            if (outputLanguage == LanguageNames.SpirV)
            {
                var binaryOutput = FileHelper.ReadAllBytesIfExists(outputPath);

                hasCompilationError = binaryOutput == null;

                textOutput = "";
                if (!hasCompilationError)
                {
                    var textOutputPath = $"{tempFile.FilePath}.txt";

                    ProcessHelper.Run(
                        CommonParameters.GetBinaryPath("spirv-tools", "trunk", "spirv-dis.exe"),
                        $"-o \"{textOutputPath}\" \"{outputPath}\"",
                        out var _,
                        out var _);

                    textOutput = FileHelper.ReadAllTextIfExists(textOutputPath);

                    FileHelper.DeleteIfExists(textOutputPath);
                }

                pipeableCode = new ShaderCode(outputLanguage, binaryOutput);
            }
            else
            {
                textOutput = FileHelper.ReadAllTextIfExists(outputPath);
                pipeableCode = new ShaderCode(outputLanguage, textOutput);
                hasCompilationError = textOutput == null;
            }
            
            if (!string.IsNullOrWhiteSpace(stdOutput))
            {
                stdError += Environment.NewLine + stdOutput;
            }

            FileHelper.DeleteIfExists(outputPath);

            return new ShaderCompilerResult(
                !hasCompilationError,
                !hasCompilationError ? pipeableCode : null,
                hasCompilationError ? (int?)1 : null,
                new ShaderCompilerOutput("Assembly", LanguageNames.SpirV, textOutput),
                new ShaderCompilerOutput("Output", null, stdError));
        }
    }
}
