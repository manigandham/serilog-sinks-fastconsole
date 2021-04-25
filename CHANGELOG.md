# Changelog

Full readme: https://github.com/manigandham/serilog-sinks-fastconsole/blob/master/README.md

## 1.4.1
- Added `ConfigureAwait(false)` to avoid SynchronizationContext when used in .NET Framework.

## 1.4.0
- Updated target frameworks to `netstandard2.0`, `netstandard2.1`, `net5.0`.
- Added `restrictedToMinimumLevel` and `loggingLevelSwitch` (standard Serilog) options to sink config.
- Improved performance by using StringBuilder internal buffer directly (only `net5.0` and higher).
- Enabled nullable reference types for project and updated type definitions for extra safety.
- Updated nuget references.

## 1.3.2
- Added `QueueLimit` option added to bound the in-memory queue used for log entries. This is useful for adding back-pressure to avoid out-of-memory issues with high-volume logging.
- Updated nuget references.

## 1.3.1
- Added `netstandard2.0` to improve dependency graph in newer solutions. See guidance at https://docs.microsoft.com/en-us/dotnet/standard/library-guidance/cross-platform-targeting

## 1.3.0
- Fix bug with default JSON object writer ending quote.

## 1.2.0
- Config options to toggle JSON output with a single sink.
- Extension methods for Serilog log configuration.
- Allow custom delegate to replace JSON object writer.

## 1.0.0
- Initial console sinks for plaintext and JSON.