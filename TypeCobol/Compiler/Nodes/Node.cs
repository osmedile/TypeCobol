﻿using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TypeCobol.Compiler.CodeElements;
using TypeCobol.Compiler.CodeElements.Expressions;
using TypeCobol.Compiler.CodeModel;
using TypeCobol.Compiler.Diagnostics;
using TypeCobol.Compiler.Symbols;
using TypeCobol.Compiler.Text;
using TypeCobol.Tools;

namespace TypeCobol.Compiler.Nodes {
    /// <summary>
    ///     Tree node, including:
    ///     - strongly-typed CodeElement data
    ///     - parent/children relations
    ///     - unique identification accross the tree
    /// </summary>
    public abstract class Node : IVisitable{
        protected List<Node> children = new List<Node>();

        /// <summary>TODO: Codegen should do its stuff without polluting this class.</summary>
        public bool? Comment = null;

        protected Node() {
        }



        /// <summary>
        /// Return the CodeElement associated with this Node.
        /// 
        /// Subclass of Node can re-declare this method with "new" to return the correct class of CodeElement.
        /// public SubClassOfCodeElement CodeElement => ...
        /// 
        /// Note : you cannot override and change the return type.
        /// For implementation details see InternalCodeElement property and GenericNode
        /// </summary>
        /// <see cref="InternalCodeElement"/>
        [CanBeNull]
        public CodeElement CodeElement => InternalCodeElement;

        [CanBeNull]
        protected abstract CodeElement InternalCodeElement {  get;}

        /// <summary>
        /// The Semantic data of this Code Element, usually type information.
        /// This is a Weak reference because the data can be hold elsewhere, and it 
        /// can be garbage collected at any moment.
        /// </summary>
        System.WeakReference _mySemanticData = null;
        public virtual ISemanticData SemanticData
        {
            get
            {
                lock (this)
                {
                    return _mySemanticData != null ? (ISemanticData)_mySemanticData.Target : null;
                }
            }
            set
            {
                lock (this)
                {
                    if (_mySemanticData == null)
                        _mySemanticData = new System.WeakReference(value);
                    else
                        _mySemanticData.Target = value;
                    if (value != null && value.SemanticKind == SemanticKinds.Symbol)
                    {
                        ((Symbol) value).TargetNode = this;
                    }
                }
            }
        }

        /// <summary>Parent node (weakly-typed)</summary>
        public Node Parent { get; private set; }

        /// <summary>List of children  (weakly-typed, read-only).</summary>
        /// If you want to modify this list, use the
        /// <see cref="Add" />
        /// and
        /// <see cref="Remove" />
        /// methods.
        public IReadOnlyList<Node> Children {
#if NET40
            get { return new ReadOnlyList<Node>(children); }
#else
            get { return children; }
#endif
        }

        /// <summary>
        /// Children count.
        /// </summary>
        public int ChildrenCount => children?.Count ?? 0;

        /// <summary>
        /// Get the Index position of the given child.
        /// </summary>
        /// <param name="child">The child to get the index position</param>
        /// <returns>The Index position if the child exist, -1 otherwise.</returns>
        public int ChildIndex(Node child)
        {
            if (children == null)
                return -1;
            return children.IndexOf(child);
        }

        /// <summary>
        /// This is usefull durring Code generation phase in order to create un Array of Node.
        /// Each Node will have it Node Index in the Array.
        /// </summary>
        public int NodeIndex { get; set; }

        /// <summary>
        /// Some interresting flags. Note each flag must be a power of 2
        /// for instance: 0x1 << 0; 0x01 << 1; 0x01 << 2 ... 0x01 << 32
        /// </summary>
        public enum Flag : int
        {
            /// <summary>
            /// Flag that indicates that the node has been visited for Type Cobol Qualification style detection.
            /// </summary>
            HasBeenTypeCobolQualifierVisited = 0x01 << 0,

