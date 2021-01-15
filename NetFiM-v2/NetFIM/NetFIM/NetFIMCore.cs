using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using NetFIM.Class;
using NetFIM.Methods;
using NetFIM.Global;

namespace NetFIM.Core
{
    class NetFIMInterpreter
    {
        
        private FiMReport Report;

        public string ReportName { get { return Report.ReportName; } }
        public string Writer { get { return Report.Writer; } }

        public Dictionary<string, FiMParagraph> Paragraphs { get { return Report.Paragraphs; } }
        public Dictionary<string, FiMObject> GlobalVariables { get { return Report.GlobalVariables; } }
        
        // https://stackoverflow.com/questions/12231569/is-there-in-c-sharp-a-method-for-listt-like-resize-in-c-for-vectort
        private T[] Resize<T>(T[] array, T element)
        {
            List<T> l = new List<T>();
            l.AddRange(array);
            //
            int c = array.Length;
            //
            if (c > 64)
                l.RemoveRange(64, c - 64);
            else if(c < 64)
            {
                if (64 > l.Capacity)
                    l.Capacity = 64;
                l.AddRange(Enumerable.Repeat(element, 64 - c));
            }
            return l.ToArray();
        }

        public void AddVariable(string key, string value, bool isConstant = false)
            => Report.GlobalVariables.Add(key, new FiMObject(value, FiMObjectTypes.STRING, isConstant, false));
        public void AddVariable(string key, int value, bool isConstant = false)
            => Report.GlobalVariables.Add(key, new FiMObject(value, FiMObjectTypes.NUMBER, isConstant, false));
        public void AddVariable(string key, bool value, bool isConstant = false)
            => Report.GlobalVariables.Add(key, new FiMObject(value, FiMObjectTypes.BOOL, isConstant, false));
        public void AddVariable(string key, string[] value, bool isConstant = false)
            => Report.GlobalVariables.Add(key, new FiMObject(Resize(value, null), FiMObjectTypes.STRING_ARRAY, isConstant, true));
        public void AddVariable(string key, bool[] value, bool isConstant = false)
            => Report.GlobalVariables.Add(key, new FiMObject(Resize(value, false), FiMObjectTypes.BOOL_ARRAY, isConstant, true));
        public void AddVariable(string key, float[] value, bool isConstant = false)
            => Report.GlobalVariables.Add(key, new FiMObject(Resize(value, 0), FiMObjectTypes.NUMBER_ARRAY, isConstant, true));

        // READ + RUN MAIN FUNCTION
        public dynamic InterpretReport(string path)
        {
            ReadReport(path);

            if(!Report.Paragraphs.Any(x => x.Value.isMainFunction))
                throw new Exception("Main paragraph not found");

            return Report.RunParagraph( Report.Paragraphs.Where(x => x.Value.isMainFunction).FirstOrDefault().Key );
        }

