namespace PDF;

public class ExtractedArea {
	public string Name { get; set; }
	public string Value { get; set; }
	public iText.Kernel.Geom.Rectangle Bounds { get; set; }

	public ExtractedArea(string name, string value, iText.Kernel.Geom.Rectangle bounds) {
		Name = name;
		Value = value;
		Bounds = bounds;
	}
	public override string ToString() => Value ?? string.Empty;
	
}