using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using NetFIM.Methods;

namespace NetFIM.Class
{
    class FiMReport
    {
        public Dictionary<string, FiMParagraph> Paragraphs { get; set; }
        public Dictionary<string, FiMObject> GlobalVariables { get; set; }
        
        public string ReportName { get; set; }
        public string Writer { get; set; }

        public dynamic RunParagraph(string paragraphName, FiMObject paramObject = null)
        {

            if(!Paragraphs.ContainsKey(paragraphName))
                throw new Exception("Invalid Paragraph name");

            FiMParagraph paragraph = Paragraphs[paragraphName];
            // Empty function
            if (paragraph.Lines.Count() == 0)
            {
                return null;
            }

            List<string> paragraphLines = new List<string>();
            paragraphLines.AddRange(paragraph.Lines);

            for(int i = 0; i < paragraphLines.Count(); i++)
            {
                if(!string.IsNullOrWhiteSpace(paragraphLines[i]) && !Regex.IsMatch(paragraphLines[i], @"( |\t)*(P\.)(P\.)*S\.($| )"))
                {
                    string nl = paragraphLines[i];
                    if (nl.StartsWith("    "))
                        nl = nl.Remove(0, "    ".Length);
                    else if (nl.StartsWith("\t"))
                        nl = nl.Remove(0, 1);
                    else
                        throw new Exception("Invalid indentation at line '" + paragraph.FirstLineIndex + i + "'");
                        
                    paragraphLines[i] = nl;
                }
            }

            //
            Dictionary<string, FiMObject> LocalVariables = new Dictionary<string, FiMObject>();

            if(paragraph.ParameterName != null)
                LocalVariables.Add(paragraph.ParameterName, paramObject);

            int lineIndex = 1;
            int skipTo = -1;
            string[] pLArray = paragraphLines.ToArray();
            foreach (string line in paragraphLines)
            {
                if(lineIndex <= skipTo)
                {
                    lineIndex++;
                    continue;
                }

                //Console.WriteLine(line);

                if (!string.IsNullOrWhiteSpace(line) && !Regex.IsMatch(line, @"( |\t)*(P\.)(P\.)*S\.($| )"))
                {
                   
                    NetFIMMethods.FiMMethodOut method;
                    try {
                        method = NetFIMMethods.ScanMethod(this, line, pLArray, paragraph.FirstLineIndex + lineIndex + 1, lineIndex, paragraph, variables: NetFIMMethods.MergeLocalAndGlobalVariables(GlobalVariables,LocalVariables), paragraphs: Paragraphs);
                    }
                    catch(Exception e) { throw e; }

                    //

                    switch(method.methodType)
                    {
                        case "WRITE-RUN":
                            {
                                if (!Paragraphs.ContainsKey(method.outArgs["Method Name"]))
                                    throw new Exception("[" + paragraph.FirstLineIndex + lineIndex + "] Paragraph '" + method.outArgs["Method Name"] + "' doesn't exist");

                                if (method.outArgs["doWrite"] == true)
                                    NetFIMMethods.WriteToScreen(RunParagraph(method.outArgs["Method Name"], method.outArgs["Parameter"]));
                                else
                                    RunParagraph(method.outArgs["Method Name"], method.outArgs["Parameter"]);
                            }
                            break;
                        case "MAKE-VARIABLE":
                            {
                                LocalVariables.Add(method.outArgs["Variable Name"], method.outArgs["Variable"]);
                            }
                            break;
                        case "MODIFY-ARRAY":
                            {
                                // In the case of Paragraph ScanMethod, we either put it on the Global or Local list.
                                if (GlobalVariables.ContainsKey(method.outArgs["Variable Name"]))
                                {
                                    var ol = GlobalVariables[method.outArgs["Variable Name"]].GetActualValue();
                                    ol[method.outArgs["Slot"]] = method.outArgs["New Value"];
                                    GlobalVariables[method.outArgs["Variable Name"]].Value = (object)ol;
                                }
                                else
                                {
                                    var ol = LocalVariables[method.outArgs["Variable Name"]].GetActualValue();
                                    ol[method.outArgs["Slot"]] = method.outArgs["New Value"];
                                    LocalVariables[method.outArgs["Variable Name"]].Value = (object)ol;
                                }
                            }
                            break;
                        case "MODIFY-VARIABLE":
                            {
                                if(GlobalVariables.ContainsKey(method.outArgs["Variable Name"]))
                                    GlobalVariables[method.outArgs["Variable Name"]] = method.outArgs["New Value"];
                                else
                                    LocalVariables[method.outArgs["Variable Name"]] = method.outArgs["New Value"];
                            }
                            break;

                        case "VARIABLE-INCREMENT":
                            {
                                if (GlobalVariables.ContainsKey(method.outArgs["Variable Name"]))
                                {
                                    float oldValue = GlobalVariables[method.outArgs["Variable Name"]].GetActualValue();
                                    oldValue++;
                                    GlobalVariables[method.outArgs["Variable Name"]].Value = (object)oldValue;
                                }
                                else
                                {
                                    float oldValue = LocalVariables[method.outArgs["Variable Name"]].GetActualValue();
                                    oldValue++;
                                    LocalVariables[method.outArgs["Variable Name"]].Value = (object)oldValue;
                                }
                            }
                            break;
                        case "VARIABLE-DECREMENT":
                            {
                                if (GlobalVariables.ContainsKey(method.outArgs["Variable Name"]))
                                {
                                    float oldValue = GlobalVariables[method.outArgs["Variable Name"]].GetActualValue();
                                    oldValue--;
                                    GlobalVariables[method.outArgs["Variable Name"]].Value = (object)oldValue;
                                }
                                else
                                {
                                    float oldValue = LocalVariables[method.outArgs["Variable Name"]].GetActualValue();
                                    oldValue--;
                                    LocalVariables[method.outArgs["Variable Name"]].Value = (object)oldValue;
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

                                        if (GlobalVariables.ContainsKey(x))
                                            GlobalVariables[x] = obj;
                                        else
                                            LocalVariables[x] = obj;
                                    });

                                }

                                if (method.outArgs.ContainsKey("Returned Variable"))
                                    return method.outArgs["Returned Variable"];
                            }
                            break;
                        case "IF-ELSE":
                            {
                                skipTo = lineIndex + method.outArgs["Skip"]+1;
                                //
                                if(method.outArgs.ContainsKey("Statement"))
                                {
                                    NetFIMMethods.FiMStatement statement = method.outArgs["Statement"];
                                    statement.changedVariables.Keys.ToList().ForEach(x =>
                                    {
                                        FiMObject obj = statement.changedVariables[x];

                                        if (GlobalVariables.ContainsKey(x))
                                            GlobalVariables[x] = obj;
                                        else
                                            LocalVariables[x] = obj;
                                    });

                                    if (statement.hasReturned)
                                        return statement.returnedObject;
                                }
                            }
                            break;

                        case "RETURN":
                                return method.outArgs["Variable"];

                        case "WRITE-WRITE":
                        default:
                            break;
                    }

                }

