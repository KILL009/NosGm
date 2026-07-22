/*
 * NosGM.ClientEnhancements
 * Inspired by ImNotAVirus/NostaleWidget@fc1b6dda5d797efc24a053180d30702f8dad162a.
 * Copyright (c) 2022 ApourtArtt
 * Modifications Copyright (c) 2026 NosGM contributors
 * SPDX-License-Identifier: MIT
 */
#include "nosgm/client_enhancements/Pattern.h"
#include "nosgm/client_enhancements/Profile.h"

#include <array>
#include <filesystem>
#include <fstream>
#include <iostream>
#include <stdexcept>
#include <string>

namespace
{
    void Require(const bool condition, const std::string& message)
    {
        if (!condition)
        {
            throw std::runtime_error(message);
        }
    }

    void TestPatternScanner()
    {
        const std::array<std::uint8_t, 8> data{0x90, 0x8B, 0x01, 0xFF, 0xE0, 0x90, 0xCC, 0x00};

        const auto exact = nosgm::client::BytePattern::Parse("8B 01 FF E0");
        Require(exact.FindFirst(data.data(), data.size()) == 1, "Exact pattern was not found.");

        const auto wildcard = nosgm::client::BytePattern::Parse("8B ?? FF E0");
        Require(wildcard.FindFirst(data.data(), data.size()) == 1, "Wildcard pattern was not found.");

        const auto missing = nosgm::client::BytePattern::Parse("DE AD BE EF");
        Require(!missing.FindFirst(data.data(), data.size()).has_value(), "Missing pattern unexpectedly matched.");

        bool rejected = false;
        try
        {
            (void)nosgm::client::BytePattern::Parse("GG");
        }
        catch (const std::invalid_argument&)
        {
            rejected = true;
        }
        Require(rejected, "Invalid pattern token was accepted.");
    }

    void TestProfileRoundTrip()
    {
        const auto path = std::filesystem::temp_directory_path() / "nosgm-client-profile-test.ini";

        nosgm::client::ClientProfile profile;
        profile.profileName = "test-profile";
        profile.expectedFileVersion = "0.9.3.3255";
        profile.expectedSha256 = std::string(64, 'a');
        profile.targetArchitecture = "x86";
        profile.features.diagnostics = true;

        std::string error;
        Require(profile.Save(path, error), "Profile save failed: " + error);

        const auto loaded = nosgm::client::ClientProfile::Load(path, error);
        Require(loaded.has_value(), "Profile load failed: " + error);
        Require(loaded->profileName == profile.profileName, "Profile name changed during round trip.");
        Require(loaded->expectedFileVersion == profile.expectedFileVersion, "Version changed during round trip.");
        Require(loaded->expectedSha256 == profile.expectedSha256, "SHA-256 changed during round trip.");
        Require(loaded->RequestedUnavailableFeatures().empty(), "Disabled features were reported as requested.");

        std::error_code removeError;
        std::filesystem::remove(path, removeError);
    }

    void TestUnsafeProfileRejected()
    {
        const auto path = std::filesystem::temp_directory_path() / "nosgm-client-profile-invalid-test.ini";
        {
            std::ofstream stream(path, std::ios::trunc);
            stream
                << "profile_name=invalid\n"
                << "expected_file_version=0.9.3.3255\n"
                << "expected_sha256=not-a-hash\n"
                << "target_architecture=x86\n";
        }

        std::string error;
        const auto loaded = nosgm::client::ClientProfile::Load(path, error);
        Require(!loaded.has_value(), "Invalid SHA-256 profile was accepted.");
        Require(error.find("64 hexadecimal") != std::string::npos, "Profile rejection reason was unclear.");

        std::error_code removeError;
        std::filesystem::remove(path, removeError);
    }
}

int main()
{
    try
    {
        TestPatternScanner();
        TestProfileRoundTrip();
        TestUnsafeProfileRejected();
        std::cout << "NosGM.ClientEnhancements tests passed.\n";
        return 0;
    }
    catch (const std::exception& exception)
    {
        std::cerr << "Test failure: " << exception.what() << '\n';
        return 1;
    }
}
