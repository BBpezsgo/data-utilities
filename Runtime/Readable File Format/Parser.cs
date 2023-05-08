﻿using System.IO;

namespace DataUtilities.ReadableFileFormat
{
    /// <summary>
    /// Can parse <see cref="string"/> to <see cref="Value"/>.
    /// To start converting, just call the <see cref="Parse"/> function.
    /// </summary>
    public class Parser : Text.TextDeserializer
    {
        Location CurrentLocation => new(CurrentCharacterIndex, CurrentColumn, CurrentLine);

        public static Value Parse(string data) => new Parser(data)._Parse();

        public Parser(string data) : base(data + ' ') { }

#pragma warning disable IDE1006 // Naming Styles
        Value _Parse()
#pragma warning restore IDE1006 // Naming Styles
        {
            ConsumeCharacters(WhitespaceCharacters);

            Value root = Value.Object();
            root.Location = CurrentLocation;

            bool inParentecieses = CurrentCharacter == '{';
            if (inParentecieses)
            {
                ConsumeNext();
                ConsumeCharacters(WhitespaceCharacters);
            }

            int endlessSafe = INFINITY;
            while (CurrentCharacter != '\0')
            {
                if (endlessSafe-- <= 0)
                { Debug.LogError($"Endless loop!"); break; }

                ConsumeCharacters(WhitespaceCharacters);
                string propertyName = ExpectPropertyName();
                ConsumeCharacters(WhitespaceCharacters);
                Value propertyValue = ExpectValue();
                root[propertyName] = propertyValue;
                ConsumeCharacters(WhitespaceCharacters);

                if (inParentecieses && CurrentCharacter == '}')
                {
                    ConsumeNext();
                    break;
                }
            }

            return root;
        }

        Value ExpectValue()
        {
            ConsumeCharacters(WhitespaceCharacters);
            var loc = CurrentLocation;
            if (CurrentCharacter == '{')
            {
                ConsumeNext();
                ConsumeCharacters(WhitespaceCharacters);
                Value objectValue = Value.Object();
                objectValue.Location = loc;
                int endlessSafe = INFINITY;
                while (CurrentCharacter != '}')
                {
                    if (endlessSafe-- <= 0)
                    { Debug.LogError($"Endless loop!"); break; }
                    ConsumeCharacters(WhitespaceCharacters);
                    string propertyName = ExpectPropertyName();
                    ConsumeCharacters(WhitespaceCharacters);
                    Value? propertyValue = ExpectValue();
                    if (propertyValue.HasValue)
                    { objectValue[propertyName] = propertyValue.Value; }
                    else
                    { Debug.LogError($"Property \"{propertyName}\" does not have a value"); }
                    ConsumeCharacters(WhitespaceCharacters);
                }
                ConsumeNext();
                return objectValue;
            }
            if (CurrentCharacter == '[')
            {
                ConsumeNext();
                ConsumeCharacters(WhitespaceCharacters);
                Value objectValue = Value.Object();
                objectValue.Location = loc;
                int endlessSafe = INFINITY;
                int index = 0;
                while (CurrentCharacter != ']')
                {
                    if (endlessSafe-- <= 0)
                    { Debug.LogError($"Endless loop!"); break; }
                    ConsumeCharacters(WhitespaceCharacters);
                    Value? listItemValue = ExpectValue();
                    if (listItemValue.HasValue)
                    { objectValue[index++] = listItemValue.Value; }
                    else
                    { Debug.LogError($"List has a null item"); }
                    ConsumeCharacters(WhitespaceCharacters);
                }
                ConsumeNext();
                objectValue["Length"] = Value.Literal(index.ToString());
                return objectValue;
            }

            if (CurrentCharacter == '"')
            {
                ConsumeNext();
                int endlessSafe = INFINITY;
                string literalValue = "";
                while (CurrentCharacter != '"')
                {
                    if (endlessSafe-- <= 0)
                    { Debug.LogError($"Endless loop!"); break; }
                    if (CurrentCharacter == '\\')
                    { ConsumeNext(); }
                    literalValue += ConsumeNext();
                }
                ConsumeNext();
                var result = Value.Literal(literalValue);
                result.Location = loc;
                return result;
            }

            {
                var anyValue = ConsumeUntil('{', '\r', '\n', ' ', '\t', '\0');
                var result = Value.Literal(anyValue);
                result.Location = loc;
                return result;
            }
        }

        string ExpectPropertyName()
        {
            ConsumeCharacters(WhitespaceCharacters);
            var result = ConsumeUntil(":");
            ConsumeNext();
            return result;
        }

        public static Value? LoadFile(string file) => !File.Exists(file) ? null : new Parser(File.ReadAllText(file))._Parse();
        public static bool TryLoadFile(string file, out Value result)
        {
            if (!File.Exists(file))
            {
                result = Value.Object();
                return false;
            }
            else
            {
                result = new Parser(File.ReadAllText(file))._Parse();
                return true;
            }
        }
    }

}