            /// <summary>
            /// This flag is used during code generation to mark extra nodes added during linearization phase.
            /// </summary>
            ExtraGeneratedLinearNode = 0x01 << 1,
            /// <summary>
            /// This flag is used during code generation to mark node having no position
            /// thus they will be generated at this end of the current buffer.
            /// </summary>
            NoPosGeneratedNode = 0x01 << 2,
            /// <summary>
            /// The Node for the End of function Declaration.
            /// </summary>
            EndFunctionDeclarationNode = 0x01 << 3,
            /// <summary>
            /// Mark this node as persistent that is to say it cannot be removed.
            /// </summary>
            PersistentNode = 0x01 << 4,
            /// <summary>
            /// A node that have been generated by The Generator Factory.
            /// </summary>
            FactoryGeneratedNode = 0x01 << 5,
            /// <summary>
            /// A node that have been generated by The Generator Factory.
            /// It is necessary to keek the sequence of insertion
            /// </summary>
            FactoryGeneratedNodeKeepInsertionIndex = 0x01 << 6,
            /// <summary>
            /// When generating this node aNew Line must be introduced first
            /// </summary>
            FactoryGeneratedNodeWithFirstNewLine = 0x01 << 7,
            /// <summary>
            /// Flag for a PROCEDURE DIVION USING PntTab-Pnt: see issue #519
            /// </summary>
            ProcedureDivisionUsingPntTabPnt = 0x01 << 8,
            /// <summary>
            /// During Generation Force this node to be generated by getting his Lines.
            /// </summary>
            ForceGetGeneratedLines = 0x01 << 9,
            /// <summary>
            /// A Node that can be ignored by the generaror
            /// </summary>
            GeneratorCanIgnoreIt = 0x01 << 10,
            /// <summary>
            /// This node has been marked has erased by the generator
            /// </summary>
            GeneratorErasedNode = 0x01 << 11,
            /// <summary>
            /// Generate in a recursive way a a Node Generated by a Factory
            /// </summary>
            FullyGenerateRecursivelyFactoryGeneratedNode = 0x01 << 12,
            /// <summary>
            /// Flag the node as a node comming from intrinsic files
            /// </summary>
            NodeIsIntrinsic = 0x01 <<13,
            /// <summary>
            /// Flag the node that belongs to the working stoage section (Usefull for DataDefinition)
            /// </summary>
            WorkingSectionNode = 0x01 << 14,
            /// <summary>
            /// Flag the node that belongs to the linkage section (usefull for DataDefinition)
            /// </summary>
            LinkageSectionNode = 0x01 << 15,
            /// <summary>
            /// Flag node belongs to Local Storage Section (usefull for DataDefinition)
            /// </summary>
            LocalStorageSectionNode = 0x01 << 16,
            /// <summary>
            /// Flag node belongs to File Section (usefull for DataDefinition)
            /// </summary>
            FileSectionNode = 0x01 << 17,
            /// <summary>
            /// Mark that the node contains an index, 
            /// this flag is usefull for generator to know if it has something special to do with index
            /// </summary>
            NodeContainsIndex = 0x01 << 18,
            /// <summary>
            /// Mark that the node contains an index that is used with a qualified Name
            /// It will be used by code generator, to know if the index has to be hashed or not.
            /// </summary>
            IndexUsedWithQualifiedName = 0x01 << 19,
            /// <summary>
            /// Mark that this node contains a boolean variable that has to be considered by CodeGen. 
            /// </summary>
            NodeContainsBoolean = 0x01 << 20,
            /// <summary>
            /// Mark that this node contains a pointer variable that has to be considered by CodeGen. 
            /// </summary>
            NodeContainsPointer = 0x01 << 21,
            /// <summary>
            /// Mark that this node declare a pointer that is used in an incrementation and need a redefine
            /// </summary>
            NodeisIncrementedPointer = 0x01 << 22,
            /// <summary>
            /// Mark that this node is declared inside a procedure or function
            /// </summary>
            InsideProcedure = 0x01 << 23,
            /// <summary>
            /// Flag node belongs to Global Storage Section (usefull for DataDefinition)
            /// </summary>
            GlobalStorageSection = 0x01 << 24,
            /// <summary>
            /// The Node for a missing END PROGRAM.
            /// </summary>
            MissingEndProgram = 0x01 << 25,
            /// <summary>
            /// Mark a program that contains procedure declaration.
            /// </summary>
            ContainsProcedure = 0x01 << 26,



        };
        /// <summary>
        /// A 32 bits value for flags associated to this Node
        /// </summary>
        public uint Flags 
        { 
            get; 
            internal set; 
        }

