using KiCAD_DB_Editor.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace KiCAD_DB_Editor.ViewModel.Utilities
{
    public partial class SExpressionToken
    {
        public static SExpressionToken FromString(string sExpressionString)
        {
            HashSet<int> indexesOfStringCharacters = new HashSet<int>();

            var matchCollection = SExpressionStringRegex().Matches(sExpressionString);
            foreach (Match m in matchCollection)
            {
                for (int i = m.Index + 1; i < m.Index + m.Length - 1; i++)
                    indexesOfStringCharacters.Add(i);
            }

            return new SExpressionToken(sExpressionString, indexesOfStringCharacters);
        }

        // ======================================================================

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

        public SExpressionToken(string sExpressionString, HashSet<int> indexesOfStringCharacters, int scanStart = -1, int scanEnd = -1)
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

            string stringScanString = sExpressionString[scanStart..(scanEnd + 1)];

            SExpTokenParsingState state = SExpTokenParsingState.ScanningForOpeningToken;
            for (int i = scanStart; i <= scanEnd && state != SExpTokenParsingState.FinishedParsingToken; i++)
            {
                switch (state)
                {
                    case SExpTokenParsingState.ScanningForOpeningToken:
                        if (sExpressionString[i] == '(' && !indexesOfStringCharacters.Contains(i))
                            state = SExpTokenParsingState.ParsingTokenName;
                        break;
                    case SExpTokenParsingState.ParsingTokenName:
                        if (!Char.IsWhiteSpace(sExpressionString[i]))
                            nameInProgress += sExpressionString[i];
                        else
                        {
                            Name = nameInProgress;
                            state = SExpTokenParsingState.ScanningForTokenSubObject;
                        }
                        break;
                    case SExpTokenParsingState.ScanningForTokenSubObject:
                        if (!Char.IsWhiteSpace(sExpressionString[i]))
                        {
                            if (sExpressionString[i] == ')')
                                state = SExpTokenParsingState.FinishedParsingToken;
                            else if (sExpressionString[i] == '(')
                            {
                                state = SExpTokenParsingState.ParsingTokenSubToken;
                                startIndexOfTokenSubToken = i;
                                tokenSubTokenDepth = 1;
                            }
                            else
                            {
                                state = SExpTokenParsingState.ParsingTokenAttribute;
                                tokenAttributeInProgress = "" + sExpressionString[i];
                            }
                        }
                        break;
                    case SExpTokenParsingState.ParsingTokenAttribute:
                        if (indexesOfStringCharacters.Contains(i))
                            tokenAttributeInProgress += sExpressionString[i];
                        else if (!Char.IsWhiteSpace(sExpressionString[i]))
                        {
                            if (sExpressionString[i] != ')')
                                tokenAttributeInProgress += sExpressionString[i];
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
                        if (!indexesOfStringCharacters.Contains(i))
                        {
                            if (sExpressionString[i] == '(')
                                tokenSubTokenDepth++;
                            else if (sExpressionString[i] == ')')
                            {
                                tokenSubTokenDepth--;
                                if (tokenSubTokenDepth == 0)
                                {
                                    SExpressionToken subToken = new SExpressionToken(sExpressionString, indexesOfStringCharacters, startIndexOfTokenSubToken, i);
                                    SubTokens.Add(subToken);
                                    state = SExpTokenParsingState.ScanningForTokenSubObject;
                                }
                            }
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

        [GeneratedRegexAttribute(@"(?:"""")|(?:"".*?[^\\](?:\\{2})*"")", RegexOptions.Singleline)]
        private static partial Regex SExpressionStringRegex();
    }
}
