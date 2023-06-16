using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShaderPlayground.Core.Util;

namespace ShaderPlayground.Core.Compilers.Metal;

internal sealed class MetalShaderConverter : IShaderCompiler
{
    public string Name { get; } = CompilerNames.MetalShaderConverter;
    public string DisplayName { get; } = "Metal Shader Converter";
    public string Url { get; } = "https://developer.apple.com/metal/shader-converter/";
    public string Description { get; } = "Metal shader converter converts shader intermediate representations in LLVM IR bytecode into bytecode suitable to be loaded into Metal. It’s available as a library and a standalone executable. All the functionality exposed through the library interface is available via the standalone executable.";

    public string[] InputLanguages { get; } = { LanguageNames.Dxil };

    public ShaderCompilerParameter[] Parameters { get; } =
    {
        CommonParameters.CreateVersionParameter("metal-shader-converter"),
        CommonParameters.ExtraOptionsParameter,
    };

    public ShaderCompilerResult Compile(ShaderCode shaderCode, ShaderCompilerArguments arguments, List<ShaderCompilerArguments> previousCompilerArguments)
    {
        using var tempFile = TempFile.FromShaderCode(shaderCode);

        var outputPath = $"{tempFile.FilePath}.metallib";

        ProcessHelper.Run(
            CommonParameters.GetBinaryPath("metal-shader-converter", arguments, "metal-shaderconverter.exe"),
            $"-o=\"{outputPath}\" {arguments.GetString(CommonParameters.ExtraOptionsParameter.Name)} \"{tempFile.FilePath}\"",
            out var stdOutput,
            out var stdError);

        var binaryOutput = FileHelper.ReadAllBytesIfExists(outputPath);

        var hasCompilationError = binaryOutput == null;

        string textOutput = null;
        if (!hasCompilationError)
        {
            ProcessHelper.Run(
                CommonParameters.GetBinaryPath("metal-shader-converter", arguments, "metal-objdump.exe"),
                $"--disassemble \"{outputPath}\"",
                out textOutput,
                out _);
        }

        FileHelper.DeleteIfExists(outputPath);

        return new ShaderCompilerResult(
            !hasCompilationError,
            !hasCompilationError ? new ShaderCode(LanguageNames.MetalIR, binaryOutput) : null,
            hasCompilationError ? (int?)1 : null,
            new ShaderCompilerOutput("Assembly", LanguageNames.MetalIR, textOutput),
            new ShaderCompilerOutput("Output", null, stdError));
    }
}
