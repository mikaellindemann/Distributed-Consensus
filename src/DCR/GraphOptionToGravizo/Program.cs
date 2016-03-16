using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using HistoryConsensus;
using Microsoft.FSharp.Core;
using Newtonsoft.Json;
using Action = HistoryConsensus.Action;

namespace GraphOptionToGravizo
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: GraphOptionToGravizo FileToRead [FileToWrite]");
                Console.WriteLine("\tIf no FileToWrite is supplied, temporary files will be created and deleted.");
                Console.WriteLine("\tIf FileToWrite is specified, the files will be created and stored on the disk.");
                return;
            }

            if (!File.Exists(args[0]))
            {
                Console.WriteLine("The specified in-file does not exist!");
                return;
            }
            TypeDescriptor.AddAttributes(
                typeof(Tuple<string, int>),
                new TypeConverterAttribute(typeof(TupleConverter)));

            var graph = JsonConvert.DeserializeObject<FSharpOption<Graph.Graph>>(File.ReadAllText(args[0])).Value;

            string randomFile = null;
            var dotFileName = args.Length >= 2 ? args[1] : randomFile = Path.GetTempFileName();

            using (TextWriter writer = new StreamWriter(File.OpenWrite(dotFileName)))
            {
                WriteGraphToDotFile(graph, writer);
            }

            try
            {
                var process = Process.Start("dot", $"-Kdot -Tpdf -O {dotFileName}");
                if (process != null) process.WaitForExit();
                else throw new Exception();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.WriteLine("If you had Graphviz installed in path the produced graph would have been displayed!");
                Console.Write("Press any key to exit...");
                Console.Read();
                return;
            }
            ShowPdf($"{dotFileName}.pdf", randomFile);
        }

        private static string ActionToString(Action.ActionType type)
        {
            if (type.IsCheckedConditon) return "CheckedCondition\"";
            if (type.IsChecksConditon) return "ChecksCondition\"";
            if (type.IsExcludedBy) return "ExcludedBy\"";
            if (type.IsExcludes) return "Excludes\"";
            if (type.IsExecuteFinish) return "ExecuteFinish\",style=filled,fillcolor=green";
            if (type.IsExecuteStart) return "ExecuteStart\",style=filled,fillcolor=red";
            if (type.IsIncludedBy) return "IncludedBy\"";
            if (type.IsIncludes) return "Includes\"";
            if (type.IsLockedBy) return "LockedBy\"";
            if (type.IsLocks) return "Locks\"";
            if (type.IsSetPendingBy) return "SetPendingBy\"";
            if (type.IsSetsPending) return "SetsPending\"";
            if (type.IsUnlockedBy) return "UnlockedBy\"";
            if (type.IsUnlocks) return "Unlocks\"";

            throw new ArgumentException("Unknown type", nameof(type));
        }

        private static void WriteGraphToDotFile(Graph.Graph graph, TextWriter writer)
        {
            var map = graph.Nodes;
            var gravizoGraph = map.Select(kvPair => kvPair.Value)
                .Select(
                    action =>
                        new
                        {
                            ActionString = $"{action.Id.Item2}[label=\"{action.Id.Item1}, {ActionToString(action.Type)}];",
                            EdgesStrings = action.Edges.Select(edge => $"{action.Id.Item2}->{edge.Item2};")
                        });

            writer.WriteLine("digraph G {");
            foreach (var element in gravizoGraph)
            {
                writer.Write($"\t{element.ActionString}");
                foreach (var edgesString in element.EdgesStrings)
                {
                    writer.Write($"{edgesString}");
                }
                writer.WriteLine();
            }
            writer.WriteLine("}");
        }

        private static void ShowPdf(string fileToShow, string fileToDelete)
        {
            var pdfProcess = Process.Start(fileToShow);
            if (fileToDelete != null && pdfProcess != null)
            {
                pdfProcess.WaitForExit();
                File.Delete(fileToDelete);
                File.Delete($"{fileToDelete}.pdf");
            }
        }
    }
}
