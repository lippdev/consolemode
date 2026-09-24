namespace ConsoleMode.Services;

/// <summary>
/// Builds the link behind the "Send feedback" button: a new GitHub issue from
/// .github/ISSUE_TEMPLATE/feedback.yml, with the environment field already filled in.
/// No token or API call is involved; the user reviews and submits the issue in the browser.
/// </summary>
public static class FeedbackService
{
    public const string Template = "feedback.yml";

    /// <summary>Issue form field ids; they must match feedback.yml.</summary>
    public const string EnvironmentField = "environment";

    public static string BuildIssueUrl(string repository, string environment) =>
        $"https://github.com/{repository}/issues/new" +
        $"?template={Uri.EscapeDataString(Template)}" +
        $"&{EnvironmentField}={Uri.EscapeDataString(environment)}";

    /// <summary>Only non-personal details: app version and install kind, Windows version and UI language.</summary>
    public static string BuildEnvironment(string appVersion, bool isInstalled, string osVersion, string architecture, string language) =>
        $"Console Mode {appVersion} ({(isInstalled ? "installed" : "portable")})\n" +
        $"Windows {osVersion} ({architecture})\n" +
        $"Language: {language}";
}
