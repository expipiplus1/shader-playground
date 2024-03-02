#addin nuget:?package=SharpZipLib&version=1.0.0
#addin nuget:?package=Cake.Compression&version=0.2.1
#addin nuget:?package=Cake.Git&version=1.0.1

var alwaysCache = Argument<bool>("always-cache", false);
var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

var windowsSdkVersion = "10.0.19041.0";

void RunAndCheckResult(FilePath exe, ProcessSettings settings)
{
    var res = StartProcess(exe, settings);
    if (res != 0)
    {
      throw new Exception($"'{exe}' failed with error code {res}");
    }
}

string DownloadCompiler(string url, string binariesFolderName, string version, bool cache)
{
  var tempFileName = $"./build/{binariesFolderName}-{version}.zip";

  if (!(cache || alwaysCache) || !FileExists(tempFileName)) {
    DownloadFile(url, tempFileName);
  }

  return tempFileName;
}

enum ZipFormat
{
  Zip,
  GZip,
  SevenZip
}

string GetFullPath(string fileName)
{
    if (FileExists(fileName))
    {
        return fileName;
    }

    var values = Environment.GetEnvironmentVariable("PATH");
    foreach (var path in values.Split(System.IO.Path.PathSeparator))
    {
        var dir = DirectoryPath.FromString(path);
        var fullPath = dir.GetFilePath(fileName).ToString();
        if (FileExists(fullPath))
        {
            return fullPath;
        }
    }
    return null;
}

void Unzip(string zip, string dstFolder, string filesToCopy, ZipFormat format)
{
  switch (format)
  {
    case ZipFormat.Zip:
    case ZipFormat.GZip:
    {
      if (filesToCopy == null)
      {
        EnsureDirectoryExists(dstFolder);
        CleanDirectory(dstFolder);
        if (format == ZipFormat.Zip)
        {
          ZipUncompress(zip, dstFolder);
        }
        else
        {
          GZipUncompress(zip, dstFolder);
        }
      } else
      {
        string unzippedFolder = $"{zip}.unzipped";
        EnsureDirectoryExists(unzippedFolder);
        CleanDirectory(unzippedFolder);
        if (format == ZipFormat.Zip)
        {
          ZipUncompress(zip, unzippedFolder);
        }
        else
        {
          GZipUncompress(zip, unzippedFolder);
        }
        EnsureDirectoryExists(dstFolder);
        CleanDirectory(dstFolder);
        CopyFiles($"{unzippedFolder}/{filesToCopy}", dstFolder, true);
        DeleteDirectory(unzippedFolder, new DeleteDirectorySettings {
          Recursive = true,
          Force = true
        });
      }
      break;
    }
    case ZipFormat.SevenZip:
    {
      string exe = @"C:\Program Files\7-Zip\7z.exe";
      if (!FileExists(exe))
      {
        exe = GetFullPath("7z.exe");
      }
      if (!FileExists(exe))
      {
        throw new InvalidOperationException("Unable to find 7z.exe in Program Files or PATH");
      }
      RunAndCheckResult(exe,
        new ProcessSettings
        {
          Arguments = $@"e -o""{dstFolder}"" ""{zip}"" {filesToCopy}"
        }
      );
      break;
    }
    default:
      throw new InvalidOperationException();
  }
}

string DownloadAndUnzipCompiler(string url, string binariesFolderName, string version, bool cache, ZipFormat format = ZipFormat.Zip)
{
  var tempFileName = DownloadCompiler(url, binariesFolderName, version, cache);
  var unzippedFolder = $"./build/{binariesFolderName}/{version}";

  Unzip(tempFileName, unzippedFolder, null, format);

  return unzippedFolder;
}

string DownloadAndUnzipCompiler(string url, string binariesFolderName, string version, bool cache, string filesToCopy, ZipFormat format = ZipFormat.Zip)
{
  var tempFileName = DownloadCompiler(url, binariesFolderName, version, cache);
  var binariesFolder = $"./src/ShaderPlayground.Core/Binaries/{binariesFolderName}/{version}";

  Unzip(tempFileName, binariesFolder, filesToCopy, format);

  return binariesFolder;
}

MSBuildSettings CreateCppBuildSettings()
{
  return new MSBuildSettings()
    .UseToolVersion(MSBuildToolVersion.VS2019)
    .WithProperty("PlatformToolset", "v142")
    .WithProperty("WindowsTargetPlatformVersion", windowsSdkVersion)
    .SetConfiguration(configuration);
}

