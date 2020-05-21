using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using TypeCobol.Compiler.CodeElements;
using TypeCobol.Compiler.CodeModel;
using TypeCobol.Compiler.Nodes;
using TypeCobol.Compiler.Parser;
using Analytics;
using TypeCobol.Compiler.Parser.Generated;

namespace TypeCobol.Compiler.CupParser.NodeBuilder
{
    /// <summary>
    /// The Program Class Builder
    /// </summary>
    public class ProgramClassBuilder : IProgramClassBuilder
    {
        public ProgramClassBuilder()
        {
            CurrentNode = Root;
        }

        /// <summary>
        /// Program object resulting of the visit the parse tree
        /// </summary>
        private Program Program { get; set; }

        /// <summary>Tree root</summary>
        public SourceFile Root { get; private set; } = new SourceFile();

        private TypeDefinition _CurrentTypeDefinition
        {
            get => _currentTypeDefinition;
            set
            {
                _currentTypeDefinition = value;
                //Reset that this type was already added to the list TypeThatNeedTypeLinking
                typeAlreadyAddedToTypeToLink = false;
            }
        }

        private bool typeAlreadyAddedToTypeToLink = false;

        private Node.Flag currentSectionFlag;
        
        private FunctionDeclaration _ProcedureDeclaration;

        public List<DataDefinition> TypedVariablesOutsideTypedef { get; } = new List<DataDefinition>();
        public List<TypeDefinition> TypeThatNeedTypeLinking { get; } = new List<TypeDefinition>();

        // Programs can be nested => track current programs being analyzed
        private Stack<Program> programsStack = null;

        private Program CurrentProgram
        {
            get { return programsStack.Peek(); }
            set { programsStack.Push(value); }
        }

        /// <summary>Class object resulting of the visit the parse tree</summary>
        public CodeModel.Class Class { get; private set; }

        private readonly SymbolTable TableOfIntrisic = new SymbolTable(null, SymbolTable.Scope.Intrinsic);
        private SymbolTable TableOfGlobals;
        private SymbolTable TableOfNamespaces;
        private SymbolTable TableOfGlobalStorage;

        public SymbolTable CustomSymbols
        {
            private get { throw new InvalidOperationException(); }
            set
            {
                if (value != null)
                {
                    SymbolTable intrinsicTable = value.GetTableFromScope(SymbolTable.Scope.Intrinsic);
                    SymbolTable nameSpaceTable = value.GetTableFromScope(SymbolTable.Scope.Namespace);

                    intrinsicTable.DataEntries.Values.ToList().ForEach(d => d.ForEach(da => da.SetFlag(Node.Flag.NodeIsIntrinsic, true)));
                    intrinsicTable.Types.Values.ToList().ForEach(d => d.ForEach(da => da.SetFlag(Node.Flag.NodeIsIntrinsic, true)));
                    intrinsicTable.Functions.Values.ToList().ForEach(d => d.ForEach(da => da.SetFlag(Node.Flag.NodeIsIntrinsic, true)));

                    TableOfIntrisic.CopyAllDataEntries(intrinsicTable.DataEntries.Values);
                    TableOfIntrisic.CopyAllTypes(intrinsicTable.Types);
                    TableOfIntrisic.CopyAllFunctions(intrinsicTable.Functions, AccessModifier.Public);

                    if (nameSpaceTable != null)
                    {
                        TableOfNamespaces = new SymbolTable(TableOfIntrisic, SymbolTable.Scope.Namespace); //Set TableOfNamespace with program if dependencies were given. (See CLI.cs runOnce2() LoadDependencies())
                        TableOfNamespaces.CopyAllPrograms(nameSpaceTable.Programs.Values);
                    }

                }
                // TODO#249: use a COPY for these
                foreach (var type in DataType.BuiltInCustomTypes)
                {
                    var createdType = DataType.CreateBuiltIn(type);
                    TableOfIntrisic.AddType(createdType); //Add default TypeCobol types BOOLEAN and DATE
                    //Add type and children to DataTypeEntries dictionary in Intrinsic symbol table
                    TableOfIntrisic.AddDataDefinitionsUnderType(createdType);
                }
            }
        }



        public NodeDispatcher Dispatcher { get; internal set; }

        public Node CurrentNode { get; private set; }

        public Dictionary<CodeElement, Node> NodeCodeElementLinkers = new Dictionary<CodeElement, Node>();

        /// <summary>
        /// The Last entered node.
        /// </summary>
        public Node LastEnteredNode
        {
            get;
            private set;
        }

        private void Enter(Node node, SymbolTable table = null)
        {
            node.SymbolTable = table ?? CurrentNode.SymbolTable;
            if (_ProcedureDeclaration != null)
            {
                node.SetFlag(Node.Flag.InsideProcedure, true);      //Set flag to know that this node belongs a Procedure or Function
            }

            CurrentNode.Add(node);
            CurrentNode = node;

            if (node.CodeElement != null)
                NodeCodeElementLinkers.Add(node.CodeElement, node);
        }

        private void Exit()
        {
            Dispatcher.OnNode(CurrentNode, CurrentProgram);
            LastEnteredNode = CurrentNode;
            CurrentNode = CurrentNode.Parent;
        }

        private void AttachEndIfExists(CodeElementEnd end)
        {
            if (end != null)
            {
                Enter(new End(end));
                Exit();
            }
        }
        
