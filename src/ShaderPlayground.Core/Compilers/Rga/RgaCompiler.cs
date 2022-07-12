using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CsvHelper;
using ShaderPlayground.Core.Util;

namespace ShaderPlayground.Core.Compilers.Rga
{
    internal sealed class RgaCompiler : IShaderCompiler
    {
        static RgaCompiler()
        {
            var asicOptions = new List<string>();

            foreach (var versionDirectory in Directory.GetDirectories(Path.Combine(AppContext.BaseDirectory, "Binaries", "rga")))
            {
                ProcessHelper.Run(
                    Path.Combine(versionDirectory, "rga.exe"),
                    "-s dx11 --list-asics",
                    out var stdOutput,
                    out var _);

                // Extract ASICs from output.
                var coreRegex = new Regex(@"\n([a-zA-Z0-9 ]+) \(");
                var matches = coreRegex.Matches(stdOutput);

                asicOptions.AddRange(matches
                    .Cast<Match>()
                    .Select(x => x.Groups[1].Value));
            }

            AsicOptions = 
                asicOptions
                .Distinct()
                .OrderBy(x => x)
                .ToArray();
        }

        public string Name { get; } = CompilerNames.Rga;
        public string DisplayName { get; } = "Radeon GPU Analyzer";
        public string Url { get; } = "https://github.com/GPUOpen-Tools/RGA";
        public string Description { get; } = "The Radeon GPU Analyzer (RGA) is an offline compiler and code analysis tool for DirectX shaders, OpenGL shaders, Vulkan shaders and OpenCL kernels.";

        public string[] InputLanguages { get; } = { LanguageNames.Hlsl, LanguageNames.Glsl, LanguageNames.SpirvAssembly };

        public ShaderCompilerParameter[] Parameters { get; } = new[]
        {
            CommonParameters.CreateVersionParameter("rga"),
            new ShaderCompilerParameter("Asic", "ASIC", ShaderCompilerParameterType.ComboBox, AsicOptions, "gfx900"),

            // HLSL
            new ShaderCompilerParameter("DirectXMode", "DirectX mode", ShaderCompilerParameterType.ComboBox, DirectXModeOptions, "dx11", filter: new ParameterFilter(CommonParameters.InputLanguageParameterName, LanguageNames.Hlsl)),
            new ShaderCompilerParameter("TargetProfile", "Target profile", ShaderCompilerParameterType.ComboBox, TargetProfileOptions, "ps_5_0", filter: new ParameterFilter(CommonParameters.InputLanguageParameterName, LanguageNames.Hlsl)),
            CommonParameters.HlslEntryPoint.WithFilter(CommonParameters.InputLanguageParameterName, LanguageNames.Hlsl),

            // GLSL
            new ShaderCompilerParameter("GlslTarget", "Target", ShaderCompilerParameterType.ComboBox, TargetOptions, TargetVulkan, filter: new ParameterFilter(CommonParameters.InputLanguageParameterName, LanguageNames.Glsl)),
            CommonParameters.GlslShaderStage.WithFilter(CommonParameters.InputLanguageParameterName, LanguageNames.Glsl)
        };

        private static readonly string[] AsicOptions;

        private static readonly string[] TargetProfileOptions =
        {
            "cs_4_0",
            "cs_4_1",
            "cs_5_0",
            "cs_6_0",
            "cs_6_1",
            "cs_6_2",
            "cs_6_3",
            "cs_6_4",
            "cs_6_5",
            "ds_5_0",
            "gs_4_0",
            "gs_4_1",
            "gs_5_0",
            "hs_5_0",
            "ps_4_0",
            "ps_4_1",
            "ps_5_0",
            "vs_4_0",
            "vs_4_1",
            "vs_5_0",
        };

        private static readonly string[] DirectXModeOptions =
        {
            "dxr",
            "dx11",
            "dx12",
        };

        private const string TargetOpenGL = "OpenGL";
        private const string TargetVulkan = "Vulkan";

        private static readonly string[] TargetOptions =
        {
            TargetOpenGL,
            TargetVulkan
        };

