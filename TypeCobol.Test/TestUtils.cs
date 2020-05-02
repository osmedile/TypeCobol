using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Antlr4.Runtime.Misc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TypeCobol.Compiler;

namespace TypeCobol.Test
{
    public class TestUtils
    {

        //folder name for test results
        private static string _report = "PerformanceReports";

        /// <summary>
        /// Compare result and expectedResult line by line.
        /// If there is at least one difference, throw an exception for the test named by the parameter testName or 
        /// Replace ExpectedResult content if content is different and boolean "autoReplace" is true
        /// </summary>
        /// <param name="testName">Name of the test</param>
        /// <param name="result"></param>
        /// <param name="expectedResult"></param>
        /// <param name="expectedResultPath"></param>
        /// <returns></returns>
        public static void compareLines(string testName, string result, string expectedResult, string expectedResultPath)
        {
            StringBuilder errors = new StringBuilder();

            //Set to true to automatically replace content in ExpectedResult File
            bool autoReplace = false;

            if (testName == string.Empty && result == string.Empty && expectedResult == string.Empty &&
                expectedResultPath == string.Empty)
            {
                if (autoReplace)
                    Assert.Fail("Set AutoReplace to false in TestUtils.compareLines()\n\n");
            }

            result = Regex.Replace(result, "(?<!\r)\n", "\r\n");
            expectedResult = Regex.Replace(expectedResult, "(?<!\r)\n", "\r\n");

            String[] expectedResultLines = expectedResult.Split('\r', '\n');
            String[] resultLines = result.Split('\r', '\n');

            var linefaults = new List<int>();
            for (int c = 0; c < resultLines.Length && c < expectedResultLines.Length; c++)
            {
                if (expectedResultLines[c] != resultLines[c]) linefaults.Add(c / 2 + 1);
            }

            if (result != expectedResult)
            {
                if (autoReplace && expectedResultPath != null)
                {
                    replaceLines(result, expectedResultPath);
                    errors.AppendLine("result != expectedResult  In test:" + testName);
                    errors.AppendLine("at line" + (linefaults.Count > 1 ? "s" : "") + ": " + string.Join(",", linefaults));
                    errors.AppendLine("Output file has been modified\n");
                    errors.AppendLine("Please rerun unit test\n");
                }
                else
                {
                    errors.Append("result != expectedResult  In test:" + testName)
                        .AppendLine(" at line" + (linefaults.Count > 1 ? "s" : "") + ": " + string.Join(",", linefaults));
                    errors.AppendLine("See TestUtils.cs compareLines method to autoreplace ExpectedResult");
                    errors.Append("=== RESULT ==========\n" + result + "====================");
                }
                throw new Exception(errors.ToString());

            }
        }

        private static void replaceLines(string result, string expectedResultPath)
        {
            using (StreamWriter writer = new StreamWriter(expectedResultPath))
            {
                writer.Write(result);
            }

        }

        public static string GetReportDirectoryPath()
        {
            return Path.Combine(Directory.GetCurrentDirectory(), _report);
        }


        

        private static string FormatPercentage(double averageTime, double totalTime)
        {
            return $" ({(averageTime * 100 / totalTime),5:#0.00} %)";
        }

        public static float Median(IList<int> numbers)
        {
            int numberCount = numbers.Count();
            int halfIndex = numbers.Count() / 2;
            var sortedNumbers = numbers.OrderBy(n => n).ToArray();
            if ((numberCount % 2) == 0)
            {
                return (sortedNumbers[halfIndex] + sortedNumbers[halfIndex-1]) / 2;
            }
            else
            {
                return sortedNumbers[halfIndex];
            }
        }

        public static float Median(IList<long> numbers)
        {
            int numberCount = numbers.Count();
            int halfIndex = numbers.Count() / 2;
            var sortedNumbers = numbers.OrderBy(n => n).ToArray();
            if ((numberCount % 2) == 0)
            {
                return (sortedNumbers[halfIndex] + sortedNumbers[halfIndex - 1]) / 2;
            }
            else
            {
                return sortedNumbers[halfIndex];
            }
        }


