using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HistoryConsensus;
using Microsoft.FSharp.Core;
using Newtonsoft.Json;
using Action = HistoryConsensus.Action;

namespace GraphOptionToSvg
{
    public class GraphToSvgConverter
    {
        public static void Main(string[] args)
        {
            if (args.Length < 1 || args.Length > 2)
            {
                Console.WriteLine("Usage: GraphOptionToGravizo FileToRead [FileToWrite]");
                Console.WriteLine("\tIf no FileToWrite is supplied, temporary files will be created and deleted.");
                Console.WriteLine("\tIf FileToWrite is specified, the files will be created and stored on the disk.");
                return;
            }

            if (!File.Exists(args[0]))
            {
                Console.WriteLine("The specified JSON-file does not exist!");
                return;
            }

            var converter = new GraphToSvgConverter();
            if (args.Length == 1)
            {
                converter.ConvertAndShow(args[0]);
            }
            else // args.Length == 2
            {
                converter.ConvertAndShow(args[0], args[1]);
            }
        }

        public void ConvertAndShow(string jsonFile)
        {
            var graph = LoadGraph(jsonFile);

            ConvertAndShow(graph);
        }

        public void ConvertAndShow(Graph.Graph graph)
        {
            var dotFileName = Path.GetTempFileName();
            File.Delete(dotFileName);

            if (ConvertGraphToSvg(graph, $"{dotFileName}.svg"))
            {
                var pdfProcess = Process.Start($"{dotFileName}.svg");
                if (pdfProcess != null)
                {
                    pdfProcess.Exited += async (sender, args) =>
                    {
                        await Task.Delay(2000);
                        try
                        {
                            File.Delete($"{dotFileName}.svg");
                        }
                        catch (Exception)
                        {
                            // Just ignore, and fill the harddrive of the user.
                        }
                    };
                }
            }
        }

        private void ConvertAndShow(string jsonFile, string svgfile)
        {
            var graph = LoadGraph(jsonFile);

            ConvertAndShow(graph, svgfile);
        }

        private void ConvertAndShow(Graph.Graph graph, string svgfile)
        {
            if (!svgfile.EndsWith(".svg"))
            {
                Console.WriteLine("Appended '.svg' to the end of FileToWrite.");
                svgfile = svgfile + ".svg";
            }

            if (ConvertGraphToSvg(graph, svgfile))
            {
                Process.Start(svgfile);
            }
        }

        private Graph.Graph LoadGraph(string file)
        {
            TypeDescriptor.AddAttributes(
                typeof(Tuple<string, int>),
                new TypeConverterAttribute(typeof(TupleConverter)));

            var option = JsonConvert.DeserializeObject<FSharpOption<Graph.Graph>>(File.ReadAllText(file));

            return FSharpOption<Graph.Graph>.get_IsSome(option) ? option.Value : null;
        }

        private void WriteGraphToDotFile(Graph.Graph graph, TextWriter writer)
        {
            var map = graph.Nodes;
            var isExecution = map.All(pair => pair.Value.Type.IsExecuteFinish) ? "shape=box," : "";
            var gravizoGraph = map.Select(kvPair => kvPair.Value)
                .Select(
                    action =>
                        new
                        {
                            ActionString = $"{action.Id.Item1 + action.Id.Item2}[{isExecution}label=\"{action.Id.Item1}, {ActionToString(action.Type)}, {FailureTypeToDotStyle(action.FailureTypes)}];",
                            EdgesStrings = action.Edges.Select(edge => $"{action.Id.Item1 + action.Id.Item2}->{edge.Item1 + edge.Item2};")
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

        public bool ConvertGraphToDot(Graph.Graph graph, string dotFile)
        {
            using (var writer = new StreamWriter(File.OpenWrite(dotFile)))
            {
                WriteGraphToDotFile(graph, writer);
            }
            return true;
        }

        public bool ConvertGraphToSvg(Graph.Graph graph, string svgfile)
        {
            var dotStartInfo = new ProcessStartInfo("dot", "-Kdot -Tsvg")
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true
            };

            var dot = Process.Start(dotStartInfo);

            if (dot == null) return false;

            using (var writer = dot.StandardInput)
            {
                WriteGraphToDotFile(graph, writer);
            }
            using (var reader = dot.StandardOutput.BaseStream)
            {
                using (var writer = File.OpenWrite(svgfile))
                {
                    var buffer = new byte[1024];
                    int bytesRead;
                    while ((bytesRead = reader.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        writer.Write(buffer, 0, bytesRead);
                    }
                }
            }

            dot.WaitForExit();
            return true;
        }

        private static string FailureTypeToDotStyle(IEnumerable<FailureTypes.FailureType> types)
        {
            // Todo: Do something sneaky with multiple types
            var type = types.FirstOrDefault();
            if (type == null) return "";

            if (type.IsCounterpartTimestampOutOfOrder) return "style=filled,fillcolor=red";
            if (type.IsExecutedWithoutProperState) return "style=filled,fillcolor=purple";
            if (type.IsFakeRelationsIn) return "style=filled,fillcolor=red";
            if (type.IsFakeRelationsOut) return "style=filled,fillcolor=red";
            if (type.IsHistoryAboutOthers) return "style=filled,fillcolor=red";
            if (type.IsIncomingChangesWhileExecuting) return "style=filled,fillcolor=red";
            if (type.IsLocalTimestampOutOfOrder) return "style=filled,fillcolor=red";
            if (type.IsMalicious) return "style=filled,fillcolor=red";
            if (type.IsMaybe) return "style=filled,fillcolor=darkgoldenrod";
            if (type.IsPartOfCycle) return "style=filled,fillcolor=darkorange4";
            if (type.IsPartialOutgoingWhenExecuting) return "style=filled,fillcolor=red";

            throw new ArgumentException("Unknown type", nameof(type));
        }

        private static string ActionToString(Action.ActionType type)
        {
            if (type.IsCheckedConditionBy) return "CheckedConditionBy\"";
            if (type.IsChecksCondition) return "ChecksCondition\"";
            if (type.IsCheckedMilestoneBy) return "CheckedMilestoneBy\"";
            if (type.IsChecksMilestone) return "ChecksMilestone\"";
            if (type.IsExcludedBy) return "ExcludedBy\"";
            if (type.IsExcludes) return "Excludes\"";
            if (type.IsExecuteFinish) return "ExecuteFinish\"";
            if (type.IsExecuteStart) return "ExecuteStart\"";
            if (type.IsIncludedBy) return "IncludedBy\"";
            if (type.IsIncludes) return "Includes\"";
            if (type.IsSetPendingBy) return "SetPendingBy\"";
            if (type.IsSetsPending) return "SetsPending\"";

            throw new ArgumentException("Unknown type", nameof(type));
        }
    }
}
