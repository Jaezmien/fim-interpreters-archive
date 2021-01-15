using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime;
using System.Linq;
using System.Drawing;
using System.Threading.Tasks;
using System.Threading;
using System.Text.RegularExpressions;
using Console = Colorful.Console;
//
using Melody.Helper;
//
using NetFiM.Methods;
using NetFiM.Global;
//
using MoonSharp.Interpreter;

// Despacito

namespace NetFiM
{
	class Program
	{
		// FUCKING SHIT, BLESS YOU MOONSHARP
		// YOU WORK ON BOTH 32 BIT AND 64 BIT PCS
		// WITHOUT ME HAVING TO DO ANY MORE SHIT
		public static bool LuaComparison(string a) {
			DynValue res = Script.RunString(a);
			return res.Boolean;
		}
		public static void Main(string[] args)
		{
			
			// Initialize Fancy InfoText Stylesheet
			// Colors from: http://helmet.kafuka.org/ponycolors
			// Lib used: Colorful.Console
			
			// MANE 6
			FancyInfoText.AddStyle("(applejack)",       Color.FromArgb(1,252,186, 99),match=>"Applejack" );
			FancyInfoText.AddStyle("(fluttershy)",      Color.FromArgb(1,253,246,174),match=>"Fluttershy");
			FancyInfoText.AddStyle("(pinkie pie)",      Color.FromArgb(1,246,183,210),match=>"Pinkie Pie");
			FancyInfoText.AddStyle("(rarity)",          Color.FromArgb(1,236,239,241),match=>"Rarity"    );
			
			// ROYALTY
			FancyInfoText.AddStyle("(princess cadence)",  Color.FromArgb(1,255,204,230),match=>"Princess Cadence" );
			FancyInfoText.AddStyle("(princess luna)",     Color.FromArgb(1, 55, 63,123),match=>"Princess Luna"    );
			FancyInfoText.AddStyle("(princess celestia)", Color.FromArgb(1,255,249,253),match=>"Princess Celestia"); // Dont ask why
			FancyInfoText.AddStyle("(queen chrysalis)",   Color.FromArgb(1, 43, 43, 43),match=>"Queen Chyrsalis"  );
			
			// FOALS
			FancyInfoText.AddStyle("(scootaloo)",Color.FromArgb(1,251,186,100));
			
			//
			while(true) {
				Console.WriteLine("[Melody]: Please input the folder name.");
				Console.Write(">> ");
				string input = Console.ReadLine();
				switch(input) {
						case "--dir": {
							Console.WriteLine("--[[ LIST OF CURRENTLY AVAILABLE FOLDER(S) TO RUN ]]--");
							foreach(string d in Directory.GetDirectories(Environment.CurrentDirectory))
								d.Split('\\').LastOrDefault().WriteLine();
							Console.WriteLine();
						}
						break;
						case "--cls": {
							Console.Clear();
						}
						break;
						case "--cmds": 
						case "--cmd": {
							Console.WriteLine("--[[ LIST OF CURRENTLY AVAILABLE COMMANDS ]]--");
							Console.WriteLine("--dir\n--cls");
						}
						break;
						default: {
							string folderName = input.TrimEnd('\\');
							Console.Clear();
							Console.ReplaceAllColorsWithDefaults();
							Console.WriteLine("---------------------------------------------",Color.DarkSlateGray);
							if(Directory.Exists(folderName)) {
								string[] files = Directory.GetFiles(folderName);
								string[] main = File.ReadAllLines(files.Where(x=>x.EndsWith("main.fpp")).FirstOrDefault());
								Run(main);
								Author = ""; ScrollTitle = "";
							} else {
								FIMOutput.Error("ERROR, Directory doesn't exist.");
							}
							Console.WriteLine("--[  END OF OUTPUT  ]--");
							Console.WriteLine("---------------------------------------------",Color.DarkSlateGray);
						}
						break;
				}
			}
			
		}
		
		public static void Run(string[] main) {
			main = FIMMethods.RemoveComments(main).UnfuckAllQuotes();
			// Remove spaces/tabs on the start
			// you still can't write one-liners but hey atleast you're not forced on tabs/space
			for(int i=0;i<main.Length;i++) {
				main[i]=main[i].TrimStart(' ').Replace("\t","");
			}
			#region Punctuation Check
			int _puncError = 0;
			bool gotError;
			if(!main.AllContainsPunctuation(out _puncError)) {
				// ERROR: NOT EVERYTHING HAS PUNCTUATIONS
				FIMOutput.Error("MISSING PUNCTUATION AT LINE " + _puncError);
				return;
			}
			#endregion
			main = main.RemovePunctuation().RemoveEmptyEnd();
			if(FIMMethods.CheckStartEndValidity(main)) {
				var info = FIMMethods.GetReportDetails(main);
				main = main.RemoveReportStartEnd();
				Console.WriteLine("FRIENDSHIP REPORT: {0}".MelodyFormat(info.Title));
				// SPECIAL CASE (two or more colors)
				Console.Write("BY: ");
				switch(info.Author.ToLower()) {
						case "twilight sparkle": {
							Console.WriteWithGradient("Twilight Sparkle",  Color.FromArgb(1, 39, 56,115), Color.FromArgb(1,209,159,228),3);
							Console.WriteLine();
						}
						break;
						case "rainbow dash": {
							// This is actually pushing the console color's limits lmao
							// Will be replaced when a fix/workaround has been found
							/*Console.Write("R",Color.FromArgb(1,238,65,68));
							Console.Write("a",Color.FromArgb(1,243,112,51));
							Console.Write("i",Color.FromArgb(1,243,112,51));
							Console.Write("n",Color.FromArgb(1,253,246,174));
							Console.Write("b",Color.FromArgb(1,253,246,174));
							Console.Write("o",Color.FromArgb(1,98,188,77));
							Console.Write("w ",Color.FromArgb(1,98,188,77));
							Console.Write("D",Color.FromArgb(1,30,152,213));
							Console.Write("a",Color.FromArgb(1,30,152,213));
							Console.Write("s",Color.FromArgb(1,102,47,137));
							Console.Write("h",Color.FromArgb(1,238,65,68));*/
							Console.Write("R",Color.DarkGreen);
							Console.Write("a",Color.DarkCyan);
							Console.Write("i",Color.DarkCyan);
							Console.Write("n",Color.DarkRed);
							Console.Write("b",Color.DarkRed);
							Console.Write("o",Color.DarkViolet);
							Console.Write("w ",Color.DarkViolet);
							Console.Write("D",Color.Yellow);
							Console.Write("a",Color.Yellow);
							Console.Write("s",Color.DarkGray);
							Console.WriteLine("h",Color.DarkGreen);
						}
						break;
						case "vinyl melody": {
							Console.Write("Vinyl ",    Color.FromArgb(1,255,197, 83)); // Mane Color
							Console.WriteLine("Melody",Color.FromArgb(1,153,157,255)); // Eye Color (Bright)
						}
						break;
						// Foals
						case "applebloom": {
							Console.Write("Apple",    Color.FromArgb(1,247, 65, 96)); // Mane Color
							Console.WriteLine("bloom",Color.FromArgb(1,244,244,155)); // Coat Color
						}
						break;
						case "sweetie belle": {
							Console.Write("Sweetie ",    Color.FromArgb(1,178,141,192)); // Mane Color
							Console.WriteLine("Belle",   Color.FromArgb(1,239,237,238)); // Coat Color
						}
						break;
						
						// :D
						case "sweetie bot": {
							Console.WriteWithGradient("Sweetie",  Color.FromArgb(1,178,141,192),Color.FromArgb(1,239,237,238),2); // Mane Color
							Console.Write(" ");
							Console.WriteWithGradient("Bot",Color.FromArgb(1,99,150,89),Color.FromArgb(1,222,7,36),2);
							Console.WriteLine();
						}
						break;
						case "sweetiebot": {
							Console.WriteWithGradient("Sweetie",  Color.FromArgb(1,178,141,192),Color.FromArgb(1,239,237,238),2); // Mane Color
							Console.WriteWithGradient("Bot",Color.FromArgb(1,99,150,89),Color.FromArgb(1,222,7,36),2);
							Console.WriteLine();
						}
						break;
						
						// Other names
						default: {
							bool gotMatch=FancyInfoText.Styles.Any(x => Regex.IsMatch(info.Author.ToLower(), x.Target.Value));
							if(gotMatch)
								Console.WriteLineStyled(info.Author.ToLower(),FancyInfoText);
							else {
								Console.WriteLine(info.Author,Color.White);
							}
						}
						break;
				}
				Console.ResetColor();
				Console.WriteLine("\n--[ START OF OUTPUT ]--");
				string _validMethodsError;
				if(FIMMethods.AreMethodsValid(main, out _validMethodsError)) {
					// p l e a s e  h e l p  m e (aka Start Here)
					FriendshipReport = new ReportContent();
					#region Get Global Variables
					bool _tempGVarError;
					Dictionary<string, object[]> _tempGVariables = main.GetAllGlobalVariables(out _tempGVarError);
					if(_tempGVarError)
						return;
					foreach(string k in _tempGVariables.Keys) {
						object[] val = _tempGVariables[k];
						FriendshipReport.GlobalVariables.Add(k, new ReportVariable() {Type = (string)val[0], Value = val[1]});
					}
					#endregion
					#region Get Main Method
					string _mainName;
					string[] _tempMain = FIMMethods.GetMainMethodLines(main, out _mainName);
					if(_tempMain.Count() == 0) {
						// MAIN METHOD IS EMPTY
						// Not sure why you'd do that but ok
						return;
					}
					if(_tempMain[0] == null) {
						FIMOutput.Error(_tempMain[1]);
						return;
					}
					if(!FIMMethods.ValidIfElseConditional(_tempMain)) {
						FIMOutput.Error("ERROR ON MAIN METHOD IF-ELSE CHECKING");
						return;
					}
					if(!FIMMethods.ValidWhileConditional(_tempMain)) {
						FIMOutput.Error("ERROR ON MAIN METHOD WHILE LOOP CHECKING");
						return;
					}
					FriendshipReport.MainMethod = new MethodContent(_tempMain,"","","");
					FriendshipReport.MainMethodName = _mainName;
					#endregion
					#region (If possible) Get other methods
					if(main.Any(x => x.StartsWith(FIMGlobal.MethodStart))) {
						Dictionary<string, object[]> _localMethods = FIMMethods.GetLocalMethodLines(main, out gotError);
						if(gotError)
							return;
						foreach(string k in _localMethods.Keys) {
							object[] cont = _localMethods[k];
							if(!FIMMethods.ValidIfElseConditional((string[])cont[3])) {
								FIMOutput.Error("ERROR ON LOCAL METHOD IF-ELSE CHECKING ("+k+")");
								return;
							}
							if(!FIMMethods.ValidWhileConditional((string[])cont[3])) {
								FIMOutput.Error("ERROR ON LOCAL METHOD WHILE LOOP CHECKING ("+k+")");
								return;
							}
							FriendshipReport.LocalMethods.Add(k, new MethodContent((string[])cont[3],(string)cont[0],(string)cont[1],(string)cont[2]));
						}
					}
					#endregion
					object _empty;
					RunMethod(FriendshipReport.MainMethod.MethodLines, out _empty);
				} else {
					FIMOutput.Error("INVALID METHOD >> " + _validMethodsError);
				}
			} else {
				FIMOutput.Error("INVALID REPORT START/END");
			}
		}
		
