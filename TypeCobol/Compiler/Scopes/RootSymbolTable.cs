using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using TypeCobol.Compiler.Domain;
using TypeCobol.Compiler.Symbols;

namespace TypeCobol.Compiler.Scopes
{
    /// <summary>
    /// The Root Symbol Table is a special Namespace
    /// </summary>
    public class RootSymbolTable : NamespaceSymbol
    {
        /// <summary>
        /// All Kinds of scope that contains symbols (i.e. inheritors of AbstractScope except RootSymbolTable).
        /// </summary>
        private static readonly Symbol.Kinds[] _AllScopeKinds = new Kinds[] { Kinds.Namespace, Kinds.Program, Kinds.Function };

        /// <summary>
        /// This is the first variable of the universe that can be assimilated to the 0 or null variable.
        /// </summary>
        public static readonly VariableSymbol BottomVariable = new VariableSymbol("<<BottomVariable>>");

        /// <summary>
        /// The index of the last variable symbol entered in this table.
        /// </summary>
        private int _variableSymbolIndex;

        /// <summary>
        /// A pool of free global index to be reused when entering a new variable.
        /// </summary>
        private readonly Stack<int> _globalIndexPool;

        /// <summary>
        /// All Ordered Symbol that can be reached from this Root Symbol Table.
        /// This is in fact the entire domain of variable within this Root Symbol Table.
        /// </summary>
        private IList<VariableSymbol> Universe { get; }

        /// <summary>
        /// The Domain of all namespaces, programs and functions.
        /// </summary>
        private Domain<AbstractScope> ScopeDomain { get; }

        /// <summary>
        /// The Domain of all types.
        /// </summary>
        private Domain<TypedefSymbol> TypeDomain { get; }

        /// <summary>
        /// Empty Constructor.
        /// </summary>
        public RootSymbolTable()
            : base(string.Intern("<<Root>>"))
        {
            base.Kind = Kinds.Root;

            _variableSymbolIndex = 0;
            _globalIndexPool = new Stack<int>();

            Universe = new List<VariableSymbol>();
            ScopeDomain = new Domain<AbstractScope>();
            TypeDomain = new Domain<TypedefSymbol>();

            //Register BottomVariable
            AddToUniverse(BottomVariable);

            //Load Builtin symbols
            SymbolTableBuilder.AddBuiltinSymbol(this);
        }

        /// <summary>
        /// Full qualified name of this Symbol à la TypeCobol using "::"
        /// </summary>
        public override string FullName => "";

        /// <summary>
        /// Full qualified name of this Symbol à la COBOL85 using OF
        /// </summary>
        public override string FullOfName => "";

        /// <summary>
        /// Full dotted qualified name
        /// </summary>
        public override string FullDotName => "";

        /// <summary>
        /// Full typed dotted qualified name
        /// </summary>
        public override string FullTypedDotName => "";

        /// <summary>
        /// Name followed by type name.
        /// </summary>
        public override string TypedName => "";

        /// <summary>
        /// Program add event.
        /// </summary>
        public event EventHandler<SymbolEventArgs> ProgramAdded;

        /// <summary>
        /// Get the Next VariableSymbol Context.
        /// </summary>
        /// <returns></returns>
        private int NextVariableSymbolIndex()
        {
            if (_globalIndexPool.Count > 0)
            {
                //Re-use free global index.
                return _globalIndexPool.Pop();
            }

            //Increment global counter
            return _variableSymbolIndex++;
        }

        /// <summary>
        /// Add the given VariableSymbol instance in this Root Symbol Table universe
        /// </summary>
        /// <param name="varSym">The Variable Symbol to be added</param>
        /// <returns>The given VariableSymbol instance.</returns>
        internal VariableSymbol AddToUniverse(VariableSymbol varSym)
        {
            System.Diagnostics.Debug.Assert(varSym != null);
            System.Diagnostics.Debug.Assert(varSym.GlobalIndex == 0);

            varSym.GlobalIndex = NextVariableSymbolIndex();
            Universe.Add(varSym);
            return varSym;
        }

        /// <summary>
        /// Remove from the universe the given variable symbol.
        /// </summary>
        /// <param name="varSym">The variable symbol to be removed</param>
        internal void RemoveFromUniverse(VariableSymbol varSym)
        {
            System.Diagnostics.Debug.Assert(varSym != null);
            System.Diagnostics.Debug.Assert(varSym.GlobalIndex != 0);

            if (varSym.GlobalIndex != 0)
            {
                Universe[varSym.GlobalIndex] = null;
                _globalIndexPool.Push(varSym.GlobalIndex);
                varSym.GlobalIndex = 0;
            }
        }

        /// <summary>
        /// Add the given AbstractScope instance the domain
        /// </summary>
        /// <param name="absScope">Abstract Scope to be added</param>
        public override void AddToDomain(AbstractScope absScope)
        {
            System.Diagnostics.Debug.Assert(absScope != null);
            ScopeDomain.Add(absScope);
            if (ProgramAdded != null && absScope.Kind == Kinds.Program)
                ProgramAdded(this, new SymbolEventArgs(absScope));
        }
        

        /// <summary>
        /// Add the given Type instance the domain
        /// </summary>
        /// <param name="type">The type to add to be added</param>
        public override void AddToDomain(TypedefSymbol type)
        {
            System.Diagnostics.Debug.Assert(type != null);
            TypeDomain.Add(type);
        }

        /// <summary>
        /// Remove the given type from the domain.
        /// </summary>
        /// <param name="type">The type to be removed</param>
        public override void RemoveFromDomain(TypedefSymbol type)
        {
            System.Diagnostics.Debug.Assert(type != null);
            TypeDomain.Remove(type);
        }
        
        /// <summary>
        /// Searches for scopes of this RootSymbolTable having the given name.
        /// </summary>
        /// <param name="name">Name of the scope searched.</param>
        /// <returns>A non-null domain entry of scopes matching the given name.</returns>
        [NotNull]
        public Domain<AbstractScope>.Entry LookupScope([NotNull] string name)
        {
            System.Diagnostics.Debug.Assert(name != null);

            if (ScopeDomain.TryGetValue(name, out var result))
                return result;

            return new Domain<AbstractScope>.Entry(name);
        }
        
    }
}
