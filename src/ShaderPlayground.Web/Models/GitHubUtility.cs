using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Octokit;
using ShaderPlayground.Core;

namespace ShaderPlayground.Web.Models
{
    internal static class GitHubUtility
    {
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

            var configJson = JsonSerializer.Serialize(new ConfigJsonModel
            {
                Language = request.Language,
                CompilationSteps = request.CompilationSteps
            }, new JsonSerializerOptions(JsonSerializerDefaults.Web));

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