        public ShaderCompilerResult Compile(ShaderCode shaderCode, ShaderCompilerArguments arguments, List<ShaderCompilerArguments> previousCompilerArguments)
        {
            var version = Version.Parse(arguments.GetString(CommonParameters.VersionParameterName));

            var isVersion21OrLater = version >= new Version(2, 1);
            var isVersion22OrLater = version >= new Version(2, 2);
            var isVersion241OrLater = version >= new Version(2, 4, 1);

            var asic = arguments.GetString("Asic");
            var directXMode = arguments.GetString("DirectXMode");
            var entryPoint = arguments.GetString("EntryPoint");
            var targetProfile = arguments.GetString("TargetProfile");
            var shaderStage = arguments.GetString(CommonParameters.GlslShaderStage.Name);

            using (var tempFile = TempFile.FromShaderCode(shaderCode))
            {
                var outputAnalysisPath = $"{tempFile.FilePath}.analysis";
                var ilPath = $"{tempFile.FilePath}.il";
                var isaPath = $"{tempFile.FilePath}.isa";
                var liveRegPath = $"{tempFile.FilePath}.livereg";
                var cfgPath = $"{tempFile.FilePath}.cfg";

                var args = $"--asic \"{asic}\" --il \"{ilPath}\" --line-numbers --isa \"{isaPath}\" --parse-isa --livereg \"{liveRegPath}\" --cfg \"{cfgPath}\"";

                switch (shaderCode.Language)
                {
                    case LanguageNames.Hlsl:
                        switch (directXMode)
                        {
                            case "dxr":
                                if (!isVersion241OrLater)
                                {
                                    throw new InvalidOperationException("DXR mode is only supported on RGA 2.4.1 and above");
                                }
                                args += " -s dxr --offline";
                                args += $" --export {entryPoint}";
                                args += $" --hlsl \"{tempFile.FilePath}\"";
                                break;

                            case "dx11":
                                args += isVersion22OrLater ? $" -s dx11" : " -s hlsl";
                                args += $" --profile {targetProfile} --function {entryPoint}";
                                args += $" \"{tempFile.FilePath}\"";
                                break;

                            case "dx12":
                                if (!isVersion22OrLater)
                                {
                                    throw new InvalidOperationException("DX12 mode is only supported on RGA 2.2 and above");
                                }
                                args += " -s dx12 --offline";
                                var stage = targetProfile.Substring(0, 2);
                                args += $" --{stage}-model {targetProfile} --{stage}-entry {entryPoint}";
                                args += $" --all-hlsl \"{tempFile.FilePath}\"";
                                break;
                        }
                        
                        break;

                    case LanguageNames.Glsl:
                        switch (arguments.GetString("GlslTarget"))
                        {
                            case TargetOpenGL:
                                args += $" -s opengl --{shaderStage}";
                                break;

                            case TargetVulkan:
                                args += $" -s {(isVersion21OrLater ? "vk-offline" : "vulkan")} --{shaderStage}";
                                break;
                        }
                        args += $" \"{tempFile.FilePath}\"";
                        break;

                    case LanguageNames.SpirvAssembly:
                        args += $" -s {(isVersion21OrLater ? "vk-spv-txt-offline" : "vulkan-spv-text")} --{shaderStage}";
                        args += $" \"{tempFile.FilePath}\"";
                        break;
                }

                var rgaPath = CommonParameters.GetBinaryPath("rga", arguments, "rga.exe");
                var result = ProcessHelper.Run(
                    rgaPath,
                    args,
                    out var stdOutput,
                    out var stdError);

                var actualOutputPathPrefix = Path.GetDirectoryName(tempFile.FilePath);

                string GetActualOutputPath(string extension)
                {
                    if (extension == "analysis" && shaderCode.Language == LanguageNames.Hlsl)
                    {
                        return Path.Combine(
                            Path.GetDirectoryName(tempFile.FilePath),
                            $"{entryPoint}_{Path.GetFileName(tempFile.FilePath)}.analysis");
                    }

                    switch (shaderCode.Language)
                    {
                        case LanguageNames.Hlsl:
                            if (directXMode == "dx12")
                            {
                                switch (targetProfile.Substring(0, 2))
                                {
                                    case "cs":
                                        switch (extension)
                                        {
                                            case "il":
                                            case "isa":
                                                shaderStage = "comp";
                                                break;

                                            default:
                                                shaderStage = "compute";
                                                break;
                                        }
                                        break;
                                }
                                goto default;
                            }
                            return Path.Combine(
                                actualOutputPathPrefix,
                                $"{asic}_{entryPoint}_{Path.GetFileName(tempFile.FilePath)}.{extension}");

                        default:
                            if (isVersion21OrLater)
                            {
                                return Path.Combine(
                                    actualOutputPathPrefix,
                                    $"{asic}_{Path.GetFileName(tempFile.FilePath)}_{shaderStage}.{extension}");
                            }
                            else
                            {
                                return Path.Combine(
                                    actualOutputPathPrefix,
                                    $"{asic}_{shaderStage}_{Path.GetFileName(tempFile.FilePath)}.{extension}");
                            }
                    }
                }

                outputAnalysisPath = GetActualOutputPath("analysis");
                ilPath = GetActualOutputPath("il");
                isaPath = GetActualOutputPath("isa");
                var isaCsvPath = GetActualOutputPath("csv");
                liveRegPath = GetActualOutputPath("livereg");
                cfgPath = GetActualOutputPath("cfg");

                var outputAnalysis = FileHelper.ReadAllTextIfExists(outputAnalysisPath);
                var il = FileHelper.ReadAllTextIfExists(ilPath);
                var isa = FileHelper.ReadAllTextIfExists(isaPath);
                var isaCsv = FileHelper.ReadAllTextIfExists(isaCsvPath);
                var liveReg = FileHelper.ReadAllTextIfExists(liveRegPath);
                var cfg = FileHelper.ReadAllTextIfExists(cfgPath);

                FileHelper.DeleteIfExists(outputAnalysisPath);
                FileHelper.DeleteIfExists(ilPath);
                FileHelper.DeleteIfExists(isaPath);
                FileHelper.DeleteIfExists(isaCsvPath);
                FileHelper.DeleteIfExists(liveRegPath);
                FileHelper.DeleteIfExists(cfgPath);

                var selectedOutputIndex = !result || stdOutput.Contains("\nError: ") || stdOutput.Contains("... failed.")
                    ? 5
                    : (int?) null;

                var isaBreakdownJson = GetIsaBreakdownJson(isaCsv);

                return new ShaderCompilerResult(
                    selectedOutputIndex == null,
                    null,
                    selectedOutputIndex,
                    new ShaderCompilerOutput("ISA Disassembly", null, isa),
                    new ShaderCompilerOutput("ISA Breakdown", "jsontable", isaBreakdownJson),
                    new ShaderCompilerOutput("IL Disassembly", null, il),
                    //new ShaderCompilerOutput("Analysis", null, outputAnalysis),
                    new ShaderCompilerOutput("Live register analysis", null, liveReg),
                    new ShaderCompilerOutput("Control flow graph", "graphviz", cfg),
                    new ShaderCompilerOutput("Build output", null, stdOutput));
            }
        }

