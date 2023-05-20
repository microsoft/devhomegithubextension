# Coding Style

## Philosophy

1. If it's inserting something into the existing classes/functions, try to follow the existing style as closely as possible.
1. If it's brand new code or refactoring a complete class or area of the code, please follow as Modern C# of a style as you can and reference the [.NET Engineering Guidelines](https://github.com/dotnet/aspnetcore/wiki/Engineering-guidelines) as much as you possibly can.

## Formatting

- We use [`.clang-format`](/.clang-format) style file to enable automatic code formatting. You can [easily format source files from Visual Studio](https://devblogs.microsoft.com/cppblog/clangformat-support-in-visual-studio-2017-15-7-preview-1/). For example, `CTRL+K CTRL+D` formats the current document.
- If you prefer another text editor or have ClangFormat disabled in Visual Studio, you could invoke [`format_sources`](/codeAnalysis/format_sources.ps1) powershell script from command line. It gets a list of all currently modified files from `git` and invokes clang-format on them.
  Please note that you should also have `clang-format.exe` in `%PATH%` for it to work. The script can infer the path of `clang-format.exe` version which is shipped with Visual Studio at `%VCINSTALLDIR%\Tools\Llvm\bin\`, if you launch it from the _Native Tools Command Prompt for VS_.
- CI doesn't enforce code formatting yet, since we're gradually applying code formatting to the codebase, but please adhere to our formatting style for any new code.
