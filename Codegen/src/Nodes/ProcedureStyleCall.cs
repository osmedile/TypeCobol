﻿

using System;
using TypeCobol.Codegen.Extensions.Compiler.CodeElements.Expressions;
using TypeCobol.Compiler.Nodes;

namespace TypeCobol.Codegen.Nodes {
	using System.Collections.Generic;
	using TypeCobol.Compiler.CodeElements;
	using TypeCobol.Compiler.Text;

    /// <summary>
///  Class that represents the Node associated to a procedure call.
/// </summary>
internal class ProcedureStyleCall: Compiler.Nodes.Call, Generated {
	private Compiler.Nodes.ProcedureStyleCall Node;
	private CallStatement call;
    //The Original Staement
    private ProcedureStyleCallStatement Statement;
    //Does this call has a CALL-END ?
    private bool HasCallEnd;

    /// <summary>
    /// Arguments Mode.
    /// </summary>
    public enum ArgMode
    {
        Input,
        InOut,
        Output
    };

    /// <summary>
    /// If imported public function are call with EXTERNA POINTER or Not.
    /// </summary>
    public static bool IsNotByExternalPointer
    {
        get;
        set;
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="node">The AST Node of Procedure Call</param>
	public ProcedureStyleCall(Compiler.Nodes.ProcedureStyleCall node)
		: base(null) {
		this.Node = node;
		Statement = (ProcedureStyleCallStatement)Node.CodeElement;       
		call = new CallStatement();
        call.ProgramOrProgramEntryOrProcedureOrFunction = new SymbolReferenceVariable(StorageDataType.ProgramName, Statement.ProcedureCall.ProcedureName);
        call.InputParameters = new List<CallSiteParameter>(Statement.ProcedureCall.Arguments);
		call.OutputParameter = null;
        //Add any optional CALL-END statement
        foreach (var child in node.Children)
        {
            this.Add(child);            
            if (child.CodeElement != null && child.CodeElement.Type == TypeCobol.Compiler.CodeElements.CodeElementType.CallStatementEnd)
            {
                HasCallEnd = true;
            }
        }
	}

    /// <summary>
    /// The Code Element associated with the AST Node
    /// </summary>
	public override CodeElement CodeElement { get { return this.Node.CodeElement; } }

	private List<ITextLine> _cache = null;

    /// <summary>
    /// TextLines to be generated by this node.
    /// Rule: TCCODEGEN_FUNCALL_PARAMS
    /// </summary>
	public override IEnumerable<ITextLine> Lines {
		get {
			if (_cache == null) {
				_cache = new List<ITextLine>();
				var hash = Node.FunctionDeclaration.Hash;
                var originalProcName = Node.FunctionDeclaration.Name.Substring(0, Math.Min(Node.FunctionDeclaration.Name.Length, 22));
                //Rule: TCCODEGEN_FIXFOR_ALIGN_FUNCALL
                TypeCobol.Compiler.Nodes.FunctionDeclaration fun_decl = this.Node.FunctionDeclaration;
                string callString = null;

                //We don't need end-if anymore, but I let it for now. Because this generated code still need to be tested on production
                bool bNeedEndIf = false;
                if (((FunctionDeclarationHeader)fun_decl.CodeElement).Visibility == AccessModifier.Public && fun_decl.GetProgramNode() != this.GetProgramNode())
                {
                    if (this.Node.IsNotByExternalPointer || IsNotByExternalPointer)
                    {
                        IsNotByExternalPointer = true;
                        string guard = string.Format("IF TC-{0}-{1}-Idt not = '{2}'", fun_decl.Library, hash, hash);
                        var guardTextLine = new TextLineSnapshot(-1, guard, null);
                        _cache.Add(guardTextLine);
                        string loadPointer = string.Format("        PERFORM TC-LOAD-POINTERS-{0}", fun_decl.Library);
                        _cache.Add(new TextLineSnapshot(-1, loadPointer, null));
                        string endIf = "    END-IF";
                        _cache.Add(new TextLineSnapshot(-1, endIf, null));


                        callString = string.Format("    CALL TC-{0}-{1}{2}", fun_decl.Library, hash, Node.FunctionCall.Arguments.Length == 0 ? "" : " USING");
                        var callTextLine = new TextLineSnapshot(-1, callString, null);
                        _cache.Add(callTextLine);
                    }
                    else
                    {
                        callString = string.Format("CALL TC-{0}-{1}{2}", fun_decl.Library, hash, Node.FunctionCall.Arguments.Length == 0 ? "" : " USING");
                        var callTextLine = new TextLineSnapshot(-1, callString, null);
                        _cache.Add(callTextLine);
                    }
                }
                else
                {
                     callString = string.Format("CALL '{0}{1}'{2}", hash, originalProcName, Node.FunctionCall.Arguments.Length == 0 ? "" : " USING");
                     var callTextLine = new TextLineSnapshot(-1, callString, null);
                     _cache.Add(callTextLine);

                }
                //Rule: TCCODEGEN_FIXFOR_ALIGN_FUNCALL_PARAMS
				var indent = new string(' ', 13);
                //Hanle Input parameters
                //Rule: TCCODEGEN_FUNCALL_PARAMS
                TypeCobol.Compiler.CodeElements.ParameterSharingMode previousSharingMode = (TypeCobol.Compiler.CodeElements.ParameterSharingMode)(-1);
                int previousSpan = 0;
                if (Statement.ProcedureCall.InputParameters != null)
                    foreach (var parameter in Statement.ProcedureCall.InputParameters)
                    {
                        var name = ToString(parameter, Node.SymbolTable, ArgMode.Input, ref previousSharingMode, ref previousSpan);
                        _cache.Add(new TextLineSnapshot(-1, indent + name, null));
                    }

                //Handle InOut parameters
                //Rule: TCCODEGEN_FUNCALL_PARAMS
                previousSharingMode = (TypeCobol.Compiler.CodeElements.ParameterSharingMode)(-1);
                previousSpan = 0;
                if (Statement.ProcedureCall.InoutParameters != null)
                    foreach (var parameter in Statement.ProcedureCall.InoutParameters)
                    {
                        var name = ToString(parameter, Node.SymbolTable, ArgMode.InOut, ref previousSharingMode, ref previousSpan);
                        _cache.Add(new TextLineSnapshot(-1, indent + name, null));
                    }

                //handle Output paramaters
                //Rule: TCCODEGEN_FUNCALL_PARAMS
                previousSharingMode = (TypeCobol.Compiler.CodeElements.ParameterSharingMode)(-1);
                previousSpan = 0;
                if (Statement.ProcedureCall.OutputParameters != null)
                    foreach (var parameter in Statement.ProcedureCall.OutputParameters)
                    {
                        var name = ToString(parameter, Node.SymbolTable, ArgMode.Output, ref previousSharingMode, ref previousSpan);
                        _cache.Add(new TextLineSnapshot(-1, indent + name, null));
                    }

                if (!HasCallEnd)
                {
                    //Rule: TCCODEGEN_FIXFOR_ALIGN_FUNCALL
                    var call_end = new TextLineSnapshot(-1, !bNeedEndIf ? "    end-call " : "        end-call ", null);
                    _cache.Add(call_end);
                }
                //We don't need end-if anymore, but I let it for now. Because this generated code still need to be tested on production
                if (bNeedEndIf)
                {
                    var end_guardTextLine = new TextLineSnapshot(-1, "    END-IF", null);
                    _cache.Add(end_guardTextLine);
                }

			}
			return _cache;
		}
	}


    /// <summary>
    /// Get the String representation of an parameter with a Sharing Mode.
    /// Rule: TCCODEGEN_FUNCALL_PARAMS
    /// </summary>
    /// <param name="parameter">The Parameter</param>
    /// <param name="table">The Symbol table</param>
    /// <param name="mode">Argument mode Input, InOut, Output, etc...</param>
    /// <param name="previousSharingMode">The previous Sharing Mode</param>
    /// <param name="previousSpan">The previous marging span</param>
    /// <returns>The String representation of the Sharing Mode paramaters</returns>
	private string ToString(TypeCobol.Compiler.CodeElements.CallSiteParameter parameter, Compiler.CodeModel.SymbolTable table, ArgMode mode,
        ref TypeCobol.Compiler.CodeElements.ParameterSharingMode previousSharingMode, ref int previousSpan) {
        Variable variable = parameter.StorageAreaOrValue;
        var name = parameter.IsOmitted ? "omitted" : variable.ToString(true);

        string share_mode = "";
        int defaultSpan = string.Intern("by reference ").Length;
        if (parameter.SharingMode.Token != null)
        {
            if (previousSharingMode != parameter.SharingMode.Value)
            {
                share_mode = "by " + parameter.SharingMode.Token.Text;
                share_mode += new string(' ', defaultSpan - share_mode.Length);
                previousSharingMode = parameter.SharingMode.Value;
            }
        }
        else
        {
            if (mode == ArgMode.InOut || mode == ArgMode.Output)
            {
                if (previousSharingMode != TypeCobol.Compiler.CodeElements.ParameterSharingMode.ByReference)
                {
                    share_mode = string.Intern("by reference ");
                    previousSharingMode = TypeCobol.Compiler.CodeElements.ParameterSharingMode.ByReference;
                }
            }
        }
        if (share_mode.Length == 0)
        {
            share_mode = new string(' ', previousSpan == 0 ? defaultSpan : previousSpan);
        }
        else
        {
            previousSpan = share_mode.Length;
        }

        if (variable != null) {
            if (variable.IsLiteral)
                return share_mode + name;
            var found = table.GetVariables(variable);
            if (found.Count < 1) {  //this can happens for special register : LENGTH OF, ADDRESS OF
                return share_mode + variable.ToCobol85();
            }
//		if (found.Count > 1) return "?AMBIGUOUS?";
            var data = found[0] as DataDescription;
            if (data != null && data.DataType == DataType.Boolean) name += "-value";
        }
        return share_mode + name;
	}

	public bool IsLeaf { get { return true; } }
}

}
