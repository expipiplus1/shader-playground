using System;
using System.Collections.Generic;
using System.Linq;
using ShaderPlayground.Core.Util;

namespace ShaderPlayground.Core.Compilers.Dxc
{
    internal sealed class DxcCompiler : IShaderCompiler
    {
        public string Name { get; } = CompilerNames.Dxc;
        public string DisplayName { get; } = "Microsoft DXC";
        public string Url { get; } = "https://github.com/Microsoft/DirectXShaderCompiler";
        public string Description { get; } = "New open-source HLSL-to-DXIL compiler (dxc.exe)";

        public string[] InputLanguages { get; } = { LanguageNames.Hlsl };

        public ShaderCompilerParameter[] Parameters { get; } = new[]
        {
            CommonParameters.CreateVersionParameter("dxc"),
            new ShaderCompilerParameter("TargetProfile", "Target profile", ShaderCompilerParameterType.ComboBox, TargetProfileOptions, "ps_6_0"),
            CommonParameters.HlslEntryPoint.WithFilter("TargetProfile", EntryPointFilter),
            new ShaderCompilerParameter("Enable16BitTypes", "Enable 16-bit types", ShaderCompilerParameterType.CheckBox, description: "Enable 16bit types and disable min precision types. Available in HLSL 2018 and shader model 6.2", filter: new ParameterFilter("TargetProfile", Enable16BitTypesFilters)),
            new ShaderCompilerParameter("DisableOptimizations", "Disable optimizations", ShaderCompilerParameterType.CheckBox),
            new ShaderCompilerParameter("OptimizationLevel", "Optimization level", ShaderCompilerParameterType.ComboBox, OptimizationLevelOptions, "3"),
            CommonParameters.ExtraOptionsParameter,
            CommonParameters.CreateOutputParameter(new[] { LanguageNames.Dxil, LanguageNames.SpirV }),
            new ShaderCompilerParameter("SpirvTarget", "SPIR-V target", ShaderCompilerParameterType.ComboBox, SpirvTargetOptions, "vulkan1.0", filter: new ParameterFilter(CommonParameters.OutputLanguageParameterName, LanguageNames.SpirV)),
        };

        private static readonly string[] EntryPointFilter =
        {
            "as_6_5",
            "as_6_6",
            "cs_6_0",
            "cs_6_1",
            "cs_6_2",
            "cs_6_3",
            "cs_6_4",
            "cs_6_5",
            "cs_6_6",
            "ds_6_0",
            "ds_6_1",
            "ds_6_2",
            "ds_6_3",
            "ds_6_4",
            "ds_6_5",
            "ds_6_6",
            "gs_6_0",
            "gs_6_1",
            "gs_6_2",
            "gs_6_3",
            "gs_6_4",
            "gs_6_5",
            "gs_6_6",
            "hs_6_0",
            "hs_6_1",
            "hs_6_2",
            "hs_6_3",
            "hs_6_4",
            "hs_6_5",
            "hs_6_6",
            "ms_6_5",
            "ms_6_6",
            "ps_6_0",
            "ps_6_1",
            "ps_6_2",
            "ps_6_3",
            "ps_6_4",
            "ps_6_5",
            "ps_6_6",
            "vs_6_0",
            "vs_6_1",
            "vs_6_2",
            "vs_6_3",
            "vs_6_4",
            "vs_6_5",
            "vs_6_6",
        };

        private static readonly string[] TargetProfileOptions = EntryPointFilter.Concat(new[]
        {
            "lib_6_3",
            "lib_6_4",
            "lib_6_5",
            "lib_6_6",
        }).ToArray();

        private static readonly string[] OptimizationLevelOptions =
        {
            "0",
            "1",
            "2",
            "3",
            "4"
        };

        private static readonly string[] SpirvTargetOptions =
        {
            "vulkan1.0",
            "vulkan1.1",
            "vulkan1.1spirv1.4",
            "vulkan1.2",
            "vulkan1.3",
            "universal1.5",
        };

