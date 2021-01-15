using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Text.RegularExpressions;
//
using Melody.Helper;
//
using NetFiM;
using NetFiM.Global;

namespace NetFiM.Methods
{
	public static class FIMMethods
	{
		
		#region Punctuations
		/// <summary>
		/// Removes ALL punctuations
		/// </summary>
		public static string RemovePunctuation(this string a) {
			string b = a;
			foreach(string c in FIMGlobal.Punctuations)
				b = b.TrimEnd(c.ToCharArray());
			return b;
		}
		public static string[] RemovePunctuation(this string[] a) {
			List<string> b = new List<string>();
			foreach(string c in a)
				b.Add(c.RemovePunctuation());
			return b.ToArray();
		}
		
		/// <summary>
		/// Checks if all lines has punctuations.
		/// Returns an int if true
		/// </summary>
		/// <param name="line">For error line</param>
		public static bool AllContainsPunctuation(this string[] a, out int line) {
			//return a.All(x => FIMGlobal.Punctuations.Any(y => x.EndsWith(y))); // Rip one line
			for(int i = 0; i < a.Count(); i++) {
				//         F U   C K    U
				if(!string.IsNullOrWhiteSpace(a[i]) && !FIMGlobal.Punctuations.Any(x => a[i].EndsWith(x))) {
					line = i+1;
					return false;
				}
			}
			line = 0;
			return true;
		}
		#endregion
		
		#region Main Method / Method Checking
		/// <summary>
		/// Gets the Main Method, returns {null,ERROR-CODE} on failure
		/// </summary>
		public static string[] GetMainMethodLines(string[] a, out string methodName) {
			List<string> b = new List<string>();
			if(a.Where(x=>x.StartsWith(FIMGlobal.MainMethodStart)).Count() > 1) {
				methodName = "";
				return new [] {null,"GOT MORE THAN TWO MAIN METHODS"};
			}
			//
			string startMethod = a.Where(x=>x.StartsWith(FIMGlobal.MainMethodStart)).FirstOrDefault();
			methodName = startMethod.Substring(FIMGlobal.MainMethodStart.Length);
			int startIndex = a.FirstIndexOf(startMethod)+1;
			for(int i = startIndex; i < a.Count(); i++) {
				if(a[i].StartsWith(FIMGlobal.MethodEnd)) {
					break;
				} else {
					if(i >= a.Count()-1)
						return new [] {null,"INVALID MAIN METHOD END"};
					b.Add(a[i]);
				}
			}
			//
			return b.ToArray();
		}
		
		/// <summary>
		/// Checks if all methods starts and ends properly
		/// </summary>
		/// <param name="errorLine">For error line</param>
		public static bool AreMethodsValid(string[] a, out string errorLine) {
			bool isInMethod = false;
			List<string> currMethods = new List<string>();
			for(int i = 0; i < a.Count(); i++) {
				string l = a[i];
				if(l.StartsWith(FIMGlobal.MainMethodStart) || l.StartsWith(FIMGlobal.MethodStart)) {
					if(isInMethod) {
						errorLine = a[i];
						return false;
					}
					// CHECK HEADING
					if(!ValidMethodHeading(l)) {
						errorLine = a[i];
						return false;
					}
					string mName;
					bool[] _spec;
					l.GetMethodName(out mName, out _spec);
					if(!currMethods.Contains(mName)) {
						currMethods.Add(mName);
					} else {
						errorLine = "TWO OR MORE METHODS HAS THE SAME METHOD NAME";
						return false;
					}
					isInMethod = true;
				} else {
					if(l.StartsWith(FIMGlobal.MethodEnd)) {
						isInMethod = false;
					}
				}
			}
			errorLine = "";
			return true;
		}
		
		private static bool ValidMethodHeading(string a) {
			string b = "";
			bool[] _spec;
			if(a.GetMethodName(out b, out _spec)) {
				/*#region Checking
				// MUST NOT CONTAIN KEYWORDS
				b.WriteLine();
				if(b.ContainsKeyword()) return false;
				// MUST NOT CONTAIN PUNCTUATION
				if(FIMGlobal.Punctuations.Any(x=>b.Contains(x))) return false;
				// MUST START WITH A VALID CHARACTER
				if(!IsValidCharacter(b[0])) return false;
				// MUST END WITH A VALID CHARACTER
				if(!IsValidCharacter(b[b.Length-1])) return false;
				#endregion
				return true;*/
				
				// Why not
				return !b.ContainsKeyword() && !FIMGlobal.Punctuations.Any(x=>b.Contains(x))
					&& IsValidCharacter(b[0]) && IsValidCharacter(b[b.Length-1]);
			}
			return false;
		}
		
		private static bool IsValidCharacter(char a) {
			return char.IsLetter(a) || char.IsNumber(a);
		}
		