		public static void RunMethod(string[] lines, out object returnVar, bool isMain = true, string MethodName = "", object[] Param = null, Dictionary<string, ReportVariable> localVar = null) {
			bool gotError = false;
			object _emptyObject;
			returnVar = null;
			Dictionary<string, ReportVariable> localVariable = new Dictionary<string, ReportVariable>();
			if(localVar != null) {
				foreach(string k in localVar.Keys) {
					localVariable.Add(k, localVar[k]);
				}
			}
			if(Param != null)
				localVariable.Add(Param[0].ToString(), new ReportVariable() {Value=Param[1],Type=Param[2].ToString()});
			int skipLine = -1;
			int currLine = 0;
			foreach(string s in lines) {
				//Console.WriteLine(currLine + " - " + skipLine);
				if(currLine <= skipLine) {
					currLine++;
					continue;
				}
				if(!string.IsNullOrEmpty(s)) {
					var _temp = ScanMethod(s, lines, currLine, localVariable, MethodName);
					if(!_temp.ContainsKey("ERROR")) {
						switch((string)_temp["Method"]) {
								case "AddVariable": {
									var _t = _temp["newVariable"] as object[];
									localVariable.Add((string)_t[0], new ReportVariable() {Value = _t[1], Type = (string)_t[2]});
								}
								break;
								case "ReplaceVariable": {
									object[] nV = _temp["variable"] as object[];
									if((bool)_temp["isGlobal"]) {
										FriendshipReport.GlobalVariables[(string)nV[0]].Value = nV[1];
									} else {
										localVariable[(string)nV[0]].Value = nV[1];
									}
								}
								break;
								case "IncrementVariable": {
									string v = _temp["variable"] as string;
									int amount=1;
									if(_temp.ContainsKey("amount"))
										amount=(int)_temp["amount"];
									if((bool)_temp["isGlobal"]) {
										int val = (int)FriendshipReport.GlobalVariables[v].Value + amount;
										FriendshipReport.GlobalVariables[v].Value = (object)val;
									} else {
										int val = (int)localVariable[v].Value + amount;
										localVariable[v].Value = (object)val;
									}
								}
								break;
								case "DecrementVariable": {
									string v = _temp["variable"] as string;
									int amount=1;
									if(_temp.ContainsKey("amount"))
										amount=(int)_temp["amount"];
									if((bool)_temp["isGlobal"]) {
										int val = (int)FriendshipReport.GlobalVariables[v].Value - amount;
										FriendshipReport.GlobalVariables[v].Value = (object)val;
									} else {
										int val = (int)localVariable[v].Value - amount;
										localVariable[v].Value = (object)val;
									}
								}
								break;
								case "IfElse":
								case "While":
								case "Write":
								case "RunMethod":
								break;
								case "Return": {
									returnVar = (object)_temp["Variable"];
									return;
								}
								default: { 
									FIMOutput.Debug("Oops, didn't handle " + (string)_temp["Method"] + " yet!\nLINE - " + s);
								}
								break;
						}
					} else return;
					if(_temp.ContainsKey("skipLine")) {
						skipLine = (int)_temp["skipLine"];
					}
				}				
				/*   */
				currLine++;
			}
			if(!isMain) {
				if(FriendshipReport.LocalMethods[MethodName].ReturnType != "") {
					gotError = true;
					FIMOutput.Error("RETURNING METHOD DIDN'T RETURN A VARIABLE!");
					return;
				}
			}
		}
		
		// [0] contains any changed variables (in the form of Dictionary), [1] contains possible return value
		public static object[] RunIfElse(string[] lines, Dictionary<string, ReportVariable> accessibleVars, out bool gotError, string MethodName = "") {
			// Much easier to change variable if it's a dictionary
			Dictionary<string, object> changedVars = new Dictionary<string, object>();
			gotError = false;
			
			// Different from the RunMethod
			// These are the variables that are ONLY accessible on the if-else itself and will be discarded once
			// the if-else is finished
			// This will be merged with the accessibleVars.
			Dictionary<string, ReportVariable> localVars = new Dictionary<string, ReportVariable>();
			// Add all existing variables to the temporary dictionary
			foreach(string k in accessibleVars.Keys)
				localVars.Add(k, accessibleVars[k]);
			
