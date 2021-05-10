using System.Collections.Generic;
using Reinforced.Typings.Ast.TypeNames;

namespace Reinforced.Typings.Ast
{
    /// <summary>
    /// AST node for typeScript interface
    /// </summary>
    public class RtAnonymousObject : RtTypeName
    {
        /// <inheritdoc />
        public List<RtNode> Members { get; private set; }

        /// <inheritdoc />
        public override IEnumerable<RtNode> Children
        {
            get
            {
                foreach (var rtMember in Members)
                {
                    yield return rtMember;
                }
            }
        }

        /// <inheritdoc />
        public override void Accept(IRtVisitor visitor)
        {
            visitor.Visit(this);
        }

        /// <inheritdoc />
        public override void Accept<T>(IRtVisitor<T> visitor)
        {
            visitor.Visit(this);
        }

        /// <summary>
        /// Constructs new instance of AST node
        /// </summary>
        public RtAnonymousObject()
        {
            Members = new List<RtNode>();
        }

    }
}
