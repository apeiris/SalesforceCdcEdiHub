# PDFToXml

**PDFToXml** is an open-source, layout-driven document extraction engine that converts **PDF documents** into **XML, HTML, or X12** by defining the PDF structure in a simple, declarative XML mapping file.

Instead of writing custom parsers for every document type, PDFToXml lets you describe *where data appears on the page*. The engine handles parsing, transformation, and structured output generation.

---

## ✨ Features

- 📄 Convert **PDF → XML, HTML, or X12**
- 🧩 Declarative, human-readable **XML layout definitions**
- 📐 Coordinate-based extraction for high accuracy
- 🛠 Extensible helper-function architecture
- 🔁 Reusable mappings for similar document templates
- 🏢 Designed for enterprise documents (POs, invoices, EDI)

---

## 🧠 How It Works

1. **Provide a PDF input document**
2. **Define an XML layout mapping** describing where data lives on the PDF
3. **Run the parser**
4. **Generate structured output** (XML / HTML / X12)

No document-specific code required.

---

## 📘 Example: Purchase Order Extraction

This example demonstrates the complete flow from **PDF input** → **XML layout definition** → **generated XML output**.

---

### 📄 PDF Input Document

The input PDF is a Purchase Order containing fixed-position fields and a tabular line-item section.

**Purchase Order (PO4.pdf)** includes:

- Purchase Order number and dates
- Buyer and supplier details
- Addresses and contact information
- Line-item table with quantities and pricing

#### PDF in Repository (GitHub-friendly)

Place the PDF in your repository, for example:

<embed src="po4.pdf" type="application/pdf" width="100%" height="600px" />

rrr
