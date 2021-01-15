using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Melody.Helper;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace NetFim__
{
	public static class NetMethods {
		public static string RemovePunctuation(this string line) {
			if(Program.punctuations.Any(x => line.EndsWith(x))) {
				foreach(string b in Program.punctuations) {
					if(line.EndsWith(b)) {
						return line.Substring(0, line.Length - 1);
					}
				}
			}
			return line;
		}
	}
	class Program
	{
		public static void Main(string[] args)
		{
			while(true) {
				"[Melody]: Please type in the file\'s name you wish to compile.".WriteLine();
				"> ".Write();
				string file = Console.ReadLine();
				file = file.EndsWith(".fpp") ? file.Substring(0, file.Count() - 4) : file ;
				if(File.Exists(@file + ".fpp")) {
					Compile(file);
					Console.WriteLine();
					Console.WriteLine("----------------------------------------------------------------");
					Console.WriteLine();
				} else {
					"[Melody]: That file doesn't exist! Please try again.".WriteLine();
				}
				gstor.Clear();
				func.Clear();
				mainmethod = new string[] {};
				codeLines = new string[] {};
				Author = "";
				ProgramTitle = "";
			}
		}
		public static void CWrite(string message, ConsoleColor color = ConsoleColor.Gray) {
			Console.ForegroundColor = color;
			message.WriteLine();
			Console.ResetColor();
		}
		#region Compiler
		public static async void Compile(string filename) {
			Console.Clear();
			string[] comlist = File.ReadAllLines(@"" + filename + ".fpp");
			codeLines = comlist;
			#region Setup
			for(int temp1 = 0; temp1 < comlist.Count(); temp1++)
				comlist[temp1] = RemoveComment(comlist[temp1]).Replace('“','"').Replace('”','"');
			comlist = comlist.Where(x => x != "null").ToArray();
			#region Checker
			// If class has started properly
			if(!comlist[0].StartsWith("Dear Princess Celestia:")) {
				Error("Invalid Class Starter");
			} else {
				ProgramTitle = comlist[0].Remove(0,"Dear Princess Celestia:".Count()).TrimStartEnd(" ", " ");
			}
			// If class has ended properly
			if(!comlist[comlist.Count()-1].StartsWith("Your faithful student, "))
				Error("Invalid Class End.");
			Author = comlist[comlist.Count()-1].Remove(0, "Your faithful student, ".Count()).TrimStartEnd(" ").RemovePunctuation();
			// Check if all sentences end with a punctuation.
			for(int temp2 = 0; temp2 < comlist.Count(); temp2++)
				if(!punctuations.Any(x => comlist[temp2].EndsWith(x, StringComparison.CurrentCulture)) && comlist[temp2] != string.Empty)
					Error("No punctuation at", comlist[temp2]);
			// Check if there's two or more main methods
			if(comlist.Count(x => x.StartsWith("Today I learned")) > 1)
				Error("Two or more main methods found");
			// Check if there's a class. If there is, continue checking and check for more. If there's not a single one, throw error.
			List<string> cl = new List<string>();
			bool isClass = false, isMain = false;
			bool isIfWhileStatement = false;
			List<int> ifWhileNum = new List<int>();
			int mtype = 0;
			string mn = "";
			string tl = "";
			for(int temp3 = 1; temp3 < comlist.Count(); temp3++) {
				string[] ac = {"I learned ","Today I learned "};
				string en = "That's all about ";
				string st = comlist[temp3].RemovePunctuation();
				string str = comlist[temp3];
				if(ac.Any(x => st.StartsWith(x))) {
					if(isClass) {
						Error("Method started inside another method ", comlist[temp3 + 1]);
					} else {
						// Method Type
						if(st.StartsWith(ac[1]))
							isMain = true;
						mtype = st.Contains("to get") || st.Contains("with") ? 1 : 0;
						// Get method names
						if(mtype == 0) {
							if(st.StartsWith(ac[0]))
								mn = st.Remove(0,ac[0].Count()).RemovePunctuation();
							else
								mn = st.Remove(0,ac[1].Count()).RemovePunctuation();
						} else {
							int temp4 = st.StartsWith(ac[0]) ? 0 : 1;
							mn = st.Remove(0, ac[temp4].Count()); // X to get/ with [a number].
							if(mn.Contains("to get")) {
								for(int abb = 0; abb < mn.Count()-5; abb++) {
									string a = "";
									for(int acb = 0; acb <= 5; acb++)
										a += mn[abb+acb];
									if(a == "to get")
										mn = mn.Substring(0, abb);
								}
							} else if(mn.Contains("with")) {
								string[] alb = mn.Split(' ');
								mn = alb.RemoveToEnd(alb.FirstIndexOf("with")).BetterToString();
							}
						}
						isClass = true;
						tl = st;
					}
				}
				if(st.TrimStartEnd(" ").EndsWith((en + mn).TrimStartEnd(" "), StringComparison.CurrentCulture)) {
					if(!isClass)
						Error("Method end detected without any method", comlist[temp3 + 1]);
					else {
						// Add method to public class list
						if(isMain) {
							mainmethod = cl.ToArray();
						}
						string tp = cl.Any(x => x.StartsWith("Then you get ")) ? "return" : "run" ;
						func.Add(mn.TrimStartEnd(" "), new fucl(cl.ToArray(), tp, temp3, tl));
						cl.Clear();
						mn = "";
						tl = "";
						mtype = -1;
						isClass = false;
						isMain = false;
					}
				}
				// wao, tabs
				if(st.StartsWith("\t") && (Regex.Matches(st,"\t").Count != ifWhileNum.Count())) {
					Error("A tab outside of an if-else statement was detected outside the code.");
				}
				// LETS TO THIS BITCH-ASS IF-ELSE
				string[] ie = {"If","When"};
				if(ie.Any(x => st.TrimStart(' ').StartsWith(x))) {
					if(!str.EndsWith(":"))
						Error("Loop-related sentences must end with a \"", st);
					isIfWhileStatement = true;
					ifWhileNum.Add(0);
					// Comparison:
					// HAD MORE THAN | HAD LESS THAN | IS GREATER OR EQUAL TO | IS LESSER OR TO | IS EQUAL TO | ISN'T EQUAL TO
					string[] cond = {"had more than", "had less than", "is greater or equal to", "is lesser or equal to", "is equal to", "isn\'t equal to", "is", "is not", "isn\'t", "wasn\'t", "was"};
					if(!cond.Any(x => st.Contains(x)))
						Error("No condition found in loop", st);
				}
				string[] elp = { "Otherwise", "Or else" };
				if(elp.Any(x => st.TrimStart(' ').StartsWith(x)))
				{
					if(!str.EndsWith(":"))
						Error("Loop-related sentences must end with a \"", st);
					string[] cond = {"had more than", "had less than", "is greater or equal to", "is lesser or equal to", "is equal to", "isn\'t equal to", "is", "is not", "isn\'t", "wasn\'t", "was"};
					if(st != "Otherwise:" && !cond.Any(x => st.Contains(x)))
						Error("No condition found in else condition", st);
					if(!isIfWhileStatement)
						Error("Recieved loop else while no loop was executed", st);
				}
				if(st.EndsWith("That's what I did")) {
					if(!isIfWhileStatement)
						Error("Recieved loop end while no loop was executed", st);
					if(ifWhileNum.Count() > 1)
						ifWhileNum.RemoveAt(ifWhileNum.Last() - 1);
					else {
						isIfWhileStatement = false;
						ifWhileNum.Clear();
					}
				}
				if(isClass && !ac.Any(x => st.TrimStart(' ').StartsWith(x)))
					cl.Add(str);
			}
			if(isClass)
				Error("A method has not been properly exited");
			if(func.Count() == 0)
				Error("No methods found");
			if(mainmethod.Count() == 0)
				Error("Main method is empty");
			#endregion
			#endregion
			Console.WriteLine(ProgramTitle + " - By: " + Author);
			Console.WriteLine("----------------------------------------------------------------");
			Console.WriteLine();
			Run(mainmethod);
		}
		public static async void Run(string[] funcname) {
			// TODO LIST:
			// I asked X Y (prints Y, waits for user input, puts it to X) [Y is always a fucking string :)))]
			string[] lines = {};
			string[] linesWithPunc = funcname;
			int lin = 1;
			bool ignore = false;
			int upToIgnore = 0;
			int tabs = 0;
			List<string> temp1 = new List<string>();
			foreach(string s in linesWithPunc)
				temp1.Add(s.RemovePunctuation());
			lines = temp1.ToArray();
			foreach(string line in lines) {
				//int l = lines.FirstIndexOf(line) + 4;
				if(!ignore) {
					#region TryWrite
					string[] tryWriteTrial = {"sang", "wrote", "said"};
					if(tryWriteTrial.Any(x => line.StartsWith("I " + x)))
						TryWrite(line);
					#endregion
					#region TryMakeVariable
					if(line.StartsWith("Did you know that "))
						TryMakeVariable(line);
					#endregion
					#region TryReplaceVariable
					foreach(string key in gstor.Keys)
						if(new string[] {"became","is now","are now","now likes","now like","becomes"}.Any(x => line.StartsWith(key + " " + x)))
							TryReplaceVariable(line);
					#endregion
					
					// STILL BEING WORKED ON:
					#region TryInput
					if(line.StartsWith("I asked")) {
						TryInput(line);
					}
					#endregion
					#region TryIncrementNumber
					if(line.EndsWith("got one more") || line.EndsWith("got one less"))
						TryAddSubNumber(line);
					#endregion
					#region RunMethod
					string[] runTrial = {"remembered", "would"};
					if(runTrial.Any(x => line.StartsWith("I " + x))) {
						string a = "I ";
						a += line.StartsWith("I remembered") ? "remembered" : "would" ;
						string b = line.Remove(0, a.Count());
						if(!func.ContainsKey(b))
							Error("Tried to run a non-existent method", line);
						//RunMethod(func[b], "run");
					}
					#endregion
					#region TryIfElse
					if(line.StartsWith("If") || line.StartsWith("When")) {
						if(!line.EndsWith(":"))
							Error("If-Else statements declerations always ends with \":\"",line);
						//Get all lines of the if-else
						List<string> ie = new List<string>();
						List<string> iepunc = new List<string>();
						string st = line.Split(' ').Skip(1).ToArray().BetterToString(" ");
						foreach(string li in lines.Skip(lin)) {
							if(li == "That's what I did.")
								break;
							ie.Add(li);
						}
						foreach(string li in linesWithPunc.Skip(lin)) {
							if(li == "That's what I did.")
								break;
							iepunc.Add(li);
						}
						TryIfElse(st,ie.ToArray(),iepunc.ToArray());
					}
					#endregion
				}
				else {
					if(lin >= upToIgnore)
						ignore = false;
				}
				lin++;
			}
			lin--;
		}
		// NOTE: METHODRUN WILL BE ADDED BACK WHEN I FINISH THE ONES IN THE MAIN METHOD <3
		#region GlobalRun
		public static void TryWrite(string linespl) {
			string[] spl = linespl.RemovePunctuation().Split(' ');
			string[] aw = {"sang", "wrote", "said", "remebered", "would"};
			if(aw.Any(x => ("I " + x).Equals(spl[0] + " " + spl[1]))) {
				string vr = "";
				List<string> wr = new List<string>();
				string al = "";
				bool isVar = false;
				bool holySh = false;
				string mt = spl.RemoveIn(0, 1).BetterToString(" ").TrimStartEnd(" ");
				if(GetType(@mt) == "string") {
					holySh = true;
					if(mt.StartsWith("\"")) mt = mt.Remove(0,1);
					if(mt.EndsWith("\"")) mt = mt.Substring(0,mt.Length-1);
					string mtstep = "";
					bool isOnVariableStep = false;
					string iOVS = "";
					foreach(char s in mt) {
						if(s.ToString() != "'") {
							if(isOnVariableStep)
								iOVS += s;
							else
								mtstep += s;
						} else {
							if(!isOnVariableStep) {
								isOnVariableStep = true;
							} else {
								isOnVariableStep = false;
								if(gstor.ContainsKey(iOVS)) {
									mtstep += @gstor[iOVS].Value.ToString().TrimStartEnd("\"");
								} else {
									// ERROR
								}
								iOVS = "";
							}
						}
					}
					if(!mtstep.StartsWith("\""))
						mtstep = "\"" + mtstep;
					if(!mtstep.EndsWith("\""))
						mtstep += "\"";
					mt = mtstep;
				}
				string[] _wrAdd = {"added to","and","plus"};
				string[] _wrMinus = {"without", "minus"};
				string[] _wrMult = {"multiplied with", "times"};
				if(func.ContainsKey(mt)) {
					//RunMethod(func[mt], "write");
				} else if(gstor.ContainsKey(mt)) {
					(">> " + @gstor[mt].Value).WriteLine();
				/* ADD */
				} else if(_wrAdd.Any(x => mt.Contains(" {0} ".MelodyFormat(x)))) {
					string[] cont = _wrAdd.Where(x => mt.Contains(" {0} ".MelodyFormat(x))).FirstOrDefault().Split(' ');
					string[] f = {"",""}, ft = {"",""};
					bool and2nd = false;
					int and2ndStart = 0;
					string[] spli = mt.Split(' ');
					for(int i = 0; i < spli.Count(); i++) {
						string vl1 = "EMPTY", vl2 = "EMPTY";
						if(i < spli.Count())
							vl1 = spli[i];
						if(i + 1 < spli.Count())
							vl2 = spli[i+1];
						if(vl1 + vl2 == "addedto") {
							and2nd = true;
							and2ndStart = i+2;
						} else if(vl1 == "and" || vl1 == "plus") {
							and2nd = true;
							and2ndStart = i+1;
						}else {
							if(and2nd) {
								if(i>=and2ndStart)
									f[1] += vl1 + " ";
							} else
								f[0] += vl1 + " ";
						}
					}
					f[0] = f[0].TrimStartEnd(" ");
					f[1] = f[1].TrimStartEnd(" ");
					for(int i = 0; i < 2; i++) {
						string k = f[i].TrimStartEnd(" ");
						if(gstor.ContainsKey(k)) {
							if(gstor[k].type != "number" && gstor[k].type != "string")
								Error("Tried combining two non-number or non-string variabies!",linespl);
							ft[i] = gstor[k].type;
							f[i] = (string)gstor[k].Value;
						}
					}
					if((ft[0] + ft[1]) != "numbernumber") {
						if(!f[0].StartsWith("\""))
							f[0] = "\""+f[0];
						if(!f[1].EndsWith("\""))
							f[1] += "\"";
					}
					(">> {0}{1}".MelodyFormat(f[0].TrimEnd('"'),f[1].TrimStart('"'))).WriteLine();
				/* SUBTRACT */
				} else if(_wrMinus.Any(x => mt.Contains(" {0} ".MelodyFormat(x)))) {
					string cont = _wrMinus.Where(x => mt.Contains(" {0} ".MelodyFormat(x))).FirstOrDefault();
					string[] f = {"",""}, ft = {"",""};
					bool and2nd = false;
					int and2ndStart = 0;
					string[] spli = mt.Split(' ');
					for(int i = 0; i < spli.Count(); i++) {
						// Good thing this one doesn't have a keyword that are two words
						if(spli[i] == cont)
							and2nd = true;
						else {
							if(and2nd)
								f[1] += spli[i] + " ";
							else
								f[0] += spli[i] + " ";
						}
					}
					f[0] = f[0].TrimStartEnd(" ");
					f[1] = f[1].TrimStartEnd(" ");
					int _removeLast = 0;
					if(int.TryParse(f[1], out _removeLast)) {
						string v = f[0];
						if(GetType2(v) == "string") {
							if(gstor.ContainsKey(v))
								v = (string)gstor[v].Value;
						} else {
							Error("First argument should be a string!",linespl);
						}
						v = v.Substring(v.Length-_removeLast);
						(">> {0}{1}".MelodyFormat(f[0].TrimEnd('"'),f[1].TrimStart('"'))).WriteLine();
					} else {
						string v = f[0];
						if(GetType2(v) == "string") {
							if(gstor.ContainsKey(v))
								v = (string)gstor[v].Value;
						} else {
							Error("First argument should be a string!",linespl);
						}
						//
						string k = f[1];
						if(GetType2(k) == "string") {
							if(gstor.ContainsKey(k))
								k = (string)gstor[k].Value;
						} else {
							Error("Second argument should be a string!",linespl);
						}
						//
						v = v.Replace(k.TrimStartEnd("\""),"").Replace("  "," ");
						(">> " + v).WriteLine();
					}
				/* MULTIPLY */
				} else if(_wrMult.Any(x => mt.Contains(" {0}".MelodyFormat(x)))) {
					string[] mtspl = mt.Split(' ');
					string cont = _wrMinus.Where(x => mt.Contains(" {0} ".MelodyFormat(x))).FirstOrDefault();
					// I SAID X [TIMES]/MULTIPLIED WITH Y [TIMES]
					
					bool and2nd = false;
					int and2ndStart = 0;
					for(int i = 0; i < mtspl.Count(); i++) {
						string vl1 = "EMPTY", vl2 = "EMPTY";
						if(i<mtspl.Count())
							vl1 = mtspl[i];
						if(i+1<mtspl.Count())
							vl2 = mtspl[i+1];
						if(cont == "multiplied with") {
							if(vl1+vl2 == "multipliedwith") {
								
							}
						} else {
							
						}
					}
					
					/*string nv = "";
					for(int i = 1; i <= count; i++) {
						nv += v;
					}
					nv = "\"" + nv + "\"";
					
					Console.WriteLine(">> " + nv);*/
				
				/* DIVIDE */
				}  else if(mt.Contains(" divided by ")) {
					string[] mtspl = mt.Split(' ');
					
					
				} else {
					Dictionary<string, string> mth = new Dictionary<string, string>() {
						{"addded to","+"}, // X + Y
						{"subtracted from","-"}, // Y - X (where Y is lower than X)
						{"multiplied to","*"}, // X * Y
						{"divided by","/"} // X / Y
					};
					if(mth.Keys.Any(x => linespl.Contains(x))) {
						int ab = 0;
						for(int ba = 0; ba < spl.Count()-1; ba++) {
							if(spl[ba] + " " + spl[ba+1] == mth.Keys.Where(x => linespl.Contains(x)).First()) {
								ab = ba+1;
								break;
							}
						}
						string[] a = {"",""};
						List<string> tmp = new List<string>();
						for(int baa = 0; baa < spl.Count(); baa++) {
							if(baa == spl.Count() - 1) {
								a[1] = tmp.BetterToString(" ");
							} else {
								if(baa == ab-1) {
									a[0] = tmp.BetterToString(" ");
									tmp.Clear();
								} else {
									tmp.Add(spl[baa]);
								}
							}
						}
						//Console.WriteLine("{0} {1}".MelodyFormat(a[0], a[1]));
					} else {
						if(GetType(@mt) != "null" || holySh)
							(">> " + @mt).WriteLine();
						else
							Error("Invalid Variable", linespl);
					}
				}
			}
		}
		public static void TryMakeVariable(string linespl) {
			string st = "Did you know that ";
			if(linespl.StartsWith(st)) {
				string f = linespl.Remove(0, st.Count());
				string[] spl = f.RemovePunctuation().Split(' '),
						 eqp = {"is","was","has","had","like","likes","liked", "are"},
						 aw = {"sang", "wrote", "said", "remebered", "would"};
				if(eqp.Any(x => spl.FirstIndexOf(x) != -1)) {
					int ind = spl.FirstIndexOf(eqp.FirstOrDefault(x => spl.Any(y => x == y)));
					string vrn = spl.RemoveToEnd(ind).BetterToString(" ");
					#region Do Rules
					if((vrn.StartsWith("\"") || vrn.EndsWith("\"")) || vrn.StartEndsWith("\"") ||
					   (vrn.StartsWith("true") || vrn.Split(' ')[0].All(x => char.IsDigit(x))) ||
					   (vrn.Contains("Dear") || vrn.Contains("I learned")) ||
					   (aw.Any(x => vrn.Contains("I " + x))))
						Error("Variable name must not start with and/or contain a Unicode involved in making literals", linespl);
					#endregion
					if(!gstor.ContainsKey(vrn)) {
						string[] str = {"phrase","name","word"},
						min = {"number"},
						bol = {"correct","right","true","yes","false","incorrect","no","wrong"};
						//
						object val = "";
						string ab = "";
						if(spl[ind+1] == "a") {
							if(str.Any(x => x == spl[ind+2]))
								ab = "string";
							else if(min.Any(x => x == spl[ind+2]))
								ab = "number";
							else if(bol.Any(x => x == spl[ind+2]))
								ab = "bool";
						} else {
							string lm = spl[ind+1];
							if(lm == "the")
								lm = spl[ind+2];
							if(str.Any(x => x == lm))
								ab = "string";
							else if(min.Any(x => x == lm))
								ab = "number";
							else if(bol.Any(x => x == spl[ind+1])) // Special for some reason :thonking:
								ab = "bool";
							else if(func.Keys.Any(x => x == lm))
								ab = "function";
							else if(gstor.Keys.Any(x => x == lm))
								ab = "variable";
							switch(ab) {
									case "bool": {
										string[] tr = "correct right true yes".Split(' ');
										string[] fl = "false incorrect no wrong".Split(' ');
										if(tr.Contains(lm))
											val = "true";
										else if(fl.Contains(lm))
											val = "false";
										else
											Error("Invalid or unhandled boolean", linespl);
									}
									break;
									case "string": {
										val = spl.RemoveStartTo(ind+2).BetterToString(" "); // Get value
										if(!val.ToString().StartEndsWith("\""))
											Error("Invalid string", linespl);
									}
									break;
									case "number": {
										val = spl.RemoveStartTo(ind+2).BetterToString(" "); // Get value
										int abb = 0;
										if(val.ToString().Any(x => char.IsLetter(x)) || !int.TryParse(val.ToString(), out abb))
											Error("Invalid number", linespl);
									}
									break;
									case "function": {
										if(MethodGetReturnType(lm) == "null")
											Error("Method return type is null", linespl);
										//val = RunMethod(func[lm], "return");
									}
									break;
									case "variable": {
										string ky = gstor.Keys.First(x => x == lm);
										val = gstor[ky].Value;
										ab = gstor[ky].type;
									}
									break;
							}
						}
						gstor.Add(vrn, new gstcl(val, ab));
					} else {
						Error("Invalid Variable Value Change", linespl);
					}
				} else {
					Error("Can\'t check where to detect variable", linespl);
				}
			}
		}
		public static void TryReplaceVariable(string linespl) {
			// TODO: FIX BUG WHERE HAVING 'IS' MAKES 'IS NOW' 'IS' (maybe just ignore this one)
			string[] spl = linespl.RemovePunctuation().Split(' ');
			string[] dai = {"became","becomes","is now","now likes","now like","are now"};
			string fl = dai.FirstOrDefault(x => linespl.Contains(x));
			string var = "";
			string val = "";
			int ind = 0, ind2 = 0;
			for(int a = 0; a < spl.Count() - 1; a++) {
				if(new [] {dai[0], dai[1],}.Any(x => x == spl[a])) {
					ind = a;
					ind2 = a;
					break;
				}
				if(new [] {dai[2], dai[3], dai[4], dai[5]}.Any(x => (spl[a] + " " + spl[a + 1]) == x)) {
					ind = a;
					ind2 = a + 1;
					break;
				}
				if(a == spl.Count() - 1)
					Error("Error on trying to replace variable", linespl);
			}
			var = spl.RemoveToEnd(ind).BetterToString(" ");
			val = spl.RemoveStartTo(ind2).BetterToString(" ");
			if(gstor.ContainsKey(var)) {
				string[] str = {"phrase","name"},
				min = {"number"},
				bol = {"correct","right","true","yes","false","incorrect","no","wrong"};
				//
				if(GetType(val) == gstor[var].type || val == "nothing") {
					gstor[var].Value = val == "nothing" ? "" : val ;
				} else {
					Error("New value is not the same type as the old one", linespl);
				}
			} else {
				Error("Global Variable doesn't exist", linespl);
			}
		}
		public static void TryAddSubNumber(string linespl) {
			string ty = linespl.EndsWith("got one more!") ? "increment" : "decrement" ;
			int tr = linespl.EndsWith("got one more!") ? "got one more!".Length : "got one less!".Length;
			string vr = linespl.Substring(0, linespl.Length - tr);
			if(gstor.ContainsKey(vr)) {
				if(gstor[vr].type != "number")
					Error("Tried to " + ty + " a non-number variable", linespl);
				int m = ty == "increment" ? 1 : -1 ;
				gstor[vr].Value = (int.Parse(gstor[vr].Value.ToString()) + 1).ToString();
			} else
				Error("Tried to " + ty + " a non-existent variable", linespl);
		}
		public static void TryIfElse(string startLine, string[] lines, string[] lWP) {
			int tabs = 0;
			// If/When X [variable] is equal to Y [anything/variable]
			// If/When X [variable] is equal to Y [anything/variable] and ... (and ...)
			List<int> otherwise = new List<int>();
			for(int i=0;i<lWP.Count();i++) {
				if(lWP[i].StartsWith("Otherwise")) {
					otherwise.Add(i);
				}
			}
			bool elseNVM = false;
			if(otherwise.Count() == 1 && lWP.Contains("Otherwise:"))
				elseNVM = true;
			string[] conditions = {"is more than","had more than","is less than","had less than", "is greater or equal to", "is lesser or equal to", "is equal to", "isn\'t equal to", "is", "is not", "isn\'t", "wasn\'t", "was"};
			int conditionFails = 0;
			foreach(string line in startLine.Split(new string[] {"and"}, StringSplitOptions.None)) {
				string cond = conditions[conditions.FirstIndexOf(conditions.Where(x => startLine.Contains(x)).FirstOrDefault())];
				string[] a = line.Split(new string[] {cond}, StringSplitOptions.None);
				if(a.Count() != 2)
					Error("Condition error", line);
				if(!DoCondition(a[0].TrimStartEnd(" "), a[1].TrimStartEnd(" "), cond, startLine))
					conditionFails++;
			}
			if(conditionFails == 0) {
				tabs += 1;
				int stop = lines.Count()-1;
				if(otherwise.Count() > 0)
					stop = otherwise[0];
				for(int i=0;i<stop;i++) {
					string line = lines[i].Remove(0,4*tabs);
					#region TryWrite
					string[] tryWriteTrial = {"sang", "wrote", "said"};
					if(tryWriteTrial.Any(x => line.StartsWith("I " + x)))
						TryWrite(line);
					#endregion
					#region TryMakeVariable
					if(line.StartsWith("Did you know that "))
						TryMakeVariable(line);
					#endregion
					#region TryReplaceVariable
					foreach(string key in gstor.Keys)
						if(new string[] {"became","is now","are now","now likes","now like","becomes"}.Any(x => line.StartsWith(key + " " + x)))
							TryReplaceVariable(line);
					#endregion
					#region TryIncrementNumber
					if(line.EndsWith("got one more") || line.EndsWith("got one less"))
						TryAddSubNumber(line);
					#endregion
					#region TryIfElse
					if(line.StartsWith("If") || line.StartsWith("When")) {
						//Get all lines of the if-else
						List<string> ie = new List<string>();
						List<string> iepunc = new List<string>();
						string st = line.Split(' ').Skip(1).ToArray().BetterToString(" ");
						foreach(string li in lines.Skip(i)) {
							if(li == "That's what I did.")
								break;
							ie.Add(li);
						}
						foreach(string li in lWP.Skip(i)) {
							if(li == "That's what I did.")
								break;
							iepunc.Add(li);
						}
						TryIfElse(st,ie.ToArray(),iepunc.ToArray());
					}
					#endregion
				}
			} else {
				if(otherwise.Count() > 0) {
					if(!elseNVM) {
					
					} else {
						tabs += 1;
						for(int i=otherwise[0]+1;i<lines.Count()-1;i++) {
							string line = lines[i].Remove(0,4*tabs);
							#region TryWrite
							string[] tryWriteTrial = {"sang", "wrote", "said"};
							if(tryWriteTrial.Any(x => line.StartsWith("I " + x)))
								TryWrite(line);
							#endregion
							#region TryMakeVariable
							if(line.StartsWith("Did you know that "))
								TryMakeVariable(line);
							#endregion
							#region TryReplaceVariable
							foreach(string key in gstor.Keys)
								if(new string[] {"became","is now","are now","now likes","now like","becomes"}.Any(x => line.StartsWith(key + " " + x)))
									TryReplaceVariable(line);
							#endregion
							#region TryIncrementNumber
							if(line.EndsWith("got one more") || line.EndsWith("got one less"))
								TryAddSubNumber(line);
							#endregion
						}
					}
				}
			}
		}
		public static void TryInput(string linespl) {
			// I ASKED X "Y" (forced string)
			string[] spl = linespl.Split(' ').Skip(2).ToArray();
			string var = "";
			string que = "";
			bool isQ =  false;
			foreach(string s in spl) {
				if(s.StartsWith("\""))
					isQ = true;
				if(!isQ)
					var += s + " ";
				else
					que += s + " ";
			}
			var = var.TrimStartEnd(" ");
			que = que.TrimStartEnd(" ").TrimStartEnd("\"");
			//
			if(gstor.ContainsKey(var)) {
				if(gstor[var].type != "string")
					Error("Tried replacing a variable that isn't taking string as a value!", linespl);
			} else {
				gstor.Add(var, new gstcl("","string"));
			}
			//
			Console.WriteLine(que);
			Console.Write("<< ");
			string res = Console.ReadLine();
			if(!res.StartsWith("\""))
				res = "\"" + res;
			if(!res.EndsWith("\""))
				res += "\"";
			gstor[var].Value = res;
		}
		#endregion
		public static bool DoCondition(string x, string y, string cond, string line) {
			string xt = GetType(x);
			string yt = GetType(y);
			object xv = GetValue(x, xt);
			object yv = GetValue(y, yt);
			if(xt.StartsWith("variable"))
				xt = xt.Remove(0,8);
			if(yt.StartsWith("variable"))
				yt = yt.Remove(0,8);
			if(xt == "null" || yt == "null")
				Error("Tried to do a condition while a variable is null",line);
			if(xt.StartsWith("method") || yt.StartsWith("method"))
				Error("Tried to do a condition on methods",line); // PLACEHOLDER, WE WILL ALLOW THIS IN THE FUTURE
			if(xt != yt)
				Error("Condition on two different types, {0} and {1}".MelodyFormat(xt,yt),line);
			switch(cond) {
				#region X == Y
					case "is":
					case "was":
					case "is equal to": {
						if(xt == "bool")
							return GetBool(xv.ToString()) == GetBool(yv.ToString());
						if(xt == "string")
							return xv.ToString().TrimStartEnd(" ") == yv.ToString().TrimStartEnd(" ");
						if(xt == "number")
							return int.Parse(xv.ToString()) == int.Parse(yv.ToString());
						Error("[CODE] ERROR 0X01"); // how do you even h
					}
					break;
				#endregion
				#region X != Y
					case "isn't":
					case "wasn't":
					case "isn't equal to": {
						if(xt == "bool")
							return GetBool(xv.ToString()) != GetBool(yv.ToString());
						if(xt == "string")
							return xv.ToString().TrimStartEnd(" ") != yv.ToString().TrimStartEnd(" ");
						if(xt == "number")
							return int.Parse(xv.ToString()) != int.Parse(yv.ToString());
						Error("[CODE] ERROR 0X01"); // how do you even h
					}
					break;
				#endregion
				#region X > Y and X < Y
					case "is more than":
					case "had more than": {
						if(xt == "number")
							return int.Parse(xv.ToString()) > int.Parse(yv.ToString());
						Error("Tried to do a number condition with a non-number varible",cond);
					}
					break;
				//
					case "is less than":
					case "had less than": {
						if(xt == "number")
							return int.Parse(xv.ToString()) < int.Parse(yv.ToString());
						Error("Tried to do a number condition with a non-number varible",cond);
					}
					break;
				#endregion
				#region X >= Y and X <= Y
					case "is greater or equal to": {
						if(xt == "number")
							return int.Parse(xv.ToString()) >= int.Parse(yv.ToString());
						Error("Tried to do a number condition with a non-number varible",cond);
					}
					break;
				//
					case "is less or equal to": {
						if(xt == "number")
							return int.Parse(xv.ToString()) <= int.Parse(yv.ToString());
						Error("Tried to do a number condition with a non-number varible",cond);
					}
					break;
				#endregion
			}
			Error("Condition failed (Either condition doesn't exist/isn't supported or the code breaking. Classified as ERROR 0X02)",cond);
			return false;
		}
		public static string GetType(string val) {
			int a = -1;
			if(int.TryParse(val, out a))
				return "number";
			string[] b = {"correct","right","true","yes"};
			string[] c = {"false","incorrect","no","wrong"};
			if(b.Any(x => val == x))
				return "boolCorrect";
			if(c.Any(x => val == x))
				return "boolIncorrect";
			if(val.StartEndsWith("\""))
				return "string";
			if(func.Keys.Contains(val))
				return "method" + func[val].type;
			if(gstor.Keys.Contains(val))
				return "variable" + gstor[val].type;
			return "null";
		}
		public static string GetType2(string val) {
			int a = -1;
			if(int.TryParse(val, out a))
				return "number";
			string[] b = {"correct","right","true","yes"};
			string[] c = {"false","incorrect","no","wrong"};
			if(b.Any(x => val == x))
				return "boolCorrect";
			if(c.Any(x => val == x))
				return "boolIncorrect";
			if(val.StartEndsWith("\""))
				return "string";
			if(func.Keys.Contains(val))
				return func[val].type;
			if(gstor.Keys.Contains(val))
				return  gstor[val].type;
			return "null";
		}
		public static object GetValue(string val, string type) {
			int a = -1;
			if(int.TryParse(val, out a))
				return a;
			if(type == "boolCorrect")
				return 1>0;
			if(type == "boolIncorrect")
				return 1<0;
			if(type == "string")
				return val.TrimStartEnd("\"");
			if(type.StartsWith("method"))
				//return "method" + func[val].type;
				return "null";
			if(type.StartsWith("variable")) {
				if(type.Remove(0,8) == "string")
					return gstor[val].Value.ToString().TrimStartEnd("\"");
				return gstor[val].Value;
			}
			return "null";
		}
		public static bool GetBool(string val) {
			string[] c = {"correct","right","true","yes"};
			string[] f = {"false","incorrect","no","wrong"};
			if(c.Any(x => x == val))
				return true;
			if(f.Any(x => x == val))
				return false;
			Error("[CODE] Tried to get invalid/unsupported bool.");
			return false;
		}
		public static string MethodGetReturnType(string val) {
			int ind = -1;
			for(int a = 0; a < val.Split(' ').Count() - 2; a++) {
				string fr = val.Split(' ')[a];
				string sc = val.Split(' ')[a + 1];
				string th = val.Split(' ')[a + 2];
				if((fr + " " + sc + " " + th) == "to get a") {
					ind = a + 3;
					break;
				}
			}
			if(ind == -1)
				Error("METHOD RETURN TYPE PARSING ERROR.");
			return GetType(val.Split(' ')[ind]);
		}
		#region Comment
		public static bool isComment(string line) {
			// 1 p.s.
			if(line.StartsWith("P.S.",StringComparison.CurrentCulture))
				return true;
			// multiple p.
			for(int a = 0; a <= line.Length-4; a += 4) {
				string l1 = line[a].ToString();
				string l2 = line[a + 1].ToString();
				string l3 = line[a + 2].ToString();
				string l4 = line[a + 3].ToString();
				if(line.Split(' ')[0].EndsWith("P.S.", StringComparison.CurrentCulture))
					return true;
			}
			return false;
		}
		public static string RemoveComment(string line) {
			if(isComment(line))
				return "null";
			//
			string bl = "";
			bool onComment = false;
			for(int ba = 0; ba < line.Count(); ba++) {
				if(line[ba] == '(')
					onComment = true;
				else if(ba > 0 && line[ba-1] == ')' && onComment)
					onComment = false;
				if(!onComment)
					bl += line[ba];
			}
			return bl;
		}
		#endregion
		#region Functions
		public static void Error(string msg) {
			//Console.Clear();
			Console.WriteLine("Error! " + msg + ". Press any key to exit the program.");
			Console.ReadKey();
			Environment.Exit(0);
		}
		public static void Error(string msg, string code) {
			//Console.Clear();
			// GET FUNCTION HEAD
			string funcHead = codeLines.Where(x => x.Contains(code)).First();
			string[] cL = codeLines;
			for(int i = 0; i < cL.Count(); i++)
				cL[i] = cL[i].RemovePunctuation();
			for(int i = cL.FirstIndexOf(code); i >= 0; i--) {
				if(codeLines[i].StartsWith("Today I learned") || codeLines[i].StartsWith("I learned")) {
					funcHead = codeLines[i];
					break;
				}
			}
			Console.WriteLine("Error! " + msg + " at function \"" + funcHead + "\". The line has been written below. Press any key to exit the program.");
			Console.WriteLine();
			Console.WriteLine(code);
			Console.ReadKey();
			Environment.Exit(0);
		}
		#endregion
		#region Storage
		// Global objects
		public static Dictionary<string, gstcl> gstor = new Dictionary<string, gstcl>();
		public class gstcl {
			public object Value { get; set; }
			public string type { get; set; }
			public gstcl(object Value, string type) {
				this.Value = Value;
				this.type = type;
			}
		}
		// Functions
		public static Dictionary<string, fucl> func = new Dictionary<string, fucl>();
		public class fucl {
			public string[] lines { get; set; }
			public string type { get; set; }
			public int linestart { get; set; }
			public string title { get; set; }
			public fucl(string[] lines, string type, int st, string title) {
				this.lines = lines;
				this.type = type;
				this.linestart = st;
				this.title = title;
			}
		}
		public static string[] mainmethod = {};
		//
		public static string ProgramTitle = "";
		public static string Author = "";
		public static string[] codeLines = new string[0];
		public static List<string> punctuations = new List<string>() {".","!","?","...",":"};
		#endregion
		#endregion
	}
}