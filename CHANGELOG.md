# Changelog

<a name="1.2.5"></a>
## [1.2.5](https://www.github.com//patricktcoakley/gdvm/releases/tag/v1.2.5) (2025-09-22)

### Bug Fixes

* **install:** remove release pre-filtering causing search/install inconsistency. ([a7e81a5](https://www.github.com//patricktcoakley/gdvm/commit/a7e81a5198539fbfc7538d631baf627209057987))

<a name="1.2.4"></a>
## [1.2.4](https://github.com/patricktcoakley/gdvm/releases/tag/v1.2.4) (2025-08-27)

### 🐛 Bug Fixes

* **install:** Installation command now properly sorts by release type and only dislays versions supported by local machine. ([0edf52a](https://github.com/patricktcoakley/gdvm/commit/0edf52a4b9afbb397c710387eb56058c4a79a89e))

<a name="1.2.3"></a>
## [1.2.3](https://github.com/patricktcoakley/gdvm/releases/tag/v1.2.3) (2025-08-24)

### 🐛 Bug Fixes

* Improve version sorting and argument validation. ([9c11bc3](https://github.com/patricktcoakley/gdvm/commit/9c11bc3e3733c7475d95461074179628a97588e3))

<a name="1.2.2"></a>
## [1.2.2](https://github.com/patricktcoakley/gdvm/releases/tag/v1.2.2) (2025-08-14)

### 🐛 Bug Fixes

* **release:** Fix filtering incorrectly sorting alphabetically instead of by release type ordering. ([8069f7d](https://github.com/patricktcoakley/gdvm/commit/8069f7d48546e0d5f4ceea4db0a79933527b0d41))

<a name="1.2.1"></a>
## [1.2.1](https://www.github.com/patricktcoakley/gdvm/releases/tag/v1.2.1) (2025-07-24)

### 🐛 Bug Fixes

* **which:** Fixed `which` not displaying the currently set versions after last refactor; consolidated error messaging; minor refactoring. ([bd4ae28](https://www.github.com/patricktcoakley/gdvm/commit/bd4ae282bbe02e41fee70bc01b38bdcbb859fde2))

<a name="1.2.0"></a>
## [1.2.0](https://github.com/patricktcoakley/gdvm/releases/tag/v1.2.0) (2025-07-24)

### ✨ Features

* Update the installation status to include download information and updated design; refactor out remaining CLI dependencies from the core. ([11e5849](https://github.com/patricktcoakley/gdvm/commit/11e5849665c8d896cd3d313adab9a83cb512ac0a))

<a name="1.1.2"></a>
## [1.1.2](https://github.com/patricktcoakley/gdvm/releases/tag/v1.1.2) (2025-07-21)

### ✨ Features

* Add GitHub token validation and switch to Versionize. ([9a791ac](https://github.com/patricktcoakley/gdvm/commit/9a791acedcfcb34f9c056910bda5d6fe3b28d165))
* **gdvm:** Add remaining features, refactor existing code, and update README to reflect first stable version: ([b03d245](https://github.com/patricktcoakley/gdvm/commit/b03d245001b8c15da0eb88837b9072e61f5f93b7))
* **local:** Make auto-install default behavior; refactoring to improve testability. ([604b75a](https://github.com/patricktcoakley/gdvm/commit/604b75a697ab078f5320ecd079dd68ca85f37ccc))

### 🐛 Bug Fixes

* Fix having setting the default when only one version is installed; separate CLI and library code; introduce a standardized Result type; remove dead code. ([96c063a](https://github.com/patricktcoakley/gdvm/commit/96c063a190b83a1ae1da9f6d5979443060bae8d1))
* resolve version resolution logic ([76693aa](https://github.com/patricktcoakley/gdvm/commit/76693aa112cb527271a3ad3a366cf69524fdf54c))

<a name="1.1.1"></a>
## [1.1.1](https://github.com/patricktcoakley/gdvm/releases/tag/v1.1.1) (2025-07-08)

### 🐛 Bug Fixes

* resolve version resolution logic ([76693aa](https://github.com/patricktcoakley/gdvm/commit/76693aa112cb527271a3ad3a366cf69524fdf54c))

<a name="1.1.0"></a>
## [1.1.0](https://github.com/patricktcoakley/gdvm/releases/tag/v1.1.0) (2025-07-08)

### ✨ Features

* **local:** Make auto-install default behavior; refactoring to improve testability. ([604b75a](https://github.com/patricktcoakley/gdvm/commit/604b75a697ab078f5320ecd079dd68ca85f37ccc))

<a name="1.0.0"></a>
## [1.0.0](https://github.com/patricktcoakley/gdvm/releases/tag/v1.0.0) (2025-06-17)

### ✨ Features

* **gdvm:** Add remaining features, refactor existing code, and update README to reflect first stable version: ([b03d245](https://github.com/patricktcoakley/gdvm/commit/b03d245001b8c15da0eb88837b9072e61f5f93b7))
  - Add project-aware features to the application, including version management and automatic project launching.
  - Add automatic project file detection and launching.
  - Add local version management with the 'local' command.
  - Implement a version resolution service for contextually detecting which version should be used for various scenarios.
  - Update README to reflect new changes.
  - Improve testability by expanding dependency injection, including the TUI portions of the application.
  - Extract existing functionality into services for better separation of concerns.
  - Add comprehensive test coverage for all new features.