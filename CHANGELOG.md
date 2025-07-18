# Changelog

<a name="1.1.1"></a>
## [1.1.1](https://github.com/patricktcoakley/gdvm/releases/tag/v1.1.1) (2025-07-08)

### 🐛 Bug Fixes

* resolve version resolution logic ([76693aa](https://github.com/patricktcoakley/gdvm/commit/76693aa112cb527271a3ad3a366cf69524fdf54c))

<a name="1.1.0"></a>
## [1.1.0](https://github.com/patricktcoakley/gdvm/releases/tag/v1.1.0) (2025-07-08)

### ✨ Features

* **local:** Make auto-install default behavior; refactoring to imporve testability. ([604b75a](https://github.com/patricktcoakley/gdvm/commit/604b75a697ab078f5320ecd079dd68ca85f37ccc))

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