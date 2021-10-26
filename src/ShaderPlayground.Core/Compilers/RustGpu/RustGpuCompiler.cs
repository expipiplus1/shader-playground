using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ShaderPlayground.Core.Util;

namespace ShaderPlayground.Core.Compilers.RustGpu
{
    internal sealed class RustGpuCompiler : IShaderCompiler
    {
        public string Name { get; } = CompilerNames.RustGpu;
        public string DisplayName { get; } = "Rust GPU";
        public string Url { get; } = "https://github.com/EmbarkStudios/rust-gpu";
        public string Description { get; } = "This is a very early stage project to make Rust a first-class language and ecosystem for building GPU code";

        public string[] InputLanguages { get; } = { LanguageNames.Rust };

        public ShaderCompilerParameter[] Parameters { get; } =
        {
            CommonParameters.CreateVersionParameter("rust-gpu"),
            CommonParameters.CreateOutputParameter(new[] { LanguageNames.SpirV })
        };

        public ShaderCompilerResult Compile(ShaderCode shaderCode, ShaderCompilerArguments arguments, List<ShaderCompilerArguments> previousCompilerArguments)
        {
            // Do some basic validation for "securiy".
            var shaderText = shaderCode.Text;
            if (shaderText.Contains("include!") ||
                shaderText.Contains("include_bytes!") || 
                shaderText.Contains("include_str!") ||
                shaderText.Contains("env!") ||
                shaderText.Contains("option_env!"))
            {
                throw new InvalidOperationException("Cannot use macros that access the file system or environment");
            }
            
            using (var tempDirectory = new TempDirectory())
            {
                var binaryPath = CommonParameters.GetBinaryPath("rust-gpu", arguments);

                var srcPath = Path.Combine(tempDirectory.DirectoryPath, "lib.rs");
                File.WriteAllText(srcPath, shaderCode.Text);

                var outDirectory = Path.Combine(tempDirectory.DirectoryPath, "output");
                Directory.CreateDirectory(outDirectory);

                var depsDirectory = Path.Combine(binaryPath, "target-release-deps");
                var spirvDepsDirectory = Path.Combine(binaryPath, "target-spirv-unknown-unknown-release-deps");

                var argsBuilder = new StringBuilder();
                argsBuilder.Append(" --crate-name shader");
                argsBuilder.Append(" --edition=2018");
                argsBuilder.Append($" {srcPath}");
                argsBuilder.Append(" --crate-type dylib");
                argsBuilder.Append(" --emit=dep-info,link");
                argsBuilder.Append(" -C opt-level=3");
                argsBuilder.Append(" -C embed-bitcode=no");
                argsBuilder.Append(" -C metadata=aa40311522c76d6c");
                argsBuilder.Append($" --out-dir {outDirectory}");
                argsBuilder.Append(" --target spirv-unknown-unknown");
                argsBuilder.Append($" -L dependency={depsDirectory}");
                argsBuilder.Append($" -L dependency={spirvDepsDirectory}");
                argsBuilder.Append($" --extern noprelude:compiler_builtins={Path.Combine(spirvDepsDirectory, "libcompiler_builtins-6ea1619052349b38.rlib")}");
                argsBuilder.Append($" --extern noprelude:core={Path.Combine(spirvDepsDirectory, "libcore-1998e626ab31ce74.rlib")}");
                argsBuilder.Append($" --extern spirv_std={Path.Combine(spirvDepsDirectory, "libspirv_std-1809c1c9c8d053c8.rlib")}");
                argsBuilder.Append($" --extern spirv_std_macros={Path.Combine(depsDirectory, "spirv_std_macros-d1d6be1ca8f46a1a.dll")}");
                argsBuilder.Append(" -Z unstable-options");
                argsBuilder.Append($" -Zcodegen-backend={Path.Combine(binaryPath, "rustc_codegen_spirv.dll")}");

                ProcessHelper.Run(
                    Path.Combine(binaryPath, "rustc.exe"),
                    argsBuilder.ToString(),
                    out var _,
                    out var stdError);

                var hasCompilationErrors = !string.IsNullOrWhiteSpace(stdError);

                var binaryOutputPath = Path.Combine(outDirectory, "shader.spv");
                var binaryOutput = FileHelper.ReadAllBytesIfExists(binaryOutputPath);
                
                var textOutputPath = Path.ChangeExtension(binaryOutputPath, ".txt");

                ProcessHelper.Run(
                    CommonParameters.GetBinaryPath("spirv-tools", arguments, "spirv-dis.exe"),
                    $"-o \"{textOutputPath}\" \"{binaryOutputPath}\"",
                    out var _,
                    out var _);

                var textOutput = FileHelper.ReadAllTextIfExists(textOutputPath);

                return new ShaderCompilerResult(
                    !hasCompilationErrors,
                    new ShaderCode(LanguageNames.SpirV, binaryOutput),
                    hasCompilationErrors ? (int?)1 : null,
                    new ShaderCompilerOutput("Output", LanguageNames.SpirvAssembly, textOutput),
                    new ShaderCompilerOutput("Errors", null, hasCompilationErrors ? stdError : "<No compilation errors>"));
            }
        }
    }
}