			object returnValue = null;
			//
			int skipLine = -1;
			int currLine = 0;
			foreach(string s in lines) {
				
				if(currLine <= skipLine) {
					currLine++;
					continue;
				}
				
				if(!string.IsNullOrEmpty(s)) {
					var _temp = ScanMethod(s, lines, currLine, localVars);
					if(!_temp.ContainsKey("ERROR")) {
						switch((string)_temp["Method"]) {
								case "AddVariable": {
									var _t = _temp["newVariable"] as object[];
									localVars.Add((string)_t[0], new ReportVariable() {Value = _t[1], Type = (string)_t[2]});
								}
								break;
								case "ReplaceVariable": {
									object[] nV = _temp["variable"] as object[];
									if((bool)_temp["isGlobal"]) {
										FriendshipReport.GlobalVariables[(string)nV[0]].Value = nV[1];
									} else {
										localVars[(string)nV[0]].Value = nV[1];
										if(!changedVars.ContainsKey((string)nV[0])) 
											changedVars.Add((string)nV[0],nV[1]);
										else
											changedVars[(string)nV[0]] = nV[1];
									}
								}
								break;
								case "IncrementVariable": {
									string v = _temp["variable"] as string;
									int amount=1;
									if(_temp.ContainsKey("amount"))
										amount=(int)_temp["amount"];
									if((bool)_temp["isGlobal"]) {
										int val = (int)FriendshipReport.GlobalVariables[v].Value + amount;
										FriendshipReport.GlobalVariables[v].Value = (object)val;
									} else {
										int val = (int)localVars[v].Value + amount;
										localVars[v].Value = (object)val;
										if(!changedVars.ContainsKey(v))
											changedVars.Add(v, (object)val);
										else
											changedVars[v] = (object)val;
									}
								}
								break;
								case "DecrementVariable": {
									string v = _temp["variable"] as string;
									int amount=1;
									if(_temp.ContainsKey("amount"))
										amount=(int)_temp["amount"];
									if((bool)_temp["isGlobal"]) {
										int val = (int)FriendshipReport.GlobalVariables[v].Value - amount;
										FriendshipReport.GlobalVariables[v].Value = (object)val;
									} else {
										int val = (int)localVars[v].Value - amount;
										localVars[v].Value = (object)val;
										if(!changedVars.ContainsKey(v))
											changedVars.Add(v, (object)val);
										else
											changedVars[v] = (object)val;
									}
								}
								break;
								case "Write":
								case "IfElse":
								break;
								default: { 
									FIMOutput.Debug("Oops, didn't handle " + (string)_temp["Method"] + " yet!\nLINE - " + s);
								}
								break;
						}
					} else {
						gotError=true;
						return new object[]{};
					}
					if(_temp.ContainsKey("skipLine")) {
						skipLine = (int)_temp["skipLine"];
					}
				}
				
				currLine++;
				
			}
			//
			return new object[] {changedVars, returnValue};
		}
		
