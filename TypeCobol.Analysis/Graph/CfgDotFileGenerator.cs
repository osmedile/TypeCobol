﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TypeCobol.Compiler.Report;

namespace TypeCobol.Analysis.Graph
{
    /// <summary>
    /// Graphviz Dot file generator for a Control Flow Graph
    /// </summary>
    /// <typeparam name="N"></typeparam>
    /// <typeparam name="D"></typeparam>
    public class CfgDotFileGenerator<N, D> : AbstractReport, ICfgFileGenerator<N, D>
    {
        /// <summary>
        /// The underlying Control Flow Graph
        /// </summary>
        public ControlFlowGraph<N, D> Cfg
        {
            get;
            set;
        }

        /// <summary>
        /// The Current Writer.
        /// </summary>
        protected TextWriter Writer
        {
            get;
            set;
        }

        /// <summary>
        /// True if an inverse graph must be generated, that is to say using predecessor edges.
        /// </summary>
        public bool Inverse
        {
            get;
            internal set;
        }

        /// <summary>
        /// The Digraph buffer
        /// </summary>
        protected StringBuilder DigraphBuilder;

        /// <summary>
        /// Get the string representing an instruction.
        /// </summary>
        /// <param name="instruction">The instruction to get the string representation.</param>
        protected virtual string InstructionToString(N instruction)
        {
            return instruction == null ? "<null>" : instruction.ToString();
        }

        /// <summary>
        /// Get the dot format name of a block.
        /// </summary>
        /// <param name="block">The block to get the dot format name.</param>
        /// <returns>The block's name</returns>
        protected virtual string BlockName(BasicBlock<N, D> block)
        {
            string name = block.HasFlag(BasicBlock<N, D>.Flags.Start) ? "START" :
                block.HasFlag(BasicBlock<N, D>.Flags.End) ? "END" : ("Block" + block.Index);
            return name;
        }

        /// <summary>
        /// Call back function for emitting a BasicBlock.
        /// </summary>
        /// <param name="block"></param>
        /// <param name="cfg"></param>
        protected virtual bool EmitBasicBlock(BasicBlock<N, D> block, ControlFlowGraph<N, D> cfg)
        {
            Writer.WriteLine(string.Format("Block{0} [", block.Index));
            Writer.Write("label = \"{");
            Writer.Write(BlockName(block));
            Writer.Write("|");

            //Print all instructions inside the block.
            foreach(var i in block.Instructions)
            {
                Writer.Write(InstructionToString(i));
                Writer.Write("\\l");
            }
            Writer.WriteLine("}\"");
            //if (block.MaybeEndBlock)
            //{
            //    Writer.WriteLine("shape = \"Msquare\"");
            //}
            Writer.WriteLine("]");

            //Emit the digraph
            if (Inverse)
            {
                foreach (var edge in block.PredecessorEdges)
                {
                    System.Diagnostics.Debug.Assert(edge >= 0 && edge < cfg.PredecessorEdges.Count);
                    DigraphBuilder.AppendLine(string.Format("Block{0} -> Block{1}", block.Index, cfg.PredecessorEdges[edge].Index));
                }
            }
            else
            {
                foreach (var edge in block.SuccessorEdges)
                {
                    System.Diagnostics.Debug.Assert(edge >= 0 && edge < cfg.SuccessorEdges.Count);
                    DigraphBuilder.AppendLine(string.Format("Block{0} -> Block{1}", block.Index, cfg.SuccessorEdges[edge].Index));
                }
            }

            return true;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="bInverse">True if an inverse graph must be generated, that is to say using predecessor edges</param>
        public CfgDotFileGenerator(bool bInverse = false)
        {
            this.Inverse = bInverse;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="cfg">The underlying Control Flow Graph</param>
        /// <param name="bInverse">True if an inverse graph must be generated, that is to say using predecessor edges</param>
        public CfgDotFileGenerator(ControlFlowGraph<N, D> cfg, bool bInverse = false)
        {
            this.Cfg = cfg;
            this.Inverse = bInverse;
        }

        public void Generate(TextWriter writer, ControlFlowGraph<N, D> cfg)
        {
            //Set the current writer
            this.Writer = writer;
            this.Cfg = cfg;
            if (Cfg != null)
            {
                DigraphBuilder = new StringBuilder();
                Writer.WriteLine("digraph Cfg {");
                if (cfg.HasFlag(ControlFlowGraph<N, D>.Flags.Compound))
                {
                    Writer.WriteLine("compound=true;");
                }
                Writer.WriteLine("node [");
                Writer.WriteLine("shape = \"record\"");
                Writer.WriteLine("]");
                Writer.WriteLine("");

                Writer.WriteLine("edge [");
                Writer.WriteLine("arrowtail = \"empty\"");
                Writer.WriteLine("]");

                //Run DFS on the flow graph, with the emiter callback method.
                if(Inverse)
                    Cfg.DFSInverse(EmitBasicBlock);
                else
                    Cfg.DFS(EmitBasicBlock);
                Writer.WriteLine(DigraphBuilder.ToString());
                Writer.WriteLine("}");
            }
            Writer.Flush();            
        }

        public override void Report(TextWriter writer)
        {
            Generate(writer, Cfg);
        }

        /// <summary>
        /// Escape a text string for the dot format
        /// </summary>
        /// <param name="text">The text string to be escaped</param>
        /// <returns>The escaped string</returns>
        public static string Escape(string text)
        {
            StringBuilder sb = new StringBuilder();
            foreach(char c in text)
            {
                switch(c)
                {                    
                    case '\\':
                    case '"':
                    case '|':
                    case '<':
                    case '>':
                    case '{':
                    case '}':
                        sb.Append('\\');
                        break;
                    case '\n':
                    case '\r':
                        sb.Append(' ');
                        continue;
                }
                sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
