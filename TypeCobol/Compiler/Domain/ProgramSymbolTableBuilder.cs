﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TypeCobol.Compiler.CodeElements;
using TypeCobol.Compiler.CodeModel;
using TypeCobol.Compiler.Diagnostics;
using TypeCobol.Compiler.Domain.Validator;
using TypeCobol.Compiler.Nodes;
using TypeCobol.Compiler.Scopes;
using TypeCobol.Compiler.Symbols;
using TypeCobol.Compiler.Types;
using Type = TypeCobol.Compiler.Types.Type;


namespace TypeCobol.Compiler.Domain
{
    /// <summary>
    /// A Symbol table builder for a program.
    /// 
    /// THESE ARE COBOL Rules
    /// ---------------------
    /// -"Global Program Scope" variables declared in working storage as global are visible to the entire program 
    ///     in which they are declared AND in all nested subprograms contained in that program.
    /// -"Local Scope" variables declared in working storage are visible to the entire program in which they are declared.
    /// -"Local Scope" variables declared in local storage are visible to the entire program in which they are declared,
    ///     but are deleted and reinitialized on every invocation.
    /// -"Nested Program Scope" Cobol does not distinguish between programs and functions/procedures, 
    /// its equivalent of a procedure or function is called a program.An infinite number of programs can be contained within a program, 
    /// and the variables of each are visible only within the scope of that individual program.
    /// You could think of this as function/procedure scope.
    /// 
    /// TypeCobol Rules can be read at: https://github.com/TypeCobolTeam/TypeCobol/issues/1081
    /// --------------------------------
    /// </summary>
    public class ProgramSymbolTableBuilder : SymbolTableBuilder
    {
        public enum DataDivisionSection
        {
            None,
            File,
            Global,
            Working,
            Local,
            Linkage
        };

        /// <summary>
        /// Add Diagnostics
        /// </summary>
        public IList<Diagnostic> Diagnostics { get; private set; }

        /// <summary>
        /// The List of Stacked Program symbol built as a Scope.
        /// The first main program will be the first element of the list, 
        /// followed by remaining stacked programs.
        /// </summary>
        public List<ProgramSymbol> Programs
        {
            get;
            private set;
        }

        /// <summary>
        /// The Current Program symbol being built as a Scope
        /// </summary>
        private ProgramSymbol CurrentProgram
        {
            get;
            set;
        }

        /// <summary>
        /// The Current scope
        /// </summary>
        private AbstractScope CurrentScope
        {
            get;
            set;
        }

        /// <summary>
        /// The current entered node.
        /// </summary>
        private Node CurrentNode
        {
            get;
            set;
        }

        /// <summary>
        /// The Last exited node.
        /// </summary>
        private Node LastExitedNode
        {
            get;
            set;
        }

        /// <summary>
        /// The last data definition symbol.
        /// </summary>
        private VariableSymbol LastDataDefinitionSymbol
        {
            get;
            set;
        }

        /// <summary>
        /// The Current DataDivision section
        /// </summary>
        private DataDivisionSection CurrentDataDivisionSection
        {
            get;
            set;
        }

        /// <summary>
        /// RootSymbolTable instance constructor.
        /// </summary>
        /// <param name="root">The RootSymbolTable to be used</param>
        public ProgramSymbolTableBuilder()
        {
            Programs = new List<ProgramSymbol>();
            Diagnostics = new List<Diagnostic>();
        }

        public override void OnNode(Node node, Program program)
        {
            
        }

        /// <summary>
        /// Called when a node is entered
        /// </summary>
        /// <param name="node">The entered node.</param>
        public override void Enter(Node node)
        {
            CurrentNode = node;
            node.SemanticData = CurrentScope;
        }

        public override void Exit(Node node)
        {
            //System.Diagnostics.Contracts.Contract.Invariant(CurrentNode == node);
            System.Diagnostics.Debug.Assert(CurrentNode == node);
            CurrentNode = node.Parent;
            LastExitedNode = node;

#if DEBUG
            //Node is not a program and define a Symbol
            if(!(node is Program) && node.CodeElement?.StorageAreaDefinitions?.Count > 0 ) {
                //These node should have a SemanticData set to a specifialized Symbol (Variable, Pargraph, ...)
                
                System.Diagnostics.Debug.Assert(!(node.SemanticData is ProgramSymbol));
            }
#endif
        }

        public override void StartCobolProgram(ProgramIdentification programIdentification, LibraryCopyCodeElement libraryCopy)
        {
            System.Diagnostics.Debug.Assert(CurrentNode != null);
            System.Diagnostics.Debug.Assert(CurrentNode.CodeElement == programIdentification);
            bool bDuplicate = false;
            if (this.CurrentProgram == null)
            {//This is the main program or a stacked program with no parent.

                ProgramSymbol prgSym = new ProgramSymbol(programIdentification.ProgramName.Name);


                this.CurrentProgram = prgSym;
                //Add the new Stacked program.
                Programs.Add(CurrentProgram);
            }
            else
            {//Nested program.
                System.Diagnostics.Debug.Assert(CurrentNode.Parent != null);
                System.Diagnostics.Debug.Assert(CurrentNode.Parent.CodeElement != null);
                System.Diagnostics.Debug.Assert(CurrentNode.Parent.CodeElement.Type == CodeElementType.ProgramIdentification);
                var prgEntry = this.CurrentProgram.Programs.Lookup(programIdentification.ProgramName.Name);
                if (prgEntry == null)
                {
                    ProgramSymbol nestedProgram = new ProgramSymbol(programIdentification.ProgramName.Name);
                    //Reenter the program as nested here and change the parent.
                    this.CurrentProgram.Programs.Enter(nestedProgram);
                    nestedProgram.Owner = this.CurrentProgram;
                    this.CurrentProgram = nestedProgram;
                    //Add it to the all scope domain
                    //this.MyRoot.AddToDomain(nestedProgram);
                }
                else
                {
                    bDuplicate = true;
                }
            }

            if (bDuplicate)
            {
                Diagnostic d = new Diagnostic(MessageCode.SemanticTCErrorInParser,
                    programIdentification.Column,
                    programIdentification.Column,
                    programIdentification.Line,
                    string.Format(TypeCobolResource.DuplicateProgram, programIdentification.ProgramName.Name));
                Diagnostics.Add(d);
            }
            //The Program Type
            if (this.CurrentProgram.Type == null)
            {
                this.CurrentProgram.Type = new ProgramType(this.CurrentProgram);
            }
            //Semantic data on the node
            CurrentNode.SemanticData = this.CurrentProgram;
            //Current scope is the current program.
            this.CurrentScope = this.CurrentProgram;
        }

