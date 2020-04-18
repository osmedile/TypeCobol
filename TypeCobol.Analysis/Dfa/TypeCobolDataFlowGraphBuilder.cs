using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TypeCobol.Analysis.Graph;
using TypeCobol.Compiler.CodeElements;
using TypeCobol.Compiler.Nodes;
using TypeCobol.Compiler.Symbols;

namespace TypeCobol.Analysis.Dfa
{
    /// <summary>
    /// Data Flow Graph Builder for Data Flow Analysis, a TypeCobol Specialization.
    /// </summary>
    public class TypeCobolDataFlowGraphBuilder : DataFlowGraphBuilder<Node, Symbol>
    {

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="cfg"></param>
        public TypeCobolDataFlowGraphBuilder(ControlFlowGraph<Node, DfaBasicBlockInfo<Symbol>> cfg) : base(cfg)
        {
        }

        /// <summary>
        /// Get Use Variables for a given node.
        /// </summary>
        /// <param name="node">The node</param>
        /// <returns>The set of used variables</returns>
        public override HashSet<Symbol> GetUseVariables(Node node)
        {
            HashSet<Symbol> symbolDefs;
            if (node.SymbolStorageAreasRead?.Values != null) {
                symbolDefs = new HashSet<Symbol>(node.SymbolStorageAreasRead.Values);
            } else
            {
                symbolDefs = new HashSet<Symbol>();
            }
            return symbolDefs;
        }

        /// <summary>
        /// Get Defined Variables for a given node
        /// </summary>
        /// <param name="node">The node</param>
        /// <returns>The set of defined variable</returns>
        public override HashSet<Symbol> GetDefVariables(Node node)
        {
            HashSet<Symbol> symbolDefs;
            if (node.SymbolStorageAreasWritten?.Values != null)
            {
                symbolDefs = new HashSet<Symbol>(node.SymbolStorageAreasWritten.Values);
            }
            else
            {
                symbolDefs = new HashSet<Symbol>();
            }
            return symbolDefs;
        }
    }
}
