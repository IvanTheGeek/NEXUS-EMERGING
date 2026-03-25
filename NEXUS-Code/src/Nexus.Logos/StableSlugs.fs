namespace Nexus.Logos

open System

[<RequireQualifiedAccess>]
module internal StableSlugs =
    let private isAsciiLowercase (character: char) =
        character >= 'a' && character <= 'z'

    let private isAsciiDigit (character: char) =
        character >= '0' && character <= '9'

    let private isAllowedDelimiter (character: char) = character = '-'

    let private isAllowedCharacter (character: char) =
        isAsciiLowercase character || isAsciiDigit character || isAllowedDelimiter character

    let validate (name: string) (value: string) =
        let normalized = value.Trim()

        if String.IsNullOrWhiteSpace(normalized) then
            invalidArg "value" $"{name} cannot be blank."

        if normalized.StartsWith("-", StringComparison.Ordinal) then
            invalidArg "value" $"{name} cannot start with '-'."

        if normalized.EndsWith("-", StringComparison.Ordinal) then
            invalidArg "value" $"{name} cannot end with '-'."

        if normalized |> Seq.exists (fun character -> not (isAllowedCharacter character)) then
            invalidArg
                "value"
                $"{name} must use only lowercase ascii letters, digits, and '-'."

        normalized
