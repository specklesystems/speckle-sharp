cmake_minimum_required (VERSION 3.16)

function (SetCompilerOptions target ac_version)
	if (${ac_version} LESS "27")
		target_compile_features (${target} PUBLIC cxx_std_14)
	else ()
		target_compile_features (${target} PUBLIC cxx_std_17)
	endif ()
	target_compile_options (${target} PUBLIC "$<$<CONFIG:Debug>:-DDEBUG>")
	if (WIN32)
		target_compile_options (${target} PUBLIC /W4 /WX
			/Zc:wchar_t-
			/wd4499
			/wd4996
			/wd4390
		)
	else ()
		target_compile_options (${target} PUBLIC -Wall -Wextra -Werror
			-fvisibility=hidden
			-Wno-multichar
			-Wno-ctor-dtor-privacy
			-Wno-invalid-offsetof
			-Wno-ignored-qualifiers
			-Wno-reorder
			-Wno-overloaded-virtual
			-Wno-missing-braces
			-Wno-unused-parameter
			-Wno-deprecated				# HashTable.hpp:1752 error: 'sprintf' is deprecated
			-Wno-unknown-pragmas
			-Wno-shorten-64-to-32		# BrepExternalBufferArray.hpp:34: error: implicit conversion loses integer precision: 'long long' to 'const GS::USize' (aka 'const unsigned int')
			-Wno-unused-private-field
			)
	endif ()
endfunction ()

macro (header_include_directories return_list)
	file (GLOB_RECURSE new_list
		*.h
		*.hpp
	)
	set (dir_list "")

	foreach (file_path ${new_list})
		get_filename_component (dir_path ${file_path} PATH)
		set (dir_list ${dir_list} ${dir_path})
	endforeach ()

	list (REMOVE_DUPLICATES dir_list)
	set (${return_list} ${dir_list})

endmacro ()

set_property (GLOBAL PROPERTY USE_FOLDERS ON)

set (CMAKE_SUPPRESS_REGENERATION 1)
set (CMAKE_CONFIGURATION_TYPES Debug;Release;RelWithDebInfo)
set (CMAKE_OSX_DEPLOYMENT_TARGET 10.15)
set (AC_API_DEVKIT_DIR $ENV{AC_API_DEVKIT_DIR} CACHE PATH "API DevKit directory.")
set (AC_ADDON_NAME "Speckle Connector" CACHE STRING "Add-On name.")
set (AC_ADDON_LANGUAGE "INT" CACHE STRING "Add-On language code.")
set (AC_MDID_DEV "1" CACHE STRING "Your Developer ID")
set (AC_MDID_LOC "1" CACHE STRING "Add-On Local ID")

set (ACAPINC_FILE_LOCATION ${AC_API_DEVKIT_DIR}/Support/Inc/ACAPinc.h)
if (EXISTS ${ACAPINC_FILE_LOCATION})
	file (READ ${ACAPINC_FILE_LOCATION} ACAPIncContent)
	string (REGEX MATCHALL "#define[ \t]+ServerMainVers_([0-9][0-9])" VersionList ${ACAPIncContent})
	set (ARCHICAD_VERSION ${CMAKE_MATCH_1})
	message (STATUS "Archicad Version: ${ARCHICAD_VERSION}")
else ()
	message (FATAL_ERROR "Failed to detect Archicad version, please check the value of the AC_API_DEVKIT_DIR variable.")
endif ()

if (WIN32)
	add_definitions (-DUNICODE -D_UNICODE)
else ()
	add_definitions (-Dmacintosh=1)
	if (${ARCHICAD_VERSION} GREATER_EQUAL "26")
		set (CMAKE_OSX_ARCHITECTURES "x86_64;arm64" PARENT_SCOPE CACHE STRING "" FORCE)
	endif ()
endif ()
add_definitions (-DACExtension)

project (${AC_ADDON_NAME})

set (AddOnSourcesFolder Sources/AddOn)
set (AddOnResourcesFolder Sources/AddOnResources)

# AddOnResources

set (ResourceObjectsDir ${CMAKE_BINARY_DIR}/ResourceObjects)

configure_file (${AddOnResourcesFolder}/ResourceMDIDIds.hpp.in ${ResourceObjectsDir}/ResourceMDIDIds.hpp)

