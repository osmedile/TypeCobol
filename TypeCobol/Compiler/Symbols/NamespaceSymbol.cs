﻿using System;
using TypeCobol.Compiler.Scopes;

namespace TypeCobol.Compiler.Symbols
{
    /// <summary>
    /// Symbol that represents a Namespace. A namespace can only contains 
    /// programs or namespaces.
    /// </summary>
    public class NamespaceSymbol : AbstractScope
    {
        /// <summary>
        /// Named constructor.
        /// </summary>
        /// <param name="name"></param>
        public NamespaceSymbol(string name)
            : base(name, Kinds.Namespace)
        {
            Types = new Scope<TypedefSymbol>(this);
            Programs = new Scope < ProgramSymbol >(this);
            Namespaces = new Scope<NamespaceSymbol>(this);
        }
        

        /// <summary>
        /// Enter a Program in this namespace
        /// </summary>
        /// <param name="name">Program's name</param>
        /// <returns>The ProgramSymbol</returns>
        public ProgramSymbol EnterProgram(string name)
        {
            Domain<ProgramSymbol>.Entry entry = Programs.Lookup(name);
            if (entry == null) 
            {
                ProgramSymbol prgSym = new ProgramSymbol(name);
                entry = Programs.Enter(prgSym);
            }
            //Set the owner
            entry.Symbol.Owner = this;
            //Add it to the all scope domain
            Symbol root = TopParent(Kinds.Root);
            //((RootSymbolTable)root)?.AddToDomain(entry.Symbol);
            return entry.Symbol;
        }

        /// <summary>
        /// Remove a program;
        /// </summary>
        /// <param name="prgSym">The program to be removed</param>
        public void RemoveProgram(ProgramSymbol prgSym)
        {
            if (prgSym != null)
            {
                Programs.Delete(prgSym);
                //Remove it from the all scope domain
                //Symbol root = TopParent(Kinds.Root);
                //((RootSymbolTable)root)?.RemoveFromDomain(prgSym);
                prgSym.Owner = null;
            }
        }

        /// <summary>
        /// All programs declared in this namespace.
        /// </summary>
        public override Scope<ProgramSymbol> Programs
        {
            get;
            protected set;
        }

        /// <summary>
        /// All namespaces declared in this namespace.
        /// </summary>
        public Scope<NamespaceSymbol> Namespaces
        {
            get;
            protected set;
        }

        /// <summary>
        /// All types declared in this namespace.
        /// </summary>
        public override Scope<TypedefSymbol> Types
        {
            get;
            protected set;
        }
        

        public override TR Accept<TR, TP>(IVisitor<TR, TP> v, TP arg) { return v.VisitNamespaceSymbol(this, arg); }
    }
}
