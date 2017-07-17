﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Web.Configuration;
using System.Xml;

namespace Grade
{
    public class Program
    {
        static Process proc;
        static ProcessStartInfo procInfo;
        private static string javaPath = "C:/Program Files/Java/jdk1.8.0_45/bin/java.exe";
        private static string pythonPath = "C:/Python36/python.exe";
        private static Dictionary<decimal, List<object>> aaaaa = new Dictionary<decimal, List<object>>();

        public Program()
        {

        }

        public static void Main(string[] args)
        {
        }

        public static decimal Grading(string[] args)
        {
            string pathToExe = "", testcase = "", solution = "",solutionScoreKey = "";//all filepaths
            string ans = "", error = "", subOut = "", solOut = "", language;//string outputs
            List<String> subList = new List<String>();
            decimal testCasePassed = 0;
            int exitcode;
            int noOfTestCase = 0;
            List<object> testcases = new List<object>();
            List<string> answers = new List<string>();
            XmlDocument slnDoc = new XmlDocument();

            pathToExe = args[0];
            testcase = args[1];
            solution = args[2];
            language = args[3];
            solutionScoreKey = args[4];

            if (language == "Java")
            {
                var url = ConfigurationManager.AppSettings["JavaPath"];
                proc = new Process();
                //procInfo = new ProcessStartInfo("C:/Program Files/Java/jdk1.8.0_101/bin/java.exe", pathToExe);
                procInfo = new ProcessStartInfo(javaPath, pathToExe);
            }
            else if (language == "C#")
            {
                proc = new Process();
                procInfo = new ProcessStartInfo(pathToExe);
            }
            else if (language == "Python")
            {
                proc = new Process();
                procInfo = new ProcessStartInfo(pythonPath, pathToExe);
            }
            else
            {
                return 5;
            }

            procInfo.CreateNoWindow = true;
            procInfo.UseShellExecute = false;

            //redirect standard output and error
            procInfo.RedirectStandardError = true;
            procInfo.RedirectStandardOutput = true;
            procInfo.RedirectStandardInput = true;

            //load test cases if any
            XmlDocument testCaseFile = new XmlDocument();
            XmlDocument solutionFile = new XmlDocument();

            List<object> d = new List<object>();

            try
            {
                testCaseFile.Load(testcase);
                XmlNodeList testcaseList = testCaseFile.SelectNodes("/body/testcase");

                foreach (XmlNode node in testcaseList)
                {
                    List<string> inputs = new List<string>();

                    noOfTestCase++;

                    try
                    {
                        proc = Process.Start(procInfo); //cannot find compiled program
                    }
                    catch (Exception excp)
                    {
                        return 4;
                    }

                    subOut = "";

                    System.IO.StreamWriter sw = proc.StandardInput;

                    foreach (XmlNode input in node.ChildNodes)
                    {
                        inputs.Add(input.InnerText);
                        sw.WriteLine(input.InnerText);
                        sw.Flush();
                    }//end of inputs



                    if (!proc.WaitForExit(20000))
                    {
                        proc.Kill();
                        return 3;
                    }
                    proc.WaitForExit();


                    //check if there is another error thrown by program
                    error = proc.StandardError.ReadToEnd();

                    string checkEmpty;
                    if (error.Equals(""))
                    {
                        //scan through all lines of standard output to retrieve anything
                        
                        do
                        {
                            checkEmpty = proc.StandardOutput.ReadLine();
                            subOut += checkEmpty;
                        } while (checkEmpty != null);

                        foreach (string input in inputs)
                        {
                            subOut += input;
                        }
                    }
                    else
                    {
                        checkEmpty = proc.StandardError.ReadLine();
                    }//check if error

                    //get the output from solution
                    solutionFile.Load(solution);

                    XmlNodeList solutions = solutionFile.SelectNodes("/body/solution");
                    //loop through all the solutions to find matching
                    //foreach (XmlNode sol in solutions)
                    //{
                    //    if (subOut.Equals(sol.InnerText))
                    //    {
                    //        testCasePassed++;
                    //        break;
                    //    }
                    //}//end of foreach loop
                    

                    if (subOut.Equals(solutions[noOfTestCase - 1].InnerText))
                    {
                        testCasePassed++;
                        d.Add(new
                        {
                            a = solutions[noOfTestCase - 1].InnerText,
                            b = 1
                        });
                    }
                    else
                    {
                        d.Add(new
                        {
                            a = solutions[noOfTestCase - 1].InnerText,
                            b = 0
                        });
                    }

                    if (!proc.WaitForExit(10000))
                    {
                        proc.Kill();
                        return 3;
                    }
                }//end of test case loop

                aaaaa.Add(Int32.Parse(solutionScoreKey),d);

                return (testCasePassed / noOfTestCase);
            }
            catch (Exception e) //when exception occures means failed to retrieve testcase, in turn means program does not take in inputs
            {//start catch, application does not accept input

                try
                {
                    proc = Process.Start(procInfo);
                }
                catch (Exception exc)
                {
                    return 2;
                }

                if (!proc.WaitForExit(10000))
                {
                    proc.Kill();
                    return 3;
                }

                proc.WaitForExit();

                //read output and error
                error = proc.StandardError.ReadToEnd();
                exitcode = proc.ExitCode; //0 means success 1 means failure

                //get output from submission

                ans = proc.StandardOutput.ReadToEnd();

                //get the output from solution
                //get the output from solution
                solutionFile.Load(solution);
                solOut = solutionFile.SelectSingleNode("/body/solution").InnerText;

                if (exitcode == 0 && error.Equals("")) //if submission properly ran and produced desired outcome
                {
                    if (ans.Equals(solOut))
                    {
                        return 1;
                    }
                    else
                    {
                        return 0;
                    }
                }
                else //means fail
                {
                    return 2;
                }
            }//end of catch
        }

