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

            using var module  = new PEFile(file.FullName, peReader);
            var metadata = module.Reader.GetMetadataReader(MetadataReaderOptions.None);
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
                                        var span = text.AsSpan();
                                        if (IsTranslationString(in span))
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

        public static void Main(string gameFolder, string output)
        {
            var directoryInfo = new DirectoryInfo(gameFolder);

            if (!directoryInfo.Exists)
            {
                Console.WriteLine("A non existing folder was provided!");
                return;
            }

            var folders = directoryInfo.GetDirectories();
            if (folders.All(di => di.Name != "bin"))
            {
                Console.WriteLine("The directory provided does not have 'bin' folder!");
                return;
            }
            if (folders.All(di => di.Name != "Modules"))
            {
                Console.WriteLine("The directory provided does not have 'Modules' folder!");
                return;
            }

            var mainFolder = directoryInfo.GetDirectories("bin")
                .FirstOrDefault()?
                .GetDirectories("Win64_Shipping_Client")
                .FirstOrDefault();
            if (mainFolder is null || !mainFolder.Exists)
            {
                Console.WriteLine("The directory provided does not have 'bin/Win64_Shipping_Client' folder!");
                return;
            }
            foreach (var fileInfo in mainFolder.GetFiles("TaleWorlds.*.dll"))
            {
                ParseLibrary(fileInfo);
            }

            var modulesFolder = directoryInfo.GetDirectories("Modules").First();
            ParseModule(modulesFolder, "Native");
            ParseModule(modulesFolder, "SandBox");
            ParseModule(modulesFolder, "SandBoxCore");
            ParseModule(modulesFolder, "StoryMode");
            ParseModule(modulesFolder, "CustomBattle");


            using var writer = new StreamWriter(output);
            using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ";"
            });
            csv.WriteRecords(LocalizationStrings);
        }
    }
}
