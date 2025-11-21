using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using NLog;
using Rectangle = iText.Kernel.Geom.Rectangle;
namespace PdfSevices {
	public enum LabelLocation {
		TOP_LEFT, TOP_RIGHT, BOTTOM_LEFT, BOTTOM_RIGHT,
		LOWER_LEFT_CORNER, LOWER_RIGHT_CORNER,
		UPPER_LEFT_CORNER, UPPER_RIGHT_CORNER,
		MIDDLE_TOP, MIDDLE_BOTTOM,
		BOTTOM_LEFT_and_TOP_RIGHT,
	}
	public class Render {
		private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
		public static void DrawBorder(PdfDocument document, Rectangle rectangle, int pageNumber = 1) {

			Logger.Debug($"Drawing border on page {pageNumber} at rect x={rectangle.GetX()}:y={rectangle.GetY()} w={rectangle.GetWidth()}:h={rectangle.GetHeight()}");
			PdfCanvas canvas = new PdfCanvas(document.GetPage(pageNumber));
			canvas.SetStrokeColor(DeviceRgb.RED)
				.SetLineWidth(1f)
				.Rectangle(rectangle.GetX(), rectangle.GetY(), rectangle.GetWidth(), rectangle.GetHeight())
				.Stroke();
		}
		/*
		public static void DrawCornerLabel(PdfDocument document, Rectangle rect, LabelLocation location, string text = "", int pageNumber = 1) {
			PdfCanvas canvas = new PdfCanvas(document.GetPage(pageNumber));
			canvas.BeginText();
			var font = iText.Kernel.Font.PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA);
			float fontSize = 8f;
		
			canvas.SetFillColor(ColorConstants.BLACK);
			switch (location) {
				case LabelLocation.BOTTOM_LEFT:
					break;
				case LabelLocation.BOTTOM_LEFT_and_TOP_RIGHT:// draw both corners
					text = $"({rect.GetX()},{rect.GetY()})";
					canvas.MoveText(rect.GetX(), rect.GetY() - 10); // Offset slightly below rect
					canvas.ShowText(text);
					text = $"({rect.GetRight()},{rect.GetTop()})";
					float textWidth = font.GetWidth(text, fontSize);
					canvas.MoveText(rect.GetRight() - rect.GetX() - textWidth, rect.GetTop() + 3 - rect.GetY() + 10); // move to top right corner
					canvas.ShowText(text);
					break;
			}
			canvas.EndText();
		}*/
		public static void DrawCornerLabel(PdfDocument document, Rectangle rect, LabelLocation location, int pageNumber = 1) {
			PdfCanvas canvas = new PdfCanvas(document.GetPage(pageNumber));
			PdfFont font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
			float fontSize = 8f;
			void DrawLabel(string text, float x, float y) {// Helper to draw label with white text on black background at given position
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
					DrawLabel($"({rect.GetX():0.##}, {rect.GetY():0.##})", rect.GetX(), rect.GetY() - fontSize - 2);
					string trText = $"({rect.GetRight():0.##}, {rect.GetTop():0.##})";
					float trTextWidth = font.GetWidth(trText, fontSize);
					DrawLabel(trText, rect.GetRight() - trTextWidth, rect.GetTop() + 2);
					break;
			}

		}
	}
}

//public static class ColorExtensions {
//	public static iText.Kernel.Colors.Color ToITextColor(this System.Drawing.Color c) {
//		return new DeviceRgb(c.R, c.G, c.B);
//	}
//}

