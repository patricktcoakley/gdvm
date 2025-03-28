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

Currently, there are a few ways to install gdvm. Aside from the package managers listed below, [zipped Windows and Linux binaries](https://github.com/patricktcoakley/gdvm/releases) are going to be uploaded after
a release (see [macOS](#macos) for reasons why it is omitted). Most people will probably want to use a package manager,
so I've tried to make it easy by having both Scoop and Homebrew packages available. There is also [gdvmup](#gdvmup) for Windows users who don't want to use Scoop. This tool
 is meant to handle all installation and management of the gdvm tool itself, much like a package manager would.

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


### gdvmup

There is also an **experimental** tool called `gdvmup` that can manage your installations on **Windows** using a Powershell script. I've only done preliminary testing and am open to feedback, but be aware things there may be issues. To try it out, you can do the following:

```powershell
irm https://raw.githubusercontent.com/patricktcoakley/gdvm/main/installer.ps1 | iex
```

which will install the latest version and add gdvmup, gdvm, and the Godot alias directories to your PATH automatically. gdvmup
can handle installation, upgrade, and deletion of the gdvm tool, but it's a WIP and may change or be integrated into the main application in the future.

Usage:

- `install` [`--quiet`] [`--version VERSION`] [`--force`] installs gdvmup and gdvm, with the optional arguments for quiet output, a specific version, or forcing an installation.
- `uninstall` removes **everything**, including gdvm, gdvmup, and all Godot installations.
- `upgrade` just reinstalls everything and will likely be removed in the future unless I can think of a use case.

If there is interest I will also consider a macOS/Linux version of this tool.


### Linux

If there is interest in packaging for Linux distributions, please create an issue and I can investigate it.

## Configuration

Once you've installed gdvm, there should be a `gdvm.ini` file located inside of the root `gdvm` directory. Currently, the only supported
setting is to set a [GitHub token](https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/managing-your-personal-access-tokens#creating-a-personal-access-token-classic) to disable
rate limiting on queries and installations. In order to do so, you need to edit the `gdvm.ini` to look like the following:

```ini
[github]
token = "<MY_SUPER_SECRET_TOKEN>"
```

which allows you to use `gdvm` without the [60 requests per hour restriction](https://docs.github.com/en/rest/using-the-rest-api/rate-limits-for-the-rest-api?apiVersion=2022-11-28#primary-rate-limit-for-unauthenticated-users). There may be other use cases in the future, 
but otherwise all functionality exists inside the CLI itself.

## Usage

### Getting Started

gdvm downloads and installs Godot into folders inside of `~/gdvm/` for macOS and Linux, and `C:\Users\USERNAME\gdvm\` for Windows; this might be customizable in the future.
Each installation will be in a folder with the `VERSION-TYPE-RUNTIME`. So if you installed the 4.3 stable with .NET support, it would be in a folder marked
`4.3-stable-mono`. By default, when you install a version a [symlink](https://en.wikipedia.org/wiki/Symbolic_link) is created in a folder called `bin`. This is what the `gdvm godot` command is using by default,
or you can run `gdvm godot -i` to pick any another installation to launch, or you can simply use `gdvm set` to pick the version you want to launch by default.
This command was added to not have to rely on having your `PATH` variable set to use symlinks. You can also just drag the symlink to your taskbar or dock (depending on your OS and desktop environment)
for easy launching through icons; for macOS you would specifically use `.app`. However, if you'd like to be able to just run `godot` from the terminal directly, see [here](#path) for basic instructions

Right now it supports installing whatever your computer supports by CPU and OS, so if you're running Windows on a standard x86-standard CPU you are able to install
and run versions of Godot all the way back to 1.x. macOS went through multiple architecture transitions since Godot 1 and so most modern Macs will only support releases
as far back as ~3.3, but if you have an older Mac you should still be able to install whatever it supports. An override to force downloads on unsupported systems
may be added later.

### Commands

![install](./assets/gdvm-install.jpg)

- `gdvm list`  will list locally installed Godot versions.
- `gdvm install [--query|-q <...strings>]` will prompt the user to install a version if no arguments are supplied, or will
  try to find the closest matching version based on the query, defaulting to "stable" if no other release type is supplied. 
  It will also set the last installed version as the default.
  - Queries:
    - `latest` or `latest standard` will install the latest stable, and `latest mono` will install the latest .NET stable.
    - `4 mono` will grab the latest stable 4.x .NET release, `3.3 rc` will grab the latest rc of 3.3 standard, `1` would take the last stable version `1`, and so on.
- `gdvm godot [--interactive|-i, args <string>]` run the set Godot version, or with the `--interactive` or `-i` flag, will prompt the user to launch an installed version.
  - Optionally, pass some arguments in, such as `--headless`.
- `gdvm set [--query <...strings>]` prompts the user to set an installed version of Godot if no arguments are supplied, or will
  try to find the closest matching version based on the query, including release type (`stable`) and version (`4`, `4.4`), or an exact match (`4.4.1-stable-mono`).
- `gdvm which` displays the location that the current Godot symlink points to.
- `gdvm remove [<string>]` prompts the user to select multiple installations to delete, or optionally takes a query to filter down to specific versions
  - For example, if you wanted to list all of the `4.y.z` versions to remove, you could jut do `gdvm remove 4` to list all of the 4 major releases.
- `gdvm logs [--level|-l <string>, --message|-m <string>`] displays all the of the logs, or optionally takes a level or message filter.
- `gdvm search [--query|-q <...strings>]` takes an optional query to search all available remote versions of Godot.
  - Queries:
    - `4` would filter all 4.x releases, including "stable", "dev", etc.
    - `4.2-rc` would only list the `4.2` `rc` releases, but `4.2 rc` would list all `4.2.x` releases with the `rc` release type, including `4.2.2.-rc3`

## Notes

### Windows

In order to use the symlink feature for Windows, you first need to enable [Developer Mode](https://learn.microsoft.com/en-us/windows/apps/get-started/enable-your-device-for-development).
Without it, you can still install, remove, etc, but you won't have the added benefit of having a symlink pointing to your desired version.

### macOS

I originally planned to supply .zip files for macOS too, and while I was able to automate code signing and notarization using my Apple Developer account,
it doesn't really make sense to go through the process when Homebrew exists since there are too many issues with Gatekeeper to justify it right now,
and there are many options to build from source already.

### PATH

> **_NOTE:_** If you use [gdvmup](#gdvmup), [Scoop](#scoop), or [Homebrew](#homebrew), this is not an issue and only relates to using the zipped binaries.

There currently isn't a built-in way to add the binaries to your `PATH` right, but it's very straightforward to do if you aren't familiar. If you're on Windows, you can generally
just follow [this](https://learn.microsoft.com/en-us/previous-versions/office/developer/sharepoint-2010/ee537574(v=office.14)). Otherwise, for macOS and Linux, if you're using zsh or bash you should be able to just open your `~/.profile` and add `export PATH="$PATH:$HOME/gdvm/bin"` and then `source ~/.profile`.

## Build

In order to build this project, you just need the .NET 9 SDK. Running `dotnet run -- <command> [args]` will let you run commands immediately, but you can also run `dotnet build -c Release` to get a release build and just copy to a directory in your PATH.

You might see some warnings for trimming with AOT, but in my testing I have not encountered any issues since I don't rely on the functionality it is warning against.

## Roadmap

- Re-work the rest of the file management code to make it more testable by having overrides, which would also allow for custom install paths.
- Get some working e2e tests, including searching, installing, setting, launching, and removing a specific version.
- Possibly consider adding multi-select and multi-query to installations so that you could bulk-install multiple versions.
- I currently have [gdvmup](#gdvmup) for Windows, and it would make sense to port that script to bash for macOS and Linux support, allowing users to more easily install gdvm without having to rely on a package manager, but at the cost of extra maintenance and overhead.

## FAQ

- **Why another version manager?**
  - I mostly created this as an experiment to play with how a command-line tool experience could be improved, and my
    goal is to make it easy to use, even for people who don't have a lot of terminal experience. I plan to mostly use it
    as a place to try out new ideas or add unique features.
- **Why C# and not a script, or Rust, Go, etc?**
  - I have written command-line tools in various languages, including Bash, Python, Powershell, Go, TypeScript, and Rust, but I wanted to explore modern .NET tooling and infrastructure, including NativeAOT static binaries and CLI/TUI libraries. I think modern .NET provides a pretty great platform to create developer tooling.
