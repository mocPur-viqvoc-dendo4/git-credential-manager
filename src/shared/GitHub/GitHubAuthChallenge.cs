using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace GitHub;

public class GitHubAuthChallenge : IEquatable<GitHubAuthChallenge>
{
    private static readonly Regex BasicRegex = new(@"Basic\s+(?'props1'.*)realm=""GitHub""(?'props2'.*)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static IList<GitHubAuthChallenge> FromHeaders(IEnumerable<string> headers)
    {
        var challenges = new List<GitHubAuthChallenge>();
        foreach (string header in headers)
        {
            var match = BasicRegex.Match(header);
            if (match.Success)
            {
                IDictionary<string, string> props = ParseProperties(match.Groups["props1"].Value + match.Groups["props2"]);

                // The enterprise shortcode is provided in the `domain_hint` property, whereas the
                // enterprise name/slug is provided in the `enterprise_hint` property.
                props.TryGetValue("domain_hint", out string domain);
                props.TryGetValue("enterprise_hint", out string enterprise);

                var challenge = new GitHubAuthChallenge(domain, enterprise);

                challenges.Add(challenge);
            }
        }

        return challenges;
    }

    private static IDictionary<string, string> ParseProperties(string str)
    {
        var props = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string prop in str.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            int delim = prop.IndexOf('=');
            if (delim < 0)
            {
                continue;
            }

            // Use AsSpan to avoid string allocations
            ReadOnlySpan<char> propSpan = prop.AsSpan();
            string key = propSpan.Slice(0, delim).Trim().ToString();
            string value = propSpan.Slice(delim + 1).Trim('"').ToString();

            props[key] = value;
        }

        return props;
    }

    public GitHubAuthChallenge() { }

    public GitHubAuthChallenge(string domain, string enterprise)
    {
        Domain = domain;
        Enterprise = enterprise;
    }

    public string Domain { get; }

    public string Enterprise { get; }

    public bool IsDomainMember(string userName)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            return false;
        }

        int delim = userName.LastIndexOf('_');
        if (delim < 0)
        {
            return string.IsNullOrWhiteSpace(Domain);
        }

        // Check for users that contain underscores but are not EMU logins
        if (GitHubConstants.InvalidUnderscoreLogins.Contains(userName, StringComparer.OrdinalIgnoreCase))
        {
            return string.IsNullOrWhiteSpace(Domain);
        }

        // Use AsSpan to avoid string allocation
        ReadOnlySpan<char> shortCode = userName.AsSpan(delim + 1);
        return shortCode.Equals(Domain, StringComparison.OrdinalIgnoreCase);
    }

    public bool Equals(GitHubAuthChallenge other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return string.Equals(Domain, other.Domain, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(Enterprise, other.Enterprise, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((GitHubAuthChallenge)obj);
    }

    public override int GetHashCode()
    {
        return (Domain?.GetHashCode() ?? 0) * 1019 ^
               (Enterprise?.GetHashCode() ?? 0) * 337;
    }
}
