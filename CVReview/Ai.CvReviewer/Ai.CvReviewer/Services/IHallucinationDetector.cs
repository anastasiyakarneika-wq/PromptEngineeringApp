namespace Ai.CvReviewer.Tests.Services;

public interface IHallucinationDetector
{
    Task<List<string>> DetectHallucinatedSkillsAsync(List<string> matchedSkills, List<string> missingSkills, string requirements, string cv);
}

public class SimpleHallucinationDetector : IHallucinationDetector
{
    public Task<List<string>> DetectHallucinatedSkillsAsync(List<string> matchedSkills, List<string> missingSkills, string requirements, string cv)
    {
        var hallucinatedSkills = new List<string>();
        var allSkills = matchedSkills.Concat(missingSkills).ToList();

        // Common fictional skills that shouldn't appear unless explicitly mentioned
        var fictionalSkills = new[]
        {
            "quantum computing", "quantum ai", "time travel programming", "telepathic coding",
            "neural interface", "cyberpunk", "holographic ui", "brain-computer interface"
        };

        // Check for fictional skills in matched skills
        foreach (var skill in matchedSkills)
        {
            var lowerSkill = skill.ToLowerInvariant();

            // Check if it's a fictional skill
            if (fictionalSkills.Any(f => lowerSkill.Contains(f)))
            {
                hallucinatedSkills.Add(skill);
                continue;
            }

            // Check if skill is actually mentioned in requirements or CV
            if (!IsSkillMentioned(skill, requirements) && !IsSkillMentioned(skill, cv))
            {
                hallucinatedSkills.Add(skill);
            }
        }

        return Task.FromResult(hallucinatedSkills);
    }

    private bool IsSkillMentioned(string skill, string text)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(skill))
            return false;

        var skillWords = skill.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var textLower = text.ToLowerInvariant();

        // For multi-word skills, check if all words appear in the text
        return skillWords.All(word => textLower.Contains(word));
    }
}