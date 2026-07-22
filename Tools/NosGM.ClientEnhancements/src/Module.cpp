/*
 * NosGM.ClientEnhancements
 * Inspired by ImNotAVirus/NostaleWidget@fc1b6dda5d797efc24a053180d30702f8dad162a.
 * Copyright (c) 2022 ApourtArtt
 * Modifications Copyright (c) 2026 NosGM contributors
 * SPDX-License-Identifier: MIT
 */
#include "nosgm/client_enhancements/ClientIdentity.h"
#include "nosgm/client_enhancements/Profile.h"

#include <algorithm>
#include <cctype>
#include <filesystem>
#include <fstream>
#include <mutex>
#include <sstream>
#include <string>
#include <vector>

#ifdef _WIN32
#include <Windows.h>
#endif

namespace
{
    std::mutex g_stateMutex;
    bool g_initialized = false;
    std::wstring g_lastStatus = L"NosGM.ClientEnhancements has not been initialized.";

    std::string Lower(std::string value)
    {
        std::transform(value.begin(), value.end(), value.begin(), [](const unsigned char c)
        {
            return static_cast<char>(std::tolower(c));
        });
        return value;
    }

    std::string JsonEscape(const std::string& value)
    {
        std::ostringstream output;
        for (const auto character : value)
        {
            switch (character)
            {
            case '\\': output << "\\\\"; break;
            case '"': output << "\\\""; break;
            case '\n': output << "\\n"; break;
            case '\r': output << "\\r"; break;
            case '\t': output << "\\t"; break;
            default:
                if (static_cast<unsigned char>(character) < 0x20U)
                {
                    output << '?';
                }
                else
                {
                    output << character;
                }
                break;
            }
        }
        return output.str();
    }

#ifdef _WIN32
    std::wstring ToWide(const std::string& value)
    {
        if (value.empty())
        {
            return {};
        }

        const auto required = MultiByteToWideChar(
            CP_UTF8,
            MB_ERR_INVALID_CHARS,
            value.data(),
            static_cast<int>(value.size()),
            nullptr,
            0);
        if (required <= 0)
        {
            return std::wstring(value.begin(), value.end());
        }

        std::wstring result(static_cast<std::size_t>(required), L'\0');
        MultiByteToWideChar(
            CP_UTF8,
            MB_ERR_INVALID_CHARS,
            value.data(),
            static_cast<int>(value.size()),
            result.data(),
            required);
        return result;
    }
#else
    std::wstring ToWide(const std::string& value)
    {
        return std::wstring(value.begin(), value.end());
    }
#endif

    void SetStatus(const std::string& value)
    {
        std::scoped_lock lock(g_stateMutex);
        g_lastStatus = ToWide(value);
    }

    bool ProfileMatches(
        const nosgm::client::ClientProfile& profile,
        const nosgm::client::ClientIdentity& identity,
        std::string& reason)
    {
        if (!identity.IsX86())
        {
            reason = "The host executable is not x86.";
            return false;
        }
        if (identity.fileVersion != profile.expectedFileVersion)
        {
            reason = "File version mismatch. Expected " + profile.expectedFileVersion
                + ", found " + identity.fileVersion + ".";
            return false;
        }
        if (Lower(identity.sha256) != Lower(profile.expectedSha256))
        {
            reason = "SHA-256 mismatch. No client memory feature was activated.";
            return false;
        }

        reason.clear();
        return true;
    }