Task("Prepare-Build-Directory")
  .Does(() => {
    EnsureDirectoryExists("./build");

    EnsureDirectoryExists("./src/ShaderPlayground.Core/Binaries");
    CleanDirectory("./src/ShaderPlayground.Core/Binaries");
  });

Task("Download-Dxc")
  .Does(() => {
    DownloadAndUnzipCompiler(
      "https://ci.appveyor.com/api/projects/dnovillo/directxshadercompiler/artifacts/build%2FRelease%2Fdxc-artifacts.zip?branch=main&pr=false&job=image%3A%20Visual%20Studio%202022",
      "dxc",
      "trunk",
      false,
      "bin/*.*");
  });

Task("Download-Glslang")
  .Does(() => {
    DownloadAndUnzipCompiler(
      "https://github.com/KhronosGroup/glslang/releases/download/main-tot/glslang-master-windows-Release.zip",
      "glslang",
      "trunk",
      false,
      "bin/*.*");
  });

Task("Download-Mali-Offline-Compiler")
  .Does(() => {
    DownloadAndUnzipCompiler(
      "https://armkeil.blob.core.windows.net/developer/Files/downloads/opengl-es-open-cl-offline-compiler/6.2/Mali_Offline_Compiler_v6.2.0.7d271f_Windows_x64.zip",
      "mali",
      "6.2.0",
      true,
      "Mali_Offline_Compiler_v6.2.0/**/*.*");

    void DownloadMali(string mobileStudioVersion, string maliVersion, string url)
    {
      var armMobileStudioExePath = DownloadCompiler(
        url,
        "arm-mobile-studio",
        mobileStudioVersion,
        true);

      var mobileStudioFolder = $"./build/arm-mobile-studio/{mobileStudioVersion}";
      EnsureDirectoryExists(mobileStudioFolder);
      CleanDirectory(mobileStudioFolder);

      RunAndCheckResult(
        @"C:\Program Files\7-Zip\7z.exe",
        new ProcessSettings
        {
          Arguments = $@"x -o""{mobileStudioFolder}"" ""{armMobileStudioExePath}"" ""mali_offline_compiler/*"" ""mali_offline_compiler/*/*"""
        });

      var binariesFolder = $"./src/ShaderPlayground.Core/Binaries/mali/{maliVersion}";

      EnsureDirectoryExists(binariesFolder);
      CleanDirectory(binariesFolder);
      CopyFiles($"{mobileStudioFolder}/mali_offline_compiler/**/*.*", binariesFolder, true);
      DeleteDirectory(mobileStudioFolder, new DeleteDirectorySettings {
        Recursive = true,
        Force = true
      });
    }

    // Can't actually download in this script because the actual URLs require login.
    DownloadMali("2021.0", "7.3.0", "TODO");
    DownloadMali("2022.1", "7.6.0", "TODO");
  });

Task("Download-Metal-Developer-Tools")
  .Does(() => {
    // Doesn't actually work because this URL requires login.
    DownloadAndUnzipCompiler(
      "https://developer.apple.com/services-account/download?path=/WWDC_2021/Metal_For_Windows_2.0_beta/Metal_Developer_Tools2.0betaWindows.exe",
      "metal-developer-tools",
      "2.0-beta",
      true,
      "macos/bin/metal.exe",
      ZipFormat.SevenZip);
  });

Task("Download-SPIRV-Cross")
  .Does(() => {
    void DownloadSpirvCross(string version, string hash)
    {
      DownloadAndUnzipCompiler(
        $"https://github.com/KhronosGroup/SPIRV-Cross/releases/download/{version}/spirv-cross-vs2017-64bit-{hash}.tar.gz",
        "spirv-cross",
        version,
        true,
        "bin/spirv-cross.exe",
        ZipFormat.GZip);
    }

    DownloadSpirvCross("2019-06-21", "b4e0163749");
    DownloadSpirvCross("2020-01-16", "f9818f0804");
    DownloadSpirvCross("2020-09-17", "8891bd3512");
    DownloadSpirvCross("2021-01-15", "9acb9ec31f");

    var unzippedFolder = DownloadAndUnzipCompiler(
      "https://github.com/KhronosGroup/SPIRV-Cross/archive/master.zip",
      "spirv-cross",
      "trunk",
      false);

    var srcDirectory = $"{unzippedFolder}/SPIRV-Cross-master";
    RunAndCheckResult(
        @"cmake.exe",
        new ProcessSettings
        {
          Arguments = ".",
          WorkingDirectory = srcDirectory
        });

    MSBuild(srcDirectory + "/SPIRV-Cross.vcxproj", CreateCppBuildSettings());

    var binariesFolder = $"./src/ShaderPlayground.Core/Binaries/spirv-cross/trunk";
    EnsureDirectoryExists(binariesFolder);
    CleanDirectory(binariesFolder);

    CopyFiles(
      $"{srcDirectory}/{configuration}/SPIRV-Cross.exe",
      binariesFolder,
      true);
  });

