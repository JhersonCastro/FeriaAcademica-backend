using FeriaAcademica.Domain.Common;

namespace FeriaAcademica.Domain.Works;

/// <summary>Links (videos, repositories, ...) must be absolute http or https URLs.</summary>
public static class ArtifactLinkRules
{
    internal static void Validate(string linkUrl)
    {
        if (!Uri.TryCreate(linkUrl.Trim(), UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new DomainException("The link must be a full http or https address, for example https://www.youtube.com/watch?v=...");
    }
}