        /// <summary>Exit() every Node that is not the top-level item for a data of a given level.</summary>
        /// <param name="levelnumber">
        /// Level number of the next data definition that will be Enter()ed.
        /// If null, a value of 1 is assumed.
        /// </param>
        private void SetCurrentNodeToTopLevelItem(IntegerValue levelnumber)
        {
            long level = levelnumber?.Value ?? 1;

            if (level == 1 || level == 77)
            {
                ExitLastLevel1Definition();
            }
            else
            {
                // Exit() previous sibling and all of its last children
                while (true)
                {
                    var data = CurrentNode as DataDefinition;
                    if (data == null) break; //we reach a section (working-storage section, local-storage, ...)
                    if (data.CodeElement.LevelNumber.Value >= level)
                    {
                        Exit();
                    }
                    else
                    {
                        break;//The CurrentNode has a number < level
                    }

                }
            }

        }

        /// <summary>Exit last level-01 data definition entry, as long as all its subordinates.</summary>
        private void ExitLastLevel1Definition()
        {
            _CurrentTypeDefinition = null;
            while (CurrentNode is DataDefinition) Exit();
        }

        public virtual void StartCobolCompilationUnit()
        {
            if (TableOfNamespaces == null)
                TableOfNamespaces = new SymbolTable(TableOfIntrisic, SymbolTable.Scope.Namespace);

            TableOfGlobalStorage = new SymbolTable(TableOfNamespaces, SymbolTable.Scope.GlobalStorage);
            TableOfGlobals = new SymbolTable(TableOfGlobalStorage, SymbolTable.Scope.Global);
            Program = null;

            Root.SymbolTable = TableOfNamespaces; //Set SymbolTable of SourceFile Node, Limited to NameSpace and Intrinsic scopes
        }

        public virtual void StartCobolProgram(ProgramIdentification programIdentification, LibraryCopyCodeElement libraryCopy)
        {
            
            if (Program == null)
            {
                if (Root.MainProgram == null)
                {
                    Root.MainProgram = new SourceProgram(TableOfGlobals, programIdentification);
                    Program = Root.MainProgram;
                }
                else
                {
                    Program = new StackedProgram(TableOfGlobals, programIdentification);                    
                }
                
                programsStack = new Stack<Program>();
                CurrentProgram = Program;
                Enter(CurrentProgram, CurrentProgram.SymbolTable);
            }
            else
            {
                var enclosing = CurrentProgram;
                CurrentProgram = new NestedProgram(enclosing, programIdentification);
                Enter(CurrentProgram, CurrentProgram.SymbolTable);
            }

            if (libraryCopy != null)
            { // TCRFUN_LIBRARY_COPY
                var cnode = new LibraryCopy(libraryCopy);
                Enter(cnode, CurrentProgram.SymbolTable);
                Exit();
            }

            TableOfNamespaces.AddProgram(CurrentProgram); //Add Program to Namespace table. 
        }

        public virtual void EndCobolProgram(TypeCobol.Compiler.CodeElements.ProgramEnd end)
        {
            AttachEndIfExists(end);
            Exit();
            programsStack.Pop();

            if (programsStack.Count == 0) //Means that we ended a main program, reset Table and program in case of a new program declaration before EOF. 
            {
                TableOfGlobals = new SymbolTable(TableOfNamespaces, SymbolTable.Scope.Global);
                Program = null;
            }
        }

        public virtual void StartEnvironmentDivision(EnvironmentDivisionHeader header)
        {
            Enter(new EnvironmentDivision(header));
        }

        public virtual void EndEnvironmentDivision()
        {
            Exit();
        }

        public virtual void StartConfigurationSection(ConfigurationSectionHeader header)
        {
            Enter(new ConfigurationSection(header));
        }

        public virtual void EndConfigurationSection()
        {
            Exit();
        }

        public virtual void StartSourceComputerParagraph(SourceComputerParagraph paragraph)
        {
            Enter(new SourceComputer(paragraph));
        }

        public virtual void EndSourceComputerParagraph()
        {
            Exit();
        }

        public virtual void StartObjectComputerParagraph(ObjectComputerParagraph paragraph)
        {
            Enter(new ObjectComputer(paragraph));
        }

        public virtual void EndObjectComputerParagraph()
        {
            Exit();
        }

        public virtual void StartSpecialNamesParagraph(SpecialNamesParagraph paragraph)
        {
            Enter(new SpecialNames(paragraph));
        }

        public virtual void EndSpecialNamesParagraph()
        {
            Exit();
        }

        public virtual void StartRepositoryParagraph(RepositoryParagraph paragraph)
        {
            Enter(new Repository(paragraph));
        }

        public virtual void EndRepositoryParagraph()
        {
            Exit();
        }

        public virtual void StartInputOutputSection(InputOutputSectionHeader header)
        {
            Enter(new InputOutputSection(header));
        }

        public virtual void EndInputOutputSection()
        {
            Exit();
        }

        public virtual void StartFileControlParagraph(FileControlParagraphHeader header)
        {
            Enter(new FileControlParagraphHeaderNode(header));
        }

        public virtual void EndFileControlParagraph()
        {
            Exit();
        }

        public virtual void StartFileControlEntry(FileControlEntry entry)
        {
            var fileControlEntry = new FileControlEntryNode(entry);
            Enter(fileControlEntry);
        }

        public virtual void EndFileControlEntry()
        {
            Exit();
        }

        public virtual void StartDataDivision(DataDivisionHeader header)
        {
            Enter(new DataDivision(header));
        }

        public virtual void EndDataDivision()
        {
            Exit();
        }