Task("Download-SPIRV-Cross-ISPC")
  .Does(() => {
    var unzippedFolder = DownloadAndUnzipCompiler(
      "https://github.com/GameTechDev/SPIRV-Cross/archive/master-ispc.zip",
      "spirv-cross-ispc",
      "trunk",
      false);

    MSBuild(unzippedFolder + "/SPIRV-Cross-master-ispc/msvc/SPIRV-Cross.vcxproj", CreateCppBuildSettings());

    var binariesFolder = $"./src/ShaderPlayground.Core/Binaries/spirv-cross-ispc/trunk";
    EnsureDirectoryExists(binariesFolder);
    CleanDirectory(binariesFolder);

    CopyFiles(
      $"{unzippedFolder}/SPIRV-Cross-master-ispc/msvc/{configuration}/SPIRV-Cross.exe",
      binariesFolder,
      true);
  });

Task("Download-SPIRV-Tools")
  .Does(() => {
    // Hack to get the actual zip URL from the intermediate download page, which contains this HTML:
    // <meta http-equiv="refresh" content="0; url=https://storage.googleapis.com/spirv-tools/artifacts/prod/graphics_shader_compiler/spirv-tools/windows-msvc-2017-release/continuous/1336/20201217-105132/install.zip" />
    var downloadPageFileName = $"./build/spirv-tools-trunk.html";
    DownloadFile(
      "https://storage.googleapis.com/spirv-tools/badges/build_link_windows_vs2017_release.html",
      downloadPageFileName);
    var downloadPageHtml = System.IO.File.ReadAllText(downloadPageFileName);
    var downloadPageUrl = System.Text.RegularExpressions.Regex.Match(downloadPageHtml, "url=([\\s\\S]+)\"").Groups[1].Value;

    DownloadAndUnzipCompiler(
      downloadPageUrl,
      "spirv-tools",
      "trunk",
      false,
      "install/bin/*.*");
  });

Task("Download-SPIRV-Tools-Legacy")
  .Does(() => {
    DownloadAndUnzipCompiler(
      "https://github.com/KhronosGroup/SPIRV-Tools/releases/download/master-tot/SPIRV-Tools-master-windows-x64-Release.zip",
      "spirv-tools-legacy",
      "trunk",
      true,
      "bin/*.*");
  });

Task("Download-XShaderCompiler")
  .Does(() => {
    DownloadAndUnzipCompiler(
      "https://github.com/LukasBanana/XShaderCompiler/releases/download/v0.10-alpha/Xsc-v0.10-alpha.zip",
      "xshadercompiler",
      "v0.10-alpha",
      true,
      "Xsc-v0.10-alpha/bin/Win32/xsc.exe");
  });

Task("Download-Slang")
  .Does(() => {
    var cudaExePath = DownloadCompiler(
      "http://developer.download.nvidia.com/compute/cuda/11.0.2/local_installers/cuda_11.0.2_451.48_win10.exe",
      "cuda",
      "11.0.2",
      true);

    var cudaFolder = "./build/cuda/11.0.2";
    EnsureDirectoryExists(cudaFolder);
    CleanDirectory(cudaFolder);

    void ExtractFile(string fileName)
    {
      RunAndCheckResult(
        @"C:\Program Files\7-Zip\7z.exe",
        new ProcessSettings
        {
          Arguments = $@"e -o""{cudaFolder}"" ""{cudaExePath}"" cuda_nvrtc\nvrtc\bin\{fileName}"
        });
    }

    var nvrtcFiles = new[]
    {
      "nvrtc64_110_0.dll",
      "nvrtc-builtins64_110.dll"
    };

    foreach (var nvrtcFile in nvrtcFiles)
    {
      ExtractFile(nvrtcFile);
    }

    var nvrtcPaths = nvrtcFiles.Select(x => cudaFolder + "/" + x);

    void DownloadSlang(string version)
    {
      var binariesFolder = DownloadAndUnzipCompiler(
        $"https://github.com/shader-slang/slang/releases/download/v{version}/slang-{version}-win64.zip",
        "slang",
        $"v{version}",
        true,
        "bin/windows-x64/release/*.*");

      CopyFiles(nvrtcPaths, binariesFolder);

      CopyFiles($"./src/ShaderPlayground.Core/Binaries/dxc/trunk/dxcompiler.dll", binariesFolder);
    }

    DownloadSlang("0.10.24");
    DownloadSlang("0.10.25");
    DownloadSlang("0.10.26");
    DownloadSlang("0.11.18");
    DownloadSlang("0.13.10");
    DownloadSlang("0.18.0");
    DownloadSlang("0.18.25");
    DownloadSlang("0.24.20");
  });

