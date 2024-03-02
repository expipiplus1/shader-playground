namespace ShaderPlayground.Core.Languages
{
    internal sealed class WgslLanguage : IShaderLanguage
    {
        public string Name { get; } = LanguageNames.Wgsl;

        public string DefaultCode { get; } = DefaultWgslCode;

        public string FileExtension { get; } = "wgsl";

        private static readonly string DefaultWgslCode = @"struct VertexOutput {
    @builtin(position) position: vec4<f32>,
};

@vertex
fn vertex() -> VertexOutput {
    return VertexOutput(vec4<f32>(1.0));
}
";
    }
}
