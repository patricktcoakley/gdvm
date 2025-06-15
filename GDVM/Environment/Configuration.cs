using Microsoft.Extensions.Configuration;
using GDVM.Error;

namespace GDVM.Environment;

public static class Configuration
{
    public static void ValidateConfiguration(IConfiguration configuration)
    {
        var githubToken = configuration["github:token"];

        if (!string.IsNullOrEmpty(githubToken))
        {
            // GitHub token format validation per Microsoft Purview specification
            // Valid prefixes: ghp_, gho_, ghu_, ghs_, ghr_
            if (!githubToken.StartsWith("ghp_") && !githubToken.StartsWith("gho_") &&
                !githubToken.StartsWith("ghu_") && !githubToken.StartsWith("ghs_") &&
                !githubToken.StartsWith("ghr_"))
            {
                throw new ConfigurationException("GitHub token should start with 'ghp_', 'gho_', 'ghu_', 'ghs_', or 'ghr_' prefix");
            }

            // https://learn.microsoft.com/en-us/purview/sit-defn-github-personal-access-token
            if (githubToken.Length != 40)
            {
                throw new ConfigurationException("GitHub token should be exactly 40 characters long");
            }

            // Check that the token contains only valid characters (alphanumeric + underscore)
            if (!githubToken.All(c => char.IsLetterOrDigit(c) || c == '_'))
            {
                throw new ConfigurationException("GitHub token contains invalid characters");
            }
        }
    }
}