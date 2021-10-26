using System.Collections.Generic;

namespace ShaderPlayground.Core
{
    public interface IShaderCompiler
    {
        string Name { get; }
        string DisplayName { get; }
        string Url { get; }
        string Description { get; }

        string[] InputLanguages { get; }
        
        ShaderCompilerParameter[] Parameters { get; }

        ShaderCompilerResult Compile(ShaderCode shaderCode, ShaderCompilerArguments arguments, List<ShaderCompilerArguments> previousCompilerArguments);
    }
}
