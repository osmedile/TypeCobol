﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using TypeCobol.Codegen.Config;
using TypeCobol.Codegen.Skeletons;
using TypeCobol.Compiler; // DocumentFormat
using TypeCobol.Compiler.Domain;
using TypeCobol.Compiler.Parser;
using TypeCobol.Tools; // CodeElementDiagnostics
using TypeCobol.Tools.Options_Config;

namespace TypeCobol.Codegen.Domain
{
    [TestClass]
    public class TestTypeCobolDomainCodegen
    {
        public static bool UseSkeleton = false;
        public static TypeCobolConfiguration DefaultConfig = null;
        public static ProgramSymbolTableBuilder Builder = null;
        public static NodeListenerFactory BuilderNodeListenerFactory = null;
        public static string DefaultIntrinsicPath = null;//@"C:\TypeCobol\Sources\##Latest_Release##\Intrinsic\Intrinsic.txt";

        [TestInitialize]
        public void TestInitialize()
        {
            SymbolTableBuilder.Root = null;
            //Create a default configurations for options
            DefaultConfig = new TypeCobolConfiguration();
            if (File.Exists(DefaultIntrinsicPath))
            {
                DefaultConfig.Copies.Add(DefaultIntrinsicPath);
            }
            DefaultConfig.Dependencies.Add(Path.Combine(Directory.GetCurrentDirectory(), "resources", "dependencies"));
            SymbolTableBuilder.Config = DefaultConfig;

            //Force the creation of the Global Symbol Table
            var global = SymbolTableBuilder.Root;

            //Allocate a static Program Symbol Table Builder
            BuilderNodeListenerFactory = () =>
            {
                Builder = new ProgramSymbolTableBuilder();
                return Builder;
            };
            Compiler.Parser.NodeDispatcher.RegisterStaticNodeListenerFactory(BuilderNodeListenerFactory);
        }


        [TestCleanup]
        public void TestCleanup()
        {
            if (BuilderNodeListenerFactory != null)
            {
                Compiler.Parser.NodeDispatcher.RemoveStaticNodeListenerFactory(BuilderNodeListenerFactory);
                if (Builder.Programs.Count != 0)
                {
                    foreach (var prog in Builder.Programs)
                        SymbolTableBuilder.Root.RemoveProgram(prog);
                }
            }
            SymbolTableBuilder.Root = null;
        }


        [TestMethod]
        [TestCategory("Config")]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void CreateDefaultParser()
        {
            System.Type DEFAULT = typeof(XmlParser);
            ConfigParser parser;
            parser = Config.Config.CreateParser("");
            Assert.AreEqual(parser.GetType(), DEFAULT);
            parser = Config.Config.CreateParser(".unexistentextension");
            Assert.AreEqual(parser.GetType(), DEFAULT);
        }

        [TestMethod]
        [TestCategory("Config")]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void CreateXMLParser()
        {
            var parser = Config.Config.CreateParser(".xml");
            Assert.AreEqual(parser.GetType(), typeof(XmlParser));
        }

        [TestMethod]
        [TestCategory("Config")]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void ParseFailureIfNotFound()
        {
            try
            {
                CodegenTestUtils.ParseConfig("NOT_FOUND");
                throw new System.Exception("Expected FileNotFoundException");
            }
            catch (FileNotFoundException) { } // Test OK
        }

        [TestMethod]
        [TestCategory("Config")]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void ParseEmpty()
        {
        }

