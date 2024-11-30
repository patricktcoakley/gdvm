# gdvm

A work-in-progress Godot version manager **(gdvm)**. [GVM](https://github.com/moovweb/gvm) was taken.

![sizzler](./assets/gdvm.gif)

## Features

gdvm is a friendly Godot version manager that lets users install multiple versions of Godot with ease. It uses a hybrid CLI/TUI design, 
meaning that in places when you don't supply an argument it falls back to a prompt to let you select what you're looking for,
as well as support for [passing it keywords](#usage) to help find the appropriate version based on your input, like `4 dev` or `latest`.
It's released as a static binary that can work on Windows, macOS, and Linux by just putting it somewhere and calling it in the
terminal, or more conveniently, by using a [package manager](#installation).

While gdvm is primarily targeting users that just want an easy way to maintain Godot installations or make it is to try out different versions,
another use case could be for things like remote installations, such as CI or server workloads, or even for WSL or containers. This could make running
automated tests or headless work much easier than having to rely on your OS package manager or having to rely on simple scripts since the only requirement
is to have the single binary, which clocks in around ~10MB.

At this point it is mostly feature-complete, so ongoing work will mostly focus on what's on the [roadmap](#roadmap).

## Installation

Aside from the package managers listed below, currently [zipped Windows and Linux binaries](https://github.com/patricktcoakley/gdvm/releases) are going to be uploaded after 
a release (see [macOS](#macos) for reasons why it is omitted.) Most people will probably want to use a package manager, 
so I've tried to make it easy by having both Scoop and Homebrew packages available. 

If there is interest in packaging for Linux distributions, please let me know and I can investigate it.

### Homebrew

Using [Homebrew](https://brew.sh), you simply need to add my [formulae](https://github.com/patricktcoakley/homebrew-forumlae) repo, like so:
```shell
brew tap patricktcoakley/formulae
brew install gdvm
```

### Scoop

Using [scoop](https://scoop.sh), you simply need to add my [bucket](https://github.com/patricktcoakley/scoop-bucket) repo, like so:

```shell
scoop bucket add patricktcoakley https://github.com/patricktcoakley/scoop-bucket
scoop install patricktcoakley/gdvm
```

## Usage

### Getting Started

gdvm downloads and installs Godot into a `~/gdvm/` for macOS and Linux, and `C:\Users\USERNAME\gdvm\` for Windows; this might be customizable in the future.
Each installation will be in a folder with the `VERSION-TYPE-RUNTIME`. So if you installed the 4.3 stable with .NET support, it would be in a folder marked 
`4.3-stable-mono`. By default, when you install a version a [symlink](https://en.wikipedia.org/wiki/Symbolic_link) is created in a folder called `bin`, which 
lets you launch that version directly from the terminal, or you can also just drag the symlink to your taskbar or dock (depending on your OS and desktop environment)
for easy launching through icons; for macOS you would specifically use `.app`. 

Right now it supports installing whatever your computer supports by CPU and OS, so if you're running Windows on a standard x86-standard CPU you are able to install 
and run versions of Godot all the way back to 1.x. macOS went through multiple architecture transitions since Godot 1 and so most modern Macs will only support releases
as far back as ~3.3, but if you have an older Mac you should still be able to install whatever it supports. An override to force downloads on unsupported systems
may be added later.

### Commands

![install](./assets/gdvm-install.jpg)

- `gdvm list`  will list locally installed Godot versions.
- `gdvm install [--query|-q <...strings>]` will prompt the user to install a version if no arguments are supplied, or will
  try to find the closest matching version based on the query, defaulting to "stable" if no other release type is supplied. 
  - Queries:
    - `latest` or `latest standard` will install the latest stable, and `latest mono` will install the latest .NET stable.        
    - `4 mono` will grab the latest stable 4.x .NET release, `3.3 rc` will grab the latest rc of 3.3 standard, `1` would take the last stable version `1`, and so on.
- `gdvm godot [--interactive|-i, args <string>]` run the set Godot version, or with the `--interactive` or `-i` flag, will prompt the user to launch an installed version.
  - Optionally, pass some arguments in, such as `--headless`.
- `gdvm set` prompts the user to set an installed version of Godot.
- `gdvm which` displays the location that the current Godot symlink points to.
- `gdvm remove [<string>]` prompts the user to select multiple installations to delete, or optionally takes a query to filter down to specific versions
  - For example, if you wanted to list all of the `4.y.z` versions to remove, you could jut do `gdvm remove 4` to list all of the 4 major releases.
- `gdvm logs [--level|-l <string>, --message|-m <string>`] displays all the of the logs, or optionally takes a level or message filter.
- `gdvm search [--query|-q <...strings>]` takes an optional query to search all available remote versions of Godot.
  - Queries:
    - `4` would filter all 4.x releases, including "stable", "dev", etc. 
    - `4.2-rc` would only list the `4.2` `rc` releases, but `4.2 rc` would list all `4.2.x` releases with the `rc` release type, including `4.2.2.-rc3`

## Notes

### Known Issues

- `--version` on [ConsoleAppFramework](https://github.com/Cysharp/ConsoleAppFramework) seems to expect some kind of root command,
  but `gdvm`, like many other version managers, is not meant to really have one. Passing in a command with `--version` displays the actual version. 
  This is a bug and will likely be fixed in the future, but for now it's just something to be aware of.

### Windows

In order to use the symlink feature for Windows, you first need to enable [Developer Mode](https://learn.microsoft.com/en-us/windows/apps/get-started/enable-your-device-for-development). 
Without it, you can still install, remove, etc, but you won't have the added benefit of having a symlink pointing to your desired version.

### macOS

I originally planned to supply .zip files for macOS too, and while I was able to automate code signing and notarization using my Apple Developer account,
it doesn't really make sense to go through the process when Homebrew exists since there are too many issues with Gatekeeper to justify it right now, 
and there are many options to build from source already. 

## Build

In order to build this project, you just need the .NET 9 SDK. Running `dotnet run -- <command> [args]` will let you run
commands immediately, but you can also run `dotnet build -c Release` to get a release build and just copy to a
directory in your PATH.

You might see some warnings for trimming with AOT, but in my testing I have not encountered any issues since I don't rely
on the functionality it is warning against.

## Roadmap
- Re-work the rest of the file management code to make it more testable by having overrides, which would also allow for custom install paths.
- Get some working e2e tests, including searching, installing, setting, launching, and removing a specific version.
- Possibly consider adding multi-select and multi-query to installations so that you could bulk-install multiple versions.
- Get passing args to `godot` working 100%; it seems like [ConsoleAppFramework](https://github.com/Cysharp/ConsoleAppFramework) 
  eats any args using `--` prefixes that are "magic"; for example, `--version` gets parsed and applied to the app itself and isn't passed to Godot.
  - This doesn't affect running the symlink itself with args, just the `godot` command, so it's not a huge deal, but it would be nice to have it working.

## FAQ

- **Why another version manager?**
  - I mostly created this as an experiment to play with how a command-line tool experience could be improved, and my
    goal is to make it easy to use, even for people who don't have a lot of terminal experience. I plan to mostly use it
    as a place to try out new ideas or add unique features. Also, I wanted to provide AOT-compiled static binaries on
    package managers for ease-of-use and installation purposes.
- **Why C# and not a script, or Rust, Go, etc?**
  - I have written command-line tools in various languages, including Bash, Python, Powershell, Go, and Rust, 
    but I wanted to explore modern .NET tooling and infrastructure, including NativeAOT static binaries and CLI/TUI libraries. 
    There is always a possibility of re-writing it in Rust in the future, but I plan to stick with C# for now, and I am 
    interested in seeing how competitive it can be in this space with all of the latest additions in .NET8+. 