		// Handles the special stuffs a method line can have
		public static bool GetMethodName(this string a, out string MethodName, out bool[] containedSpecial) {
			string b = a;
			if(b.StartsWith("Today "))
				b = b.Substring(6);
			if(!b.StartsWith(FIMGlobal.MethodStart)) {
				MethodName = "";
				containedSpecial = new bool[] {false,false};
				return false;
			}
			b = b.Remove(0,FIMGlobal.MethodStart.Count());
			string[] bSpl = b.Split(' ');
			bool[] spec = {false,false};
			#region Get Name
			string _methodName = "";
			// CASE 0: NO PARAGRAPH SPECIAL
			if(!b.ContainsKeyword(FIMGlobal.Keywords.Method_Parameter) && !FIMGlobal.Keywords.Method_Return.Any(x => b.ContainsKeyword(x))) {
				_methodName = b;
			}
			// CASE 1: PARAMETER
			else if(b.ContainsKeyword(FIMGlobal.Keywords.Method_Parameter) && !FIMGlobal.Keywords.Method_Return.Any(x => b.ContainsKeyword(x))) {
				spec[0] = true;
				// Get parameter index
				int _paramIndex = bSpl.FirstIndexOf(FIMGlobal.Keywords.Method_Parameter);
				List<string> _mTemp = new List<string>();
				for(int i = 0; i < bSpl.Count()-1; i++) {
					if(i >= _paramIndex)
						break;
					_mTemp.Add(bSpl[i]);
				}
				_methodName = _mTemp.BetterToString(" ");
			}
			// CASE 2: RETURN
			else if(!b.ContainsKeyword(FIMGlobal.Keywords.Method_Parameter) && FIMGlobal.Keywords.Method_Return.Any(x => b.ContainsKeyword(x))) {
				// GET FIRST KEYWORD SEEN
				spec[1] = true;
				string[] _bTemp = b.Split(' ').DirectAdd(" "); // Leave one space
				int keywordIndex = -1;
				for(int i = 0; i < _bTemp.Count()-2; i++) {
					if(_bTemp[i] == "with") {
						keywordIndex = i;
						break;
					} 
					if ("{0} {1}".MelodyFormat(_bTemp[i],_bTemp[i+1]) == "to get") {
						keywordIndex = i;
						break;
					}
				}
				List<string> _mTemp = new List<string>();
				for(int i = 0; i < bSpl.Count()-1; i++) {
					if(i >= keywordIndex)
						break;
					_mTemp.Add(bSpl[i]);
				}
				_methodName = _mTemp.BetterToString(" ");
			}
			// CASE 3: BOTH
			else {
				spec = new bool[] {true,true};
				// GET WHAT KEYWORD WAS FIRST
				int _paramIndex = bSpl.FirstIndexOf(FIMGlobal.Keywords.Method_Parameter);
				string[] _bTemp = b.Split(' ').DirectAdd(" ");
				int _retrnIndex = -1;
				for(int i = 0; i < _bTemp.Count()-2; i++) {
					if(_bTemp[i] == "with") {
						_retrnIndex = i;
						break;
					} 
					if ("{0} {1}".MelodyFormat(_bTemp[i],_bTemp[i+1]) == "to get") {
						_retrnIndex = i;
						break;
					}
				}
				int _splIndex = _paramIndex < _retrnIndex ? _paramIndex : _retrnIndex;
				List<string> _mTemp = new List<string>();
				for(int i = 0; i < bSpl.Count()-1; i++) {
					if(i >= _splIndex)
						break;
					_mTemp.Add(bSpl[i]);
				}
				_methodName = _mTemp.BetterToString(" ");
			}
		
			#endregion
			containedSpecial = spec;
			MethodName = _methodName;
			return true;
		}
		
		public static Dictionary<string, object[]> GetLocalMethodLines(string[] a, out bool gotError) {
			gotError = false;
			Dictionary<string, object[]> _temp = new Dictionary<string, object[]>();
			List<string> _linesTemp = new List<string>();
			string[] _headTemp = a.Where(x => x.StartsWith(FIMGlobal.MethodStart)).ToArray();
			foreach(string _h in _headTemp) {
				int startIndex = a.FirstIndexOf(_h);
				string methodName = "";
				bool[] _spec = new bool[] {false,false};
				_h.GetMethodName(out methodName, out _spec);
				string paramn;
				string[] special = GetMethodSpecial(_h, methodName, out paramn);
				if(special == new string[] {"ERROR","ERROR"}) {
					gotError = true;
					return new Dictionary<string, object[]>();
				}
				string retrnType = special[1];
				string paramType = special[0];
				//a[startIndex].WriteLine();
				for(int i = startIndex+1; i < a.Count(); i++) {
					string k = a[i];
					if(k.StartsWith(FIMGlobal.MethodEnd)) {
						_temp.Add(methodName, new object[] {retrnType,paramType,paramn,_linesTemp.ToArray()});
						_linesTemp.Clear();
						break;
					} else {
						_linesTemp.Add(k);
					}
				}
			}
			return _temp;
		}
		
