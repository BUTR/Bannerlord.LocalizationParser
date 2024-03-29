﻿using CommandLine;

using CsvHelper;
using CsvHelper.Configuration;

using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.Metadata;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace Bannerlord.LocalizationParser
{
    public class Options
    {
        [Option('g', "gameFolder", Required = true, HelpText = "Set output to verbose messages.")]
        public string? GameFolder { get; set; }

        [Option('o', "output", Required = true, HelpText = "Set output to verbose messages.")]
        public string? Output { get; set; }
    }

    public static class Program
    {
        private record LocalizationString(string Assembly, string Text) { }

        private static readonly List<LocalizationString> LocalizationStrings = new();

        private static bool IsTranslationString(in ReadOnlySpan<char> span)
        {
            if (span.Length < 3)
                return false;

            if (span[0] != '{' || span[1] != '=')
                return false;

            if (span.IndexOf('}') == -1)
                return false;

            return true;
        }

        private static void ParseLibrary(FileInfo file)
        {
            var peReader = new PEReader(new FileStream(file.FullName, FileMode.Open, FileAccess.Read), PEStreamOptions.Default);
            if (!peReader.HasMetadata)
                return;

            Console.WriteLine("Parsing {0}", file.FullName);

            using var module = new PEFile(file.FullName, peReader);
            var metadata = module.Reader.GetMetadataReader(MetadataReaderOptions.None);
            foreach (var fieldDefinitionHandle in metadata.FieldDefinitions)
            {
                var fieldDefinition = metadata.GetFieldDefinition(fieldDefinitionHandle);
                if (!fieldDefinition.HasFlag(System.Reflection.FieldAttributes.Literal))
                    continue;
                var constantHandle = fieldDefinition.GetDefaultValue();
                if (constantHandle.IsNil)
                    continue;
                var constant = metadata.GetConstant(constantHandle);
                if (constant.TypeCode != ConstantTypeCode.String)
                    continue;
                var blob = metadata.GetBlobReader(constant.Value);
                var text = blob.ReadConstant(constant.TypeCode) as string;
                if (IsTranslationString(text))
                    LocalizationStrings.Add(new(file.Name, text));
            }
            foreach (var methodDefinitionHandle in metadata.MethodDefinitions)
            {
                var methodDefinition = metadata.GetMethodDefinition(methodDefinitionHandle);
                if (methodDefinition.HasBody())
                {
                    var body = module.Reader.GetMethodBody(methodDefinition.RelativeVirtualAddress);
                    var reader = body.GetILReader();
                    while (reader.RemainingBytes > 0)
                    {
                        var opCode = reader.DecodeOpCode();
                        if (opCode.IsDefined())
                        {
                            switch (opCode.GetOperandType())
                            {
                                case OperandType.String:
                                    var metadataToken = reader.ReadInt32();
                                    string? text = null;
                                    try
                                    {
                                        var userString = MetadataTokens.UserStringHandle(metadataToken);
                                        text = metadata.GetUserString(userString);
                                    }
                                    catch (BadImageFormatException)
                                    {
                                        text = null;
                                    }
                                    if (text != null)
                                    {
                                        if (IsTranslationString(text))
                                            LocalizationStrings.Add(new(file.Name, text));
                                    }
                                    break;
                            }
                        }
                    }
                }
            }
        }

        private static void ParseModule(DirectoryInfo modulesFolder, string module)
        {
            Console.WriteLine($"Checking module {module}...");

            var moduleFolder = modulesFolder.GetDirectories(module)
                .FirstOrDefault()?
                .GetDirectories("bin")
                .FirstOrDefault()?
                .GetDirectories("Win64_Shipping_Client")
                .FirstOrDefault();
            if (moduleFolder?.Exists == true)
            {
                foreach (var fileInfo in moduleFolder.GetFiles("*.dll"))
                {
                    ParseLibrary(fileInfo);
                }
            }
        }

        public static void Main(string[] args) => Parser.Default.ParseArguments<Options>(args)
            .WithNotParsed(o =>
            {
                Console.WriteLine("Received incorrect input. Using interactive mode!");
                Console.WriteLine("Please write the directory location path to look for strings!");
                var gameFolder = Console.ReadLine();
                Console.WriteLine("Please write the output directory path!");
                var output = Console.ReadLine();

                Execute(gameFolder, output);
            })
            .WithParsed(o =>
            {
                Execute(o.GameFolder, o.Output);
            });

        public static void Execute(string? gameFolder, string? output)
        {
            var directoryInfo = new DirectoryInfo(gameFolder);

            if (!directoryInfo.Exists)
            {
                Console.WriteLine("A non existing directory location for was provided!");
                return;
            }

            var folders = directoryInfo.GetDirectories();

            var noBinFolder = false;
            if (folders.All(di => di.Name != "bin"))
            {
                noBinFolder = true;
                Console.WriteLine("The directory provided does not have 'bin' directory!");
                //return;
            }
            else
            {
                Console.WriteLine("Checking 'bin/Win64_Shipping_Client'...");

                var mainFolder = directoryInfo.GetDirectories("bin")
                    .FirstOrDefault()?
                    .GetDirectories("Win64_Shipping_Client")
                    .FirstOrDefault();
                if (mainFolder is null || !mainFolder.Exists)
                {
                    Console.WriteLine("The directory provided does not have 'bin/Win64_Shipping_Client' folder!");
                    return;
                }
                foreach (var fileInfo in mainFolder.GetFiles("*.dll"))
                {
                    ParseLibrary(fileInfo);
                }
            }

            var noModules = false;
            if (folders.All(di => di.Name != "Modules"))
            {
                noModules = true;
                Console.WriteLine("The directory provided does not have 'Modules' directory!");
                //return;
            }
            else
            {
                Console.WriteLine("Checking modules...");

                var modulesFolder = directoryInfo.GetDirectories("Modules").First();
                ParseModule(modulesFolder, "Native");
                ParseModule(modulesFolder, "SandBox");
                ParseModule(modulesFolder, "SandBoxCore");
                ParseModule(modulesFolder, "StoryMode");
                ParseModule(modulesFolder, "CustomBattle");
            }

            if (noBinFolder && noModules)
            {
                Console.WriteLine("The provided directory is not a valid M&B:2 directory! Checking all available .dll's within the directory...");

                foreach (var fileInfo in directoryInfo.GetFiles("*.dll"))
                {
                    ParseLibrary(fileInfo);
                }
            }

            using var writer = new StreamWriter(output);
            using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ";"
            });
            csv.WriteRecords(LocalizationStrings);
        }
    }
}