/*
 * NosGM.ClientEnhancements
 * Inspired by ImNotAVirus/NostaleWidget@fc1b6dda5d797efc24a053180d30702f8dad162a.
 * Copyright (c) 2022 ApourtArtt
 * Modifications Copyright (c) 2026 NosGM contributors
 * SPDX-License-Identifier: MIT
 */
#pragma once

#include <cstdint>
#include <string>
#include <vector>

namespace nosgm::client
{
    class PatchTransaction final
    {
    public:
        PatchTransaction(
            void* target,
            std::vector<std::uint8_t> expectedBytes,
            std::vector<std::uint8_t> replacementBytes);

        PatchTransaction(const PatchTransaction&) = delete;
        PatchTransaction& operator=(const PatchTransaction&) = delete;

        ~PatchTransaction();

        bool Apply(std::string& reason) noexcept;
        bool Restore(std::string& reason) noexcept;
        [[nodiscard]] bool IsApplied() const noexcept;

    private:
        void* target_{};
        std::vector<std::uint8_t> expectedBytes_;
        std::vector<std::uint8_t> replacementBytes_;
        std::vector<std::uint8_t> originalBytes_;
        bool applied_{};
    };
}
