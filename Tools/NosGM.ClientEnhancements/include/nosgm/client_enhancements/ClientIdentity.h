/*
 * NosGM.ClientEnhancements
 * Inspired by ImNotAVirus/NostaleWidget@fc1b6dda5d797efc24a053180d30702f8dad162a.
 * Copyright (c) 2022 ApourtArtt
 * Modifications Copyright (c) 2026 NosGM contributors
 * SPDX-License-Identifier: MIT
 */
#pragma once

#include <filesystem>
#include <optional>
#include <string>

namespace nosgm::client
{
    struct ClientIdentity
    {
        std::filesystem::path executablePath;
        std::string fileVersion;
        std::string sha256;
        std::string architecture;

        [[nodiscard]] bool IsX86() const noexcept;
    };

    std::optional<ClientIdentity> InspectClientExecutable(
        const std::filesystem::path& executablePath,
        std::string& error);

    std::optional<ClientIdentity> InspectCurrentProcessHost(std::string& error);
}
