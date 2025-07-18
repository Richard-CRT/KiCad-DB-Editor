using KiCad_DB_Editor.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace KiCad_DB_Editor.Utilities
{
    public partial class SExpressionToken
    {
        public enum SExpTokenParsingState
        {
            ScanningForOpeningToken,
            ParsingTokenName,
            ScanningForTokenSubObject,
            ParsingTokenAttribute,
            ParsingTokenSubToken,
            FinishedParsingToken,
        }

        public string Name { get; set; }
        public List<string> Attributes { get; set; }
        public List<SExpressionToken> SubTokens { get; set; }

        public SExpressionToken(string sExpressionString, int scanStart = -1, int scanEnd = -1)
        {
            Name = "";
            Attributes = new();
            SubTokens = new();

            string nameInProgress = "";
            string tokenAttributeInProgress = "";
            int startIndexOfTokenSubToken = -1;
            int tokenSubTokenDepth = -1;

            if (scanStart == -1)
                scanStart = 0;
            if (scanEnd == -1)
                scanEnd = sExpressionString.Length - 1;

            bool inString = false;
            int numConsecutiveBackslashes = 0;

            SExpTokenParsingState state = SExpTokenParsingState.ScanningForOpeningToken;
            for (int i = scanStart; i <= scanEnd && state != SExpTokenParsingState.FinishedParsingToken; i++)
            {
                char c = sExpressionString[i];
                switch (state)
                {
                    case SExpTokenParsingState.ScanningForOpeningToken:
                        if (c == '(')
                            state = SExpTokenParsingState.ParsingTokenName;
                        break;
                    case SExpTokenParsingState.ParsingTokenName:
                        if (Char.IsWhiteSpace(c))
                        {
                            Name = nameInProgress;
                            state = SExpTokenParsingState.ScanningForTokenSubObject;
                        }
                        else if (c == ')')
                        {
                            Name = nameInProgress;
                            state = SExpTokenParsingState.FinishedParsingToken;
                        }
                        else
                            nameInProgress += c;
                        break;
                    case SExpTokenParsingState.ScanningForTokenSubObject:
                        if (!Char.IsWhiteSpace(c))
                        {
                            if (c == ')')
                                state = SExpTokenParsingState.FinishedParsingToken;
                            else if (c == '(')
                            {
                                state = SExpTokenParsingState.ParsingTokenSubToken;
                                startIndexOfTokenSubToken = i;
                                tokenSubTokenDepth = 1;
                            }
                            else if (c == '"')
                            {
                                state = SExpTokenParsingState.ParsingTokenAttribute;
                                tokenAttributeInProgress = "" + c;
                                inString = true;
                                numConsecutiveBackslashes = 0;
                            }
                            else
                            {
                                state = SExpTokenParsingState.ParsingTokenAttribute;
                                tokenAttributeInProgress = "" + c;
                            }
                        }
                        break;
                    case SExpTokenParsingState.ParsingTokenAttribute:
                        if (inString)
                        {
                            tokenAttributeInProgress += c;
                            if (c == '"' && (numConsecutiveBackslashes % 2) == 0)
                            {
                                inString = false;
                                Attributes.Add(tokenAttributeInProgress);
                                state = SExpTokenParsingState.ScanningForTokenSubObject;
                            }
                            else if (c == '\\')
                                numConsecutiveBackslashes++;
                            else
                                numConsecutiveBackslashes = 0;
                        }
                        else if (!Char.IsWhiteSpace(c))
                        {
                            if (c != ')')
                                tokenAttributeInProgress += c;
                            else
                            {
                                Attributes.Add(tokenAttributeInProgress);
                                state = SExpTokenParsingState.FinishedParsingToken;
                            }
                        }
                        else
                        {
                            Attributes.Add(tokenAttributeInProgress);
                            state = SExpTokenParsingState.ScanningForTokenSubObject;
                        }
                        break;
                    case SExpTokenParsingState.ParsingTokenSubToken:
                        if (!inString)
                        {
                            if (c == '"')
                            {
                                inString = true;
                                numConsecutiveBackslashes = 0;
                            }
                            else
                            {
                                if (c == '(')
                                    tokenSubTokenDepth++;
                                else if (c == ')')
                                {
                                    tokenSubTokenDepth--;
                                    if (tokenSubTokenDepth == 0)
                                    {
                                        SExpressionToken subToken = new SExpressionToken(sExpressionString, startIndexOfTokenSubToken, i);
                                        SubTokens.Add(subToken);
                                        state = SExpTokenParsingState.ScanningForTokenSubObject;
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (c == '"' && (numConsecutiveBackslashes % 2) == 0)
                                inString = false;
                            else if (c == '\\')
                                numConsecutiveBackslashes++;
                            else
                                numConsecutiveBackslashes = 0;
                        }
                        break;
                }
            }

            if (state != SExpTokenParsingState.FinishedParsingToken)
                throw new FormatException("Did not finish parsing S-Expression token within the string limits");
        }

        public string Serialise(int maxPrettyPrintDepth = 0, char prettyPrintIndent = '\t', int depth = 0)
        {
            int prettyPrintDepth = maxPrettyPrintDepth - depth;
            if (prettyPrintDepth == 0)
                return string.Format(
                    "{0}({1}{2}{3}{4}{5})",
                    new string(prettyPrintIndent, depth),
                    this.Name,
                    this.Attributes.Count > 0 ? " " : "",
                    string.Join(" ", this.Attributes),
                    this.SubTokens.Count > 0 ? " " : "",
                    string.Join(" ", this.SubTokens)
                    );
            else
            {
                if (this.SubTokens.Count > 0)
                {
                    return string.Format(
                        "{0}({1}{2}{3}{4}{5}{4}{0})",
                        new string(prettyPrintIndent, depth),
                        this.Name,
                        this.Attributes.Count > 0 ? " " : "",
                        string.Join(" ", this.Attributes),
                        Environment.NewLine,
                        string.Join("\n", this.SubTokens.Select(sT => sT.Serialise(maxPrettyPrintDepth, prettyPrintIndent, depth + 1)))
                        );
                }
                else
                    return string.Format(
                        "{0}({1}{2}{3})",
                        new string(prettyPrintIndent, depth),
                        this.Name,
                        this.Attributes.Count > 0 ? " " : "",
                        string.Join(" ", this.Attributes)
                        );
            }
        }

        public override string ToString()
        {
            return this.Serialise();
        }
    }
}
