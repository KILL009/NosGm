/*
 * NosGM.ClientEnhancements
 * Inspired by ImNotAVirus/NostaleWidget@fc1b6dda5d797efc24a053180d30702f8dad162a.
 * Copyright (c) 2022 ApourtArtt
 * Modifications Copyright (c) 2026 NosGM contributors
 * SPDX-License-Identifier: MIT
 */
#include "nosgm/client_enhancements/ClientIdentity.h"
#include "nosgm/client_enhancements/Profile.h"

#include <filesystem>
#include <iostream>
#include <string>

namespace
{
    void PrintUsage()
    {
        std::wcout
            << L"NosGM.ClientProbe\n"
            << L"Usage:\n"
            << L"  NosGM.ClientProbe.exe --input <NostaleClient.exe> "
               L"[--profile-output <client-profile.ini>]\n";
    }
}

int wmain(const int argc, wchar_t* argv[])
{
    std::filesystem::path input;
    std::filesystem::path profileOutput = L"client-profile.generated.ini";

    for (int index = 1; index < argc; ++index)
    {
        const std::wstring argument = argv[index];
        if (argument == L"--input" && index + 1 < argc)
        {
            input = argv[++index];
        }
        else if (argument == L"--profile-output" && index + 1 < argc)
        {
            profileOutput = argv[++index];
        }
        else if (argument == L"--help" || argument == L"-h")
        {
            PrintUsage();
            return 0;
        }
        else
        {
            std::wcerr << L"Unknown or incomplete argument: " << argument << L"\n";
            PrintUsage();
            return 2;
        }
    }

    if (input.empty())
    {
        PrintUsage();
        return 2;
    }

    std::string error;
    const auto identity = nosgm::client::InspectClientExecutable(input, error);
    if (!identity)
    {
        std::cerr << "Probe failed: " << error << '\n';
        return 3;
    }

    std::cout
        << "File: " << identity->executablePath.filename().string() << '\n'
        << "Version: " << identity->fileVersion << '\n'
        << "Architecture: " << identity->architecture << '\n'
        << "SHA-256: " << identity->sha256 << '\n';

    if (!identity->IsX86())
    {
        std::cerr << "Blocked: NosGM.ClientEnhancements supports only the x86 client.\n";
        return 4;
    }

    nosgm::client::ClientProfile profile;
    profile.profileName = "local-" + identity->fileVersion;
    profile.expectedFileVersion = identity->fileVersion;
    profile.expectedSha256 = identity->sha256;
    profile.targetArchitecture = identity->architecture;
    profile.features.diagnostics = true;

    if (!profile.Save(profileOutput, error))
    {
        std::cerr << "Profile creation failed: " << error << '\n';
        return 5;
    }

    std::cout << "Profile written to: " << profileOutput.string() << '\n';
    std::cout << "All signature-dependent features remain disabled by default.\n";
    return 0;
}