        /// <summary>
        /// Test the value of a flag
        /// </summary>
        /// <param name="flag">The flag to test</param>
        /// <returns>true if the flag is set, false otherwise</returns>
        public bool IsFlagSet(Flag flag)
        {
            return (Flags & (uint)flag) != 0;
        }

        /// <summary>
        /// Set the value of a flag.
        /// </summary>
        /// <param name="flag"></param>
        /// <param name="value"></param>
        /// <param name="bRecurse">True if the setting must be recursive over the Children</param>
        public void SetFlag(Flag flag, bool value, bool bRecurse = false)
        {            
            Flags = value  ? (Flags | (uint)flag) : (Flags & ~(uint)flag);
            if (bRecurse)
            {
                foreach (var child in children)
                {
                    child.SetFlag(flag, value, bRecurse);
                }
            }
        }

        public void CopyFlags(uint flag) { Flags = flag; }

        /// <summary>
        /// Used by the Generator to specify a Layout the current Node
        /// </summary>
        public ColumnsLayout ? Layout
        {
            get;
            set;
        }

        public virtual string Name {
            get { return string.Empty; }
        }

        private QualifiedName _qualifiedName;

        public virtual QualifiedName QualifiedName {
            get {
                if (string.IsNullOrEmpty(Name)) return null;
                if (_qualifiedName != null) return _qualifiedName;

                List<string> qn = new List<string>() {Name};
                var parent = this.Parent;
                while (parent != null)
                {
                    if (!string.IsNullOrEmpty(parent.Name)) {
                        qn.Add(parent.Name);
                    }
                    parent = parent.Parent;
                }
                qn.Reverse();
                _qualifiedName = new URI(qn);
                return _qualifiedName;
            }
        }


        private QualifiedName _visualQualifiedName;
        public virtual QualifiedName VisualQualifiedName
        {
            get
            {
                if (string.IsNullOrEmpty(Name)) return null;
                if (_visualQualifiedName != null) return _visualQualifiedName;

                List<string> qn = new List<string>() {Name};
                var parent = this.Parent;
                while (parent != null)
                {
                    if (!string.IsNullOrEmpty(parent.Name))
                    {
                        qn.Add(parent.Name);
                    }
                    if (parent is FunctionDeclaration) //If it's a procedure, we can exit we don't need the program name
                        break;
                    if (parent is Program)
                        break;
                    parent = parent.Parent;
                }
                qn.Reverse();
                _visualQualifiedName = new URI(qn);
                return _visualQualifiedName;
            }
        }

        /// <summary>Non-unique identifier of this node. Depends on CodeElement type and name (if applicable).</summary>
        public virtual string ID {
            get { return null; }
        }

        private string _URI;
        /// <summary>Node unique identifier (scope: tree this Node belongs to)</summary>
        public string URI {
            get
            {
                if (_URI != null) return _URI;
                if (ID == null) return null;
                var puri = Parent?.URI;
                if (puri == null) return ID;
                _URI =  puri + '.' + ID;
                return _URI;
            }
        }



        private SourceFile _root;
        /// <summary>First Node with null Parent among the parents of this Node.</summary>
        public SourceFile Root {
            get
            {
                if (_root != null) return _root;
                var current = this;
                while (current.Parent != null) current = current.Parent;
                _root = (SourceFile) current;
                return _root;
            }
        }

        /// <summary>
        ///     How far removed from SourceFile is this Node?
        ///     Values are 0 if SourceFile is this, 1 of SourceFile is this.Parent,
        ///     2 if SourceFile is this.Parent.Parent, and so on.
        /// </summary>
        public int Generation {
            get {
                var generation = 0;
                var parent = Parent;
                while (parent != null) {
                    generation++;
                    parent = parent.Parent;
                }
                return generation;
            }
        }