        public override void EndCobolProgram(TypeCobol.Compiler.CodeElements.ProgramEnd end)
        {
            //------------------------------------------------------------------------------------------------------------
            //System.Diagnostics.Debug.Assert(LastExitedNode != null);
            //System.Diagnostics.Debug.Assert(LastExitedNode.CodeElement != null);
            //System.Diagnostics.Debug.Assert(LastExitedNode.CodeElement.Type == CodeElementType.ProgramIdentification);
            //------------------------------------------------------------------------------------------------------------

            //For a stacked program the Parent is null and not for a nested program.
            this.CurrentProgram = (ProgramSymbol) LastExitedNode.Parent?.SemanticData;
        }

        public override void StartDataDivision(DataDivisionHeader header)
        {
            CurrentDataDivisionSection = DataDivisionSection.None;
            LastDataDefinitionSymbol = null;
        }

        public override void StartFileSection(FileSectionHeader header)
        {
            CurrentDataDivisionSection = DataDivisionSection.File;
            LastDataDefinitionSymbol = null;
        }

        public override void EndFileSection()
        {
            CurrentDataDivisionSection = DataDivisionSection.None;
            LastDataDefinitionSymbol = null;
        }

        public override void StartGlobalStorageSection(GlobalStorageSectionHeader header)
        {
            CurrentDataDivisionSection = DataDivisionSection.Global;
            LastDataDefinitionSymbol = null;
        }

        public override void EndGlobalStorageSection()
        {
            CurrentDataDivisionSection = DataDivisionSection.None;
            LastDataDefinitionSymbol = null;
        }

        public override void StartWorkingStorageSection(WorkingStorageSectionHeader header)
        {
            CurrentDataDivisionSection = DataDivisionSection.Working;
            LastDataDefinitionSymbol = null;
        }

        public override void EndWorkingStorageSection()
        {
            CurrentDataDivisionSection = DataDivisionSection.None;
            LastDataDefinitionSymbol = null;
        }

        public override void StartLocalStorageSection(LocalStorageSectionHeader header)
        {
            CurrentDataDivisionSection = DataDivisionSection.Local;
            LastDataDefinitionSymbol = null;
        }

        public override void EndLocalStorageSection()
        {
            CurrentDataDivisionSection = DataDivisionSection.None;
            LastDataDefinitionSymbol = null;
        }

        public override void StartLinkageSection(LinkageSectionHeader header)
        {
            CurrentDataDivisionSection = DataDivisionSection.Linkage;
            LastDataDefinitionSymbol = null;
        }

        public override void EndLinkageSection()
        {
            CurrentDataDivisionSection = DataDivisionSection.None;
            LastDataDefinitionSymbol = null;
        }

        public override void EndDataDivision()
        {
            LastDataDefinitionSymbol = null;
        }

        /// <summary>
        /// Resolve in the current program linkage section, the given parameter.
        /// </summary>
        /// <param name="p">The parameter to resolve.</param>
        /// <returns>The resolved variable if any, null otherwise</returns>
        private VariableSymbol ResolveUsingParameter(CallTargetParameter p)
        {
            string pname = p.StorageArea.SymbolReference.Name;
            var pvar = this.CurrentProgram.LinkageStorageData.Lookup(pname);
            if (pvar != null)
            {
                if (p.SharingMode == null)
                    pvar.Symbol.SetFlag(Symbol.Flags.ByReference, true);
                else
                switch (p.SharingMode.Value)
                {
                    case ParameterSharingMode.ByContent:
                        pvar.Symbol.SetFlag(Symbol.Flags.ByContent, true);
                        break;
                    case ParameterSharingMode.ByReference:
                        pvar.Symbol.SetFlag(Symbol.Flags.ByReference, true);
                        break;
                    case ParameterSharingMode.ByValue:
                        pvar.Symbol.SetFlag(Symbol.Flags.ByValue, true);
                        break;
                }

                if (p.PassingDirection != null)
                switch (p.PassingDirection.Value)
                {
                    case ParameterPassingDirection.Input:
                        pvar.Symbol.SetFlag(Symbol.Flags.Input, true);
                        break;
                    case ParameterPassingDirection.Output:
                        pvar.Symbol.SetFlag(Symbol.Flags.Output, true);
                        break;
                    case ParameterPassingDirection.InOut:
                        pvar.Symbol.SetFlag(Symbol.Flags.Inout, true);
                        break;
                    case ParameterPassingDirection.Returning:
                        pvar.Symbol.SetFlag(Symbol.Flags.Returning, true);
                        break;
                }
            }
            return pvar?.Symbol;
        }

        /// <summary>
        /// Starting a PROCEDURE DIVISION => Collect all parameters.
        /// </summary>
        /// <param name="header"></param>
        public override void StartProcedureDivision(ProcedureDivisionHeader header)
        {
            System.Diagnostics.Debug.Assert(CurrentProgram != null);
            System.Diagnostics.Debug.Assert(CurrentProgram.Type != null);
            System.Diagnostics.Debug.Assert(CurrentProgram.Type.Tag == Type.Tags.Program);
            if (CurrentProgram.Type != null)
            {
                LastDataDefinitionSymbol = null;
                List<VariableSymbol> usings = new List<VariableSymbol>();
                IList<CallTargetParameter> usingParams = header.UsingParameters;
                if (usingParams != null)
                {
                    foreach (var p in usingParams)
                    {
                        var pvar = ResolveUsingParameter(p);
                        if (pvar != null)
                        {
                            usings.Add(pvar);
                        }
                    }
                }
                CallTargetParameter retParam = header.ReturningParameter;
                VariableSymbol returnVar = null;
                if (retParam != null)
                {
                    returnVar = ResolveUsingParameter(retParam);
                }

                ProgramType prgType = (ProgramType) CurrentProgram.Type;
                prgType.Usings = usings;
                prgType.ReturnVariable = returnVar;
            }
        }

        public override void EndProcedureDivision()
        {
            LastDataDefinitionSymbol = null;
        }
        
        /// <summary>
        /// Convert a data usage to a Type UsageFormat.
        /// </summary>
        /// <param name="usage"></param>
        /// <returns></returns>
        public static Type.UsageFormat DataUsage2UsageFormat(DataUsage usage)
        {
            switch (usage)
            {
                case DataUsage.Binary:
                case DataUsage.NativeBinary:
                    return Type.UsageFormat.Binary;

                case DataUsage.FloatingPoint:
                    return Type.UsageFormat.Comp1;

                case DataUsage.Display:
                    return Type.UsageFormat.Display;

                case DataUsage.FunctionPointer:
                    return Type.UsageFormat.FunctionPointer;

                case DataUsage.Index:
                    return Type.UsageFormat.Index;

                case DataUsage.National:
                    return Type.UsageFormat.National;

                case DataUsage.None:
                    return Type.UsageFormat.None;

                case DataUsage.ObjectReference:
                    return Type.UsageFormat.ObjectReference;

                case DataUsage.PackedDecimal:
                    return Type.UsageFormat.PackedDecimal;

                case DataUsage.Pointer:
                    return Type.UsageFormat.Pointer;

                case DataUsage.ProcedurePointer:
                    return Type.UsageFormat.ProcedurePointer;

                case DataUsage.LongFloatingPoint:
                    return Type.UsageFormat.Comp2;

                case DataUsage.DBCS:
                    return Type.UsageFormat.Display1;
                default:
                    return Type.UsageFormat.None;

            }
        }

