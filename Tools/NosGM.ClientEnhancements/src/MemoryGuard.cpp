/*
 * NosGM.ClientEnhancements
 * Inspired by ImNotAVirus/NostaleWidget@fc1b6dda5d797efc24a053180d30702f8dad162a.
 * Copyright (c) 2022 ApourtArtt
 * Modifications Copyright (c) 2026 NosGM contributors
 * SPDX-License-Identifier: MIT
 */
#include "nosgm/client_enhancements/MemoryGuard.h"

#include <cstdint>
#include <limits>

#ifdef _WIN32
#include <Windows.h>
#include <Psapi.h>
#endif

namespace
{
#ifdef _WIN32
    bool HasReadableProtection(const DWORD protection) noexcept
    {
        const auto normalized = protection & 0xFFU;
        return normalized == PAGE_READONLY
            || normalized == PAGE_READWRITE
            || normalized == PAGE_WRITECOPY
            || normalized == PAGE_EXECUTE_READ
            || normalized == PAGE_EXECUTE_READWRITE
            || normalized == PAGE_EXECUTE_WRITECOPY;
    }

    bool HasExecutableProtection(const DWORD protection) noexcept
    {
        const auto normalized = protection & 0xFFU;
        return normalized == PAGE_EXECUTE
            || normalized == PAGE_EXECUTE_READ
            || normalized == PAGE_EXECUTE_READWRITE
            || normalized == PAGE_EXECUTE_WRITECOPY;
    }

    bool ValidateRegion(
        const void* address,
        const std::size_t size,
        const bool requireExecutable,
        std::string& reason) noexcept
    {
        if (address == nullptr || size == 0)
        {
            reason = "Memory range is empty.";
            return false;
        }

        const auto start = reinterpret_cast<std::uintptr_t>(address);
        if (start > std::numeric_limits<std::uintptr_t>::max() - size)
        {
            reason = "Memory range overflows the address space.";
            return false;
        }
        const auto end = start + size;

        auto cursor = start;
        while (cursor < end)
        {
            MEMORY_BASIC_INFORMATION info{};
            if (VirtualQuery(reinterpret_cast<const void*>(cursor), &info, sizeof(info)) == 0)
            {
                reason = "VirtualQuery failed.";
                return false;
            }

            if (info.State != MEM_COMMIT || (info.Protect & PAGE_GUARD) != 0 || info.Protect == PAGE_NOACCESS)
            {
                reason = "Memory range is not committed and accessible.";
                return false;
            }

            if (!HasReadableProtection(info.Protect))
            {
                reason = "Memory range is not readable.";
                return false;
            }

            if (requireExecutable && !HasExecutableProtection(info.Protect))
            {
                reason = "Memory range is not executable.";
                return false;
            }

            const auto regionStart = reinterpret_cast<std::uintptr_t>(info.BaseAddress);
            const auto regionEnd = regionStart + info.RegionSize;
            if (regionEnd <= cursor)
            {
                reason = "VirtualQuery returned an invalid region.";
                return false;
            }
            cursor = regionEnd;
        }

        reason.clear();
        return true;
    }
#endif
}

namespace nosgm::client
{
    bool MemoryGuard::IsInsideMainModule(
        const void* address,
        const std::size_t size,
        std::string& reason) noexcept
    {
#ifdef _WIN32
        if (address == nullptr || size == 0)
        {
            reason = "Memory range is empty.";
            return false;
        }

        MODULEINFO info{};
        const auto module = GetModuleHandleW(nullptr);
        if (module == nullptr
            || !GetModuleInformation(GetCurrentProcess(), module, &info, sizeof(info)))
        {
            reason = "Unable to inspect the host module.";
            return false;
        }

        const auto moduleStart = reinterpret_cast<std::uintptr_t>(info.lpBaseOfDll);
        const auto moduleEnd = moduleStart + static_cast<std::uintptr_t>(info.SizeOfImage);
        const auto rangeStart = reinterpret_cast<std::uintptr_t>(address);
        if (rangeStart > std::numeric_limits<std::uintptr_t>::max() - size)
        {
            reason = "Memory range overflows the address space.";
            return false;
        }
        const auto rangeEnd = rangeStart + size;

        if (rangeStart < moduleStart || rangeEnd > moduleEnd)
        {
            reason = "Memory range is outside the host executable image.";
            return false;
        }

        reason.clear();
        return true;
#else
        (void)address;
        (void)size;
        reason = "Memory validation is supported only on Windows.";
        return false;
#endif
    }

    bool MemoryGuard::IsReadable(
        const void* address,
        const std::size_t size,
        std::string& reason) noexcept
    {
#ifdef _WIN32
        return ValidateRegion(address, size, false, reason);
#else
        (void)address;
        (void)size;
        reason = "Memory validation is supported only on Windows.";
        return false;
#endif
    }

    bool MemoryGuard::IsExecutable(
        const void* address,
        const std::size_t size,
        std::string& reason) noexcept
    {
#ifdef _WIN32
        return ValidateRegion(address, size, true, reason);
#else
        (void)address;
        (void)size;
        reason = "Memory validation is supported only on Windows.";
        return false;
#endif
    }
}
