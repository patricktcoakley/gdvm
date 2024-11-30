# gdvm

A work-in-progress Godot version manager **(gdvm)**. [GVM](https://github.com/moovweb/gvm) was taken.

![sizzler](./assets/gdvm.gif)

## Features

gdvm is a Godot version manager that lets users install multiple versions of Godot with ease. It uses a hybrid TUI/CLI design, 
meaning that in places when you don't supply an argument, it falls back to a prompt to let you select what you're looking for. 
Features include the ability to install, manage, and run Godot installations from the terminal using queries or user promots. 
This is useful if you want to try out different versions or install the same version on different machines, and potentially 
for things like remote installations, such as CI or server workloads, or even for WSL or containers. 

At this point it is mostly feature-complete, so ongoing work will include re-writing stuff for testability and easier extension,
bug fixes, etc, and I am mostly looking forward for feedback to see what can be improved on the UX side.

## Installation

Aside from the package managers listed below, currently zipped Windows and Linux binaries are going to be uploaded after 
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

- `gdvm list`  will list locally installed Godot versions.
- `gdvm install [--query|-q <...strings>]` will prompt the user to install a version if no arguments are supplied, or will
  try to find the closest matching version based on the query, defaulting to "stable" if no other release type is supplied. 
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
  - For example, `4` will filter all 4.x releases, including "stable", "dev", etc. 
  - `4.2-rc` would only list the `4.2` `rc` releases, but `4.2 rc` would list all `4.2.x` releases with the `rc` release type, including `4.2.2.-rc3`

## Notes

### Known Issues

- `--version` on [ConsoleAppFramework](https://github.com/Cysharp/ConsoleAppFramework) seems to expect some kind of root command,
  but `gdvm`, like many other version managers, is not meant to really have one. Passing in a command with `--version` displays the action version,
  and for now I will likely just investigate if this is something that can be fixed.

### Windows

In order to use the [symlink](https://en.wikipedia.org/wiki/Symbolic_link) feature for Windows, you first need to
enable [Developer Mode](https://learn.microsoft.com/en-us/windows/apps/get-started/enable-your-device-for-development).
Without it, you can still install, remove, etc, but you won't have the added benefit of having a symlink pointing to your
desired version.

### macOS

I originally planned to supply .zip files for macOS too, and while I was able to automate code signing and notarization,
it doesn't really make sense to go through the process when Homebrew exists since there are too many issues with Gatekeeper to
justify it right now, and there are many options to build from source already.

## Build

In order to build this project, you just need the .NET 9 SDK. Running `dotnet run -- <command> [args]` will let you run
commands immediately, but you can also run `dotnet build -c Release` to get a release build and just copy to a
directory in your PATH.

You might see some warnings for trimming with AOT, but in my testing I have not encountered any issues since I don't rely
on the functionality it is warning against.

## Roadmap
- Re-work most of the file management code to be more testable and create more tests.
- Add a few e2e tests to make sure symlinking and file pathing doesn't break.
- Possibly consider adding multi-select and multi-query to installations so that you could bulk-install multiple versions.
- Get passing args to `godot` working; it seems like [ConsoleAppFramework](https://github.com/Cysharp/ConsoleAppFramework) 
  eats any args using `--` prefixes that are "magic"; `--version` gets parsed and applied to the app itself and not Godot.
  - This doesn't affect running the symlink itself with args, so it's not a huge deal, but it would be nice to have it working.

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