		public static Dictionary<string,object> ScanMethod(string line, string[] lines, int currLine, Dictionary<string, ReportVariable> vars, string MethodName = "") {
			bool gotError = false;
			object _emptyObject;
			Dictionary<string, object> ret = new Dictionary<string, object>();
			ret.Add("Method","");
			goto Search;
			// oh cool, goto!
		Error:
			return new Dictionary<string, object>(){{"ERROR","ERROR"}};
		Search:
			// IF-ELSE
			if(FIMGlobal.Keywords.If_Start.Any(x => line.StartsWith(x + " "))) {
				ret["Method"] = "IfElse";
				if(!FIMGlobal.CompareKeywordList.Any(x => line.Contains(" " + x + " "))) {
					FIMOutput.Error("NO COMPARISON KEYWORD DETECTED!\n\nLINE > " + line);
					goto Error;
				}
				List<string> ifLines = new List<string>();
				// Get all lines
				for(int i = currLine; i < lines.Count(); i++) {
					string l = lines[i];
					ifLines.Add(l);
					if(l.Equals(FIMGlobal.Keywords.If_End)) {
						ret.Add("skipLine", i);
						break;
					}
				}
				// TODO: IMPLEMENT IF-ELSE COMPLETELY
				// {POSSIBLE RETURN VALUE}
				object[] results = RunFunction.IfElse(ifLines.ToArray(), vars, out gotError);
				if(gotError) {
					goto Error;
				}
				if(results[1] != null) {
					"got return".WriteLine();
					goto Error;
				}
				/*kif(results.Count() > 0) {
					var cvD = results[0] as Dictionary<string, object>;
					if(cvD != null && cvD.Count() > 0) {
						// ZIPPING UP
						List<object[]> changedVars = new List<object[]>();
						foreach(string k in cvD.Keys) {
							changedVars.Add(new object[] {k, cvD[k]});
						}
						ret.Add("ChangedVariables", changedVars);
					}
					if(results[1] != null) {
						ret.Add("IfElseMethod","Return");
					}
				}*/
				//continue;
				return ret;
			}
			// WHILE
			if(FIMGlobal.Keywords.Loop_While.Any(x => line.StartsWith(x + " "))) {
				ret["Method"] = "While";
				if(!FIMGlobal.CompareKeywordList.Any(x => line.Contains(" " + x + " "))) {
					FIMOutput.Error("NO COMPARISON KEYWORD DETECTED!\n\nLINE > " + line);
					goto Error;
				}
				List<string> ifLines = new List<string>();
				// Get all lines
				for(int i = currLine; i < lines.Count(); i++) {
					string l = lines[i];
					ifLines.Add(l);
					if(l.Equals(FIMGlobal.Keywords.Loop_End)) {
						ret.Add("skipLine", i);
						break;
					}
				}
				// TODO: IMPLEMENT WHILE LOOP COMPLETELY
				// {POSSIBLE RETURN VALUE}
				bool isDone = false;
				object[] results = new object[2];
				
				//object[] _t = RunFunction.While(ifLines.ToArray(), vars, out gotError, out isDone);
				while(!isDone && !gotError) {
					object[] _t = RunFunction.While(ifLines.ToArray(), vars, out gotError, out isDone);
					if(!isDone)
						results = _t;
				}
				if(gotError) {
					goto Error;
				}
				/*if(results.Count() > 0) {
					var cvD = results[0] as Dictionary<string, object>;
					if(cvD != null && cvD.Count() > 0) {
						// ZIPPING UP
						List<object[]> changedVars = new List<object[]>();
						foreach(string k in cvD.Keys) {
							changedVars.Add(new object[] {k, cvD[k]});
						}
						ret.Add("ChangedVariables", changedVars);
					}
					if(results[1] != null) {
						ret.Add("IfElseMethod","Return");
					}
				}*/
				//continue;
				return ret;
			}
			// INCREMENT-DECREMENT
			#region Increment
			else if(line.StartsWith(FIMGlobal.Keywords.Variable_Increment.Start + " ")) {
				ret["Method"] = "IncrementVariable";
				// Remove keyword
				string _t = line.Substring(0, line.Length - (FIMGlobal.Keywords.Variable_Increment.End.Count()+1));
				if(FriendshipReport.GlobalVariables.ContainsKey(_t)) {
					if(FriendshipReport.GlobalVariables[_t].Type != VariableTypes.Number) {
						FIMOutput.Error("TRIED TO INCREMENT A NON-INTEGER (GLOBAL) VARIABLE!\n\nLINE > " + line);
						goto Error;
					}
					ret.Add("isGlobal",true);
				} else if(vars.ContainsKey(_t)) {
					if(vars[_t].Type != VariableTypes.Number) {
						FIMOutput.Error("TRIED TO INCREMENT A NON-INTEGER VARIABLE!\n\nLINE > " + line);
						goto Error;
					}
					ret.Add("isGlobal",false);
				} else {
					FIMOutput.Error("INVALID VARIABLE TO INCREMENT!\n\nLINE > " + line);
					goto Error;
				}
				ret.Add("variable",_t);
			}
			else if(line.EndsWith(" " + FIMGlobal.Keywords.Variable_Increment.End)) {
				ret["Method"] = "IncrementVariable";
				string _t = line.Substring(0, line.Length - (FIMGlobal.Keywords.Variable_Increment.End.Count()+1));
				if(FriendshipReport.GlobalVariables.ContainsKey(_t)) {
					if(FriendshipReport.GlobalVariables[_t].Type != VariableTypes.Number) {
						FIMOutput.Error("TRIED TO INCREMENT A NON-INTEGER (GLOBAL) VARIABLE!\n\nLINE > " + line);
						goto Error;
					}
					ret.Add("isGlobal",true);
				} else if(vars.ContainsKey(_t)) {
					if(vars[_t].Type != VariableTypes.Number) {
						FIMOutput.Error("TRIED TO INCREMENT A NON-INTEGER VARIABLE!\n\nLINE > " + line);
						goto Error;
					}
					ret.Add("isGlobal",false);
				} else {
					FIMOutput.Error("INVALID VARIABLE TO INCREMENT!\n\nLINE > " + line);
					goto Error;
				}
				ret.Add("variable",_t);
			}
			#endregion
			#region Decrement
			else if(line.StartsWith(FIMGlobal.Keywords.Variable_Decrement.Start + " ")) {
				ret["Method"] = "DecrementVariable";
				// Remove keyword
				string _t = line.Remove(0, FIMGlobal.Keywords.Variable_Decrement.Start.Count()+1);
				if(FriendshipReport.GlobalVariables.ContainsKey(_t)) {
					if(FriendshipReport.GlobalVariables[_t].Type != VariableTypes.Number) {
						FIMOutput.Error("TRIED TO DECREMENT A NON-INTEGER (GLOBAL) VARIABLE!\n\nLINE > " + line);
						goto Error;
					}
					ret.Add("isGlobal",true);
				} else if(vars.ContainsKey(_t)) {
					if(vars[_t].Type != VariableTypes.Number) {
						FIMOutput.Error("TRIED TO DECREMENT A NON-INTEGER VARIABLE!\n\nLINE > " + line);
						goto Error;
					}
					ret.Add("isGlobal",false);
				} else {
					FIMOutput.Error("INVALID VARIABLE TO DECREMENT!\n\nLINE > " + line);
					goto Error;
				}
				ret.Add("variable",_t);
			}
			else if(line.EndsWith(" " + FIMGlobal.Keywords.Variable_Decrement.End)) {
				ret["Method"] = "DecrementVariable";
				string _t = line.Substring(0, line.Length - (FIMGlobal.Keywords.Variable_Decrement.End.Count()+1));
				if(FriendshipReport.GlobalVariables.ContainsKey(_t)) {
					if(FriendshipReport.GlobalVariables[_t].Type != VariableTypes.Number) {
						FIMOutput.Error("TRIED TO DECREMENT A NON-INTEGER (GLOBAL) VARIABLE!\n\nLINE > " + line);
						goto Error;
					}
					ret.Add("isGlobal",true);
				} else if(vars.ContainsKey(_t)) {
					if(vars[_t].Type != VariableTypes.Number) {
						FIMOutput.Error("TRIED TO DECREMENT A NON-INTEGER VARIABLE!\n\nLINE > " + line);
						goto Error;
					}
					ret.Add("isGlobal",false);
				} else {
					FIMOutput.Error("INVALID VARIABLE TO DECREMENT!\n\nLINE > " + line);
					goto Error;
				}
				ret.Add("variable",_t);
			}
			#endregion
			// Much useful if I just reuse the same method but with a "parameter"
			#region Increment-Prefix
			if(line.StartsWith(FIMGlobal.Keywords.IncrementPrefix + " ")) {
				ret["Method"] = "IncrementVariable";
				string[] l = line.Split(' ').Skip(1).ToArray();
				if(l.Where(x => x=="to").Count() > 1) {
					// ERROR
					FIMOutput.Error("KEYWORD 'TO' IS USED MORE THAN ONCE\n\nLINE > " + line);
					goto Error;
				} 
				if(l.Where(x => x=="to").Count() == 0) {
					// ERROR
					FIMOutput.Error("KEYWORD 'TO' IS MISSING\n\nLINE > " + line);
					goto Error;
				}
				string[] keys = {"",""};
				bool _sw=false;
				List<string> _bf = new List<string>();
				for(int i = 0; i < l.Count(); i++) {
					if(_sw) {
						_bf.Add(l[i]);
					} else {
						if(l[i]=="to") {
							_sw=true;
							keys[0]=_bf.BetterToString(" ");
							_bf.Clear();
						} else
							_bf.Add(l[i]);
					}
				}
				keys[1]=_bf.BetterToString(" ");
				_bf.Clear();
				keys[0] = keys[0].TrimStartEnd(' ');
				keys[1] = keys[1].TrimStartEnd(' ');
				// Keys[0] can be a local,global or a just-declared variable (and must be an integer)
				int amount = 0;
				if(FriendshipReport.GlobalVariables.ContainsKey(keys[0])) {
					if(FriendshipReport.GlobalVariables[keys[0]].Type == VariableTypes.Number) {
						amount = (int)FriendshipReport.GlobalVariables[keys[0]].Value;
					} else {
						// ERROR
						FIMOutput.Error("TRIED TO INCREMENT A NON-INTEGER TO AN INTEGER\n\nLINE > " +line);
						goto Error;
					}
				} else if(vars.ContainsKey(keys[0])) {
					if(vars[keys[0]].Type == VariableTypes.Number) {
						amount = (int)vars[keys[0]].Value;
					} else {
						// ERROR
						FIMOutput.Error("TRIED TO INCREMENT A NON-INTEGER TO AN INTEGER\n\nLINE > " +line);
						goto Error;
					}
				} else {
					object _nil;
					string k1T = FIMMethods.GetVariableAssignType(keys[0], out _nil);
					
					if(k1T == VariableTypes.Number) {
						keys[0] = keys[0].Split(' ').Skip(FIMMethods.GetVariableAssignType2(keys[0])).ToArray().BetterToString(" ");
						amount = (int)keys[0].GetVariableValue(out gotError);
						if(gotError) {
							// ERROR
							FIMOutput.Error("ERROR WHILE GETTING VALUE [I-1]\n\nLINE > " +line);
							goto Error;
						}
					} else {
						_nil = keys[0].GetVariableValue(out gotError);
						if(gotError) {
							// ERROR
							FIMOutput.Error("ERROR WHILE GETTING VALUE [I-2]\n\nLINE > " +line);
							goto Error;
						}
						if(_nil.GetType() == typeof(int)) {
							amount = (int)_nil;
						} else {
							// ERROR
							FIMOutput.Error("ERROR WHILE GETTING VALUE [I-3]\n\nLINE > " +line);
							goto Error;
						}
					}
				}
				// Keys[1] must be a local,global though
				if(FriendshipReport.GlobalVariables.ContainsKey(keys[1])) {
					// ok
					ret["isGlobal"] = true;
				} else if(vars.ContainsKey(keys[1])) {
					// ok
					ret["isGlobal"] = false;
				} else {
					// ERROR
					FIMOutput.Error("INVALID VARIABLE TO BE INCREMENTED\nLINE > " + line);
					goto Error;
				}
				ret["variable"]=keys[1];
				ret["amount"]=amount;
			}
			#endregion
			#region Decrement-Prefix
			if(line.StartsWith(FIMGlobal.Keywords.DecrementPrefix + " ")) {
				ret["Method"] = "DecrementVariable";
				string[] l = line.Split(' ').Skip(1).ToArray();
				if(l.Where(x => x=="to").Count() > 1) {
					// ERROR
					FIMOutput.Error("KEYWORD 'TO' IS USED MORE THAN ONCE\n\nLINE > " + line);
					goto Error;
				} 
				if(l.Where(x => x=="to").Count() == 0) {
					// ERROR
					FIMOutput.Error("KEYWORD 'TO' IS MISSING\n\nLINE > " + line);
					goto Error;
				}
				string[] keys = {"",""};
				bool _sw=false;
				List<string> _bf = new List<string>();
				for(int i = 0; i < l.Count(); i++) {
					if(_sw) {
						_bf.Add(l[i]);
					} else {
						if(l[i]=="to") {
							_sw=true;
							keys[0]=_bf.BetterToString(" ");
							_bf.Clear();
						} else
							_bf.Add(l[i]);
					}
				}
				keys[1]=_bf.BetterToString(" ");
				_bf.Clear();
				keys[0] = keys[0].TrimStartEnd(' ');
				keys[1] = keys[1].TrimStartEnd(' ');
				// Keys[0] can be a local,global or a just-declared variable (and must be an integer)
				int amount = 0;
				if(FriendshipReport.GlobalVariables.ContainsKey(keys[0])) {
					if(FriendshipReport.GlobalVariables[keys[0]].Type == VariableTypes.Number) {
						amount = (int)FriendshipReport.GlobalVariables[keys[0]].Value;
					} else {
						// ERROR
						FIMOutput.Error("TRIED TO DECREMENT A NON-INTEGER TO AN INTEGER\n\nLINE > " +line);
						goto Error;
					}
				} else if(vars.ContainsKey(keys[0])) {
					if(vars[keys[0]].Type == VariableTypes.Number) {
						amount = (int)vars[keys[0]].Value;
					} else {
						// ERROR
						FIMOutput.Error("TRIED TO DECREMENT A NON-INTEGER TO AN INTEGER\n\nLINE > " +line);
						goto Error;
					}
				} else {
					object _nil;
					string k1T = FIMMethods.GetVariableAssignType(keys[0], out _nil);
					
					if(k1T == VariableTypes.Number) {
						keys[0] = keys[0].Split(' ').Skip(FIMMethods.GetVariableAssignType2(keys[0])).ToArray().BetterToString(" ");
						amount = (int)keys[0].GetVariableValue(out gotError);
						if(gotError) {
							// ERROR
							FIMOutput.Error("ERROR WHILE GETTING VALUE [D-1]\n\nLINE > " +line);
							goto Error;
						}
					} else {
						_nil = keys[0].GetVariableValue(out gotError);
						if(gotError) {
							// ERROR
							FIMOutput.Error("ERROR WHILE GETTING VALUE [D-2]\n\nLINE > " +line);
							goto Error;
						}
						if(_nil.GetType() == typeof(int)) {
							amount = (int)_nil;
						} else {
							// ERROR
							FIMOutput.Error("ERROR WHILE GETTING VALUE [D-3]\n\nLINE > " +line);
							goto Error;
						}
					}
				}
				// Keys[1] must be a local,global though
				if(FriendshipReport.GlobalVariables.ContainsKey(keys[1])) {
					// ok
					ret["isGlobal"] = true;
				} else if(vars.ContainsKey(keys[1])) {
					// ok
					ret["isGlobal"] = false;
				} else {
					// ERROR
					FIMOutput.Error("INVALID VARIABLE TO BE DEREMENTED\nLINE > " + line);
					goto Error;
				}
				ret["variable"]=keys[1];
				ret["amount"]=amount;
			}
			#endregion
			// WRITE
			else if(FIMGlobal.Keywords.Method_Write.Any(x => line.StartsWith(x + " "))) {
				// RUN METHOD		
				if(line.StartsWith("I remembered")) {
					ret["Method"] = "RunMethod";
					string methodName = line.Substring("I remembered".Length+1);
					string[] methodNameSplit = methodName.Split(' ');
					bool hasParam = false;
					object[] prm = {"","",""};
					// CHECK PARAMETER
					if(methodName.ContainsKeyword(FIMGlobal.Keywords.Method_Parameter)) {
						string extra = methodNameSplit.Skip(methodNameSplit.FirstIndexOf(FIMGlobal.Keywords.Method_Parameter)+1).ToArray().BetterToString(" ");
						object param_Value;
						if(FIMMethods.VariableDeclerationAssignsVariable(extra)) {
							// HAS TYPE ( for variables that are made only to be used once )
							string param_Type = FIMMethods.GetVariableAssignType(extra, out param_Value, true);
							if(param_Type == "ERROR") {
								FIMOutput.Error("INVALID PARAMETER\n\nLINE > " + line);
								goto Error;
							}
							prm[1] = param_Value;
							prm[2] = param_Type;
						} else {
							// PURE VALUE
							if(FriendshipReport.GlobalVariables.ContainsKey(extra)) {
								prm[1] = FriendshipReport.GlobalVariables[extra].Value;
								prm[2] = FriendshipReport.GlobalVariables[extra].Type;
							} else if(vars.ContainsKey(extra)) {
								prm[1] = vars[extra].Value;
								prm[2] = vars[extra].Type;
							} else {
								FIMOutput.Error("INVALID PARAMETER\n\nLINE > " + line);
								goto Error;
							}
						}
						methodName="";
						foreach(string s in methodNameSplit) {
							if(s != FIMGlobal.Keywords.Method_Parameter)
								methodName += s + " ";
							else break;
						}
						methodName=methodName.TrimEnd(' ');
						hasParam = true;
					}
					//
					if(FriendshipReport.LocalMethods.ContainsKey(methodName)) {
						MethodContent l = FriendshipReport.LocalMethods[methodName];
						if(hasParam) {
							if(l.ParamType == "") {
								FIMOutput.Error("TRIED USING A PARAMETER ON A METHOD THAT DOESN'T USE ONE\n\nLINE > " + line);
								goto Error;
							}
							prm[0]=l.ParamName;
						}
						//
						if(l.ReturnType != "") {
							FIMOutput.Error("TRIED RUNNING A METHOD THAT RETURNS A VALUE ON AN INCORRECT WAY\n" +
							                "PLEASE RUN THE METHOD ON A WRITE/VARIABLE DECLERATION FUNCTION" +
							                "\nLINE > " + line);
							goto Error;
						}
						if(!hasParam)
							prm = null;
						RunMethod(l.MethodLines, out _emptyObject, false, methodName, prm);
					}
					else {
						if(FriendshipReport.MainMethodName == methodName) {
							FIMOutput.Error("A METHOD TRIED RUNNING THE MAIN METHOD\n\nLINE > " + line);
						} else {
							FIMOutput.Error("TRIED RUNNING AN NON-EXISTENT METHOD\n\nLINE > " + line);
						}
						goto Error;
					}
				}
				else {
					// WRITE
					ret["Method"] = "Write";
					string _temp = line.Split(' ').Skip(2).ToArray().BetterToString(" ");
					// USING FUNCTION?
					if(FriendshipReport.LocalMethods.ContainsKey(_temp)) {
						MethodContent f = FriendshipReport.LocalMethods.Where(x => _temp.Equals(x.Key)).FirstOrDefault().Value;
						if(f.ReturnType == "") {
							FIMOutput.Error("TRIED TO PRINT A RETURN VARIABLE OF A METHOD THAT DOESN'T HAVE ONE\n\nLINE > " + line);
							goto Error;
						}
						//
						string methodName = line.Substring("I remembered".Length+1);
						string[] methodNameSplit = methodName.Split(' ');
						bool hasParam = false;
						object[] prm = {"","",""};
						// CHECK PARAMETER
						if(methodName.ContainsKeyword(FIMGlobal.Keywords.Method_Parameter)) {
							string extra = methodNameSplit.Skip(methodNameSplit.FirstIndexOf(FIMGlobal.Keywords.Method_Parameter)+1).ToArray().BetterToString(" ");
							object param_Value;
							string param_Type = FIMMethods.GetVariableAssignType(extra, out param_Value, true);
							if(param_Type == "ERROR") {
								FIMOutput.Error("INVALID PARAMETER\n\nLINE > " + line);
								goto Error;
							}
							prm[1] = param_Value;
							prm[2] = param_Type;
							methodName = methodNameSplit.Where(x => methodNameSplit.FirstIndexOf(x) < methodNameSplit.FirstIndexOf(FIMGlobal.Keywords.Method_Parameter)).ToArray().BetterToString(" ");
							hasParam = true;
						}
						object __var = RunFunction.GetReturnVariable(_temp, MergeVariableDictionary(vars), out gotError);
						if(__var != null) {
							FIMOutput.Output(__var.ToString());
						}
					} else {
						// JUST VARIABLE
						RunFunction.Write(_temp, MergeVariableDictionary(vars), out gotError);
						if(gotError) {
							goto Error;
						}
					}
				}
			}
			// MAKE VARIABLE
			else if(line.StartsWith(FIMGlobal.Keywords.Method_Declare + " ") && FIMGlobal.Keywords.Variable_Initialization.Any(x => line.Contains(" " + x + " "))) {
				ret["Method"] = "AddVariable";
				string dec = FIMGlobal.Keywords.Variable_Initialization.Where(x => line.Contains(" " + x + " ")).FirstOrDefault();
				string[] a_Spl = line.Split(' ').Skip(4).ToArray();
				int dec_Index = a_Spl.FirstIndexOf(dec);
				List<string> var_TempName = new List<string>();
				for(int i = 0; i < dec_Index; i++) {
					var_TempName.Add(a_Spl[i]);
				}
				string var_Name = var_TempName.BetterToString(" ");
				if(vars.ContainsKey(var_Name)) {
					FIMOutput.Error("A LOCAL VARIABLE WITH THE NAME ("+var_Name+") ALREADY EXISTS\n\nLINE > " + line);
					goto Error;
				}
				if(FriendshipReport.GlobalVariables.ContainsKey(var_Name)) {
					FIMOutput.Error("A GLOBAL VARIABLE WITH THE NAME ("+var_Name+") ALREADY EXISTS\n\nLINE > " + line);
					goto Error;
				}
				string after = a_Spl.Skip(var_Name.Split(' ').Count()+1).ToArray().BetterToString(" ");
				object var_Value;
				if(FriendshipReport.LocalMethods.ContainsKey(after)) {
					MethodContent f = FriendshipReport.LocalMethods.Where(x => after.Equals(x.Key)).FirstOrDefault().Value;
					if(f.ReturnType == "") {
						FIMOutput.Error("TRIED TO PRINT A RETURN VARIABLE OF A METHOD THAT DOESN'T HAVE ONE\n\nLINE > " + line);
						goto Error;
					}
					bool hasParam = false;
					object[] prm = {"","",""};
					string methodName=after;
					string[] methodNameSplit = methodName.Split(' ');
					// CHECK PARAMETER
					if(methodName.ContainsKeyword(FIMGlobal.Keywords.Method_Parameter)) {
						string extra = methodNameSplit.Skip(methodNameSplit.FirstIndexOf(FIMGlobal.Keywords.Method_Parameter)+1).ToArray().BetterToString(" ");
						object param_Value;
						string param_Type = FIMMethods.GetVariableAssignType(extra, out param_Value, true);
						if(param_Type == "ERROR") {
							FIMOutput.Error("INVALID PARAMETER\n\nLINE > " + line);
							goto Error;
						}
						prm[1] = param_Value;
						prm[2] = param_Type;
						methodName = methodNameSplit.Where(x => methodNameSplit.FirstIndexOf(x) < methodNameSplit.FirstIndexOf(FIMGlobal.Keywords.Method_Parameter)).ToArray().BetterToString(" ");
						hasParam = true;
					}
					var_Value = RunFunction.GetReturnVariable(after, MergeVariableDictionary(vars), out gotError);
				} else {
					string var_Type = FIMMethods.GetVariableAssignType(after, out var_Value);
					string temp_Value = after.Split(' ').Skip(FIMMethods.GetVariableAssignType2(after)).ToArray().BetterToString(" ");
					if(temp_Value.GetValueType() == "ERROR") {
						FIMOutput.Error("INVALID VARIABLE ASSIGNMENT\n\nLINE > " + line);
						goto Error;
					}
					if(FIMMethods.VariableDeclerationAssignsVariable(after)) {
						if(FriendshipReport.GlobalVariables.ContainsKey(temp_Value)) {
							ReportVariable v = FriendshipReport.GlobalVariables.Where(x => temp_Value.Equals(x.Key)).FirstOrDefault().Value;
							if(v.Type != var_Type) {
								FIMOutput.Error("INVALID VARIABLE ASSIGNMENT TYPE\n\nLINE > " + line);
								goto Error;
							}
							var_Value = v.Value;
						}
						else if (vars.ContainsKey(temp_Value)) {
							ReportVariable v = vars.Where(x => temp_Value.Equals(x.Key)).FirstOrDefault().Value;
							if(v.Type != var_Type) {
								FIMOutput.Error("INVALID VARIABLE ASSIGNMENT TYPE\n\nLINE > " + line);
								goto Error;
							}
							var_Value = v.Value;
						}
						else {
							if(temp_Value.GetValueType() == "STRING") {
								bool _replaceError;
								string a = FIMMethods.StringReplaceVariable(temp_Value, ZipVariableDictionary(MergeVariableDictionary(vars)), out _replaceError);
								if(_replaceError) {
									FIMOutput.Error(a+"\n\nLINE > ");
									goto Error;
								}
								var_Value = a;
							} else {
								var_Value = temp_Value.GetVariableValue(out gotError);
								if(gotError) {
									// ERROR
									goto Error;
								}
							}
						}
					} else {
						var_Value = FIMGlobal.Keywords.Variable_Empty;
					}
				}
				
				//localVariable.Add(var_Name, new ReportVariable() {Value = var_Value, Type = var_Value.GetValueType()});
				ret.Add("newVariable", new object[] {var_Name, var_Value, var_Value.GetValueType()});
			}
			// REPLACE VARIABLE
			else if(FIMGlobal.Keywords.Method_Replace.Any(x => line.Contains(" " + x + " "))) {
				ret["Method"] = "ReplaceVariable";
				string[] keyword = {FIMGlobal.Keywords.Method_Replace.Where(x => line.Contains(" " + x + " ")).FirstOrDefault()};
				string[] keys = line.Split(keyword, StringSplitOptions.None);
				keys[0] = keys[0].TrimEnd(' ');
				keys[1] = keys[1].TrimStart(' ');
				object newVal;
				// GET VARIABLE
				ReportVariable _t1;
				string _t1Key;
				bool isGlobal = true;
				if(FriendshipReport.GlobalVariables.ContainsKey(keys[0])) {
					_t1Key = FriendshipReport.GlobalVariables.Keys.Where(x => keys[0].Equals(x)).FirstOrDefault();
					_t1 = FriendshipReport.GlobalVariables[_t1Key];
				}
				else if(vars.ContainsKey(keys[0])) {
					_t1Key = vars.Keys.Where(x => keys[0].Equals(x)).FirstOrDefault();
					_t1 = vars[_t1Key];
					isGlobal = false;
				}
				else {
					FIMOutput.Error("FIRST VARIABLE WAS NOT FOUND!\n\nLINE > " + line);
					goto Error;
				}
				// CHECK IF VAL[1] IS A VARIABLE, IF SO THEN CHANGE ITS VALUE + VARIABLE TYPE CHECK + STRING MANIPULATION
				object _nil;
				if(FriendshipReport.GlobalVariables.ContainsKey(keys[1])) {
					ReportVariable _t = FriendshipReport.GlobalVariables.Where(x => keys[1].Equals(x.Key)).FirstOrDefault().Value;
					if(_t1.Type != _t.Type) {
						FIMOutput.Error("SECOND VARIABLE AS DIFFERENT TYPES AS THE FIRST ONE\n\nLINE > " + line);
						goto Error;
					}
					newVal = _t.Value;
				}
				else if(vars.ContainsKey(keys[1])) {
					ReportVariable _t = vars.Where(x => keys[1].Equals(x.Key)).FirstOrDefault().Value;
					if(_t1.Type != _t.Type) {
						FIMOutput.Error("SECOND VARIABLE AS DIFFERENT TYPES AS THE FIRST ONE\n\nLINE > " + line);
						goto Error;
					}
					newVal = _t.Value;
				}
				else {
					string k1T = FIMMethods.GetVariableAssignType(keys[1], out _nil);
					if(k1T == "STRING") {
						k1T = FIMMethods.GetVariableAssignType(keys[1], out newVal, true);
						bool _replaceError;
						string a = FIMMethods.StringReplaceVariable((string)newVal, ZipVariableDictionary(MergeVariableDictionary(vars)), out _replaceError);
						if(_replaceError) {
							FIMOutput.Error(a+"\n\nLINE > ");
							goto Error;
						}
						newVal = a;
					} else if(k1T == "ERROR") {
						newVal = keys[1].GetVariableValue(out gotError);
						if(gotError) {
							// ERROR
							goto Error;
						}
					} else {
						k1T = FIMMethods.GetVariableAssignType(keys[1], out newVal, true);
					}
				}
				ret.Add("isGlobal", isGlobal);
				ret.Add("variable", new object[] {_t1Key, newVal});
			}
			// RETURN
			else if(line.StartsWith(FIMGlobal.Keywords.Method_Return2 + " ")) {
				// Figure this out later
				string l = line.Substring((FIMGlobal.Keywords.Method_Return2 + " ").Count());
				object newVal = null;
				string newType = "";
				// Get local/global variable
				if(FriendshipReport.GlobalVariables.ContainsKey(l)) {
					newVal = FriendshipReport.GlobalVariables[l].Value;
				} else if(vars.ContainsKey(l)) {
					newVal = vars[l].Value;
				} else {
					object _nil;
					string k1T = FIMMethods.GetVariableAssignType(l, out _nil);
					if(k1T == "STRING") {
						k1T = FIMMethods.GetVariableAssignType(l, out newVal, true);
						bool _replaceError;
						string a = FIMMethods.StringReplaceVariable((string)newVal, ZipVariableDictionary(MergeVariableDictionary(vars)), out _replaceError);
						if(_replaceError) {
							FIMOutput.Error(a+"\n\nLINE > ");
							goto Error;
						}
						newVal = a;
						newType = k1T;
					} else if(k1T == "ERROR") {
						newVal = l.GetVariableValue(out gotError);
						if(gotError) {
							// ERROR
							goto Error;
						}
					} else {
						k1T = FIMMethods.GetVariableAssignType(l, out newVal, true);
					}
				}
				newType = newVal.GetValueType();
				// Check if method returns the correct variable type
				if(FriendshipReport.LocalMethods[MethodName].ReturnType != newType) {
					FIMOutput.Error("TRIED TO RETURN AN INVALID VARIABLE (INCORRECT TYPE)\n\nLINE > " + line);
					goto Error;
				}
				ret["Variable"] = newVal;
				ret["Method"] = "Return";
			}
			
			return ret;
		}