        /// <summary>
        /// Checks if the given DataDefinition instance has a single Usage definition
        /// </summary>
        /// <param name="dataDef">The Data Definition to be checked</param>
        /// <returns>True if it has a single usage definition, false otherwise</returns>
        private static bool HasSingleUsageDefinition(DataDefinition dataDef)
        {
            return dataDef.Picture == null && dataDef.Usage != null && dataDef.Usage != DataUsage.None;
        }

        /// <summary>
        /// Checks if the given DataDefinition instance is only a single Usage definition
        /// </summary>
        /// <param name="dataDef">The Data Definition to be checked</param>
        /// <returns>True if it is a single usage definition, false otherwise</returns>
        private static bool IsSingleUsageDefinition(DataDefinition dataDef)
        {
            return dataDef.ChildrenCount == 0 && HasSingleUsageDefinition(dataDef);
        }

        /// <summary>
        /// Checks if the given DataDefinition instance has single picture definition
        /// </summary>
        /// <param name="dataDef">The Data Definition to be checked</param>
        /// <returns>True if it has a single picture definition, false otherwise</returns>
        internal static bool HasSinglePictureDefinition(DataDefinition dataDef)
        {
            return dataDef.Picture != null;
        }

        /// <summary>
        /// Checks if the given DataDefinition instance is only a single picture definition
        /// </summary>
        /// <param name="dataDef">The Data Definition to be checked</param>
        /// <returns>True if it is a single picture definition, false otherwise</returns>
        internal static bool IsSinglePictureDefinition(DataDefinition dataDef)
        {
            return dataDef.ChildrenCount == 0 && HasSinglePictureDefinition(dataDef);
        }

        /// <summary>
        /// Checks if the given DataDefinition is a Type Definition
        /// </summary>
        /// <param name="dataDef">The Data Definition to be checked</param>
        /// <returns>true if yes, false otherwise</returns>
        internal bool IsTypedefDefinition(DataDefinition dataDef)
        {
            return dataDef.CodeElement.Type == CodeElements.CodeElementType.DataDescriptionEntry &&
                dataDef.CodeElement is DataTypeDescriptionEntry;
        }

        /// <summary>
        /// Determines if the given DataDefinition instance is REDEFINES
        /// </summary>
        /// <param name="dataDef">The DataDefinition instance to be checked</param>
        /// <returns>true if yes, false otherwise</returns>
        internal bool IsRedefinedDataDefinition(DataDefinition dataDef)
        {
            return dataDef.CodeElement != null && dataDef.CodeElement.Type == CodeElementType.DataRedefinesEntry;
        }

        /// <summary>
        /// Determines if the given DataDefinition instance is RENAMES
        /// </summary>
        /// <param name="dataDef">The DataDefinition instance to be checked</param>
        /// <returns>true if yes, false otherwise</returns>
        internal bool IsRenamesDataDefinition(DataDefinition dataDef)
        {
            return dataDef.CodeElement != null && dataDef.CodeElement.Type == CodeElementType.DataRenamesEntry;
        }

        /// <summary>
        /// Create the Usage type corresponding to a DataDefinition.
        /// </summary>
        /// <param name="dataDef">The DataDefinition to create the usage type.</param>
        /// <returns>The usage type</returns>
        internal static Type CreateUsageType(DataDefinition dataDef)
        {
            System.Diagnostics.Debug.Assert(HasSingleUsageDefinition(dataDef));
            Type.UsageFormat usage = DataUsage2UsageFormat(dataDef.Usage.Value);
            Type type = BuiltinTypes.BuiltinUsageType(usage);
            return type;
        }

        /// <summary>
        /// This method handles the case if a symbol is a redefines symbol if so it creates a RedefinesSymbol instance
        /// otherwise it creates a VariableSymbol instance.
        /// The symbol will be put in the the domain of the current program if the typedef instance is null, otherwise it will
        /// be put in the typedef instance.
        /// </summary>
        /// <param name="type">The type of the Symbol to be created</param>
        /// <param name="dataDef">The DataDefintion instance from which the Symbol is created</param>
        /// <param name="parentScope">The current Parent Scope.</param>
        /// <param name="typedef">The TypedefSymbol instance if the symbol to be created is a field of a Typedef symbol, null otherwise</param>
        /// <returns>The Symbol created</returns>
        private VariableSymbol CreateAndAddRedefinesOrVariableSymbol(Type type, DataDefinition dataDef, Scope<VariableSymbol> parentScope, TypedefSymbol typedef)
        {
            VariableSymbol sym = IsRedefinedDataDefinition(dataDef)
                ? CreateRedefinesSymbol(dataDef, parentScope)
                : new VariableSymbol(dataDef.Name);
            if (sym != null)
            {
                sym.Type = type;
                DecorateSymbol(dataDef, sym, parentScope);
                if (typedef == null)
                    CurrentProgram.AddToDomain(sym);
                else
                    typedef.Add(sym);
            }
            return sym;
        }

        /// <summary>
        /// Create a Symbol instance for a variable of a single usage type.
        /// </summary>
        /// <param name="dataDef">The DataDefinition instance</param>
        /// <param name="parentScope">The current parent scope</param>
        /// <param name="typedef">not null if  we have been called by a TYPEDEF declaration, null otherwise</param>
        /// <returns>The Symbol instance of usage type.</returns>
        internal VariableSymbol CreateUsageSymbol(DataDefinition dataDef, Scope<VariableSymbol> parentScope, TypedefSymbol typedef)
        {
            System.Diagnostics.Debug.Assert(IsSingleUsageDefinition(dataDef));
            Type type = CreateUsageType(dataDef);
            return CreateAndAddRedefinesOrVariableSymbol(type, dataDef, parentScope, typedef);
        }

        /// <summary>
        /// Create the Picture Type of the Given DataDefinition
        /// </summary>
        /// <param name="dataDef">The DataDefinition to create the Picture Type</param>
        /// <returns>The Picture Type</returns>
        internal static PictureType CreatePictureType(DataDefinition dataDef)
        {
            System.Diagnostics.Debug.Assert(HasSinglePictureDefinition(dataDef));
            Type.UsageFormat usage = dataDef.Usage.HasValue ? DataUsage2UsageFormat(dataDef.Usage.Value) : Type.UsageFormat.None;
            PictureValidator pictureValidator = new PictureValidator(dataDef.Picture.Value, dataDef.SignIsSeparate);
            PictureType type = new PictureType(pictureValidator);
            //Use permissive Usage setter which allows COMP1 and COMP2
            type.Usage = usage;
            return type;
        }

