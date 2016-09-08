﻿namespace TypeCobol.Compiler.CodeElements.Functions {

using System;
using System.Collections.Generic;

public class FunctionDeclarationProfile: CodeElement/*, Returning*/ {

	/// <summary>INPUT datanames, as long as wether they are passed BY REFERENCE or BY VALUE.</summary>
	public SyntaxProperty<Passing.Mode> Input { get; internal set; }
	/// <summary>OUTPUT datanames, always passed BY REFERENCE.</summary>
	public SyntaxProperty<Passing.Mode> Output { get; internal set; }
	/// <summary>INOUT datanames, always passed BY REFERENCE.</summary>
	public SyntaxProperty<Passing.Mode> Inout { get; internal set; }
	/// <summary>RETURNING dataname.</summary>
	public SyntaxProperty<Passing.Mode> Returning { get; internal set; }

	public ParametersProfile Profile { get; private set; }

	public FunctionDeclarationProfile(): base(CodeElementType.ProcedureDivisionHeader) {
		Profile = new ParametersProfile();
	}

	/// <summary>Only called if there are no INPUT/OUTPUT/INOUT/USING parameters.</summary>
	public FunctionDeclarationProfile(ProcedureDivisionHeader other): this() {
		if (other.UsingParameters != null && other.UsingParameters.Count > 0)
			throw new NotImplementedException("Implementation error #245");
		if (other.ReturningParameter != null) {
			// we might have a RETURNING parameter to convert, but only if there is neither
			// PICTURE nor TYPE clause for the returning parameter in the function declaration.
			// however, this is as syntax error.
			var pentry = new ParameterDescriptionEntry();
			Profile.ReturningParameter = new ParameterDescription(pentry);
			var data = other.ReturningParameter.StorageArea as DataOrConditionStorageArea;
			if (data != null) {
				pentry.DataName = CreateSymbolDefinition(data.SymbolReference);
				// Picture will remain empty, we can't do anything about it
			}
		}
		this.ConsumedTokens = other.ConsumedTokens;
	}

	private SymbolDefinition CreateSymbolDefinition(SymbolReference reference) {
		return new SymbolDefinition(reference.NameLiteral, reference.Type);
	}
}

}
