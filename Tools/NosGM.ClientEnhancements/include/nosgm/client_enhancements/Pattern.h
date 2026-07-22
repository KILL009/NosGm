/*
 * NosGM.ClientEnhancements
 * Inspired by ImNotAVirus/NostaleWidget@fc1b6dda5d797efc24a053180d30702f8dad162a.
 * Copyright (c) 2022 ApourtArtt
 * Modifications Copyright (c) 2026 NosGM contributors
 * SPDX-License-Identifier: MIT
 */
#pragma once

#include <cstddef>
#include <cstdint>
#include <optional>
#include <string>
#include <string_view>
#include <vector>

namespace nosgm::client
{
    struct PatternByte
    {
        std::uint8_t value{};
        bool wildcard{};
    };

    class BytePattern final
    {
    public:
        static BytePattern Parse(std::string_view expression);

        [[nodiscard]] std::optional<std::size_t> FindFirst(
            const std::uint8_t* data,
            std::size_t size) const noexcept;

        [[nodiscard]] const std::vector<PatternByte>& Bytes() const noexcept;
        [[nodiscard]] bool Empty() const noexcept;
        [[nodiscard]] std::size_t Size() const noexcept;

    private:
        explicit BytePattern(std::vector<PatternByte> bytes);

        std::vector<PatternByte> bytes_;
    };
}
