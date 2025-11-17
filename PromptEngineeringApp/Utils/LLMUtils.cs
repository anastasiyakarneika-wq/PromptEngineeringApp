using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PromptEngineeringApp.Utils
{
    public static class LLMUtils
    {
        public static string RemoveExtrasFromResults(this string result)
        {
            return result.Replace("\n", "")
                .Replace("\r", "")
                .Replace("```json", "")
                .Replace("```", "");
        }
    }
}