        public class CompilationStats
        {
            public CompilationStats()
            {
                IterationNumber = 0;
                
                Line = 0;
                TotalCodeElements = 0;
            }

            public float IterationNumber { get; set; }


            public List<int> TextUpdateTime { get; } = new List<int>();
            public List<int> ScannerTime { get; } = new List<int>();
            public List<int> PreprocessorTime { get; } = new List<int>();
            public List<int> CodeElementParserTime { get; } = new List<int>();
            public List<int> TemporarySemanticsParserTime { get; } = new List<int>();
            public List<int> CrossCheckerParserTime { get; } = new List<int>();
            public List<int> TotalProcessingTime { get; } = new List<int>();
            public List<long> CheckErrors { get; } = new List<long>();

            //Number of lines in Cobol file
            public int Line { get; set; }
            //Number of CodeElements found during the parsing
            public int TotalCodeElements { get; set; }

            public void AccumulateResults(CompilationUnit compilationUnit, long timeToCheckErrors)
            {
                Line = compilationUnit.CobolTextLines.Count;
                TotalCodeElements = compilationUnit.CodeElementsDocumentSnapshot.CodeElements.Count();

                this.TextUpdateTime.Add(compilationUnit.PerfStatsForText.LastRefreshTime);
                this.ScannerTime.Add(compilationUnit.PerfStatsForScanner.LastRefreshTime);
                this.PreprocessorTime.Add(compilationUnit.PerfStatsForPreprocessor.LastRefreshTime);
                this.CodeElementParserTime.Add(compilationUnit.PerfStatsForCodeElementsParser.LastRefreshTime);
                this.TemporarySemanticsParserTime.Add(compilationUnit.PerfStatsForTemporarySemantic.LastRefreshTime);
                this.CrossCheckerParserTime.Add(compilationUnit.PerfStatsForProgramCrossCheck.LastRefreshTime);

                this.CheckErrors.Add(timeToCheckErrors);

                this.TotalProcessingTime.Add(compilationUnit.PerfStatsForText.LastRefreshTime
                                              + compilationUnit.PerfStatsForScanner.LastRefreshTime
                                              + compilationUnit.PerfStatsForPreprocessor.LastRefreshTime
                                              + compilationUnit.PerfStatsForCodeElementsParser.LastRefreshTime
                                              + compilationUnit.PerfStatsForTemporarySemantic.LastRefreshTime
                                              + compilationUnit.PerfStatsForProgramCrossCheck.LastRefreshTime);
            }


            public void ExportToCSV(string reportName, string localDirectoryFullName, string cobolFileName, string separator = "\t")
            {
                var testName = reportName + "_" + cobolFileName.Split('.')[0];

                // Display a performance report
                StringBuilder report = new StringBuilder();
                AppendColumns("TestName", "Property", "value");
                AppendColumns(testName, "Lines", Line.ToString(), "", "", "");
                AppendColumns(testName, "TotalCodeElements", TotalCodeElements.ToString(), "", "", "");
                AppendColumns(testName, "Iteration times", IterationNumber.ToString(CultureInfo.InvariantCulture), "", "", "");

                AppendColumns("TestName", "phase", "average time", "average percent", "median", "median percentage");
                FormatLine("text update", TextUpdateTime);
                FormatLine("scanner", ScannerTime);
                FormatLine("preprocessor", PreprocessorTime);
                FormatLine("code elements", CodeElementParserTime);
                FormatLine("Node/Symbold", TemporarySemanticsParserTime);
                FormatLine("Cross check", CrossCheckerParserTime);
                FormatLineLong("Check errors", CheckErrors);
                FormatLine("Total time", TotalProcessingTime);
                
                //report.AppendLine("Total median time: " + Median(stats.TotalProcessingTime).ToString("##0.00") + " ms");

                var reportFile = testName + "_" + DateTime.Now.ToString("yyyMMdd_HH_mm_ss") + ".txt";
                Directory.CreateDirectory(GetReportDirectoryPath());
                File.WriteAllText(Path.Combine(localDirectoryFullName, reportFile), report.ToString());
                //Console.WriteLine(report.ToString());


                void FormatLine(string phase, IList<int> numbers)
                {
                    AppendColumns(testName, phase, numbers.Average().ToString(), (numbers.Average() * 100 / TotalProcessingTime.Average()) + "%",
                                                 Median(numbers).ToString(), (Median(numbers) * 100 / Median(TotalProcessingTime)) + "%");
                    //report.AppendLine("" + separator + phase + separator + "${time,10:#####0.00} ms " + FormatPercentage(time, totalTime) );
                }

                void FormatLineLong(string phase, IList<long> numbers)
                {
                    AppendColumns(testName, phase, numbers.Average().ToString(), (numbers.Average() * 100 / TotalProcessingTime.Average()) + "%",
                        Median(numbers).ToString(), (Median(numbers) * 100 / Median(TotalProcessingTime)) + "%");
                }

                void AppendColumns(params string[] columns)
                {
                    foreach (var col  in columns)
                    {
                        report.Append(col + separator);
                    }
                    report.AppendLine();
                }
            }


