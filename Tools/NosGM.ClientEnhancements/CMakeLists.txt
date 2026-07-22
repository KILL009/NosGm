# NosGM.ClientEnhancements
# Inspired by ImNotAVirus/NostaleWidget@fc1b6dda5d797efc24a053180d30702f8dad162a.
# Copyright (c) 2022 ApourtArtt
# Modifications Copyright (c) 2026 NosGM contributors
# SPDX-License-Identifier: MIT
cmake_minimum_required(VERSION 3.22)

project(NosGMClientEnhancements VERSION 0.1.0 LANGUAGES CXX)

option(NOSGM_CLIENT_ENHANCEMENTS_BUILD_TESTS "Build unit tests" ON)

add_library(nosgm_client_core STATIC
    src/Pattern.cpp
    src/Profile.cpp)

target_include_directories(nosgm_client_core
    PUBLIC
        ${CMAKE_CURRENT_SOURCE_DIR}/include)

target_compile_features(nosgm_client_core PUBLIC cxx_std_17)

if(MSVC)
    target_compile_options(nosgm_client_core PRIVATE /W4 /permissive- /EHsc)
else()
    target_compile_options(nosgm_client_core PRIVATE -Wall -Wextra -Wpedantic)
endif()

if(WIN32)
    if(NOT CMAKE_SIZEOF_VOID_P EQUAL 4)
        message(FATAL_ERROR "NosGM.ClientEnhancements must be configured with -A Win32.")
    endif()

    add_library(NosGM.ClientEnhancements SHARED
        src/ClientIdentity.cpp
        src/MemoryGuard.cpp
        src/PatchTransaction.cpp
        src/Module.cpp)

    target_include_directories(NosGM.ClientEnhancements
        PRIVATE
            ${CMAKE_CURRENT_SOURCE_DIR}/include)
    target_link_libraries(NosGM.ClientEnhancements
        PRIVATE
            nosgm_client_core
            bcrypt
            version
            psapi)
    target_compile_features(NosGM.ClientEnhancements PRIVATE cxx_std_17)
    target_compile_definitions(NosGM.ClientEnhancements PRIVATE WIN32_LEAN_AND_MEAN NOMINMAX)
    target_compile_options(NosGM.ClientEnhancements PRIVATE /W4 /permissive- /EHsc)
    set_target_properties(NosGM.ClientEnhancements PROPERTIES PREFIX "")

    add_executable(NosGM.ClientProbe
        probe/Main.cpp
        src/ClientIdentity.cpp)

    target_include_directories(NosGM.ClientProbe
        PRIVATE
            ${CMAKE_CURRENT_SOURCE_DIR}/include)
    target_link_libraries(NosGM.ClientProbe
        PRIVATE
            nosgm_client_core
            bcrypt
            version)
    target_compile_features(NosGM.ClientProbe PRIVATE cxx_std_17)
    target_compile_definitions(NosGM.ClientProbe PRIVATE WIN32_LEAN_AND_MEAN NOMINMAX)
    target_compile_options(NosGM.ClientProbe PRIVATE /W4 /permissive- /EHsc)
endif()

if(NOSGM_CLIENT_ENHANCEMENTS_BUILD_TESTS)
    enable_testing()

    add_executable(NosGM.ClientEnhancements.Tests tests/Tests.cpp)
    target_link_libraries(NosGM.ClientEnhancements.Tests PRIVATE nosgm_client_core)
    target_compile_features(NosGM.ClientEnhancements.Tests PRIVATE cxx_std_17)

    if(MSVC)
        target_compile_options(NosGM.ClientEnhancements.Tests PRIVATE /W4 /permissive- /EHsc)
    else()
        target_compile_options(NosGM.ClientEnhancements.Tests PRIVATE -Wall -Wextra -Wpedantic)
    endif()

    add_test(NAME client-enhancements-tests COMMAND NosGM.ClientEnhancements.Tests)
endif()