        private static readonly string[] Enable16BitTypesFilters =
        {
            "cs_6_2",
            "cs_6_3",
            "cs_6_4",
            "ds_6_2",
            "ds_6_3",
            "ds_6_4",
            "gs_6_2",
            "gs_6_3",
            "gs_6_4",
            "hs_6_2",
            "hs_6_3",
            "hs_6_4",
            "ps_6_2",
            "ps_6_3",
            "ps_6_4",
            "vs_6_2",
            "vs_6_3",
            "vs_6_4",
            "lib_6_3",
            "lib_6_4",
        };

        public ShaderCompilerResult Compile(ShaderCode shaderCode, ShaderCompilerArguments arguments, List<ShaderCompilerArguments> previousCompilerArguments)
        {
            var entryPoint = arguments.GetString("EntryPoint");
            var targetProfile = arguments.GetString("TargetProfile");
            var disableOptimizations = arguments.GetBoolean("DisableOptimizations");
            var optimizationLevel = Convert.ToInt32(arguments.GetString("OptimizationLevel"));
            var outputLanguage = arguments.GetString(CommonParameters.OutputLanguageParameterName);

            var spirv = (outputLanguage == LanguageNames.SpirV) ? $"-spirv -fspv-target-env={arguments.GetString("SpirvTarget")}" : string.Empty;

            using (var tempFile = TempFile.FromShaderCode(shaderCode))
            {
                var fcPath = $"{tempFile.FilePath}.fc";
                var fePath = $"{tempFile.FilePath}.fe";
                var foPath = $"{tempFile.FilePath}.fo";

                var args = $"{spirv} -T {targetProfile} -O{optimizationLevel} -Fc \"{fcPath}\" -Fe \"{fePath}\" -Fo \"{foPath}\"";

                if (!targetProfile.StartsWith("lib_"))
                {
                    args += $" -E {entryPoint}";
                }

                if (disableOptimizations)
                {
                    args += " -Od";
                }

                if (arguments.GetBoolean("Enable16BitTypes"))
                {
                    args += " -enable-16bit-types";
                }

                args += $" {arguments.GetString(CommonParameters.ExtraOptionsParameter.Name)} \"{tempFile.FilePath}\"";

                var dxcPath = CommonParameters.GetBinaryPath("dxc", arguments, "dxc.exe");
                ProcessHelper.Run(
                    dxcPath, 
                    args,
                    out var stdOutput,
                    out _);

                int? selectedOutputIndex = null;

                var disassembly = FileHelper.ReadAllTextIfExists(fcPath);
                if (string.IsNullOrWhiteSpace(disassembly))
                {
                    disassembly = "<Compilation error occurred>";
                    selectedOutputIndex = 2;
                }

                var binaryOutput = FileHelper.ReadAllBytesIfExists(foPath);
                var buildOutput = FileHelper.ReadAllTextIfExists(fePath);

                if (!string.IsNullOrWhiteSpace(stdOutput))
                {
                    buildOutput += Environment.NewLine + stdOutput;
                }

                FileHelper.DeleteIfExists(fcPath);
                FileHelper.DeleteIfExists(fePath);
                FileHelper.DeleteIfExists(foPath);

                // Run again to get AST (can't be done in combination with output files, above).
                ProcessHelper.Run(
                    dxcPath,
                    $"-T {targetProfile} -E {entryPoint} -ast-dump \"{tempFile.FilePath}\"",
                    out var stdOutputAst,
                    out var _);

                return new ShaderCompilerResult(
                    selectedOutputIndex == null,
                    new ShaderCode(outputLanguage, binaryOutput),
                    selectedOutputIndex,
                    new ShaderCompilerOutput("Disassembly", outputLanguage, disassembly),
                    new ShaderCompilerOutput("AST", null, stdOutputAst),
                    new ShaderCompilerOutput("Build output", null, buildOutput));
            }
        }
    }
}