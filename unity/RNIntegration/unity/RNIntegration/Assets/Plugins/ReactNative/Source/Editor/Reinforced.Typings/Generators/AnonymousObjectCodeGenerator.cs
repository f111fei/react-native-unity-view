using System;
using System.Collections.Generic;
using System.Reflection;
using Reinforced.Typings.Ast;
using Reinforced.Typings.Attributes;

namespace Reinforced.Typings.Generators
{
    /// <summary>
    ///     Default code generator for interfaces. Derived from class generator since interfaces are very similar to classes in
    ///     TypeScript
    /// </summary>
    public class AnonymousObjectCodeGenerator : TsCodeGeneratorBase<MemberInfo[], RtAnonymousObject>
    {
        /// <summary>
        ///     Main code generator method. This method should write corresponding TypeScript code for element (1st argument) to
        ///     WriterWrapper (3rd argument) using TypeResolver if necessary
        /// </summary>
        /// <param name="element">Element code to be generated to output</param>
        /// <param name="result">Resulting node</param>
        /// <param name="resolver">Type resolver</param>
        public override RtAnonymousObject GenerateNode(MemberInfo[] element, RtAnonymousObject result, TypeResolver resolver)
        {
            GenerateMembers(resolver, result, element);
            return result;
        }

        /// <summary>
        ///     Exports list of type members
        /// </summary>
        /// <typeparam name="T">Type member type</typeparam>
        /// <param name="element">Exporting class</param>
        /// <param name="resolver">Type resolver</param>
        /// <param name="typeMember">Output writer</param>
        /// <param name="members">Type members to export</param>
        protected virtual void GenerateMembers<T>(TypeResolver resolver, RtAnonymousObject typeMember, IEnumerable<T> members) where T : MemberInfo
        {
            foreach (var m in members)
            {
                var generator = Context.Generators.GeneratorFor(m);
                var member = generator.Generate(m, resolver);
                if (member != null) typeMember.Members.Add(member);
            }
        }
    }
}