/*
 * NosGM.ClientEnhancements
 * Inspired by ImNotAVirus/NostaleWidget@fc1b6dda5d797efc24a053180d30702f8dad162a.
 * Copyright (c) 2022 ApourtArtt
 * Modifications Copyright (c) 2026 NosGM contributors
 * SPDX-License-Identifier: MIT
 */
#pragma once

#include <cstddef>
#include <string>

namespace nosgm::client
{
    class MemoryGuard final
    {
    public:
        static bool IsInsideMainModule(const void* address, std::size_t size, std::string& reason) noexcept;
        static bool IsReadable(const void* address, std::size_t size, std::string& reason) noexcept;
        static bool IsExecutable(const void* address, std::size_t size, std::string& reason) noexcept;
    };
}
