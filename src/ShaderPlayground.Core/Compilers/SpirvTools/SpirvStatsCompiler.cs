using System.Collections.Generic;
using ShaderPlayground.Core.Util;

namespace ShaderPlayground.Core.Compilers.SpirvTools
{
    internal sealed class SpirvStatsCompiler : IShaderCompiler
    {
        public string Name { get; } = CompilerNames.SpirvStats;
        public string DisplayName { get; } = "spirv-stats";
        public string Url { get; } = "https://github.com/KhronosGroup/SPIRV-Tools/blob/master/tools/stats/stats.cpp";
        public string Description { get; } = "Shows statistics from SPIR-V files.";

        public string[] InputLanguages { get; } = { LanguageNames.SpirV };

        public ShaderCompilerParameter[] Parameters { get; } =
        {
            CommonParameters.CreateVersionParameter("spirv-tools-legacy")
        };

        public ShaderCompilerResult Compile(ShaderCode shaderCode, ShaderCompilerArguments arguments, List<ShaderCompilerArguments> previousCompilerArguments)
        {
            using (var tempFile = TempFile.FromShaderCode(shaderCode))
            {
                ProcessHelper.Run(
                    CommonParameters.GetBinaryPath("spirv-tools-legacy", arguments, "spirv-stats.exe"),
                    $"\"{tempFile.FilePath}\"",
                    out var stdOutput,
                    out var _);

                return new ShaderCompilerResult(
                    true,
                    null,
                    null,
                    new ShaderCompilerOutput("Output", null, stdOutput));
            }
        }
    }
}