		/// <summary>
		/// Gets any possible RETURN / PARAMETER
		/// </summary>
		/// <param name="a">Original line</param>
		/// <param name="b">GetMethodName()'d line</param>
		/// <returns></returns>
		public static string[] GetMethodSpecial(string _a, string _b, out string paramName) {
			string[] special = {"",""};
			paramName = "";
			string _c = _a;
			if(_c.StartsWith("Today "))
				_c = _c.Substring(6);
			_c = _c.Remove(0,FIMGlobal.MethodStart.Count());
			if(_c == _b) // CASE 0, A METHOD NAME WITHOUT ANY SPECIAL IS THE SAME AS THE GETMETHODNAME VALUE
				return special;
			// Let's go bois
			#region Case
			string mn = _c.Remove(0,_b.Length+1);
			string[] mnSpl = mn.Split(' ');
			// CASE 1: PARAMETER ONLY
			string[] mnSpl2 = mnSpl.Skip(1).ToArray();
			if(mn.ContainsKeyword(FIMGlobal.Keywords.Method_Parameter,true) && !FIMGlobal.Keywords.Method_Return.Any(x => mn.ContainsKeyword(x,true))) {
				object _temp;
				special[0] = GetVariableAssignType(mnSpl.Skip(1).ToArray().BetterToString(" "), out _temp);
				if(GetVariableAssignType2(mnSpl2.BetterToString(" "))-mnSpl2.Count() == 0) {
					FIMOutput.Error("ERROR ON PARSING LOCAL METHOD LINE\nPARAMETER VARIABLE IS MISSING\nLINE BEING PARSED: > " + _a);
					return new string[] {"ERROR","ERROR"};
				}
				paramName = mnSpl2.Skip(GetVariableAssignType2(mnSpl2.BetterToString(" "))).ToArray().BetterToString(" ");
			}
			// CASE 2: RETURN ONLY
			else if(!mn.ContainsKeyword(FIMGlobal.Keywords.Method_Parameter,true) && FIMGlobal.Keywords.Method_Return.Any(x => mn.ContainsKeyword(x,true))) {
				// Get Keyword
				int ind = mn.ContainsKeyword(FIMGlobal.Keywords.Method_Return[0],true) ? 2 : 1 ;
				if(mnSpl.Count()-(ind+GetVariableAssignType2(mnSpl.Skip(ind).ToArray().BetterToString(" ")))>0) {
					FIMOutput.Error("ERROR ON PARSING LOCAL METHOD LINE\nA VARIABLE IS DETECTED ON THE RETURN ARGUMENT\nLINE BEING PARSED: > " + _a);
					return new [] {"ERROR","ERROR"};
				}
				object _temp;
				special[1] = GetVariableAssignType(mnSpl.Skip(ind).ToArray().BetterToString(" "), out _temp);
				// RETURN SHOULDN'T ASK FOR VARIABLE NAME
			}
			// CASE 3: BOTH
			else {
				// GET WHAT KEYWORD WAS FIRST
				int _paramIndex = mnSpl.FirstIndexOf(FIMGlobal.Keywords.Method_Parameter)+1;
				string[] _bTemp = mnSpl.DirectAdd(" ");
				int _retrnIndex = -1;
				int _retrnOrigIndex = -1;
				for(int i = 0; i < _bTemp.Count()-2; i++) {
					if(_bTemp[i] == "with") {
						_retrnOrigIndex = i;
						_retrnIndex = i;
						break;
					} 
					if ("{0} {1}".MelodyFormat(_bTemp[i],_bTemp[i+1]) == "to get") {
						_retrnOrigIndex = i;
						_retrnIndex = i+1;
						break;
					}
				}
				_retrnIndex++;
				object _temp;
				string[] pRet = {"",""}; // PARAM , RETURN
				// CASE 1: PARAM / RETURN
				if(_paramIndex-1 < _retrnOrigIndex) {
					bool _sw = false;
					for(int i = _paramIndex; i < mnSpl.Count(); i++) {
						if(i == _retrnOrigIndex)
							_sw = true;
						if(!_sw) {
							pRet[0] += mnSpl[i] + " ";
						} else {
							if(i > _retrnIndex-1) {
								pRet[1] += mnSpl[i] + " ";
							}
						}
					}
				}
				// CASE 2: RETURN / PARAM
				else if(_paramIndex-1 > _retrnOrigIndex) {
					bool _sw = false;
					for(int i = 0; i < mnSpl.Count(); i++) {
						if(i == _paramIndex-1) {
							_sw = true;
							continue; // wao first time i used this
						}
						if(_sw) {
							pRet[0] += mnSpl[i] + " ";
						} else {
							if(i > _retrnIndex-1) {
								pRet[1] += mnSpl[i] + " ";
							}
						}
					}
				}
				//
				if(pRet[0].Split(' ').Skip(GetVariableAssignType2(pRet[0])).Count() == 0) {
					FIMOutput.Error("ERROR ON PARSING LOCAL METHOD LINE\nPARAMETER VARIABLE IS MISSING\nLINE BEING PARSED: > " + _a);
					return new string[] {"ERROR","ERROR"};
				}
				if(pRet[1].Split(' ').Skip(GetVariableAssignType2(pRet[1])+1).Count() != 0) {
					FIMOutput.Error("ERROR ON PARSING LOCAL METHOD LINE\nA VARIABLE IS DETECTED ON THE RETURN ARGUMENT\nLINE BEING PARSED: > " + _a);
					return new [] {"ERROR","ERROR"};
				}
				//
				special[0] = GetVariableAssignType(mnSpl.Skip(_paramIndex).ToArray().BetterToString(" "), out _temp);
				special[1] = GetVariableAssignType(mnSpl.Skip(_retrnIndex).ToArray().BetterToString(" "), out _temp);
				paramName = pRet[0].Split(' ').Skip(GetVariableAssignType2(pRet[0])).ToArray().BetterToString(" ");
			}
			#endregion
			return special;
		}
		#endregion
		
		#region If-Else Conditional
		
		public static bool ValidIfElseConditional(string[] methodlines) {
			List<string> ml = new List<string>();
			foreach(string s in methodlines) {
				ml.Add(s.TrimStart(' ').TrimEnd('	'));
			}
			return ml.Where(x => FIMGlobal.Keywords.If_Start.Any(y => x.StartsWith(y + " "))).Count() ==
				ml.Where(x => x.Equals(FIMGlobal.Keywords.If_End)).Count();
		}
		