		public static Dictionary<string, ReportVariable> MergeVariableDictionary(Dictionary<string, ReportVariable> local) {
			Dictionary<string, ReportVariable> complete = new Dictionary<string, ReportVariable>();
			foreach(string k in FriendshipReport.GlobalVariables.Keys)
				complete.Add(k, FriendshipReport.GlobalVariables[k]);
			foreach(string k in local.Keys) {
				complete.Add(k, local[k]);
			}
			return complete;
		}
		
		/// <summary>
		/// Zips up the dictionary for cross-class usage.
		/// Still not sure on how to fix the Inconsistent Accessibility error :/
		/// </summary>
		private static Dictionary<string, object> ZipVariableDictionary(Dictionary<string, ReportVariable> vars) {
			Dictionary<string, object> complete = new Dictionary<string, object>();
			foreach(string k in vars.Keys) {
				complete.Add(k, vars[k].Value);
			}
			return complete;
		}
		
		
		public class RunFunction {
			public static void Write(string line, Dictionary<string, ReportVariable> vars, out bool isError) {
				isError = false;
				if(line.StartEndsWith("\"")) {
					bool _replaceError;
					string a = FIMMethods.StringReplaceVariable(line, ZipVariableDictionary(vars), out _replaceError);
					if(_replaceError) {
						FIMOutput.Error(a+"\n\nLINE > " + line);
						isError = true;
						return;
					}
					FIMOutput.Output(a);
				}
				else {
					if(vars.ContainsKey(line)) {
						FIMOutput.Output(vars[line].Value.ToString());
					} else {
						if(line.GetValueType() != "ERROR")
							FIMOutput.Output(line);
						else
							FIMOutput.Error("THE VARIABLE ("+line+") DOESN'T EXIST\n\nLINE > " + line);
					}
				}
			}
			
