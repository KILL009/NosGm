/*
 * NosGM.ClientEnhancements
 * Inspired by ImNotAVirus/NostaleWidget@fc1b6dda5d797efc24a053180d30702f8dad162a.
 * Copyright (c) 2022 ApourtArtt
 * Modifications Copyright (c) 2026 NosGM contributors
 * SPDX-License-Identifier: MIT
 */
#include "nosgm/client_enhancements/ClientIdentity.h"

#include <algorithm>
#include <array>
#include <cctype>
#include <fstream>
#include <iomanip>
#include <sstream>
#include <vector>

#ifdef _WIN32
#include <Windows.h>
#include <bcrypt.h>
#include <winver.h>
#endif

namespace
{
    std::string Lower(std::string value)
    {
        std::transform(value.begin(), value.end(), value.begin(), [](const unsigned char c)
        {
            return static_cast<char>(std::tolower(c));
        });
        return value;
    }

#ifdef _WIN32
    std::optional<std::string> ReadFileVersion(
        const std::filesystem::path& path,
        std::string& error)
    {
        DWORD ignored = 0;
        const auto size = GetFileVersionInfoSizeW(path.c_str(), &ignored);
        if (size == 0)
        {
            error = "The executable does not contain readable version information.";
            return std::nullopt;
        }

        std::vector<std::byte> buffer(size);
        if (!GetFileVersionInfoW(path.c_str(), 0, size, buffer.data()))
        {
            error = "GetFileVersionInfoW failed with error " + std::to_string(GetLastError()) + ".";
            return std::nullopt;
        }

        VS_FIXEDFILEINFO* info = nullptr;
        UINT infoSize = 0;
        if (!VerQueryValueW(buffer.data(), L"\\", reinterpret_cast<void**>(&info), &infoSize)
            || info == nullptr
            || infoSize < sizeof(VS_FIXEDFILEINFO))
        {
            error = "The executable version block is invalid.";
            return std::nullopt;
        }

        std::ostringstream stream;
        stream
            << HIWORD(info->dwFileVersionMS) << '.'
            << LOWORD(info->dwFileVersionMS) << '.'
            << HIWORD(info->dwFileVersionLS) << '.'
            << LOWORD(info->dwFileVersionLS);
        return stream.str();
    }

    std::optional<std::string> ReadArchitecture(
        const std::filesystem::path& path,
        std::string& error)
    {
        std::ifstream stream(path, std::ios::binary);
        if (!stream)
        {
            error = "Unable to open executable for PE inspection.";
            return std::nullopt;
        }

        IMAGE_DOS_HEADER dos{};
        stream.read(reinterpret_cast<char*>(&dos), sizeof(dos));
        if (!stream || dos.e_magic != IMAGE_DOS_SIGNATURE || dos.e_lfanew <= 0)
        {
            error = "The selected file is not a valid PE executable.";
            return std::nullopt;
        }

        stream.seekg(dos.e_lfanew, std::ios::beg);
        DWORD signature = 0;
        IMAGE_FILE_HEADER header{};
        stream.read(reinterpret_cast<char*>(&signature), sizeof(signature));
        stream.read(reinterpret_cast<char*>(&header), sizeof(header));
        if (!stream || signature != IMAGE_NT_SIGNATURE)
        {
            error = "The selected file contains an invalid PE header.";
            return std::nullopt;
        }

        switch (header.Machine)
        {
        case IMAGE_FILE_MACHINE_I386:
            return std::string("x86");
        case IMAGE_FILE_MACHINE_AMD64:
            return std::string("x64");
        case IMAGE_FILE_MACHINE_ARM64:
            return std::string("arm64");
        default:
            std::ostringstream streamValue;
            streamValue << "machine-0x" << std::hex << std::uppercase << header.Machine;
            return streamValue.str();
        }
    }

