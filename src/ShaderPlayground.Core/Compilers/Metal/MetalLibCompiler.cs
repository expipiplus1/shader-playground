using System;
using System.Collections.Generic;
using System.Linq;
using ShaderPlayground.Core.Util;

namespace ShaderPlayground.Core.Compilers.Metal
{
    internal sealed class MetalLibCompiler : IShaderCompiler
    {
        public string Name { get; } = CompilerNames.MetalLib;
        public string DisplayName { get; } = "Metallib";
        public string Url { get; } = "https://developer.apple.com/documentation/metal/shader_authoring";
        public string Description { get; } = "Metal provides a platform-optimized, low-overhead API for developing the latest 3D pro applications and amazing games using a rich shading language with tighter integration between graphics and compute programs.";

        public string[] InputLanguages { get; } = { LanguageNames.MetalIR };

        public ShaderCompilerParameter[] Parameters { get; } =
        {
            CommonParameters.CreateVersionParameter("metal"),
            CommonParameters.CreateOutputParameter(new[] { LanguageNames.MetalLib }),
        };

        // Corresponds to MetalCompiler.MetalVersions.
        private static readonly VersionMetadata[] PlatformVersions =
        {
            new VersionMetadata("10.11.0", "macosx", "macosx"),
            new VersionMetadata("10.11.0", "macosx", "macosx"),
            new VersionMetadata("10.12.0", "macosx", "macosx"),
            new VersionMetadata("10.13.0", "macosx", "macosx"),
            new VersionMetadata("10.14.0", "macosx", "macosx"),
            new VersionMetadata("10.15.0", "macosx", "macosx"),
            new VersionMetadata("11.0.0", "macosx", "macosx"),
            new VersionMetadata("12.0.0", "macosx", "macosx"),
            new VersionMetadata("8.0.0", "iphoneos", "ios"),
            new VersionMetadata("9.0.0", "iphoneos", "ios"),
            new VersionMetadata("10.0.0", "iphoneos", "ios"),
            new VersionMetadata("11.0.0", "iphoneos", "ios"),
            new VersionMetadata("12.0.0", "iphoneos", "ios"),
            new VersionMetadata("13.0.0", "iphoneos", "ios"),
            new VersionMetadata("14.0.0", "iphoneos", "ios"),
            new VersionMetadata("15.0.0", "iphoneos", "ios"),
        };

        private readonly struct VersionMetadata
        {
            public readonly string VersionNumber;
            public readonly string PlatformShort;
            public readonly string PlatformFull;

            public VersionMetadata(string versionNumber, string platformShort, string platformFull)
            {
                VersionNumber = versionNumber;
                PlatformShort = platformShort;
                PlatformFull = platformFull;
            }
        }

        public ShaderCompilerResult Compile(ShaderCode shaderCode, ShaderCompilerArguments arguments, List<ShaderCompilerArguments> previousCompilerArguments)
        {
            using var tempFile = TempFile.FromShaderCode(shaderCode);

            var outputPath = $"{tempFile.FilePath}.metallib";

            var metalArguments = previousCompilerArguments.Single(x => x.Compiler.Name == CompilerNames.Metal);
            var metalVersion = metalArguments.GetString("MetalVersion");
            var versionMetadata = PlatformVersions[Array.IndexOf(MetalCompiler.MetalVersions, metalVersion)];

            ProcessHelper.Run(
                CommonParameters.GetBinaryPath("metal", arguments, "air-lld.exe"),
                $"-arch air64 -{versionMetadata.PlatformFull}_version_min {versionMetadata.VersionNumber} -o \"{outputPath}\" \"{tempFile.FilePath}\"",
                out _,
                out var stdError);

            var binaryOutput = FileHelper.ReadAllBytesIfExists(outputPath);

            FileHelper.DeleteIfExists(outputPath);

            var hasCompilationError = !string.IsNullOrWhiteSpace(stdError);

            if (!hasCompilationError)
            {
                stdError = "[Compilation successful; download binary below]";
            }

            return new ShaderCompilerResult(
                !hasCompilationError,
                !hasCompilationError ? new ShaderCode(LanguageNames.MetalLib, binaryOutput) : null,
                null,
                new ShaderCompilerOutput("Output", null, stdError));
        }
    }
}
