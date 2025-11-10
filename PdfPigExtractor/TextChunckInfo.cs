using iText.Kernel.Geom;

namespace PdfDataExtraction {
	/// <summary>
	/// Represents a piece of text extracted from the PDF along with its bounding box location.
	/// </summary>
	public class TextChunkInfo {
		public string Text { get; set; }

		/// <summary>
		/// The bounding box of the text chunk on the page.
		/// </summary>
		public Rectangle? Location { get; set; }

		/// <summary>
		/// Gets the Y-coordinate of the baseline start (useful for determining rows).
		/// </summary>
		public float BaseLineY { get; set; }
	}
}