using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using TypeCobol.Compiler.CodeElements;
using TypeCobol.Compiler.CodeElements.Expressions;
using TypeCobol.Compiler.CodeModel;
using TypeCobol.Compiler.Nodes;
using TypeCobol.Compiler.Parser;

namespace TypeCobol.Compiler.Diagnostics
{
    public class TypeCobolLinker 
    {
        
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="typedVariablesOutsideTypedef">Variable outside that use the "type" syntax</param>
        /// <param name="typeThatNeedTypeLinking">Typedef that need all its typed children to be resolved (only use case for now is "Depending on")</param>
        /// <param name="typeToResolve">All others typedef</param>
        public static void LinkedTypedVariables([NotNull][ItemNotNull] in List<DataDefinition> typedVariablesOutsideTypedef, 
            [NotNull][ItemNotNull] in List<TypeDefinition> typeThatNeedTypeLinking)
        {
            //Stack to detect circular reference between types
            Stack<DataDefinition> currentlyCheckedTypedefStack = new Stack<DataDefinition>();

            
            foreach (var dataDefinition in typedVariablesOutsideTypedef)
            {
                //There should be no typeDef at this point, because it's the job of the Linker and only it should do it
                System.Diagnostics.Debug.Assert(dataDefinition.TypeDefinition == null);
                

                if (ResolveType(dataDefinition)) //If type has been found in SymbolTable
                {
                    //Reference the path between the typedDataDefChild and its TypeDefinition on the SymbolTable on the original DataDefinition outside a typedef
                    if (dataDefinition.SymbolTable.TypesReferences.TryGetValue(dataDefinition.TypeDefinition, out var dataDefsThatReferencedThisType))
                    {
                        //Link between the type and the dataDefinition cannot already be done
                        System.Diagnostics.Debug.Assert(!dataDefsThatReferencedThisType.Contains(dataDefinition));

                        //Type already referenced in our SymbolTable, it means all further typed children of the type have already been linked into this SymbolTable 
                        dataDefsThatReferencedThisType.Add(dataDefinition);
                    }
                    else
                    {
                        dataDefinition.SymbolTable.TypesReferences.Add(dataDefinition.TypeDefinition, new List<DataDefinition> { dataDefinition });

                        //First time this TypeDefinition is added to dataDefinition.SymbolTable, then link all children of the type
                        LinkTypedChildren(dataDefinition.TypeDefinition, currentlyCheckedTypedefStack, dataDefinition.SymbolTable);
                    }
                }
            }

            //Now link type that use depending On
            System.Diagnostics.Debug.Assert(currentlyCheckedTypedefStack.Count == 0, "Stack must be empty");
            foreach (var typeDefinition in typeThatNeedTypeLinking)
            {
                LinkTypedChildren(typeDefinition, currentlyCheckedTypedefStack, typeDefinition.SymbolTable);
            }

            /*
            //Now just check for circular in remaining typedef
            System.Diagnostics.Debug.Assert(currentlyCheckedTypedefStack.Count == 0, "Stack must be empty");
            foreach (var typeDefinition in typeToResolve)
            {
                LinkTypedChildren(typeDefinition, currentlyCheckedTypedefStack);//No need to keep reference because these are unused types
            }
            */
        }


        public static void CheckCircularReferences([NotNull] TypeDefinition typeDefinition)
        {
                                 
            if (typeDefinition.TypedChildren.Count == 0                     //no typed children     
                || typeDefinition.TypedChildren[0] == null                  //TypeDefinition of first children could not be resolved
                || typeDefinition.TypedChildren[0].TypeDefinition != null)  //TypeDefinition of first children already resolved
            {
                //In these case, nothing to do because no children or job has already be done
                return;
            }

            //Stack to detect circular reference between types
            Stack<DataDefinition> currentlyCheckedTypedefStack = new Stack<DataDefinition>();
            LinkTypedChildren(typeDefinition, currentlyCheckedTypedefStack);
        }