        public SymbolTable SymbolTable { get; set; }

        public object this[string attribute] {
            get { return Attributes.Get(this, attribute); }
        }

        public virtual IEnumerable<ITextLine> Lines {
            get {
                var lines = new List<ITextLine>();
                if (CodeElement == null || CodeElement.ConsumedTokens == null) return lines;
                foreach (var token in CodeElement.ConsumedTokens) {//JCM: Don't take in account imported token.
                    if (!(token is TypeCobol.Compiler.Preprocessor.ImportedToken)) {
                        if (!lines.Contains(token.TokensLine))
                            lines.Add(token.TokensLine);
                    }
                }
                return lines;
            }
        }
        public virtual IEnumerable<ITextLine> SelfAndChildrenLines {
            get {
                var lines = new List<ITextLine>();
                lines.AddRange(Lines);
                foreach (var child in children) {
                    lines.AddRange(child.SelfAndChildrenLines);
                }
                return lines;
            }
        }



        /// <summary>
        /// Marker for Code Generation to know if this Node will generate code.
        /// TODO this method should be in CodeGen project
        /// </summary>
        public bool NeedGeneration { get; set; }

        /// <summary>
        /// List of diagnostics detected for the current node. 
        /// Please use AddDiagnostic and RemoveDiagnostic to interact with this property. 
        /// </summary>
        public List<Diagnostic> Diagnostics
        {
            get { return _Diagnostics; }
        }

        /// <summary>
        /// Allows to store the used storage areas and their fully qualified Name. 
        /// </summary>
        public Dictionary<StorageArea, string> QualifiedStorageAreas { get; set; }

        private List<Diagnostic> _Diagnostics;

        /// <summary>
        /// Method to add a new diagnostic to this node
        /// </summary>
        /// <param name="diagnostic"></param>
        public void AddDiagnostic(Diagnostic diagnostic)
        {
            if(_Diagnostics == null)
                _Diagnostics = new List<Diagnostic>();

            _Diagnostics.Add(diagnostic);
        }

        /// <summary>
        /// Method to remove a diagnostic from this node
        /// </summary>
        /// <param name="diagnostic"></param>
        public void RemoveDiagnostic(Diagnostic diagnostic)
        {
            if (_Diagnostics == null || (_Diagnostics != null && _Diagnostics.Count == 0))
                return;

            _Diagnostics.Remove(diagnostic);
        }

        public IList<N> GetChildren<N>() where N : Node {
            return children.OfType<N>().ToList();
        }


        private Program _programNode;
        /// <summary>
        /// Get the Program Node corresponding to a Child
        /// </summary>
        /// <param name="child">The Child Node</param>
        /// <returns>The Program Node</returns>
        public Program GetProgramNode()
        {
            if (_programNode != null) return _programNode;
            Node child = this;
            while (child != null && !(child is Program))
                child = child.Parent;
            _programNode = (Program)child;

            return _programNode;
        }
        
        /// <summary>Search for all children of a specific Name</summary>
        /// <param name="name">Name we search for</param>
        /// <param name="deep">true for deep search, false for shallow search</param>
        /// <returns>List of all children with the proper name ; empty list if there is none</returns>
        public IList<T> GetChildren<T>(string name, bool deep) where T : Node {
            var results = new List<T>();
            foreach (var child in children) {
                var typedChild = child as T;
                if (typedChild != null && name.Equals(child.Name, StringComparison.OrdinalIgnoreCase)) results.Add(typedChild);
                if (deep) results.AddRange(child.GetChildren<T>(name, true));
            }
            return results;
        }


        /// <summary>Adds a node as a children of this one.</summary>
        /// <param name="child">Child to-be.</param>
        /// <param name="index">Child position in children list.</param>
        public virtual void Add(Node child, int index = -1) {
            if (index < 0) children.Add(child);
            else children.Insert(index, child);
            child.Parent = this;
        }

