using System;
using System.Collections.Generic;
using System.IO;
using ShaderPlayground.Core.Util;

namespace ShaderPlayground.Core.Compilers.Metal
{
    internal sealed class MetalCompiler : IShaderCompiler
    {
        public string Name { get; } = CompilerNames.Metal;
        public string DisplayName { get; } = "Metal";
        public string Url { get; } = "https://developer.apple.com/documentation/metal/shader_authoring";
        public string Description { get; } = "Metal provides a platform-optimized, low-overhead API for developing the latest 3D pro applications and amazing games using a rich shading language with tighter integration between graphics and compute programs.";

        public string[] InputLanguages { get; } = { LanguageNames.Metal };

        public ShaderCompilerParameter[] Parameters { get; } =
        {
            CommonParameters.CreateVersionParameter("metal"),
            new ShaderCompilerParameter("MetalVersion", "Metal Language Version", ShaderCompilerParameterType.ComboBox, MetalVersions, "metal3.1"),
            CommonParameters.CreateOutputParameter(new[] { LanguageNames.MetalIR }),
        };

        internal static readonly string[] MetalVersions =
        {
            "macos-metal1.0",
            "macos-metal1.1",
            "macos-metal1.2",
            "macos-metal2.0",
            "macos-metal2.1",
            "macos-metal2.2",
            "macos-metal2.3",
            "macos-metal2.4",
            "ios-metal1.0",
            "ios-metal1.1",
            "ios-metal1.2",
            "ios-metal2.0",
            "ios-metal2.1",
            "ios-metal2.2",
            "ios-metal2.3",
            "ios-metal2.4",
            "metal3.0",
            "metal3.1",
        };

        public ShaderCompilerResult Compile(ShaderCode shaderCode, ShaderCompilerArguments arguments, List<ShaderCompilerArguments> previousCompilerArguments)
        {
            using var tempFile = TempFile.FromShaderCode(shaderCode);

            var outputPath = $"{tempFile.FilePath}.ll";

            var includePath = Path.Combine(CommonParameters.GetBinaryPath("metal", arguments), "include", "metal");

            var metalVersion = arguments.GetString("MetalVersion");

            ProcessHelper.Run(
                CommonParameters.GetBinaryPath("metal", arguments, "metal.exe"),
                $"-std={metalVersion} -S -emit-llvm -I \"{includePath}\" -o \"{outputPath}\" \"{tempFile.FilePath}\"",
                out _,
                out var stdError);

            var textOutput = FileHelper.ReadAllTextIfExists(outputPath);

            FileHelper.DeleteIfExists(outputPath);

            var hasCompilationError = textOutput == null;

            byte[] binaryOutput = null;
            if (!hasCompilationError)
            {
                var binaryOutputPath = $"{tempFile.FilePath}.air";

                ProcessHelper.Run(
                    CommonParameters.GetBinaryPath("metal", arguments, "metal.exe"),
                    $"-std={arguments.GetString("MetalVersion")} -I \"{includePath}\" -o \"{binaryOutputPath}\" -c \"{tempFile.FilePath}\"",
                    out _,
                    out _);

                binaryOutput = FileHelper.ReadAllBytesIfExists(binaryOutputPath);

                FileHelper.DeleteIfExists(binaryOutputPath);
            }

            return new ShaderCompilerResult(
                !hasCompilationError,
                !hasCompilationError ? new ShaderCode(LanguageNames.MetalIR, binaryOutput) : null,
                hasCompilationError ? (int?)1 : null,
                new ShaderCompilerOutput("Assembly", LanguageNames.MetalIR, textOutput),
                new ShaderCompilerOutput("Output", null, stdError));
        }
    }
}
