﻿using System;
using TypeCobol.Compiler.CodeModel;
using TypeCobol.Compiler.CupParser.NodeBuilder;
using TypeCobol.Compiler.Nodes;
using TypeCobol.Compiler.Parser;
using TypeCobol.Compiler.Scopes;
using TypeCobol.Compiler.Symbols;
using TypeCobol.Tools.Options_Config;

namespace TypeCobol.Compiler.Domain
{
    /// <summary>
    /// Abstract base Class use to build the Symbol Table from a program perspective;
    /// </summary>
    public abstract class SymbolTableBuilder : ProgramClassBuilderNodeListener
    {

        /// <summary>
        /// Called when A node has been syntactically recognized by the TypeCobol Parser.
        /// </summary>
        /// <param name="node">The node being built</param>
        /// <param name="program">The Program that contains the node.</param>
        public abstract override void OnNode(Node node, Program program);
    }
}
