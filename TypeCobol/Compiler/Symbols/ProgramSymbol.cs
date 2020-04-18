using System.Collections.Generic;
using System.IO;
using System.Linq;
using TypeCobol.Compiler.CodeElements;
using TypeCobol.Compiler.Scopes;

namespace TypeCobol.Compiler.Symbols
{
    /// <summary>
    /// Represents a Program Symbol
    /// </summary>
    public class ProgramSymbol : AbstractScope
    {
        /// <summary>
        /// Named constructor.
        /// </summary>
        /// <param name="name"></param>
        public ProgramSymbol(string name) : base(name, Kinds.Program)
        {
            Types = new Scope<TypedefSymbol>(this);
            FileData = new Scope<VariableSymbol>(this);
            GlobalStorageData = new Scope<VariableSymbol>(this);
            WorkingStorageData = new Scope<VariableSymbol>(this);
            LocalStorageData = new Scope<VariableSymbol>(this);
            LinkageStorageData = new Scope<VariableSymbol>(this);
            Sections = new Scope<SectionSymbol>(this);
            Paragraphs = new Scope<ParagraphSymbol>(this);
            Programs = new Scope<ProgramSymbol>(this);
            Domain = new Domain<VariableSymbol>();
        }

        /// <summary>
        /// All types of this program.
        /// </summary>
        public override Scope<TypedefSymbol> Types
        {
            get;
            protected set;
        }

        /// <summary>
        /// File data scope of the program.
        /// </summary>
        public override Scope<VariableSymbol> FileData
        {
            get;
            protected set;
        }

        /// <summary>
        /// Global Storage data scope of the program.
        /// </summary>
        public override Scope<VariableSymbol> GlobalStorageData
        {
            get;
            protected set;
        }

        /// <summary>
        /// Working Storage data scope of the program.
        /// </summary>
        public override Scope<VariableSymbol> WorkingStorageData
        {
            get;
            protected set;
        }

        /// <summary>
        /// Working Storage data scope of the program.
        /// </summary>
        public override Scope<VariableSymbol> LocalStorageData
        {
            get;
            protected set;
        }

        /// <summary>
        /// Linkage Storage data scope of the program.
        /// </summary>
        public override Scope<VariableSymbol> LinkageStorageData
        {
            get;
            protected set;
        }

        /// <summary>
        /// Section scope of the program.
        /// </summary>
        public override Scope<SectionSymbol> Sections
        {
            get;
            protected set;
        }

        /// <summary>
        /// Paragraph scope of the program.
        /// </summary>
        public override Scope<ParagraphSymbol> Paragraphs
        {
            get;
            protected set;
        }

        /// <summary>
        /// Programs scope of the program.
        /// </summary>
        public override Scope<ProgramSymbol> Programs
        {
            get;
            protected set;
        }

        /// <summary>
        /// The Domain of this program.
        /// </summary>
        internal Domain<VariableSymbol> Domain
        {
            get;
            set;
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
            entry.Symbol.Owner = this;
            return entry.Symbol;
        }
        
        /// <summary>
        /// Add the given VariableSymbol instance in this Program domain
        /// </summary>
        /// <param name="varSym">The Variable Symbol to be added</param>
        /// <returns>The given VariableSymbol instance.</returns>
        public VariableSymbol AddToDomain(VariableSymbol varSym)
        {
            System.Diagnostics.Debug.Assert(varSym != null);
            lock (Domain)
            {
                //First add it in the Global Domain.
                //Symbol root = TopParent(Kinds.Root);
                //((RootSymbolTable) root)?.AddToUniverse(varSym);
                Domain.Add(varSym);
            }
            return varSym;
        }

        /// <summary>
        /// Is this program nested.
        /// </summary>
        public virtual bool IsNested => Owner != null && Kind == Kinds.Program && Owner.Kind == Kinds.Program;

        /// <summary>
        /// Get the Variable visibility mask.
        /// </summary>
        public virtual Flags VariableVisibilityMask => IsNested ? (Flags.Global | Flags.GLOBAL_STORAGE) : 0;