		public static bool ValidWhileConditional(string[] methodlines) {
			List<string> ml = new List<string>();
			foreach(string s in methodlines) {
				ml.Add(s.TrimStart(' ').TrimEnd('	'));
			}
			return ml.Where(x => FIMGlobal.Keywords.Loop_While.Any(y => x.StartsWith(y + " "))).Count() ==
				ml.Where(x => x.Equals(FIMGlobal.Keywords.Loop_End)).Count();
		}
		
		#endregion
		
		#region Variable
		/// <summary>
		/// Gets all Global Variables
		/// </summary>
		public static Dictionary<string, object[]> GetAllGlobalVariables(this string[] fileContent, out bool gotError) {
			bool isInMethod = false;
			Dictionary<string, object[]> _temp1 = new Dictionary<string, object[]>();
			// STRING IS VARIABLE NAME
			// STRING[] CONTAINS {TYPE,VALUE} SINCE ITS A GLOBAL VARIABLE
			foreach(string s in fileContent) {
				if(s.StartsWith(FIMGlobal.MainMethodStart) || s.StartsWith(FIMGlobal.MethodStart))
					isInMethod = true;
				if(s.EndsWith(FIMGlobal.MethodEnd))
					isInMethod = false;
				//
				if(!isInMethod && !string.IsNullOrWhiteSpace(s)) {
					string name;
					object[] v = GetVariable(s, out name);
					if(v[0] == null) {
						FIMOutput.Error((string)v[1]);
						gotError = true;
						return new Dictionary<string, object[]>();
					}
					if(v.Count() == 1) {
						FIMOutput.Error((string)v[0]);
						gotError = true;
						return new Dictionary<string, object[]>();
					}
					_temp1.Add(name, v);
				}
			}
			gotError = false;
			return _temp1;
		}

		// RETURN: TYPE, VALUE
		private static object[] GetVariable(string a, out string VariableName) {
			VariableName = "";
			/*// Assigns a variable
			if(!VariableDeclerationDoesntAssignVariable(a)) {
				// Get what decleration is used
				string dec = FIMGlobal.Keywords.Variable_Initialization.Where(x => a.Contains(" " + x + " ")).FirstOrDefault();
				// Then get its index
				string[] a_Spl = a.Split(' ').Skip(4).ToArray();
				int dec_Index = a_Spl.FirstIndexOf(dec);
				
				List<string> var_TempName = new List<string>();
				for(int i = 0; i < dec_Index; i++) {
					var_TempName.Add(a_Spl[i]);
				}
				string var_Name = var_TempName.BetterToString(" ");
				if(var_Name.ContainsKeyword()) {
					return new string[] {null, "VARIABLE NAME CONTAINS KEYWORD\n\nLINE > " + a};
				}
				string after = a_Spl.Skip(var_Name.Split(' ').Count()+1).ToArray().BetterToString(" ");
				object content;
				string type = GetVariableAssignType(after, out content, true);
				if(type == "ERROR") {
					return new string[] {null, "<INVALID [GLOBAL] VARIABLE ASSIGNMENT>\n"+content+a};
				}
				VariableName = var_Name;
				return new object[] {type,content};
			}
			// Doesn't assign a variable
			else {
				// Get what decleration is used
				string dec = FIMGlobal.Keywords.Variable_Initialization.Where(x => a.Contains(" " + x + " ")).FirstOrDefault();
				// Then get its index
				string[] a_Spl = a.Split(' ').Skip(4).ToArray();
				int dec_Index = a_Spl.FirstIndexOf(dec);
				
				List<string> var_TempName = new List<string>();
				for(int i = 0; i < dec_Index; i++) {
					var_TempName.Add(a_Spl[i]);
				}
				string var_Name = var_TempName.BetterToString(" ");
				if(var_Name.ContainsKeyword()) {
					return new string[] {null, "VARIABLE NAME CONTAINS KEYWORD\n\nLINE > " + a};
				}
				string after = a_Spl.Skip(var_Name.Split(' ').Count()+1).ToArray().BetterToString(" ");
				object content; // UNUSED
				string type = GetVariableAssignType(after, out content);
				if(type == "ERROR") {
					return new string[] {null, "<INVALID [GLOBAL] VARIABLE ASSIGNMENT>\n\nLINE > " + a};
				}
				VariableName = var_Name;
				return new object[] {type,FIMGlobal.Keywords.Variable_Empty};
			}*/
			bool doesVariableAssign = VariableDeclerationAssignsVariable(a);
			// Get what decleration is used
			string dec = FIMGlobal.Keywords.Variable_Initialization.Where(x => a.Contains(" " + x + " ")).FirstOrDefault();
			// Then get its index
			string[] a_Spl = a.Split(' ').Skip(4).ToArray();
			int dec_Index = a_Spl.FirstIndexOf(dec);
			
			List<string> var_TempName = new List<string>();
			for(int i = 0; i < dec_Index; i++) {
				var_TempName.Add(a_Spl[i]);
			}
			string var_Name = var_TempName.BetterToString(" ");
			if(var_Name.ContainsKeyword()) {
				return new string[] {null, "VARIABLE NAME CONTAINS KEYWORD\n\nLINE > " + a};
			}
			string after = a_Spl.Skip(var_Name.Split(' ').Count()+1).ToArray().BetterToString(" ");
			object content; // UNUSED
			string type = GetVariableAssignType(after, out content, doesVariableAssign);
			if(type == "ERROR") {
				return new string[] {null, "<INVALID [GLOBAL] VARIABLE ASSIGNMENT>\n\nLINE > " + a};
			}
			VariableName = var_Name;
			return new object[] {type,(doesVariableAssign ? content : FIMGlobal.Keywords.Variable_Empty)};
		}
				