        private static string GetIsaBreakdownJson(string isaCsv)
        {
            if (isaCsv == null)
            {
                return null;
            }

            var tableRows = new List<JsonTableRow>();

            using (var reader = new StringReader(isaCsv))
            using (var csv = new CsvReader(reader, new CsvHelper.Configuration.Configuration { HasHeaderRecord = false }))
            {
                while (csv.Read())
                {
                    var field0 = csv.GetField(0);

                    string field1, field2, field3, field4;
                    if (field0.Contains("label"))
                    {
                        field1 = field2 = field3 = field4 = "";
                    }
                    else
                    {
                        field1 = csv.GetField(1);
                        field2 = csv.GetField(2);
                        field3 = csv.GetField(3);
                        field4 = csv.GetField(4);
                    }

                    tableRows.Add(new JsonTableRow
                    {
                        Data = new[]
                        {
                            field0,
                            field1,
                            field2,
                            field3,
                            field4
                            //csv.GetField(5),
                        }
                    });
                }
            }

            var table = new JsonTable
            {
                Header = new JsonTableRow
                {
                    Data = new[]
                    {
                        "Offset",
                        "OpCode",
                        "Parameters",
                        "Category",
                        "Cycles",
                        //"Binary"
                    }
                },

                Rows = tableRows
            };

            return table.ToJson();
        }
    }
}