			public static object GetReturnVariable(string line, Dictionary<string, ReportVariable> vars, out bool isError, object[] Param = null) {
				isError = false;
				if(FriendshipReport.LocalMethods.ContainsKey(line)) {
					// Run method with returnVar
					object _var = null;
					MethodContent m = FriendshipReport.LocalMethods[line];
					if(Param != null) {
						object[] p = {m.ParamName,Param[0],Param[1]};
						p[0].WriteLine();
						p[1].WriteLine();
						p[2].WriteLine();
						RunMethod(m.MethodLines, out _var, false, line, p, vars);
					} else {
						
						RunMethod(m.MethodLines, out _var, false, line, null, vars);
					}
					return _var;
				} else {
					// Method doesn't exist (don't do anything)
				}
				return null;
			}
			
			public static object[] IfElse(string[] lines, Dictionary<string, ReportVariable> vars, out bool isError) {
				isError = false;
				// Case 1: If
				if(!lines.Any(x => FIMGlobal.Keywords.If_Else.Any(y => x.StartsWith(y)))) {
					// Remove keyword
					string cond = lines[0];
					string keyword = FIMGlobal.Keywords.If_Start.Where(x => cond.StartsWith(x)).FirstOrDefault() + " ";
					cond = cond.Remove(0,keyword.Count());
					// Remove optional "then"
					if(cond.EndsWith("then"))
						cond = cond.Substring(0,cond.Length-5);
					// GET SIGN
					string _signWord;
					string sign = FIMMethods.GetCompareSign(cond, out _signWord);
					// SPLIT
					string[] keys = cond.Split(new [] {_signWord}, StringSplitOptions.None);
					keys[0] = keys[0].TrimEnd(' ');
					keys[1] = keys[1].TrimStart(' ');
					object[] val = {null,null};
					for(int i = 0; i < 2; i++) {
						if(vars.ContainsKey(keys[i]))
							val[i] = vars[keys[i]].Value;
						else if (FriendshipReport.GlobalVariables.ContainsKey(keys[i]))
							val[i] = FriendshipReport.GlobalVariables[keys[i]].Value;
						else {
							if(FIMMethods.VariableDeclerationAssignsVariable(keys[i])) {
								string _t = FIMMethods.GetVariableAssignType(keys[i], out val[i], true);
							} else {
								object _v = keys[i].GetVariableValue(out isError);
								if(_v == null) {
									isError = true;
									FIMOutput.Error("ERROR GETTING VALUE {0}!\n\nLINE > ".MelodyFormat(i+1) + lines[0]);
									return new object[] {};
								}
								val[i] = _v;
							}
						}
					}
					// BLESS THIS SHIT OMFG
					bool condTrue = LuaComparison("return {0} {1} {2}".MelodyFormat(val[0],sign,val[1]));
					if(condTrue == true) {
						// Do inner lines
						lines = lines.Skip(1).Take(lines.Length-2).ToArray();
						/*for(int i = 0; i < lines.Count(); i++) {
							lines[i] = lines[i].Remove(0,1); // Remove "space"
						}*/
						return RunIfElse(lines, vars, out isError);
						// TODO: Make RunMethod efficient for method and ifelse running
					}
				}
				// Case 2: If-Else
				else if(lines.Where(x => FIMGlobal.Keywords.If_Else.Any(y => x.StartsWith(y))).Count() == 1) {			
					#region SETUP
					// oops joot camp
					if(lines.Where(x => FIMGlobal.Keywords.If_Else.Any(y => x.Equals(y))).Count() == 0) {
						// ERROR
						isError = true;
						FIMOutput.Error("INVALID ELSE SENTENCE\n\nIF-ELSE HEADER > " + lines[0]);
						return new object[] {};
					}
					string splitKeyword = lines.Where(x => FIMGlobal.Keywords.If_Else.Any(y => x.Equals(y))).FirstOrDefault();
					int splitIndex = lines.FirstIndexOf(splitKeyword); // You only see this once tho lol
					// Remove keyword
					string cond = lines[0];
					string keyword = FIMGlobal.Keywords.If_Start.Where(x => cond.StartsWith(x)).FirstOrDefault() + " ";
					cond = cond.Remove(0,keyword.Count());
					// Remove optional "then"
					if(cond.EndsWith("then"))
						cond = cond.Substring(0,cond.Length-5);
					// GET SIGN
					string _signWord;
					string sign = FIMMethods.GetCompareSign(cond, out _signWord);
					// SPLIT
					string[] keys = cond.Split(new [] {_signWord}, StringSplitOptions.None);
					keys[0] = keys[0].TrimEnd(' ');
					keys[1] = keys[1].TrimStart(' ');
					object[] val = {null,null};
					for(int i = 0; i < 2; i++) {
						if(vars.ContainsKey(keys[i]))
							val[i] = vars[keys[i]].Value;
						else if (FriendshipReport.GlobalVariables.ContainsKey(keys[i]))
							val[i] = FriendshipReport.GlobalVariables[keys[i]].Value;
						else {
							if(FIMMethods.VariableDeclerationAssignsVariable(keys[i])) {
								string _t = FIMMethods.GetVariableAssignType(keys[i], out val[i], true);
							} else {
								object _v = keys[i].GetVariableValue(out isError);
								if(_v == null) {
									isError = true;
									FIMOutput.Error("ERROR GETTING VALUE {0}!\n\nLINE > ".MelodyFormat(i+1) + lines[0]);
									return new object[] {};
								}
								val[i] = _v;
							}
						}
					}
					#endregion
					// enter hell
					#region RunIF
					bool condTrue = LuaComparison("return {0} {1} {2}".MelodyFormat(val[0],sign,val[1]));
					if(condTrue) {
						lines = lines.Select((vl,id) => new {id,vl}).Where(x => 1 <= x.id && x.id < splitIndex).Select(x => x.vl).ToArray();
						/*for(int i = 0; i < lines.Count(); i++) {
							lines[i] = lines[i].Remove(0,1); // Remove "space"
						}*/
						return RunIfElse(lines, vars, out isError);
					}
					#endregion
					#region RunELSE
					lines = lines.Select((vl,id) => new {id,vl}).Where(x => splitIndex+1 <= x.id && x.id < lines.Length-1).Select(x => x.vl).ToArray();
					/*for(int i = 0; i < lines.Count(); i++) {
						lines[i] = lines[i].Remove(0,1); // Remove "space"
					}*/
					return RunIfElse(lines, vars, out isError);
					#endregion
					// exit hell
				}
				// Case 3: If-ElseIf(n)-Else
				else if(lines.Where(x => FIMGlobal.Keywords.If_Else.Any(y => x.StartsWith(y))).Count() >= 1 && lines.Where(x => FIMGlobal.Keywords.If_Else.Any(y => x.StartsWith(y))).Count() == 1) {
					
				}
				// Casfe 4: If-ElseIf(n)
				else if(lines.Where(x => FIMGlobal.Keywords.If_Else.Any(y => x.StartsWith(y))).Count() == 0) {
					
				}
				isError = true;
				return new object[] {"ERROR"};
			}
			