        public static decimal createSolution(string[] args)
        {
            XmlNode docNode, bodyNode, solutionsNode;
            string pathToExe = "", pathToSol = "", pathToTestCase = "", hasTestCase = "", subOut = "", error="";
            string language = "";
            bool programFailed = false;
            XmlDocument slnDoc = new XmlDocument();

            pathToExe = args[0];
            language = args[1];
            pathToSol = args[2];
            pathToTestCase = args[3];

            if (language == "Java")
            {

                proc = new Process();
                //procInfo = new ProcessStartInfo("C:/Program Files/Java/jdk1.8.0_91/bin/java.exe", pathToExe);
                procInfo = new ProcessStartInfo(javaPath, pathToExe);
            }
            else if (language == "C#")
            {
                proc = new Process();
                procInfo = new ProcessStartInfo(pathToExe);
            }
            else if (language == "Python")
            {
                proc = new Process();
                procInfo = new ProcessStartInfo(pythonPath, pathToExe);
            }
            else
            {
                return 5;
            }

            procInfo.CreateNoWindow = true;
            procInfo.UseShellExecute = false;

            //redirect standard output and error
            procInfo.RedirectStandardError = true;
            procInfo.RedirectStandardOutput = true;
            procInfo.RedirectStandardInput = true;

            if (pathToTestCase.Equals(""))
            {
                //no test case practical
                try
                {
                    //create part of the solution file first
                    docNode = slnDoc.CreateXmlDeclaration("1.0", "UTF-8", null);
                    slnDoc.AppendChild(docNode);
                    bodyNode = slnDoc.CreateElement("body");
                    slnDoc.AppendChild(bodyNode);


                    //start the process 
                    proc = Process.Start(procInfo);


                    //to delete process if it enters an infinite loop
                    if (!proc.WaitForExit(40000))
                        {
                        proc.Kill();
                        return 3;
                    }

                    proc.WaitForExit();
                    error = proc.StandardError.ReadToEnd();

                    //read the output from the program
                    if (error.Equals(""))
                    {
                        subOut = proc.StandardOutput.ReadToEnd();

                        solutionsNode = slnDoc.CreateElement("solution");
                        solutionsNode.AppendChild(slnDoc.CreateTextNode(subOut));
                        bodyNode.AppendChild(solutionsNode);
                    }
                    else
                    {
                        //program given fail if an error was encountered
                        proc.Kill();
                        programFailed = true;
                        return 3;
                    }
                    //proc.WaitForExit();

                    //store into the solutions file if no error
                    if (programFailed == false)
                    {
                        //save the XML file
                        var fP = pathToSol;
                        slnDoc.Save(fP);
                        //solution has run successfully
                        return 1;
                    }
                }
                catch (Exception e)
                {
                    //any exception
                    return 3;
                }
            }

            //run the appropirate method based off a testcase is present or not
            //load test cases if any
            XmlDocument testCaseFile = new XmlDocument();
            XmlDocument solutionFile = new XmlDocument();

            try
            {
                //load test case
                testCaseFile.Load(pathToTestCase);
                XmlNodeList testcaseList = testCaseFile.SelectNodes("/body/testcase");

                //create part of the solution file first
                docNode = slnDoc.CreateXmlDeclaration("1.0", "UTF-8", null);
                slnDoc.AppendChild(docNode);
                bodyNode = slnDoc.CreateElement("body");
                slnDoc.AppendChild(bodyNode);

                foreach (XmlNode testcase in testcaseList)
                {
                    List<string> inputs = new List<string>();
                    // noOfTestCase++;
                    proc = Process.Start(procInfo);
                    subOut = "";

                    System.IO.StreamWriter sw = proc.StandardInput;
                    
                    foreach (XmlNode input in testcase.ChildNodes)
                    {
                        inputs.Add(input.InnerText);
                        sw.WriteLine(input.InnerText);
                        sw.Flush();
                    }//end of inputs

                    //check if there is another error thrown by program
                    error = proc.StandardError.ReadToEnd();

                    if (error.Equals(""))
                    {
                        //scan through all lines of standard output to retrieve anything
                        string checkEmpty;
                        do
                        {
                            checkEmpty = proc.StandardOutput.ReadLine();
                            subOut += checkEmpty;

                        } while (checkEmpty != null);

                        foreach (string input in inputs)
                        {
                            subOut += input;
                        }

                        solutionsNode = slnDoc.CreateElement("solution");
                        solutionsNode.AppendChild(slnDoc.CreateTextNode(subOut));
                        bodyNode.AppendChild(solutionsNode);
                    }
                    else
                    {
                        //program given fail if an error was encountered
                        programFailed = true;
                        sw.Close();
                        break; //break out of loop
                    }
                    if (!proc.WaitForExit(10000))
                    {
                        proc.Kill();
                        return 4;
                    }
                    //proc.WaitForExit();
                }

                //create the solution file 
                if (programFailed == false)
                {
                    //save the XML file
                    var fP = pathToSol;
                    slnDoc.Save(fP);

                    //solution has run successfully
                    return 1;
                }
                else
                {
                    proc.Kill();
                    return 3;
                }
            }
            catch (Exception ex)
            {
                //test case cannot be read
                return 2;
            }
        }//end of createSolution

    }
}
