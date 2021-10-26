using System.Collections.Generic;
using System.IO;
using ShaderPlayground.Core.Util;

namespace ShaderPlayground.Core.Compilers.Glslang
{
    internal sealed class SpirvRemapCompiler : IShaderCompiler
    {
        public string Name { get; } = CompilerNames.SpirvRemap;
        public string DisplayName { get; } = "spirv-remap";
        public string Url { get; } = "https://github.com/KhronosGroup/glslang/blob/master/README-spirv-remap.txt";
        public string Description { get; } = "spirv-remap is a utility to improve compression of SPIR-V binary files via entropy reduction, plus optional stripping of debug information and load/store optimization.";

        public string[] InputLanguages { get; } = { LanguageNames.SpirV };

        public ShaderCompilerParameter[] Parameters { get; } =
        {
            CommonParameters.CreateVersionParameter("glslang"),
            new ShaderCompilerParameter("Map", "Map", ShaderCompilerParameterType.ComboBox, MapOptions, defaultValue: "all"),
            new ShaderCompilerParameter("Dce", "Dead code elimination", ShaderCompilerParameterType.ComboBox, DceOptions, defaultValue: "all"),
            new ShaderCompilerParameter("Opt", "Optimize", ShaderCompilerParameterType.ComboBox, OptOptions, defaultValue: "all"),
            new ShaderCompilerParameter("Strip", "Strip debug info", ShaderCompilerParameterType.CheckBox, defaultValue: "true"),
            CommonParameters.CreateOutputParameter(new[] { LanguageNames.SpirV })
        };

        private static readonly string[] MapOptions =
        {
            "all",
            "types",
            "names",
            "funcs"
        };

        private static readonly string[] DceOptions =
        {
            "all",
            "types",
            "funcs"
        };

        private static readonly string[] OptOptions =
        {
            "all",
            "loadstore"
        };

        public ShaderCompilerResult Compile(ShaderCode shaderCode, ShaderCompilerArguments arguments, List<ShaderCompilerArguments> previousCompilerArguments)
        {
            var outputLanguage = arguments.GetString(CommonParameters.OutputLanguageParameterName);

            using (var tempFile = TempFile.FromShaderCode(shaderCode))
            {
                var outputPath = $"{tempFile.FilePath}.out";
                Directory.CreateDirectory(outputPath);

                var stripArgument = string.Empty;
                if (arguments.GetBoolean("Strip"))
                {
                    stripArgument = "--strip-all";
                }

                ProcessHelper.Run(
                    CommonParameters.GetBinaryPath("glslang", arguments, "spirv-remap.exe"),
                    $"--map {arguments.GetString("Map")} --dce {arguments.GetString("Dce")} --opt {arguments.GetString("Opt")} {stripArgument} --input \"{tempFile.FilePath}\" --output \"{outputPath}\"",
                    out var _,
                    out var _);

                var outputFile = Path.Combine(outputPath, Path.GetFileName(tempFile.FilePath));

                var binaryOutput = FileHelper.ReadAllBytesIfExists(outputFile);

                var textOutputPath = $"{tempFile.FilePath}.txt";

                ProcessHelper.Run(
                    CommonParameters.GetBinaryPath("spirv-tools", arguments, "spirv-dis.exe"),
                    $"-o \"{textOutputPath}\" \"{outputFile}\"",
                    out var _,
                    out var _);

                FileHelper.DeleteDirectoryIfExists(outputPath);

                var textOutput = FileHelper.ReadAllTextIfExists(textOutputPath);

                FileHelper.DeleteIfExists(textOutputPath);

                return new ShaderCompilerResult(
                    true,
                    new ShaderCode(outputLanguage, binaryOutput),
                    null,
                    new ShaderCompilerOutput("Output", outputLanguage, textOutput));
            }
        }
    }
}
