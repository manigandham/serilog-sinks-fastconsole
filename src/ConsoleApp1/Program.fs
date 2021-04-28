open System
open Serilog
open Serilog.Events
open Serilog.Sinks.FastConsole

let logger =
         LoggerConfiguration()
             .MinimumLevel.Debug()
             .WriteTo.FastConsole()
             .WriteTo.File("test.log", shared = true, restrictedToMinimumLevel = LogEventLevel.Debug)
             .CreateLogger()

[<EntryPoint>]
let main argv =
    logger.Debug("test logging message")
    0 // return an integer exit code