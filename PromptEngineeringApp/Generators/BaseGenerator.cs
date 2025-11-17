

using DotNetEnv;
using Microsoft.SemanticKernel;
using PromptEngineeringApp.Utils;

namespace PromptEngineeringApp.Generators
{
    public  class BaseGenerator
    {
        private Kernel _kernel;
        protected BaseGenerator() {
            if (_kernel == null) {
                Env.Load();

                _kernel = Kernel.CreateBuilder()
                    .AddAzureOpenAIChatCompletion(
                        deploymentName: Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME")!,
                        endpoint: Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")!,
                        apiKey: Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")!)
                    .Build();
            }

        }

        public async Task<string> CallLLMAsync(string prompt, string systemMessage)
        {
            try
            {
                var result = await _kernel.InvokePromptAsync(prompt, new KernelArguments
                {
                    ["system_message"] = systemMessage
                });
                var stringResult = result.ToString();
                stringResult = stringResult.RemoveExtrasFromResults();
                Console.WriteLine(stringResult);
                return stringResult;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating prompt results : {ex.Message}");
                return "Error";
            }
        }
    }
}
