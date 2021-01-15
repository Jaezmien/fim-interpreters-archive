using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using NetFIM.Class;
using NetFIM.Global;

using MoonSharp.Interpreter;

namespace NetFIM.Methods
{
    class NetFIMMethods
    {

        public static void WriteToScreen(dynamic value)
        {
            if(value is string)
            {
                Console.WriteLine(Regex.Replace(value, @"(?<!\\)\\n", "\n"));
            }
            else
            {
                Console.WriteLine(value);
            }
        }

        private static bool DoLuaComparison(string l, string m, string r)
        {
            DynValue res = Script.RunString(l + " " + m + " " + r);
            return res.Boolean;
        }

        //

        public static List<string> SanitizeReport(List<string> oldLines)
        {
            List<string> newLines = new List<string>();

            int _l = 1;
            foreach(string line in oldLines)
            {
                string l = line.Replace('“', '"').Replace('”', '"');

                if(l.Contains("(") && l.Contains(")"))
                    l = Regex.Replace(l, @"(?<!\\)\([^\)]+\) ?", ""); // regex is weird

                if( string.IsNullOrWhiteSpace(l) )
                {
                    newLines.Add("");
                    _l++;
                    continue;
                }

                l = l.TrimEnd(' ');

                if (NetFIMGlobal.Punctuations.Any(x => l.EndsWith(x.ToString())))
                    l = l.TrimEnd(NetFIMGlobal.Punctuations.Where(x => l.EndsWith(x.ToString())).First());
                else
                {
                    if(!Regex.IsMatch(l, @"( |\t)*(P\.)(P\.)*S\.($| )"))
                        throw new Exception("main.fpp line " + _l + " is missing a punctuation.");
                }

                newLines.Add(l);
                _l++;
            }

            return newLines;
        }

        public static List<string> RemoveLinesAfterReportEnd(List<string> oldLines)
        {
            List<string> newLines = new List<string>();

            bool reportHasEnded = false;
            foreach(string l in oldLines)
            {
                if (!reportHasEnded)
                {
                    if(l.StartsWith(NetFIMGlobal.ReportEnd))
                        reportHasEnded = true;
                    newLines.Add(l);
                }
            }

            return newLines;
        }

        //

        public static FiMObjectTypes GetVariableInitializerType(string line)
            => GetVariableInitializerType(line, out string _phrase);
        public static FiMObjectTypes GetVariableInitializerType(string line, out string phrase)
        {
            phrase = "";
            // reminder: line should start with the variable initializer

            if (NetFIMGlobal.Keywords.Variable_NumberArray.Any(x => line.StartsWith(x)))
            {
                phrase = NetFIMGlobal.Keywords.Variable_NumberArray.Where(x => line.StartsWith(x)).FirstOrDefault();
                return FiMObjectTypes.NUMBER_ARRAY;
            }

            if (NetFIMGlobal.Keywords.Variable_Number.Any(x => line.StartsWith(x)))
            {
                phrase = NetFIMGlobal.Keywords.Variable_Number.Where(x => line.StartsWith(x)).FirstOrDefault();
                return FiMObjectTypes.NUMBER;
            }

            if (NetFIMGlobal.Keywords.Variable_Character.Any(x => line.StartsWith(x)))
            {
                phrase = NetFIMGlobal.Keywords.Variable_Character.Where(x => line.StartsWith(x)).FirstOrDefault();
                return FiMObjectTypes.CHAR;
            }

            if (NetFIMGlobal.Keywords.Variable_StringArray.Any(x => line.StartsWith(x)))
            {
                phrase = NetFIMGlobal.Keywords.Variable_StringArray.Where(x => line.StartsWith(x)).FirstOrDefault();
                return FiMObjectTypes.STRING_ARRAY;
            }

            if (NetFIMGlobal.Keywords.Variable_String.Any(x => line.StartsWith(x)))
            {
                phrase = NetFIMGlobal.Keywords.Variable_String.Where(x => line.StartsWith(x)).FirstOrDefault();
                return FiMObjectTypes.STRING;
            }

            if (NetFIMGlobal.Keywords.Variable_BooleanArray.Any(x => line.StartsWith(x)))
            {
                phrase = NetFIMGlobal.Keywords.Variable_BooleanArray.Where(x => line.StartsWith(x)).FirstOrDefault();
                return FiMObjectTypes.BOOL_ARRAY;
            }

            if (NetFIMGlobal.Keywords.Variable_Boolean.Any(x => line.StartsWith(x)))
            {
                phrase = NetFIMGlobal.Keywords.Variable_Boolean.Where(x => line.StartsWith(x)).FirstOrDefault();
                return FiMObjectTypes.BOOL;
            }

            if(line.StartsWith(NetFIMGlobal.Keywords.Variable_Empty))
            {
                phrase = NetFIMGlobal.Keywords.Variable_Empty;
                return FiMObjectTypes.UNKNOWN;
            }

            throw new Exception("Could not find type of variable initializer of sub-line '" + line + "'");

        }

        private static bool isBoolString(string value)
            => NetFIMGlobal.Keywords.Boolean_True.Any(x => value == x) || NetFIMGlobal.Keywords.Boolean_False.Any(x => value == x);
        public static bool ConvertBoolString(string value)
        {

            if(NetFIMGlobal.Keywords.Boolean_True.Any(x => value==x))
                return true;

            if(NetFIMGlobal.Keywords.Boolean_False.Any(x => value==x))
                return false;


            throw new Exception("Cannot find boolean value of '" + value + "'");
        }

        public static bool isVariableNameAccepted(string name)
            => !(name.StartsWith(" ") || name.EndsWith(" ") || Regex.IsMatch(name, @"^\d") || NetFIMGlobal.KeywordsList.Any(x => name.Contains(x) && (name.Contains(" " + x) || name.Contains(x + " ") )));

        // Used mainly for Variable Decleration

        public static FiMObject InterpretVariableDecleration(string line, out string variableName, Dictionary<string, FiMObject> variables = null)
        {
            line = line.Substring(NetFIMGlobal.Keywords.Method_Declare.Length + 1);
            FiMObject obj;
            variableName = "";

            // Check if variable is constant (will crash if changed value)
            bool isConstant = line.Contains(NetFIMGlobal.Keywords.Variable_Constant);
            if (isConstant)
                line = line.Replace(NetFIMGlobal.Keywords.Variable_Constant + " ", "");
            
            List<string> _variableName = new List<string>();
            string[] sline = line.Split(' ');
            bool gotVariableName = false;
            string variableInitializer = "";
            for (int k = 0; k < sline.Count(); k++)
            {
                if (NetFIMGlobal.Keywords.Variable_Initialization.Any(x => sline[k] == x))
                {
                    variableInitializer = NetFIMGlobal.Keywords.Variable_Initialization.Where(x => sline[k] == x).FirstOrDefault();
                    gotVariableName = true;
                    break;
                }
                else
                {
                    _variableName.Add(sline[k]);
                }
            }

            if (!gotVariableName)
                throw new Exception("Variable decleration has not finished");

            variableName = string.Join(" ", _variableName.ToArray());

            // Variable checks
            string vn = variableName;
            if (!isVariableNameAccepted(variableName))
                throw new Exception("Variable name '" + variableName + "' has broke one or more restrictions");

            try {
                var _obj = TryGetValue(line.Substring(variableName.Length + 2 + variableInitializer.Length),variables);
                obj = new FiMObject(_obj.Value, _obj.Type, isConstant, _obj.isArray);

                if (obj.Type == FiMObjectTypes.UNKNOWN && obj.isConstant)
                    throw new Exception("Null value on a constant variable");
            }
            catch (Exception e) { throw e; }

            return obj;
        }

        private static dynamic ConvertStringToProperValue(string val, FiMObjectTypes type)
        {

            if (val == NetFIMGlobal.Keywords.Variable_Empty)
            {
                if(type == FiMObjectTypes.STRING)
                    return "nothing";
                if (type == FiMObjectTypes.NUMBER)
                    return 0;
                if (type == FiMObjectTypes.BOOL_ARRAY)
                    return false;
                if (type == FiMObjectTypes.CHAR)
                    throw new Exception("MELODY | No empty value for char yet :/");
            }

            if(type == FiMObjectTypes.CHAR)
            {
                if (!val.StartsWith("'") && !val.EndsWith("'"))
                    throw new Exception("Invalid char '" + val + "'");
                if (val.Substring(0, val.Length - 1).Substring(1).Length > 1)
                    throw new Exception("Too many characters in char");
                return val.Substring(0, val.Length - 1).Substring(1);
            }
            if(type == FiMObjectTypes.STRING)
            {
                if (!val.StartsWith("\"") && !val.EndsWith("\""))
                    throw new Exception("Invalid string '" + val + "'");
                return val.Substring(0,val.Length-1).Substring(1);
            }
            if(type == FiMObjectTypes.NUMBER)
            {
                if (!float.TryParse(val, out float v))
                    throw new Exception("Invalid number '" + val + "'");
                return v;
            }
            if(type == FiMObjectTypes.BOOL)
            {
                try { return ConvertBoolString(val); }
                catch(Exception ex) { throw ex; }
            }

            throw new Exception("Could not identify and convert value '" + val + "'");
        }

