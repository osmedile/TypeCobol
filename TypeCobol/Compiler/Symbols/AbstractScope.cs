using System;
using System.Collections.Generic;
using TypeCobol.Compiler.CodeElements;
using TypeCobol.Compiler.Scopes;
using TypeCobol.Compiler.Types;

namespace TypeCobol.Compiler.Symbols
{
    /// <summary>
    /// Represents any symbol that contain other symbols (i.e. ProgramSymbol or NamespaceSymbol)
    /// Don't confuse with Scope class
    /// </summary>
    public abstract class AbstractScope : Symbol, IScope
    {
        /// <summary>
        /// Named constructor
        /// </summary>
        protected AbstractScope(string name, Kinds kind)
            : base(name, kind)
        {
        }

        public virtual Scope<VariableSymbol> FileData
        {
            get { return null; }
            protected set {; }
        }

        public virtual Scope<VariableSymbol> GlobalStorageData
        {
            get { return null; }
            protected set {; }
        }

        public virtual Scope<VariableSymbol> WorkingStorageData
        {
            get { return null; }
            protected set {; }
        }

        public virtual Scope<VariableSymbol> LocalStorageData
        {
            get { return null; }
            protected set {; }
        }

        public virtual Scope<VariableSymbol> LinkageStorageData
        {
            get { return null; }
            protected set {; }
        }

        public virtual Scope<SectionSymbol> Sections
        {
            get { return null; }
            protected set {; }
        }

        public virtual Scope<ParagraphSymbol> Paragraphs
        {
            get { return null; }
            protected set { }
        }
        

        public virtual Scope<ProgramSymbol> Programs
        {
            get { return null; }
            protected set { }
        }
                

        /// <summary>
        /// Compute the path represented by a Symbol Reference
        /// </summary>
        /// <param name="symRef">The Symbol Reference instance</param>
        /// <returns>The corresponding Path in the COBOL IN|OF ORDER. The paths are return ed in lower cases</returns>
        public static string[] SymbolReferenceToPath(SymbolReference datSymRef)
        {
            string[] paths = null;
            IList<SymbolReference> refs = null;

            if (datSymRef.IsQualifiedReference)
            {//Path in reverse order DVZF0OS3::EventList --> {EventList, DVZF0OS3}
                QualifiedSymbolReference qualifiedSymbolReference = datSymRef as QualifiedSymbolReference;
                refs = qualifiedSymbolReference.AsList();
            }
            else
            {
                refs = new List<SymbolReference>() { datSymRef };
            }

            paths = new string[refs.Count];
            for (int i = 0; i < refs.Count; i++)
            {
                paths[i] = refs[i].Name;
            }

            return paths;
        }
        
    }
}