        /// <summary>
        /// Adds children to this node
        /// </summary>
        /// <param name="toAdd">children to be added</param>
        /// <param name="index">children position</param>
        public virtual void AddRange(IEnumerable<Node> toAdd, int index = -1)
        {            
            if (index < 0)
                children.AddRange(toAdd);
            else children.InsertRange(index, toAdd);
            foreach (Node child in toAdd)
            {
                child.Parent = this;
            }
        }

        /// <summary>
        /// Allow to manually set the parent node
        /// </summary>
        /// <param name="parent">Parent node</param>
        public virtual void SetParent(Node parent)
        {
            this.Parent = parent;
        }

        /// <summary>Removes a child from this node.</summary>
        /// <param name="node">Child to remove. If this is not one of this Node's current children, nothing happens.</param>
        public void Remove(Node child) {
            children.Remove(child);
            child.Parent = null;
        }

        /// <summary>Removes this node from its Parent's children list and set this.Parent to null.</summary>
        public void Remove() {
            if (Parent != null) Parent.Remove(this);
        }

        /// <summary>Position of a specific child among its siblings.</summary>
        /// <param name="child">Child to be searched for.</param>
        /// <returns>Position in the children list.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">As List</exception>
        public int IndexOf(Node child) {
            return children.IndexOf(child);
        }

        /// <summary>Delete all childrens of this node.</summary>
        public void Clear() {
            foreach (var child in children) child.Parent = null;
            children.Clear();
        }

        /// <summary>
        /// Get All Childrens.
        /// </summary>
        /// <param name="lines">A List to store all children.</param>
        public void ListChildren(List<Node> list)
        {
            if (list == null) return;
            foreach (var child in children)
            {
                list.Add(child);
                child.ListChildren(list);
            }
        }

        /// <summary>Get this node or one of its children that has a given URI.</summary>
        /// <param name="uri">Node unique identifier to search for</param>
        /// <returns>Node n for which n.URI == uri, or null if no such Node was found</returns>
        public Node Get(string uri) {
            string gen_uri = URI;
            if (gen_uri != null)
            {
                if (uri.IndexOf('(') >= 0 && uri.IndexOf(')') > 0)
                {//Pattern matching URI                    
                    System.Text.RegularExpressions.Regex re = new System.Text.RegularExpressions.Regex(uri);
                    if (re.IsMatch(URI))
                    {
                        return this;
                    }
                }
                if (gen_uri.EndsWith(uri))
                {
                    return this;
                }
            }
            foreach (var child in Children)
            {
                var found = child.Get(uri);
                if (found != null) return found;
            }
            return null;
        }

        /// <summary>Get this node or one of its children that has a given URI.</summary>
        /// <param name="uri">Node unique identifier to search for</param>
        /// <returns>Node n for which n.URI == uri, or null if no such Node was found</returns>
        public Node Get(string uri, int startIndex)
        {
            string gen_uri = URI;
            if (gen_uri != null)
            {
                if (uri.IndexOf('(') >= 0 && uri.IndexOf(')') > 0)
                {//Pattern matching URI                    
                    System.Text.RegularExpressions.Regex re = new System.Text.RegularExpressions.Regex(uri);
                    if (re.IsMatch(URI))
                    {
                        return this;
                    }
                }
                if (gen_uri.EndsWith(uri))
                {
                    return this;
                }
            }
            for (int i = startIndex; i < children.Count; i++)
            {
                var child = this.children[i];
                var found = child.Get(uri);
                if (found != null) return found;
            }
            return null;
        }

        /// <summary>As <see cref="Get" /> method, but can specify the type of Node to retrieve.</summary>
        /// <typeparam name="N"></typeparam>
        /// <param name="uri"></param>
        /// <returns>null if a node with the given URI is found but is not of the proper type</returns>
        public N Get<N>(string uri) where N : Node
        {
            var node = Get(uri);
            try
            {
                return (N)node;
            }
            catch (InvalidCastException)
            {
                return default(N);
            }
        }


        public override string ToString() {
            var str = new StringBuilder();
            Dump(str, 0);
            return str.ToString();
        }

