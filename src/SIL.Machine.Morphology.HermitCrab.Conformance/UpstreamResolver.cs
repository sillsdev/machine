#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SIL.Machine.Morphology.HermitCrab.Conformance;

/// <summary>One <c>upstream:</c> entry of a words.yaml: an issue or pull request the fixture's expectations depend on.</summary>
public sealed record UpstreamEntry(string Ref, string Kind, string Head, string Affects, string Note)
{
    public bool IsEngine => Affects == "engine";
}

public enum UpstreamState
{
    /// <summary>engine/pr: the build contains the PR head, or the PR's merge commit.</summary>
    Satisfied,

    /// <summary>Still open, not in this build, or the tools could not say (the reason says which).</summary>
    Pending,

    /// <summary>The PR merged: citations should be re-pointed to the merge commit.</summary>
    Merged,

    /// <summary>The PR was closed without merging: needs a human decision.</summary>
    ClosedUnmerged,

    /// <summary>The issue was closed (or Jira resolved Done/Fixed): retire the fixture or re-check it.</summary>
    IssueClosed,

    /// <summary>Jira resolved Won't Fix or Won't Do: needs a human decision.</summary>
    Rejected,
}

public sealed record ProcessResult(int ExitCode, string Output, string Error);

/// <summary>
/// Resolves a fixture's <c>upstream</c> entries (the suite's PROTOCOL.md section 12; the suite's
/// <c>scripts/upstream.py</c> is the reference, and this mirrors it). Git ancestry decides first and needs no
/// network; <c>gh</c> is asked only when git cannot decide; Jira is asked only when JIRA_URL and a token are in the
/// environment. Any failure of a tool or the network yields <see cref="UpstreamState.Pending"/> with a reason, never
/// an exception, so a run is never failed by being offline.
/// </summary>
/// <param name="checkout">The machine checkout the runner was built from, or null when unknown (git cannot decide).</param>
/// <param name="run">Runs argv (no shell) and returns its result; may throw when the tool is missing.</param>
/// <param name="getEnv">Environment lookup.</param>
/// <param name="jiraGet">GET a Jira URL with an Authorization header value, returning the body.</param>
public sealed class UpstreamResolver(
    string? checkout,
    Func<IReadOnlyList<string>, ProcessResult>? run = null,
    Func<string, string?>? getEnv = null,
    Func<string, string, string>? jiraGet = null
)
{
    public const string CheckoutEnvironmentVariable = "HC_MACHINE_CHECKOUT";

    private static readonly Regex RefPattern = new(@"^([A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+)#([0-9]+)$");
    private static readonly Regex JiraPattern = new(@"^jira:([A-Z][A-Z0-9]*-[0-9]+)$");
    private static readonly Regex HeadPattern = new("^[0-9a-f]{40}$");
    private static readonly string[] RejectedResolutions = ["won't fix", "won't do", "wontfix", "rejected"];

    private readonly Dictionary<UpstreamEntry, (UpstreamState, string)> _cache = new();
    private readonly Func<IReadOnlyList<string>, ProcessResult> _run = run ?? RunProcess;
    private readonly Func<string, string?> _getEnv = getEnv ?? Environment.GetEnvironmentVariable;
    private readonly Func<string, string, string> _jiraGet = jiraGet ?? HttpGet;

    /// <summary>A resolver for the checkout this runner was built from: <c>HC_MACHINE_CHECKOUT</c>, else the nearest
    /// enclosing git working tree of the assembly's directory, else none (everything undecided is pending).</summary>
    public static UpstreamResolver CreateDefault()
    {
        string? checkout = Environment.GetEnvironmentVariable(CheckoutEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(checkout))
        {
            for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
            {
                string dotGit = Path.Combine(dir.FullName, ".git");
                if (Directory.Exists(dotGit) || File.Exists(dotGit))
                {
                    checkout = dir.FullName;
                    break;
                }
            }
        }
        return new UpstreamResolver(string.IsNullOrWhiteSpace(checkout) ? null : checkout);
    }

    public (UpstreamState State, string Reason) Resolve(UpstreamEntry entry)
    {
        if (!_cache.TryGetValue(entry, out (UpstreamState, string) result))
            _cache[entry] = result = ResolveUncached(entry);
        return result;
    }

    /// <summary>The engine entries that keep a fixture from running on this build, as "state ref (head x not in this
    /// build)" text; empty when the fixture runs. <c>affects: loader</c> entries never skip.</summary>
    public string SkipReason(WordsYaml words)
    {
        var blocking = new List<string>();
        foreach (UpstreamEntry entry in words.Upstream.Where(e => e.IsEngine))
        {
            UpstreamState state = Resolve(entry).State;
            if (state == UpstreamState.Satisfied)
                continue;
            string head = entry.Head.Length >= 7 ? $" (head {entry.Head[..7]} not in this build)" : "";
            blocking.Add($"{state.ToString().ToLowerInvariant()} {entry.Ref}{head}");
        }
        return blocking.Count == 0 ? "" : string.Join("; ", blocking) + " — deviation expected";
    }

    /// <summary>The FieldWorks-producible gate: a shape HCLoader never writes is allowed iff the fixture has a
    /// non-closed <c>affects: loader</c> entry for sillsdev/FieldWorks (GitHub) or a Jira key. <paramref name="note"/>
    /// is non-empty when openness could not be confirmed (offline counts as open).</summary>
    public bool AllowsNeverShape(IEnumerable<UpstreamEntry> entries, out string note)
    {
        note = "";
        foreach (UpstreamEntry entry in entries.Where(e => e.Affects == "loader"))
        {
            if (!TryParseRef(entry.Ref, out string repo, out _) || (repo != "sillsdev/FieldWorks" && repo != "jira"))
                continue;
            (UpstreamState state, string reason) = Resolve(entry);
            if (state != UpstreamState.Pending)
                continue;
            note = reason == "open" ? "" : $"{entry.Ref} not confirmed open ({reason})";
            return true;
        }
        return false;
    }

    private (UpstreamState, string) ResolveUncached(UpstreamEntry entry)
    {
        try
        {
            if (!TryParseRef(entry.Ref, out string repo, out string number))
                return Pending($"invalid entry: bad ref '{entry.Ref}'");
            bool isPr = entry.Kind == "pr";
            if (isPr && !HeadPattern.IsMatch(entry.Head))
                return Pending("invalid entry: a pr needs a full 40-hex head");
            if (repo == "jira")
                return ResolveJira(number);

            if (isPr && entry.IsEngine && Contains(entry.Head) == true)
                return (UpstreamState.Satisfied, $"build contains head {entry.Head[..7]}");

            string[] args = isPr
                ? ["gh", "pr", "view", number, "-R", repo, "--json", "state,mergeCommit"]
                : ["gh", "issue", "view", number, "-R", repo, "--json", "state,stateReason"];
            ProcessResult gh = _run(args);
            if (gh.ExitCode != 0)
            {
                string why = gh.Error.Trim();
                return Pending($"gh unavailable or unauthenticated or offline: {why[..Math.Min(why.Length, 120)]}");
            }

            using JsonDocument json = JsonDocument.Parse(gh.Output);
            string? state = Text(json.RootElement, "state");
            switch (state)
            {
                case "OPEN":
                    return (UpstreamState.Pending, "open");
                case "MERGED" when isPr:
                    string? merge = json.RootElement.TryGetProperty("mergeCommit", out JsonElement mc)
                        ? Text(mc, "oid")
                        : null;
                    if (entry.IsEngine && merge != null && HeadPattern.IsMatch(merge) && Contains(merge) == true)
                        return (UpstreamState.Satisfied, $"merged as {merge[..7]} and the build contains it");
                    return (
                        UpstreamState.Merged,
                        $"merged as {(merge is { Length: >= 7 } ? merge[..7] : "unknown commit")}"
                    );
                case "CLOSED" when isPr:
                    return (UpstreamState.ClosedUnmerged, "closed without merging");
                case "CLOSED":
                    return (
                        UpstreamState.IssueClosed,
                        $"closed ({Text(json.RootElement, "stateReason") ?? "no reason given"})"
                    );
                default:
                    return Pending($"unrecognised upstream state '{state}'");
            }
        }
        catch (Exception ex)
        {
            return Pending($"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static (UpstreamState, string) Pending(string reason) =>
        (UpstreamState.Pending, $"{reason}; treated as pending");

    private static string? Text(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object
        && element.TryGetProperty(name, out JsonElement value)
        && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    /// <summary>True or false when the checkout's HEAD does or does not contain <paramref name="sha"/>; null when git cannot say.</summary>
    private bool? Contains(string sha)
    {
        if (checkout == null)
            return null;
        try
        {
            return _run(["git", "-C", checkout, "merge-base", "--is-ancestor", sha, "HEAD"]).ExitCode switch
            {
                0 => true,
                1 => false,
                _ => null, // 128: commit not present
            };
        }
        catch (Exception)
        {
            return null;
        }
    }

    private (UpstreamState, string) ResolveJira(string key)
    {
        string? url = _getEnv("JIRA_URL");
        string? pat = _getEnv("JIRA_PAT_TOKEN");
        string? user = _getEnv("JIRA_USERNAME");
        string? token = _getEnv("JIRA_API_TOKEN");
        string? authorization =
            !string.IsNullOrEmpty(pat) ? "Bearer " + pat
            : !string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(token)
                ? "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{token}"))
            : null;
        if (string.IsNullOrEmpty(url) || authorization == null)
        {
            return (
                UpstreamState.Pending,
                "jira unchecked (no JIRA_URL and credentials in the environment); treated as pending"
            );
        }

        try
        {
            string body = _jiraGet(
                $"{url.TrimEnd('/')}/rest/api/2/issue/{key}?fields=status,resolution",
                authorization
            );
            using JsonDocument json = JsonDocument.Parse(body);
            JsonElement fields = json.RootElement.GetProperty("fields");
            string? resolution = fields.TryGetProperty("resolution", out JsonElement r) ? Text(r, "name") : null;
            if (resolution == null)
            {
                string status = fields.TryGetProperty("status", out JsonElement s)
                    ? Text(s, "name") ?? "no status"
                    : "no status";
                return (UpstreamState.Pending, $"open ({status})");
            }
            return RejectedResolutions.Contains(resolution.ToLowerInvariant())
                ? (UpstreamState.Rejected, $"resolved {resolution}")
                : (UpstreamState.IssueClosed, $"resolved {resolution}");
        }
        catch (Exception ex)
        {
            return (UpstreamState.Pending, $"jira unchecked ({ex.GetType().Name}); treated as pending");
        }
    }

    /// <summary>(owner/repo, number) or ("jira", KEY-123); false for any other text.</summary>
    public static bool TryParseRef(string text, out string repo, out string number)
    {
        Match jira = JiraPattern.Match(text);
        if (jira.Success)
        {
            (repo, number) = ("jira", jira.Groups[1].Value);
            return true;
        }
        Match match = RefPattern.Match(text);
        (repo, number) = match.Success ? (match.Groups[1].Value, match.Groups[2].Value) : ("", "");
        return match.Success;
    }

    private static ProcessResult RunProcess(IReadOnlyList<string> argv)
    {
        ProcessStartInfo info = ChildProcessEnvironment.CreateStartInfo(argv[0]);
        info.RedirectStandardOutput = true;
        info.RedirectStandardError = true;
        foreach (string arg in argv.Skip(1))
            info.ArgumentList.Add(arg);
        using Process process = Process.Start(info) ?? throw new IOException($"cannot start {argv[0]}");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(30_000))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException($"{argv[0]} timed out");
        }
        return new ProcessResult(process.ExitCode, output.Result, error.Result);
    }

    private static string HttpGet(string url, string authorization)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("Authorization", authorization);
        request.Headers.TryAddWithoutValidation("Accept", "application/json");
        using HttpResponseMessage response = client.Send(request);
        response.EnsureSuccessStatusCode();
        return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
    }
}