		public static bool VariableDeclerationAssignsVariable(string a) {
			
			if(FIMGlobal.Keywords.Variable_Boolean.Any(x => a.StartsWith(x))) return true;
			if(FIMGlobal.Keywords.Variable_BooleanArray.Any(x => a.StartsWith(x))) return true;
			
			if(FIMGlobal.Keywords.Variable_Character.Any(x => a.StartsWith(x))) return true;

			if(FIMGlobal.Keywords.Variable_Number.Any(x => a.StartsWith(x))) return true;
			if(FIMGlobal.Keywords.Variable_NumberArray.Any(x => a.StartsWith(x))) return true;
			
			if(FIMGlobal.Keywords.Variable_String.Any(x => a.StartsWith(x))) return true;
			if(FIMGlobal.Keywords.Variable_StringArray.Any(x => a.StartsWith(x))) return true;
			
			return false;
		}
		
		// AFTER THE IS/HAS/LIKE/LIKES/WAS
		// OR THE BEGINNING OF THE VARIABLE_[TYPE] VALUES
		public static string GetVariableAssignType(string a, out object Var_Content, bool getContent = false) {
			bool isNothing = false;
			if(a.EndsWith(FIMGlobal.Keywords.Variable_Empty))
				isNothing = true;
			Var_Content = "";
			object _tempContent = "";
			if(FIMGlobal.Keywords.Variable_Boolean.Any(x => a.StartsWith(x))) {
				if(getContent) {
					if(isNothing) {
						// ERROR: CAN'T BE [BOOL?]
						Var_Content = "BOOLEAN VALUE CANNOT BE EMPTY\n\nLINE > ";
						return "ERROR";
					}
					
					// GETS AND REMOVES THE KEYWORD USED
					_tempContent = a.Split(' ').Skip(FIMGlobal.Keywords.Variable_Boolean.Where(x => a.StartsWith(x)).FirstOrDefault().Split(' ').Count())
												.ToArray()
												.BetterToString();
					//
					
					if(FIMGlobal.Keywords.Boolean_True.Any(x => _tempContent.ToString().ToLower().StartsWith(x))) {
						_tempContent = true;
					}else if(FIMGlobal.Keywords.Boolean_False.Any(x => _tempContent.ToString().ToLower().StartsWith(x))) {
						_tempContent = false;
					}else {
						// ERROR: INVALID BOOL VALUE
						Var_Content = "INVALID BOOLEAN VALUE\n\nLINE > ";
						return "ERROR";
					}
					
					Var_Content = _tempContent;
				}
				return "BOOLEAN";
			}
			if(FIMGlobal.Keywords.Variable_BooleanArray.Any(x => a.StartsWith(x))) return "BOOLEAN_ARRAY";
			
			if(FIMGlobal.Keywords.Variable_Character.Any(x => a.StartsWith(x))) {
				if(getContent && !isNothing) {
					_tempContent = a.Split(' ').Skip(FIMGlobal.Keywords.Variable_Character.Where(x => a.StartsWith(x)).FirstOrDefault().Split(' ').Count())
												.ToArray()
												.BetterToString(" ");
					if(!_tempContent.ToString().StartEndsWith("'")) {
						// ERROR: NOT A CHARACTER
						Var_Content = "INVALID CHARACTER (CHARACTER SHOULD START AND END WITH ')\n\nLINE > ";
						return "ERROR";
					}
					if(_tempContent.ToString().TrimStartEnd("'").Count() > 1) {
						// ERROR: NOT A VALID CHARACTER
						Var_Content = "TOO MANY CHARACTERS IN CHARACTER LITERAL\n\nLINE > ";
						return "ERROR";
					}
					char val;
					if(!char.TryParse(_tempContent.ToString().TrimStartEnd("'"), out val)) {
						Var_Content = "INVALID CHARACTER\n\nLINE > ";
						return "ERROR";
					}
					Var_Content = val;
				}
				return "CHARACTER";
			}

			if(FIMGlobal.Keywords.Variable_Number.Any(x => a.StartsWith(x))) {
				if(getContent) {
					if(isNothing) {
						// ERROR: CAN'T BE NULL INT
						Var_Content = "INTEGER CANNOT BE EMPTY\n\nLINE > ";
						return "ERROR";
					}
					_tempContent = a.Split(' ').Skip(FIMGlobal.Keywords.Variable_Number.Where(x => a.StartsWith(x)).FirstOrDefault().Split(' ').Count())
												.ToArray()
												.BetterToString(" ");
					int val = 0;
					if(!int.TryParse(_tempContent.ToString(), out val)) {
						// ERROR: INVALID VALUE
						Var_Content = "INTERGER VALUE INVALID\n\nGOT > " + _tempContent + "\nLINE > ";
						return "ERROR";
					}
					Var_Content = val;
				}
				return "NUMBER";
			}
			if(FIMGlobal.Keywords.Variable_NumberArray.Any(x => a.StartsWith(x))) return "NUMBER_ARRAY";
			
			if(FIMGlobal.Keywords.Variable_String.Any(x => a.StartsWith(x))) {
				if(getContent && !isNothing) {
					_tempContent = a.Split(' ').Skip(FIMGlobal.Keywords.Variable_String.Where(x => a.StartsWith(x)).FirstOrDefault().Split(' ').Count())
												.ToArray()
												.BetterToString(" ");
					if(!_tempContent.ToString().StartEndsWith("\"")) {
						// ERROR: NOT A STRING
						Var_Content = "STRING SHOULD START AND END WITH \"\n\nLINE > ";
						return "ERROR";
					}
					Var_Content = _tempContent.ToString();
				}
				return "STRING";
			}
			if(FIMGlobal.Keywords.Variable_StringArray.Any(x => a.StartsWith(x))) return "STRING_ARRAY";
			
			return "ERROR";
		}
		