        /// <summary>
        /// Don't override this method, implement VisitNode on child
        /// </summary>
        /// <param name="astVisitor"></param>
        /// <returns></returns>
        public bool AcceptASTVisitor(IASTVisitor astVisitor) {
            bool continueVisit = astVisitor.BeginNode(this) && VisitNode(astVisitor);

            if (continueVisit)
            {
                CodeElement?.AcceptASTVisitor(astVisitor);
            }

            if (continueVisit) {
                //To Handle concurrent modifications during traverse.
                //Get the array of Children that must be traverse.
                if (astVisitor.CanModifyChildrenNode)
                {
                    //Make copy of  children array if the visitor can modify the children
                    Node[] childrenNodes = children.ToArray();
                    continueVisit = VisitChildrens(astVisitor, true, childrenNodes);
                }
                else
                {
                    continueVisit = VisitChildrens(astVisitor, true, children);
                }
                
            }

            astVisitor.EndNode(this);
            return continueVisit;
        }

        private static bool VisitChildrens(IASTVisitor astVisitor, bool continueVisit, IEnumerable<Node> childrenNodes)
        {
            foreach (Node child in childrenNodes)
            {
                if (!continueVisit && astVisitor.IsStopVisitingChildren)
                {
                    break;
                }

                continueVisit = child.AcceptASTVisitor(astVisitor);
            }

            return continueVisit;
        }

        public abstract bool VisitNode(IASTVisitor astVisitor);


        private void Dump(StringBuilder str, int i)
        {
            for (var c = 0; c < i; c++) str.Append("  ");
            if (Comment == true) str.Append('*');
            if (!string.IsNullOrEmpty(Name)) str.AppendLine(Name);
            else if (!string.IsNullOrEmpty(ID)) str.AppendLine(ID);
            else if (CodeElement == null) str.AppendLine("?");
            else str.AppendLine(CodeElement.ToString());
            foreach (var child in Children) child.Dump(str, i + 1);
        }

        /// <summary>TODO: Codegen should do its stuff without polluting this class.</summary>
        public void RemoveAllChildren() {
            children.Clear();
        }

        /// <summary>Implementation of the GoF Visitor pattern.</summary>
        public void Accept(NodeVisitor visitor) {
            visitor.Visit(this);
        }

        /// <summary>
        ///     Return true if this Node is inside a COPY
        /// </summary>
        /// <returns></returns>
        public bool IsInsideCopy() {
            return CodeElement != null && CodeElement.IsInsideCopy();
        }
        /// <summary>
        /// Dictionary that contains pairs of StorageArea and Tuple "string,DataDefintion" for the Read Area
        /// The tuple stores the complete qualified name of the corresponding node (as string) and the DataDefintion.
        /// Node properties are context dependent and the tuple ensures the retrieved DataDefintion is consistent with the context
        /// </summary>
        public IDictionary<StorageArea, Tuple<string,DataDefinition>> StorageAreaReadsDataDefinition { get; internal set; }
        /// <summary>
        /// Dictionary that contains pairs of StorageArea and Tuple "string,DataDefintion" for the Write Area
        /// The tuple stores the complete qualified name of the corresponding node (as string) and the DataDefintion.
        /// Node properties are context dependent and the tuple ensures the retrieved DataDefintion is consistent with the context
        /// </summary>
        public IDictionary<StorageArea, Tuple<string,DataDefinition>> StorageAreaWritesDataDefinition { get; internal set; }

