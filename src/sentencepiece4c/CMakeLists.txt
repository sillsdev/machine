# CMakeList.txt : CMake project for sentencepiece4c, include source and define
# project specific logic here.
#
cmake_minimum_required (VERSION 3.15)
cmake_policy(SET CMP0091 NEW)
set(CMAKE_POLICY_DEFAULT_CMP0077 NEW)
set(CMAKE_MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>")
project ("sentencepiece4c" VERSION 0.1.96 LANGUAGES CXX C)
set(CMAKE_CXX_STANDARD 11)
set(CMAKE_CXX_STANDARD_REQUIRED True)

include(FetchContent)

set(SPM_ENABLE_SHARED OFF)

FetchContent_Declare(
    sentencepiece
    GIT_REPOSITORY https://github.com/google/sentencepiece.git
    GIT_TAG v0.1.96
)
FetchContent_MakeAvailable(sentencepiece)

# Add source to this project's executable.
add_library(sentencepiece4c SHARED
    sentencepiece4c.cpp
    sentencepiece4c.h
)

target_include_directories(sentencepiece4c PRIVATE ${sentencepiece_SOURCE_DIR}/src)

target_link_libraries(sentencepiece4c PRIVATE
    sentencepiece-static
    sentencepiece_train-static
)

set_target_properties(sentencepiece4c
    PROPERTIES
    ARCHIVE_OUTPUT_DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR}/bin
    LIBRARY_OUTPUT_DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR}/bin
    RUNTIME_OUTPUT_DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR}/bin
    ARCHIVE_OUTPUT_DIRECTORY_RELEASE ${CMAKE_CURRENT_SOURCE_DIR}/bin
    LIBRARY_OUTPUT_DIRECTORY_RELEASE ${CMAKE_CURRENT_SOURCE_DIR}/bin
    RUNTIME_OUTPUT_DIRECTORY_RELEASE ${CMAKE_CURRENT_SOURCE_DIR}/bin
)