        public virtual void StartFileSection(FileSectionHeader header)
        {
            Enter(new FileSection(header));
            currentSectionFlag = Node.Flag.FileSectionNode;
        }

        public virtual void EndFileSection()
        {
            ExitLastLevel1Definition();
            Exit();
            currentSectionFlag = 0;
        }

        public virtual void StartGlobalStorageSection(GlobalStorageSectionHeader header)
        {
            Enter(new GlobalStorageSection(header), CurrentNode.SymbolTable.GetTableFromScope(SymbolTable.Scope.GlobalStorage));
            currentSectionFlag = Node.Flag.GlobalStorageSection;
        }

        public virtual void EndGlobalStorageSection()
        {
            ExitLastLevel1Definition();
            Exit(); // Exit GlobalStorageSection
            currentSectionFlag = 0;
        }

        public virtual void StartFileDescriptionEntry(FileDescriptionEntry entry)
        {
            ExitLastLevel1Definition();
            Enter(new FileDescriptionEntryNode(entry));
        }

        public virtual void EndFileDescriptionEntry()
        {            
            Exit();
        }

        public virtual void EndFileDescriptionEntryIfAny()
        {
            if (this.CurrentNode is FileDescriptionEntryNode)
            {
                EndFileDescriptionEntry();
            }
        }

        public virtual void StartDataDescriptionEntry(DataDescriptionEntry entry)
        {
            var dataTypeDescriptionEntry = entry as DataTypeDescriptionEntry;
            if (dataTypeDescriptionEntry != null) StartTypeDefinitionEntry(dataTypeDescriptionEntry);
            else
            {
                SetCurrentNodeToTopLevelItem(entry.LevelNumber);

                var symbolTable = SyntaxTree.CurrentNode.SymbolTable;
                if (entry.IsGlobal)
                    symbolTable = symbolTable.GetTableFromScope(SymbolTable.Scope.Global);

                //Update DataType of CodeElement by searching info on the declared Type into SymbolTable.
                //Note that the AST is not complete here, but you can only refer to a Type that has previously been defined.
                var node = new DataDescription(entry);
                if (_CurrentTypeDefinition != null)
                    node.ParentTypeDefinition = _CurrentTypeDefinition;
                Enter(node, symbolTable);

                if (entry.Indexes != null && entry.Indexes.Length > 0)
                {
                    
                    foreach (var index in entry.Indexes)
                    {
                        var indexNode = new IndexDefinition(index);
                        Enter(indexNode, symbolTable);
                        if (_CurrentTypeDefinition != null)
                            indexNode.ParentTypeDefinition = _CurrentTypeDefinition;
                        symbolTable.AddVariable(indexNode);
                        Exit();
                    }
                }
                node.SymbolTable.AddVariable(node);
                CheckIfItsTyped(node, node.CodeElement);
            }

            CurrentNode.SetFlag(currentSectionFlag, true);
        }

        private void CheckIfItsTyped(DataDefinition dataDefinition, CommonDataDescriptionAndDataRedefines commonDataDescriptionAndDataRedefines)
        {
            //Is a type referenced
            if (commonDataDescriptionAndDataRedefines.UserDefinedDataType != null)
            {
                if (_CurrentTypeDefinition != null)
                {
                    _CurrentTypeDefinition.TypedChildren.Add(dataDefinition);
                }
                else
                {
                    TypedVariablesOutsideTypedef.Add(dataDefinition);
                }
            }

            //Special case for Depending On.
            //Depending on inside typedef can reference other variable declared in type referenced in this typedef
            //To resolve variable after "depending on" we first have to resolve all types used in this typedef.
            //This resolution must be recursive until all sub types have been resolved.
            if (commonDataDescriptionAndDataRedefines.OccursDependingOn != null)
            {
                if (_CurrentTypeDefinition != null && !typeAlreadyAddedToTypeToLink)
                {
                    TypeThatNeedTypeLinking.Add(_CurrentTypeDefinition);
                    typeAlreadyAddedToTypeToLink = true;
                }
            }
        }


        public virtual void StartDataRedefinesEntry(DataRedefinesEntry entry)
        {
            SetCurrentNodeToTopLevelItem(entry.LevelNumber);
            var symbolTable = CurrentNode.SymbolTable;
            if (entry.IsGlobal)
                symbolTable = symbolTable.GetTableFromScope(SymbolTable.Scope.Global);

            var node = new DataRedefines(entry);
            if (_CurrentTypeDefinition != null)
                node.ParentTypeDefinition = _CurrentTypeDefinition;
            Enter(node, symbolTable);
            node.SymbolTable.AddVariable(node);

            CheckIfItsTyped(node, node.CodeElement);
        }

        public virtual void StartDataRenamesEntry(DataRenamesEntry entry)
        {
            SetCurrentNodeToTopLevelItem(entry.LevelNumber);
            var node = new DataRenames(entry);
            if (_CurrentTypeDefinition != null)
                node.ParentTypeDefinition = _CurrentTypeDefinition;
            Enter(node);
            node.SymbolTable.AddVariable(node);
        }

        public virtual void StartDataConditionEntry(DataConditionEntry entry)
        {
            SetCurrentNodeToTopLevelItem(entry.LevelNumber);
            var node = new DataCondition(entry);
            if (_CurrentTypeDefinition != null)
                node.ParentTypeDefinition = _CurrentTypeDefinition;
            Enter(node);
                node.SymbolTable.AddVariable(node);
        }

