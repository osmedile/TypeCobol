﻿namespace TypeCobol.Compiler.Nodes {
    using System;
    using System.Collections.Generic;
    using TypeCobol.Compiler.CodeElements;
    using TypeCobol.Compiler.CodeElements.Expressions;



    public interface Statement { }



    public class Accept: GenericNode<AcceptStatement>, CodeElementHolder<AcceptStatement>, Statement {
	    public Accept(AcceptStatement statement): base(statement) { }

        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return astVisitor.Visit(this);
        }
    }

    public class Alter: GenericNode<AlterStatement>, CodeElementHolder<AlterStatement>, Statement {
	    public Alter(AlterStatement statement): base(statement) { }
        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return astVisitor.Visit(this);
        }
    }

    public class Call: GenericNode<CallStatement>, CodeElementHolder<CallStatement>, Statement {
	    public Call(CallStatement statement): base(statement) { }
        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return astVisitor.Visit(this);
        }
    }
    public class ProcedureStyleCall : GenericNode<ProcedureStyleCallStatement>, CodeElementHolder<ProcedureStyleCallStatement>, Statement, FunctionCaller {
        public ProcedureStyleCall(ProcedureStyleCallStatement statement) : base(statement) { }

        public FunctionCall FunctionCall
        {
            get { return ((ProcedureStyleCallStatement) CodeElement).ProcedureCall; }
        }

        public FunctionDeclaration FunctionDeclaration {get; set;}

        /// <summary>
        /// True if this Procedure call in case of External call is not performed by COBOL EXTERNAL POINTER,
        /// false otherwise.
        /// </summary>
        public bool IsNotByExternalPointer
        {
            get;
            set;
        }

        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return astVisitor.Visit(this);
        }
    }

    public class Cancel: GenericNode<CancelStatement>, CodeElementHolder<CancelStatement>, Statement {
	    public Cancel(CancelStatement statement): base(statement) { }
        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return astVisitor.Visit(this);
        }
    }

    public class Continue: GenericNode<ContinueStatement>, CodeElementHolder<ContinueStatement>, Statement {
	    public Continue(ContinueStatement statement): base(statement) { }
        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return astVisitor.Visit(this);
        }
    }

    public class Delete: GenericNode<DeleteStatement>, CodeElementHolder<DeleteStatement>, Statement {
	    public Delete(DeleteStatement statement): base(statement) { }
        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return astVisitor.Visit(this);
        }
    }

    public class Display: GenericNode<DisplayStatement>, CodeElementHolder<DisplayStatement>, Statement {
	    public Display(DisplayStatement statement): base(statement) { }
        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return astVisitor.Visit(this);
        }
    }

    public class Entry: GenericNode<EntryStatement>, CodeElementHolder<EntryStatement>, Statement {
	    public Entry(EntryStatement statement): base(statement) { }
        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return astVisitor.Visit(this);
        }
    }

    public class Exec: GenericNode<ExecStatement>, CodeElementHolder<ExecStatement>, Statement {
	    public Exec(ExecStatement statement): base(statement) { }
        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return astVisitor.Visit(this);
        }
    }

    public class Exit: GenericNode<ExitStatement>, CodeElementHolder<ExitStatement>, Statement {
	    public Exit(ExitStatement statement): base(statement) { }
        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return astVisitor.Visit(this);
        }
    }

    public class ExitMethod: GenericNode<ExitMethodStatement>, CodeElementHolder<ExitMethodStatement>, Statement {
	    public ExitMethod(ExitMethodStatement statement): base(statement) { }
        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return astVisitor.Visit(this);
        }
    }

    public class ExitProgram: GenericNode<ExitProgramStatement>, CodeElementHolder<ExitProgramStatement>, Statement {
	    public ExitProgram(ExitProgramStatement statement): base(statement) { }
        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return astVisitor.Visit(this);
        }
    }

    public class Goback: GenericNode<GobackStatement>, CodeElementHolder<GobackStatement>, Statement {
	    public Goback(GobackStatement statement): base(statement) { }
        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return astVisitor.Visit(this);
        }
    }

    public class Goto: GenericNode<GotoStatement>, CodeElementHolder<GotoStatement>, Statement {
	    public Goto(GotoStatement statement): base(statement) { }
        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return astVisitor.Visit(this);
        }
    }

    public class Initialize: GenericNode<InitializeStatement>, CodeElementHolder<InitializeStatement>, Statement {
	    public Initialize(InitializeStatement statement): base(statement) { }
        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return astVisitor.Visit(this);
        }
    }

    public class Inspect: GenericNode<InspectStatement>, CodeElementHolder<InspectStatement>, Statement, VariableWriter {
	    public Inspect(InspectStatement statement): base(statement) { }
	    public IDictionary<StorageArea,object> VariablesWritten { get { return this.CodeElement().VariablesWritten; } }
	    public bool IsUnsafe { get { return this.CodeElement().IsUnsafe; } }
        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return astVisitor.Visit(this) && astVisitor.VisitVariableWriter(this);
        }
    }

    public class Invoke: GenericNode<InvokeStatement>, CodeElementHolder<InvokeStatement>, Statement {
	    public Invoke(InvokeStatement statement): base(statement) { }
        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return astVisitor.Visit(this);
        }
    }

    public class Merge: GenericNode<MergeStatement>, CodeElementHolder<MergeStatement>, Statement {
	    public Merge(MergeStatement statement): base(statement) { }
        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return astVisitor.Visit(this);
        }
    }

    public class Move: GenericNode<MoveStatement>, CodeElementHolder<MoveStatement>, Statement, VariableWriter,FunctionCaller {
	    public Move(MoveStatement statement): base(statement) { }
	    public FunctionCall FunctionCall { get { return this.CodeElement().FunctionCall; } }
	   
	    public IDictionary<StorageArea, object> VariablesWritten { get { return this.CodeElement().VariablesWritten; } }
	    public bool IsUnsafe { get { return this.CodeElement().IsUnsafe; } }

        public FunctionDeclaration FunctionDeclaration { get; set; }

        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return astVisitor.Visit(this) && astVisitor.VisitVariableWriter(this);
        }
    }

    public class Release: GenericNode<ReleaseStatement>, CodeElementHolder<ReleaseStatement>, Statement {
	    public Release(ReleaseStatement statement): base(statement) { }
        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return astVisitor.Visit(this);
        }
    }

    public class Return: GenericNode<ReturnStatement>, CodeElementHolder<ReturnStatement>, Statement {
	    public Return(ReturnStatement statement): base(statement) { }
        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return astVisitor.Visit(this);
        }
    }

    public class Set: GenericNode<SetStatement>, CodeElementHolder<SetStatement>, Statement, VariableWriter {
	    public Set(SetStatement statement): base(statement) { }
	    public IDictionary<StorageArea, object> VariablesWritten { get { return this.CodeElement().VariablesWritten; } }
	    public bool IsUnsafe { get { return this.CodeElement().IsUnsafe; } }
        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return astVisitor.Visit(this) && astVisitor.VisitVariableWriter(this);
        }
    }

    public class Sort: GenericNode<SortStatement>, CodeElementHolder<SortStatement>, Statement {
	    public Sort(SortStatement statement): base(statement) { }
        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return astVisitor.Visit(this);
        }
    }

    public class Start: GenericNode<StartStatement>, CodeElementHolder<StartStatement>, Statement {
	    public Start(StartStatement statement): base(statement) { }
        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return astVisitor.Visit(this);
        }
    }

    public class Stop: GenericNode<StopStatement>, CodeElementHolder<StopStatement>, Statement {
	    public Stop(StopStatement statement): base(statement) { }
        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return astVisitor.Visit(this);
        }
    }

    public class String: GenericNode<StringStatement>, CodeElementHolder<StringStatement>, Statement, VariableWriter {
	    public String(StringStatement statement): base(statement) { }
	    public IDictionary<StorageArea,object> VariablesWritten { get { return this.CodeElement().VariablesWritten; } }
	    public bool IsUnsafe { get { return this.CodeElement().IsUnsafe; } }
        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return astVisitor.Visit(this) && astVisitor.VisitVariableWriter(this);
        }
    }

    public class Unstring: GenericNode<UnstringStatement>, CodeElementHolder<UnstringStatement>, Statement, VariableWriter {
	    public Unstring(UnstringStatement statement): base(statement) { }
	    public IDictionary<StorageArea,object> VariablesWritten { get { return this.CodeElement().VariablesWritten; } }
	    public bool IsUnsafe { get { return this.CodeElement().IsUnsafe; } }
        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return astVisitor.Visit(this) && astVisitor.VisitVariableWriter(this);
        }
    }

    public class XmlGenerate: GenericNode<XmlGenerateStatement>, CodeElementHolder<XmlGenerateStatement>, Statement {
	    public XmlGenerate(XmlGenerateStatement statement): base(statement) { }
        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return astVisitor.Visit(this);
        }
    }

    public class XmlParse: GenericNode<XmlParseStatement>, CodeElementHolder<XmlParseStatement>, Statement {
	    public XmlParse(XmlParseStatement statement): base(statement) { }
        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return astVisitor.Visit(this);
        }
    }



    // --- ARITHMETIC STATEMENTS ---

    public class Add: GenericNode<AddStatement>, CodeElementHolder<AddStatement>, Statement, VariableWriter {
	    public Add(AddStatement statement): base(statement) { }
	    public IDictionary<StorageArea,object> VariablesWritten { get { return this.CodeElement().VariablesWritten; } }
	    public bool IsUnsafe { get { return this.CodeElement().IsUnsafe; } }
        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return astVisitor.Visit(this) && astVisitor.VisitVariableWriter(this);
        }
    }

    public class Use : GenericNode<UseStatement>, CodeElementHolder<UseStatement>, Statement
    {
        public Use(UseStatement statement) : base(statement) { }

        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return astVisitor.Visit(this);
        }
    }

    public class Subtract: GenericNode<SubtractStatement>, CodeElementHolder<SubtractStatement>, Statement, VariableWriter {
	    public Subtract(SubtractStatement statement): base(statement) { }
	    public IDictionary<StorageArea,object> VariablesWritten { get { return this.CodeElement().VariablesWritten; } }
	    public bool IsUnsafe { get { return this.CodeElement().IsUnsafe; } }
        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return astVisitor.Visit(this) && astVisitor.VisitVariableWriter(this);
        }
    }

    public class Multiply: GenericNode<MultiplyStatement>, CodeElementHolder<MultiplyStatement>, Statement, VariableWriter {
	    public Multiply(MultiplyStatement statement): base(statement) { }
	    public IDictionary<StorageArea,object> VariablesWritten { get { return this.CodeElement().VariablesWritten; } }
	    public bool IsUnsafe { get { return this.CodeElement().IsUnsafe; } }
        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return astVisitor.Visit(this) && astVisitor.VisitVariableWriter(this);
        }
    }

    public class Divide: GenericNode<DivideStatement>, CodeElementHolder<DivideStatement>, Statement, VariableWriter {
	    public Divide(DivideStatement statement): base(statement) { }
	    public IDictionary<StorageArea,object> VariablesWritten { get { return this.CodeElement().VariablesWritten; } }
	    public bool IsUnsafe { get { return this.CodeElement().IsUnsafe; } }
        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return astVisitor.Visit(this) && astVisitor.VisitVariableWriter(this);
        }
    }

    public class Compute: GenericNode<ComputeStatement>, CodeElementHolder<ComputeStatement>, Statement, VariableWriter {
	    public Compute(ComputeStatement statement): base(statement) { }
	    public IDictionary<StorageArea,object> VariablesWritten { get { return this.CodeElement().VariablesWritten; } }
	    public bool IsUnsafe { get { return this.CodeElement().IsUnsafe; } }
        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return astVisitor.Visit(this) && astVisitor.VisitVariableWriter(this);
        }
    }



    // --- FILE STATEMENTS ---

    public class Open: GenericNode<OpenStatement>, CodeElementHolder<OpenStatement>, Statement {
	    public Open(OpenStatement statement): base(statement) { }
        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return astVisitor.Visit(this);
        }
    }

    public class Close: GenericNode<CloseStatement>, CodeElementHolder<CloseStatement>, Statement {
	    public Close(CloseStatement statement): base(statement) { }
        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return astVisitor.Visit(this);
        }
    }

    public class Read: GenericNode<ReadStatement>, CodeElementHolder<ReadStatement>, Statement {
	    public Read(ReadStatement statement): base(statement) { }
        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return astVisitor.Visit(this);
        }
    }

    public class Write: GenericNode<WriteStatement>, CodeElementHolder<WriteStatement>, Statement {
	    public Write(WriteStatement statement): base(statement) { }
        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return astVisitor.Visit(this);
        }
    }

    public class Rewrite: GenericNode<RewriteStatement>, CodeElementHolder<RewriteStatement>, Statement {
	    public Rewrite(RewriteStatement statement): base(statement) { }
        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return astVisitor.Visit(this);
        }
    }



    // --- FLOW CONTROL STATEMENTS ---

    public interface StatementCondition { }

    // TODO#248
    // IF
    //  |---> THEN
    //  |      \--> statements
    //  \---> ELSE
    //         \--> statements

    public class If: GenericNode<IfStatement>, CodeElementHolder<IfStatement>, Statement {
	    public If(IfStatement statement): base(statement) { }
        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return astVisitor.Visit(this);
        }
    }
    public class Then: Node, CodeElementHolder<CodeElement>, StatementCondition {
	    public Then()
        {
            SetFlag(Node.Flag.GeneratorCanIgnoreIt, true);
        }
        protected override CodeElement InternalCodeElement => null;
        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return astVisitor.Visit(this);
        }
    }
    public class Else: GenericNode<ElseCondition>, CodeElementHolder<ElseCondition>, StatementCondition {
	    public Else(ElseCondition statement): base(statement) { }
        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return astVisitor.Visit(this);
        }
    }
    public class NextSentence: GenericNode<NextSentenceStatement>, CodeElementHolder<NextSentenceStatement>, Statement {
	    public NextSentence(NextSentenceStatement statement): base(statement) { }
        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return astVisitor.Visit(this);
        }
    }

    // TODO#248
    // EVALUATE
    //  |---> WHEN
    //  |      \--> conditions
    //  |---> THEN
    //  |      \--> statements
    //  |---> WHEN
    //  |      \--> conditions
    //  |---> THEN
    //  |      \--> statements
    // ...
    //  \---> WHEN-OTHER
    //         \--> statements
    //
    // or maybe:
    // EVALUATE
    //  |---> WHEN
    //  |      |--> conditions
    //  |      \--> THEN
    //  |            \--> statements
    //  |---> WHEN
    //  |      |--> conditions
    //  |      \--> THEN
    //  |            \--> statements
    // ...
    //  \---> WHEN-OTHER
    //         \--> statements

    public class Evaluate: GenericNode<EvaluateStatement>, CodeElementHolder<EvaluateStatement>, Statement {
	    public Evaluate(EvaluateStatement statement): base(statement) { }
        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return astVisitor.Visit(this);
        }
    }
    public class WhenGroup: Node, CodeElementHolder<CodeElement>, StatementCondition {
	    public WhenGroup() { }

        protected override CodeElement InternalCodeElement => null;
        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return astVisitor.Visit(this);
        }
    }
    public class When: GenericNode<WhenCondition>, CodeElementHolder<WhenCondition>, StatementCondition {
	    public When(WhenCondition statement): base(statement) { }
        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return astVisitor.Visit(this);
        }
    }
    public class WhenOther: GenericNode<WhenOtherCondition>, CodeElementHolder<WhenOtherCondition>, StatementCondition {
	    public WhenOther(WhenOtherCondition statement): base(statement) { }
        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return astVisitor.Visit(this);
        }
    }

    // TODO#248
    // PERFORM
    //  \---> statements

        public class Perform: GenericNode<PerformStatement>, CodeElementHolder<PerformStatement>, Statement {
	        public Perform(PerformStatement statement): base(statement) { }

            public override bool VisitNode(IASTVisitor astVisitor) {
                return astVisitor.Visit(this);
            }
        }
    public class PerformProcedure: GenericNode<PerformProcedureStatement>, CodeElementHolder<PerformProcedureStatement>, Statement {
	    public PerformProcedure(PerformProcedureStatement statement): base(statement) { }

            public override bool VisitNode(IASTVisitor astVisitor)
            {
                return astVisitor.Visit(this);
            }
        }

    // TODO#248
    // SEARCH
    //  |---> WHEN
    //  |      \--> conditions
    //  |---> THEN
    //  |      \--> statements
    //  |---> WHEN
    //  |      \--> conditions
    //  |---> THEN
    //         \--> statements
    //
    // or maybe:
    // SEARCH
    //  |---> WHEN
    //  |      |--> conditions
    //  |      \--> THEN
    //  |            \--> statements
    //  |---> WHEN
    //         |--> conditions
    //         \--> THEN
    //               \--> statements
    public class Search: GenericNode<SearchStatement>, CodeElementHolder<SearchStatement>, Statement {
	    public Search(SearchStatement statement): base(statement) { }
        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return astVisitor.Visit(this);
        }
    }
    public class WhenSearch: GenericNode<WhenSearchCondition>, CodeElementHolder<WhenSearchCondition>, StatementCondition {
	    public WhenSearch(WhenSearchCondition statement): base(statement) { }
        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return astVisitor.Visit(this);
        }
    }

} // end of namespace TypeCobol.Compiler.Nodes