        /// <summary>
        /// Get the type visibility mask for a Program.
        /// </summary>
        public virtual Flags TypeVisibilityMask => IsNested ? (Flags.Global | Flags.Private | Flags.Public) : 0;

        /// <summary>
        /// Get the function visibility mask for a Program.
        /// </summary>
        public virtual Flags FunctionVisibilityMask => IsNested ? (Flags.Private | Flags.Public) : 0;

        
        
        /// <summary>
        /// Dump a section
        /// </summary>
        /// <param name="name">Section's name</param>
        /// <param name="section">The section to dump</param>
        /// <param name="tw">TextWriter instance</param>
        /// <param name="indentLevel">indentation level</param>
        private void DumpSection(string name, Scope<VariableSymbol> section, TextWriter tw, int indentLevel)
        {
            if (section.Any())
            {
                string s = new string(' ', 2 * indentLevel);
                tw.Write(s);
                tw.Write(name);
                tw.WriteLine();
                foreach (var v in section)
                {
                    v.Dump(tw, indentLevel);
                }
                tw.WriteLine();
            }
        }

        public void DumpFileSection(TextWriter tw, int indentLevel)
        {
            DumpSection("FILE SECTION.", this.FileData, tw, indentLevel);
        }
        public void DumpGlobalSection(TextWriter tw, int indentLevel)
        {
            DumpSection("GLOBAL-STORAGE SECTION.", this.GlobalStorageData, tw, indentLevel);
        }
        public void DumpWorkingSection(TextWriter tw, int indentLevel)
        {
            DumpSection("WORKING-STORAGE SECTION.", this.WorkingStorageData, tw, indentLevel);
        }
        public void DumpLocalSection(TextWriter tw, int indentLevel)
        {
            DumpSection("LOCAL-STORAGE SECTION.", this.LocalStorageData, tw, indentLevel);
        }
        public void DumpLinkageSection(TextWriter tw, int indentLevel)
        {
            DumpSection("LINKAGE SECTION.", this.LinkageStorageData, tw, indentLevel);
        }
        /// <summary>
        /// Dump the DataDivision
        /// </summary>
        /// <param name="tw"></param>
        /// <param name="indentLevel"></param>
        public void DumpDataDivision(TextWriter tw, int indentLevel)
        {
            string s = new string(' ', 2 * indentLevel);
            tw.Write(s);
            tw.Write("DATA DIVISION.");
            tw.WriteLine();
            DumpFileSection(tw, indentLevel);
            DumpGlobalSection(tw, indentLevel);
            DumpWorkingSection(tw, indentLevel);
            DumpLocalSection(tw, indentLevel);
            DumpLinkageSection(tw, indentLevel);
        }

        /// <summary>
        /// Dump all nested Programs.
        /// </summary>
        /// <param name="tw"></param>
        /// <param name="indentLevel"></param>
        public void DumpNestedPrograms(TextWriter tw, int indentLevel)
        {
            foreach (var p in Programs)
            {
                p.Dump(tw, indentLevel);
                tw.WriteLine();
            }
        }

        /// <summary>
        /// Dump this symbol in the given TextWriter instance.
        /// </summary>
        /// <param name="tw">TextWriter instance</param>
        /// <param name="indentLevel">Indentation level</param>
        public override void Dump(TextWriter tw, int indentLevel)
        {
            string s = new string(' ', 2 * indentLevel);
            tw.Write(s);
            tw.WriteLine("IDENTIFICATION DIVISION.");
            tw.Write(s);
            tw.Write("PROGRAM-ID. ");
            tw.Write(Name);
            tw.Write(".");
            tw.WriteLine();
            DumpDataDivision(tw, indentLevel);
            tw.WriteLine();
            tw.Write(s);
            tw.Write("PROCEDURE DIVISION.");
            this.Type?.Dump(tw, indentLevel + 1);
            tw.WriteLine();
            DumpNestedPrograms(tw, indentLevel);
            tw.Write(s);
            tw.Write("END PROGRAM ");
            tw.Write(Name);
            tw.Write(".");
        }

        public override TR Accept<TR, TP>(IVisitor<TR, TP> v, TP arg) { return v.VisitProgramSymbol(this, arg); }
    }
}
