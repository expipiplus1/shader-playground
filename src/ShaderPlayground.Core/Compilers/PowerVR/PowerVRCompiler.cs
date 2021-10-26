using System;
using System.Collections.Generic;
using System.IO;
using ShaderPlayground.Core.Util;

namespace ShaderPlayground.Core.Compilers.PowerVR
{
    internal sealed class PowerVRCompiler : IShaderCompiler
    {
        public string Name { get; } = CompilerNames.PowerVR;
        public string DisplayName { get; } = "PowerVR compiler";
        public string Url { get; } = "https://community.imgtec.com/developers/powervr/tools/pvrshadereditor/";
        public string Description { get; } = "PowerVR GLSL-ES shader compiler";

        public string[] InputLanguages { get; } = { LanguageNames.Glsl };

        public ShaderCompilerParameter[] Parameters { get; } = new[]
        {
            CommonParameters.CreateVersionParameter("powervr"),
            CommonParameters.GlslShaderStage
        };

        public ShaderCompilerResult Compile(ShaderCode shaderCode, ShaderCompilerArguments arguments, List<ShaderCompilerArguments> previousCompilerArguments)
        {
            string shaderType;
            switch (arguments.GetString("ShaderStage"))
            {
                case "vert":
                    shaderType = "-v";
                    break;

                case "tesc":
                    shaderType = "-tc";
                    break;

                case "tese":
                    shaderType = "-te";
                    break;

                case "geom":
                    shaderType = "-g";
                    break;

                case "frag":
                    shaderType = "-f";
                    break;

                case "comp":
                    shaderType = "-c";
                    break;

                default:
                    throw new InvalidOperationException();
            }

            using (var tempFile = TempFile.FromShaderCode(shaderCode))
            {
                var outputDisassemblyPath = Path.ChangeExtension(tempFile.FilePath, ".disasm");
                var outputProfilePath = Path.ChangeExtension(tempFile.FilePath, ".prof");

                ProcessHelper.Run(
                    CommonParameters.GetBinaryPath("powervr", arguments, "GLSLESCompiler_Rogue.exe"),
                    $"{tempFile.FilePath} {tempFile.FilePath} {shaderType} -disasm -profile",
                    out var stdOutput,
                    out var stdError);

                if (stdError == string.Empty)
                {
                    stdError = stdOutput;
                }

                var outputDisassembly = FileHelper.ReadAllTextIfExists(outputDisassemblyPath);
                var outputProfile = FileHelper.ReadAllTextIfExists(outputProfilePath);

                FileHelper.DeleteIfExists(outputDisassemblyPath);
                FileHelper.DeleteIfExists(outputProfilePath);

                var selectedOutputIndex = stdError.Contains("failed")
                    ? 2
                    : (int?) null;

                return new ShaderCompilerResult(
                    true,
                    null,
                    selectedOutputIndex,
                    new ShaderCompilerOutput("Disassembly", null, outputDisassembly),
                    new ShaderCompilerOutput("Profiling", null, outputProfile),
                    new ShaderCompilerOutput("Output", null, stdError));
            }
        }
    }
}