                lineIndex++;
            }

            return null;
        }
        
        /// <returns>Changed variables only</returns>
        public Dictionary<string, FiMObject> RunStatement(string[] lines, Dictionary<string, FiMObject> variables)
        {
            Dictionary<string, FiMObject> changedVariables = new Dictionary<string, FiMObject>();



            return changedVariables;
        }

        public FiMReport()
        {
            Paragraphs = new Dictionary<string, FiMParagraph>();
            GlobalVariables = new Dictionary<string, FiMObject>();
        }
    }

    //
    class FiMParagraph
    {
        private bool _isMainFunction { get; set; }
        public bool isMainFunction { get { return _isMainFunction; } }

        private List<string> _Lines { get; set; }
        public List<string> Lines { get { return _Lines; } }

        private FiMObjectTypes _ReturnType { get; set; }
        public FiMObjectTypes ReturnType { get { return _ReturnType; } }

        private FiMObjectTypes _ParameterType { get; set; }
        public FiMObjectTypes ParameterType { get { return _ParameterType; } }

        private int _FirstLineIndex { get; set; }
        public int FirstLineIndex { get { return _FirstLineIndex; } }

        private string _ParameterName { get; set; }
        public string ParameterName { get { return _ParameterName; } }

        //

        public FiMParagraph(List<string> lines, int firstLine, bool isMainFunction = false, FiMObjectTypes returnType = FiMObjectTypes.UNKNOWN, FiMObjectTypes paramType = FiMObjectTypes.UNKNOWN, string paramName = null)
        {
            this._Lines = lines;
            this._ReturnType = returnType;
            this._ParameterType = paramType;
            this._ParameterName = paramName;

            this._isMainFunction = isMainFunction;

            this._FirstLineIndex = firstLine;
        }
    }

    //
    enum FiMObjectTypes
    {
        NUMBER,
        NUMBER_ARRAY,
        CHAR,
        STRING, // aka CHAR_ARRAY
        STRING_ARRAY,
        BOOL,
        BOOL_ARRAY,

        UNKNOWN,
    }
    class FiMObject
    {
        private FiMObjectTypes _Type { get; set; }
        public FiMObjectTypes Type { get { return _Type; } }
        
        public object Value { get; set; }

        private bool _isConstant { get; set; }
        public bool isConstant { get { return _isConstant; } }

        private bool _isArray { get; set; }
        public bool isArray { get { return _isArray; } }

        //

        public dynamic GetActualValue()
        {

            if(Value == null)
            {
                return "nothing";
            }

            switch(Type)
            {
                case FiMObjectTypes.NUMBER:
                        return float.Parse(Value.ToString());
                case FiMObjectTypes.NUMBER_ARRAY:
                        return (float[])Value;
                case FiMObjectTypes.CHAR:
                        return char.Parse(Value.ToString());
                case FiMObjectTypes.STRING:
                        return Value.ToString();
                case FiMObjectTypes.STRING_ARRAY:
                        return (string[])Value;
                case FiMObjectTypes.BOOL:
                        return bool.Parse(Value.ToString());
                case FiMObjectTypes.BOOL_ARRAY:
                        return (bool[])Value;
            }

            throw new Exception("Invalid variable type");
        }

        public FiMObject(object Value, FiMObjectTypes Type, bool isConstant = false, bool isArray = false)
        {
            this.Value = Value;
            this._Type = Type;
            this._isConstant = isConstant;
            this._isArray = isArray;
        }
        public static FiMObject FromArraySlot(FiMObject parent, int slot)
        {
            if (!parent.isArray)
                throw new Exception("Tried to make a new FiMObject from a non-array variable");
            //
            switch (parent.Type)
            {
                case FiMObjectTypes.NUMBER_ARRAY:
                    {
                        var ar = (float[])parent.Value;
                        return new FiMObject(ar[slot],FiMObjectTypes.NUMBER, parent.isConstant, parent.isArray);
                    }
                case FiMObjectTypes.STRING_ARRAY:
                    {
                        var ar = (string[])parent.Value;
                        return new FiMObject(ar[slot], FiMObjectTypes.STRING, parent.isConstant, parent.isArray);
                    }
                case FiMObjectTypes.BOOL_ARRAY:
                    {
                        var ar = (bool[])parent.Value;
                        return new FiMObject(ar[slot], FiMObjectTypes.BOOL, parent.isConstant, parent.isArray);
                    }
                default:
                    throw new Exception("Invalid variable type");
            }
        }
    }
}