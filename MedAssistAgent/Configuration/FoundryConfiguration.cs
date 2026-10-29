namespace MedAssistAgent.Configuration;

public sealed record FoundryConfiguration(Uri ProjectEndpoint, string Model)
{
    public static FoundryConfiguration FromEnvironment() => Parse(
        Environment.GetEnvironmentVariable("FOUNDRY_PROJECT_ENDPOINT"),
        Environment.GetEnvironmentVariable("FOUNDRY_MODEL"));

    public static FoundryConfiguration Parse(string? endpoint, string? model)
    {
        if (string.IsNullOrWhiteSpace(endpoint) ||
            !Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(uri.UserInfo) ||
            !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment) ||
            !uri.AbsolutePath.StartsWith("/api/projects/", StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(uri.AbsolutePath["/api/projects/".Length..].Trim('/')))
        {
            throw new InvalidOperationException(
                "Set FOUNDRY_PROJECT_ENDPOINT to the HTTPS project endpoint copied from Foundry: https://<resource>.services.ai.azure.com/api/projects/<project>.");
        }

        if (string.IsNullOrWhiteSpace(model))
            throw new InvalidOperationException("Set FOUNDRY_MODEL to your deployed model's deployment name.");

        return new(uri, model.Trim());
    }
}
