﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TypeCobol.Compiler.CodeElements;
using TypeCobol.Compiler.Concurrency;
using TypeCobol.Compiler.Parser;
using TUVienna.CS_CUP.Runtime;

namespace TypeCobol.Compiler.CupParser
{
    /// <summary>
    /// The Code Element Tokenizer for CS Cup Symbol.
    /// </summary>
    public class CodeElementTokenizer : TUVienna.CS_CUP.Runtime.Scanner, IEnumerable<TUVienna.CS_CUP.Runtime.Symbol>, IEnumerator<TUVienna.CS_CUP.Runtime.Symbol>
    {
        private const int NSTARTS = 1;
        /// <summary>
        /// With CS CUP real toke start at 3, 0 is for EOF and 1 for error.
        /// 2 is for the StatementStart terminal symbol
        /// </summary>
        public const int CS_CUP_START_TOKEN = 2;
        /// <summary>
        /// The EOF symbol
        /// </summary>
        public static TUVienna.CS_CUP.Runtime.Symbol EOF => new TUVienna.CS_CUP.Runtime.Symbol(0, null);


        private TUVienna.CS_CUP.Runtime.Symbol dataRedefines = new TUVienna.CS_CUP.Runtime.Symbol(((int)CodeElementType.DataRedefinesEntry) + CS_CUP_START_TOKEN);

        private Symbol[] dataDescriptionSymbols = new Symbol[2]
        {
            new Symbol(((int)CodeElementType.DataDescriptionEntry) + CS_CUP_START_TOKEN),
            new Symbol(((int)CodeElementType.DataDescriptionEntry) + CS_CUP_START_TOKEN)
        };

        private Symbol[] dataConditionSymbols = new Symbol[2]
        {
            new Symbol(((int)CodeElementType.DataConditionEntry) + CS_CUP_START_TOKEN),
            new Symbol(((int)CodeElementType.DataConditionEntry) + CS_CUP_START_TOKEN)
        };

        private Symbol[] moveSymbols = new Symbol[2]
        {
            new Symbol(((int)CodeElementType.MoveStatement) + CS_CUP_START_TOKEN),
            new Symbol(((int)CodeElementType.MoveStatement) + CS_CUP_START_TOKEN)
        };
        private Symbol[] setSymbols = new Symbol[2]
        {
            new Symbol(((int)CodeElementType.SetStatement) + CS_CUP_START_TOKEN),
            new Symbol(((int)CodeElementType.SetStatement) + CS_CUP_START_TOKEN)
        };
        private Symbol[] displaySymbols = new Symbol[2]
        {
            new Symbol(((int)CodeElementType.DisplayStatement) + CS_CUP_START_TOKEN),
            new Symbol(((int)CodeElementType.DisplayStatement) + CS_CUP_START_TOKEN)
        };
        private Symbol[] addSymbols = new Symbol[2]
        {
            new Symbol(((int)CodeElementType.AddStatement) + CS_CUP_START_TOKEN    ),
            new Symbol(((int)CodeElementType.AddStatement) + CS_CUP_START_TOKEN    )
        };
        private Symbol[] paragraphSymbols = new Symbol[2]
        {
            new Symbol(((int)CodeElementType.ParagraphHeader) + CS_CUP_START_TOKEN    ),
            new Symbol(((int)CodeElementType.ParagraphHeader) + CS_CUP_START_TOKEN    )
        };
        private int idx = 0;


        private Symbol[,] symbolsCache = new Symbol[Enum.GetValues(typeof(CodeElementType)).Length+1, 2];

     
        public void toto()
        {
            foreach (var ceType in Enum.GetValues(typeof(CodeElementType)).Cast<int>())
            {
                symbolsCache[ceType,0] = new Symbol(ceType + CS_CUP_START_TOKEN    );
                symbolsCache[ceType,1] = new Symbol(ceType + CS_CUP_START_TOKEN    );
            }
        }

        /// <summary>
        /// Current code element line index
        /// </summary>
        private int m_CodeElementsLineIndex;
        /// <summary>
        /// Current Code Element Index inside a Code Element Line index
        /// </summary>
        private int m_CodeElementIndex;
        /// <summary>
        /// The list of Code Elements
        /// </summary>
        public ISearchableReadOnlyList<CodeElementsLine> CodeElementsLines
        {
            get;
            internal set;
        }

        /// <summary>
        /// Any Start Token
        /// </summary>
        private int StartToken
        {
            get; set;
        }

        /// <summary>
        /// Any first Code Eelement
        /// </summary>
        private CodeElement[] FirstCodeElements
        {
            get; set;
        }