        /// <summary>
        /// Create a Symbol instance for a variable of a single picture type.
        /// </summary>
        /// <param name="dataDef">The DataDefinition instance</param>
        /// <param name="parentScope">The current parent scope</param>
        /// <param name="typedef">not null if  we have been called by a TYPEDEF declaration, null otherwise</param>
        /// <returns>The Symbol instance of usage type.</returns>
        internal VariableSymbol CreatePictureSymbol(DataDefinition dataDef, Scope<VariableSymbol> parentScope, TypedefSymbol typedef)
        {
            System.Diagnostics.Debug.Assert(IsSinglePictureDefinition(dataDef));
            Type type = CreatePictureType(dataDef);
            return CreateAndAddRedefinesOrVariableSymbol(type, dataDef, parentScope, typedef);
        }

        /// <summary>
        /// Create an untyped symbol
        /// </summary>
        /// <param name="dataDef">The DataDefinition instance</param>
        /// <param name="parentScope">The current parent scope</param>
        /// <param name="typedef">not null if  we have been called by a TYPEDEF declaration, null otherwise</param>
        /// <returns>The untyped symbol</returns>
        internal VariableSymbol CreateSymbolWithoutType(DataDefinition dataDef, Scope<VariableSymbol> parentScope, TypedefSymbol typedef)
        {
            Type type = BuiltinTypes.BuiltinUsageType(Type.UsageFormat.None);
            return CreateAndAddRedefinesOrVariableSymbol(type, dataDef, parentScope, typedef);
        }

        /// <summary>
        /// Create a Group Symbol
        /// </summary>
        /// <param name="dataDef">The DataDefinition instance</param>
        /// <param name="parentScope">The current parent scope</param>
        /// <param name="typedef">not null if  we have been called by a TYPEDEF declaration, null otherwise</param>
        /// <returns></returns>
        internal VariableSymbol CreateGroupSymbol(DataDefinition dataDef, Scope<VariableSymbol> parentScope, TypedefSymbol typedef)
        {
            //We create a group symbol having the group type
            VariableSymbol sym = IsRedefinedDataDefinition(dataDef)
                ? CreateRedefinesSymbol(dataDef, parentScope)
                : new VariableSymbol(dataDef.Name);

            if (sym != null)
            {
                //We create the group type
                GroupType recType = new GroupType(sym);
                //Store the symbol associated to this Group Type.
                recType.Symbol = sym;
                sym.Type = recType;
                //Set any leading type.
                if (HasSingleUsageDefinition(dataDef))
                {
                    Type leadingType = CreateUsageType(dataDef);
                    recType.LeadingType = leadingType;
                }
                else if (HasSinglePictureDefinition(dataDef))
                {
                    Type leadingType = CreatePictureType(dataDef);
                    recType.LeadingType = leadingType;
                }

                DecorateSymbol(dataDef, sym, parentScope);
                if (typedef == null)
                    CurrentProgram.AddToDomain(sym);
                else
                    typedef.Add(sym);
                //We build the GroupType fields
                foreach (var child in dataDef.Children)
                {
                    DataDefinition df = (DataDefinition)child;
                    VariableSymbol df_sym = DataDefinition2Symbol(df, recType.Scope, typedef);
                    //if df_sym == null this may be a bad symbol.
                    if (df_sym != null)
                    {
                        recType.Scope.Enter(df_sym);
                        //Important set the Owner before calling HandleIndexes
                        df_sym.Owner = sym;
                        //Handle indexes belonging to this Data Definition
                        HandleIndexes(df, df_sym, recType.Scope, typedef);
                    }
                }
            }

            return sym;
        }

        /// <summary>
        /// Set the accessor modifiers for the given symbol.
        /// </summary>
        /// <param name="symbol">The Symbol to set the aaccessor modifier</param>
        /// <param name="modifier">The accessor's modifier</param>
        internal void SetSymbolAccessModifer(Symbol symbol, AccessModifier modifier)
        {
            switch (modifier)
            {
                case AccessModifier.Public:
                    symbol.SetFlag(Symbol.Flags.Public, true);
                    break;
                case AccessModifier.Private:
                    symbol.SetFlag(Symbol.Flags.Private, true);
                    break;
                default:
                    break;
            }
        }
        
        
        /// <summary>
        /// Create a condition variable symbol
        /// </summary>
        /// <param name="dataDef">The DataDefinition which is a DataConditionEntry</param>
        /// <param name="parentScope">The current parent scope</param>
        /// <param name="bCalledForTypedef">true if this is for a TYPEDEF declaration.</param>
        /// <returns>The Condition symbol</returns>
        internal VariableSymbol CreateConditionSymbol(DataDefinition dataDef, Scope<VariableSymbol> parentScope, TypedefSymbol typedef)
        {
            System.Diagnostics.Debug.Assert(dataDef.CodeElement.Type == CodeElementType.DataConditionEntry);
            VariableSymbol sym = new VariableSymbol(dataDef.Name);
            sym.Type = BuiltinTypes.Level88Type;
            DecorateSymbol(dataDef, sym, parentScope);
            if (typedef == null)
                CurrentProgram.AddToDomain(sym);
            else
                typedef.Add(sym);
            return sym;
        }