			// The return of If-Else, fortunately we only have to worry about one thing. Keeping track of the variable
			public static object[] While(string[] lines, Dictionary<string, ReportVariable> vars, out bool isError, out bool isDone) {
				isError = false;
				isDone = false;
				//
				// Remove keyword
				string cond = lines[0];
				string keyword = FIMGlobal.Keywords.Loop_While.Where(x => cond.StartsWith(x)).FirstOrDefault() + " ";
				cond = cond.Remove(0,keyword.Count());
				// GET SIGN
				string _signWord;
				string sign = FIMMethods.GetCompareSign(cond, out _signWord);
				// SPLIT
				string[] keys = cond.Split(new [] {_signWord}, StringSplitOptions.None);
				keys[0] = keys[0].TrimEnd(' ');
				keys[1] = keys[1].TrimStart(' ');
				object[] val = {null,null};
				for(int i = 0; i < 2; i++) {
					if(vars.ContainsKey(keys[i]))
						val[i] = vars[keys[i]].Value;
					else if (FriendshipReport.GlobalVariables.ContainsKey(keys[i]))
						val[i] = FriendshipReport.GlobalVariables[keys[i]].Value;
					else {
						if(FIMMethods.VariableDeclerationAssignsVariable(keys[i])) {
							string _t = FIMMethods.GetVariableAssignType(keys[i], out val[i], true);
						}else{
							bool _e = false;
							object _v = keys[i].GetVariableValue(out _e);
							if (_e) {
								isError = true;
								FIMOutput.Error("ERROR GETTING VALUE {0}!\n\nLINE > ".MelodyFormat(i + 1) + lines[0]);
								return new object[] { };
							}
							val[i] = _v;
						}
						
					}
				}
				// BLESS THIS SHIT OMFG
				bool condTrue = LuaComparison("return {0} {1} {2}".MelodyFormat(val[0],sign,val[1]));
				if(condTrue == true) {
					// Do inner lines
					lines = lines.Skip(1).Take(lines.Length-2).ToArray();
					/*for(int i = 0; i < lines.Count(); i++) {
						lines[i] = lines[i].Remove(0,1); // Remove "space"
					}*/
					return RunIfElse(lines, vars, out isError);
				}
				//
				isDone = true;
				isError = false;
				return new object[] {null,null};
			}
			
		}
		
