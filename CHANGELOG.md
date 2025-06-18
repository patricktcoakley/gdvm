# Changelog

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