		public static int GetVariableAssignType2(string a) {
			if(FIMGlobal.Keywords.Variable_Boolean.Any(x => a.StartsWith(x)))
				return FIMGlobal.Keywords.Variable_Boolean.Where(x => a.StartsWith(x)).FirstOrDefault().Split(' ').Count();
			if(FIMGlobal.Keywords.Variable_BooleanArray.Any(x => a.StartsWith(x)))
				return FIMGlobal.Keywords.Variable_BooleanArray.Where(x => a.StartsWith(x)).FirstOrDefault().Split(' ').Count();
			
			if(FIMGlobal.Keywords.Variable_Character.Any(x => a.StartsWith(x)))
				return FIMGlobal.Keywords.Variable_Character.Where(x => a.StartsWith(x)).FirstOrDefault().Split(' ').Count();

			if(FIMGlobal.Keywords.Variable_Number.Any(x => a.StartsWith(x)))
				return FIMGlobal.Keywords.Variable_Number.Where(x => a.StartsWith(x)).FirstOrDefault().Split(' ').Count();
			if(FIMGlobal.Keywords.Variable_NumberArray.Any(x => a.StartsWith(x)))
				return FIMGlobal.Keywords.Variable_NumberArray.Where(x => a.StartsWith(x)).FirstOrDefault().Split(' ').Count();
			
			if(FIMGlobal.Keywords.Variable_String.Any(x => a.StartsWith(x)))
				return FIMGlobal.Keywords.Variable_String.Where(x => a.StartsWith(x)).FirstOrDefault().Split(' ').Count();
			if(FIMGlobal.Keywords.Variable_StringArray.Any(x => a.StartsWith(x)))
				return FIMGlobal.Keywords.Variable_StringArray.Where(x => a.StartsWith(x)).FirstOrDefault().Split(' ').Count();
			
			return -1;
		}
		
		/// <summary>
		/// Changes any '' to the corresponding variable value
		/// </summary>
		public static string StringReplaceVariable(string a, Dictionary<string, object> vars, out bool isError) {
			isError = false;
			// So we dont waste time looping even though there's nothing to replace
			if(a.TrimStartEnd("\"", true).Split('"').Count() == 1) {
				return a;
			}
			List<string> _temp1 = new List<string>();
			string _tempName = "";
			bool _isChecking = false;
			int _ignore = -1;
			string aTrim = a.TrimStartEnd("\"", true);
			for(int i = 0; i < aTrim.Count(); i++) {
				string v = aTrim[i].ToString();
				if(i != _ignore) {
					// DO IGNORES :D
					if(v == "\\") {
						_ignore = i + 1;
					} else {
						if(v == "\"") {
							if(_isChecking) {
								_tempName += v;
								if(!_temp1.Contains(_tempName.TrimStartEnd("\"",true)))
									_temp1.Add(_tempName);
								_tempName = "";
								_isChecking = false;
							} else {
								_isChecking = true;
							}
						}
						if(_isChecking) {
							_tempName += v;
						}
					}
				}
			}
			
			if(_isChecking) {
				// ERROR: VARIABLE ASSIGNMENT HASN'T ENDED
				isError = true;
				return "VARIABLE ASSIGNMENT ERROR, PLEASE CHECK THE DOUBLE QUOTATION MARKS";
			}
			string _tempOutput = a;
			foreach(string s in _temp1) {
				/*if(Program.FriendshipReport.GlobalVariables.ContainsKey(s)) {
					_tempOutput = _tempOutput.Replace(s,Program.FriendshipReport.GlobalVariables[s].Value.ToString().TrimStartEnd("\"", true));
				} else {
					if(isMain) {
						if(Program.FriendshipReport.MainMethod.LocalVariables.ContainsKey(s)) {
							_tempOutput = _tempOutput.Replace(s, Program.FriendshipReport.MainMethod.LocalVariables[s].Value.ToString().TrimStartEnd("\"", true));
						} else {
							// ERROR: NULL VARIABLE
							isError = true;
							return "VARIABLE DOESN'T EXIST GLOBALLY OR IN THE MAIN METHOD";
						}
					} else {
						if(Program.FriendshipReport.LocalMethods[methodHead].LocalVariables.ContainsKey(s)) {
							_tempOutput = _tempOutput.Replace(s, Program.FriendshipReport.LocalMethods[methodHead].LocalVariables[s].Value.ToString().TrimStartEnd("\"", true));
						} else {
							// ERROR: NULL VARIABLE
							isError = true;
							return "VARIABLE DOESN'T EXIST GLOBALLY OR IN THE MAIN METHOD";
						}
					}
				}*/
				if(vars.ContainsKey(s.TrimStartEnd("\"",true))) {
					_tempOutput = _tempOutput.Replace(s, vars[s.TrimStartEnd("\"",true)].ToString().TrimStartEnd("\"",true));
				} else {
					isError = true;
					return "VARIABLE DOESN'T EXIST GLOBALLY OR LOCALLY";
				}
			}
			return _tempOutput;
		}
		#endregion
		