        /// <summary>
        /// Search both dictionaries for a given StorageArea
        /// </summary>
        /// <param name="searchedStorageArea">StorageArea to search for</param>
        /// <param name="isReadDataDefiniton">[Optional] True if storage area needs to be searched in StorageAreaReadsDataDefinition,
        /// false if storage area needs to be searched in StorageAreaWritesDataDefinition.
        /// If parameter is not present, the search is done in both dictionaries</param>
        /// <returns>Correpsonding DataDefinition</returns>
        public DataDefinition GetDataDefinitionFromStorageAreaDictionary(StorageArea searchedStorageArea, bool? isReadDataDefiniton=null)
        {
            Tuple<string, DataDefinition> searchedElem = null;
            if (isReadDataDefiniton == null)
            {
                StorageAreaReadsDataDefinition?.TryGetValue(
                    searchedStorageArea, out searchedElem);
                if (searchedElem == null)
                {
                    StorageAreaWritesDataDefinition?.TryGetValue(
                        searchedStorageArea, out searchedElem);
                }
            }
            if (isReadDataDefiniton.HasValue&&isReadDataDefiniton.Value)
            {
                StorageAreaReadsDataDefinition?.TryGetValue(
                    searchedStorageArea, out searchedElem);
            }

            if (isReadDataDefiniton.HasValue && !isReadDataDefiniton.Value)
            {
                StorageAreaWritesDataDefinition?.TryGetValue(
                    searchedStorageArea, out searchedElem);
            }
            return searchedElem?.Item2;
        }
        
        public DataDefinition GetDataDefinitionForQualifiedName(QualifiedName qualifiedName, bool? isReadDictionary=null)
        {
            Tuple<string, DataDefinition> searchedElem = null;
            if (isReadDictionary.HasValue)
            {
                searchedElem = isReadDictionary.Value
                    ? StorageAreaReadsDataDefinition
                        ?.FirstOrDefault(elem => elem.Key.SymbolReference.Name == qualifiedName.ToString()).Value
                    : StorageAreaWritesDataDefinition
                        ?.FirstOrDefault(elem => elem.Key.SymbolReference.Name == qualifiedName.ToString()).Value;
            }
            else
            {
                searchedElem = StorageAreaReadsDataDefinition?.FirstOrDefault(elem => elem.Key.SymbolReference.Name == qualifiedName.ToString()).Value ??
                               StorageAreaWritesDataDefinition?.FirstOrDefault(elem => elem.Key.SymbolReference.Name == qualifiedName.ToString()).Value;

            }

            return searchedElem?.Item2;
        }
    }

    /// <summary>
    /// <![CDATA[
    /// This class provide generics support for CodeElement property.
    /// 
    /// 
    /// Note: Generics cannot be used directly on Node class for CodeElement property.
    /// If you try to use generics with Node<CE> where CE : CodeElement.
    /// then the property Children have to use generic Node as well.
    /// Children<Node<CE>> is wrong because not all children use the same class for the CodeElement.
    /// ]]>
    /// </summary>
    /// <typeparam name="CE">The type of CodeElement associated with this Node</typeparam>
    public abstract class GenericNode<CE> : Node where CE : CodeElement {


        protected GenericNode([NotNull] CE codeElement)
        {
            this.CodeElement = codeElement;
        }

        [NotNull]
        protected override CodeElement InternalCodeElement => CodeElement;

        /// <summary>
        /// Use "new" keyword so we can change the return type.
        /// </summary>
        [NotNull]
        public new CE CodeElement {get; }
    }

    /// <summary>Implementation of the GoF Visitor pattern.</summary>
    public interface NodeVisitor {
        void Visit(Node node);
    }


    /// <summary>A <see cref="Node" /> who can type its parent more strongly should inherit from this.</summary>
    /// <typeparam name="C">Class (derived from <see cref="Node{T}" />) of the parent node.</typeparam>
    public interface Child<P> where P : Node {}

    /// <summary>Extension method to get a more strongly-typed parent than just Node.</summary>
    public static class ChildExtension {
        /// <summary>Returns this node's parent in as strongly-typed.</summary>
        /// <typeparam name="P">Class (derived from <see cref="Node{T}" />) of the parent.</typeparam>
        /// <param name="child">We want this <see cref="Node" />'s parent.</param>
        /// <returns>This <see cref="Node" />'s parent, but strongly-typed.</returns>
        public static P Parent<P>(this Child<P> child) where P : Node {
            var node = child as Node;
            if (node == null) throw new ArgumentException("Child must be a Node.");
            return (P) node.Parent;
        }
    }

    /// <summary>A <see cref="Node" /> who can type its children more strongly should inherit from this.</summary>
    /// <typeparam name="C">Class (derived from <see cref="Node{T}" />) of the children nodes.</typeparam>
    public interface Parent<C> where C : Node {}