        public virtual void StartTypeDefinitionEntry(DataTypeDescriptionEntry typedef)
        {
            SetCurrentNodeToTopLevelItem(typedef.LevelNumber);

            // TCTYPE_GLOBAL_TYPEDEF
            var symbolTable = SyntaxTree.CurrentNode.SymbolTable.GetTableFromScope(typedef.IsGlobal ? SymbolTable.Scope.Global : SymbolTable.Scope.Declarations);

            var node = new TypeDefinition(typedef);
            Enter(node, symbolTable);

            symbolTable.AddType(node);

            _CurrentTypeDefinition = node;
            CheckIfItsTyped(node, node.CodeElement);
        }

        public virtual void StartWorkingStorageSection(WorkingStorageSectionHeader header)
        {
            Enter(new WorkingStorageSection(header));
            currentSectionFlag = Node.Flag.WorkingSectionNode;
            if (_ProcedureDeclaration != null)
            {
                CurrentNode.SetFlag(Node.Flag.ForceGetGeneratedLines, true);
            }
        }

        public virtual void EndWorkingStorageSection()
        {
            ExitLastLevel1Definition();
            Exit(); // Exit WorkingStorageSection
            currentSectionFlag = 0;
        }

        public virtual void StartLocalStorageSection(LocalStorageSectionHeader header)
        {
            Enter(new LocalStorageSection(header));
            currentSectionFlag = Node.Flag.LocalStorageSectionNode;
            if (_ProcedureDeclaration != null)
            {
                CurrentNode.SetFlag(Node.Flag.ForceGetGeneratedLines, true);
            }
        }

        public virtual void EndLocalStorageSection()
        {
            ExitLastLevel1Definition();
            Exit(); // Exit LocalStorageSection
            currentSectionFlag = 0;
        }

        public virtual void StartLinkageSection(LinkageSectionHeader header)
        {
            Enter(new LinkageSection(header));
            currentSectionFlag = Node.Flag.LinkageSectionNode;
            if (_ProcedureDeclaration != null)
            {
                CurrentNode.SetFlag(Node.Flag.ForceGetGeneratedLines, true);
            }
        }

        public virtual void EndLinkageSection()
        {
            ExitLastLevel1Definition();
            Exit(); // Exit LinkageSection
            currentSectionFlag = 0;
        }

        public virtual void StartProcedureDivision(ProcedureDivisionHeader header)
        {
            Enter(new ProcedureDivision(header));
            if (_ProcedureDeclaration != null)
            {
                CurrentNode.SetFlag(Node.Flag.ForceGetGeneratedLines, true);
            }
        }

        public virtual void EndProcedureDivision()
        {
            Exit();
        }

        public virtual void StartDeclaratives(DeclarativesHeader header)
        {
            Enter(new Declaratives(header));
        }

        public virtual void EndDeclaratives(DeclarativesEnd end)
        {
            AttachEndIfExists(end);
            Exit();
        }

        public virtual void EnterUseStatement(UseStatement useStatement)
        {
            Enter(new Use(useStatement));
            Exit();
        }

        private Tools.UIDStore uidfactory = new Tools.UIDStore();
        private TypeDefinition _currentTypeDefinition;

        public virtual void StartFunctionDeclaration(FunctionDeclarationHeader header)
        {
            header.SetLibrary(CurrentProgram.Identification.ProgramName.Name);
            var node = new FunctionDeclaration(header)
            {
                Label = uidfactory.FromOriginal(header?.FunctionName.Name),
                Library = CurrentProgram.Identification.ProgramName.Name
            };
            _ProcedureDeclaration = node;
            CurrentProgram.Root.SetFlag(Node.Flag.ContainsProcedure, true);
            //DO NOT change this without checking all references of Library. 
            // (SymbolTable - function, type finding could be impacted) 

            //Function must be added to Declarations scope
            var declarationSymbolTable = CurrentNode.SymbolTable.GetTableFromScope(SymbolTable.Scope.Declarations);
            declarationSymbolTable.AddFunction(node);
            Enter(node, new SymbolTable(declarationSymbolTable, SymbolTable.Scope.Function));

            var declaration = node.CodeElement;
            var funcProfile = node.Profile; //Get functionprofile to set parameters

            foreach (var parameter in declaration.Profile.InputParameters) //Set Input Parameters
            {
                var paramNode = new ParameterDescription(parameter);
                paramNode.SymbolTable = CurrentNode.SymbolTable;
                paramNode.SetFlag(Node.Flag.LinkageSectionNode, true);
                paramNode.PassingType = ParameterDescription.PassingTypes.Input;
                funcProfile.InputParameters.Add(paramNode);

                paramNode.SetParent(CurrentNode);
                CurrentNode.SymbolTable.AddVariable(paramNode);
                CheckIfItsTyped(paramNode, paramNode.CodeElement);
            }
            foreach (var parameter in declaration.Profile.OutputParameters) //Set Output Parameters
            {
                var paramNode = new ParameterDescription(parameter);
                paramNode.SymbolTable = CurrentNode.SymbolTable;
                paramNode.SetFlag(Node.Flag.LinkageSectionNode, true);
                paramNode.PassingType = ParameterDescription.PassingTypes.Output;
                funcProfile.OutputParameters.Add(paramNode);

                paramNode.SetParent(CurrentNode);
                CurrentNode.SymbolTable.AddVariable(paramNode);
                CheckIfItsTyped(paramNode, paramNode.CodeElement);
            }
            foreach (var parameter in declaration.Profile.InoutParameters) //Set Inout Parameters
            {
                var paramNode = new ParameterDescription(parameter);
                paramNode.SymbolTable = CurrentNode.SymbolTable;
                paramNode.SetFlag(Node.Flag.LinkageSectionNode, true);
                paramNode.PassingType = ParameterDescription.PassingTypes.InOut;
                funcProfile.InoutParameters.Add(paramNode);

                paramNode.SetParent(CurrentNode);
                CurrentNode.SymbolTable.AddVariable(paramNode);
                CheckIfItsTyped(paramNode, paramNode.CodeElement);
            }

            if (declaration.Profile.ReturningParameter != null) //Set Returning Parameters
            {
                var paramNode = new ParameterDescription(declaration.Profile.ReturningParameter);
                paramNode.SymbolTable = CurrentNode.SymbolTable;
                paramNode.SetFlag(Node.Flag.LinkageSectionNode, true);
                node.Profile.ReturningParameter = paramNode;

                paramNode.SetParent(CurrentNode);
                CurrentNode.SymbolTable.AddVariable(paramNode);
                CheckIfItsTyped(paramNode, paramNode.CodeElement);
            }
        }