    void WriteStatusReport(
        const std::filesystem::path& profilePath,
        const nosgm::client::ClientProfile& profile,
        const nosgm::client::ClientIdentity& identity,
        const std::string& result,
        const std::string& detail,
        const std::vector<std::string>& unavailableFeatures)
    {
        std::error_code error;
        auto reportPath = profilePath;
        reportPath += L".status.json";

        std::ofstream output(reportPath, std::ios::trunc);
        if (!output)
        {
            return;
        }

        output
            << "{\n"
            << "  \"apiVersion\": 1,\n"
            << "  \"profile\": \"" << JsonEscape(profile.profileName) << "\",\n"
            << "  \"clientFile\": \"" << JsonEscape(identity.executablePath.filename().string()) << "\",\n"
            << "  \"fileVersion\": \"" << JsonEscape(identity.fileVersion) << "\",\n"
            << "  \"sha256\": \"" << JsonEscape(identity.sha256) << "\",\n"
            << "  \"architecture\": \"" << JsonEscape(identity.architecture) << "\",\n"
            << "  \"result\": \"" << JsonEscape(result) << "\",\n"
            << "  \"detail\": \"" << JsonEscape(detail) << "\",\n"
            << "  \"activeFeatures\": [\"diagnostics\"],\n"
            << "  \"unavailableRequestedFeatures\": [";

        for (std::size_t index = 0; index < unavailableFeatures.size(); ++index)
        {
            if (index != 0)
            {
                output << ", ";
            }
            output << '"' << JsonEscape(unavailableFeatures[index]) << '"';
        }

        output << "]\n}\n";
        (void)error;
    }
}

#ifdef _WIN32
#define NOSGM_EXPORT extern "C" __declspec(dllexport)
#else
#define NOSGM_EXPORT extern "C"
#endif

NOSGM_EXPORT unsigned int __stdcall NosGM_GetApiVersion()
{
    return 1U;
}

NOSGM_EXPORT int __stdcall NosGM_Initialize(const wchar_t* profilePathValue)
{
#ifndef _WIN32
    (void)profilePathValue;
    SetStatus("NosGM.ClientEnhancements is supported only on Windows.");
    return 10;
#else
    std::scoped_lock lock(g_stateMutex);
    if (g_initialized)
    {
        g_lastStatus = L"NosGM.ClientEnhancements is already initialized.";
        return 1;
    }
    if (profilePathValue == nullptr || profilePathValue[0] == L'\0')
    {
        g_lastStatus = L"A client profile path is required.";
        return 2;
    }

    const std::filesystem::path profilePath(profilePathValue);
    std::string error;
    const auto profile = nosgm::client::ClientProfile::Load(profilePath, error);
    if (!profile)
    {
        g_lastStatus = ToWide(error);
        return 3;
    }

    const auto identity = nosgm::client::InspectCurrentProcessHost(error);
    if (!identity)
    {
        g_lastStatus = ToWide(error);
        return 4;
    }

    if (!ProfileMatches(*profile, *identity, error))
    {
        WriteStatusReport(profilePath, *profile, *identity, "blocked", error, {});
        g_lastStatus = ToWide(error);
        return 5;
    }

    const auto unavailable = profile->RequestedUnavailableFeatures();
    std::ostringstream detail;
    detail << "Exact client identity verified. Diagnostics initialized.";
    if (!unavailable.empty())
    {
        detail << " Signature-dependent features remain disabled in the foundation build:";
        for (const auto& name : unavailable)
        {
            detail << ' ' << name;
        }
        detail << '.';
    }

    WriteStatusReport(profilePath, *profile, *identity, "verified", detail.str(), unavailable);
    g_initialized = true;
    g_lastStatus = ToWide(detail.str());
    return 0;
#endif
}

NOSGM_EXPORT int __stdcall NosGM_Shutdown()
{
    std::scoped_lock lock(g_stateMutex);
    g_initialized = false;
    g_lastStatus = L"NosGM.ClientEnhancements shut down. No patches remain active.";
    return 0;
}

NOSGM_EXPORT const wchar_t* __stdcall NosGM_GetLastStatus()
{
    std::scoped_lock lock(g_stateMutex);
    return g_lastStatus.c_str();
}

#ifdef _WIN32
BOOL APIENTRY DllMain(HMODULE module, const DWORD reason, LPVOID)
{
    if (reason == DLL_PROCESS_ATTACH)
    {
        DisableThreadLibraryCalls(module);
    }
    return TRUE;
}
#endif
