/*
 * NosGM.ClientEnhancements
 * Inspired by ImNotAVirus/NostaleWidget@fc1b6dda5d797efc24a053180d30702f8dad162a.
 * Copyright (c) 2022 ApourtArtt
 * Modifications Copyright (c) 2026 NosGM contributors
 * SPDX-License-Identifier: MIT
 */
#include "nosgm/client_enhancements/Profile.h"

#include <algorithm>
#include <cctype>
#include <fstream>
#include <sstream>
#include <unordered_map>

namespace
{
    std::string Trim(std::string value)
    {
        const auto isNotSpace = [](const unsigned char c)
        {
            return std::isspace(c) == 0;
        };

        value.erase(value.begin(), std::find_if(value.begin(), value.end(), isNotSpace));
        value.erase(std::find_if(value.rbegin(), value.rend(), isNotSpace).base(), value.end());
        return value;
    }

    std::string Lower(std::string value)
    {
        std::transform(value.begin(), value.end(), value.begin(), [](const unsigned char c)
        {
            return static_cast<char>(std::tolower(c));
        });
        return value;
    }

    bool ParseBoolean(const std::string& value, bool& result)
    {
        const auto normalized = Lower(Trim(value));
        if (normalized == "true" || normalized == "1" || normalized == "yes" || normalized == "on")
        {
            result = true;
            return true;
        }
        if (normalized == "false" || normalized == "0" || normalized == "no" || normalized == "off")
        {
            result = false;
            return true;
        }
        return false;
    }

    bool IsHexString(const std::string& value)
    {
        return std::all_of(value.begin(), value.end(), [](const unsigned char c)
        {
            return std::isxdigit(c) != 0;
        });
    }
}

namespace nosgm::client
{
    bool ClientProfile::IsComplete(std::string& reason) const
    {
        if (profileName.empty())
        {
            reason = "profile_name is required.";
            return false;
        }
        if (expectedFileVersion.empty())
        {
            reason = "expected_file_version is required.";
            return false;
        }
        if (expectedSha256.size() != 64 || !IsHexString(expectedSha256))
        {
            reason = "expected_sha256 must contain exactly 64 hexadecimal characters.";
            return false;
        }
        if (Lower(targetArchitecture) != "x86")
        {
            reason = "Only the x86 client architecture is supported.";
            return false;
        }

        reason.clear();
        return true;
    }

    std::vector<std::string> ClientProfile::RequestedUnavailableFeatures() const
    {
        std::vector<std::string> names;
        if (features.discordPresence)
        {
            names.emplace_back("discord_presence");
        }
        if (features.cooldownLabels)
        {
            names.emplace_back("cooldown_labels");
        }
        if (features.fpsControl)
        {
            names.emplace_back("fps_control");
        }
        if (features.resolutionManager)
        {
            names.emplace_back("resolution_manager");
        }
        if (features.minimizedRendering)
        {
            names.emplace_back("minimized_rendering");
        }
        return names;
    }

    std::optional<ClientProfile> ClientProfile::Load(
        const std::filesystem::path& path,
        std::string& error)
    {
        std::ifstream stream(path);
        if (!stream)
        {
            error = "Unable to open profile: " + path.string();
            return std::nullopt;
        }

        ClientProfile profile;
        std::string line;
        std::size_t lineNumber = 0;

        while (std::getline(stream, line))
        {
            ++lineNumber;
            line = Trim(line);
            if (line.empty() || line.front() == '#' || line.front() == ';' || line.front() == '[')
            {
                continue;
            }

            const auto separator = line.find('=');
            if (separator == std::string::npos)
            {
                error = "Invalid profile line " + std::to_string(lineNumber) + ": expected key=value.";
                return std::nullopt;
            }

            const auto key = Lower(Trim(line.substr(0, separator)));
            const auto value = Trim(line.substr(separator + 1));
            bool parsedBoolean = false;

            if (key == "profile_name")
            {
                profile.profileName = value;
            }
            else if (key == "expected_file_version")
            {
                profile.expectedFileVersion = value;
            }
            else if (key == "expected_sha256")
            {
                profile.expectedSha256 = Lower(value);
            }
            else if (key == "target_architecture")
            {
                profile.targetArchitecture = Lower(value);
            }
            else if (key == "enable_diagnostics")
            {
                if (!ParseBoolean(value, parsedBoolean))
                {
                    error = "Invalid boolean on line " + std::to_string(lineNumber) + ".";
                    return std::nullopt;
                }
                profile.features.diagnostics = parsedBoolean;
            }
            else if (key == "enable_discord_presence")
            {
                if (!ParseBoolean(value, parsedBoolean))
                {
                    error = "Invalid boolean on line " + std::to_string(lineNumber) + ".";
                    return std::nullopt;
                }
                profile.features.discordPresence = parsedBoolean;
            }
            else if (key == "enable_cooldown_labels")
            {
                if (!ParseBoolean(value, parsedBoolean))
                {
                    error = "Invalid boolean on line " + std::to_string(lineNumber) + ".";
                    return std::nullopt;
                }
                profile.features.cooldownLabels = parsedBoolean;
            }
            else if (key == "enable_fps_control")
            {
                if (!ParseBoolean(value, parsedBoolean))
                {
                    error = "Invalid boolean on line " + std::to_string(lineNumber) + ".";
                    return std::nullopt;
                }
                profile.features.fpsControl = parsedBoolean;
            }
            else if (key == "enable_resolution_manager")
            {
                if (!ParseBoolean(value, parsedBoolean))
                {
                    error = "Invalid boolean on line " + std::to_string(lineNumber) + ".";
                    return std::nullopt;
                }
                profile.features.resolutionManager = parsedBoolean;
            }
            else if (key == "enable_minimized_rendering")
            {
                if (!ParseBoolean(value, parsedBoolean))
                {
                    error = "Invalid boolean on line " + std::to_string(lineNumber) + ".";
                    return std::nullopt;
                }
                profile.features.minimizedRendering = parsedBoolean;
            }
            else
            {
                error = "Unknown profile key on line " + std::to_string(lineNumber) + ": " + key;
                return std::nullopt;
            }
        }

        if (!profile.IsComplete(error))
        {
            return std::nullopt;
        }

        return profile;
    }

    bool ClientProfile::Save(const std::filesystem::path& path, std::string& error) const
    {
        std::ofstream stream(path, std::ios::trunc);
        if (!stream)
        {
            error = "Unable to write profile: " + path.string();
            return false;
        }

        stream
            << "# Generated by NosGM.ClientProbe. Review before use.\n"
            << "profile_name=" << profileName << "\n"
            << "expected_file_version=" << expectedFileVersion << "\n"
            << "expected_sha256=" << expectedSha256 << "\n"
            << "target_architecture=" << targetArchitecture << "\n\n"
            << "# Foundation build: only diagnostics is available.\n"
            << "enable_diagnostics=" << (features.diagnostics ? "true" : "false") << "\n"
            << "enable_discord_presence=" << (features.discordPresence ? "true" : "false") << "\n"
            << "enable_cooldown_labels=" << (features.cooldownLabels ? "true" : "false") << "\n"
            << "enable_fps_control=" << (features.fpsControl ? "true" : "false") << "\n"
            << "enable_resolution_manager=" << (features.resolutionManager ? "true" : "false") << "\n"
            << "enable_minimized_rendering=" << (features.minimizedRendering ? "true" : "false") << "\n";

        if (!stream)
        {
            error = "Failed while writing profile: " + path.string();
            return false;
        }

        error.clear();
        return true;
    }
}
