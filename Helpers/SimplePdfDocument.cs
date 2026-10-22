using System.Globalization;
using System.Text;

namespace Ice_Cream_Parlour_Eproject.Helpers
{
    /// <summary>
    /// Small dependency free PDF writer used for invoices and report exports.
    /// It produces a valid PDF 1.4 document with Helvetica text, which is enough
    /// for tabular reports and keeps the deployment footprint tiny.
    /// </summary>
    public sealed class SimplePdfDocument
    {
        private const float PageWidth = 595f;   // A4 portrait, points
        private const float PageHeight = 842f;
        private const float Margin = 40f;

        private readonly List<StringBuilder> _pages = new();
        private StringBuilder? _current;
        private float _y = PageHeight - Margin;

        public SimplePdfDocument()
        {
            AddPage();
        }

        private void AddPage()
        {
            _current = new StringBuilder();
            _pages.Add(_current);
            _y = PageHeight - Margin;
        }

        private void EnsureSpace(float required)
        {
            if (_current == null || _y - required < Margin)
            {
                AddPage();
            }
        }

        /// <summary>Writes a single line of text.</summary>
        public void WriteLine(string? text, float fontSize = 10f, bool bold = false, float indent = 0f)
        {
            WriteText(text, fontSize, bold, Margin + indent);
            _y -= fontSize * 1.5f;
        }

        /// <summary>Writes text at the current baseline without moving to the next line.</summary>
        public void WriteText(string? text, float fontSize, bool bold, float x, float? y = null)
        {
            EnsureSpace(fontSize * 1.6f);

            var escaped = Escape(text ?? string.Empty);
            var posY = y ?? _y;
            _current!.Append(
                string.Format(CultureInfo.InvariantCulture,
                    "BT /{0} {1} Tf 1 0 0 1 {2} {3} Tm ({4}) Tj ET\n",
                    bold ? "F2" : "F1", fontSize, x, posY, escaped));
        }

        /// <summary>Writes a horizontal row of columns (used for tables).</summary>
        public void WriteColumns(IReadOnlyList<string?> cells, IReadOnlyList<float> widths,
            float fontSize = 9f, bool bold = false, int rightAlignFrom = -1)
        {
            EnsureSpace(fontSize * 1.7f);
            var x = Margin;

            for (var i = 0; i < cells.Count && i < widths.Count; i++)
            {
                var text = cells[i] ?? string.Empty;
                var cellX = x;

                if (rightAlignFrom >= 0 && i >= rightAlignFrom)
                {
                    var approxWidth = text.Length * fontSize * 0.5f;
                    cellX = Math.Max(x, x + widths[i] - approxWidth);
                }

                WriteText(Truncate(text, widths[i], fontSize), fontSize, bold, cellX);
                x += widths[i];
            }

            _y -= fontSize * 1.6f;
        }

        public void AddHorizontalLine()
        {
            EnsureSpace(6f);
            _current!.Append(string.Format(CultureInfo.InvariantCulture,
                "0.7 w {0} {1} m {2} {1} l S\n", Margin, _y, PageWidth - Margin));
            _y -= 6f;
        }

        public void AddSpace(float points = 8f) => _y -= points;

        private static string Truncate(string text, float width, float fontSize)
        {
            var maxChars = (int)Math.Max(4, width / (fontSize * 0.5f));
            return text.Length <= maxChars ? text : text[..Math.Max(0, maxChars - 1)] + ".";
        }

        private static string Escape(string value)
        {
            var builder = new StringBuilder(value.Length);
            foreach (var c in value)
            {
                switch (c)
                {
                    case '\\': builder.Append("\\\\"); break;
                    case '(': builder.Append("\\("); break;
                    case ')': builder.Append("\\)"); break;
                    case '\r': break;
                    case '\n': builder.Append(' '); break;
                    default:
                        builder.Append(c >= 32 && c <= 126 ? c : '?');
                        break;
                }
            }
            return builder.ToString();
        }

        public byte[] Build()
        {
            // Object layout: 1 catalog, 2 pages, 3/4 fonts, then 2 objects per page.
            var objects = new List<string>
            {
                "<< /Type /Catalog /Pages 2 0 R >>",
                string.Empty, // pages object, filled in below
                "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>",
                "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>"
            };

            var pageObjectNumbers = new List<int>();

            foreach (var page in _pages)
            {
                var pageObj = objects.Count + 1;
                var contentObj = pageObj + 1;
                pageObjectNumbers.Add(pageObj);

                objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {PageWidth} {PageHeight}] " +
                            $"/Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents {contentObj} 0 R >>");
                objects.Add($"<< /Length {Encoding.ASCII.GetByteCount(page.ToString())} >>\nstream\n{page}\nendstream");
            }

            var kids = string.Join(" ", pageObjectNumbers.Select(n => $"{n} 0 R"));
            objects[1] = $"<< /Type /Pages /Kids [{kids}] /Count {_pages.Count} >>";

            using var stream = new MemoryStream();
            void Write(string text)
            {
                var bytes = Encoding.ASCII.GetBytes(text);
                stream.Write(bytes, 0, bytes.Length);
            }

            Write("%PDF-1.4\n");

            var offsets = new long[objects.Count + 1];
            for (var i = 0; i < objects.Count; i++)
            {
                offsets[i + 1] = stream.Position;
                Write($"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
            }

            var xrefPosition = stream.Position;
            Write($"xref\n0 {objects.Count + 1}\n");
            Write("0000000000 65535 f \n");
            for (var i = 1; i <= objects.Count; i++)
            {
                Write($"{offsets[i]:D10} 00000 n \n");
            }

            Write($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xrefPosition}\n%%EOF\n");

            return stream.ToArray();
        }
    }
}