    std::optional<std::string> ComputeSha256(
        const std::filesystem::path& path,
        std::string& error)
    {
        BCRYPT_ALG_HANDLE algorithm = nullptr;
        BCRYPT_HASH_HANDLE hash = nullptr;
        std::vector<unsigned char> hashObject;
        std::array<unsigned char, 32> digest{};

        const auto closeHandles = [&]()
        {
            if (hash != nullptr)
            {
                BCryptDestroyHash(hash);
                hash = nullptr;
            }
            if (algorithm != nullptr)
            {
                BCryptCloseAlgorithmProvider(algorithm, 0);
                algorithm = nullptr;
            }
        };

        if (BCryptOpenAlgorithmProvider(&algorithm, BCRYPT_SHA256_ALGORITHM, nullptr, 0) < 0)
        {
            error = "BCryptOpenAlgorithmProvider failed.";
            return std::nullopt;
        }

        DWORD objectSize = 0;
        DWORD copied = 0;
        if (BCryptGetProperty(
                algorithm,
                BCRYPT_OBJECT_LENGTH,
                reinterpret_cast<PUCHAR>(&objectSize),
                sizeof(objectSize),
                &copied,
                0) < 0)
        {
            closeHandles();
            error = "BCryptGetProperty failed.";
            return std::nullopt;
        }

        hashObject.resize(objectSize);
        if (BCryptCreateHash(
                algorithm,
                &hash,
                hashObject.data(),
                static_cast<ULONG>(hashObject.size()),
                nullptr,
                0,
                0) < 0)
        {
            closeHandles();
            error = "BCryptCreateHash failed.";
            return std::nullopt;
        }

        std::ifstream stream(path, std::ios::binary);
        if (!stream)
        {
            closeHandles();
            error = "Unable to open executable for SHA-256 calculation.";
            return std::nullopt;
        }

        std::array<char, 64 * 1024> buffer{};
        while (stream)
        {
            stream.read(buffer.data(), static_cast<std::streamsize>(buffer.size()));
            const auto count = stream.gcount();
            if (count > 0
                && BCryptHashData(
                    hash,
                    reinterpret_cast<PUCHAR>(buffer.data()),
                    static_cast<ULONG>(count),
                    0) < 0)
            {
                closeHandles();
                error = "BCryptHashData failed.";
                return std::nullopt;
            }
        }

        if (!stream.eof())
        {
            closeHandles();
            error = "Failed while reading executable for SHA-256 calculation.";
            return std::nullopt;
        }

        if (BCryptFinishHash(hash, digest.data(), static_cast<ULONG>(digest.size()), 0) < 0)
        {
            closeHandles();
            error = "BCryptFinishHash failed.";
            return std::nullopt;
        }

        closeHandles();

        std::ostringstream output;
        output << std::hex << std::setfill('0');
        for (const auto value : digest)
        {
            output << std::setw(2) << static_cast<unsigned int>(value);
        }
        return Lower(output.str());
    }
#endif
}

namespace nosgm::client
{
    bool ClientIdentity::IsX86() const noexcept
    {
        return architecture == "x86";
    }

    std::optional<ClientIdentity> InspectClientExecutable(
        const std::filesystem::path& executablePath,
        std::string& error)
    {
#ifdef _WIN32
        std::error_code fileError;
        if (!std::filesystem::is_regular_file(executablePath, fileError))
        {
            error = "Client executable was not found: " + executablePath.string();
            return std::nullopt;
        }

        auto version = ReadFileVersion(executablePath, error);
        if (!version)
        {
            return std::nullopt;
        }

        auto architecture = ReadArchitecture(executablePath, error);
        if (!architecture)
        {
            return std::nullopt;
        }

        auto sha256 = ComputeSha256(executablePath, error);
        if (!sha256)
        {
            return std::nullopt;
        }

        ClientIdentity identity;
        identity.executablePath = std::filesystem::absolute(executablePath, fileError);
        if (fileError)
        {
            identity.executablePath = executablePath;
        }
        identity.fileVersion = *version;
        identity.sha256 = *sha256;
        identity.architecture = *architecture;
        error.clear();
        return identity;
#else
        (void)executablePath;
        error = "Client inspection is supported only on Windows.";
        return std::nullopt;
#endif
    }

    std::optional<ClientIdentity> InspectCurrentProcessHost(std::string& error)
    {
#ifdef _WIN32
        std::vector<wchar_t> buffer(32768);
        const auto length = GetModuleFileNameW(nullptr, buffer.data(), static_cast<DWORD>(buffer.size()));
        if (length == 0 || length >= buffer.size())
        {
            error = "GetModuleFileNameW failed with error " + std::to_string(GetLastError()) + ".";
            return std::nullopt;
        }
        return InspectClientExecutable(std::filesystem::path(std::wstring(buffer.data(), length)), error);
#else
        error = "Current process inspection is supported only on Windows.";
        return std::nullopt;
#endif
    }
}
