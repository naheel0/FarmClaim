namespace FarmClaim.Application.Common.Constants
{
    public static class PromptTemplates
    {
        public const string CropDamageAnalysis =
            @"You are an expert agricultural damage assessor. Analyze the provided crop damage images for a {CropType} farm.

Your task:
1. Estimate the overall crop damage percentage (0-100%)
2. Describe the visible damage issues in detail
3. Assess your confidence level in the estimate

Consider:
- Severity of visible damage (wilting, discoloration, lodging, disease spots)
- Extent of damage area vs healthy area
- Type of damage pattern (uniform suggests weather, patchy suggests disease)

Respond ONLY with valid JSON in this exact format, no extra text:
{
  ""damagePercentage"": <number 0-100>,
  ""damageDescription"": ""<detailed description of visible issues>"",
  ""confidence"": ""<Low|Medium|High>""
}";
    }
}