        public virtual void EndFunctionDeclaration(FunctionDeclarationEnd end)
        {
            Enter(new FunctionEnd(end));
            Exit();
            Exit();// exit DECLARE FUNCTION
            _ProcedureDeclaration = null;
        }

        public virtual void StartFunctionProcedureDivision(ProcedureDivisionHeader header)
        {
            if (header.UsingParameters != null && header.UsingParameters.Count > 0)
                DiagnosticUtils.AddError(header, "TCRFUN_DECLARATION_NO_USING");//TODO#249

            Enter(new ProcedureDivision(header));
        }

        public virtual void EndFunctionProcedureDivision()
        {
            Exit();
        }

        public virtual void StartSection(SectionHeader header)
        {
            var section = new Section(header);
            Enter(section);
            section.SymbolTable.AddSection(section);
        }

        public virtual void EndSection()
        {
            Exit();
        }

        public virtual void StartParagraph(ParagraphHeader header)
        {
            var paragraph = new Paragraph(header);
            Enter(paragraph);
            paragraph.SymbolTable.AddParagraph(paragraph);
        }

        public virtual void EndParagraph()
        {
            Exit();
        }

        public virtual void StartSentence()
        {
            Enter(new Sentence());
        }

        public virtual void EndSentence(SentenceEnd end, bool bCheck)
        {
            //if (bCheck)
            //{
            //    if (CurrentNode is ProcedureDivision || CurrentNode is Paragraph)
            //        return;
            //    else
            //        return;
            //}
            AttachEndIfExists(end);
            if (CurrentNode is Sentence) Exit();//TODO remove this and check what happens when exiting last CALL in FIN-STANDARD in BigBatch file (ie. CheckPerformance test)
        }

        /// <summary>
        /// Checks if the last statement being entered must leads to the start of a sentence.
        /// </summary>
        public virtual void CheckStartSentenceLastStatement()
        {
            if (LastEnteredNode != null)
            {
                Node parent = LastEnteredNode.Parent;
                if (parent is Paragraph || parent is ProcedureDivision)
                {   //JCM Hack: So in this case the statement must be given as parent a Sentence Node.
                    //This hack is perform because using the Enter/Exit mechanism to create a Syntax Tree
                    //Is not appropriate with LALR(1) grammar, because there is not start token that can telle
                    //The beginning of  list of statement that belongs to a sentence.
                    //So we check if the statement is part of a paragraph or a procedure division.
                    parent.Remove(LastEnteredNode);
                    //Start a sentence
                    StartSentence();
                    CurrentNode.Add(LastEnteredNode);                    
                }
            }
        }

        public virtual void StartExecStatement(ExecStatement execStmt)
        {
            ExitLastLevel1Definition();
            Enter(new Exec(execStmt));
        }

        public virtual void EndExecStatement()
        {
            //Code duplicated in OnExecStatement
            //EndExecStatement (therefore StartExecStatement) is fired if the exec is in a procedure division and is the first instruction
            //OnExecStatement is fired if the exec is in a procedure division and is not the first instruction

            //Code to generate a specific ProcedureDeclaration as Nested when an Exec Statement is spotted. See Issue #1209
            //This might be helpful for later
            //if (_ProcedureDeclaration != null)
            //{
            //    _ProcedureDeclaration.SetFlag(Node.Flag.GenerateAsNested, true);
            //}

            //Code to generate all ProcedureDeclarations as Nested when an Exec Statement is spotted. See Issue #1209
            //This is the selected solution until we determine the more optimal way to generate a program that contains Exec Statements
            if (_ProcedureDeclaration != null)
            {
                CurrentNode.Root.MainProgram.SetFlag(Node.Flag.GenerateAsNested, true);
            }
            Exit();
        }

        public virtual void OnContinueStatement(ContinueStatement stmt)
        {
            Enter(new Continue(stmt));
            Exit();
        }

        public virtual void OnEntryStatement(EntryStatement stmt)
        {
            Enter(new Entry(stmt));
            Exit();
        }

        public virtual void OnAcceptStatement(AcceptStatement stmt)
        {
            Enter(new Accept(stmt));
            Exit();
        }

        public virtual void OnInitializeStatement(InitializeStatement stmt)
        {
            Enter(new Initialize(stmt));
            Exit();
        }

        public virtual void OnInspectStatement(InspectStatement stmt)
        {
            Enter(new Inspect(stmt));
            Exit();
        }

        public virtual void OnMoveStatement(MoveStatement stmt)
        {
            Enter(new Move(stmt));
            Exit();
        }