        /// <summary>
        /// Creates a REDEFINES symbol Not typed, but with the redefined symbol resolved.
        /// </summary>
        /// <param name="dataDef">The DataDefinition which is a DataRedefinesEntry</param>
        /// <param name="parentScope">The parent scope</param>
        /// <returns>The symbol which is a RedefinesSymbol not typed if the redefined symbol is resolved, null otherwise</returns>
        internal RedefinesSymbol CreateRedefinesSymbol(DataDefinition dataDef, Scope<VariableSymbol> parentScope)
        {
            System.Diagnostics.Debug.Assert(dataDef.CodeElement.Type == CodeElementType.DataRedefinesEntry);
            //System.Diagnostics.Debug.Assert(parentScope != null);
            DataRedefinesEntry dataRedefines = (DataRedefinesEntry) dataDef.CodeElement;
            SymbolReference symRef = dataRedefines.RedefinesDataName;
            if (parentScope == null)
            {//Redefines is not supported here
                Diagnostic d = new Diagnostic(MessageCode.SemanticTCErrorInParser,
                    symRef.NameLiteral.Token.Column,
                    symRef.NameLiteral.Token.EndColumn,
                    symRef.NameLiteral.Token.Line,
                    string.Format(TypeCobolResource.ErrRedefinesNotAllowedHere, symRef.Name));
                Diagnostics.Add(d);
                return null;
            }

            //Lookup the redifined symbol in the parent scope.
            var entry = parentScope.Lookup(symRef.Name);
            if (entry == null)
            {//Inexisting renames
                Diagnostic d = new Diagnostic(MessageCode.SemanticTCErrorInParser,
                    symRef.NameLiteral.Token.Column,
                    symRef.NameLiteral.Token.EndColumn,
                    symRef.NameLiteral.Token.Line,
                    string.Format(TypeCobolResource.UnknowRedefinesdSymbol, symRef.Name));
                Diagnostics.Add(d);
                return null;
            }
            //Find the Symbol which is the last in the parent scope which is not a redefines also.
            VariableSymbol matchingSymbol = null;
            List<VariableSymbol> previousRedefines = new List<VariableSymbol>();
            foreach (var variableSymbol in parentScope.Reverse())
            {
                matchingSymbol = variableSymbol;
                if (!variableSymbol.HasFlag(Symbol.Flags.Redefines))
                {
                    //Ignore all previous redefines
                    break;
                }
                previousRedefines.Add(variableSymbol);
            }

            VariableSymbol redefined = matchingSymbol != null ? entry.FirstOrDefault(s => s == matchingSymbol) : null;
            if (entry.Count > 1 && redefined == null)
            {
                Diagnostic d = new Diagnostic(MessageCode.SemanticTCErrorInParser,
                    symRef.NameLiteral.Token.Column,
                    symRef.NameLiteral.Token.EndColumn,
                    symRef.NameLiteral.Token.Line,
                    string.Format(TypeCobolResource.MultipleRenameSymbolFound, symRef.Name));
                Diagnostics.Add(d);
                return null;
            }
            //In fact the redefined must be the last symbol
            if (redefined == null)
            {
                //Check if we are redefining via a REDEFINES
                if (entry.Count == 1 && entry.Symbol.HasFlag(Symbol.Flags.Redefines) && previousRedefines.Contains(entry.Symbol))
                {//We are redefining via previous REDEFINES.
                    redefined = entry.Symbol;
                }
                else
                {
                    Diagnostic d = new Diagnostic(MessageCode.SemanticTCErrorInParser,
                        symRef.NameLiteral.Token.Column,
                        symRef.NameLiteral.Token.EndColumn,
                        symRef.NameLiteral.Token.Line,
                        string.Format(TypeCobolResource.ErrRedefineWasNotImmediatlyPrec, symRef.Name,
                            ((DataRedefinesEntry) dataDef.CodeElement).LevelNumber));
                    Diagnostics.Add(d);
                    return null;
                }
            }

            RedefinesSymbol sym = new RedefinesSymbol(dataDef.Name, redefined);
            return sym;
        }

        /// <summary>
        /// Create a Variable Symbol that represents an Index.
        /// </summary>
        /// <param name="dataDef"></param>
        /// <param name="parentScope">The parent scope</param>
        /// <param name="typedef">not null if  we have been called by a TYPEDEF declaration, null otherwise</param>
        /// <returns>The Index symbol</returns>
        internal IndexSymbol CreateIndexSymbol(DataDefinition dataDef, Scope<VariableSymbol> parentScope, TypedefSymbol typedef)
        {
            IndexSymbol sym = new IndexSymbol(dataDef.Name);
            DecorateSymbol(dataDef, sym, parentScope);
            if (typedef == null)
                CurrentProgram.AddToDomain(sym);
            else
                typedef.Add(sym);
            return sym;
        }

        /// <summary>
        /// Resolve a RENAMES referenced variable
        /// </summary>
        /// <param name="renamesd">The Renamed reference</param>
        /// <param name="parentScope">The current parent scope</param>
        /// <returns>The Renamed variable resolved if any, null otherwise</returns>
        private VariableSymbol ResolveRenamedVariable(SymbolReference renamed, Scope<VariableSymbol> parentScope)
        {
            Domain<VariableSymbol>.Entry candidateRenamed = null;//TODO OSMBeforeMerge CurrentProgram.ResolveReference(renamed, true);
            if (candidateRenamed == null || candidateRenamed.Count == 0)
            {
                Diagnostic d = new Diagnostic(MessageCode.SemanticTCErrorInParser,
                    renamed.NameLiteral.Token.Column,
                    renamed.NameLiteral.Token.EndColumn,
                    renamed.NameLiteral.Token.Line,
                    string.Format(TypeCobolResource.RenamesObjectNotFound, renamed.ToString()));
                Diagnostics.Add(d);
                return null;
            }

            if (candidateRenamed.Count > 1)
            {
                Diagnostic d = new Diagnostic(MessageCode.SemanticTCErrorInParser,
                    renamed.NameLiteral.Token.Column,
                    renamed.NameLiteral.Token.EndColumn,
                    renamed.NameLiteral.Token.Line,
                    string.Format(TypeCobolResource.VariableNotUnique, renamed.ToString()));
                Diagnostics.Add(d);
                return null;
            }                            
            var renamedSymbol = candidateRenamed.Symbol;
            //Check Level
            if (renamedSymbol.Level == 1 || renamedSymbol.Level == 77 || renamedSymbol.Level == 88 || renamedSymbol.Level == 66)
            {
                Diagnostic d = new Diagnostic(MessageCode.SemanticTCErrorInParser,
                    renamed.NameLiteral.Token.Column,
                    renamed.NameLiteral.Token.EndColumn,
                    renamed.NameLiteral.Token.Line,
                    string.Format(TypeCobolResource.CannotRenamesLevel, renamedSymbol.Level));
                Diagnostics.Add(d);
                return null;
            }
            return renamedSymbol;
        }

        /// <summary>
        /// Renames context for validation.
        /// </summary>
        private struct RenamesContext
        {
            /// <summary>
            /// Renames DataDefinition object
            /// </summary>
            public DataDefinition DataDef
            { get; set; }

            /// <summary>
            /// The renames symbol
            /// </summary>
            public RenamesSymbol Symbol
            { get; set;}

            /// <summary>
            /// The parent scope of the rename
            /// </summary>
            public Scope<VariableSymbol> ParentScope
            { get; set;}
        }

        /// <summary>
        /// The List of Renames to validate.
        /// </summary>
        private readonly List<RenamesContext> _renamesToValidate = new List<RenamesContext>();

