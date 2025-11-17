namespace PromptEngineeringApp.Utils
{
    public static class FileUtils
    {
        public static async Task<string> ReadFileAsync(string filePath)
        {
            using StreamReader reader = new(filePath);
            return await reader.ReadToEndAsync();
        }
    }
}