        public virtual void OnSetStatement(SetStatement stmt)
        {
            Enter(new Set(stmt));
            Exit();
        }

        public virtual void OnStopStatement(StopStatement stmt)
        {
            Enter(new Stop(stmt));
            Exit();
        }

        public virtual void OnExitMethodStatement(ExitMethodStatement stmt)
        {
            Enter(new ExitMethod(stmt));
            Exit();
        }

        public virtual void OnExitProgramStatement(ExitProgramStatement stmt)
        {
            Enter(new ExitProgram(stmt));
            Exit();
        }

        public virtual void OnGobackStatement(GobackStatement stmt)
        {
            Enter(new Goback(stmt));
            Exit();
        }

        public virtual void OnCloseStatement(CloseStatement stmt)
        {
            Enter(new Close(stmt));
            Exit();
        }

        public virtual void OnDisplayStatement(DisplayStatement stmt)
        {
            Enter(new Display(stmt));
            Exit();
        }

        public virtual void OnOpenStatement(OpenStatement stmt)
        {
            Enter(new Open(stmt));
            Exit();
        }

        public virtual void OnMergeStatement(MergeStatement stmt)
        {
            Enter(new Merge(stmt));
            Exit();
        }

        public virtual void OnReleaseStatement(ReleaseStatement stmt)
        {
            Enter(new Release(stmt));
            Exit();
        }

        public virtual void OnSortStatement(SortStatement stmt)
        {
            Enter(new Sort(stmt));
            Exit();
        }

        public virtual void OnAlterStatement(AlterStatement stmt)
        {
            Enter(new Alter(stmt));
            Exit();
        }

        public void OnExitStatement(ExitStatement stmt)
        {
            Enter(new Exit(stmt));
            Exit();
        }

        public virtual void OnGotoStatement(GotoStatement stmt)
        {
            Enter(new Goto(stmt));
            Exit();
        }

        public virtual void OnPerformProcedureStatement(PerformProcedureStatement stmt)
        {
            Enter(new PerformProcedure(stmt));
            Exit();
        }

        public virtual void OnCancelStatement(CancelStatement stmt)
        {
            Enter(new Cancel(stmt));
            Exit();
        }

        public virtual void OnProcedureStyleCall(ProcedureStyleCallStatement stmt, CallStatementEnd end)
        {
            Enter(new ProcedureStyleCall(stmt));            
            AttachEndIfExists(end);
            Exit();
        }

        public virtual void OnExecStatement(ExecStatement stmt)
        {
            Enter(new Exec(stmt));

            //Code duplicated in OnExecStatement
            //EndExecStatement (therefore StartExecStatement) is fired if the exec is in a procedure division and is the first instruction
            //OnExecStatement is fired if the exec is in a procedure division and is not the first instruction

            //Code to generate a specific ProcedureDeclaration as Nested when an Exec Statement is spotted. See Issue #1209
            //This might be helpful for later
            //if (_ProcedureDeclaration != null)
            //{
            //    _ProcedureDeclaration.SetFlag(Node.Flag.GenerateAsNested, true);
            //}

            //Code to generate all ProcedureDeclarations as Nested when an Exec Statement is spotted. See Issue #1209
            //This is the selected solution until we determine the more optimal way to generate a program that contains Exec Statements
            if (_ProcedureDeclaration != null)
            {
                CurrentNode.Root.MainProgram.SetFlag(Node.Flag.GenerateAsNested, true);
            }
            Exit();
        }

        public virtual void StartAddStatementConditional(TypeCobol.Compiler.CodeElements.AddStatement stmt)
        {
            Enter(new Add(stmt));
        }

        public virtual void EndAddStatementConditional(TypeCobol.Compiler.CodeElements.AddStatementEnd end)
        {
            AttachEndIfExists(end);
            Exit();
        }

        public virtual void StartCallStatementConditional(TypeCobol.Compiler.CodeElements.CallStatement stmt)
        {
            Enter(new Call(stmt));
        }

        public virtual void EndCallStatementConditional(TypeCobol.Compiler.CodeElements.CallStatementEnd end)
        {
            AttachEndIfExists(end);
            Exit();
        }

        public virtual void StartComputeStatementConditional(TypeCobol.Compiler.CodeElements.ComputeStatement stmt)
        {
            Enter(new Compute(stmt));
        }

        public virtual void EndComputeStatementConditional(TypeCobol.Compiler.CodeElements.ComputeStatementEnd end)
        {
            AttachEndIfExists(end);
            Exit();
        }

        public virtual void StartOnSizeError(TypeCobol.Compiler.CodeElements.OnSizeErrorCondition cond)
        {
            Enter(new OnSizeError(cond));
        }

        public virtual void EndOnSizeError()
        {
            Exit();
        }

        public virtual void StartNoSizeError(TypeCobol.Compiler.CodeElements.NotOnSizeErrorCondition cond)
        {
            Enter(new NoSizeError(cond));
        }

        public virtual void EndNoSizeError()
        {
            Exit();
        }

        public virtual void StartOnException(TypeCobol.Compiler.CodeElements.OnExceptionCondition cond)
        {
            Enter(new OnException(cond));
        }

        public virtual void EndOnException()
        {
            Exit();
        }

        public virtual void StartNoException(TypeCobol.Compiler.CodeElements.NotOnExceptionCondition cond)
        {
            Enter(new NoException(cond));
        }

        public virtual void EndNoException()
        {
            Exit();
        }

