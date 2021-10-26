using System;
using System.Collections.Generic;
using System.IO;
using ShaderPlayground.Core.Util;

namespace ShaderPlayground.Core.Compilers.IntelShaderAnalyzer
{
    internal sealed class IntelShaderAnalyzerCompiler : IShaderCompiler
    {
        public string Name { get; } = CompilerNames.IntelShaderAnalyzer;
        public string DisplayName { get; } = "Intel Shader Analyzer";
        public string Url { get; } = "https://github.com/GameTechDev/IntelShaderAnalyzer";
        public string Description { get; } = "Intel Shader Analyzer is a tool for offline static analysis of shaders for Intel GPU Architectures.";

        public string[] InputLanguages { get; } = 
        {
            LanguageNames.Hlsl,
            LanguageNames.Dxbc,
            //LanguageNames.Dxil
        };

        public ShaderCompilerParameter[] Parameters { get; } =
        {
            CommonParameters.CreateVersionParameter("intelshaderanalyzer"),
            new ShaderCompilerParameter("Api", "API", ShaderCompilerParameterType.ComboBox, ApiOptions, "dx11", filter: HlslFilter),
            new ShaderCompilerParameter("TargetProfile", "Target profile", ShaderCompilerParameterType.ComboBox, TargetProfileOptions, "ps_5_0", filter: HlslFilter),
            CommonParameters.HlslEntryPoint.WithFilter(HlslFilter)
        };

        private static readonly ParameterFilter HlslFilter = new ParameterFilter(CommonParameters.InputLanguageParameterName, LanguageNames.Hlsl);

        private static readonly string[] ApiOptions =
        {
            "dx11",
            "dx12"
        };

        private static readonly string[] TargetProfileOptions =
        {
            "cs_4_0",
            "cs_4_1",
            "cs_5_0",
            "cs_5_1",
            "ds_5_0",
            "ds_5_1",
            "gs_4_0",
            "gs_4_1",
            "gs_5_0",
            "gs_5_1",
            "hs_5_0",
            "hs_5_1",
            "ps_4_0",
            "ps_4_1",
            "ps_5_0",
            "ps_5_1",
            "vs_4_0",
            "vs_4_1",
            "vs_5_0",
            "vs_5_1"
        };

        public ShaderCompilerResult Compile(ShaderCode shaderCode, ShaderCompilerArguments arguments, List<ShaderCompilerArguments> previousCompilerArguments)
        {
            var args = string.Empty;

            switch (shaderCode.Language)
            {
                case LanguageNames.Hlsl:
                    args += $" --function {arguments.GetString("EntryPoint")}";
                    args += $" --profile {arguments.GetString("TargetProfile")}";
                    args += $" -s hlsl --api {arguments.GetString("Api")}";
                    break;

                case LanguageNames.Dxbc:
                    args += " -s dxbc --api dx11";
                    break;

                case LanguageNames.Dxil:
                    args += " -s dxbc --api dx12";
                    break;

                default:
                    throw new InvalidOperationException();
            }

            using (var tempFile = TempFile.FromShaderCode(shaderCode))
            {
                var outputPrefix = $"{Path.ChangeExtension(tempFile.FilePath, null)}out";

                ProcessHelper.Run(
                    CommonParameters.GetBinaryPath("intelshaderanalyzer", arguments, "IntelShaderAnalyzer.exe"),
                    $"{args} \"{tempFile.FilePath}\" --isa \"{outputPrefix}\"",
                    out var stdOutput,
                    out _);

                var hasCompilationErrors = !string.IsNullOrWhiteSpace(stdOutput);

                var outputFileNamePrefix = Path.GetFileNameWithoutExtension(outputPrefix);

                var outputs = new List<ShaderCompilerOutput>();
                foreach (var file in Directory.GetFiles(Path.GetDirectoryName(outputPrefix), outputFileNamePrefix + "*.asm"))
                {
                    outputs.Add(new ShaderCompilerOutput(
                        Path.GetFileNameWithoutExtension(file).Substring(outputFileNamePrefix.Length),
                        null,
                        File.ReadAllText(file)));

                    File.Delete(file);
                }

                outputs.Add(new ShaderCompilerOutput("Errors", null, hasCompilationErrors ? stdOutput : "<No compilation errors>"));

                return new ShaderCompilerResult(
                    !hasCompilationErrors,
                    null,
                    hasCompilationErrors ? (int?)outputs.Count : null,
                    outputs.ToArray());
            }
        }
    }
}