Task("Download-HLSLParser")
  .Does(() => {
    var unzippedFolder = DownloadAndUnzipCompiler(
      "https://github.com/Thekla/hlslparser/archive/master.zip",
      "hlslparser",
      "trunk",
      false);

    MSBuild(unzippedFolder + "/hlslparser-master/hlslparser.vcxproj", CreateCppBuildSettings());

    var binariesFolder = $"./src/ShaderPlayground.Core/Binaries/hlslparser/trunk";
    EnsureDirectoryExists(binariesFolder);
    CleanDirectory(binariesFolder);

    CopyFiles(
      $"{unzippedFolder}/hlslparser-master/{configuration}/hlslparser.exe",
      binariesFolder,
      true);
  });

Task("Download-zstd")
  .Does(() => {
    void DownloadZstd(string version)
    {
      DownloadAndUnzipCompiler(
        $"https://github.com/facebook/zstd/releases/download/v{version}/zstd-v{version}-win64.zip",
        "zstd",
        $"v{version}",
        true,
        "zstd.exe");
    }

    DownloadZstd("1.3.4");
  });

Task("Download-LZMA")
  .Does(() => {
    void DownloadLzma(string version, string displayVersion)
    {
      DownloadAndUnzipCompiler(
        $"https://www.7-zip.org/a/lzma{version}.7z",
        "lzma",
        displayVersion,
        true,
        @"bin\lzma.exe",
        ZipFormat.SevenZip);
    }

    DownloadLzma("1805", "18.05");
  });

Task("Download-RGA")
  .Does(() => {
    var amdDriverExePath = DownloadCompiler(
      "https://drivers.amd.com/drivers/beta/win10-radeon-software-adrenalin-2020-edition-20.8.3-sep8.exe",
      "amd-driver",
      "19.9.2",
      true);
 
    var amdDriverFolder = "./build/amd-driver/22.1.2";
    EnsureDirectoryExists(amdDriverFolder);
    CleanDirectory(amdDriverFolder);
 
    void ExtractFile(string fileName)
    {
      RunAndCheckResult(
        @"C:\Program Files\7-Zip\7z.exe",
        new ProcessSettings
        {
          Arguments = $@"e -o""{amdDriverFolder}"" ""{amdDriverExePath}"" Packages\Drivers\Display\WT6A_INF\B346681\{fileName}",
        });
    }
 
    ExtractFile("atidxx64.dll");
    ExtractFile("amdvlk64.dll");
 
    var driverDllPaths = new[]
    {
      amdDriverFolder + "/atidxx64.dll",
      amdDriverFolder + "/amdvlk64.dll"
    };

    void DownloadRga(string version, string filesToCopy)
    {
      var binariesFolder = DownloadAndUnzipCompiler(
        $"https://github.com/GPUOpen-Tools/radeon_gpu_analyzer/releases/download/{version}/rga-windows-x64-{version}.zip",
        "rga",
        version,
        true,
        filesToCopy);

      CopyFiles(driverDllPaths, binariesFolder);

      CopyFiles("./lib/x64/d3dcompiler_47.dll", binariesFolder);
    }

    DownloadRga("2.0.1", "bin/**/*.*");
    DownloadRga("2.1", "**/*.*");
    DownloadRga("2.2", "**/*.*");
    DownloadRga("2.3", "**/*.*");
    DownloadRga("2.3.1", "**/*.*");
    DownloadRga("2.4", "**/*.*");
    DownloadRga("2.4.1", "**/*.*");
    DownloadRga("2.6", "**/*.*");
    DownloadRga("2.6.1", "**/*.*");
    DownloadRga("2.6.2", "**/*.*");
    DownloadRga("2.7.1", "**/*.*");
  });

