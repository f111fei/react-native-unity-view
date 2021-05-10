using System.Linq;
using Reinforced.Typings.Ast;
#pragma warning disable 1591
namespace Reinforced.Typings.Visitors.TypeScript
{
    partial class TypeScriptExportVisitor
    {
        public override void Visit(RtAnonymousObject node)
        {
            if (node == null) return;

            Write("{"); Br();
            AppendTabs();
            Tab();

            foreach (var rtMember in DoSortMembers(node.Members))
            {
                Visit(rtMember);
            }
            Br();
            UnTab();
            AppendTabs(); WriteLine("}");
        }

    }
}