        /// <summary>
        /// Internal Symbol Yielder
        /// </summary>
        private IEnumerator<TUVienna.CS_CUP.Runtime.Symbol> symbol_yielder;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="codeElementsLines">The List of Code Elements</param>
        public CodeElementTokenizer(ISearchableReadOnlyList<CodeElementsLine> codeElementsLines)
        {
            StartToken = -1;
            this.CodeElementsLines = codeElementsLines;
            Reset();
            toto();
        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        /// <param name="from">From which to copy the Tokenizer in the same state.</param>
        public CodeElementTokenizer(CodeElementTokenizer from) : this(-1, from)
        {
        }

        /// <summary>
        /// Copy constructor with a start symbol.
        /// </summary>
        /// <param name="start">From which to copy the Tokenizer in the same state.</param>
        /// <param name="from">From which to copy the Tokenizer in the same state.</param>
        public CodeElementTokenizer(int start, CodeElementTokenizer from) : this(start, from, null)
        {
        }

        /// <summary>
        /// Copy constructor with a start symbol and a first CodeElement.
        /// </summary>
        /// <param name="start">From which to copy the Tokenizer in the same state.</param>
        /// <param name="firstCE">The first code elemnt</param>
        /// <param name="from">From which to copy the Tokenizer in the same state.</param>
        public CodeElementTokenizer(int start, CodeElementTokenizer from, params CodeElement[] firstCE)
        {
            StartToken = start;
            FirstCodeElements = firstCE;
            this.CodeElementsLines = from.CodeElementsLines;
            m_CodeElementsLineIndex = from.m_CodeElementsLineIndex;
            m_CodeElementIndex = from.m_CodeElementIndex;
            Reset();
        }

        /// <summary>
        /// Singleton Constructor
        /// </summary>
        /// <param name="codeElementsLines">The List of Code Elements</param>
        public CodeElementTokenizer(int start, params CodeElement[] firstCE)
        {
            StartToken = start;
            FirstCodeElements = firstCE;
            this.CodeElementsLines = null;
            Reset();
        }

        public TUVienna.CS_CUP.Runtime.Symbol next_token()
        {
            if (symbol_yielder.MoveNext())
                return symbol_yielder.Current;
            return EOF;
        }

        /// <summary>
        /// Enumerator all Symbol from the CodeElementLines
        /// </summary>
        /// <returns>An Enumerator on Symbols</returns>
        public IEnumerator<TUVienna.CS_CUP.Runtime.Symbol> GetEnumerator()
        {
            if (StartToken >= 0)
            {
                TUVienna.CS_CUP.Runtime.Symbol start_symbol = new TUVienna.CS_CUP.Runtime.Symbol(StartToken, null);
                yield return start_symbol;
            }
            if (FirstCodeElements != null)
            {
                foreach (CodeElement ce in FirstCodeElements)
                {
                    if (ce != null)
                    {
                        yield return produceSymbol(ce);
                    }
                }
            }
            if (CodeElementsLines != null)
            {
                foreach(var cel in CodeElementsLines)
                {
                    if (cel.CodeElements != null)
                    {
                        int ceCount = cel.CodeElements.Count;
                        for (; m_CodeElementIndex < ceCount; m_CodeElementIndex++)
                        {
                            yield return produceSymbol(cel.CodeElements[m_CodeElementIndex]);
                        }
                        m_CodeElementIndex = 0;
                    }
                }
            }
            m_CodeElementsLineIndex = 0;
            yield return EOF;

            TUVienna.CS_CUP.Runtime.Symbol produceSymbol(CodeElement ce)
            {
                TUVienna.CS_CUP.Runtime.Symbol symbol;
                
                if (ce.Type == CodeElementType.DataDescriptionEntry)
                {
                    symbol = dataDescriptionSymbols[idx];
                    idx = (idx + 1) % 2;
                    symbol.Reset(ce);
                }
                else if (ce.Type == CodeElementType.DataConditionEntry)
                {
                    symbol = dataConditionSymbols[idx];
                    idx = (idx + 1) % 2;
                    symbol.Reset(ce);
                }
                else if (ce.Type == CodeElementType.MoveStatement)
                {
                    symbol = moveSymbols[idx];
                    idx = (idx + 1) % 2;
                    symbol.Reset(ce);
                }/*
                else if(ce.Type == CodeElementType.SetStatement)
                {
                    symbol = setSymbols[idx];
                    idx = (idx + 1) % 2;
                    symbol.Reset(ce);
                }/*
                else if (ce.Type == CodeElementType.DisplayStatement)
                {
                    symbol = displaySymbols[idx];
                    idx = (idx + 1) % 2;
                    symbol.Reset(ce);
                }
                else if (ce.Type == CodeElementType.AddStatement)
                {
                    symbol = addSymbols[idx];
                    idx = (idx + 1) % 2;
                    symbol.Reset(ce);
                }*/

                else
                {
                    symbol = new TUVienna.CS_CUP.Runtime.Symbol(((int)ce.Type) + CS_CUP_START_TOKEN    , ce);
                }
                return symbol;
            }

            TUVienna.CS_CUP.Runtime.Symbol produceSymbol2(CodeElement ce)
            {
                TUVienna.CS_CUP.Runtime.Symbol symbol;
                idx = (idx + 1) % 2;
                if (ce.Type <= CodeElementType.EntryStatement)
                {
                    symbol = symbolsCache[(int) ce.Type, idx];
                    symbol.Reset(ce);
                }/*
                else if (ce.Type == CodeElementType.MoveStatement)
                {
                    symbol = moveSymbols[idx];
                    idx = (idx + 1) % 2;
                    symbol.Reset(ce);
                }
                else*/
                {
                    symbol = new TUVienna.CS_CUP.Runtime.Symbol(((int)ce.Type) + CS_CUP_START_TOKEN    , ce);
                }
                
                
                return symbol;
            }
        }

        /// <summary>
        /// Get the string representation of a CodeElementType
        /// </summary>
        /// <param name="ceType"></param>
        /// <returns></returns>
        public static string ToString(CodeElementType ceType)
        {
            string name = System.Enum.GetName(typeof(CodeElementType), ceType);
            return name;
        }

        /// <summary>
        /// Get the string representation of the CodeElementType correponding to a Cup Token.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public static string CupTokenToString(int token)
        {
            return ToString((CodeElementType)(token - CS_CUP_START_TOKEN + 1));
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public TUVienna.CS_CUP.Runtime.Symbol Current => symbol_yielder.Current;

        public void Dispose()
        {
            symbol_yielder = null;
        }

        object System.Collections.IEnumerator.Current => symbol_yielder.Current;

        public bool MoveNext()
        {
            return symbol_yielder != null && symbol_yielder.MoveNext();
        }

        public void Reset()
        {
            symbol_yielder = GetEnumerator();
        }
    }
}
