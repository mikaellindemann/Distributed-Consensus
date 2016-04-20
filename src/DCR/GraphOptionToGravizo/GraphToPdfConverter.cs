using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HistoryConsensus;
using Microsoft.FSharp.Core;
using Newtonsoft.Json;
using Action = HistoryConsensus.Action;

namespace GraphOptionToGravizo
{
    public class GraphToPdfConverter
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

            var converter = new GraphToPdfConverter();
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

            if (ConvertGraphToPdf(graph, $"{dotFileName}.pdf"))
            {
                var pdfProcess = Process.Start($"{dotFileName}.pdf");
                if (pdfProcess != null)
                {
                    pdfProcess.Exited += async (sender, args) =>
                    {
                        await Task.Delay(2000);
                        try
                        {
                            File.Delete($"{dotFileName}.pdf");
                        }
                        catch (Exception)
                        {
                            // Just ignore, and fill the harddrive of the user.
                        }
                    };
                }
            }
        }

        private void ConvertAndShow(string jsonFile, string pdfFile)
        {
            var graph = LoadGraph(jsonFile);

            ConvertAndShow(graph, pdfFile);
        }

        private void ConvertAndShow(Graph.Graph graph, string pdfFile)
        {
            if (!pdfFile.EndsWith(".pdf"))
            {
                Console.WriteLine("Appended '.pdf' to the end of FileToWrite.");
                pdfFile = pdfFile + ".pdf";
            }

            if (ConvertGraphToPdf(graph, pdfFile))
            {
                Process.Start(pdfFile);
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
            var gravizoGraph = map.Select(kvPair => kvPair.Value)
                .Select(
                    action =>
                        new
                        {
                            ActionString = $"{action.Id.Item1 + action.Id.Item2}[label=\"{action.Id.Item1}, {ActionToString(action.Type)}];",
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

        public bool ConvertGraphToPdf(Graph.Graph graph, string pdffile)
        {
            var dotStartInfo = new ProcessStartInfo("dot", "-Kdot -Tpdf")
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            var dot = Process.Start(dotStartInfo);

            if (dot == null) return false;

            using (var writer = dot.StandardInput)
            {
                WriteGraphToDotFile(graph, writer);
            }
            using (var writer = new StreamWriter(File.OpenWrite(@"C:\Users\mikae\Desktop\graph.gv")))
            {
                WriteGraphToDotFile(graph, writer);
            }
            using (var reader = dot.StandardOutput.BaseStream)
            {
                using (var writer = File.OpenWrite(pdffile))
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

        private static string ActionToString(Action.ActionType type)
        {
            if (type.IsCheckedCondition) return "CheckedCondition\"";
            if (type.IsChecksCondition) return "ChecksCondition\"";
            if (type.IsExcludedBy) return "ExcludedBy\"";
            if (type.IsExcludes) return "Excludes\"";
            if (type.IsExecuteFinish) return "ExecuteFinish\",style=filled,fillcolor=green";
            if (type.IsExecuteStart) return "ExecuteStart\",style=filled,fillcolor=red";
            if (type.IsIncludedBy) return "IncludedBy\"";
            if (type.IsIncludes) return "Includes\"";
            if (type.IsSetPendingBy) return "SetPendingBy\"";
            if (type.IsSetsPending) return "SetsPending\"";

            throw new ArgumentException("Unknown type", nameof(type));
        }
    }
}
