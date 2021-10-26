using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ShaderPlayground.Core
{
    public sealed class ShaderCompilerArguments : Dictionary<string, string>
    {
        private static readonly Regex IdentifierRegex = new Regex("^[_a-zA-Z0-9\\-=\\/ ]*$", RegexOptions.Compiled);

        private Dictionary<string, ShaderCompilerParameter> _parameters;

        public readonly IShaderCompiler Compiler;

        public ShaderCompilerArguments(IShaderCompiler compiler)
            : base()
        {
            Compiler = compiler;
            Initialize(compiler);
        }

        public ShaderCompilerArguments(IShaderCompiler compiler, IDictionary<string, string> dictionary)
            : base(dictionary)
        {
            Compiler = compiler;
            Initialize(compiler);
        }

        private void Initialize(IShaderCompiler compiler)
        {
            _parameters = compiler.Parameters.ToDictionary(x => x.Name);
        }

        public bool GetBoolean(string name)
        {
            GetValue(name, out var parameter, out var value);

            if (parameter.ParameterType != ShaderCompilerParameterType.CheckBox)
            {
                throw new ArgumentOutOfRangeException($"Parameter {name} is not a checkbox.");
            }

            if (!Boolean.TryParse(value, out var result))
            {
                throw new ArgumentOutOfRangeException($"Invalid value for {name}: '{value}'");
            }
            return result;
        }

        public string GetString(string name)
        {
            GetValue(name, out var parameter, out var value);

            switch (parameter.ParameterType)
            {
                case ShaderCompilerParameterType.ComboBox:
                    if (!parameter.Options.Contains(value))
                    {
                        throw new ArgumentOutOfRangeException($"Invalid option for {name}: '{value}'");
                    }
                    return value;

                case ShaderCompilerParameterType.TextBox:
                    if (!IdentifierRegex.IsMatch(value))
                    {
                        throw new ArgumentOutOfRangeException($"Invalid value for {name}: '{value}'");
                    }
                    return value;

                default:
                    throw new ArgumentOutOfRangeException($"Parameter {name} is not a string value.");
            }
        }

        public void GetValue(string name, out ShaderCompilerParameter parameter, out string value)
        {
            if (!_parameters.TryGetValue(name, out parameter))
            {
                throw new ArgumentOutOfRangeException($"No parameter named '{name}'.");
            }

            value = this[name];
        }
    }
}
