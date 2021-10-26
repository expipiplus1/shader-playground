using System;
using System.Collections.Generic;
using ShaderPlayground.Core.Util;

namespace ShaderPlayground.Core.Compilers.Fxc
{
    public sealed class FxcCompiler : IShaderCompiler
    {
        public string Name { get; } = CompilerNames.Fxc;
        public string DisplayName { get; } = "Microsoft FXC";
        public string Url { get; } = "https://msdn.microsoft.com/en-us/library/windows/desktop/bb232919(v=vs.85).aspx";
        public string Description { get; } = "Legacy HLSL-to-DXBC compiler (fxc.exe)";

        public string[] InputLanguages { get; } = { LanguageNames.Hlsl };

        public ShaderCompilerParameter[] Parameters { get; } = new[]
        {
            CommonParameters.CreateVersionParameter("fxc"),
            new ShaderCompilerParameter("TargetProfile", "Target profile", ShaderCompilerParameterType.ComboBox, TargetProfileOptions, "ps_5_0"),
            CommonParameters.HlslEntryPoint,
            new ShaderCompilerParameter("DisableOptimizations", "Disable optimizations", ShaderCompilerParameterType.CheckBox),
            new ShaderCompilerParameter("OptimizationLevel", "Optimization level", ShaderCompilerParameterType.ComboBox, OptimizationLevelOptions, "1"),
            CommonParameters.CreateOutputParameter(new[] { LanguageNames.Dxbc })
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
            "ps_2_0",
            "ps_2_a",
            "ps_2_b",
            "ps_2_sw",
            "ps_3_0",
            "ps_3_sw",
            "ps_4_0",
            "ps_4_0_level_9_1",
            "ps_4_0_level_9_3",
            "ps_4_1",
            "ps_5_0",
            "ps_5_1",
            "tx_1_0",
            "vs_1_1",
            "vs_2_0",
            "vs_2_a",
            "vs_2_sw",
            "vs_3_0",
            "vs_3_sw",
            "vs_4_0",
            "vs_4_0_level_9_1",
            "vs_4_0_level_9_3",
            "vs_4_1",
            "vs_5_0",
            "vs_5_1"
        };

        private static readonly string[] OptimizationLevelOptions =
        {
            "0",
            "1",
            "2",
            "3"
        };

        public ShaderCompilerResult Compile(ShaderCode shaderCode, ShaderCompilerArguments arguments, List<ShaderCompilerArguments> previousCompilerArguments)
        {
            var entryPoint = arguments.GetString("EntryPoint");
            var targetProfile = arguments.GetString("TargetProfile");
            var disableOptimizations = arguments.GetBoolean("DisableOptimizations");
            var optimizationLevel = Convert.ToInt32(arguments.GetString("OptimizationLevel"));

            using (var tempFile = TempFile.FromShaderCode(shaderCode))
            {
                var fcPath = $"{tempFile.FilePath}.fc";
                var fePath = $"{tempFile.FilePath}.fe";
                var foPath = $"{tempFile.FilePath}.fo";

                var args = $"--target-profile {targetProfile} --entry-point {entryPoint} --optimization-level {optimizationLevel}";
                args += $" --assembly-file \"{fcPath}\" --errors-file \"{fePath}\" --object-file \"{foPath}\"";
                args += $" --file \"{tempFile.FilePath}\"";

                if (disableOptimizations)
                {
                    args += " --disable-optimizations";
                }

                var fxcShimPath = CommonParameters.GetBinaryPath("fxc", arguments, "ShaderPlayground.Shims.Fxc.dll");

                ProcessHelper.Run(
                    "dotnet.exe",
                    $"\"{fxcShimPath}\" {args}",
                    out var _,
                    out var stderr);

                int? selectedOutputIndex = null;

                bool success = true;

                var disassembly = FileHelper.ReadAllTextIfExists(fcPath);
                if (string.IsNullOrWhiteSpace(disassembly))
                {
                    disassembly = "<Compilation error occurred>";
                    selectedOutputIndex = 1;
                    success = false;
                }

                var binaryOutput = FileHelper.ReadAllBytesIfExists(foPath);
                var buildOutput = FileHelper.ReadAllTextIfExists(fePath);
                if (string.IsNullOrWhiteSpace(buildOutput))
                {
                    buildOutput = stderr;
                }

                FileHelper.DeleteIfExists(fcPath);
                FileHelper.DeleteIfExists(fePath);
                FileHelper.DeleteIfExists(foPath);

                return new ShaderCompilerResult(
                    success,
                    new ShaderCode(LanguageNames.Dxbc, binaryOutput),
                    selectedOutputIndex,
                    new ShaderCompilerOutput("Disassembly", LanguageNames.Dxbc, disassembly),
                    new ShaderCompilerOutput("Build output", null, buildOutput));
            }
        }
    }
}