        private static dynamic GuessProperValueFromString(string val)
            => GuessProperValueFromString(val, out FiMObjectTypes type);
        private static dynamic GuessProperValueFromString(string val, out FiMObjectTypes type)
        {
            if (val == NetFIMGlobal.Keywords.Variable_Empty)
            {
                type = FiMObjectTypes.UNKNOWN;
                return null;
            }
            //
            if (val.StartsWith("'") && val.EndsWith("'"))
            {
                type = FiMObjectTypes.CHAR;
                if (val.Substring(0, val.Length - 1).Substring(1).Length > 1)
                    throw new Exception("Too many characters in char");
                return val.Substring(0, val.Length - 1).Substring(1);
            }
            //
            if (val.StartsWith("\"") && val.EndsWith("\""))
            {
                type = FiMObjectTypes.STRING;
                return val.Substring(0, val.Length - 1).Substring(1);
            }
            if (float.TryParse(val, out float v))
            {
                type = FiMObjectTypes.NUMBER;
                return v;
            }
            if (isBoolString(val))
            {
                type = FiMObjectTypes.BOOL;
                return ConvertBoolString(val);
            }

            throw new Exception("Could not identify and convert value '" + val + "'");
        }
        /// <summary>
        /// Will try to get object type and value of line `[type] [value]`
        /// </summary>
        private class FiMDeterminedObject
        {
            public FiMObjectTypes Type { get; set; }
            public dynamic Value;
            public bool isArray { get; set; }
        }
        private static FiMDeterminedObject TryGetValue(string line, Dictionary<string, FiMObject> variables = null)
        {
            FiMDeterminedObject t = new FiMDeterminedObject();
            
            bool noInitializer = false;
            Exception initializerError = new Exception();
            try
            {
                t.Type = GetVariableInitializerType(line, out string ph);
                line = line.Substring(ph.Length + 1);
            }
            catch(Exception ex)
            {
                noInitializer = true;
                initializerError = ex;
            }

            // if a variable exists just grab it
            if (variables.ContainsKey(line))
            {
                t.Type = variables[line].Type;
                t.isArray = variables[line].isArray;
                t.Value = variables[line].Value;
                return t;
            }
            else if(noInitializer)
                throw initializerError;

            if (t.Type == FiMObjectTypes.BOOL_ARRAY || t.Type == FiMObjectTypes.NUMBER_ARRAY || t.Type == FiMObjectTypes.STRING_ARRAY)
                t.isArray = true;

            if(t.isArray)
            {
                if (t.Type == FiMObjectTypes.BOOL_ARRAY)
                    t.Value = new bool[63];
                else if (t.Type == FiMObjectTypes.NUMBER_ARRAY)
                    t.Value = new float[63];
                else if (t.Type == FiMObjectTypes.STRING_ARRAY)
                    t.Value = new string[63];

                // non-empty 
                if(line.Length > 0)
                {

                    FiMObjectTypes expectedType = FiMObjectTypes.UNKNOWN;

                    if (t.Type == FiMObjectTypes.BOOL_ARRAY)
                        expectedType = FiMObjectTypes.BOOL;
                    else if (t.Type == FiMObjectTypes.NUMBER_ARRAY)
                        expectedType = FiMObjectTypes.NUMBER;
                    else if (t.Type == FiMObjectTypes.STRING_ARRAY)
                        expectedType = FiMObjectTypes.STRING;

                    string[] objs = line.Split(new string[] { "and" }, StringSplitOptions.None);

                    int t_index = 0;
                    foreach(string _v in objs)
                    {
                        string v = _v.TrimStart(' ').TrimEnd(' ');
                        if(variables.ContainsKey(v))
                        {

                            if(expectedType != variables[v].Type)
                                throw new Exception("Tried to add variable '" + v + "' to a table with different type");

                            v = (string)variables[v].Value;

                        }
                        else
                        {
                            try
                            {
                                if(t.Type == FiMObjectTypes.NUMBER_ARRAY && DoesArithmetic(v, out bool arth_prfx, out string arth_sign))
                                {
                                    FiMArithmetic arth = arth_prfx ? ParsePrefixArithmetic(v, arth_sign, variables) : ParseInfixArithmetic(v, arth_sign, variables);
                                    //
                                    v = arth.Calculate().ToString();
                                }
                                else
                                    v = ConvertStringToProperValue(v, expectedType);
                            }
                            catch(Exception ex)
                            {
                                throw ex;
                            }
                        }
                        t.Value[t_index]=v;
                        t_index++;
                    }
                }
            }
            else
            {
                if (line.Length == 0)
                    t.Type = FiMObjectTypes.UNKNOWN;
                
                if(t.Type == FiMObjectTypes.NUMBER && DoesArithmetic(line,out bool arth_prfx, out string arth_sign))
                {
                    FiMArithmetic arth = arth_prfx ? ParsePrefixArithmetic(line, arth_sign, variables) : ParseInfixArithmetic(line, arth_sign, variables);
                    //
                    t.Value = ConvertStringToProperValue(arth.Calculate().ToString(), t.Type);
                }
                else
                {
                    t.Value = ConvertStringToProperValue(line, t.Type);
                }
            }
            //

            return t;
        }

        public static string ReplaceStringVariables(string line, Dictionary<string, FiMObject> variables)
        {
            if (!line.Contains("\""))
                return line;

            string nl = "";
            string tl = "";
            bool d = false;
            for(int i = 0; i < line.Length; i++)
            {

                if (line[i] == '\\')
                {
                    i += 2;
                    continue;
                }

                if (line[i] == '"')
                {
                    if (!d)
                        d = true;
                    else
                    {

                        if (Regex.IsMatch(tl, @" \d+$"))
                        {
                            // check if variable name minus the last digit is a list
                            // if not, return variable not found

                            string v = Regex.Replace(tl, @" \d+$", "");
                            if (!variables.ContainsKey(v))
                                throw new Exception("Variable '" + tl + "' not found");

                            int slot = int.Parse(tl.Remove(0, v.Length).TrimStart(' '));

                            FiMObject o = variables[v];
                            if (!o.isArray)
                                throw new Exception("Tried to read slot " + slot + " of a non-array variable");

                            nl += (string)o.GetActualValue()[slot];
                        }
                        else
                        {
                            if (!variables.ContainsKey(tl))
                                throw new Exception("Variable '" + tl + "' does not exist");

                            FiMObject o = variables[tl];

                            if (o.isArray)
                                throw new Exception("Tried to merge an array to a string");

                            nl += o.Value.ToString();
                        }

                        tl = "";
                        d = false;
                    }
                } else
                {
                    if (d)
                        tl += line[i];
                    else
                        nl += line[i];
                }
                
            }



            return nl;
        }

        public static Dictionary<string, FiMObject> MergeLocalAndGlobalVariables(Dictionary<string, FiMObject> l1, Dictionary<string,FiMObject> l2)
        {
            Dictionary<string, FiMObject> l3 = new Dictionary<string, FiMObject>(l1);
            l2.ToList().ForEach(x => l3.Add(x.Key, x.Value));

            return l3;
        }

        //

        public class FiMMethodOut
        {
            public string methodType { get; set; }
            public Dictionary<string, dynamic> outArgs { get; set; }

