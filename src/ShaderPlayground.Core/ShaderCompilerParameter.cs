using System;
using System.Text.Json.Serialization;

namespace ShaderPlayground.Core
{
    public sealed class ShaderCompilerParameter
    {
        public string Name { get; }
        public string DisplayName { get; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ShaderCompilerParameterType ParameterType { get; }

        public string[] Options { get; }
        public string DefaultValue { get; }

        public string Description { get; }

        public ParameterFilter Filter { get; }

        internal ShaderCompilerParameter(
            string name, 
            string displayName, 
            ShaderCompilerParameterType parameterType, 
            string[] options = null, 
            string defaultValue = null, 
            string description = null,
            ParameterFilter filter = null)
        {
            Name = name;
            DisplayName = displayName;
            ParameterType = parameterType;
            Options = options ?? Array.Empty<string>();
            DefaultValue = defaultValue;
            Description = description;
            Filter = filter;
        }

        public ShaderCompilerParameter WithFilter(string name, string value)
        {
            return WithFilter(new ParameterFilter(name, new[] { value }));
        }

        public ShaderCompilerParameter WithFilter(string name, string[] values)
        {
            return WithFilter(new ParameterFilter(name, values));
        }

        public ShaderCompilerParameter WithFilter(ParameterFilter filter)
        {
            return new ShaderCompilerParameter(
                Name,
                DisplayName,
                ParameterType,
                Options,
                DefaultValue,
                Description,
                filter);
        }

        public ShaderCompilerParameter WithDisplayName(string displayName)
        {
            return new ShaderCompilerParameter(
                Name,
                displayName,
                ParameterType,
                Options,
                DefaultValue,
                Description,
                Filter);
        }

        public ShaderCompilerParameter WithDefaultValue(string defaultValue)
        {
            return new ShaderCompilerParameter(
                Name,
                DisplayName,
                ParameterType,
                Options,
                defaultValue,
                Description,
                Filter);
        }
    }

    public sealed class ParameterFilter
    {
        public string Name { get; }
        public string[] Values { get; }

        public ParameterFilter(string name, string[] values)
        {
            Name = name;
            Values = values;
        }

        public ParameterFilter(string name, string value)
            : this(name, new[] {  value })
        {
            
        }
    }
}