    /// <summary>Extension method to get children more strongly-typed than just Node.</summary>
    public static class ParentExtension {
        /// <summary>
        ///     Returns a read-only list of strongly-typed children of a <see cref="Node" />.
        ///     The children are more strongly-typed than the ones in the Node.Children property.
        ///     The list is read-only because the returned list is a copy of the Node.Children list property ;
        ///     thus, writing node.StrongChildren().Add(child) will be a compilation error.
        ///     Strongly-typed children are to be iterated on, but to modify a Node's children list you have
        ///     to use the (weakly-typed) Node.Children property.
        /// </summary>
        /// <typeparam name="C">Class (derived from <see cref="Node{T}" />) of the children.</typeparam>
        /// <param name="parent">We want this <see cref="Node" />'s children.</param>
        /// <returns>Strongly-typed list of a <see cref="Node" />'s children.</returns>
        public static IReadOnlyList<C> Children<C>(this Parent<C> parent) where C : Node {
            var node = parent as Node;
            if (node == null) throw new ArgumentException("Parent must be a Node.");
            //TODO? maybe use ConvertAll or Cast from LINQ, but only
            // if the performance is better or if it avoids a copy.
            var result = new List<C>();
            foreach (var child in node.Children) result.Add((C) child);
#if NET40
            return new ReadOnlyList<C>(result);
#else
            return result.AsReadOnly();
#endif

        }
    }


    /// <summary>SourceFile of any Node tree, with null CodeElement.</summary>
    public class SourceFile : GenericNode<CodeElement> {
        public SourceFile() : base(null)
        {
            GeneratedCobolHashes = new Dictionary<string, string>();
        }
        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return astVisitor.Visit(this);
        }

        /// <summary>
        /// Dictionary of hashes and signatures for the different function and procedure. Allows to avoid duplicates. 
        /// </summary>
        public Dictionary<string, string> GeneratedCobolHashes { get; set; }


        public IEnumerable<Program> Programs {
            get
            {
                return this.children.Where(c => c is Program && !((Program)c).IsNested).Select(c => c as Program);
            }
        }

        public SourceProgram MainProgram { get; internal set; }

        public IEnumerable<Class> Classes
        {
            get
            {
                return this.children.OfType<Class>();
            }
        }











    }

    public class LibraryCopy : GenericNode<LibraryCopyCodeElement>, Child<Program> {
        public LibraryCopy(LibraryCopyCodeElement ce) : base(ce) {}

        public override string ID {
            get { return "copy"; }
        }

        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return astVisitor.Visit(this);
        }
    }

    public class Class : GenericNode<ClassIdentification> {
        public Class(ClassIdentification identification) : base(identification) {}

        public override string ID {
            get { return "class";  }
        }
        public override string Name { get { return this.CodeElement.ClassName.Name; } }

        public override bool VisitNode(IASTVisitor astVisitor) {
            return astVisitor.Visit(this);
        }
    }

    public class Factory : GenericNode<FactoryIdentification> {
        public Factory(FactoryIdentification identification) : base(identification) {}

        public override string ID {
            get { return "TODO#248"; }
        }

        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return astVisitor.Visit(this);
        }
    }

    public class Method : GenericNode<MethodIdentification> {
        public Method(MethodIdentification identification) : base(identification) {}

        public override string ID {
            get { return "Method"; }
        }

        public override string Name { get { return this.CodeElement.MethodName.Name; } }

        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return astVisitor.Visit(this);
        }
    }

    public class Object : GenericNode<ObjectIdentification> {
        public Object(ObjectIdentification identification) : base(identification) {}

        public override string ID {
            get { return "TODO#248"; }
        }

        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return astVisitor.Visit(this);
        }
    }

    public class End : GenericNode<CodeElementEnd> {
        public End(CodeElementEnd end) : base(end) {}

        public override string ID {
            get { return "end"; }
        }

        public override bool VisitNode(IASTVisitor astVisitor)
        {
            return astVisitor.Visit(this);
        }
    }
} // end of namespace TypeCobol.Compiler.Nodes