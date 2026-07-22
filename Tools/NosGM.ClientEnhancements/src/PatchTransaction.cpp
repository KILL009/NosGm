/*
 * NosGM.ClientEnhancements
 * Inspired by ImNotAVirus/NostaleWidget@fc1b6dda5d797efc24a053180d30702f8dad162a.
 * Copyright (c) 2022 ApourtArtt
 * Modifications Copyright (c) 2026 NosGM contributors
 * SPDX-License-Identifier: MIT
 */
#include "nosgm/client_enhancements/PatchTransaction.h"

#include "nosgm/client_enhancements/MemoryGuard.h"

#include <algorithm>
#include <cstring>
#include <utility>

#ifdef _WIN32
#include <Windows.h>
#endif

namespace nosgm::client
{
    PatchTransaction::PatchTransaction(
        void* target,
        std::vector<std::uint8_t> expectedBytes,
        std::vector<std::uint8_t> replacementBytes)
        : target_(target),
          expectedBytes_(std::move(expectedBytes)),
          replacementBytes_(std::move(replacementBytes))
    {
    }

    PatchTransaction::~PatchTransaction()
    {
        std::string ignored;
        Restore(ignored);
    }

    bool PatchTransaction::Apply(std::string& reason) noexcept
    {
#ifdef _WIN32
        if (applied_)
        {
            reason = "Patch is already applied.";
            return false;
        }
        if (target_ == nullptr || expectedBytes_.empty())
        {
            reason = "Patch target and expected bytes are required.";
            return false;
        }
        if (expectedBytes_.size() != replacementBytes_.size())
        {
            reason = "Expected and replacement byte sequences must have the same length.";
            return false;
        }
        if (!MemoryGuard::IsInsideMainModule(target_, expectedBytes_.size(), reason)
            || !MemoryGuard::IsReadable(target_, expectedBytes_.size(), reason)
            || !MemoryGuard::IsExecutable(target_, expectedBytes_.size(), reason))
        {
            return false;
        }

        const auto* current = static_cast<const std::uint8_t*>(target_);
        if (!std::equal(expectedBytes_.begin(), expectedBytes_.end(), current))
        {
            reason = "Target bytes do not match the verified profile.";
            return false;
        }

        originalBytes_.assign(current, current + expectedBytes_.size());

        DWORD oldProtection = 0;
        if (!VirtualProtect(target_, replacementBytes_.size(), PAGE_EXECUTE_READWRITE, &oldProtection))
        {
            originalBytes_.clear();
            reason = "VirtualProtect could not make the target writable.";
            return false;
        }

        std::memcpy(target_, replacementBytes_.data(), replacementBytes_.size());
        FlushInstructionCache(GetCurrentProcess(), target_, replacementBytes_.size());

        applied_ = true;

        DWORD ignoredProtection = 0;
        const auto protectionRestored = VirtualProtect(
            target_,
            replacementBytes_.size(),
            oldProtection,
            &ignoredProtection) != FALSE;

        if (!protectionRestored)
        {
            std::string rollbackReason;
            if (Restore(rollbackReason))
            {
                reason = "Page protection restoration failed, so the patch was rolled back.";
            }
            else
            {
                reason = "Page protection restoration failed and rollback also failed: " + rollbackReason;
            }
            return false;
        }
        reason.clear();
        return true;
#else
        reason = "Patching is supported only on Windows.";
        return false;
#endif
    }

    bool PatchTransaction::Restore(std::string& reason) noexcept
    {
#ifdef _WIN32
        if (!applied_)
        {
            reason.clear();
            return true;
        }
        if (target_ == nullptr || originalBytes_.empty())
        {
            reason = "Original bytes are unavailable.";
            return false;
        }

        DWORD oldProtection = 0;
        if (!VirtualProtect(target_, originalBytes_.size(), PAGE_EXECUTE_READWRITE, &oldProtection))
        {
            reason = "VirtualProtect could not make the target writable for restoration.";
            return false;
        }

        std::memcpy(target_, originalBytes_.data(), originalBytes_.size());
        FlushInstructionCache(GetCurrentProcess(), target_, originalBytes_.size());

        DWORD ignoredProtection = 0;
        const auto protectionRestored = VirtualProtect(
            target_,
            originalBytes_.size(),
            oldProtection,
            &ignoredProtection) != FALSE;

        applied_ = false;
        originalBytes_.clear();

        if (!protectionRestored)
        {
            reason = "Original bytes were restored, but page protection restoration failed.";
            return false;
        }

        reason.clear();
        return true;
#else
        reason = "Patching is supported only on Windows.";
        return false;
#endif
    }

    bool PatchTransaction::IsApplied() const noexcept
    {
        return applied_;
    }
}