Task("Download-IntelShaderAnalyzer")
  .Does(() => {
    DownloadAndUnzipCompiler(
      "https://github.com/GameTechDev/IntelShaderAnalyzer/releases/download/v1/IntelShaderAnalyzer_v1.zip",
      "intelshaderanalyzer",
      "v1",
      true,
      "IntelShaderAnalyzer/*.*");
  });

  Task("Copy-PowerVR")
  .Does(() => {
    void CopyVersion(string version)
    {
      var binariesFolder = $"./src/ShaderPlayground.Core/Binaries/powervr/{version}";
      EnsureDirectoryExists(binariesFolder);
      CleanDirectory(binariesFolder);

      CopyFiles($"./lib/PowerVR/{version}/*.*", binariesFolder);
    }

    CopyVersion("2018 R1");
  });

Task("Build-ANGLE")
  .Does(() => {
    RunAndCheckResult(MakeAbsolute(File("./external/angle/build.bat")), new ProcessSettings {
      WorkingDirectory = MakeAbsolute(Directory("./external/angle"))
    });

    var binariesFolder = $"./src/ShaderPlayground.Core/Binaries/angle/trunk";
    EnsureDirectoryExists(binariesFolder);
    CleanDirectory(binariesFolder);

    CopyFiles(
      "./external/angle/source/out/Release/angle_shader_translator.exe",
      binariesFolder,
      true);

    CopyFiles(
      "./external/angle/source/out/Release/libc++.dll",
      binariesFolder,
      true);
  });

Task("Build-Clspv")
  .Does(() => {
    RunAndCheckResult(MakeAbsolute(File("./external/clspv/build.bat")), new ProcessSettings {
      WorkingDirectory = MakeAbsolute(Directory("./external/clspv"))
    });

    var binariesFolder = $"./src/ShaderPlayground.Core/Binaries/clspv/trunk";
    EnsureDirectoryExists(binariesFolder);
    CleanDirectory(binariesFolder);

    CopyFiles(
      "./external/clspv/source/build/bin/Release/clspv.exe",
      binariesFolder,
      true);
  });

Task("Build-Tint")
  .Does(() => {
    RunAndCheckResult(MakeAbsolute(File("./external/tint/build.bat")), new ProcessSettings {
      WorkingDirectory = MakeAbsolute(Directory("./external/tint"))
    });

    var binariesFolder = $"./src/ShaderPlayground.Core/Binaries/tint/trunk";
    EnsureDirectoryExists(binariesFolder);
    CleanDirectory(binariesFolder);

    CopyFiles(
      "./external/tint/source/build/Release/tint.exe",
      binariesFolder,
      true);
  });

Task("Build-Rust-GPU")
  .Does(() => {
    // TODO: Actually build from source.

    var binariesFolder = $"./src/ShaderPlayground.Core/Binaries/rust-gpu/trunk";
    EnsureDirectoryExists(binariesFolder);
    CleanDirectory(binariesFolder);

    CopyFiles(
      "./build/rust-gpu/trunk/**/*.*",
      binariesFolder,
      true);
  });

Task("Build-Naga")
  .Does(() => {
    void BuildNagaLegacy()
    {
      const string repoPath = "./build/naga-legacy";
      if (DirectoryExists(repoPath))
      {
        DeleteDirectory(repoPath, new DeleteDirectorySettings { Recursive = true });
      }
      GitClone("https://github.com/gfx-rs/naga.git", repoPath);

      void BuildVersion(string commit, string displayName)
      {
        GitCheckout(repoPath, commit);

        RunAndCheckResult(
          "cargo",
          new ProcessSettings
          {
            Arguments = "build --release",
            WorkingDirectory = repoPath,
          });

        var binariesFolder = $"./src/ShaderPlayground.Core/Binaries/naga/{displayName}";
        EnsureDirectoryExists(binariesFolder);
        CleanDirectory(binariesFolder);

        CopyFiles(
          $"{repoPath}/target/release/naga.*",
          binariesFolder,
          false);
      }

      BuildVersion("8376bab5622f89ed9689cd0c3aedfd97c333c5bf", "v0.5.0");
      BuildVersion("3a2f7e611e4fe8ec50d9bb365916f22e7c30e46c", "v0.7.0");
    }

    void BuildNaga()
    {
      const string repoPath = "./build/naga";
      if (DirectoryExists(repoPath))
      {
        DeleteDirectory(repoPath, new DeleteDirectorySettings { Recursive = true });
      }
      GitClone("https://github.com/gfx-rs/wgpu.git", repoPath);

      void BuildVersion(string tag)
      {
        GitCheckout(repoPath, tag);

        RunAndCheckResult(
          "cargo",
          new ProcessSettings
          {
            Arguments = "build --release",
            WorkingDirectory = $"{repoPath}/naga-cli",
          });

        var binariesFolder = $"./src/ShaderPlayground.Core/Binaries/naga/{tag}";
        EnsureDirectoryExists(binariesFolder);
        CleanDirectory(binariesFolder);

        CopyFiles(
          $"{repoPath}/target/release/naga.*",
          binariesFolder,
          false);
      }

      BuildVersion("v0.19.3");
    }

    BuildNagaLegacy();
    BuildNaga();
  });

