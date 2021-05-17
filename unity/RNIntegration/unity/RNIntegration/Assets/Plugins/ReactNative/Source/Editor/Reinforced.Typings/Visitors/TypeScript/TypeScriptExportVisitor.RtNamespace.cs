using System;
using System.Linq;
using Reinforced.Typings.Ast;
#pragma warning disable 1591
namespace Reinforced.Typings.Visitors.TypeScript
{
    partial class TypeScriptExportVisitor
    {
        public override void Visit(RtNamespace node)
        {
            if (node == null) return;

            if (!node.IsAmbientNamespace)
            {
                Context = WriterContext.Module;
                AppendTabs();
                foreach (var ns in node.Name.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    AppendTabs();
                    if (node.GenerationMode == NamespaceGenerationMode.Module)
                    {
                        WriteLine(String.Format("module {0} {{", ns));
                    }
                    else
                    {
                        if (node.Export) Write("export ");
                        WriteLine(String.Format("namespace {0} {{", ns));
                    }
                    Tab();
                }
            }
            foreach (var rtCompilationUnit in node.CompilationUnits.OrderBy(c => c is RtCompilationUnit ? ((RtCompilationUnit) c).Order : 0))
            {
                Visit(rtCompilationUnit);
            }
            if (!node.IsAmbientNamespace)
            {
                foreach (var ns in node.Name.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    Context = WriterContext.None;
                    UnTab();
                    AppendTabs();
                    WriteLine("}");
                }
            }
        }
    }
}
