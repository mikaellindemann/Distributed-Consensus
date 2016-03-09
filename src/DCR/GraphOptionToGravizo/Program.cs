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
        private static string ActionToString(Action.ActionType type)
        {
            if (type.IsCheckedConditon) return "CheckedCondition\"";
            if (type.IsChecksConditon)  return "ChecksCondition\"";
            if (type.IsExcludedBy)      return "ExcludedBy\"";
            if (type.IsExcludes)        return "Excludes\"";
            if (type.IsExecuteFinish)   return "ExecuteFinish\",style=filled,fillcolor=green";
            if (type.IsExecuteStart)    return "ExecuteStart\"";
            if (type.IsIncludedBy)      return "IncludedBy\"";
            if (type.IsIncludes)        return "Includes\"";
            if (type.IsLockedBy)        return "LockedBy\"";
            if (type.IsLocks)           return "Locks\"";
            if (type.IsSetPendingBy)    return "SetPendingBy\"";
            if (type.IsSetsPending)     return "SetsPending\"";
            if (type.IsUnlockedBy)      return "UnlockedBy\"";
            if (type.IsUnlocks)         return "Unlocks\"";

            throw new ArgumentException("Unknown type", nameof(type));
        }

        public static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: GraphOptionToGravizo FileToRead [FileToWrite]");
                return;
            }
            TypeDescriptor.AddAttributes(
                typeof(Tuple<string, int>),
                new TypeConverterAttribute(typeof(TupleConverter)));
            var file = args[0];
            var json = File.ReadAllText(file);

            var graph = JsonConvert.DeserializeObject<FSharpOption<Graph.Graph>>(json).Value.Nodes;

            var gravizoGraph = graph.Select(kvPair => kvPair.Value)
                .Select(
                    action =>
                        new
                        {
                            ActionString = $"{action.Id.Item2}[label=\"{action.Id.Item1}, {ActionToString(action.Type)}];",
                            EdgesStrings = action.Edges.Select(edge => $"{action.Id.Item2}->{edge.Item2};")
                        });

            TextWriter writer;
            if (args.Length >= 2)
            {
                var outStream = File.OpenWrite(args[1]);
                writer = new StreamWriter(outStream);
            }
            else
            {
                writer = Console.Out;
            }
            writer.WriteLine("digraph G {");
            foreach (var element in gravizoGraph)
            {
                writer.WriteLine(element.ActionString);
                foreach (var edgesString in element.EdgesStrings)
                {
                    writer.WriteLine(edgesString);
                }
            }
            writer.WriteLine("}");

            writer.Flush();
            writer.Close();

            if (args.Length >= 2)
            {
                try
                {
                    Process.Start("gvedit", args[1]);
                }
                catch (Exception)
                {
                    Console.WriteLine("If you had Graphviz installed in path, GVEdit would have launched now!");
                    Console.Write("Press any key to exit...");
                    Console.Read();
                }
            }
        }
    }
}
