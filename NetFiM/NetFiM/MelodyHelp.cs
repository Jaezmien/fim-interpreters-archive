/*
 * pls no steal :(((( don mek cow sad
*/
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Just some commands that I'm too lazy to make every time (only some of these though :/)
/// </summary>
namespace Melody.Helper
{
	public static class ExtendedMethods {
		private static Random rng = new Random(); // Thanks for pointing this out Atilla
		/// <summary>
		/// Clenches the number to the minimum and maximum number
		/// </summary>
		public static int Clench(this int value, int min, int max) {
			if(value < min)
				return min;
			if(value > max)
				return max;
			return value;
		}
		/// <summary>
		/// Loops the number so that it's value represents it's value if it followed the minimum and maximum rule
		/// </summary>
		public static int Loop(this int value, int min, int max) {
			int ret = value;
			if(ret < min)
				while(ret < min)
					ret += min;
			else if(ret > max)
				while(ret > max)
					ret -= max;
			return ret;
		}
		/// <summary>
		/// Randomizes the list using the Fisher-Yates method
		/// </summary>
		public static List<T> Shuffle<T>(this List<T> list) {
			int n = list.Count;  
			
		    while (n-- > 1)
		    {
		    	int k = rng.Next(n + 1);
		        T value = list[k];  
		        list[k] = list[n];  
		        list[n] = value;  
		    }  
		    return list;
		}
		/// <summary>
		/// Randomizes the array using the Fisher-Yates method
		/// </summary>
		public static T[] Shuffle<T>(this T[] array) {
			int n = array.Count();
		    while (n-- > 1)
		    {
		    	int k = rng.Next(n + 1);
		        T value = array[k];  
		        array[k] = array[n];  
		        array[n] = value;  
		    }  
		    return array;
		}
		/// <summary>
		/// Gets a random element from the array
		/// </summary>
		public static T GetRandomElement<T>(this T[] array) {
			return array[rng.Next(0, array.Count()-1)];
		}
		/// <summary>
		/// Gets a random element from the list
		/// </summary>
		public static T GetRandomElement<T>(this List<T> list) {
			return list[rng.Next(0, list.Count()-1)];
		}
		public static bool isDivisibleBy(this int divident, int divisor) {
			if(divident % divisor == 0)
				return true;
			return false;
		}
		public static string TrimStartEnd(this string a, string st, string en, bool isOnce = false) {
			string res = a;
			if(isOnce) {
				if(res.StartsWith(st))
					res = res.Remove(0,st.Count());
				if(res.EndsWith(en))
					res = res.Substring(0, res.Count() - en.Count());
			} else {
				while(res.StartsWith(st))
					res = res.Remove(0,st.Length);
				while(res.EndsWith(en))
					res = res.Substring(0, res.Count() - en.Count());
			}
			return res;
		}
		public static string TrimStartEnd(this string a, string ch, bool isOnce = false) {
			string res = a;
			if(isOnce) {
				if(res.StartsWith(ch))
					res = res.Remove(0,ch.Count());
				if(res.EndsWith(ch))
					res = res.Substring(0, res.Count() - ch.Count());
			} else {
				while(res.StartsWith(ch))
					res = res.Remove(0,ch.Count());
				while(res.EndsWith(ch))
					res = res.Substring(0, res.Count() - ch.Count());
			}
			return res;
		}
		public static string TrimStartEnd(this string a, char st, char en) {
			return a.TrimStart(st).TrimEnd(en);
		}
		public static string TrimStartEnd(this string a, char ch) {
			return a.TrimStart(ch).TrimEnd(ch);
		}
		/// <summary>
		/// Converts a string hex number to int(I usually use this on Discord Bots)
		/// </summary>
		public static int HexToDec(this string hex) {
			if(hex.StartsWith("0x"))
				return Convert.ToInt32(hex.Remove(0,2), 16);
			return Convert.ToInt32(hex, 16);
		}
		/// <summary>
		/// Converts an int decimal to hex (I usually use this on Discord Bots)
		/// </summary>
		public static string DecToHex(this int dec) {
			return dec.ToString("X");
		}
		public static string[] RemoveAt(this string[] array, int index) {
			if(index < 0 || index >= array.Count())
				return array;
			return array.Where(x => x != array[index]).ToArray();
		}
		public static string[] RemoveToEnd(this string[] array, int index) {
			if(index < 0 || index >= array.Count())
				return array;
			List<string> al = new List<string>();
			for(int ab = 0; ab < array.Count(); ab++)
				if(ab < index)
					al.Add(array[ab]);
			return al.ToArray();
		}
		public static bool StartEndsWith(this string val, string ch) {
			if(val.StartsWith(ch) && val.EndsWith(ch))
				return true;
			return false;
		}
		public static string[] RemoveStartTo(this string[] array, int index) {
			if(index < 0 || index >= array.Count())
				return array;
			List<string> al = new List<string>();
			for(int ab = 0; ab < array.Count(); ab++)
				if(ab > index)
					al.Add(array[ab]);
			return al.ToArray();
		}
		public static string[] RemoveIn(this string[] array, int from, int into) {
			List<string> al = new List<string>();
			for(int ab = 0; ab < array.Count(); ab++)
				if(!(ab >= from && ab <= into))
					al.Add(array[ab]);
			return al.ToArray();
		}
		public static int FirstIndexOf(this string[] array, string word) {
			for(int a = 0; a < array.Count(); a++)
				if(array[a] == word)
					return a;
			return -1;
		}
		public static string[] Remove(this string[] array, string word, bool firstOnly = false) {
			if(!array.Contains(word))
				return array;
			if(!firstOnly)
				return array.RemoveAt(array.FirstIndexOf(word)).ToArray();
			string remob = array.First(x => x == word);
			return array.RemoveAt(array.FirstIndexOf(remob));
		}
		public static string BetterToString(this string[] array, string seperator = "") {
			string st = "";
			foreach(string b in array)
				st += b + seperator;
			st = st.Substring(0, st.Length-seperator.Length);
			return st;
		}
		/// <summary>
		/// WARNING: Will return null on equals
		/// </summary>
		public static bool? AverageBoolVote(this bool[] votes) {
			bool[] ye = votes.Where(x => x).ToArray();
			bool[] ne = votes.Where(x => !x).ToArray();
			if(ye.Count() < ne.Count())
				return false;
			if(ye.Count() > ne.Count())
				return true;
			return null;
		}
		public static string BetterToString(this List<string> list, string seperator = "") {
			string st = "";
			foreach(string b in list)
				st += b + seperator;
			st = st.Substring(0, st.Length-seperator.Length);
			return st;
		}
		public static int AddElements(this int[] array) {
			int v = 0;
			foreach(int b in array)
				v += b;
			return v;
		}
		public static string[] SplitOn(this string stronk, int count) {
			if(count == 0)
				return new  [] {stronk};
			List<string> a = new List<string>();
			string s = stronk;
			int k = 0;
			while(true) {
				if(s.Length > count) {
					a.Add(s.Substring(0, count));
					s = s.Remove(0, count);
					k++;
				} else {
					a.Add(s);
					break;
				}
			}
			return a.ToArray();
		}
		/// <summary>
		/// (NOTE: THE CHAR THAT HAS THE MODULO WILL NOT BE INCLUDED ON THE TEMP STRING)
		/// </summary>
		public static string[] SplitBy(this string input, int mod) {
			List<string> a = new List<string>();
			string t = "";
			for(int i = 0; i < input.Length; i++) {
				if(i % mod == 0) {
					a.Add(t);
					t = "";
				}
				t += input[i];
			}
			if(t != "")
				a.Add(t);
			if(a[0] == "")
				a.RemoveAt(0);
			return a.ToArray();
		}
		// Will keep the int above/on zero
		public static int ClenchUnNegative(this int a) {
			int mu = 1;
			if(a < 0)
				mu = -1;
			return a * mu;
		}
		// Will keep the int below/on zero
		public static int ClenchNegative(this int a) {
			int mu = -1;
			if(a < 0)
				mu = 1;
			return a * mu;
		}
		// Just a shortcut for debugs
		public static void WriteLine<T>(this T a) {
			Console.WriteLine(a);
		}
		public static void Write<T>(this T a) {
			Console.Write(a);
		}
		//
		public static bool isInAny(this string a, object[] b, bool toLower = true) {
			foreach(object d in b) {
				if(toLower) {
					if(a.ToLower() == d.ToString().ToLower())
						return true;
				} else {
					if(a == d.ToString())
						return true;
				}
			}
			return false;
		}
		//
		public static int BetterIndexOfArray(this string[] a, string b) {
			for(int c = 0; c < a.Count(); c++)
				if(a[c] == b)
					return c;
			return -1;
		}
		public static string Cover(this string a, string b) {
			return b + a + b;
		}
		public static int ToInt(this string a) {
			return int.Parse(a);
		}
		// Because string.Format only allows ~3 parameters. And I'm using C# 5.0
		public static string MelodyFormat(this string str, params object[] par) {
			string s = str;
			for(int i = 0; i < par.Count(); i++) {
				if(par[i] != null) {
					s = s.Replace("{"+i+"}", par[i].ToString());
				} else {
					"MELODYFORMAT-ERROR-PAR_HAS_NULL".WriteLine();
					return str;
				}
			}
			return s;
		}
		public static List<T> AddMany<T>(this List<T> list, params T[] param) {
			List<T> temp = list;
			foreach(T a in param)
				temp.Add(a);
			return temp;
		}
		public static T[] AddMany<T>(this T[] array, params T[] param) {
			List<T> temp = array.ToList();
			foreach(T a in param)
				temp.Add(a);
			return temp.ToArray();
		}
		public static T[] DirectAdd<T>(this T[] array, T item) {
			List<T> a = array.ToList();
			a.Add(item);
			return a.ToArray();
		}
		public static List<T> RemoveFirst<T>(this List<T> list) {
			if(list.Count() == 0 && list.Count() == 1)
				return new List<T>();
			var rem = list[0];
			return list.Where(x => !x.Equals(rem)).ToList();
		}
		public static bool AllSame(this string[] array, string key = null) {
			if(array.Count() == 0 || array.Count() == 1)
				return true;
			string a = key != null ? key : array[0];
			if(array.Where(b => a != b).Count() >= 1)
				return false;
			return true;
		}
		public static bool Reverse(this bool a) {
			return !a;
		}
		/// <returns>The first key it gets</returns>
		public static T GetKeyFromValue<T>(this Dictionary<T, T> dictionary, T value) {
			return dictionary.Keys.Where(x => dictionary[x].Equals(value)).FirstOrDefault();
		}
		/// <returns>Array of keys it gets</returns>
		public static T[] GetKeysFromValue<T>(this Dictionary<T, T> dictionary, T value) {
			return dictionary.Keys.Where(x => dictionary[x].Equals(value)).ToArray();
		}
		public static string CapitalFirst(this string a) {
			return a[0].ToString().ToUpper() + a.Remove(0,1);
		}
		public static List<T> RemoveSimilar<T>(this List<T> list) {
			List<T> temp = new List<T>();
			foreach(T a in list)
				if(!temp.Contains(a))
					temp.Add(a);
			return temp;
		}
		public static List<string> ToStringArray(this string text) {
			List<string> l = new List<string>();
			for(int i = 0; i < text.Count(); i++) {
				l.Add(text[i].ToString());
			}
			return l;
		}
        // STACKOVERFLOW - 11743160
        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }
        public static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }
        public static int parseWord(this string number)
		{
			string[] words = number.TrimStartEnd(" ").ToLower().Split(new char[] {' ', '-', ','}, StringSplitOptions.RemoveEmptyEntries);
		    string[] ones = {"one", "two", "three", "four", "five", "six", "seven", "eight", "nine"};
		    string[] teens = {"eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen"};
		    string[] tens = {"ten", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety"};
		    Dictionary<string, int> modifiers = new Dictionary<string, int>() {
		        {"billion", 1000000000},
		        {"million", 1000000},
		        {"thousand", 1000},
		        {"hundred", 100}
		    };
		    if(number == "eleventy billion")
		    	return int.MaxValue;
		    int result = 0, currentResult = 0, lastModifier = 1;
		    foreach(string word in words)
		    {
		    	if(modifiers.ContainsKey(word)) {
		    		lastModifier *= modifiers[word];
		    	} else {
		    		int n;

		    		if(lastModifier > 1) {
		    			result += currentResult * lastModifier;
		    			lastModifier = 1;
		    			currentResult = 0;
		    		}

		    		if((n = Array.IndexOf(ones, word) + 1) > 0) {
		    			currentResult += n;
		    		} else if((n = Array.IndexOf(teens, word) + 1) > 0) {
		    			currentResult += n + 10;
		    		} else if((n = Array.IndexOf(tens, word) + 1) > 0) {
		    			currentResult += n * 10;
		    		} else if(word != "and") {
		    			throw new ApplicationException("Unrecognized word: " + word);
		    		}
		    	}
		    }
		    return result + currentResult * lastModifier;
		} // From: Ryan on StackOverflow
		public static Dictionary<T, T> ReverseDictionary<T>(this Dictionary<T, T> dictionary) {
			var d = new Dictionary<T, T>();
			foreach(T key in dictionary.Keys)
				d.Add(dictionary[key], key);
			return d;
		}
		public static string[] SplitWords(this string a) {
			List<string> b = new List<string>();
			for(int i = 0; i < a.Length; i++)
				b.Add(a[i].ToString());
			return b.ToArray();
		}
		public static bool BetterExists<T>(this List<T> list, T value) {
			if(list.Contains(value))
				return true;
			return false;
		}
		public static bool HasDuplicate<T>(this T[] array, bool includeZero = true) {
			int s = 0;
			int c = 0;
			while(c < array.Count()) {
				while(s < array.Count()) {
					if(c != s && array[s].Equals(array[c]) && (includeZero && s != 0)) {
						return true;
					}
					s++;
				}
				c++;
			}
			return false;
		}
		public static string[] ToLower(this string[] array) {
			List<string> a = new List<string>();
			foreach(string b in array)
				a.Add(b.ToLower());
			return a.ToArray();
		}
		public static List<string> ToLower(this List<string> list) {
			List<string> a = new List<string>();
			foreach(string b in list)
				a.Add(b.ToLower());
			return a;
		}
        public static int RemoveDecimal(this double x) {
        	return int.Parse(x.ToString().Split('.')[0]);
        }
        public static string[] RemoveFromEnd(this string[] a, int b = 1) {
        	List<string> c = new List<string>();
        	for(int i = 0; i < a.Count()-(b+1); i++) {
        		c.Add(a[i]);
        	}
        	return c.ToArray();
        }
        public static string[] Replace(this string[] a, string ba, string bb) {
        	string[] b = a;
        	for(int i = 0; i < b.Count(); i++)
        		b[i] = b[i].Replace(ba,bb);
        	return b;
        }
        public static string[] UnfuckAllQuotes(this string[] a) {
        	string[] b = a;
        	for(int i = 0; i < b.Count(); i++)
        		b[i] = b[i].Replace('“','"').Replace('”','"');
        	return b;
        }
        public static string UnfuckQuotes(this string a) {
        	return a.Replace('“','"').Replace('”','"');
        }
	}
	public class MelodyHelp
	{
		const int ___VERSION = 1;
		private static Random rng = new Random();
		public static void ForEachDetect<T>(T[] array, T key, Action<T> action) {
			foreach(T b in array)
				if(b.Equals(key))
					action(b);
		}
		public static void ForEachDetect<T>(List<T> array, T key, Action<T> action) {
			foreach(T b in array)
				if(b.Equals(key))
					action(b);
		}
		public static void ForEach<T>(List<T> array, Action<T> action) {
			foreach(T b in array)
				action(b);
		}
		public static void ForEach<T>(T[] array, Action<T> action) {
			foreach(T b in array)
				action(b);
		}
		public static string[] alphabet = {"a","b","c","d","e","f","g","h","i","j","k","l","m","n","o","p","q","r","s","t","u","v","w","x","y","z"};
		/// <param name="min">Must be lower than 100</param>
		/// <returns>Returns true if min is lower (or equals) to the random number</returns>
		public static bool RandomLowerThan(int min, bool trueonequals = true) {
			int ch = min >= 100 ? rng.Next(0, 50) : min ;
			int a = rng.Next(0, 100);
			if(trueonequals)
				if(ch <= a)
					return true;
			else
				if(ch < a)
					return true;
			return false;
		}
		/// <param name="max">Must be lower than 100</param>
		/// <returns>Returns true if max is higher (or equals) to the random number</returns>
		public static bool RandomHigherThan(int max, bool trueonequals = true) {
			int ch = max >= 100 ? rng.Next(0, 50) : max ;
			int a = rng.Next(0, 100);
			if(trueonequals)
				if(ch >= a)
					return true;
			else
				if(ch > a)
					return true;
			return false;
		}
		public static string Format(string str, params object[] par) {
			string s = str;
			for(int i = 0; i < par.Count(); i++) {
				s = s.Replace("{"+i+"}", par[i].ToString());
			}
			return s;
		}
		// I'm stupid, doing this :333
		public static int GetRandomInt(int min = 0, int max = 1) {
			return rng.Next(min, max);
		}
		public static double GetRandomDouble(double min = 0.0, double max = 1.0) {
			return rng.NextDouble() * (max - min) + min;
		}
	}
}