        [TestMethod]
        [TestCategory("Config")]
        [TestCategory("DomainCodegen")]
        [TestCategory("Parsing")]
        [TestProperty("Time", "fast")]
        public void ParseTypes()
        {
            string file = Path.Combine("TypeCobol", "Types");
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(file + ".rdz.cbl", skeletons);
            file = Path.Combine("TypeCobol", "Types2");
            //CodegenTestUtils.ParseGenerateCompare(file + ".rdz.cbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestCategory("Parsing")]
        [TestProperty("Time", "fast")]
        public void ParseBooleans()
        {
            string file = Path.Combine("TypeCobol", "TypeBOOL");
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(file + ".rdz.cbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestCategory("Parsing")]
        [TestProperty("Time", "fast")]
        public void TypeBoolValue()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "TypeBoolValue") + ".rdz.cbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void TypedefInnerBool()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "TypedefInnerBool") + ".rdz.cbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void TypedefInnerBool2()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "TypedefInnerBool2") + ".rdz.cbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void ParseUnsafe()
        {
            string file = Path.Combine("TypeCobol", "unsafe");
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(file + ".rdz.cbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void QualifiedNamesInsideFunctions()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "QualifiedNamesInsideFunctions") + ".rdz.cbl", skeletons);
            //CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "QualifiedNamesInsideFunctions2") + ".rdz.cbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void ParseQualifiedNames()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "QualifiedNames") + ".rdz.cbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void ParseQualifReferenceModifier()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "QualifReferenceModifier") + ".rdz.cbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void ParseQualifReferenceModifierProc()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "QualifReferenceModifierProc") + ".rdz.cbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void ParseFunctions()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "Function") + ".rdz.cbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void ParseFunctionsDeclaration()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;

            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "FunDeclare") + ".rdz.cbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void ParseLibrary()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "Library") + ".rdz.cbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void ParseLibrary2()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "Library2") + ".rdz.tcbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void ParseLibrary4()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "Library4") + ".rdz.tcbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void CallPublicProcFromPrivateProc()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "CallPublicProcFromPrivateProc") + ".rdz.tcbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void CallPublicProcFromPrivateProc3()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "CallPublicProcFromPrivateProc3") + ".rdz.tcbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void CallPublicProcFromPrivateProc4()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "CallPublicProcFromPrivateProc4") + ".rdz.tcbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void CallPublicProcFromPublicProc()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "CallPublicProcFromPublicProc") + ".rdz.tcbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void CallPublicProcFromPublicProc2()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "CallPublicProcFromPublicProc-DeclarativesNoDebug") + ".rdz.tcbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void CallPublicProcFromPublicProc3()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "CallPublicProcFromPublicProc-DeclarativesWithDebug") + ".rdz.tcbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void CallPublicProcInsideProcOfSameProgram()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "CallPublicProcInsideProcOfSameProgram") + ".rdz.tcbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void ParseProcedureCall()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "ProcedureCall") + ".rdz.cbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void ParseProcedureCallPrivateQualifier()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "ProcedureCall-PrivateQualifier") + ".rdz.tcbl", skeletons);
        }
        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void ParseProcedureCallPublic()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "ProcedureCall-Public") + ".rdz.tcbl", skeletons);
        }
        public void ParseProcedureCallPublic2()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "ProcedureCall-Public2") + ".rdz.tcbl", skeletons);
        }

        public void ParseProcedureCallPublic3()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "ProcedureCall-Public3") + ".rdz.tcbl", skeletons);
        }


        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void ParseProcedureCallPublicAndDeclaratives()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "ProcedureCall-PublicAndDeclaratives") + ".rdz.tcbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void ParseProcedureCallPublicAndDeclaratives2()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "ProcedureCall-PublicAndDeclaratives2") + ".rdz.tcbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void ParseProcedureCallPublicAndDeclaratives3()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "ProcedureCall-PublicAndDeclaratives3") + ".rdz.tcbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void ParseProcedureCallPublicAndDeclaratives4()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "ProcedureCall-PublicAndDeclaratives4") + ".rdz.tcbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void ParseProcCallWithQualified()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "ProcCallWithQualified") + ".rdz.tcbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void Codegen()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "Codegen") + ".rdz.cbl", skeletons);
        }

        [TestMethod]
        [Ignore]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void ParseSuccessiveCalls()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "SuccessiveCalls") + ".rdz.cbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void ParseTypeDefLevel88()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "TypeDefLevel88") + ".rdz.cbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void ParseTypedefPublicPrivate()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "Typedef") + ".rdz.tcbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void ParseTypedefNoPic()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "TypedefNoPic") + ".rdz.tcbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void ParseLineExceed()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "LineExceed") + ".rdz.cbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void ParseLineExceed2()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "LineExceed2") + ".rdz.cbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void ParseLineExceedAutoSplit3()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "LineExceed3") + ".rdz.cbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void ParseLineExceedAutoSplit4()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "LineExceed4") + ".rdz.cbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void ParseLineExceedAutoSplit5()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "LineExceed5") + ".rdz.cbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void ParseLineExceedAutoSplit6()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "LineExceed6") + ".rdz.cbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void ParseMoveUnsafe()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "MoveUnsafe") + ".rdz.tcbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void ParseSetBoolProcedure()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "SetBoolProcedure") + ".rdz.tcbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void ParseVariableTypeExternal()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "VariableTypeExternal") + ".rdz.tcbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void ProcPublicWithoutDataDivision()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "ProcPublicWithoutDataDivision") + ".rdz.tcbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void TypedefFixedFormatCutLine()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "TypedefFixedFormatCutLine") + ".rdz.tcbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void TypedefQualifBoolSet()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "TypedefQualifBoolSet") + ".rdz.tcbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void TypedefQualifBoolSetOverCol72()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "TypedefQualifBoolSetOverCol72") + ".rdz.tcbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void TypedefQualifMultipleBoolSet()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "TypedefQualifMultipleBoolSet") + ".rdz.tcbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void Non72ColumnOverflowCheck()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "Non72ColumnOverflowCheck") + ".rdz.tcbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void TypedefQualifDependOn()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "TypedefQualifDependOn") + ".rdz.tcbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void TypedefQualifDependOn2()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "TypedefQualifDependOn2") + ".rdz.tcbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void TypedefQualifDependOn3()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "TypedefQualifDependOn3") + ".rdz.tcbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void TypedefQualifDependOn4()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "TypedefQualifDependOn4") + ".rdz.tcbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void TypedefQualifDependOn5()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "TypedefQualifDependOn5") + ".rdz.tcbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void TypedefQualifDependOn6()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "TypedefQualifDependOn6") + ".rdz.tcbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void TypedefQualifDependOn7()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "TypedefQualifDependOn7") + ".rdz.tcbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void TypedefQualifDependOn8()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "TypedefQualifDependOn8") + ".rdz.tcbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void TypedefQualifIndexedBy()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "TypedefQualifIndexedBy") + ".rdz.tcbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void TypedefQualifIndexedBy1()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "TypedefQualifIndexedBy1") + ".rdz.tcbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void TypedefQualifIndexedBy2()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "TypedefQualifIndexedBy2") + ".rdz.tcbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void TypedefQualifIndexedBy3()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "TypedefQualifIndexedBy3") + ".rdz.tcbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void TypedefQualifIndexedBy4()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "TypedefQualifIndexedBy4") + ".rdz.tcbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void TypedefQualifIndexedBy5()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "TypedefQualifIndexedBy5") + ".rdz.tcbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void TypedefQualifIndexedBy6()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "TypedefQualifIndexedBy6") + ".rdz.tcbl", skeletons);
        }


        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void TypedefQualifIndexedBy7()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "TypedefQualifIndexedBy7") + ".rdz.tcbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void TypedefQualifIndexedBy8()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "TypedefQualifIndexedBy8") + ".rdz.tcbl", skeletons);
        }
        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