		#region Extras
		/// <summary>
		/// Gets the first decleration keyword it detects
		/// 
		/// Input must start where you want it to detect
		/// </summary>
		public static string GetVariableDeclerationKeyword(string a) {
			/*if(FIMGlobal.Keywords.Variable_String.Where(x => a.StartsWith(x)))
				return FIMGlobal.Keywords.Variable_String.Where(x => a.StartsWith(x)).FirstOrDefault();*/
			string[][] l = {
				FIMGlobal.Keywords.Variable_String,
				FIMGlobal.Keywords.Variable_StringArray,
				FIMGlobal.Keywords.Variable_Number,
				FIMGlobal.Keywords.Variable_NumberArray,
				FIMGlobal.Keywords.Variable_Boolean,
				FIMGlobal.Keywords.Variable_BooleanArray,
				FIMGlobal.Keywords.Variable_Character,
			};
			/*foreach(string[] v in l) {
				if(v.ToArray().Where(x => a.StartsWith(x)))
					return v.Where(x => a.StartsWith(x)).FirstOrDefault();
			}
			if(a.StartsWith(FIMGlobal.Keywords.Variable_Empty))
				return FIMGlobal.Keywords.Variable_Empty;*/
			return "ERROR";
		}
		
		/// <summary>
		/// Removes the two types of comments. Should be used with FIMMethods.RemoveEmptyEnd()
		/// </summary>
		public static string[] RemoveComments(string[] a) {
			List<string> b = new List<string>();
			for(int i = 0; i < a.Count(); i++) {
				string l = a[i];
				bool inLine = false;
				// P.S
				if(l.Split(' ')[0].ToLower() == "p.s.")
					inLine = true;
				else if(l.Split(' ')[0].ToLower().EndsWith("p.s.")) {
					string bl = l.Split(' ')[0].Split('.').RemoveFromEnd().BetterToString();
					if(bl.ToLower().Replace("p","").Count() > 0) {
						inLine = true;
					}
				}
				
				// ()
				if(!inLine) {
					string newLine = "";
					//
					if(l.StartsWith("(") && l.EndsWith(")")) {
						b.Add("");
						continue;
					}
					if (new [] { "(", ")" }.All(x => l.Contains(x))) {
						int lastSeenEnd = -1;
						bool isBlockComment = false;
						// DETECT ()
						int ignore = -1;
						for (int c = 0; c < l.Count(); c++) {
							string le = l[c].ToString();
								
							if(c != ignore) {
								if(le == "\\") {
									ignore = c + 1;
									continue;
								}
								if (le == "(") {
									if (isBlockComment) {
										// RETURN ERROR: COMMENT STARTED INSIDE A COMMENT
									} else {
										isBlockComment = true;
									}
								} else if (le == ")") {
									if (isBlockComment) {
										isBlockComment = false;
										lastSeenEnd = c + 1;
									} else {
										// RETURN ERROR: END COMMENT WITHOUT A COMMENT STARTED
									}
								} else {
									if (c == lastSeenEnd && le == " ") {
										// IGNORE SPACE AFTER ) IF THERE IS ONE
									} else {
										if (!isBlockComment) {
											newLine += le;
										}
									}
								}
							} else newLine += le;
						}
					}
					//
					b.Add(newLine != "" ? newLine.TrimEnd(' ') : l.TrimEnd(' '));
				}
			}
			return b.ToArray();
		}
		
		/// <summary>
		/// Extended method that removes the empty lines after the last valid line
		/// </summary>
		public static string[] RemoveEmptyEnd(this string[] a) {
			List<string> b = new List<string>();
			bool c = false;
			foreach(string d in a.Reverse()) {
				if(d != "")
					c = true;
				if(c)
					b.Add(d);
			}
			b.Reverse();
			return b.ToArray();
		}
		
		/// <summary>
		/// Extended method that removes the empty lines before the last valid line
		/// </summary>
		public static string[] RemoveEmptyStart(this string[] a) {
			List<string> b = new List<string>();
			bool c = false;
			foreach(string d in a) {
				if(d != "")
					c = true;
				if(c)
					b.Add(d);
			}
			return b.ToArray();
		}
		
		/// <summary>
		/// Extended method that removes all empty lines from all sides
		/// </summary>
		public static string[] RemoveEmptyLines(this string[] a) {
			return a.RemoveEmptyStart().RemoveEmptyEnd();
		}
		
		/// <summary>
		/// Checks if the Friendship Report has started and ended properly
		/// </summary>
		public static bool CheckStartEndValidity(string[] a) {
			return a.FirstOrDefault().StartsWith(FIMGlobal.ReportStart) && a.LastOrDefault().StartsWith(FIMGlobal.ReportEnd);
		}
		
		/// <summary>
		/// Removes the "Dear Princess Celestia" and "Your faithful student" parts
		/// </summary>
		public static string[] RemoveReportStartEnd(this string[] a) {
			return a.Where(x => !x.StartsWith(FIMGlobal.ReportStart) && !x.StartsWith(FIMGlobal.ReportEnd)).ToArray().RemoveEmptyLines().ToArray();
		}
		