        /// <summary>
        /// Detect circular reference between type
        /// Resolve TypeDefinition
        /// Link the type with the DataDefinition that use it in SymbolTable
        /// </summary>
        /// <param name="currentlyCheckedTypedefStack"></param>
        /// <param name="symbolTable"></param>
        /// <param name="typeDefinition"></param>
        private static void LinkTypedChildren([NotNull] TypeDefinition typeDefinition,
            [CanBeNull] Stack<DataDefinition> currentlyCheckedTypedefStack, [CanBeNull] SymbolTable symbolTable = null)
        {

            if (typeDefinition.TypedChildren.Count == 0)
            {
                return;
            }
            currentlyCheckedTypedefStack?.Push(typeDefinition);
            LinkTypedChildren0();
            currentlyCheckedTypedefStack?.Pop();


            //Only reason to use private method here, is because there multiple return path, so we can easily handle currentlyCheckedTypedefStack push/pop 
            void LinkTypedChildren0()
            {
                //If all typed children of typedef are resolved it means this typedef has already been fully linked
                if (typeDefinition.TypedChildren[typeDefinition.TypedChildren.Count - 1] == null || typeDefinition.TypedChildren[typeDefinition.TypedChildren.Count - 1]?.TypeDefinition != null)
                {
                    //If symbolTable is null, it means we don't want to register link between TypeDefinition and DataDefinition that use it
                    //So we can stop here
                    if (symbolTable == null)
                    {
                        return;
                    }

                    //As typedef has already been linked, then no need to check circular reference
                    currentlyCheckedTypedefStack = null;
                }


                for (var i = 0; i < typeDefinition.TypedChildren.Count; i++)
                {
                    var typedDataDefChild = typeDefinition.TypedChildren[i];
                    //If a typedDataDefChild is null it means is TypeDefinition could not be resolved, so let's continue to the next
                    if (typedDataDefChild == null)
                    {
                        continue;
                    }


                    //Resolve type of typedDataDefChild if not already done yet
                    if (typedDataDefChild.TypeDefinition == null)
                    {
                        if (!ResolveType(typedDataDefChild)) //Use the symbolTable of this typedDataDefChild to resolve the type
                        {
                            typeDefinition.TypedChildren[i] = null; //set to null so we don't try to check its TypeDefinition another time.
                            continue;                               //Go to the next typedChildren
                        }
                    }


                    System.Diagnostics.Debug.Assert(typedDataDefChild.TypeDefinition != null); //type must be resolved now


                    if (currentlyCheckedTypedefStack?.Contains(typedDataDefChild.TypeDefinition) == true)
                    {
                        DiagnosticUtils.AddError(typedDataDefChild, "Type circular reference detected : "
                                                                    + string.Join(" -> ", currentlyCheckedTypedefStack.Select(t => t.Name)), code: MessageCode.SemanticTCErrorInParser);

                        continue;//Go to the next typedChildren, it means the type has already been linked anyway
                                 //Do not make the link in symbolTable.TypesReferences with method ReferenceThisDataDefByThisType to avoid infinite loop in SymbolTable
                    }


                    if (symbolTable != null)//If we need to keep references between typed variable and type
                    {
                        //Reference the path between the typedDataDefChild and its TypeDefinition on the SymbolTable on the original DataDefinition outside a typedef
                        if (!ReferenceThisDataDefByThisType(symbolTable, typedDataDefChild))
                        {
                            //Type was already linked, so stop here
                            continue;
                        }
                    }


                    //Continue to link children
                    LinkTypedChildren(typedDataDefChild.TypeDefinition, currentlyCheckedTypedefStack, symbolTable);
                }
            }
        }

        /// <summary>
        /// Lookup the TypeDefinition from the DataType of this dataDefinition.
        /// If no TypeDefinition can be found, then property TypeDefinition stay null.
        /// </summary>
        /// <param name="dataDefinition"></param>
        /// <returns>true if type has been resolved</returns>
        private static bool ResolveType([NotNull] in DataDefinition dataDefinition)
        {
            //dataDefinition.CodeElement cannot be null, only Index have a null CodeElement and Index cannot be typed
            var types = dataDefinition.SymbolTable.GetType(dataDefinition.CodeElement.DataType); 

            if (types.Count < 1)
            {
                string message = "TYPE \'" + dataDefinition.CodeElement.DataType + "\' is not referenced";
                DiagnosticUtils.AddError(dataDefinition, message, MessageCode.SemanticTCErrorInParser);
            }
            else if (types.Count > 1)
            {
                string message = "Ambiguous reference to TYPE \'" + dataDefinition.CodeElement.DataType + "\'";
                DiagnosticUtils.AddError(dataDefinition, message, MessageCode.SemanticTCErrorInParser);
            }
            else
            {
                dataDefinition.TypeDefinition = types[0];
                dataDefinition.DataType.RestrictionLevel = types[0].DataType.RestrictionLevel;
                return true;
            }

            return false;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="symbolTable"></param>
        /// <param name="dataDefinition"></param>
        /// <returns>true if reference has been set. False is reference was already made</returns>
        private static bool ReferenceThisDataDefByThisType([NotNull] in SymbolTable symbolTable, [NotNull] in DataDefinition dataDefinition)
        {
            //Reminder on TypesReferences 
            //Key is TypeDefinition, Value is List<DataDefinition>
            //
            //TypesReferences can be understood as:
            //TypeDefinition is referenced by these DataDefinitions


            //We don't use the SymbolTable of the DataDefinition because we are crawling (see explanation at top of the class)
            if (symbolTable.TypesReferences.TryGetValue(dataDefinition.TypeDefinition, out var dataDefsThatReferencedThisType))
            {
                if (dataDefsThatReferencedThisType.Contains(dataDefinition))
                {
                    return false;
                }
                dataDefsThatReferencedThisType.Add(dataDefinition);
            }
            else
            {
                //Same here, don't use the SymbolTable of the DataDefinition
                symbolTable.TypesReferences.Add(dataDefinition.TypeDefinition, new List<DataDefinition> { dataDefinition });
            }

            return true;
        }
    }
}