            public FiMMethodOut()
            {
                methodType = "";
                outArgs = new Dictionary<string, object>();
            }
        }
        public static FiMMethodOut ScanMethod(FiMReport report, string line, string[] lines, int lineIndex, int actualIndex, FiMParagraph thisParagraph, Dictionary<string, FiMObject> variables = null, Dictionary<string, FiMParagraph> paragraphs = null)
        {
            FiMMethodOut methodOut = new FiMMethodOut();

            // ARRAY MODIFY
            if(NetFIMGlobal.Keywords.Variable_Initialization.Any(x => line.Contains(" " + x)))
            {

                string vinit = " " + NetFIMGlobal.Keywords.Variable_Initialization.Where(x => line.Contains(" " + x)).FirstOrDefault();
                string[] vs = line.Split(new string[] { vinit }, StringSplitOptions.None);
                string vn = vs[0];

                if(Regex.IsMatch(vn, @" \d+$"))
                {
                    methodOut.methodType = "MODIFY-ARRAY";

                    string _vn = Regex.Replace(vn, @" \d+$", "");
                    int slot = int.Parse(vn.Substring(_vn.Length).TrimStart(' ')) - 1;
                    vn = _vn;

                    if(variables.ContainsKey(vn))
                    {
                        string nv = string.Join(vinit,vs.Skip(1).ToArray()).TrimStart(' ').TrimEnd(' ');
                        FiMObject v = variables[vn];

                        if (v.isConstant)
                            throw new Exception("[" + lineIndex + "] Cannot modify a constant array");

                        if (slot < 0 && v.GetActualValue().Count > slot)
                            throw new Exception("[" + lineIndex + "] Invalid slot '" + slot + "'");

                        FiMObjectTypes expectedType = FiMObjectTypes.UNKNOWN;

                        if (v.Type == FiMObjectTypes.BOOL_ARRAY)
                            expectedType = FiMObjectTypes.BOOL;
                        else if (v.Type == FiMObjectTypes.NUMBER_ARRAY)
                            expectedType = FiMObjectTypes.NUMBER;
                        else if (v.Type == FiMObjectTypes.STRING_ARRAY)
                            expectedType = FiMObjectTypes.STRING;

                        if(variables.ContainsKey(nv))
                        {
                            if (expectedType != variables[nv].Type)
                                throw new Exception("[" + lineIndex + "] Tried to change array variable to a value of different type");

                            nv = (string)variables[nv].Value;
                        }
                        else
                        {
                            try
                            {

                                if (DoesArithmetic(nv, out bool arth_prfx, out string arth_sign))
                                {
                                    if (expectedType != FiMObjectTypes.NUMBER)
                                        throw new Exception("[" + lineIndex + "] Tried to do arithmetic on a non-number array");

                                    FiMArithmetic arth = arth_prfx ? ParsePrefixArithmetic(nv, arth_sign, variables) : ParseInfixArithmetic(nv, arth_sign, variables);
                                    //
                                    nv = arth.Calculate().ToString();
                                }
                                else
                                {
                                    // optional variable type
                                    string ls = nv;

                                    try
                                    {
                                        GetVariableInitializerType(nv, out string ph);
                                        ls = nv.Substring(ph.Length + 1);
                                    }
                                    catch { }

                                    bool regexValue = false;
                                    if (Regex.IsMatch(ls, @" \d+$"))
                                    {
                                        string _v = Regex.Replace(ls, @" \d+$", "");
                                        if (variables.ContainsKey(_v))
                                        {
                                            int _slot = int.Parse(ls.Substring(_v.Length).TrimStart(' ')) - 1;

                                            FiMObject o = variables[_v];

                                            if (!o.isArray)
                                                throw new Exception("Tried to read slot " + _slot + " of a non-array variable");
                                            if (_slot < 0 && o.GetActualValue().Count > _slot)
                                                throw new Exception("Invalid slot '" + _slot + "'");

                                            FiMObjectTypes _expectedType = FiMObjectTypes.UNKNOWN;

                                            if (o.Type == FiMObjectTypes.BOOL_ARRAY)
                                                _expectedType = FiMObjectTypes.BOOL;
                                            else if (o.Type == FiMObjectTypes.NUMBER_ARRAY)
                                                _expectedType = FiMObjectTypes.NUMBER;
                                            else if (o.Type == FiMObjectTypes.STRING_ARRAY)
                                                _expectedType = FiMObjectTypes.STRING;

                                            if (expectedType != _expectedType)
                                                throw new Exception(string.Format("[" + lineIndex + "] Tried to change {0} value to {1}", expectedType, _expectedType));

                                            nv = o.GetActualValue()[_slot];
                                            regexValue = true;
                                        }
                                    }

                                    if (!regexValue) {
                                        try
                                        {
                                            nv = ConvertStringToProperValue(nv, expectedType);
                                        }
                                        catch(Exception ex)
                                        {
                                            throw new Exception("[" + lineIndex + "] " + ex.Message);
                                        }
                                    }
                                }

                            }
                            catch (Exception ex)
                            {
                                throw new Exception("[" + lineIndex + "] " + ex.Message);
                            }
                        }

                        methodOut.outArgs.Add("Slot", slot);
                        methodOut.outArgs.Add("Variable Name", vn);
                        methodOut.outArgs.Add("New Value", nv);
                    }
                    else
                    {
                        throw new Exception("[" + lineIndex + "] Variable '" + vn + "' not found");
                    }

                }
                
            }

            if(!string.IsNullOrWhiteSpace(methodOut.methodType))
                return methodOut;

            // WRITE
            if(NetFIMGlobal.Keywords.Method_Write.Any(x => line.StartsWith(x)))
            {

                // RUN FUNCTION (run without writing any possible returns)
                if(line.StartsWith("I remembered") || line.StartsWith("I would"))
                {

                    methodOut.methodType = "WRITE-RUN";

                    string l = line.Substring(NetFIMGlobal.Keywords.Method_Write.Where(x => line.StartsWith(x)).FirstOrDefault().Length).TrimStart(' ');

                    string _rmn = l.Split(new string[] { "using" }, StringSplitOptions.None)[0].TrimEnd(' ');

                    if (!paragraphs.ContainsKey(_rmn))
                        throw new Exception("[" + lineIndex + "] Paragraph '" + _rmn + "' doesn't exist");

                    FiMParagraph _rpr = paragraphs[_rmn];

                    FiMObject _rpo = null;

                    if (_rpr.ParameterType != FiMObjectTypes.UNKNOWN)
                    {
                        if (!l.Contains(" using "))
                        {
                            _rpo = null; // debating wether this should crash or default to null :thonking:
                        }
                        else
                        {
                            string p = l.Split(new string[] { "using" }, StringSplitOptions.None)[1].TrimStart(' ');

                            if(Regex.IsMatch(l, @" \d+$"))
                            {
                                string v = Regex.Replace(l, @" \d+$", "");
                                if (!variables.ContainsKey(v))
                                    throw new Exception("[" + lineIndex + "] Variable '" + l + "' not found");

                                int slot = int.Parse(l.Substring(v.Length).TrimStart(' ')) - 1;

                                FiMObject o = variables[v];

                                if (!o.isArray)
                                    throw new Exception("[" + lineIndex + "] Tried to read slot " + slot + " of a non-array variable");
                                if (slot < 0 && o.GetActualValue().Count > slot)
                                    throw new Exception("[" + lineIndex + "] Invalid slot '" + slot + "'");

                                if (o.Type == FiMObjectTypes.STRING_ARRAY)
                                {
                                    try
                                    {
                                        string val = ReplaceStringVariables(o.GetActualValue()[slot], variables);

                                        _rpo = new FiMObject(val, FiMObjectTypes.STRING, o.isConstant, o.isArray);
                                    }
                                    catch (Exception ex)
                                    {
                                        throw new Exception("[" + lineIndex + "] " + ex.Message);
                                    }
                                }
                                else
                                {
                                    _rpo = FiMObject.FromArraySlot(o, slot);
                                }
                                
                            }

                            if (!variables.ContainsKey(p))
                                throw new Exception("[" + lineIndex + "] Tried to use variable '" + p + "' which doesn't exist");

                            _rpo = variables[p];

                            _rpo = new FiMObject(_rpo.Value, _rpo.Type, true, _rpo.isArray);
                        }
                    }

                    //

                    methodOut.outArgs.Add("Method Name", _rmn);
                    methodOut.outArgs.Add("Parameter", _rpo);
                    methodOut.outArgs.Add("doWrite", false);

                }

                // WRITE (run. method must has return type)
                else
                {

                    methodOut.methodType = "WRITE-WRITE";

                    string l = line.Remove(0, NetFIMGlobal.Keywords.Method_Write.Where(x => line.StartsWith(x)).FirstOrDefault().Length + 1 );

                    // string
                    if(l.StartsWith("\"") && l.EndsWith("\""))
                    {
                        l = l.Substring(0, l.Length - 1).Substring(1);

                        try
                        {
                            WriteToScreen(ReplaceStringVariables(l, variables));
                        }
                        catch (Exception ex)
                        {
                            throw new Exception("[" + lineIndex + "] " + ex.Message);
                        }
                    }
                    // array/arithmetic/variable or function
                    else
                    {

                        // is function
                        string f_tl = l.Split(new string[] { "using" }, StringSplitOptions.None)[0].TrimEnd(' ');
                        if (paragraphs.Keys.Any(x => f_tl.Equals(x) ))
                        {
                            methodOut.methodType = "WRITE-RUN";
                            string p_key = paragraphs.Keys.Where(x => f_tl.Equals(x)).FirstOrDefault();
                            FiMParagraph p_par = paragraphs[p_key];

                            if (p_par.ReturnType == FiMObjectTypes.UNKNOWN)
                                throw new Exception("[" + lineIndex + "] Using incorrect phrase to run a paragraph that deosn't return a value");

                            FiMObject param = null;

                            if(p_par.ParameterType != FiMObjectTypes.UNKNOWN)
                            {
                                if(!l.Contains(" using "))
                                {
                                    param = null; // debating wether this should crash or default to null :thonking:
                                }
                                else
                                {
                                    string p = l.Split(new string[] { "using" }, StringSplitOptions.None)[1].TrimStart(' ');

                                    if (Regex.IsMatch(l, @" \d+$"))
                                    {
                                        string v = Regex.Replace(l, @" \d+$", "");
                                        if (!variables.ContainsKey(v))
                                            throw new Exception("[" + lineIndex + "] Variable '" + l + "' not found");

                                        int slot = int.Parse(l.Substring(v.Length).TrimStart(' ')) - 1;

                                        FiMObject o = variables[v];

                                        if (!o.isArray)
                                            throw new Exception("[" + lineIndex + "] Tried to read slot " + slot + " of a non-array variable");
                                        if (slot < 0 && o.GetActualValue().Count > slot)
                                            throw new Exception("[" + lineIndex + "] Invalid slot '" + slot + "'");

                                        if (o.Type == FiMObjectTypes.STRING_ARRAY)
                                        {
                                            try
                                            {
                                                string val = ReplaceStringVariables(o.GetActualValue()[slot], variables);

                                                param = new FiMObject(val, FiMObjectTypes.STRING, o.isConstant, false);
                                            }
                                            catch (Exception ex)
                                            {
                                                throw new Exception("[" + lineIndex + "] " + ex.Message);
                                            }
                                        }
                                        else
                                        {
                                            param = FiMObject.FromArraySlot(o, slot);
                                        }

                                    }
                                    else
                                    {
                                        if (!variables.ContainsKey(p))
                                            throw new Exception("[" + lineIndex + "] Tried to use variable '" + p + "' which doesn't exist");

                                        param = variables[p];
                                    }

                                    param = new FiMObject(param.Value, param.Type, true, param.isArray);
                                }
                            }
                            // eh, just ignore if the user added a parameter

                            methodOut.outArgs.Add("Method Name", p_key);
                            methodOut.outArgs.Add("Parameter", param);
                            methodOut.outArgs.Add("doWrite", true);
                        }
                        // is arithmetic
                        else if (DoesArithmetic(l, out bool arth_prfx, out string arth_sign))
                        {
                            FiMArithmetic arth = arth_prfx ? ParsePrefixArithmetic(l, arth_sign, variables) : ParseInfixArithmetic(l, arth_sign, variables);
                            //
                            WriteToScreen(arth.Calculate().ToString());
                        }
                        // is array
                        else if(Regex.IsMatch(l, @" \d+$"))
                        {
                            // check if variable name minus the last digit is a list
                            // if not, return variable not found

                            string v = Regex.Replace(l, @" \d+$", "");
                            if(!variables.ContainsKey(v))
                                throw new Exception("[" + lineIndex + "] Variable '" + l + "' not found");

                            int slot = int.Parse(l.Substring(v.Length).TrimStart(' '))-1;

                            FiMObject o = variables[v];                        
                            
                            if (!o.isArray)
                                throw new Exception("[" + lineIndex + "] Tried to read slot " + slot + " of a non-array variable");
                            if (slot < 0 && o.GetActualValue().Count > slot)
                                throw new Exception("[" + lineIndex + "] Invalid slot '" + slot + "'");

                            if(o.Type == FiMObjectTypes.STRING_ARRAY)
                            {
                                try
                                {
                                    WriteToScreen(ReplaceStringVariables(o.GetActualValue()[slot], variables));
                                }
                                catch (Exception ex)
                                {
                                    throw new Exception("[" + lineIndex + "] " + ex.Message);
                                }
                            }
                            else
                            {
                                WriteToScreen(o.GetActualValue()[slot]);
                            }
                        }
                        // is variable
                        else
                        {
                            if (!variables.ContainsKey(l))
                                throw new Exception("[" + lineIndex + "] Variable '" + l + "' not found");

                            FiMObject o = variables[l];
                            if (o.isArray)
                                throw new Exception("[" + lineIndex + "] Tried to output an array");

                            if(o.Type == FiMObjectTypes.STRING)
                            {
                                try
                                {
                                    WriteToScreen(ReplaceStringVariables(o.GetActualValue(), variables));
                                }
                                catch (Exception ex)
                                {
                                    throw new Exception("[" + lineIndex + "] " + ex.Message);
                                }
                            }
                            else
                            {
                                WriteToScreen(o.GetActualValue());
                            }
                        }
                    }

                }

            }
            
            // MAKE VARIABLE
            else if (line.StartsWith(NetFIMGlobal.Keywords.Method_Declare))
            {
                methodOut.methodType = "MAKE-VARIABLE";

                string variableName;
                FiMObject obj;
                try { obj = NetFIMMethods.InterpretVariableDecleration(line, out variableName, variables); }
                catch (Exception e) { throw new Exception(e.Message); }

                if (variables.ContainsKey(variableName))
                    throw new Exception("A variable with the name '" + variableName + "' already exists.");

                methodOut.outArgs.Add("Variable Name", variableName);
                methodOut.outArgs.Add("Variable", (object)obj);
            }

            // MODIFY VARIABLE
            else if (NetFIMGlobal.Keywords.Method_Replace.Any(x => line.Contains(" " + x + " ")))
            {
                methodOut.methodType = "MODIFY-VARIABLE";
                string ph = NetFIMGlobal.Keywords.Method_Replace.Where(x => line.Contains(" " + x + " ")).FirstOrDefault();
                
                //
                string v = line.Split(new string[] { ph }, StringSplitOptions.None)[0].TrimEnd(' ');
                if (!variables.ContainsKey(v))
                    throw new Exception("[" + lineIndex + "] Variable '" + v + "' not found");
                //
                FiMObject var = variables[v];
                if (var.isArray)
                    throw new Exception("[" + lineIndex + "] Incorrect phrase for array variable modification");
                if (var.isConstant)
                    throw new Exception("[" + lineIndex + "] Cannot modify a constant variable");

                //
                try
                {
                    string testString = line.Split(new string[] { ph }, StringSplitOptions.None)[1].TrimStart(' ');
                    if (DoesArithmetic(testString, out bool arth_prfx, out string arth_sign))
                    {
                        FiMArithmetic arth = arth_prfx ? ParsePrefixArithmetic(testString, arth_sign, variables) : ParseInfixArithmetic(testString, arth_sign, variables);
                        //
                        var.Value = arth.Calculate();
                    }
                    else
                    {
                        string ls = testString;
                        try
                        {
                            GetVariableInitializerType(testString, out string testph);
                            ls = testString.Substring(testph.Length + 1);
                        }
                        catch { }

                        bool regexValue = false;
                        if (Regex.IsMatch(ls, @" \d+$"))
                        {
                            string _v = Regex.Replace(ls, @" \d+$", "");
                            if (variables.ContainsKey(_v))
                            {
                                int slot = int.Parse(ls.Substring(_v.Length).TrimStart(' ')) - 1;

                                FiMObject o = variables[_v];

                                if (!o.isArray)
                                    throw new Exception("[" + lineIndex + "] Tried to read slot " + slot + " of a non-array variable");
                                if (slot < 0 && o.GetActualValue().Count > slot)
                                    throw new Exception("[" + lineIndex + "] Invalid slot '" + slot + "'");

                                FiMObjectTypes expectedType = FiMObjectTypes.UNKNOWN;

                                if (o.Type == FiMObjectTypes.BOOL_ARRAY)
                                    expectedType = FiMObjectTypes.BOOL;
                                else if (o.Type == FiMObjectTypes.NUMBER_ARRAY)
                                    expectedType = FiMObjectTypes.NUMBER;
                                else if (o.Type == FiMObjectTypes.STRING_ARRAY)
                                    expectedType = FiMObjectTypes.STRING;

                                if (expectedType != var.Type)
                                    throw new Exception(string.Format("[" + lineIndex + "] Tried to change {0} value to {1}", var.Type, expectedType));

                                var.Value = o.GetActualValue()[slot];
                                regexValue = true;
                            }
                        }

                        if(!regexValue)
                        {
                            try
                            {
                                var.Value = ConvertStringToProperValue(ls, var.Type);
                            }
                            catch (Exception ex)
                            {
                                throw new Exception("[" + lineIndex + "] " + ex.Message);
                            }
                        }

                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("[" + lineIndex + "] " + ex.Message);
                }
                //
                methodOut.outArgs.Add("Variable Name", v);
                methodOut.outArgs.Add("New Value", var);
            }

            // INCREMENT VARIABLE
            else if (line.StartsWith(NetFIMGlobal.Keywords.Variable_Increment.Start + " "))
            {
                methodOut.methodType = "VARIABLE-INCREMENT";

                string n = line.Substring(NetFIMGlobal.Keywords.Variable_Increment.Start.Length + 1);

                if(!variables.ContainsKey(n))
                    throw new Exception("[" + lineIndex + "] Variable '" + n + "' doesn't exist");

                FiMObject v = variables[n];

                if(v.Type != FiMObjectTypes.NUMBER)
                    throw new Exception("[" + lineIndex + "] Tried to increment a non-number variable");
                if(v.isConstant)
                    throw new Exception("[" + lineIndex + "] Tried to increment a constant variable");

                methodOut.outArgs.Add("Variable Name", n);
            }
            else if(line.EndsWith(" " + NetFIMGlobal.Keywords.Variable_Increment.End))
            {
                methodOut.methodType = "VARIABLE-INCREMENT";

                string n = line.Substring(0, line.Length - (NetFIMGlobal.Keywords.Variable_Increment.End.Length + 1));

                if (!variables.ContainsKey(n))
                    throw new Exception("[" + lineIndex + "] Variable '" + n + "' doesn't exist");

                FiMObject v = variables[n];

                if (v.Type != FiMObjectTypes.NUMBER)
                    throw new Exception("[" + lineIndex + "] Tried to increment a non-number variable");
                if (v.isConstant)
                    throw new Exception("[" + lineIndex + "] Tried to increment a constant variable");

                methodOut.outArgs.Add("Variable Name", n);
            }

            // DECREMENT VARIABLE
            else if(line.StartsWith(NetFIMGlobal.Keywords.Variable_Decrement.Start + " "))
            {
                methodOut.methodType = "VARIABLE-DECREMENT";

                string n = line.Substring(NetFIMGlobal.Keywords.Variable_Decrement.Start.Length + 1);

                if (!variables.ContainsKey(n))
                    throw new Exception("[" + lineIndex + "] Variable '" + n + "' doesn't exist");

                FiMObject v = variables[n];

                if (v.Type != FiMObjectTypes.NUMBER)
                    throw new Exception("[" + lineIndex + "] Tried to decrement a non-number variable");
                if (v.isConstant)
                    throw new Exception("[" + lineIndex + "] Tried to decrement a constant variable");

                methodOut.outArgs.Add("Variable Name", n);
            }
            else if (line.EndsWith(" " + NetFIMGlobal.Keywords.Variable_Decrement.End))
            {
                methodOut.methodType = "VARIABLE-DECREMENT";

                string n = line.Substring(0, line.Length - (NetFIMGlobal.Keywords.Variable_Decrement.End.Length + 1));

                if (!variables.ContainsKey(n))
                    throw new Exception("[" + lineIndex + "] Variable '" + n + "' doesn't exist");

                FiMObject v = variables[n];

                if (v.Type != FiMObjectTypes.NUMBER)
                    throw new Exception("[" + lineIndex + "] Tried to decrement a non-number variable");
                if (v.isConstant)
                    throw new Exception("[" + lineIndex + "] Tried to decrement a constant variable");

                methodOut.outArgs.Add("Variable Name", n);
            }

            // WHILE
            else if (NetFIMGlobal.Keywords.Loop_While.Any(x => line.StartsWith(x + " ")))
            {
                methodOut.methodType = "WHILE";

                if (!lines.Skip(actualIndex).Any(x => x.StartsWith(NetFIMGlobal.Keywords.Loop_End)))
                    throw new Exception("[" + lineIndex + "] Could not find while loop statement end");

                string l = line;
                string ph = NetFIMGlobal.Keywords.Loop_While.Where(x => line.StartsWith(x + " ")).FirstOrDefault();
                l = l.Substring(ph.Length + 1);

                int end = Array.FindIndex(lines.Skip(actualIndex - 1).ToArray(), x => x.StartsWith(NetFIMGlobal.Keywords.Loop_End)) - 1;
                methodOut.outArgs.Add("Skip", end);
                
                List<string> While_Lines = new List<string>();

                for(int i = actualIndex-1+1; i < actualIndex + end; i++)
                    While_Lines.Add(lines[i]);

                if(While_Lines.Count() > 0)
                {
                    try
                    {
                        FiMConditional cond = ParseConditional(l, variables);
                        Dictionary<string, FiMObject> w_changed = new Dictionary<string, FiMObject>();
                        while (true)
                        {
                            if (cond.Check())
                            {
                                FiMStatement cur = RunStatement(report, While_Lines.ToArray(), actualIndex - 1, thisParagraph, variables, paragraphs);

                                //
                                cur.changedVariables.Keys.ToList().ForEach(x =>
                                {
                                    FiMObject k = cur.changedVariables[x];

                                    variables[x] = k;

                                    if (w_changed.ContainsKey(x))
                                        w_changed[x] = k;
                                    else
                                        w_changed.Add(x, k);
                                });
                                //

                                if (cur.hasReturned==true) {
                                    methodOut.outArgs.Add("Returned Variable", cur.returnedObject);
                                    break;
                                }

                                cond = ParseConditional(cond.LeftVariableName, cond.RightVariableName, cond.Sign, variables);
                            }
                            else
                                break;
                            
                        }
                        methodOut.outArgs.Add("Changed Variables", w_changed);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(ex.Message);
                    }
                }
            }

            // IF-ELSE
            else if(NetFIMGlobal.Keywords.If_Start.Any(x => line.StartsWith(x + " ")))
            {
                methodOut.methodType = "IF-ELSE";
                
                if (!lines.Skip(actualIndex).Any(x => x.StartsWith(NetFIMGlobal.Keywords.If_End)))
                    throw new Exception("[" + lineIndex + "] Could not find if-else statement end");

                string l = line;

                // remove optional suffix
                if (line.EndsWith(NetFIMGlobal.Keywords.If_Suffix))
                    l = l.Substring(0, line.Length - (NetFIMGlobal.Keywords.If_Suffix.Length + 1));

                int end = Array.FindIndex(lines.Skip(actualIndex-1).ToArray(), x => x.StartsWith(NetFIMGlobal.Keywords.If_End))-1;
                methodOut.outArgs.Add("Skip", end);

                //

                List<FiMConditionalIndex> ind = new List<FiMConditionalIndex>
                {
                    new FiMConditionalIndex
                    {
                        Index = actualIndex - 1,
                        Line = line
                    }
                };

                lines.Skip(actualIndex - 1).Where(x => x.StartsWith("Otherwise")).ToList().ForEach(x =>
                {
                    ind.Add(new FiMConditionalIndex
                    {
                        Index = (actualIndex - 1) + Array.FindIndex(lines.Skip(actualIndex - 1).ToArray(), y => y.StartsWith(x)),
                        Line = x
                    });
                });

                // checks
                if(ind.Where(x => x.Line.Equals("Otherwise")).Count() > 0)
                {
                    if (ind.Where(x => x.Line.Equals("Otherwise")).Count() > 1)
                        throw new Exception("[" + lineIndex + "] Multiple else statements found");
                    if(!ind.Last().Line.Equals("Otherwise"))
                        throw new Exception("[" + lineIndex + "] else statement expected at to be the last statement");
                }

                List<string> IfElse_Lines = new List<string>();
                int IfElse_Index = 0;
                
                foreach(FiMConditionalIndex i in ind)
                {
                    string il = i.Line;
                    int ie = ind.IndexOf(i) == (ind.Count() - 1) ? end : ind[ind.IndexOf(i)+1].Index-1;

                    // if statement
                    if(ind.IndexOf(i)==0)
                    {
                        string ph = NetFIMGlobal.Keywords.If_Start.Where(x =>i.Line.StartsWith(x + " ")).FirstOrDefault();
                        il = il.Substring(ph.Length + 1);
                        //
                        try
                        {

                            FiMConditional cond = ParseConditional(il, variables);

                            if(cond.Check())
                            {
                                for(int li = 1; li < ie-i.Index+1; li++)
                                {
                                    IfElse_Lines.Add(lines.Skip(i.Index).ToArray()[li]);
                                }
                                IfElse_Index = i.Index;
                                break;
                            }

                        }
                        catch(Exception ex)
                        {
                            throw new Exception("[" + lineIndex + "] " + ex.Message);
                        }
                    }
                    // else-if
                    else if(il.StartsWith("Otherwise "))
                    {
                        string ph = NetFIMGlobal.Keywords.If_Start.Where(x => i.Line.StartsWith("Otherwise ")).FirstOrDefault();
                        il = il.Substring(ph.Length + 1);
                        //
                        try
                        {

                            FiMConditional cond = ParseConditional(il, variables);

                            if (cond.Check())
                            {
                                // run inside
                                for (int li = 1; li < ie - i.Index + 1; li++)
                                {
                                    IfElse_Lines.Add(lines.Skip(i.Index).ToArray()[li]);
                                }
                                IfElse_Index = i.Index;
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            throw new Exception("[" + lineIndex + "] " + ex.Message);
                        }
                    }
                    // else
                    else
                    {
                        for (int li = 1; li < ie - 1; li++)
                        {
                            IfElse_Lines.Add(lines.Skip(i.Index).ToArray()[li]);
                        }
                        IfElse_Index = i.Index;
                        break;
                    }
                    
                }

                if(IfElse_Lines.Count() > 0)
                {
                    try
                    {
                        methodOut.outArgs.Add("Statement", RunStatement(report, IfElse_Lines.ToArray(), IfElse_Index, thisParagraph, variables, paragraphs));
                    }
                    catch(Exception ex)
                    {
                        throw new Exception(ex.Message);
                    }
                }

            }

            // RETURN
            else if(line.StartsWith(NetFIMGlobal.Keywords.Method_Return2 + " "))
            {
                methodOut.methodType = "RETURN";

                if (thisParagraph.ReturnType == FiMObjectTypes.UNKNOWN)
                    throw new Exception("[" + lineIndex + "] Tried to return on a paragraph that doesn't return any value");

                string var = line.Substring(NetFIMGlobal.Keywords.Method_Return2.Length + 1);
                //
                string ls = var;
                try
                {
                    GetVariableInitializerType(var, out string testph);
                    ls = var.Substring(testph.Length + 1);
                }
                catch { }

                bool regexValue = false;
                if (Regex.IsMatch(ls, @" \d+$"))
                {
                    string _v = Regex.Replace(ls, @" \d+$", "");
                    if (variables.ContainsKey(_v))
                    {
                        int slot = int.Parse(ls.Substring(_v.Length).TrimStart(' ')) - 1;

                        FiMObject o = variables[_v];

                        if (!o.isArray)
                            throw new Exception("Tried to read slot " + slot + " of a non-array variable");
                        if (slot < 0 && o.GetActualValue().Count > slot)
                            throw new Exception("Invalid slot '" + slot + "'");

                        FiMObjectTypes expectedType = FiMObjectTypes.UNKNOWN;

                        if (o.Type == FiMObjectTypes.BOOL_ARRAY)
                            expectedType = FiMObjectTypes.BOOL;
                        else if (o.Type == FiMObjectTypes.NUMBER_ARRAY)
                            expectedType = FiMObjectTypes.NUMBER;
                        else if (o.Type == FiMObjectTypes.STRING_ARRAY)
                            expectedType = FiMObjectTypes.STRING;

                        if (expectedType != thisParagraph.ReturnType)
                            throw new Exception(string.Format("[" + lineIndex + "] Expected {0}, got {1}", thisParagraph.ReturnType, expectedType));

                        methodOut.outArgs.Add("Variable", o.GetActualValue()[slot]);
                        regexValue = true;
                    }
                }

                if (!regexValue)
                {
                    try
                    {
                        if(variables.ContainsKey(ls))
                        {
                            if (variables[ls].Type != thisParagraph.ReturnType)
                                throw new Exception(string.Format("Expected {0}, got {1}", thisParagraph.ReturnType, variables[ls].Type));

                            methodOut.outArgs.Add("Variable", variables[ls].GetActualValue());
                        }
                        else if(thisParagraph.ReturnType == FiMObjectTypes.NUMBER && DoesArithmetic(ls, out bool arth_prfx, out string arth_sign))
                        {
                            FiMArithmetic arth = arth_prfx ? ParsePrefixArithmetic(ls, arth_sign, variables) : ParseInfixArithmetic(ls, arth_sign, variables);
                            //
                            methodOut.outArgs.Add("Variable", arth.Calculate());
                        }
                        else
                        {
                            methodOut.outArgs.Add("Variable", ConvertStringToProperValue(ls, thisParagraph.ReturnType));
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(ex.Message);
                    }
                }
            }

            return methodOut;
        }

        //

        public class FiMStatement
        {
            public Dictionary<string, FiMObject> changedVariables { get; set; }
            public bool hasReturned { get; set; }
            public FiMObject returnedObject { get; set; }

            public FiMStatement()
            {
                changedVariables = new Dictionary<string, FiMObject>();
                hasReturned = false;
                returnedObject = null;
            }
        }
        public static FiMStatement RunStatement(FiMReport report, string[] lines, int firstLineIndex, FiMParagraph thisParagraph, Dictionary<string, FiMObject> variables, Dictionary<string, FiMParagraph> paragraphs)
        {
            FiMStatement statement = new FiMStatement();
            Dictionary<string, FiMObject> localVariables = new Dictionary<string, FiMObject>();

            for (int i = 0; i < lines.Count(); i++)
            {
                if (!string.IsNullOrWhiteSpace(lines[i]))
                {
                    string nl = lines[i];
                    if (nl.StartsWith("    "))
                        nl = nl.Remove(0, "    ".Length);
                    else if (nl.StartsWith("\t"))
                        nl = nl.Remove(0, 1);
                    else
                        throw new Exception("Invalid indentation at statement");
                    lines[i] = nl;
                }
            }

            int lineIndex = 1;
            int skipTo = -1;
            string[] pLArray = lines.ToArray();
            foreach (string line in lines)
            {
                if (lineIndex <= skipTo)
                {
                    lineIndex++;
                    continue;
                }

                //Console.WriteLine(line);

                if (!string.IsNullOrWhiteSpace(line) && !Regex.IsMatch(line, @"( |\t)*(P\.)(P\.)*S\.($| )"))
                {

                    NetFIMMethods.FiMMethodOut method;
                    try
                    {
                        method = NetFIMMethods.ScanMethod(report, line, pLArray, firstLineIndex + lineIndex, lineIndex, thisParagraph, variables: MergeLocalAndGlobalVariables(variables,localVariables), paragraphs: paragraphs);
                    }
                    catch (Exception e) { throw e; }

                    //

                    switch (method.methodType)
                    {
                        case "WRITE-RUN":
                            {
                                if (!paragraphs.ContainsKey(method.outArgs["Method Name"]))
                                    throw new Exception("[" + firstLineIndex + lineIndex + "] Paragraph '" + method.outArgs["Method Name"] + "' doesn't exist");

                                if (method.outArgs["doWrite"] == true)
                                    NetFIMMethods.WriteToScreen(report.RunParagraph(method.outArgs["Method Name"], method.outArgs["Parameter"]));
                                else
                                    report.RunParagraph(method.outArgs["Method Name"], method.outArgs["Parameter"]);
                            }
                            break;
                        case "MAKE-VARIABLE":
                            {
                                localVariables.Add(method.outArgs["Variable Name"], method.outArgs["Variable"]);
                            }
                            break;
                        case "MODIFY-ARRAY":
                            {
                                // In the case of IF/WHILE statements,
                                // We check if it's a modification of its local variable or an existing variable

                                // Just change the variable's value
                                if(localVariables.ContainsKey(method.outArgs["Variable Name"]))
                                {
                                    var ol = localVariables[method.outArgs["Variable Name"]].GetActualValue();
                                    ol[method.outArgs["Slot"]] = method.outArgs["New Value"];
                                    localVariables[method.outArgs["Variable Name"]].Value = (object)ol;
                                }
                                // We change the value in `variables` list and keep a track of the change for the final merge with the actual list.
                                else
                                {
                                    var ol = variables[method.outArgs["Variable Name"]].GetActualValue();
                                    ol[method.outArgs["Slot"]] = method.outArgs["New Value"];
                                    variables[method.outArgs["Variable Name"]].Value = (object)ol;
                                    if (statement.changedVariables.ContainsKey(method.outArgs["Variable Name"]))
                                        statement.changedVariables[method.outArgs["Variable Name"]] = variables[method.outArgs["Variable Name"]];
                                    else
                                        statement.changedVariables.Add(method.outArgs["Variable Name"], variables[method.outArgs["Variable Name"]]);
                                }
                            }
                            break;
                        case "MODIFY-VARIABLE":
                            {
                                if (localVariables.ContainsKey(method.outArgs["Variable Name"]))
                                {
                                    localVariables[method.outArgs["Variable Name"]] = method.outArgs["New Value"];
                                }
                                else
                                {
                                    variables[method.outArgs["Variable Name"]] = method.outArgs["New Value"];

                                    if (statement.changedVariables.ContainsKey(method.outArgs["Variable Name"]))
                                        statement.changedVariables[method.outArgs["Variable Name"]] = method.outArgs["New Value"];
                                    else
                                        statement.changedVariables.Add(method.outArgs["Variable Name"], method.outArgs["New Value"]);
                                }
                            }
                            break;

                        case "VARIABLE-INCREMENT":
                            {
                                if (localVariables.ContainsKey(method.outArgs["Variable Name"]))
                                {
                                    float oldValue = localVariables[method.outArgs["Variable Name"]].GetActualValue();
                                    oldValue++;
                                    localVariables[method.outArgs["Variable Name"]].Value = (object)oldValue;
                                }
                                else
                                {
                                    float oldValue = variables[method.outArgs["Variable Name"]].GetActualValue();
                                    oldValue++;
                                    variables[method.outArgs["Variable Name"]].Value = (object)oldValue;

                                    if (statement.changedVariables.ContainsKey(method.outArgs["Variable Name"]))
                                        statement.changedVariables[method.outArgs["Variable Name"]].Value = (object)oldValue;
                                    else
                                        statement.changedVariables.Add(method.outArgs["Variable Name"], variables[method.outArgs["Variable Name"]]);
                                }
                            }
                            break;
                        case "VARIABLE-DECREMENT":
                            {
                                if (localVariables.ContainsKey(method.outArgs["Variable Name"]))
                                {
                                    float oldValue = localVariables[method.outArgs["Variable Name"]].GetActualValue();
                                    oldValue--;
                                    localVariables[method.outArgs["Variable Name"]].Value = (object)oldValue;
                                }
                                else
                                {
                                    float oldValue = variables[method.outArgs["Variable Name"]].GetActualValue();
                                    oldValue--;
                                    variables[method.outArgs["Variable Name"]].Value = (object)oldValue;

                                    if (statement.changedVariables.ContainsKey(method.outArgs["Variable Name"]))
                                        statement.changedVariables[method.outArgs["Variable Name"]].Value = (object)oldValue;
                                    else
                                        statement.changedVariables.Add(method.outArgs["Variable Name"], variables[method.outArgs["Variable Name"]]);
                                }
                            }
                            break;

                        case "WHILE":
                            {
                                skipTo = lineIndex + method.outArgs["Skip"] + 1;
                                //
                                Dictionary<string, FiMObject> changed = method.outArgs["Changed Variables"];

                                if (changed.Count() > 0)
                                {

                                    changed.Keys.ToList().ForEach(x =>
                                    {
                                        FiMObject obj = changed[x];

                                        if (localVariables.ContainsKey(x))
                                            localVariables[x] = obj;
                                        else
                                        {
                                            variables[x] = obj;

                                            if (statement.changedVariables.ContainsKey(x))
                                                statement.changedVariables[x] = obj;
                                            else
                                                statement.changedVariables.Add(x, obj);
                                        }
                                    });

                                }

                                if (method.outArgs["Returned Variable"] != null)
                                {
                                    statement.hasReturned = true;
                                    statement.returnedObject = method.outArgs["Returned Variable"];
                                    return statement;
                                }
                            }
                            break;
                        case "IF-ELSE":
                            {
                                skipTo = lineIndex + method.outArgs["Skip"] + 1;
                                //
                                if (method.outArgs.ContainsKey("Statement"))
                                {
                                    NetFIMMethods.FiMStatement _statement = method.outArgs["Statement"];
                                    _statement.changedVariables.Keys.ToList().ForEach(x =>
                                    {

                                        FiMObject obj = _statement.changedVariables[x];

                                        if (localVariables.ContainsKey(x))
                                            localVariables[x] = obj;
                                        else
                                        {
                                            variables[x] = obj;

                                            if (statement.changedVariables.ContainsKey(x))
                                                statement.changedVariables[x] = obj;
                                            else
                                                statement.changedVariables.Add(x, obj);
                                        }

                                    });

                                    if (method.outArgs["Returned Variable"] != null)
                                    {
                                        statement.hasReturned = true;
                                        statement.returnedObject = method.outArgs["Returned Variable"];
                                        return statement;
                                    }
                                }
                            }
                            break;

                        case "RETURN":
                            {
                                statement.hasReturned = true;
                                statement.returnedObject = method.outArgs["Variable"];
                                return statement;
                            }

                        case "WRITE-WRITE":
                        default:
                            break;
                    }

                }
                lineIndex++;
            }

            return statement;
        }

        //

        public class FiMArithmetic
        {
            public string LeftValue { get; set; }
            public string RightValue { get; set; }
            public string Sign { get; set; }

            public float Calculate()
            {
                DynValue res = Script.RunString(string.Format("return {0} {1} {2}", LeftValue, Sign, RightValue));
                return (float)res.Number;
            }
        }

        public static bool DoesArithmetic(string line, out bool isPrefix, out string arithmeticType)
        {
            isPrefix = true;
            arithmeticType = null;

            #region PREFIX
                if (line.StartsWith(NetFIMGlobal.Keywords.Arithmetic.Add.Prefix + " ") && line.Contains(" and "))
                    { arithmeticType = "+"; return true; }
                if (NetFIMGlobal.Keywords.Arithmetic.Subtract.Prefix.Any(x => line.StartsWith(x + " ")) && line.Contains(" and "))
                    { arithmeticType = "-"; return true; }
                if (NetFIMGlobal.Keywords.Arithmetic.Multiply.Prefix.Any(x => line.StartsWith(x + " "))
                    && NetFIMGlobal.Keywords.Arithmetic.Multiply.Prefix_Infix.Any(x => line.Contains(" " + x + " ")))
                    { arithmeticType = "*"; return true; }
                if (line.StartsWith(NetFIMGlobal.Keywords.Arithmetic.Divide.Prefix)
                    && NetFIMGlobal.Keywords.Arithmetic.Divide.Prefix_Infix.Any(x => line.Contains(" " + x + " ")))
                    { arithmeticType = "/"; return true; }
            #endregion

            isPrefix = false;
            #region INFIX
                if (NetFIMGlobal.Keywords.Arithmetic.Add.Infix.Any(x => line.Contains(" " + x + " ")))
                    { arithmeticType = "+"; return true; }
                if (NetFIMGlobal.Keywords.Arithmetic.Subtract.Infix.Any(x => line.Contains(" " + x + " ")))
                    { arithmeticType = "-"; return true; }
                if (NetFIMGlobal.Keywords.Arithmetic.Multiply.Infix.Any(x => line.Contains(" " + x + " ")))
                    { arithmeticType = "*"; return true; }
                if (NetFIMGlobal.Keywords.Arithmetic.Divide.Infix.Any(x => line.Contains(" " + x + " ")))
                    { arithmeticType = "/"; return true; }
            #endregion

            return false;
        }

        public static FiMArithmetic ParsePrefixArithmetic(string line, string arthm, Dictionary<string, FiMObject> variables)
        {
            FiMArithmetic ar = new FiMArithmetic
            {
                Sign = arthm,
            };

            float GetValue(string l)
            {
                try
                {
                    GetVariableInitializerType(l, out string ph);
                    string ls = l.Substring(ph.Length + 1);

                    if (Regex.IsMatch(ls, @" \d+$"))
                    {
                        string v = Regex.Replace(ls, @" \d+$", "");
                        if (variables.ContainsKey(v))
                        {
                            int slot = int.Parse(ls.Substring(v.Length).TrimStart(' ')) - 1;

                            FiMObject o = variables[v];

                            if (!o.isArray)
                                throw new Exception("Tried to read slot " + slot + " of a non-array variable");
                            if (slot < 0 && o.GetActualValue().Count > slot)
                                throw new Exception("Invalid slot '" + slot + "'");

                            if (o.Type == FiMObjectTypes.NUMBER_ARRAY)
                            {
                                try
                                {
                                    return o.GetActualValue()[slot];
                                }
                                catch (Exception ex)
                                {
                                    throw new Exception(ex.Message);
                                }
                            }
                            else
                            {
                                throw new Exception("Tried to do arithmetic with a non-number element of an array");
                            }
                        }
                    }

                    if (variables.ContainsKey(ls))
                    {
                        FiMObject var = variables[ls];
                        if (var.Type != FiMObjectTypes.NUMBER)
                            throw new Exception("Tried to do arithmetic with a non-number variable");
                        return var.GetActualValue();
                    }
                    else
                        return ConvertStringToProperValue(ls, FiMObjectTypes.NUMBER);
                }
                catch
                {
                    if (Regex.IsMatch(l, @" \d+$"))
                    {
                        string v = Regex.Replace(l, @" \d+$", "");
                        if (variables.ContainsKey(v))
                        {
                            int slot = int.Parse(l.Substring(v.Length).TrimStart(' ')) - 1;

                            FiMObject o = variables[v];

                            if (!o.isArray)
                                throw new Exception("Tried to read slot " + slot + " of a non-array variable");
                            if (slot < 0 && o.GetActualValue().Count > slot)
                                throw new Exception("Invalid slot '" + slot + "'");

                            if (o.Type == FiMObjectTypes.NUMBER_ARRAY)
                            {
                                try
                                {
                                    return o.GetActualValue()[slot];
                                }
                                catch (Exception ex)
                                {
                                    throw new Exception(ex.Message);
                                }
                            }
                            else
                            {
                                throw new Exception("Tried to do arithmetic with a non-number element of an array");
                            }
                        }
                    }

                    if (variables.ContainsKey(l))
                    {
                        FiMObject var = variables[l];
                        if (var.Type != FiMObjectTypes.NUMBER)
                            throw new Exception("Tried to do arithmetic with a non-number variable");
                        return var.GetActualValue();
                    }
                    else
                        return ConvertStringToProperValue(l, FiMObjectTypes.NUMBER);
                }
            }

            //

            if (arthm == "+")
            {
                string pfx = NetFIMGlobal.Keywords.Arithmetic.Add.Prefix;
                string[] spl = line.Substring(pfx.Length + 1).Split(new string[] { "and" }, StringSplitOptions.None);
                //
                ar.LeftValue =  GetValue(spl[0].TrimEnd(' ')).ToString();
                ar.RightValue = GetValue(spl[1].TrimStart(' ')).ToString();
            }
            else if (arthm == "-")
            {
                string pfx = NetFIMGlobal.Keywords.Arithmetic.Subtract.Prefix.Where(x => line.StartsWith(x + " ")).FirstOrDefault();
                string[] spl = line.Substring(pfx.Length + 1).Split(new string[] { "and" }, StringSplitOptions.None);
                //
                ar.LeftValue = GetValue(spl[0].TrimEnd(' ')).ToString();
                ar.RightValue = GetValue(spl[1].TrimStart(' ')).ToString();
            }
            else if (arthm == "*")
            {
                string pfx = NetFIMGlobal.Keywords.Arithmetic.Subtract.Prefix.Where(x => line.StartsWith(x + " ")).FirstOrDefault();
                string pf_inf = NetFIMGlobal.Keywords.Arithmetic.Multiply.Prefix_Infix.Where(x => line.Contains(" " + x + " ")).FirstOrDefault();
                string[] spl = line.Substring(pfx.Length + 1).Split(new string[] { pf_inf }, StringSplitOptions.None);
                //
                ar.LeftValue = GetValue(spl[0].TrimEnd(' ')).ToString();
                ar.RightValue = GetValue(spl[1].TrimStart(' ')).ToString();
            }
            else if (arthm == "/")
            {
                string pfx = NetFIMGlobal.Keywords.Arithmetic.Divide.Prefix;
                string pf_inf = NetFIMGlobal.Keywords.Arithmetic.Divide.Prefix_Infix.Where(x => line.Contains(" " + x + " ")).FirstOrDefault();
                string[] spl = line.Substring(pfx.Length + 1).Split(new string[] { pf_inf }, StringSplitOptions.None);
                //
                ar.LeftValue = GetValue(spl[0].TrimEnd(' ')).ToString();
                ar.RightValue = GetValue(spl[1].TrimStart(' ')).ToString();
            }

            //

            return ar;
        }
        public static FiMArithmetic ParseInfixArithmetic(string line, string arthm, Dictionary<string, FiMObject> variables)
        {
            FiMArithmetic ar = new FiMArithmetic
            {
                Sign = arthm,
            };

            float GetValue(string l)
            {
                try
                {
                    GetVariableInitializerType(l, out string ph);
                    string ls = l.Substring(ph.Length + 1);

                    if (Regex.IsMatch(ls, @" \d+$"))
                    {
                        string v = Regex.Replace(ls, @" \d+$", "");
                        if (variables.ContainsKey(v))
                        {
                            int slot = int.Parse(ls.Substring(v.Length).TrimStart(' ')) - 1;

                            FiMObject o = variables[v];

                            if (!o.isArray)
                                throw new Exception("Tried to read slot " + slot + " of a non-array variable");
                            if (slot < 0 && o.GetActualValue().Count > slot)
                                throw new Exception("Invalid slot '" + slot + "'");

                            if (o.Type == FiMObjectTypes.NUMBER_ARRAY)
                            {
                                try
                                {
                                    return o.GetActualValue()[slot];
                                }
                                catch (Exception ex)
                                {
                                    throw new Exception(ex.Message);
                                }
                            }
                            else
                            {
                                throw new Exception("Tried to do arithmetic with a non-number element of an array");
                            }
                        }
                    }

                    if (variables.ContainsKey(ls))
                    {
                        FiMObject var = variables[ls];
                        if (var.Type != FiMObjectTypes.NUMBER)
                            throw new Exception("Tried to do arithmetic with a non-number variable");
                        return var.GetActualValue();
                    }
                    else
                        return ConvertStringToProperValue(ls, FiMObjectTypes.NUMBER);
                }
                catch
                {
                    if (Regex.IsMatch(l, @" \d+$"))
                    {
                        string v = Regex.Replace(l, @" \d+$", "");
                        if (variables.ContainsKey(v))
                        {
                            int slot = int.Parse(l.Substring(v.Length).TrimStart(' ')) - 1;

                            FiMObject o = variables[v];

                            if (!o.isArray)
                                throw new Exception("Tried to read slot " + slot + " of a non-array variable");
                            if (slot < 0 && o.GetActualValue().Count > slot)
                                throw new Exception("Invalid slot '" + slot + "'");

                            if (o.Type == FiMObjectTypes.NUMBER_ARRAY)
                            {
                                try
                                {
                                    return o.GetActualValue()[slot];
                                }
                                catch (Exception ex)
                                {
                                    throw new Exception(ex.Message);
                                }
                            }
                            else
                            {
                                throw new Exception("Tried to do arithmetic with a non-number element of an array");
                            }
                        }
                    }

                    if (variables.ContainsKey(l))
                    {
                        FiMObject var = variables[l];
                        if (var.Type != FiMObjectTypes.NUMBER)
                            throw new Exception("Tried to do arithmetic with a non-number variable");
                        return var.GetActualValue();
                    }
                    else
                        return ConvertStringToProperValue(l, FiMObjectTypes.NUMBER);
                }
            }

            //

            string infs = "";

            if (arthm == "+")
                infs = NetFIMGlobal.Keywords.Arithmetic.Add.Infix.Where(x => line.Contains(" " + x + " ")).FirstOrDefault();
            else if (arthm == "-")
                infs = NetFIMGlobal.Keywords.Arithmetic.Subtract.Infix.Where(x => line.Contains(" " + x + " ")).FirstOrDefault();
            else if (arthm == "*")
                infs = NetFIMGlobal.Keywords.Arithmetic.Multiply.Infix.Where(x => line.Contains(" " + x + " ")).FirstOrDefault();
            else if (arthm == "/")
                infs = NetFIMGlobal.Keywords.Arithmetic.Divide.Infix.Where(x => line.Contains(" " + x + " ")).FirstOrDefault();

            string[] spl = line.Split(new string[] { infs }, StringSplitOptions.None);
            //
            ar.LeftValue = GetValue(spl[0].TrimEnd(' ')).ToString();
            ar.RightValue = GetValue(spl[1].TrimStart(' ')).ToString();

            //

            return ar;
        }

        //

        public class FiMConditional {
            public string LeftValue { get; set; }
            public string LeftVariableName { get; set; }
            public string RightValue { get; set; }
            public string RightVariableName { get; set; }
            public string Sign { get; set; }

            public bool Check()
            {
                DynValue res = Script.RunString(string.Format("return {0} {1} {2}", LeftValue, Sign, RightValue));
                return res.Boolean;
            }
        };
        private class FiMConditionalIndex
        {
            public int Index { get; set; }
            public string Line { get; set; }
        }

        public static string ConvertConditionalToSymbol(string word)
        {
            if (NetFIMGlobal.Keywords.Conditional_LessThanEqual.Any(x => word.Equals(x)))
                return "<=";
            if (NetFIMGlobal.Keywords.Conditional_GreaterThan.Any(x => word.Equals(x)))
                return ">";

            if (NetFIMGlobal.Keywords.Conditional_GreaterThanEqual.Any(x => word.Equals(x)))
                return ">=";
            if (NetFIMGlobal.Keywords.Conditional_LessThan.Any(x => word.Equals(x)))
                return "<";

            if (NetFIMGlobal.Keywords.Conditional_Not.Any(x => word.Equals(x)))
                return "~=";
            if (NetFIMGlobal.Keywords.Conditional_Equal.Any(x => word.Equals(x)))
                return "==";

            throw new Exception("Invalid conditional '" + word + "'");
        }
        public static string GetConditional(string word)
        {
            if (NetFIMGlobal.Keywords.Conditional_LessThanEqual.Any(x => word.Contains(" " + x + " ")))
                return NetFIMGlobal.Keywords.Conditional_LessThanEqual.Where(x => word.Contains(" " + x + " ")).FirstOrDefault();
            if (NetFIMGlobal.Keywords.Conditional_GreaterThan.Any(x => word.Contains(" " + x + " ")))
                return NetFIMGlobal.Keywords.Conditional_GreaterThan.Where(x => word.Contains(" " + x + " ")).FirstOrDefault();

            if (NetFIMGlobal.Keywords.Conditional_GreaterThanEqual.Any(x => word.Contains(" " + x + " ")))
                return NetFIMGlobal.Keywords.Conditional_GreaterThanEqual.Where(x => word.Contains(" " + x + " ")).FirstOrDefault();
            if (NetFIMGlobal.Keywords.Conditional_LessThan.Any(x => word.Contains(" " + x + " ")))
                return NetFIMGlobal.Keywords.Conditional_LessThan.Where(x => word.Contains(" " + x + " ")).FirstOrDefault();

            if (NetFIMGlobal.Keywords.Conditional_Not.Any(x => word.Contains(" " + x + " ")))
                return NetFIMGlobal.Keywords.Conditional_Not.Where(x => word.Contains(" " + x + " ")).FirstOrDefault();
            if (NetFIMGlobal.Keywords.Conditional_Equal.Any(x => word.Contains(" " + x + " ")))
                return NetFIMGlobal.Keywords.Conditional_Equal.Where(x => word.Contains(" " + x + " ")).FirstOrDefault();

            throw new Exception("Conditional not found");
        }

        public static FiMConditional ParseConditional(string line, Dictionary<string, FiMObject> variables)
        {
            string cond = GetConditional(line);
            string[] s_vars = line.Split(new string[] { " " + cond + " " }, StringSplitOptions.None);

            FiMConditional c = new FiMConditional
            {
                Sign = ConvertConditionalToSymbol(cond)
            };

            FiMObject GetValue(string l)
            {
                try
                {
                    GetVariableInitializerType(l, out string ph);
                    string ls = l.Substring(ph.Length + 1);

                    if (Regex.IsMatch(ls, @" \d+$"))
                    {
                        string v = Regex.Replace(ls, @" \d+$", "");
                        if (variables.ContainsKey(v))
                        {
                            int slot = int.Parse(ls.Substring(v.Length).TrimStart(' ')) - 1;

                            FiMObject o = variables[v];

                            if (!o.isArray)
                                throw new Exception("Tried to read slot " + slot + " of a non-array variable");
                            if (slot < 0 && o.GetActualValue().Count > slot)
                                throw new Exception("Invalid slot '" + slot + "'");

                            if (o.Type == FiMObjectTypes.STRING_ARRAY)
                            {
                                try
                                {
                                    string val = ReplaceStringVariables(o.GetActualValue()[slot], variables);
                                    return new FiMObject(val, FiMObjectTypes.STRING, o.isConstant, false);
                                }
                                catch (Exception ex)
                                {
                                    throw new Exception(ex.Message);
                                }
                            }
                            else
                            {
                                return FiMObject.FromArraySlot(o, slot);
                            }
                        }
                    }

                    if (variables.ContainsKey(ls))
                    {
                        if(variables[l].isArray)
                            throw new Exception("Tried to use a whole array as a conditional value");
                        return variables[ls];
                    }
                    else
                    {
                        dynamic value = GuessProperValueFromString(ls, out FiMObjectTypes type);

                        return new FiMObject(value, type, false, false);
                    }
                }
                catch
                {
                    if (Regex.IsMatch(l, @" \d+$"))
                    {
                        string v = Regex.Replace(l, @" \d+$", "");
                        if (variables.ContainsKey(v))
                        {
                            int slot = int.Parse(l.Substring(v.Length).TrimStart(' ')) - 1;

                            FiMObject o = variables[v];

                            if (!o.isArray)
                                throw new Exception("Tried to read slot " + slot + " of a non-array variable");
                            if (slot < 0 && o.GetActualValue().Count > slot)
                                throw new Exception("Invalid slot '" + slot + "'");

                            if (o.Type == FiMObjectTypes.STRING_ARRAY)
                            {
                                try
                                {
                                    string val = ReplaceStringVariables(o.GetActualValue()[slot], variables);
                                    return new FiMObject(val, FiMObjectTypes.STRING, o.isConstant, false);
                                }
                                catch (Exception ex)
                                {
                                    throw new Exception(ex.Message);
                                }
                            }
                            else
                            {
                                return FiMObject.FromArraySlot(o, slot);
                            }
                        }
                    }

                    if (variables.ContainsKey(l))
                    {
                        if (variables[l].isArray)
                            throw new Exception("Tried to use a whole array as a conditional value");
                        return variables[l];
                    }
                    else
                    {
                        dynamic value = GuessProperValueFromString(l, out FiMObjectTypes type);

                        return new FiMObject(value, type, false, false);
                    }
                }
            };

            FiMObject lv = GetValue(s_vars[0]);
            FiMObject rv = GetValue(s_vars[1]);

            //

            if (lv.Type != rv.Type)
                throw new Exception(string.Format("Tried to compare between '{0}' and '{1}",lv.Type,rv.Type));

            c.LeftValue = lv.Value.ToString();
            c.LeftVariableName = s_vars[0];
            c.RightValue = rv.Value.ToString();
            c.RightVariableName = s_vars[1];
                        
            //

            return c;
        }

        public static FiMConditional ParseConditional(string var1, string var2, string cond, Dictionary<string, FiMObject> variables)
        {
            FiMConditional c = new FiMConditional
            {
                Sign = cond,
            };

            FiMObject GetValue(string l)
            {
                try
                {
                    GetVariableInitializerType(l, out string ph);
                    string ls = l.Substring(ph.Length + 1);

                    if (Regex.IsMatch(ls, @" \d+$"))
                    {
                        string v = Regex.Replace(ls, @" \d+$", "");
                        if (variables.ContainsKey(v))
                        {
                            int slot = int.Parse(ls.Substring(v.Length).TrimStart(' ')) - 1;

                            FiMObject o = variables[v];

                            if (!o.isArray)
                                throw new Exception("Tried to read slot " + slot + " of a non-array variable");
                            if (slot < 0 && o.GetActualValue().Count > slot)
                                throw new Exception("Invalid slot '" + slot + "'");

                            if (o.Type == FiMObjectTypes.STRING_ARRAY)
                            {
                                try
                                {
                                    string val = ReplaceStringVariables(o.GetActualValue()[slot], variables);
                                    return new FiMObject(val, FiMObjectTypes.STRING, o.isConstant, false);
                                }
                                catch (Exception ex)
                                {
                                    throw new Exception(ex.Message);
                                }
                            }
                            else
                            {
                                return FiMObject.FromArraySlot(o, slot);
                            }
                        }
                    }

                    if (variables.ContainsKey(ls))
                    {
                        if (variables[l].isArray)
                            throw new Exception("Tried to use a whole array as a conditional value");
                        return variables[ls];
                    }
                    else
                    {
                        dynamic value = GuessProperValueFromString(ls, out FiMObjectTypes type);

                        return new FiMObject(value, type, false, false);
                    }
                }
                catch
                {
                    if (Regex.IsMatch(l, @" \d+$"))
                    {
                        string v = Regex.Replace(l, @" \d+$", "");
                        if (variables.ContainsKey(v))
                        {
                            int slot = int.Parse(l.Substring(v.Length).TrimStart(' ')) - 1;

                            FiMObject o = variables[v];

                            if (!o.isArray)
                                throw new Exception("Tried to read slot " + slot + " of a non-array variable");
                            if (slot < 0 && o.GetActualValue().Count > slot)
                                throw new Exception("Invalid slot '" + slot + "'");

                            if (o.Type == FiMObjectTypes.STRING_ARRAY)
                            {
                                try
                                {
                                    string val = ReplaceStringVariables(o.GetActualValue()[slot], variables);
                                    return new FiMObject(val, FiMObjectTypes.STRING, o.isConstant, false);
                                }
                                catch (Exception ex)
                                {
                                    throw new Exception(ex.Message);
                                }
                            }
                            else
                            {
                                return FiMObject.FromArraySlot(o, slot);
                            }
                        }
                    }

                    if (variables.ContainsKey(l))
                    {
                        if (variables[l].isArray)
                            throw new Exception("Tried to use a whole array as a conditional value");
                        return variables[l];
                    }
                    else
                    {
                        dynamic value = GuessProperValueFromString(l, out FiMObjectTypes type);

                        return new FiMObject(value, type, false, false);
                    }
                }
            };

            FiMObject lv = GetValue(var1);
            FiMObject rv = GetValue(var2);

            //

            if (lv.Type != rv.Type)
                throw new Exception(string.Format("Tried to compare between '{0}' and '{1}", lv.Type, rv.Type));

            c.LeftValue = lv.Value.ToString();
            c.LeftVariableName = var1;
            c.RightValue = rv.Value.ToString();
            c.RightVariableName = var2;

            //

            return c;
        }

    }
}