file (GLOB AddOnImageFiles
	${AddOnResourcesFolder}/RFIX/Images/*.svg
)
if (WIN32)
	file (GLOB AddOnResourceFiles
		${AddOnResourcesFolder}/R${AC_ADDON_LANGUAGE}/*.grc
		${AddOnResourcesFolder}/RFIX/*.grc
		${AddOnResourcesFolder}/RFIX/*.grc
		${AddOnResourcesFolder}/RFIX.win/*.rc2
		${AddOnResourcesFolder}/Tools/*.py
	)
else ()
	file (GLOB AddOnResourceFiles
		${AddOnResourcesFolder}/R${AC_ADDON_LANGUAGE}/*.grc
		${AddOnResourcesFolder}/RFIX/*.grc
		${AddOnResourcesFolder}/RFIX.mac/*.plist
		${AddOnResourcesFolder}/Tools/*.py
	)
endif ()

source_group ("Images" FILES ${AddOnImageFiles})
source_group ("Resources" FILES ${AddOnResourceFiles})
add_custom_target (
	AddOnResources ALL
	DEPENDS "${ResourceObjectsDir}/AddOnResources.stamp"
	SOURCES ${AddOnResourceFiles} ${AddOnImageFiles}
)

get_filename_component (AddOnSourcesFolderAbsolute "${CMAKE_CURRENT_LIST_DIR}/${AddOnSourcesFolder}" ABSOLUTE)
get_filename_component (AddOnResourcesFolderAbsolute "${CMAKE_CURRENT_LIST_DIR}/${AddOnResourcesFolder}" ABSOLUTE)
if (WIN32)
	add_custom_command (
		OUTPUT "${ResourceObjectsDir}/AddOnResources.stamp"
		DEPENDS ${AddOnResourceFiles} ${AddOnImageFiles}
		COMMENT "Compiling resources..."
		COMMAND ${CMAKE_COMMAND} -E make_directory "${ResourceObjectsDir}"
		COMMAND python "${AddOnResourcesFolderAbsolute}/Tools/CompileResources.py" "${AC_ADDON_LANGUAGE}" "${AC_API_DEVKIT_DIR}" "${AddOnSourcesFolderAbsolute}" "${AddOnResourcesFolderAbsolute}" "${ResourceObjectsDir}" "${ResourceObjectsDir}/${AC_ADDON_NAME}.res"
		COMMAND ${CMAKE_COMMAND} -E touch "${ResourceObjectsDir}/AddOnResources.stamp"
	)
else ()
	add_custom_command (
		OUTPUT "${ResourceObjectsDir}/AddOnResources.stamp" "${CMAKE_BINARY_DIR}/$CONFIGURATION/${AC_ADDON_NAME}.bundle/Contents/Resources/English.lproj/Localizable.strings"
		DEPENDS ${AddOnResourceFiles} ${AddOnImageFiles} 
		COMMENT "Compiling resources..."
		COMMAND ${CMAKE_COMMAND} -E make_directory "${ResourceObjectsDir}"
		COMMAND python3 "${AddOnResourcesFolderAbsolute}/Tools/CompileResources.py" "${AC_ADDON_LANGUAGE}" "${AC_API_DEVKIT_DIR}" "${AddOnSourcesFolderAbsolute}" "${AddOnResourcesFolderAbsolute}" "${ResourceObjectsDir}" "${CMAKE_BINARY_DIR}/$<CONFIG>/${AC_ADDON_NAME}.bundle/Contents/Resources"
		COMMAND ${CMAKE_COMMAND} -E copy "${AC_API_DEVKIT_DIR}/Support/Inc/PkgInfo" "${CMAKE_BINARY_DIR}/$<CONFIG>/${AC_ADDON_NAME}.bundle/Contents/PkgInfo"
		COMMAND ${CMAKE_COMMAND} -E touch "${ResourceObjectsDir}/AddOnResources.stamp"
	)
endif ()

# AddOn

file (GLOB_RECURSE AddOnHeaderFiles
	${AddOnSourcesFolder}/*.h
	${AddOnSourcesFolder}/*.hpp
)
file (GLOB_RECURSE AddOnSourceFiles
	${AddOnSourcesFolder}/*.c
	${AddOnSourcesFolder}/*.cpp
)
set (
	AddOnFiles
	${AddOnHeaderFiles}
	${AddOnSourceFiles}
)
source_group ("Sources" FILES ${AddOnFiles})
if (WIN32)
	add_library (AddOn SHARED ${AddOnFiles})
else ()
	add_library (AddOn MODULE ${AddOnFiles})
endif ()

set_target_properties (AddOn PROPERTIES OUTPUT_NAME ${AC_ADDON_NAME})
if (WIN32)
	set_target_properties (AddOn PROPERTIES SUFFIX ".apx")
	if (DEFINED AC_EXECUTABLE_PATH)
		set_target_properties (AddOn PROPERTIES VS_DEBUGGER_COMMAND "${AC_EXECUTABLE_PATH}")
	endif ()
else ()
	set_target_properties (AddOn PROPERTIES BUNDLE TRUE)
	set_target_properties (AddOn PROPERTIES MACOSX_BUNDLE_INFO_PLIST "${CMAKE_CURRENT_LIST_DIR}/${AddOnResourcesFolder}/RFIX.mac/Info.plist")
	set_target_properties (AddOn PROPERTIES LIBRARY_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/$<CONFIG>")
	if (DEFINED AC_EXECUTABLE_PATH)
		set_target_properties (AddOn PROPERTIES XCODE_SCHEME_EXECUTABLE "${AC_EXECUTABLE_PATH}")
	endif ()
endif ()

if (WIN32)
	target_link_options (AddOn PUBLIC "${ResourceObjectsDir}/${AC_ADDON_NAME}.res")
	target_link_options (AddOn PUBLIC /export:GetExportedFuncAddrs,@1 /export:SetImportedFuncAddrs,@2)
endif ()

header_include_directories (include_list)

target_include_directories (AddOn PUBLIC
	${AC_API_DEVKIT_DIR}/Support/Inc
	${include_list}
)

if (WIN32)
	if (${ARCHICAD_VERSION} LESS "27")
		target_link_libraries (AddOn
			"$<$<CONFIG:Debug>:${AC_API_DEVKIT_DIR}/Support/Lib/Win/ACAP_STATD.lib>"
			"$<$<CONFIG:Release>:${AC_API_DEVKIT_DIR}/Support/Lib/Win/ACAP_STAT.lib>"
			"$<$<CONFIG:RelWithDebInfo>:${AC_API_DEVKIT_DIR}/Support/Lib/Win/ACAP_STAT.lib>"
		)
	else ()
		target_link_libraries (AddOn
			"$<$<CONFIG:Debug>:${AC_API_DEVKIT_DIR}/Support/Lib/ACAP_STATD.lib>"
			"$<$<CONFIG:Release>:${AC_API_DEVKIT_DIR}/Support/Lib/ACAP_STAT.lib>"
			"$<$<CONFIG:RelWithDebInfo>:${AC_API_DEVKIT_DIR}/Support/Lib/ACAP_STAT.lib>"
		)
	endif ()
else ()
	find_library (CocoaFramework Cocoa)
	if (${ARCHICAD_VERSION} LESS "27")
		target_link_libraries (AddOn
			"${AC_API_DEVKIT_DIR}/Support/Lib/Mactel/libACAP_STAT.a"
			${CocoaFramework}
		)
	else ()
		target_link_libraries (AddOn
			"${AC_API_DEVKIT_DIR}/Support/Lib/libACAP_STAT.a"
			${CocoaFramework}
		)
	endif ()
endif ()

SetCompilerOptions (AddOn ARCHICAD_VERSION)
set_source_files_properties (${AddOnSourceFiles} PROPERTIES LANGUAGE CXX)

file (GLOB ModuleFolders ${AC_API_DEVKIT_DIR}/Support/Modules/*)
target_include_directories (AddOn PUBLIC ${ModuleFolders})
if (WIN32)
	file (GLOB LibFilesInFolder ${AC_API_DEVKIT_DIR}/Support/Modules/*/*/*.lib)
	target_link_libraries (AddOn ${LibFilesInFolder})
else ()
	file (GLOB FrameworkFilesInFolder ${AC_API_DEVKIT_DIR}/Support/Frameworks/*.framework)
	target_link_libraries (AddOn ${FrameworkFilesInFolder})
endif ()
add_dependencies (AddOn AddOnResources)

if (WIN32)
	if (DEFINED REGISTER_ARCHICAD_ADDON_PATH)
		file(TO_NATIVE_PATH "${CMAKE_BINARY_DIR}/$<CONFIG>/${AC_ADDON_NAME}.apx" native_path)
		add_custom_command (
			TARGET AddOn
			POST_BUILD
			COMMAND python "${REGISTER_ARCHICAD_ADDON_PATH}" register "${native_path}" ${ARCHICAD_VERSION}
	    )
	endif ()
else ()
	if (DEFINED REGISTER_ARCHICAD_ADDON_PATH)
		add_custom_command (
			TARGET AddOn
			POST_BUILD
			COMMAND python3 "${REGISTER_ARCHICAD_ADDON_PATH}" register "${CMAKE_BINARY_DIR}/$<CONFIG>/${AC_ADDON_NAME}.bundle" ${ARCHICAD_VERSION}
	    )
	endif ()
endif ()