#if DOMAIN_CHECKER
        [Ignore] //This test has detected inconsitency in TypeCobol Typedef declared in nested procedures: DVZF0OS3.Imbrique.EventList.Counter
#endif
        public void TypedefQualifIndexedBy9()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "TypedefQualifIndexedBy9") + ".rdz.tcbl", skeletons);
        }
        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void TypedefQualifIndexedBy10()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "TypedefQualifIndexedBy10") + ".rdz.tcbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void TypedefQualifIndexedBy11()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "TypedefQualifIndexedBy11") + ".rdz.tcbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void TypedefQualifIndexedBy12()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "TypedefQualifIndexedBy12") + ".rdz.tcbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void BooleanTester()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "BooleanTester") + ".rdz.tcbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void GlobalBoolean()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "GlobalBoolean") + ".rdz.tcbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void DeclarativesTest()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "Declaratives") + ".rdz.tcbl", skeletons);
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void TypeCobolVersionTest()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "TCOBVersion") + ".rdz.tcbl", skeletons, false, "TestTypeCobolVersion");
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void ProcedureSubscriptTest()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "ProcedureSubscript") + ".rdz.tcbl", skeletons, false, "TestTypeCobolVersion");
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void PointersTest()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "Pointers") + ".rdz.tcbl", skeletons, false, "TestTypeCobolVersion");
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void ProgramParameterCompUsageTypesTest()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "ProgramParameterCompUsageTypes") + ".rdz.tcbl", skeletons, false, "TestTypeCobolVersion");
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void CobolLineSplit_IN_VarTest()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "CobolLineSplit_IN_Var") + ".rdz.tcbl", skeletons, false, "TestTypeCobolVersion");
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void CobolLineSplit_IN_Var1Test()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "CobolLineSplit_IN_Var1") + ".rdz.tcbl", skeletons, false, "TestTypeCobolVersion");
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void CobolLineSplit_IN_Var2Test()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "CobolLineSplit_IN_Var2") + ".rdz.tcbl", skeletons, false, "TestTypeCobolVersion");
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void GenQualifiedBoolVarTest()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "GenQualifiedBoolVar") + ".rdz.tcbl", skeletons, false, "TestTypeCobolVersion");
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void GenMoveCorrTest()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "GenMoveCorr") + ".rdz.tcbl", skeletons, false, "TestTypeCobolVersion");
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void GenCorrStatementsTest()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "GenCorrStatements") + ".rdz.tcbl", skeletons, false, "TestTypeCobolVersion");
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void GenContinueInsideTypdefTest()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "GenContinueInsideTypdef") + ".rdz.tcbl", skeletons, false, "TestTypeCobolVersion");
        }


        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void Gen_73_80_RemoveTextTest()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "Gen_73_80_RemoveText") + ".rdz.tcbl", skeletons, false, "TestTypeCobolVersion");
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void GenGlobalKeywordTest()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "GenGlobalKeyword") + ".rdz.tcbl", skeletons, false, "TestTypeCobolVersion");
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void SetUnsafeTest()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "SetUnsafe") + ".rdz.tcbl", skeletons, false, "TestTypeCobolVersion");
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void GenTCobVersionAfterOptionsTest()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "GenTCobVersionAfterOptions") + ".rdz.cbl", skeletons, false, "TestTypeCobolVersion");
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void CopyReplaceInProcLinkage()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "CopyReplaceInProcLinkage") + ".rdz.tcbl", skeletons, false, "TestTypeCobolVersion");
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void MisPlaceCopyInstrWithProcMetaDataTest()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "MisPlaceCopyInstrWithProcMetaData") + ".rdz.tcbl", skeletons, false, "TestTypeCobolVersion");
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void MisPlaceCopyInstrWithProcMetaDataTest2()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "MisPlaceCopyInstrWithProcMetaData2") + ".rdz.tcbl", skeletons, false, "TestTypeCobolVersion");
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void DeclarativesWithProcedures()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "DeclarativesWithProcedures") + ".tcbl", skeletons, false, "TestTypeCobolVersion");
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void DeclarativesWithProcedures2()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "DeclarativesWithProcedures2") + ".tcbl", skeletons, false, "TestTypeCobolVersion");
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void DeclarativesWithInstructionsWithinTest()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "DeclarativesWithInstructionsWithin") + ".rdz.tcbl", skeletons, false, "TestTypeCobolVersion");
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void FormalizedCommentsTest()
        {
            var skeletons = CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml");
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "FormalizedComments") + ".tcbl", skeletons, false, "TestTypeCobolVersion");
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void MultilinesCommentsTest()
        {
            var skeletons = CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml");
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "MultilinesComments") + ".tcbl", skeletons, false, "TestTypeCobolVersion");
        }

        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void MoveUnsafeToQualifiedInsideFunction()
        {
            var skeletons = CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml");
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "MoveUnsafeToQualifiedInsideFunction") + ".rdz.tcbl", skeletons, false, "TestTypeCobolVersion");
        }

#if EUROINFO_RULES
        [TestMethod]
        [TestCategory("DomainCodegen")]
        [TestProperty("Time", "fast")]
        public void RemarksGeneration()
        {
            var skeletons = UseSkeleton ? CodegenTestUtils.ParseConfig(Path.Combine("TypeCobol", "skeletons") + ".xml") : null;
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "Remarks", "RemarksLess") + ".rdz.cbl", skeletons, true);
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "Remarks", "RemarksPartial") + ".rdz.cbl", skeletons, true);
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "Remarks", "RemarksNonUsed") + ".rdz.cbl", skeletons, true);
            CodegenTestUtils.ParseGenerateCompare(Path.Combine("TypeCobol", "Remarks", "NestedProgram") + ".rdz.cbl", skeletons, true);
        }
#endif
    }
}
