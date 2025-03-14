﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

using CommandLine;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.CodeAnalysis.Sarif.Driver;

namespace Microsoft.CodeAnalysis.IL
{
    internal static class BinSkim
    {
        private static int Main(string[] args)
        {
            args = ExpandArguments.GenerateArguments(args, new FileSystem(), new EnvironmentVariables());
            args = RewriteArgs(args);

            var rewrittenArgs = new List<string>(args);

            bool richResultCode = rewrittenArgs.RemoveAll(arg => arg.Equals("--rich-return-code")) == 0;

            using var telemetry = new Sdk.Telemetry();
            telemetry.LogCommandLine(args);

            return Parser.Default.ParseArguments<
                AnalyzeOptions,
                ExportRulesMetadataOptions,
                ExportConfigurationOptions,
                DumpOptions>(args)
              .MapResult(
                (AnalyzeOptions analyzeOptions) => new MultithreadedAnalyzeCommand(telemetry).Run(analyzeOptions),
                (ExportRulesMetadataOptions exportRulesMetadataOptions) => new ExportRulesMetadataCommand().Run(exportRulesMetadataOptions),
                (ExportConfigurationOptions exportConfigurationOptions) => new ExportConfigurationCommand().Run(exportConfigurationOptions),
                (DumpOptions dumpOptions) => new DumpCommand().Run(dumpOptions),
                _ => HandleParseError(args, richResultCode));
        }

        private static int HandleParseError(string[] args, bool richResultCode)
        {
            string[] validArgs = new[] { "help", "version", "--version", "--help" };
            return args.Any(arg => validArgs.Contains(arg, StringComparer.OrdinalIgnoreCase))
                ? richResultCode ? (int)RuntimeConditions.None : 0
                : richResultCode ? (int)RuntimeConditions.InvalidCommandLineOption : 1;
        }

        private static string[] RewriteArgs(string[] args)
        {
            var rewritten = new List<string>();
            for (int i = 0; i < args.Length; i++)
            {
                rewritten.Add(args[i]);
                string next =
                    i + 1 < args.Length
                        ? args[i + 1]
                        : null;

                switch (args[i])
                {
                    // AnalyzeOptionsBase
                    case "-q":
                    case "--quiet":
                    case "-r":
                    case "--recurse":
                    case "-e":
                    case "--environment":
                    case "--rich-return-code":

                    // AnalyzeOptions
                    case "--ignorePdbLoadError":
                    {
                        if (!EvaluatesToTrueOrFalse(next))
                        {
                            rewritten.Add("True");
                        }

                        break;
                    }

                    default:
                    {
                        break;
                    }
                }
            }

            return rewritten.ToArray();
        }

        private static bool EvaluatesToTrueOrFalse(string value)
        {
            return value == "True" || value == "False" ||
                   value == "true" || value == "false" ||
                   value == "1" || value == "0";
        }
    }
}