        // READ FILE CONTENT
        public void ReadReport(string path)
        {
            Report = new FiMReport();

            // CHECKS
            if (!Directory.Exists(path))
            {
                throw new FiMException("Directory doesn't exist!");
            }
            if (!File.Exists(path + "/main.fpp"))
            {
                throw new FiMException("main.fpp doesn't exist!");
            }
            //

            List<string> lines = File.ReadAllLines(path + "/main.fpp").ToList();
            #region Read main.fpp File

            try
            {
                lines = NetFIMMethods.SanitizeReport(lines);
            }
            catch (Exception e)
            {
                throw e;
            }

            // lets be strict on the report
            if (!lines.First().StartsWith(NetFIMGlobal.ReportStart))
                throw new Exception("Invalid Report beginning");
            else
                Report.ReportName = lines.First().Remove(0, NetFIMGlobal.ReportStart.Length);
            
            lines = NetFIMMethods.RemoveLinesAfterReportEnd(lines);
            if (!lines.Last().StartsWith(NetFIMGlobal.ReportEnd))
                throw new Exception("Invalid Report ending");
            else
                Report.Writer = lines.Last().Remove(0, NetFIMGlobal.ReportEnd.Length);

            // Grab global variables
            bool gv_inside = false;
            for(int i = 0; i < lines.Count; i++)
            {
                string line = lines[i];
                if (line.Replace("Today ", "").StartsWith("I learned "))
                    gv_inside = true;
                if (line.StartsWith("That's all about "))
                    gv_inside = false;

                if(!gv_inside)
                {
                    if(line.StartsWith(NetFIMGlobal.Keywords.Method_Declare))
                    {
                        string variableName;
                        FiMObject obj;
                        try { obj = NetFIMMethods.InterpretVariableDecleration(line, out variableName, Report.GlobalVariables); }
                        catch (Exception e) { throw new Exception("[" + i + "] " + e.Message); }

                        if (Report.GlobalVariables.ContainsKey(variableName))
                            throw new Exception("A global variable with the name '" + variableName + "' already exists.");

                        Report.GlobalVariables.Add(variableName, obj);
                    }
                }
            }

            // Grab functions
            for(int i = 0; i < lines.Count(); i++)
            {
                string line = lines[i];
                if(line.Replace("Today ","").StartsWith("I learned "))
                {
                    string funcName = line.Replace("Today ", "").Replace("I learned ", "");

                    FiMObjectTypes funcType = FiMObjectTypes.UNKNOWN;
                    FiMObjectTypes retrType = FiMObjectTypes.UNKNOWN;
                    string retrName = null;
                    
                    if(NetFIMGlobal.Keywords.Method_Return.Any(x => funcName.Contains(x)) || funcName.Contains(NetFIMGlobal.Keywords.Method_Parameter))
                    {
                        if(NetFIMGlobal.Keywords.Method_Return.Any(x => funcName.Contains(x)))
                        {
                            string r_ph = NetFIMGlobal.Keywords.Method_Return.Where(x => funcName.Contains(x)).FirstOrDefault();
                            string r_ap;
                            try
                            {
                                funcType = NetFIMMethods.GetVariableInitializerType(funcName.Split(new string[] { r_ph }, StringSplitOptions.None)[1].TrimStart(' ').TrimEnd(' '), out r_ap);
                            }
                            catch(Exception ex) { throw new Exception("Invalid Paragraph return - " + ex); }
                            //
                            funcName = funcName.Replace(" " + r_ph + " " + r_ap, "");
                        }
                        if (funcName.Contains(NetFIMGlobal.Keywords.Method_Parameter))
                        {
                            string p_ap;
                            try
                            {
                                retrType = NetFIMMethods.GetVariableInitializerType(funcName.Split(new string[] { NetFIMGlobal.Keywords.Method_Parameter }, StringSplitOptions.None)[1].TrimStart(' ').TrimEnd(' '), out p_ap);
                                retrName = funcName.Split(new string[] { NetFIMGlobal.Keywords.Method_Parameter + " " + p_ap }, StringSplitOptions.None)[1].TrimStart(' ');
                            }
                            catch { throw new Exception("Invalid Paragraph parameter"); }
                            //
                            funcName = funcName.Split(new string[] { NetFIMGlobal.Keywords.Method_Parameter + " " + p_ap }, StringSplitOptions.None)[0].TrimEnd(' ');
                        }
                    }

                    if (NetFIMGlobal.KeywordsList.Any(x => funcName.Contains(x)))
                        throw new Exception("Paragraph name '"+funcName+"' has broken one or more restrictions");

                    List<string> iLines = new List<string>();

                    int il = i+1;
                    bool ibrk = false;
                    while(!ibrk)
                    {

                        string inn = lines[il];

                        if (inn.StartsWith("That's all about "))
                        {
                            // might also want to do the check here
                            if (inn.Substring("That's all about ".Length) != funcName)
                                throw new Exception("Paragraph '" + funcName + "' doesn't end with the same name");
                            ibrk = true;
                        }
                        else
                            iLines.Add(inn);

                        il++;

                    }

                    if(!ibrk)
                        throw new Exception("Paragraph '" + funcName + "' doesn't end properly");

                    FiMParagraph paragraph = new FiMParagraph(iLines, i, line.StartsWith("Today "), returnType: funcType, paramType: retrType, paramName: retrName);

                    if (Report.Paragraphs.Any(x => x.Value.isMainFunction == true && line.StartsWith("Today ")))
                        throw new Exception("Multiple main paragraphs found");

                    Report.Paragraphs.Add(funcName, paragraph);

                    i = il;
                }
            }

            #endregion
        }

    }

    //
    [Serializable]
    class FiMException : Exception
    {
        public FiMException() { }
        public FiMException(string message) : base(message) { }
        public FiMException(string message, Exception inner) : base(message, inner) { }
    }
}