            public void CreateRunReport(string reportName, string localDirectoryFullName, string cobolFileName, bool incremental)
            {

                // Display a performance report
                StringBuilder report = new StringBuilder();
                report.AppendLine("Program properties :");

                report.AppendLine("- " + Line + " lines");
                report.AppendLine("- " + TotalCodeElements + " code elements");

                report.AppendLine(" Iteration : " + IterationNumber);

                report.AppendLine("");
                report.AppendLine(incremental
                    ? "Incremental compilation performance (average time)"
                    : "Full compilation performance (average time)");

                FormatLine(TextUpdateTime.Average(), TotalProcessingTime.Average(), "text update");
                FormatLine(ScannerTime.Average(), TotalProcessingTime.Average(), "scanner");
                FormatLine(PreprocessorTime.Average(), TotalProcessingTime.Average(), "preprocessor");
                FormatLine(CodeElementParserTime.Average(), TotalProcessingTime.Average(), "code elements");
                FormatLine(TemporarySemanticsParserTime.Average(), TotalProcessingTime.Average(), "Node/Symbol");
                FormatLine(CrossCheckerParserTime.Average(), TotalProcessingTime.Average(), "cross check");
                FormatLine(CheckErrors.Average(), TotalProcessingTime.Average(), "Check errors");

                report.AppendLine("Total average time: " + TotalProcessingTime.Average().ToString("##0.00") + " ms");

                //-------------------
                report.AppendLine("");
                report.AppendLine(incremental
                    ? "Incremental compilation performance (median)"
                    : "Full compilation performance (median)");

                FormatLine(Median(TextUpdateTime), Median(TotalProcessingTime), "text update");
                FormatLine(Median(ScannerTime), Median(TotalProcessingTime), "scanner");
                FormatLine(Median(PreprocessorTime), Median(TotalProcessingTime), "preprocessor");
                FormatLine(Median(CodeElementParserTime), Median(TotalProcessingTime), "code elements");
                FormatLine(Median(TemporarySemanticsParserTime), Median(TotalProcessingTime), "Node/Symbol");
                FormatLine(Median(CrossCheckerParserTime), Median(TotalProcessingTime), "cross check");
                FormatLine(Median(CheckErrors), Median(TotalProcessingTime), "Check errors");

                report.AppendLine("Total median time: " + Median(TotalProcessingTime).ToString("##0.00") + " ms");





                var reportFile = reportName + "_" + cobolFileName.Split('.')[0] + "_" +
                                    DateTime.Now.ToString("yyyMMdd_HH_mm_ss") + ".txt";
                Directory.CreateDirectory(GetReportDirectoryPath());
                File.WriteAllText(Path.Combine(localDirectoryFullName, reportFile), report.ToString());
                Console.WriteLine(report.ToString());


                void FormatLine(double time, double totalTime, string text)
                {
                    report.AppendLine($"{time,10:#####0.00} ms " + FormatPercentage(time, totalTime) + " " + text);
                }
            }
        }
    }
}
