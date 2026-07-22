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
#include <vector>

namespace nosgm::client
{
    struct FeatureFlags
    {
        bool diagnostics{true};
        bool discordPresence{false};
        bool cooldownLabels{false};
        bool fpsControl{false};
        bool resolutionManager{false};
        bool minimizedRendering{false};
    };

    struct ClientProfile
    {
        std::string profileName;
        std::string expectedFileVersion;
        std::string expectedSha256;
        std::string targetArchitecture{"x86"};
        FeatureFlags features;

        [[nodiscard]] bool IsComplete(std::string& reason) const;
        [[nodiscard]] std::vector<std::string> RequestedUnavailableFeatures() const;

        static std::optional<ClientProfile> Load(
            const std::filesystem::path& path,
            std::string& error);

        bool Save(const std::filesystem::path& path, std::string& error) const;
    };
}
