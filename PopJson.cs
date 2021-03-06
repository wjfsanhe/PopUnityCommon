using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;


namespace PopX
{
	public class Json
	{
		public const string	NamePattern = "([\"]{1}[A-Za-z0-9]+[\"]{1})";
		public const string	SemiColonPattern = "([\\s]*):([\\s]*)";
		//	https://stackoverflow.com/a/32155765/355753
		public const string	ValueStringPattern = "([\\s]*)\"([^\\\\\"]*)\"([\\s]*)";
		public const string ValueBoolPattern = "([\\s]*)(true|false){1}([\\s]*)";

		public const string WhiteSpaceCharacters = " \n\r\t\b\f";
		public const string ValidAsciiCharacters = "\"\\/";

		public static string GetNamePattern(string Name)
		{
			return "([\"]{1}" + Name + "[\"]{1})";
		}

		public static bool IsValidJsonAscii(char Char)
		{
			//	https://www.json.org/
			//	gr: this tests unicode, which is supposed to be valid, but for binary-testing purposes it's letting through 0xff and stuff. no good
			//if (System.Char.IsLetter(Char))
			//	return true;
			if (IsWhiteSpace(Char))
				return true;
			if (Char >= 'A' && Char <= 'Z')
				return true;
			if (Char >= 'a' && Char <= 'z')
				return true;
			if (Char >= '0' && Char <= '9')
				return true;
			var MatchIndex = ValidAsciiCharacters.IndexOf(Char);
			if (MatchIndex != -1)
				return true;

			//Debug.Log("Non json char: " + Char);
			return false;
		}


		public static bool IsWhiteSpace(char Char)
		{
			var MatchIndex = WhiteSpaceCharacters.IndexOf(Char);
			return MatchIndex != -1;
		}

		//	works on byte[] and string, but can't do a generics with that in c#, so accessor for now. 
		//	Which is probably ultra slow
		static public int GetJsonLength(System.Func<int, char> GetChar)
		{
			var Index = 0;

			while (IsJsonWhitespace (GetChar (Index))) {
				Index++;
			}

			//	pull json off the front
			if (GetChar(Index) != '{')
				throw new System.Exception("Data is not json. Starts["+Index+"] with " + (char)GetChar(Index));
			Index++;

			var OpeningBrace = '{';
			var ClosingBrace = '}';
			int BraceCount = 1;
			while (BraceCount > 0)
			{
				try
				{
					var Char = GetChar(Index);
					if (Char == OpeningBrace)
						BraceCount++;
					if (Char == ClosingBrace)
						BraceCount--;
					Index++;
				}
				catch //	OOB
				{
					throw new System.Exception("Json braces not balanced");
				}
			}
			return Index;
		}

		static public int GetJsonLength(string Data,int StartPos=0)
		{
			return GetJsonLength((i) => { return Data[i+StartPos]; });
		}

		static public int GetJsonLength(byte[] Data,int StartPos=0)
		{
			return GetJsonLength((i) => { return (char)Data[i+StartPos]; });
		}

		static void EscapeChar(ref char[] CharPair)
		{
			switch(CharPair[0])
			{
			case '\n':	CharPair[0] = '\\';	CharPair[1] = 'n';	break;
			case '\t':	CharPair[0] = '\\';	CharPair[1] = 't';	break;
			case '\r':	CharPair[0] = '\\';	CharPair[1] = 'r';	break;
			case '\\':	CharPair[0] = '\\';	CharPair[1] = '\\';	break;
			}
		}

		static public string EscapeValue(string Unescaped,bool WrapInQuotes=true)
		{
			var Escaped = WrapInQuotes ? "\"" : "";

			var CharPair = new char[2];
			var NullChar = '\0';
			foreach (var Char in Unescaped) {
				CharPair [0] = Char;
				CharPair [1] = NullChar;
				EscapeChar (ref CharPair);
				Escaped += CharPair [0];
				if (CharPair [1] != NullChar)
					Escaped += CharPair [1];
			}

			if (WrapInQuotes)
				Escaped += "\"";
			return Escaped;				
		}

		static public string EscapeValue(bool Value)
		{
			return Value ? "true" : "false";
		}

		static public void	Replace(ref string Json,string Key,string Value)
		{
			var StringElementPattern = GetNamePattern (Key) + PopX.Json.SemiColonPattern + ValueStringPattern;
			var RegExpression = new Regex (StringElementPattern);

			//	todo: json escaping!
			var Replacement = '"' + Key + '"' + ':' + EscapeValue(Value);

			//	harder to debug, but simpler implementation
			Json = RegExpression.Replace (Json, Replacement);
		}


		static public void Replace(ref string Json, string Key,bool Value)
		{
			var StringElementPattern = GetNamePattern(Key) + PopX.Json.SemiColonPattern + ValueBoolPattern;
			var RegExpression = new Regex(StringElementPattern);

			//	todo: json escaping!
			var Replacement = '"' + Key + '"' + ':' + EscapeValue(Value);

			//	harder to debug, but simpler implementation
			Json = RegExpression.Replace(Json, Replacement);
		}

		static bool IsJsonWhitespace(char Char)
		{
			switch(Char)
			{
			case '\n':
			case ' ':
			case '\r':
			case '\t':
				return true;
			default:
				return false;
			}
		}

		static public void	Append(ref string Json,string Key,string Value)
		{
			//	construct what we're injecting
			var NewContent = '"' + Key + '"' + ':' + EscapeValue(Value);

			//	find end brace
			var LastBrace = Json.LastIndexOf('}');
			if (LastBrace < 0)
				throw new System.Exception ("Missing end brace of json");

			//	look for any json content between end of start brace
			//	need to cope with comments
			var HasJsonContent = false;
			for (int i = LastBrace - 1;	i >= 0;	i--) {
				var Char = Json [i];
				if (IsJsonWhitespace (Char))
					continue;
				if (Char == '{')
					break;
				HasJsonContent = true;
			}

			if (HasJsonContent)
				NewContent = ",\n" + NewContent;
			NewContent += '\n';

			Json = Json.Insert (LastBrace, NewContent);
		}
	}



}