Task("Build-Fxc-Shim")
  .Does(() => {
    DotNetCorePublish("./shims/ShaderPlayground.Shims.Fxc/ShaderPlayground.Shims.Fxc.csproj", new DotNetCorePublishSettings
    {
      Configuration = configuration
    });

    var fxcVersion = System.Diagnostics.FileVersionInfo.GetVersionInfo("./lib/x64/d3dcompiler_47.dll").ProductVersion;
    var binariesFolder = $"./src/ShaderPlayground.Core/Binaries/fxc/{fxcVersion}";

    EnsureDirectoryExists(binariesFolder);
    CleanDirectory(binariesFolder);

    CopyFiles(
      $"./shims/ShaderPlayground.Shims.Fxc/bin/{configuration}/netcoreapp3.1/publish/**/*.*",
      binariesFolder,
      true);
  });

Task("Build-HLSLcc-Shim")
  .Does(() => {
    MSBuild("./shims/ShaderPlayground.Shims.HLSLcc/ShaderPlayground.Shims.HLSLcc.vcxproj", CreateCppBuildSettings());

    var binariesFolder = "./src/ShaderPlayground.Core/Binaries/hlslcc/trunk";

    EnsureDirectoryExists(binariesFolder);
    CleanDirectory(binariesFolder);

    CopyFiles(
      $"./shims/ShaderPlayground.Shims.HLSLcc/{configuration}/ShaderPlayground.Shims.HLSLcc.exe",
      binariesFolder,
      true);
  });

Task("Build-GLSL-Optimizer-Shim")
  .Does(() => {
    MSBuild("./shims/ShaderPlayground.Shims.GlslOptimizer/Source/projects/vs2010/glsl_optimizer_lib.vcxproj", CreateCppBuildSettings()
      .SetPlatformTarget(PlatformTarget.Win32));

    MSBuild("./shims/ShaderPlayground.Shims.GlslOptimizer/ShaderPlayground.Shims.GlslOptimizer.vcxproj", CreateCppBuildSettings());

    var binariesFolder = "./src/ShaderPlayground.Core/Binaries/glsl-optimizer/trunk";

    EnsureDirectoryExists(binariesFolder);
    CleanDirectory(binariesFolder);

    CopyFiles(
      $"./shims/ShaderPlayground.Shims.GlslOptimizer/{configuration}/ShaderPlayground.Shims.GlslOptimizer.exe",
      binariesFolder,
      true);
  });

Task("Build-HLSL2GLSL-Shim")
  .Does(() => {
    MSBuild("./shims/ShaderPlayground.Shims.Hlsl2Glsl/Source/hlslang.vcxproj", CreateCppBuildSettings()
      .SetPlatformTarget(PlatformTarget.Win32));

    MSBuild("./shims/ShaderPlayground.Shims.Hlsl2Glsl/ShaderPlayground.Shims.Hlsl2Glsl.vcxproj", CreateCppBuildSettings());

    var binariesFolder = "./src/ShaderPlayground.Core/Binaries/hlsl2glsl/trunk";

    EnsureDirectoryExists(binariesFolder);
    CleanDirectory(binariesFolder);

    CopyFiles(
      $"./shims/ShaderPlayground.Shims.Hlsl2Glsl/{configuration}/ShaderPlayground.Shims.Hlsl2Glsl.exe",
      binariesFolder,
      true);
  });