        /// <summary>
        /// Validate a RENAMES Symbol.
        /// </summary>
        /// <param name="renamesCtx">The RENAMES context</param>
        /// <returns>The computed Rename Type if successful, false otherwise.</returns>
        private RenamesType ValidateRenamesSymbol(RenamesContext renamesCtx)
        {
            DataDefinition dataDef = renamesCtx.DataDef;
            Scope< VariableSymbol > parentScope = renamesCtx.ParentScope;
            RenamesSymbol renamesSymbol = renamesCtx.Symbol;
            System.Diagnostics.Debug.Assert(dataDef.CodeElement.Type == CodeElementType.DataRenamesEntry);
            DataRenamesEntry dataRenEntry = (DataRenamesEntry)dataDef.CodeElement;
            SymbolReference from = dataRenEntry.RenamesFromDataName;
            SymbolReference to = dataRenEntry.RenamesToDataName;

            VariableSymbol fromSymbol = ResolveRenamedVariable(from, parentScope);
            if (fromSymbol == null)
                return null;

            VariableSymbol toSymbol = null;
            if (to != null)
            {
                toSymbol = ResolveRenamedVariable(to, parentScope);
                if (toSymbol == null)
                    return null;
            }

            //It must renames the last group
            System.Diagnostics.Debug.Assert(parentScope.Owner != null);
            Symbol zeroOneParent = parentScope.Owner.LookupParentLevelSymbol(01, true);
            VariableSymbol lastSymbol = zeroOneParent != null && zeroOneParent.Kind == Symbol.Kinds.Variable
                ? (VariableSymbol)zeroOneParent : null;
            if (!(lastSymbol?.Type != null && lastSymbol.Type.Tag == Type.Tags.Group))
            {
                Diagnostic d = new Diagnostic(MessageCode.SemanticTCErrorInParser,
                    from.NameLiteral.Token.Column,
                    from.NameLiteral.Token.EndColumn,
                    from.NameLiteral.Token.Line,
                    string.Format(TypeCobolResource.RenamesNotAGroup, dataDef.Name));
                Diagnostics.Add(d);
                return null;
            }

            //Full Validate the rename.
            GroupType containerType = (GroupType)lastSymbol.Type;
            RenamesValidator validator = new RenamesValidator(renamesSymbol, containerType, fromSymbol, toSymbol);
            lastSymbol.Accept(validator, null);
            if (!validator.IsValid)
            {
                if (!validator.FromSeen)
                {
                    Diagnostic d = new Diagnostic(MessageCode.SemanticTCErrorInParser,
                        from.NameLiteral.Token.Column,
                        from.NameLiteral.Token.EndColumn,
                        from.NameLiteral.Token.Line,
                        string.Format(TypeCobolResource.RenamesObjectNotFound, from.Name));
                    Diagnostics.Add(d);
                    return null;
                }
                if (!validator.ToSeen)
                {
                    Diagnostic d = new Diagnostic(MessageCode.SemanticTCErrorInParser,
                        to.NameLiteral.Token.Column,
                        to.NameLiteral.Token.EndColumn,
                        to.NameLiteral.Token.Line,
                        string.Format(TypeCobolResource.RenamesObjectNotFound, to.Name));
                    Diagnostics.Add(d);
                    return null;
                }
                if (validator.FromIndex > validator.ToIndex)
                {
                    Diagnostic d = new Diagnostic(MessageCode.SemanticTCErrorInParser,
                        dataDef.CodeElement.Line,
                        dataDef.CodeElement.Column,
                        dataDef.CodeElement.Column,
                        string.Format(TypeCobolResource.RenamesObjectOrder, from.Name, to.Name));
                    Diagnostics.Add(d);
                    return null;
                }
                if (validator.ContainsOccur)
                {
                    Diagnostic d = new Diagnostic(MessageCode.SemanticTCErrorInParser,
                        dataDef.CodeElement.Line,
                        dataDef.CodeElement.Column,
                        dataDef.CodeElement.Column,
                        string.Format(TypeCobolResource.RenamesContainsOccur, validator.OccurSymbol.Name));
                    Diagnostics.Add(d);
                    return null;
                }
            }
            return validator.Type;
        }

        /// <summary>
        /// Validate all pending current RENAMES.
        /// </summary>
        private void ValidateRenames()
        {
            try
            {
                foreach (var rename in _renamesToValidate)
                {
                    RenamesType type = ValidateRenamesSymbol(rename);
                    if (type != null)
                    {
                        rename.Symbol.Type = type;
                    }
                }
            }
            finally
            {
                _renamesToValidate.Clear();
            }
        }

        /// <summary>
        /// Create a Rename Symbol
        /// </summary>
        /// <param name="dataDef">The Data Definition of the rename symbol</param>
        /// <param name="parentScope">The parent scope</param>
        /// <param name="typedef">not null if  we have been called by a TYPEDEF declaration, null otherwise</param>
        /// <returns>The rename symbol instance if one hase been created, null otherwise</returns>
        internal RenamesSymbol CreateRenameSymbol(DataDefinition dataDef, Scope<VariableSymbol> parentScope, TypedefSymbol typedef)
        {
            System.Diagnostics.Debug.Assert(dataDef.CodeElement.Type == CodeElementType.DataRenamesEntry);
            RenamesSymbol sym = new RenamesSymbol(dataDef.Name);
            //Add it to the list of RENAMES that must be validated afterwards.
            RenamesContext ctx = new RenamesContext();
            ctx.DataDef = dataDef;
            ctx.Symbol = sym;
            ctx.ParentScope = parentScope;
            _renamesToValidate.Add(ctx);

            DecorateSymbol(dataDef, sym, parentScope);
            if (typedef == null)
                CurrentProgram.AddToDomain(sym);
            else
                typedef.Add(sym);

            return sym;
        }

