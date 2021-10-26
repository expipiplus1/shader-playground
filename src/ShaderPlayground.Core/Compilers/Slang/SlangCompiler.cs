using System;
using System.Collections.Generic;
using ShaderPlayground.Core.Util;

namespace ShaderPlayground.Core.Compilers.Slang
{
    internal sealed class SlangCompiler : IShaderCompiler
    {
        public string Name { get; } = CompilerNames.Slang;
        public string DisplayName { get; } = "Slang";
        public string Url { get; } = "https://github.com/shader-slang/slang";
        public string Description { get; } = "Slang is a shading language that extends HLSL with new capabilities for building modular, extensible, and high-performance real-time shading systems";

        public string[] InputLanguages { get; } = { LanguageNames.Slang, LanguageNames.Hlsl };

        public ShaderCompilerParameter[] Parameters { get; } =
        {
            CommonParameters.CreateVersionParameter("slang"),
            // The default HLSL entry point (CommonParameters.HlslEntryPoint) uses PSMain, but the Slang sample is a compute shader with "computeMain"
            new ShaderCompilerParameter("EntryPoint", "Entry point", ShaderCompilerParameterType.TextBox, defaultValue: "computeMain"),
            // Default to cs_6_0 as assuming DXIL will be the default target 
            new ShaderCompilerParameter("Profile", "Profile", ShaderCompilerParameterType.ComboBox, ProfileOptions, "cs_6_0"),
            new ShaderCompilerParameter("OptimizationLevel", "Optimization level", ShaderCompilerParameterType.ComboBox, OptimizationLevelOptions, "-O1"),
            CommonParameters.CreateOutputParameter(new[] { LanguageNames.Dxil, LanguageNames.SpirV, LanguageNames.Dxbc, LanguageNames.Hlsl, LanguageNames.Glsl, LanguageNames.Cpp, LanguageNames.Cuda, LanguageNames.Ptx })
        };

        private static readonly string[] OptimizationLevelOptions =
        {
            "-O0",
            "-O1",
            "-O2",
            "-O3",
        };

        private static readonly string[] ProfileOptions =
        {
            // DXBC
            "cs_4_0",
            "cs_4_1",
            "cs_5_0",
            "cs_5_1",
            // DXIL
            "cs_6_0",
            "cs_6_1",
            "cs_6_2",
            "cs_6_3",
            "cs_6_4",
            "cs_6_5",
            "cs_6_6",

            // DXBC
            "ds_5_0",
            "ds_5_1",
            // DXIL
            "ds_6_0",
            "ds_6_1",
            "ds_6_2",
            "ds_6_3",
            "ds_6_4",
            "ds_6_5",
            "ds_6_6",

            // DXBC
            "gs_4_0",
            "gs_4_1",
            "gs_5_0",
            "gs_5_1",
            // DXIL
            "gs_6_0",
            "gs_6_1",
            "gs_6_2",
            "gs_6_3",
            "gs_6_4",
            "gs_6_5",
            "gs_6_6",

            // DXBC
            "hs_5_0",
            "hs_5_1",
            // DXIL
            "hs_6_0",
            "hs_6_1",
            "hs_6_2",
            "hs_6_3",
            "hs_6_4",
            "hs_6_5",
            "hs_6_6",

            // DXBC
            "ps_4_0",
            "ps_4_1",
            "ps_5_0",
            "ps_5_1",
            // DXIL
            "ps_6_0",
            "ps_6_1",
            "ps_6_2",
            "ps_6_3",
            "ps_6_4",
            "ps_6_5",
            "ps_6_6",

            // DXBC
            "vs_4_0",
            "vs_4_1",
            "vs_5_0",
            "vs_5_1",
            // DXIL
            "vs_6_0",
            "vs_6_1",
            "vs_6_2",
            "vs_6_3",
            "vs_6_4",
            "vs_6_5",
            "vs_6_6",

            "glsl_vertex",
            "glsl_tess_control",
            "glsl_tess_eval",
            "glsl_geometry",
            "glsl_fragment",
            "glsl_compute"
        };

        public ShaderCompilerResult Compile(ShaderCode shaderCode, ShaderCompilerArguments arguments, List<ShaderCompilerArguments> previousCompilerArguments)
        {
            var outputLanguage = arguments.GetString(CommonParameters.OutputLanguageParameterName);

            bool DoCompilation(bool binary, out string outputPath, out string stdError)
            {
                var args = $"-entry {arguments.GetString("EntryPoint")}";
                args += $" -profile {arguments.GetString("Profile")}";

                var target = outputLanguage switch
                {
                    LanguageNames.Glsl => "glsl",
                    LanguageNames.Hlsl => "hlsl",
                    LanguageNames.Cuda => "cuda",
                    LanguageNames.Cpp => "cpp",
                    LanguageNames.Ptx => "ptx",
                    LanguageNames.Dxil => binary ? "dxil" : "dxil-assembly",
                    LanguageNames.SpirV => binary ? "spirv" : "spirv-assembly",
                    LanguageNames.Dxbc => binary ? "dxbc" : "dxbc-assembly",
                    _ => throw new InvalidOperationException()
                };
                args += $" -target {target}";

                var optimizationLevel = arguments.GetString("OptimizationLevel");
                args += $" {optimizationLevel}";

                using (var tempFile = TempFile.FromShaderCode(shaderCode))
                {
                    outputPath = $"{tempFile.FilePath}.out";

                    ProcessHelper.Run(
                        CommonParameters.GetBinaryPath("slang", arguments, "slangc.exe"),
                        $"\"{tempFile.FilePath}\" -o \"{outputPath}\" {args}",
                        out _,
                        out stdError);

                    var hasCompilationErrors = !string.IsNullOrWhiteSpace(stdError);

                    return hasCompilationErrors;
                }
            }

            var hasCompilationErrors = DoCompilation(false, out var textOutputPath, out var stdError);
            var textOutput = FileHelper.ReadAllTextIfExists(textOutputPath);
            FileHelper.DeleteIfExists(textOutputPath);

            byte[] binaryOutput = null;
            if (!hasCompilationErrors)
            {
                switch (outputLanguage)
                {
                    case LanguageNames.Dxil:
                    case LanguageNames.SpirV:
                    case LanguageNames.Dxbc:
                        hasCompilationErrors = DoCompilation(true, out var binaryOutputPath, out stdError);
                        binaryOutput = FileHelper.ReadAllBytesIfExists(binaryOutputPath);
                        FileHelper.DeleteIfExists(binaryOutputPath);
                        break;
                }
            }

            return new ShaderCompilerResult(
                !hasCompilationErrors,
                new ShaderCode(outputLanguage, binaryOutput),
                hasCompilationErrors ? (int?)1 : null,
                new ShaderCompilerOutput("Output", outputLanguage, textOutput),
                new ShaderCompilerOutput("Errors", null, hasCompilationErrors ? stdError : "<No compilation errors>"));
        }
    }
}
