# Changelog

## 2022-09-19

* Updated glslang to latest trunk (#112)
* Added Metal compiler 3.0 beta 4 (#111)
* Added Slang compiler v0.24.20 (#111)
* Added RGA 2.6.2

## 2022-07-12

* Added support for ANSI codes in error output (#104)
* Added all dxc target versions (#110)
* Added Mali offline compiler 7.6.0
* All ASICs are now shown for RGA compiler (#108)
* Update DXC compiler (#107)

## 2022-02-02

* Added RGA 2.6 (which enables DX12 offline mode)
* Upgraded website to ASP.NET Core 6.0

## 2021-10-26

* Added Metal compiler 2.0

## 2021-10-13

* Added Naga compiler v0.7.0 (#101)

## 2021-09-25

* Added Metal compiler 2.0 beta 5
* Added binary output for Metal .air file

## 2021-06-23

* Added Metal compiler (using Metal Developer Tools for Windows)

## 2021-06-22

* Added DXIL, SPIR-V & DXBC targets for Slang (#96)
* New compiler - [Naga](https://github.com/gfx-rs/naga)
* Added SPIRV-Cross v2021-01-15

## 2021-04-15

* Added Mali offline compiler 7.3.0

## 2020-12-24

* Updated Slang with RTX output
* Addd Rust GPU compiler
* Added RGA 2.4

## 2020-10-15

* Added Tint compiler and WGSL language

## 2020-09-29

* Added RGA 2.3.1

## 2020-06-12

* Updated Slang with CUDA/C++ output

## 2020-02-25

* Fixed scrolling (#63)

## 2020-02-23

* Added GLSL syntax highlighting (#64)

## 2020-02-22

* Added SPIR-V syntax highlighting

## 2020-02-16

* Fixed RGA compiler errors not being reported correctly (#62)
* Updated DXC to latest trunk
* Updated glslang to latest trunk
* Updated SMOL-V to latest trunk
* Added SPIRV-Cross v2020-01-16
* Added RGA v2.3
* Added Slang v0.13.10
* Added spirv1.x output targets to glslang

## 2018-08-25

* New compiler - [RGA](https://github.com/GPUOpen-Tools/RGA)

## 2018-07-02

* Fixed ISPC interface name (#36)

## 2018-06-29

* New compiler - [ANGLE](https://github.com/google/angle)
* Added [JSON reflection backend](https://github.com/KhronosGroup/SPIRV-Cross/issues/544) to SPIRV-Cross compiler

## 2018-06-28

* New compiler - [spirv-remap](https://github.com/KhronosGroup/glslang/blob/master/README-spirv-remap.txt)
* New compiler - [spirv-as](https://github.com/KhronosGroup/SPIRV-Tools#assembler-binary-parser-and-disassembler)
* SPIR-V assembly can now be used as input language
* Binary output can now be downloaded as C header file

## 2018-06-27

* New compiler - [MARK-V](https://github.com/KhronosGroup/SPIRV-Tools/blob/master/tools/comp/markv.cpp)

## 2018-06-26

* New compression backend - [miniz](https://github.com/richgel999/miniz)
* New compression backend - [LZMA](https://www.7-zip.org/sdk.html)

## 2018-06-25

* New compression backend - [ZStandard](http://zstd.net)

## 2018-06-24

* New compiler - [SMOL-V](https://github.com/aras-p/smol-v)

## 2018-06-20

* New compiler - [spirv-opt](https://github.com/KhronosGroup/SPIRV-Tools#optimizer-tool)
* Add AST output to XShaderCompiler
* New compiler - [HLSLParser](https://github.com/Thekla/hlslparser)
* New compiler - spirv-stats

## 2018-06-18

* New compiler - [GLSL optimizer](https://github.com/aras-p/glsl-optimizer)
* New compiler - [hlsl2glslfork](https://github.com/aras-p/hlsl2glslfork)

## 2018-06-15

* Add option to toggle between outputs for all steps in pipeline
* Add support for multiple versions of each compiler (only used for Slang at the moment)

## 2018-06-13

* New compiler - [Slang](https://github.com/shader-slang/slang)
* New compiler - [spirv-cfg](https://vulkan.lunarg.com/doc/view/1.0.39.1/windows/spirv_toolchain.html#user-content-spir-v-control-flow-visualization)

## 2018-06-11

* New compiler - [HLSLcc](https://github.com/Unity-Technologies/HLSLcc)
* New compiler - [SPIR-V to ISPC](https://github.com/GameTechDev/SPIRV-Cross)

## 2018-06-09

* New compiler - [XShaderCompiler](https://github.com/LukasBanana/XShaderCompiler)

## 2018-05-20

* Add `--auto-map-locations` parameter to `glslangValidator.exe` invocation
* Fix GLSL default code
* Add changelog :)
* Add "Download binary" button

## 2018-05-19

* Initial release