        /// <summary>
        /// Checks if the given DataDefinition maybe a group, that is to say
        /// It has Children but none of them are IndexDefinition.
        /// </summary>
        /// <param name="dataDef"></param>
        /// <returns></returns>
        private bool MaybeGroup(DataDefinition dataDef)
        {
            if (dataDef.ChildrenCount == 0)
                return false;
            foreach (var child in dataDef.Children)
            {
                //IndexDefintion doesn't have CodeElement.
                if (child.CodeElement != null)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Convert a Data Definition to a Symbol
        /// </summary>
        /// <param name="dataDef">The Data Definition to convert</param>
        /// <param name="parentScope">The Parent Scope which is used for instance to look for target REDEFINES or RENAMES symbols.</param>
        /// <param name="typedef">not null if  we have been called by a TYPEDEF declaration, null otherwise</param>
        /// <returns></returns>
        internal VariableSymbol DataDefinition2Symbol(DataDefinition dataDef, Scope<VariableSymbol> parentScope, TypedefSymbol typedef)
        {
            VariableSymbol sym = null;
            if (dataDef.CodeElement != null)
            {
                switch (dataDef.CodeElement.Type)
                {
                    case CodeElements.CodeElementType.DataDescriptionEntry:
                    case CodeElements.CodeElementType.DataRedefinesEntry:                    
                        {
                            DataDescriptionEntry entry = dataDef.CodeElement as DataDescriptionEntry;
                            //Special case Typedef
                            if (IsTypedefDefinition(dataDef) && typedef == null)
                            {
                                sym = null; //TODO OSM
                            }
                            else if (IsSingleUsageDefinition(dataDef))
                            {
                                sym = CreateUsageSymbol(dataDef, parentScope, typedef);
                            }
                            else if (IsSinglePictureDefinition(dataDef))
                            {
                                sym = CreatePictureSymbol(dataDef, parentScope, typedef);
                            }
                            else
                            {
                                if (!MaybeGroup(dataDef))
                                {//No Type symbol
                                    if (dataDef.DataType != null || (entry != null && entry.UserDefinedDataType != null))
                                    {
                                        //TypeCobol.Compiler.CodeElements.DataType dataType = dataDef.DataType;
                                        if (entry != null && entry.UserDefinedDataType != null)
                                        {
                                            sym = null; //TODO OSM
                                        }
                                        else
                                        {
                                            sym = CreateSymbolWithoutType(dataDef, parentScope, typedef);
                                        }
                                    }
                                    else
                                    {
                                        sym = CreateSymbolWithoutType(dataDef, parentScope, typedef);
                                    }
                                }
                                else
                                {//This is a group Type
                                    sym = CreateGroupSymbol(dataDef, parentScope, typedef);
                                }
                            }
                        }
                        break;
                    case CodeElements.CodeElementType.DataRenamesEntry:
                        {
                            sym = CreateRenameSymbol(dataDef, parentScope, typedef);
                        }
                        break;
                    case CodeElements.CodeElementType.DataConditionEntry:
                        {
                            sym = CreateConditionSymbol(dataDef, parentScope, typedef);
                        }
                        break;
                }
            }
            if (sym != null)
            {//Consider an array
                if (dataDef.MaxOccurencesCount > 1)
                {//Change the symbol type to an array type.
                    ArrayType arrayType = new ArrayType();
                    arrayType.MinOccur = dataDef.MinOccurencesCount;
                    arrayType.MaxOccur = dataDef.MaxOccurencesCount;
                    arrayType.ElementType = sym.Type;
                    sym.Type = arrayType;
                }                
            }
            dataDef.SemanticData = sym;
            return sym;
        }

        /// <summary>
        /// Handles Indexes associated to a DataDefinitions.
        /// </summary>
        /// <param name="dataDef">The Indexed Data Definition instance</param>
        /// <param name="indexedSym">The Indexed Symbol</param>
        /// <param name="typedef">not null if  we have been called by a TYPEDEF declaration, null otherwise</param>
        private void HandleIndexes(DataDefinition dataDef, VariableSymbol indexedSym, Scope<VariableSymbol> parentScope, TypedefSymbol typedef)
        {
            foreach (var child in dataDef.Children)
            {
                if (child.CodeElement == null && child is IndexDefinition indexDef)
                {
                    //An index definition symbol
                    var indexSym = CreateIndexSymbol(indexDef, parentScope, typedef);
                    //Attach the Indexed
                    indexSym.Owner = indexedSym.Owner;
                    indexSym.Indexed = indexedSym;
                    //Add the index in the current Scope.
                    System.Diagnostics.Debug.Assert(parentScope != null);
                    parentScope.Enter(indexSym);
                    if (indexedSym.HasFlag(Symbol.Flags.Global))
                    {//For a Global Index
                        indexSym.SetFlag(Symbol.Flags.Global, true);
                    }

                    indexDef.SemanticData = indexSym;
                }
            }
        }

        /// <summary>
        /// Get the scope of the current DataDivision section from the CurrentScope.
        /// </summary>
        private Scope<VariableSymbol> GetCurrentDataDivisionSectionScope()
        {
            switch (CurrentDataDivisionSection)
            {
                case DataDivisionSection.File:                    
                    return CurrentScope.FileData;
                case DataDivisionSection.Global:
                    return CurrentScope.GlobalStorageData;
                case DataDivisionSection.Working:
                    return CurrentScope.WorkingStorageData;
                case DataDivisionSection.Local:
                    return CurrentScope.LocalStorageData;
                case DataDivisionSection.Linkage:
                    return CurrentScope.LinkageStorageData;
                default:
                    System.Diagnostics.Debug.Assert(CurrentDataDivisionSection == DataDivisionSection.File ||
                        CurrentDataDivisionSection == DataDivisionSection.Global ||
                        CurrentDataDivisionSection == DataDivisionSection.Working ||
                        CurrentDataDivisionSection == DataDivisionSection.Local ||
                        CurrentDataDivisionSection == DataDivisionSection.Linkage
                        );
                    break;
            }

            return null;
        }

        /// <summary>
        /// Get the current Data Division section flag.
        /// </summary>
        /// <returns></returns>
        internal Symbol.Flags? GetCurrentDataDivisionSectionFlag()
        {
            switch (CurrentDataDivisionSection)
            {
                case DataDivisionSection.File:
                    return Symbol.Flags.FILE_SECTION;
                case DataDivisionSection.Global:
                    return Symbol.Flags.GLOBAL_STORAGE;
                case DataDivisionSection.Working:
                    return Symbol.Flags.WORKING_STORAGE;
                case DataDivisionSection.Local:
                    return Symbol.Flags.LOCAL_STORAGE;
                case DataDivisionSection.Linkage:
                    return Symbol.Flags.LINKAGE;
                default:
                    break;
            }

            return null;
        }

        /// <summary>
        /// Decorate the given Variable Symbol, with some flags
        /// </summary>
        /// <param name="dataDef">Symbol's Data Definition</param>
        /// <param name="sym">The Symbol to be decorated</param>
        /// <param name="parentScope">The parent scope</param>
        internal void DecorateSymbol(DataDefinition dataDef, VariableSymbol sym, Scope<VariableSymbol> parentScope)
        {
            dataDef.SemanticData = sym;
            if (sym.Owner == null)
                sym.Owner = parentScope.Owner;
            //Section flag
            Symbol.Flags? fSection = GetCurrentDataDivisionSectionFlag();
            if (fSection.HasValue)
            {
                sym.SetFlag(fSection.Value, true);
            }

            //Global variable ?
            if (dataDef.CodeElement != null)
            {
                switch (dataDef.CodeElement.Type)
                {
                    case CodeElementType.DataConditionEntry:
                    {
                        sym.Level = 88;
                        if (parentScope.Owner != null)
                        {
                            if (parentScope.Owner.Kind != Symbol.Kinds.Program && parentScope.Owner.Kind != Symbol.Kinds.Function)
                                sym.SetFlag(parentScope.Owner.Flag & Symbol.SymbolVisibilityMask , parentScope.Owner.HasFlag(Symbol.SymbolVisibilityMask));                                
                        }
                        //Store Contition Values.
                        sym.Value = ((DataConditionEntry)dataDef.CodeElement).ConditionValues;

                     }
                        break;
                    case CodeElementType.DataRenamesEntry:
                    {
                        sym.Level = 66;
                        if (parentScope.Owner != null)
                        {
                            if (parentScope.Owner.Kind != Symbol.Kinds.Program && parentScope.Owner.Kind != Symbol.Kinds.Function)
                                    sym.SetFlag(parentScope.Owner.Flag & Symbol.SymbolVisibilityMask, parentScope.Owner.HasFlag(Symbol.SymbolVisibilityMask));
                        }
                     }
                        break;
                    case CodeElementType.DataDescriptionEntry:
                    case CodeElementType.DataRedefinesEntry:
                    {
                        CommonDataDescriptionAndDataRedefines dataDescEntry =
                            (CommonDataDescriptionAndDataRedefines)dataDef.CodeElement;
                        sym.Value = dataDescEntry.InitialValue;
                        sym.Level = dataDescEntry.LevelNumber != null ? (int)dataDescEntry.LevelNumber.Value : 0;
                        if (dataDescEntry.IsGlobal || parentScope.Owner.HasFlag(Symbol.Flags.Global))
                        {//No Global inside GLOBAL-STORAGE.
                            if (fSection.HasValue && fSection != Symbol.Flags.GLOBAL_STORAGE)
                            {
                                //This a global symbol
                                sym.SetFlag(Symbol.Flags.Global, true);
                            }
                        }
                            //Propagate other visibility than global
                            if (parentScope.Owner.Kind != Symbol.Kinds.Program && parentScope.Owner.Kind != Symbol.Kinds.Function)
                                sym.SetFlag(parentScope.Owner.Flag & Symbol.SymbolVisibilityMask & ~Symbol.Flags.Global, parentScope.Owner.HasFlag(Symbol.SymbolVisibilityMask & ~Symbol.Flags.Global));
                            //Other interresting flags that apply to a symbol.
                            if (dataDescEntry.IsBlankWhenZero != null && dataDescEntry.IsBlankWhenZero.Value)
                                sym.SetFlag(Symbol.Flags.BlankWhenZero, true);
                            if (dataDescEntry.IsJustified != null && dataDescEntry.IsJustified.Value)
                                sym.SetFlag(Symbol.Flags.Justified, true);
                            if (dataDescEntry.IsGroupUsageNational != null && dataDescEntry.IsGroupUsageNational.Value)
                                sym.SetFlag(Symbol.Flags.GroupUsageNational, true);
                            if (dataDescEntry.SignIsSeparate != null && dataDescEntry.SignIsSeparate.Value)
                                sym.SetFlag(Symbol.Flags.Sign, true);
                            if (dataDescEntry.IsSynchronized != null && dataDescEntry.IsSynchronized.Value)
                                sym.SetFlag(Symbol.Flags.Sync, true);
                            if (dataDef.CodeElement.Type == CodeElementType.DataDescriptionEntry)
                            {
                                DataDescriptionEntry dataDesc = (DataDescriptionEntry)dataDef.CodeElement;
                                if (dataDesc.IsExternal)
                                    sym.SetFlag(Symbol.Flags.External, true);
                            }
                        }
                        break;
                    default:
                        System.Diagnostics.Debug.Assert(dataDef.CodeElement.Type == CodeElementType.DataDescriptionEntry ||
                                 dataDef.CodeElement.Type == CodeElementType.DataRenamesEntry ||
                                 dataDef.CodeElement.Type == CodeElementType.DataRedefinesEntry ||
                                 dataDef.CodeElement.Type == CodeElementType.DataConditionEntry);
                        break;
                }
            }
        }

        /// <summary>
        /// Store the given data definition symbol in teh current DataDivion section
        /// </summary>
        /// <param name="dataDefSym"></param>
        private void StoreDataDivisionSymbol(VariableSymbol dataDefSym)
        {
            System.Diagnostics.Debug.Assert(dataDefSym != null);
            System.Diagnostics.Debug.Assert(CurrentScope != null);
            System.Diagnostics.Debug.Assert(CurrentDataDivisionSection != DataDivisionSection.None);
            //We must be in ProgramScope
            System.Diagnostics.Debug.Assert(CurrentScope is ProgramSymbol);

            if (dataDefSym.Owner == null) //Because Symbols as TYPEDEF already have their parent.
                dataDefSym.Owner = CurrentProgram;
            if (dataDefSym.Kind == Symbol.Kinds.Typedef)
            {//Typedef are already entered at creation time.
                ;
            }
            else switch (CurrentDataDivisionSection)
                {
                    case DataDivisionSection.File:
                        System.Diagnostics.Debug.Assert(CurrentScope.FileData != null);
                        CurrentScope.FileData.Enter(dataDefSym);
                        dataDefSym.SetFlag(Symbol.Flags.FILE_SECTION, true);
                        break;
                    case DataDivisionSection.Global:
                        System.Diagnostics.Debug.Assert(CurrentScope.GlobalStorageData != null);
                        CurrentScope.GlobalStorageData.Enter(dataDefSym);
                        dataDefSym.SetFlag(Symbol.Flags.GLOBAL_STORAGE, true);
                        break;
                    case DataDivisionSection.Working:
                        System.Diagnostics.Debug.Assert(CurrentScope.WorkingStorageData != null);
                        CurrentScope.WorkingStorageData.Enter(dataDefSym);
                        dataDefSym.SetFlag(Symbol.Flags.WORKING_STORAGE, true);
                        break;
                    case DataDivisionSection.Local:
                        System.Diagnostics.Debug.Assert(CurrentScope.LocalStorageData != null);
                        CurrentScope.LocalStorageData.Enter(dataDefSym);
                        dataDefSym.SetFlag(Symbol.Flags.LOCAL_STORAGE, true);
                        break;
                    case DataDivisionSection.Linkage:
                        System.Diagnostics.Debug.Assert(CurrentScope.LinkageStorageData != null);
                        CurrentScope.LinkageStorageData.Enter(dataDefSym);
                        dataDefSym.SetFlag(Symbol.Flags.LINKAGE, true);
                        break;
                    default:
                        System.Diagnostics.Debug.Assert(CurrentDataDivisionSection == DataDivisionSection.File ||
                            CurrentDataDivisionSection == DataDivisionSection.Global ||
                            CurrentDataDivisionSection == DataDivisionSection.Working ||
                            CurrentDataDivisionSection == DataDivisionSection.Local ||
                            CurrentDataDivisionSection == DataDivisionSection.Linkage
                            );
                        break;
                }
        }

        /// <summary>
        /// Level1 Definition Tracker, This tracker is used to create all DataDefinition symbols.
        /// </summary>
        /// <param name="level1Node">The level 1 definition node</param>
        public override void OnLevel1Definition(DataDefinition level1Node)
        {
            //Clear any pending RENAMES to validate.
            _renamesToValidate.Clear();
            var scope = GetCurrentDataDivisionSectionScope();
            VariableSymbol dataDefSym = DataDefinition2Symbol(level1Node, scope, null);
            this.LastDataDefinitionSymbol = dataDefSym;
            if (dataDefSym != null)
            {
                //We must validate all RENAMES at a 01 Level definition
                ValidateRenames();
                StoreDataDivisionSymbol(dataDefSym);
                //Handle indexes belonging to this Data Definition
                HandleIndexes(level1Node, dataDefSym, scope, null);
            }
        }

        public override void StartParagraph(ParagraphHeader header)
        {
            ParagraphSymbol paragraphSymbol = new ParagraphSymbol(header.ParagraphName.Name);
            CurrentNode.SemanticData = paragraphSymbol;
        }

        public override void StartSection(SectionHeader header)
        {
            SectionSymbol sectionSymbol = new SectionSymbol(header.SectionName.Name);
            CurrentNode.SemanticData = sectionSymbol;
        }
    }
}
