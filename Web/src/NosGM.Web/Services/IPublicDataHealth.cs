// SPDX-License-Identifier: MIT

namespace NosGM.Web.Services;

public interface IPublicDataHealth
{
    bool IsReady { get; }

    DateTimeOffset? ObservedAt { get; }

    string? LastError { get; }
}