Task("Build-SMOL-V-Shim")
  .Does(() => {
    MSBuild("./shims/ShaderPlayground.Shims.Smolv/ShaderPlayground.Shims.Smolv.vcxproj", CreateCppBuildSettings());

    var binariesFolder = "./src/ShaderPlayground.Core/Binaries/smol-v/trunk";

    EnsureDirectoryExists(binariesFolder);
    CleanDirectory(binariesFolder);

    CopyFiles(
      $"./shims/ShaderPlayground.Shims.Smolv/{configuration}/ShaderPlayground.Shims.Smolv.exe",
      binariesFolder,
      true);
  });

Task("Build-Miniz-Shim")
  .Does(() => {
    MSBuild("./shims/ShaderPlayground.Shims.Miniz/ShaderPlayground.Shims.Miniz.vcxproj", CreateCppBuildSettings());

    var binariesFolder = "./src/ShaderPlayground.Core/Binaries/miniz/2.0.7";

    EnsureDirectoryExists(binariesFolder);
    CleanDirectory(binariesFolder);

    CopyFiles(
      $"./shims/ShaderPlayground.Shims.Miniz/{configuration}/ShaderPlayground.Shims.Miniz.exe",
      binariesFolder,
      true);
  });

Task("Build-YARI-V-Shim")
  .Does(() => {
    MSBuild("./shims/ShaderPlayground.Shims.Yariv/ShaderPlayground.Shims.Yariv.vcxproj", CreateCppBuildSettings());

    var binariesFolder = "./src/ShaderPlayground.Core/Binaries/yari-v/trunk";

    EnsureDirectoryExists(binariesFolder);
    CleanDirectory(binariesFolder);

    CopyFiles(
      $"./shims/ShaderPlayground.Shims.Yariv/{configuration}/ShaderPlayground.Shims.Yariv.exe",
      binariesFolder,
      true);
  });

Task("Build-Shims")
  .IsDependentOn("Build-Fxc-Shim")
  .IsDependentOn("Build-HLSLcc-Shim")
  .IsDependentOn("Build-GLSL-Optimizer-Shim")
  .IsDependentOn("Build-HLSL2GLSL-Shim")
  .IsDependentOn("Build-SMOL-V-Shim")
  .IsDependentOn("Build-Miniz-Shim")
  .IsDependentOn("Build-YARI-V-Shim");

Task("Build")
  .IsDependentOn("Build-Shims")
  .Does(() => {
    var outputFolder = "./build/site";
    EnsureDirectoryExists(outputFolder);
    CleanDirectory(outputFolder);

    DotNetCorePublish("./src/ShaderPlayground.Web/ShaderPlayground.Web.csproj", new DotNetCorePublishSettings
    {
      Configuration = configuration,
      OutputDirectory = outputFolder
    });

    ZipCompress(outputFolder, "./build/site.zip");
  });

Task("Test")
  .IsDependentOn("Build")
  .Does(() => {
    DotNetCoreTest("./src/ShaderPlayground.Core.Tests/ShaderPlayground.Core.Tests.csproj", new DotNetCoreTestSettings
    {
      Configuration = configuration
    });
  });

Task("Default")
  .IsDependentOn("Prepare-Build-Directory")
  .IsDependentOn("Build-ANGLE")
  .IsDependentOn("Build-Clspv")
  .IsDependentOn("Build-Tint")
  .IsDependentOn("Build-Rust-GPU")
  .IsDependentOn("Build-Naga")
  .IsDependentOn("Download-Dxc")
  .IsDependentOn("Download-Glslang")
  .IsDependentOn("Download-Mali-Offline-Compiler")
  .IsDependentOn("Download-Metal-Developer-Tools")
  .IsDependentOn("Download-SPIRV-Cross")
  .IsDependentOn("Download-SPIRV-Tools")
  .IsDependentOn("Download-SPIRV-Tools-Legacy")
  .IsDependentOn("Download-XShaderCompiler")
  .IsDependentOn("Download-SPIRV-Cross-ISPC")
  .IsDependentOn("Download-Slang")
  .IsDependentOn("Download-HLSLParser")
  .IsDependentOn("Download-zstd")
  .IsDependentOn("Download-LZMA")
  .IsDependentOn("Download-RGA")
  .IsDependentOn("Download-IntelShaderAnalyzer")
  .IsDependentOn("Copy-PowerVR")
  .IsDependentOn("Build")
  .IsDependentOn("Test");

RunTarget(target);
