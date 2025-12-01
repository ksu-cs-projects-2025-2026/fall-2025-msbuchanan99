using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using iText.Kernel.Pdf;
using iText.Kernel.Font;

class Program
{
    static bool IsPrivateUseArea(char c) => c >= '\uE000' && c <= '\uF8FF';

    static bool IsSymbolChar(char c)
    {
        var cat = CharUnicodeInfo.GetUnicodeCategory(c);
        return IsPrivateUseArea(c) ||
               cat == UnicodeCategory.OtherSymbol ||
               cat == UnicodeCategory.MathSymbol ||
               cat == UnicodeCategory.CurrencySymbol ||
               cat == UnicodeCategory.ModifierSymbol;
    }

    static string ToUPlus(char c) => $"U+{((int)c).ToString("X4", CultureInfo.InvariantCulture)}";

    static void Main(string[] args)
    {
        var pdfPath = @"C:\Users\megha\Desktop\Threadfolio\PDFFontReader\pattern.pdf";
        var outCsv = @"C:\Users\megha\Desktop\Threadfolio\PDFFontReader\symbolFont.csv";

        using var pigDoc = UglyToad.PdfPig.PdfDocument.Open(pdfPath);

        // iText is only used to resolve real readable font names
        using var reader = new PdfReader(pdfPath);
        using var itextDoc = new iText.Kernel.Pdf.PdfDocument(reader);

        using var sw = new StreamWriter(outCsv, false, new System.Text.UTF8Encoding(true));
        sw.WriteLine("SymbolUnicode,FontName");

        var seen = new HashSet<(int CodePoint, string FontName)>();

        int lastPage = pigDoc.NumberOfPages;
        int firstOfLastTwo = 5;

        for (int p = firstOfLastTwo; p <= lastPage; p++)
        {
            var pigPage = pigDoc.GetPage(p);
            var itextPage = itextDoc.GetPage(p);

            // Grab iText font resources
            var resources = itextPage.GetResources();
            var fontDict = resources?.GetResource(iText.Kernel.Pdf.PdfName.Font) as iText.Kernel.Pdf.PdfDictionary;

            // Map CID names -> readable names
            var fontNameMap = new Dictionary<string, string>();

            if (fontDict != null)
            {
                foreach (var key in fontDict.KeySet())
                {
                    var fontObj = fontDict.GetAsDictionary(key);
                    if (fontObj == null) continue;

                    string cidName = key.GetValue(); // e.g., CIDFont+F1

                    // Try to read BaseFont or full font name
                    string readableName = fontObj.GetAsName(iText.Kernel.Pdf.PdfName.BaseFont)?.ToString() ?? cidName;

                    // Remove surrounding "/" if present
                    readableName = readableName.Trim('/');

                    // Try deeper FontProgram for human-friendly names
                    try
                    {
                        var pdfFont = iText.Kernel.Font.PdfFontFactory.CreateFont(fontObj);
                        if (pdfFont != null && pdfFont.GetFontProgram() != null)
                        {
                            var fn = pdfFont.GetFontProgram().GetFontNames().GetFontName();
                            if (!string.IsNullOrWhiteSpace(fn))
                                readableName = fn;
                        }
                    }
                    catch { /* ignore */ }

                    fontNameMap[cidName] = readableName;
                }
            }

            // Now walk symbols in text
            foreach (var letter in pigPage.Letters)
            {
                char ch = letter.Value[0];
                if (!IsSymbolChar(ch)) continue;

                string fontKey = letter.FontName ?? "Unknown";
                string readable = fontNameMap.ContainsKey(fontKey) ? fontNameMap[fontKey] : fontKey;

                var keyPair = ((int)ch, readable);
                if (seen.Add(keyPair))
                {
                    sw.WriteLine($"{ToUPlus(ch)},{readable}");
                }
            }
        }

        Console.WriteLine($"✅ Done. Created: {outCsv}");
    }
}
