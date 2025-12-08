using iText.IO.Font;
using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NLog;
using Rectangle = iText.Kernel.Geom.Rectangle;
namespace PDF;
public enum LabelLocation {
	TOP_LEFT, TOP_RIGHT, BOTTOM_LEFT, BOTTOM_RIGHT,
	LOWER_LEFT_CORNER, LOWER_RIGHT_CORNER,
	UPPER_LEFT_CORNER, UPPER_RIGHT_CORNER,
	MIDDLE_TOP, MIDDLE_BOTTOM,
	BOTTOM_LEFT_and_TOP_RIGHT,
	BOTTOM_LEFT_and_TOP_RIGHT_NODECIMAL
}
public class Render {
	private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
	public static void DrawBorder(PdfDocument document, Rectangle rectangle, int pageNumber = 1) {
		if (rectangle == null) return;
		//if(rectangle!=null)
		//Logger.Debug($"Drawing border on page {pageNumber} at rect x={rectangle.GetX()}:y={rectangle.GetY()} w={rectangle.GetWidth()}:h={rectangle.GetHeight()}");
		PdfCanvas canvas = new(document.GetPage(pageNumber));
		canvas.SetStrokeColor(DeviceRgb.RED)
			.SetLineWidth(1f)
			.Rectangle(rectangle.GetX(), rectangle.GetY(), rectangle.GetWidth(), rectangle.GetHeight())
			.Stroke();
	}
	

	private static PdfFont GetBoldFontForAnnotation() {
		return PdfFontFactory.CreateFont(
			StandardFonts.HELVETICA_BOLD,
			PdfEncodings.WINANSI,
			PdfFontFactory.EmbeddingStrategy.FORCE_NOT_EMBEDDED);   // this is the key
	}

	public static void DrawCornerLabel(PdfDocument document, Rectangle rect, LabelLocation location, int pageNumber = 1) {
		PdfCanvas canvas = new(document.GetPage(pageNumber));
		//PdfFont font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
		PdfFont font = GetBoldFontForAnnotation();
		float fontSize = 8f;
		void DrawLabel(string text, float x, float y) {// Helper to draw label with white text on black background at given position
													   //	if (surpressDecimals) {text=int.Parse(text).ToString();}
			float textWidth = font.GetWidth(text, fontSize);
			float textHeight = font.GetAscent(text, fontSize) - font.GetDescent(text, fontSize);

			canvas.SaveState();// Draw black rectangle background
			canvas.SetFillColor(ColorConstants.BLACK);
			canvas.Rectangle(x, y, textWidth, textHeight);
			canvas.Fill();
			canvas.RestoreState();
			canvas.BeginText(); // Draw white text over rectangle
			canvas.SetFontAndSize(font, fontSize);
			canvas.SetFillColor(ColorConstants.WHITE);
			canvas.SetTextMatrix(x, y);
			canvas.ShowText(text);
			canvas.EndText();
		}
		
		switch (location) {
			case LabelLocation.BOTTOM_LEFT:
				DrawLabel($"({rect.GetX():0.##}, {rect.GetY():0.##})", rect.GetX(), rect.GetY() - fontSize - 2);
				break;
			case LabelLocation.TOP_RIGHT:
				string topRightText = $"({rect.GetRight():0.##}, {rect.GetTop():0.##})";
				float textWidth = font.GetWidth(topRightText, fontSize);
				DrawLabel(topRightText, rect.GetRight() - textWidth, rect.GetTop() + 2);
				break;
			case LabelLocation.BOTTOM_LEFT_and_TOP_RIGHT:

				DrawLabel($"{rect.GetX():0.##}, {rect.GetY():0.##}",
				rect.GetX(), rect.GetY() - fontSize - 3);

				string tr2 = $"{rect.GetRight():0.##},{rect.GetTop():0.##}";
				float w2 = font.GetWidth(tr2, fontSize);
				DrawLabel(tr2,
					rect.GetRight() - w2, rect.GetTop() + 3);
				break;
			case LabelLocation.BOTTOM_LEFT_and_TOP_RIGHT_NODECIMAL:

				DrawLabel($"{rect.GetX():0}, {rect.GetY():0}",
				rect.GetX(), rect.GetY() - 7);

				string tr3 = $"{rect.GetRight():0},{rect.GetTop():0}";
				float w3 = font.GetWidth(tr3, fontSize);
				DrawLabel(tr3,rect.GetRight() - w3, rect.GetTop() + 1);
				break;
		}

	}
}