		public static string Author = "", ScrollTitle = ""; 
		
		public static Colorful.StyleSheet FancyInfoText = new Colorful.StyleSheet(Color.White);
		
		//
		 
		public static ReportContent FriendshipReport;
		
		
		public class ReportContent {
			public MethodContent MainMethod { get; set; }
			public string MainMethodName { get; set; }
			
			public Dictionary<string, MethodContent> LocalMethods { get; set; }
			public Dictionary<string, ReportVariable> GlobalVariables { get; set; }
			public ReportContent() {
				LocalMethods = new Dictionary<string, MethodContent>();
				GlobalVariables = new Dictionary<string, ReportVariable>();
			}
		}
		public class ReportVariable {
			public string Type { get; set; }
			/*
			 * CURRENT HANDLED TYPES:
			 * 	BOOLEAN
			 *  BOOLEAN-ARRAY
			 *  NUMBER
			 *  NUMBER-ARRAY
			 *  CHARACTER
			 *  STRING
			 *  STRING-ARRAY
			*/
			public object Value { get; set; }
		}
		public class VariableTypes {
			public static string Bool = "BOOLEAN";
			public static string BoolArray = "BOOLEAN-ARRAY";
			public static string Number = "NUMBER";
			public static string NumberArray = "NUMBER-ARRAY";
			public static string Character = "CHARACTER";
			public static string String = "STRING";
			public static string StringArray = "STRING-ARRAY";
		}
		public class MethodContent {
			public string[] MethodLines { get; set; }
			public Dictionary<string, ReportVariable> LocalVariables { get; set; }
			public string ReturnType { get; set; } // ONLY ONE
			public string ParamType { get; set; } // ONLY ONE
			public string ParamName { get; set; }
			public MethodContent(string[] lines, string returnType, string paramType, string paramName) {
				ReturnType = returnType;
				ParamType = paramType;
				ParamName = paramName;
				MethodLines = lines;
				LocalVariables = new Dictionary<string, ReportVariable>();
			}
		}
	}
}