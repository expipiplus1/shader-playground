using ShaderPlayground.Core.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace ShaderPlayground.Core.Compilers.Tint
{
    internal sealed class TintCompiler : IShaderCompiler
    {
        public string Name { get; } = CompilerNames.Tint;
        public string DisplayName { get; } = "Tint";
        public string Url { get; } = "https://dawn.googlesource.com/tint/";
        public string Description { get; } = "WebGPU Shader Language (WGSL) Compiler";

        public string[] InputLanguages { get; } =
        {
            LanguageNames.SpirV,
            LanguageNames.SpirvAssembly,
            LanguageNames.Wgsl,
        };

        private static readonly string ShaderStageName = "ShaderStage";
        private static readonly string EntryPointName = "EntryPoint";
        private static readonly string AllShaderStages = "<all>";
        private static readonly string EntryPointDescription = $"Entry point name.\nOnly used when 'Shader stage' is not '{AllShaderStages}'";

        public ShaderCompilerParameter[] Parameters { get; } =
        {
            CommonParameters.CreateVersionParameter("tint"),
            new ShaderCompilerParameter(ShaderStageName, "Shader stage", ShaderCompilerParameterType.ComboBox, ShaderStageOptions, defaultValue: AllShaderStages),
            new ShaderCompilerParameter(EntryPointName, "Entry Point", ShaderCompilerParameterType.TextBox, defaultValue: "main", description: EntryPointDescription),
            CommonParameters.CreateOutputParameter(new[] {
                LanguageNames.SpirV,
                LanguageNames.SpirvAssembly,
                LanguageNames.Wgsl,
                LanguageNames.Metal,
                LanguageNames.Hlsl
            })
        };

        private static readonly string[] ShaderStageOptions =
        {
            AllShaderStages,
            "compute",
            "vertex",
            "fragment",
        };

        public ShaderCompilerResult Compile(ShaderCode shaderCode, ShaderCompilerArguments arguments, List<ShaderCompilerArguments> previousCompilerArguments)
        {
            var stage = arguments.GetString(ShaderStageName);
            var entryPoint = arguments.GetString(EntryPointName);
            var outputLanguage = arguments.GetString(CommonParameters.OutputLanguageParameterName);
            var exePath = CommonParameters.GetBinaryPath("tint", arguments, "tint.exe");

            if (!RunTint(exePath, shaderCode, stage, entryPoint, outputLanguage, out var output, out var error))
            {
                return ErrorResult(outputLanguage, error);
            }

            if (outputLanguage == LanguageNames.SpirV)
            {
                // SPIR-V is currently the only binary format that requires disassembling.
                if (!RunTint(exePath, shaderCode, stage, entryPoint, LanguageNames.SpirvAssembly, out var asm, out error))
                {
                    return ErrorResult(outputLanguage, error);
                }
                return new ShaderCompilerResult(
                    true,
                    new ShaderCode(outputLanguage, output),
                    0,
                    new ShaderCompilerOutput("Disassembly", outputLanguage, Encoding.ASCII.GetString(asm)),
                    new ShaderCompilerOutput("Build output", null, ""));
            }
            else
            {
                var disassembly = Encoding.ASCII.GetString(output);
                return new ShaderCompilerResult(
                    true,
                    new ShaderCode(outputLanguage, disassembly),
                    0,
                    new ShaderCompilerOutput("Disassembly", outputLanguage, disassembly),
                    new ShaderCompilerOutput("Build output", null, ""));
            }

        }

        private static ShaderCompilerResult ErrorResult(string outputLanguage, string error)
        {
            return new ShaderCompilerResult(
                false,
                new ShaderCode(outputLanguage, (byte[])null),
                1,
                new ShaderCompilerOutput("Disassembly", outputLanguage, null),
                new ShaderCompilerOutput("Build output", null, error));
        }

        private static bool RunTint(string exePath, ShaderCode code, string stage, string entryPoint, string outputLanguage,
            out byte[] output, out string error)
        {
            var args = new List<string>();
            switch (outputLanguage)
            {
                case LanguageNames.SpirV:
                    args.Add("--format spirv");
                    break;
                case LanguageNames.SpirvAssembly:
                    args.Add("--format spvasm");
                    break;
                case LanguageNames.Wgsl:
                    args.Add("--format wgsl");
                    break;
                case LanguageNames.Metal:
                    args.Add("--format msl");
                    break;
                case LanguageNames.Hlsl:
                    args.Add("--format hlsl");
                    break;
            }

            if (stage != AllShaderStages)
            {
                args.Add($"-ep {stage} {entryPoint}");
            }

            using (var inputFile = TempFile.FromShaderCode(code))
            {
                var outputFile = $"{inputFile.FilePath}.o";

                args.Add($"-o {outputFile}");
                args.Add(inputFile);

                ProcessHelper.Run(
                    exePath,
                    String.Join(" ", args),
                    out var stdOutput,
                    out var stdError);

                output = FileHelper.ReadAllBytesIfExists(outputFile);
                FileHelper.DeleteIfExists(outputFile);
                error = stdError;
                if (output == null && error == "")
                {
                    error = (stdOutput != "") ? stdOutput : "<no output>";
                }
                return error == "";
            }
        }
    }
}
