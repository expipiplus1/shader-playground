using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Octokit;
using ShaderPlayground.Core;

namespace ShaderPlayground.Web.Models
{
    internal static class GitHubUtility
    {
        private static readonly JsonSerializerSettings SerializerSettings;

        static GitHubUtility()
        {
            SerializerSettings = new JsonSerializerSettings();
            SerializerSettings.Formatting = Formatting.Indented;
        }

        private static GitHubClient CreateClient()
        {
            var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");

            return new GitHubClient(new ProductHeaderValue("ShaderPlayground"))
            {
                Credentials = new Credentials(token)
            };
        }

        public static async Task<string> CreateGistId(ShaderCompilationRequestViewModel request)
        {
            var language = Compiler.AllLanguages.First(x => x.Name == request.Language);

            var configJson = JsonConvert.SerializeObject(new ConfigJsonModel
            {
                Language = request.Language,
                CompilationSteps = request.CompilationSteps
            }, SerializerSettings);

            var client = CreateClient();

            var gist = await client.Gist.Create(new NewGist
            {
                Public = false,
                Files =
                {
                    { "shader." + language.FileExtension, request.Code },
                    { "config.json", configJson }
                }
            });

            return gist.Id;
        }

        private sealed class ConfigJsonModel
        {
            public string Language { get; set; }
            public CompilationStepViewModel[] CompilationSteps { get; set; }
        }
    }
}