        public virtual void StartOnOverflow(TypeCobol.Compiler.CodeElements.OnOverflowCondition cond)
        {
            Enter(new OnOverflow(cond));
        }

        public virtual void EndOnOverflow()
        {
            Exit();
        }

        public virtual void StartNoOverflow(TypeCobol.Compiler.CodeElements.NotOnOverflowCondition cond)
        {
            Enter(new NoOverflow(cond));
        }

        public virtual void EndNoOverflow()
        {
            Exit();
        }

        public virtual void StartOnInvalidKey(TypeCobol.Compiler.CodeElements.InvalidKeyCondition cond)
        {
            Enter(new OnInvalidKey(cond));
        }

        public virtual void EndOnInvalidKey()
        {
            Exit();
        }

        public virtual void StartNoInvalidKey(TypeCobol.Compiler.CodeElements.NotInvalidKeyCondition cond)
        {
            Enter(new NoInvalidKey(cond));
        }

        public virtual void EndNoInvalidKey()
        {
            Exit();
        }

        public virtual void StartOnAtEnd(TypeCobol.Compiler.CodeElements.AtEndCondition cond)
        {
            Enter(new OnAtEnd(cond));
        }

        public virtual void EndOnAtEnd()
        {
            Exit();
        }

        public virtual void StartNoAtEnd(TypeCobol.Compiler.CodeElements.NotAtEndCondition cond)
        {
            Enter(new NoAtEnd(cond));
        }

        public virtual void EndNoAtEnd()
        {
            Exit();
        }

        public virtual void StartDeleteStatementConditional(TypeCobol.Compiler.CodeElements.DeleteStatement stmt)
        {
            Enter(new Delete(stmt));
        }

        public virtual void EndDeleteStatementConditional(TypeCobol.Compiler.CodeElements.DeleteStatementEnd end)
        {
            AttachEndIfExists(end);
            Exit();
        }

        public virtual void StartDivideStatementConditional(TypeCobol.Compiler.CodeElements.DivideStatement stmt)
        {
            Enter(new Divide(stmt));
        }

        public virtual void EndDivideStatementConditional(TypeCobol.Compiler.CodeElements.DivideStatementEnd end)
        {
            AttachEndIfExists(end);
            Exit();
        }

        public virtual void StartEvaluateStatementWithBody(TypeCobol.Compiler.CodeElements.EvaluateStatement stmt)
        {
            Enter(new Evaluate(stmt));// enter EVALUATE
        }

        public virtual void EndEvaluateStatementWithBody(TypeCobol.Compiler.CodeElements.EvaluateStatementEnd end)
        {
            AttachEndIfExists(end);// exit EVALUATE
            Exit();
        }

        public virtual void StartWhenConditionClause(List<TypeCobol.Compiler.CodeElements.CodeElement> conditions)
        {
            Enter(new WhenGroup());// enter WHEN group
            foreach(var cond in conditions)
            {
                TypeCobol.Compiler.CodeElements.WhenCondition condition = null;
                if (cond is TypeCobol.Compiler.CodeElements.WhenSearchCondition)
                {
                    TypeCobol.Compiler.CodeElements.WhenSearchCondition whensearch =
                        cond as TypeCobol.Compiler.CodeElements.WhenSearchCondition;
                    condition = new WhenCondition();
                    whensearch.ApplyPropertiesToCE(condition);

                    condition.SelectionObjects = new EvaluateSelectionObject[1];
                    condition.SelectionObjects[0] = new EvaluateSelectionObject();
                    condition.SelectionObjects[0].BooleanComparisonVariable = new BooleanValueOrExpression(whensearch.Condition);

                    var conditionNameConditionOrSwitchStatusCondition = whensearch.Condition as ConditionNameConditionOrSwitchStatusCondition;
                    if (conditionNameConditionOrSwitchStatusCondition != null)
                        condition.StorageAreaReads = new List<StorageArea>
                        {
                            conditionNameConditionOrSwitchStatusCondition.ConditionReference
                        };
                }
                else
                {
                    condition = cond as TypeCobol.Compiler.CodeElements.WhenCondition;
                }
                Enter(new When(condition));
                Exit();
            }
            Exit();// exit WHEN group
            Enter(new Then());// enter THEN
        }


        public virtual void EndWhenConditionClause()
        {
            Exit();// exit THEN
        }

        public virtual void StartWhenOtherClause(TypeCobol.Compiler.CodeElements.WhenOtherCondition cond)
        {
            Enter(new WhenOther(cond));// enter WHEN OTHER
        }

        public virtual void EndWhenOtherClause()
        {
            Exit();// exit WHEN OTHER
        }

        public virtual void StartIfStatementWithBody(TypeCobol.Compiler.CodeElements.IfStatement stmt)
        {
            Enter(new If(stmt));
            Enter(new Then());
        }
        public virtual void EnterElseClause(TypeCobol.Compiler.CodeElements.ElseCondition clause)
        {
            Exit();// we want ELSE to be child of IF, not THEN, so exit THEN
            Enter(new Else(clause));// ELSE
        }
        public virtual void EndIfStatementWithBody(TypeCobol.Compiler.CodeElements.IfStatementEnd end)
        {
            Exit(); // Exit ELSE (if any) or THEN
            AttachEndIfExists(end);
            // DO NOT Exit() IF node because this will be done in ExitStatement
            Exit();//JCM exit any way ???
        }

        public virtual void AddNextSentenceStatement(TypeCobol.Compiler.CodeElements.NextSentenceStatement stmt)
        {
            Enter(new NextSentence(stmt));
            Exit();
        }

