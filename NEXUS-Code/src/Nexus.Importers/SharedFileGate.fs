namespace Nexus.Importers

open System
open System.Globalization
open System.IO
open System.Text
open System.Threading

[<RequireQualifiedAccess>]
module SharedFileGate =
    type AcquireOptions =
        { MaxAttempts: int
          InitialDelayMs: int }

    type HeldLock =
        private
            { LockPath: string
              Stream: FileStream }

    let defaultAcquireOptions =
        { MaxAttempts = 40
          InitialDelayMs = 50 }

    let private ensureParentDirectory (lockPath: string) =
        let directory = Path.GetDirectoryName(lockPath)

        if not (String.IsNullOrWhiteSpace(directory)) then
            Directory.CreateDirectory(directory) |> ignore

    let private writeMetadata (stream: FileStream) =
        let acquiredAt = DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture)
        stream.SetLength(0L)
        stream.Position <- 0L

        use writer = new StreamWriter(stream, Encoding.UTF8, 1024, true)
        writer.WriteLine($"pid = {Environment.ProcessId}")
        writer.WriteLine($"acquired_at_utc = \"{acquiredAt}\"")
        writer.Flush()
        stream.Flush(true)

    let acquireWith options lockPath =
        let normalizedLockPath = Path.GetFullPath(lockPath)
        ensureParentDirectory normalizedLockPath

        let rec loop (attempt: int) (delayMs: int) =
            try
                let stream = new FileStream(normalizedLockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None)
                writeMetadata stream

                Ok
                    { LockPath = normalizedLockPath
                      Stream = stream }
            with
            | :? IOException as ex when attempt < options.MaxAttempts ->
                Thread.Sleep(delayMs)
                loop (attempt + 1) (min 1000 (delayMs * 2))
            | :? UnauthorizedAccessException as ex when attempt < options.MaxAttempts ->
                Thread.Sleep(delayMs)
                loop (attempt + 1) (min 1000 (delayMs * 2))
            | ex ->
                Error $"Could not acquire shared file gate at {normalizedLockPath}: {ex.Message}"

        loop 0 (max 1 options.InitialDelayMs)

    let acquire lockPath = acquireWith defaultAcquireOptions lockPath

    let release heldLock =
        heldLock.Stream.Dispose()