		/// <summary>
		/// Gets the AUTHOR NAME and FRIENDSHIP REPORT TITLE
		/// </summary>
		public static _FIMProjectInfo GetReportDetails(string[] a) {
			string[] b = {"",""};
			// AUTHOR
			b[0] = a.FirstOrDefault().Remove(0,FIMGlobal.ReportStart.Count());
			// TITLE
			b[1] = a.LastOrDefault().Remove(0,FIMGlobal.ReportEnd.Count());
			//
			return new _FIMProjectInfo(b[1],b[0]);
		}
		public class _FIMProjectInfo { 
			private string _Author;
			private string _Title;
			public string Author {
				get { return _Author; }
			}
			public string Title {
				get { return _Title; }
			}
			public _FIMProjectInfo(string author, string title) {
				_Author = author;
				_Title = title;
			}
		}
		
		public static bool ContainsKeyword(this string a, bool c = false) {
			foreach(string b in FIMGlobal.KeywordsList) {
				if(c) {
					if(a.Contains(" " + b + " ") || a.EndsWith(" " + b) || (a.StartsWith(" " + b)) || (a.StartsWith(b)))
						return true;
				} else {
					if(a.Contains(" " + b + " ") || a.EndsWith(" " + b) || (a.StartsWith(" " + b)))
						return true;
				}
			}
			return false;
		}
		
		public static bool ContainsKeyword(this string a, string b, bool c = false) {
			if(c)
				return (a.Contains(" " + b + " ") || a.EndsWith(" " + b) || (a.StartsWith(" " + b)) || (a.StartsWith(b)));
			return (a.Contains(" " + b + " ") || a.EndsWith(" " + b) || (a.StartsWith(" " + b)));
		}
		
		public static string GetValueType(this object a) {
			if(a.ToString().StartEndsWith("\""))
				return "STRING";
			int _t;
			if(int.TryParse(a.ToString(), out _t))
				return "NUMBER";
			if(a.ToString().StartEndsWith("'"))
				return "CHARACTER";
			if(FIMGlobal.Keywords.Boolean_True.Any(x => a.ToString() == x) ||
			   FIMGlobal.Keywords.Boolean_False.Any(x => a.ToString() == x))
				return "BOOLEAN";
			if(a as IEnumerable<string[]> != null) {
				return "STRING_ARRAY";
			}
			if(a as IEnumerable<bool[]> != null) {
				return "BOOLEAN_ARRAY";
			}
			if(a as IEnumerable<int[]> != null) {
				return "NUMBER_ARRAY";
			}
			return "ERROR";
		}
		
		public static object GetVariableValue(this string a, out bool gotError) {
			gotError = false;
			if(a.StartEndsWith("\""))
				return a;
			int _t;
			if(int.TryParse(a, out _t))
				return _t;
			char _c;
			if(a.StartEndsWith("'") && char.TryParse(a.TrimStartEnd("'"), out _c))
				return _c;
			if(FIMGlobal.Keywords.Boolean_True.Any(x => a == x))
			    return true;
			if(FIMGlobal.Keywords.Boolean_False.Any(x => a == x))
				return false;
			/*if(a as IEnumerable<string[]> != null) {
				return "STRING_ARRAY";
			}
			if(a as IEnumerable<bool[]> != null) {
				return "BOOLEAN_ARRAY";
			}
			if(a as IEnumerable<int[]> != null) {
				return "NUMBER_ARRAY";
			}*/
			gotError = true;
			return null;
		}
		
		public static string GetCompareSign(string a, out string word) {
			if(FIMGlobal.Keywords.Conditional_Not.Any(x => a.Contains(" " + x + " "))) {
				word = FIMGlobal.Keywords.Conditional_Not.Where(x => a.Contains(" " + x + " ")).FirstOrDefault();
				return "~=";
			}
			
			//
			if(FIMGlobal.Keywords.Conditional_LessThanEqual.Any(x => a.Contains(" " + x + " "))) {
				word = FIMGlobal.Keywords.Conditional_LessThanEqual.Where(x => a.Contains(" " + x + " ")).FirstOrDefault();
				return "<=";
			}
			//
			if(FIMGlobal.Keywords.Conditional_GreaterThanEqual.Any(x => a.Contains(" " + x + " "))) {
				word = FIMGlobal.Keywords.Conditional_GreaterThanEqual.Where(x => a.Contains(" " + x + " ")).FirstOrDefault();
				return ">=";
			}
			//
			if(FIMGlobal.Keywords.Conditional_LessThan.Any(x => a.Contains(" " + x + " "))) {
				word = FIMGlobal.Keywords.Conditional_LessThan.Where(x => a.Contains(" " + x + " ")).FirstOrDefault();
				return "<";
			}
			//
			if(FIMGlobal.Keywords.Conditional_GreaterThan.Any(x => a.Contains(" " + x + " "))) {
				word = FIMGlobal.Keywords.Conditional_GreaterThan.Where(x => a.Contains(" " + x + " ")).FirstOrDefault();
				return ">";
			}
			//
			if(FIMGlobal.Keywords.Conditional_Equal.Any(x => a.Contains(" " + x + " "))) {
				word = FIMGlobal.Keywords.Conditional_Equal.Where(x => a.Contains(" " + x + " ")).FirstOrDefault();
				return "==";
			}
			//
			word = null;
			return null;
		}

		#endregion
		
	}
	public static class FIMOutput {
		public static void Error(string a) {
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine(a);
			Console.ResetColor();
		}
		
		public static void Output(string a) {
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine(a.TrimStartEnd("\"", true).Replace(@"\n",Environment.NewLine).Replace("\\",""));
			Console.ResetColor();
		}
		
		public static void Debug(string a) {
			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.WriteLine(a);
			Console.ResetColor();
		}
	}
}
