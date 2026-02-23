cmake_minimum_required(VERSION 3.16)
project(categorybars LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

find_package(Qt6 REQUIRED COMPONENTS Core Gui Quick Quick3D Graphs)

qt_standard_project_setup()

qt_add_executable(categorybars
    main.cpp
    categorydatareader.h
    categorydatareader.cpp
)

qt6_add_qml_module(categorybars
    URI CategoryBarsExample
    VERSION 1.0
    QML_FILES
        qml/categorybars/main.qml
    RESOURCE_PREFIX /
)

target_link_libraries(categorybars PRIVATE
    Qt6::Core
    Qt6::Gui
    Qt6::Quick
    Qt6::Quick3D
    Qt6::Graphs
)

set_target_properties(categorybars PROPERTIES
    WIN32_EXECUTABLE TRUE
    MACOSX_BUNDLE TRUE
)