        public virtual void StartInvokeStatementConditional(TypeCobol.Compiler.CodeElements.InvokeStatement stmt)
        {
            Enter(new Invoke(stmt));
        }

        public virtual void EndInvokeStatementConditional(TypeCobol.Compiler.CodeElements.InvokeStatementEnd end)
        {
            AttachEndIfExists(end);
            Exit();
        }

        public virtual void StartMultiplyStatementConditional(TypeCobol.Compiler.CodeElements.MultiplyStatement stmt)
        {
            Enter(new Multiply(stmt));
        }

        public virtual void EndMultiplyStatementConditional(TypeCobol.Compiler.CodeElements.MultiplyStatementEnd end)
        {
            AttachEndIfExists(end);
            Exit();
        }

        public virtual void StartPerformStatementWithBody(TypeCobol.Compiler.CodeElements.PerformStatement stmt)
        {
            Enter(new Perform(stmt));
        }

        public virtual void EndPerformStatementWithBody(TypeCobol.Compiler.CodeElements.PerformStatementEnd end)
        {
            AttachEndIfExists(end);
            Exit();
        }

        public virtual void StartSearchStatementWithBody([NotNull] TypeCobol.Compiler.CodeElements.SearchStatement stmt)
        {
            Enter(new Search(stmt));
        }

        public virtual void EndSearchStatementWithBody(TypeCobol.Compiler.CodeElements.SearchStatementEnd end)
        {
            AttachEndIfExists(end);
            Exit();
        }

        public virtual void StartWhenSearchConditionClause(TypeCobol.Compiler.CodeElements.WhenSearchCondition condition)
        {
            Enter(new WhenSearch(condition));
        }

        public virtual void EndWhenSearchConditionClause()
        {
            Exit(); // WHEN
        }

        public virtual void EnterReadStatementConditional(TypeCobol.Compiler.CodeElements.ReadStatement stmt)
        {
            Enter(new Read(stmt));
        }

        public virtual void EndReadStatementConditional(TypeCobol.Compiler.CodeElements.ReadStatementEnd end)
        {
            AttachEndIfExists(end);
            Exit();
        }

        public virtual void EnterReturnStatementConditional(TypeCobol.Compiler.CodeElements.ReturnStatement stmt)
        {
            Enter(new Return(stmt));
        }

        public virtual void EndReturnStatementConditional(TypeCobol.Compiler.CodeElements.ReturnStatementEnd end)
        {
            AttachEndIfExists(end);
            Exit();
        }

        public virtual void StartRewriteStatementConditional(TypeCobol.Compiler.CodeElements.RewriteStatement stmt)
        {
            Enter(new Rewrite(stmt));
        }

        public virtual void EndRewriteStatementConditional(TypeCobol.Compiler.CodeElements.RewriteStatementEnd end)
        {
            AttachEndIfExists(end);
            Exit();
        }

        public virtual void StartStartStatementConditional(TypeCobol.Compiler.CodeElements.StartStatement stmt)
        {
            Enter(new Start(stmt));
        }

        public virtual void EndStartStatementConditional(TypeCobol.Compiler.CodeElements.StartStatementEnd end)
        {
            AttachEndIfExists(end);
            Exit();
        }

        public virtual void StartStringStatementConditional([NotNull] TypeCobol.Compiler.CodeElements.StringStatement stmt)
        {
            Enter(new Nodes.String(stmt));
        }

        public virtual void EndStringStatementConditional(TypeCobol.Compiler.CodeElements.StringStatementEnd end)
        {
            AttachEndIfExists(end);
            Exit();
        }

        public virtual void StartSubtractStatementConditional(TypeCobol.Compiler.CodeElements.SubtractStatement stmt)
        {
            Enter(new Subtract(stmt));
        }

        public virtual void EndSubtractStatementConditional(TypeCobol.Compiler.CodeElements.SubtractStatementEnd end)
        {
            AttachEndIfExists(end);
            Exit();
        }

        public virtual void StartUnstringStatementConditional(TypeCobol.Compiler.CodeElements.UnstringStatement stmt)
        {
            Enter(new Unstring(stmt));
        }

        public virtual void EndUnstringStatementConditional(TypeCobol.Compiler.CodeElements.UnstringStatementEnd end)
        {
            AttachEndIfExists(end);
            Exit();
        }

        public virtual void StartWriteStatementConditional(TypeCobol.Compiler.CodeElements.WriteStatement stmt)
        {
            Enter(new Write(stmt));
        }

        public virtual void EndWriteStatementConditional(TypeCobol.Compiler.CodeElements.WriteStatementEnd end)
        {
            AttachEndIfExists(end);
            Exit();
        }

        public virtual void StartXmlGenerateStatementConditional([NotNull] TypeCobol.Compiler.CodeElements.XmlGenerateStatement stmt)
        {
            Enter(new XmlGenerate(stmt));
        }

        public virtual void EndXmlGenerateStatementConditional(TypeCobol.Compiler.CodeElements.XmlStatementEnd end)
        {
            AttachEndIfExists(end);
            Exit();
        }

        public virtual void StartXmlParseStatementConditional([NotNull] TypeCobol.Compiler.CodeElements.XmlParseStatement stmt)
        {
            Enter(new XmlParse(stmt));
        }

        public virtual void EndXmlParseStatementConditional(TypeCobol.Compiler.CodeElements.XmlStatementEnd end)
        {
            AttachEndIfExists(end);
            Exit();
        }

    }
}
