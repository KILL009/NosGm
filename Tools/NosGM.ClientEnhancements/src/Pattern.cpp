/*
 * NosGM.ClientEnhancements
 * Inspired by ImNotAVirus/NostaleWidget@fc1b6dda5d797efc24a053180d30702f8dad162a.
 * Copyright (c) 2022 ApourtArtt
 * Modifications Copyright (c) 2026 NosGM contributors
 * SPDX-License-Identifier: MIT
 */
#include "nosgm/client_enhancements/Pattern.h"

#include <charconv>
#include <cctype>
#include <stdexcept>
#include <utility>

namespace
{
    bool IsWhitespace(const char value) noexcept
    {
        return std::isspace(static_cast<unsigned char>(value)) != 0;
    }
}

namespace nosgm::client
{
    BytePattern::BytePattern(std::vector<PatternByte> bytes)
        : bytes_(std::move(bytes))
    {
    }

    BytePattern BytePattern::Parse(const std::string_view expression)
    {
        std::vector<PatternByte> bytes;
        std::size_t cursor = 0;

        while (cursor < expression.size())
        {
            while (cursor < expression.size() && IsWhitespace(expression[cursor]))
            {
                ++cursor;
            }

            if (cursor >= expression.size())
            {
                break;
            }

            const auto tokenStart = cursor;
            while (cursor < expression.size() && !IsWhitespace(expression[cursor]))
            {
                ++cursor;
            }

            const auto token = expression.substr(tokenStart, cursor - tokenStart);
            if (token == "?" || token == "??")
            {
                bytes.push_back(PatternByte{0, true});
                continue;
            }

            if (token.size() != 2)
            {
                throw std::invalid_argument("Pattern tokens must be two hexadecimal digits or ??.");
            }

            unsigned int parsed = 0;
            const auto* first = token.data();
            const auto* last = token.data() + token.size();
            const auto result = std::from_chars(first, last, parsed, 16);
            if (result.ec != std::errc{} || result.ptr != last || parsed > 0xFFU)
            {
                throw std::invalid_argument("Pattern contains an invalid hexadecimal token.");
            }

            bytes.push_back(PatternByte{static_cast<std::uint8_t>(parsed), false});
        }

        if (bytes.empty())
        {
            throw std::invalid_argument("Pattern cannot be empty.");
        }

        return BytePattern(std::move(bytes));
    }

    std::optional<std::size_t> BytePattern::FindFirst(
        const std::uint8_t* data,
        const std::size_t size) const noexcept
    {
        if (data == nullptr || bytes_.empty() || size < bytes_.size())
        {
            return std::nullopt;
        }

        const auto lastStart = size - bytes_.size();
        for (std::size_t offset = 0; offset <= lastStart; ++offset)
        {
            bool matches = true;
            for (std::size_t index = 0; index < bytes_.size(); ++index)
            {
                const auto& expected = bytes_[index];
                if (!expected.wildcard && data[offset + index] != expected.value)
                {
                    matches = false;
                    break;
                }
            }

            if (matches)
            {
                return offset;
            }
        }

        return std::nullopt;
    }

    const std::vector<PatternByte>& BytePattern::Bytes() const noexcept
    {
        return bytes_;
    }

    bool BytePattern::Empty() const noexcept
    {
        return bytes_.empty();
    }

    std::size_t BytePattern::Size() const noexcept
    {
        return bytes_.size();
    }
}
