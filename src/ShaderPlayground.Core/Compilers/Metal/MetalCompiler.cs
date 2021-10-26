using System;
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
            new ShaderCompilerParameter("MetalVersion", "Metal Language Version", ShaderCompilerParameterType.ComboBox, MetalVersions, "macos-metal2.4"),
            CommonParameters.CreateOutputParameter(new[] { LanguageNames.MetalIR }),
        };

        private static readonly string[] MetalVersions =
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
        };

        // Corresponds to MetalVersions just above.
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

        public ShaderCompilerResult Compile(ShaderCode shaderCode, ShaderCompilerArguments arguments)
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

                // This is the command line we actually want to run:
                // $"metal.exe -std={arguments.GetString("MetalVersion")} -I \"{includePath}\" -o \"{binaryOutputPath}\" \"{tempFile.FilePath}\"",
                // But because that writes the .air file to a temp folder, it doesn't work on Azure. So I used "-v" to find out the "real"
                // command lines used internally by the front-end driver, and that's what we use here.

                var intermediateOutputPath = $"{tempFile.FilePath}-intermediate.air";

                var versionMetadata = PlatformVersions[Array.IndexOf(MetalVersions, metalVersion)];

                ProcessHelper.Run(
                    CommonParameters.GetBinaryPath("metal", arguments, "metal.exe"),
                    $"-cc1 -triple air64-apple-{versionMetadata.PlatformShort}{versionMetadata.VersionNumber} -Wdeprecated-objc-isa-usage -Werror=deprecated-objc-isa-usage -Werror=implicit-function-declaration -Wuninitialized -Wunused-variable -Wunused-value -Wunused-function -Wsign-compare -Wreturn-type -Wmissing-braces -finclude-default-header -emit-llvm-bc -disable-free -disable-llvm-verifier -discard-value-names -main-file-name {Path.GetFileName(tempFile.FilePath)} -mrelocation-model static -mthread-model posix -mdisable-fp-elim -fno-strict-return -menable-no-infs -menable-no-nans -menable-unsafe-fp-math -fno-signed-zeros -mreassociate -freciprocal-math -fno-trapping-math -ffp-contract=fast -ffast-math -ffinite-math-only -no-integrated-as -faligned-alloc-unavailable -dwarf-column-info -debugger-tuning=lldb -v -I \"{includePath}\" -Wmtl-shader-return-type -Werror=mtl-shader-return-type -std={metalVersion} -fno-dwarf-directory-asm -fno-autolink -fdebug-compilation-dir \"{CommonParameters.GetBinaryPath("metal", arguments)}\" -ferror-limit 19 -fmessage-length 316 -fencode-extended-block-signature -fregister-global-dtors-with-atexit -fobjc-runtime=macosx -fdiagnostics-show-option -fcolor-diagnostics -o \"{intermediateOutputPath}\" -x metal \"{tempFile.FilePath}\"",
                    out _,
                    out _);

                ProcessHelper.Run(
                    CommonParameters.GetBinaryPath("metal", arguments, "air-lld.exe"),
                    $"-arch air64 -{versionMetadata.PlatformFull}_version_min {versionMetadata.VersionNumber} -o \"{binaryOutputPath}\" \"{intermediateOutputPath}\"",
                    out _,
                    out _);

                FileHelper.DeleteIfExists(intermediateOutputPath);

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
