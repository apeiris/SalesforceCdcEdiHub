using System.Drawing.Printing;
using System.Windows.Forms;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;
using Font=System.Drawing.Font;
using Properties= SalesforceCdcEdiHub.WinForms.Properties;
namespace WinForms;
partial class MainForm {
	private System.ComponentModel.IContainer components = null;

	protected override void Dispose(bool disposing) {
		if (disposing && (components != null)) {
			components.Dispose();
			}
		base.Dispose(disposing);
		}

	private void InitializeComponent() {
		btnAuthenticate = new Button();
		txtResult = new TextBox();
		btnGetTokenAsync = new Button();
		statusStrip1 = new StatusStrip();
		toolStripStatusLabel1 = new ToolStripStatusLabel();
		tabControl1 = new TabControl();
		tbpSfObjects = new TabPage();
		tableLayoutPanel3 = new TableLayoutPanel();
		dgvCDCEnabledObjects = new DataGridView();
		tableLayoutPanel10 = new TableLayoutPanel();
		button26 = new Button();
		button25 = new Button();
		button24 = new Button();
		button23 = new Button();
		button22 = new Button();
		button21 = new Button();
		button20 = new Button();
		button19 = new Button();
		button18 = new Button();
		button17 = new Button();
		button16 = new Button();
		button15 = new Button();
		button14 = new Button();
		button13 = new Button();
		button12 = new Button();
		button11 = new Button();
		button10 = new Button();
		button9 = new Button();
		button8 = new Button();
		button7 = new Button();
		button6 = new Button();
		button5 = new Button();
		button4 = new Button();
		button3 = new Button();
		button2 = new Button();
		bsA = new Button();
		btnMoveRight = new Button();
		dgvRegisteredCDCCandidates = new DataGridView();
		btnMoveLeft = new Button();
		label2 = new Label();
		lblDestinationList = new Label();
		grpPrimaryKey = new GroupBox();
		pictureBox1 = new PictureBox();
		label3 = new Label();
		textBox1 = new TextBox();
		chkAddIdentityField = new CheckBox();
		btnCommitToDB = new Button();
		btnClearDestination = new Button();
		lblSourceList = new Label();
		pictureBox2 = new PictureBox();
		tbpPubSub = new TabPage();
		tableLayoutPanel1 = new TableLayoutPanel();
		splitContainer1 = new SplitContainer();
		splitContainer2 = new SplitContainer();
		lbxObjects = new ListBox();
		lblPanel1 = new Label();
		rtxFieldsJsonArray = new RichTextBox();
		dgvObject = new DataGridView();
		lblSelectedTable = new Label();
		tableLayoutPanel2 = new TableLayoutPanel();
		btnListEvents = new Button();
		btnSubscribe = new Button();
		button1 = new Button();
		btnRegisterFields = new Button();
		btnDeleteCDCRegistration = new Button();
		tableLayoutPanel4 = new TableLayoutPanel();
		dgvRelations = new DataGridView();
		lblRelations = new Label();
		grpFilterOptions = new GroupBox();
		rbtFilterNone = new RadioButton();
		rbtFilterSubscribed = new RadioButton();
		tbpOAuth2 = new TabPage();
		tbpSOQL = new TabPage();
		tableLayoutPanel11 = new TableLayoutPanel();
		splitcSoql = new SplitContainer();
		tableLayoutPanel12 = new TableLayoutPanel();
		rtSoqlQuery = new RichTextBox();
		btnDeleteSoql = new Button();
		btnSaveSoql = new Button();
		tableLayoutPanel16 = new TableLayoutPanel();
		btnExecSoql = new Button();
		chkUseTooling = new CheckBox();
		button30 = new Button();
		button31 = new Button();
		tableLayoutPanel13 = new TableLayoutPanel();
		dgvSOQLResult = new DataGridView();
		tableLayoutPanel14 = new TableLayoutPanel();
		btnSoqlRDelete = new Button();
		btnSoqlSave = new Button();
		lblSoqlText = new Label();
		tableLayoutPanel15 = new TableLayoutPanel();
		btnBuildSelect = new Button();
		tableLayoutPanel17 = new TableLayoutPanel();
		lblSOQLRowCount = new Label();
		cmbSOQL = new ComboBox();
		tbpDescribeObject = new TabPage();
		tableLayoutPanel5 = new TableLayoutPanel();
		btnDeleteCmbObjectSelected = new Button();
		label1 = new Label();
		dgvSchema = new DataGridView();
		btnDescribe = new Button();
		cmbObjects = new ComboBox();
		lblCDCName = new Label();
		tbpEventLog = new TabPage();
		splitContainer3 = new SplitContainer();
		tableLayoutPanel6 = new TableLayoutPanel();
		tableLayoutPanel7 = new TableLayoutPanel();
		btnClearLog = new Button();
		rtxLog = new RichTextBox();
		rtfLog = new RichTextBox();
		tbpCDCEvents = new TabPage();
		tableLayoutPanel8 = new TableLayoutPanel();
		splitContainer4 = new SplitContainer();
		lbxCDCTopics = new ListBox();
		splitContainer5 = new SplitContainer();
		lbxCDCEvents = new ListBox();
		dgvFilteredFields = new DataGridView();
		tableLayoutPanel9 = new TableLayoutPanel();
		btnGetCDCSubscriptions = new Button();
		btnCDCStartSubscription = new Button();
		lblCDCStatus = new Label();
		tbpX12 = new TabPage();
		tableLayoutPanel18 = new TableLayoutPanel();
		cmbCDCTables = new ComboBox();
		grpx = new GroupBox();
		radioButton3 = new RadioButton();
		radioButton2 = new RadioButton();
		radioButton1 = new RadioButton();
		splitContainer7 = new SplitContainer();
		splitContainer6 = new SplitContainer();
		dgvCDCTables = new DataGridView();
		btnLogTest = new Button();
		statusStrip1.SuspendLayout();
		tabControl1.SuspendLayout();
		tbpSfObjects.SuspendLayout();
		tableLayoutPanel3.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)dgvCDCEnabledObjects).BeginInit();
		tableLayoutPanel10.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)dgvRegisteredCDCCandidates).BeginInit();
		grpPrimaryKey.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
		((System.ComponentModel.ISupportInitialize)pictureBox2).BeginInit();
		tbpPubSub.SuspendLayout();
		tableLayoutPanel1.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
		splitContainer1.Panel1.SuspendLayout();
		splitContainer1.Panel2.SuspendLayout();
		splitContainer1.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)splitContainer2).BeginInit();
		splitContainer2.Panel1.SuspendLayout();
		splitContainer2.Panel2.SuspendLayout();
		splitContainer2.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)dgvObject).BeginInit();
		tableLayoutPanel2.SuspendLayout();
		tableLayoutPanel4.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)dgvRelations).BeginInit();
		grpFilterOptions.SuspendLayout();
		tbpOAuth2.SuspendLayout();
		tbpSOQL.SuspendLayout();
		tableLayoutPanel11.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)splitcSoql).BeginInit();
		splitcSoql.Panel1.SuspendLayout();
		splitcSoql.Panel2.SuspendLayout();
		splitcSoql.SuspendLayout();
		tableLayoutPanel12.SuspendLayout();
		tableLayoutPanel16.SuspendLayout();
		tableLayoutPanel13.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)dgvSOQLResult).BeginInit();
		tableLayoutPanel14.SuspendLayout();
		tableLayoutPanel15.SuspendLayout();
		tableLayoutPanel17.SuspendLayout();
		tbpDescribeObject.SuspendLayout();
		tableLayoutPanel5.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)dgvSchema).BeginInit();
		tbpEventLog.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)splitContainer3).BeginInit();
		splitContainer3.Panel1.SuspendLayout();
		splitContainer3.Panel2.SuspendLayout();
		splitContainer3.SuspendLayout();
		tableLayoutPanel6.SuspendLayout();
		tableLayoutPanel7.SuspendLayout();
		tbpCDCEvents.SuspendLayout();
		tableLayoutPanel8.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)splitContainer4).BeginInit();
		splitContainer4.Panel1.SuspendLayout();
		splitContainer4.Panel2.SuspendLayout();
		splitContainer4.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)splitContainer5).BeginInit();
		splitContainer5.Panel1.SuspendLayout();
		splitContainer5.Panel2.SuspendLayout();
		splitContainer5.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)dgvFilteredFields).BeginInit();
		tableLayoutPanel9.SuspendLayout();
		tbpX12.SuspendLayout();
		tableLayoutPanel18.SuspendLayout();
		grpx.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)splitContainer7).BeginInit();
		splitContainer7.Panel1.SuspendLayout();
		splitContainer7.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)splitContainer6).BeginInit();
		splitContainer6.Panel1.SuspendLayout();
		splitContainer6.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)dgvCDCTables).BeginInit();
		SuspendLayout();
		// 
		// btnAuthenticate
		// 
		btnAuthenticate.Location = new Point(389, 226);
		btnAuthenticate.Margin = new Padding(4, 3, 4, 3);
		btnAuthenticate.Name = "btnAuthenticate";
		btnAuthenticate.Size = new Size(100, 35);
		btnAuthenticate.TabIndex = 0;
		btnAuthenticate.Text = "Authenticate";
		btnAuthenticate.UseVisualStyleBackColor = true;
		btnAuthenticate.Click += btnAuthenticate_Click;
		// 
		// txtResult
		// 
		txtResult.Location = new Point(21, 20);
		txtResult.Margin = new Padding(4, 3, 4, 3);
		txtResult.Multiline = true;
		txtResult.Name = "txtResult";
		txtResult.Size = new Size(674, 115);
		txtResult.TabIndex = 1;
		// 
		// btnGetTokenAsync
		// 
		btnGetTokenAsync.Location = new Point(21, 226);
		btnGetTokenAsync.Margin = new Padding(4, 3, 4, 3);
		btnGetTokenAsync.Name = "btnGetTokenAsync";
		btnGetTokenAsync.Size = new Size(311, 35);
		btnGetTokenAsync.TabIndex = 2;
		btnGetTokenAsync.Text = "Task<token,iUrl,tenantId> = GetTokenAsync()";
		btnGetTokenAsync.UseVisualStyleBackColor = true;
		btnGetTokenAsync.Click += btnGetTokenAsync_Click;
		// 
		// statusStrip1
		// 
		statusStrip1.Items.AddRange(new ToolStripItem[] { toolStripStatusLabel1 });
		statusStrip1.Location = new Point(0, 706);
		statusStrip1.Name = "statusStrip1";
		statusStrip1.Size = new Size(1423, 22);
		statusStrip1.TabIndex = 3;
		statusStrip1.Text = "statusStrip1";
		// 
		// toolStripStatusLabel1
		// 
		toolStripStatusLabel1.Name = "toolStripStatusLabel1";
		toolStripStatusLabel1.Size = new Size(118, 17);
		toolStripStatusLabel1.Text = "toolStripStatusLabel1";
		// 
		// tabControl1
		// 
		tabControl1.Controls.Add(tbpSfObjects);
		tabControl1.Controls.Add(tbpPubSub);
		tabControl1.Controls.Add(tbpOAuth2);
		tabControl1.Controls.Add(tbpSOQL);
		tabControl1.Controls.Add(tbpDescribeObject);
		tabControl1.Controls.Add(tbpEventLog);
		tabControl1.Controls.Add(tbpCDCEvents);
		tabControl1.Controls.Add(tbpX12);
		tabControl1.Dock = DockStyle.Fill;
		tabControl1.Location = new Point(0, 0);
		tabControl1.Name = "tabControl1";
		tabControl1.SelectedIndex = 0;
		tabControl1.Size = new Size(1423, 706);
		tabControl1.TabIndex = 4;
		tabControl1.Selected += TabControl1_Selected1;
		// 
		// tbpSfObjects
		// 
		tbpSfObjects.Controls.Add(tableLayoutPanel3);
		tbpSfObjects.Location = new Point(4, 24);
		tbpSfObjects.Name = "tbpSfObjects";
		tbpSfObjects.Size = new Size(1415, 678);
		tbpSfObjects.TabIndex = 2;
		tbpSfObjects.Text = "Objects";
		tbpSfObjects.UseVisualStyleBackColor = true;
		// 
		// tableLayoutPanel3
		// 
		tableLayoutPanel3.ColumnCount = 4;
		tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
		tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 62F));
		tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
		tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 276F));
		tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
		tableLayoutPanel3.Controls.Add(dgvCDCEnabledObjects, 0, 1);
		tableLayoutPanel3.Controls.Add(tableLayoutPanel10, 0, 5);
		tableLayoutPanel3.Controls.Add(btnMoveRight, 1, 2);
		tableLayoutPanel3.Controls.Add(dgvRegisteredCDCCandidates, 2, 1);
		tableLayoutPanel3.Controls.Add(btnMoveLeft, 1, 3);
		tableLayoutPanel3.Controls.Add(label2, 0, 0);
		tableLayoutPanel3.Controls.Add(lblDestinationList, 2, 5);
		tableLayoutPanel3.Controls.Add(grpPrimaryKey, 2, 6);
		tableLayoutPanel3.Controls.Add(lblSourceList, 0, 6);
		tableLayoutPanel3.Controls.Add(pictureBox2, 0, 7);
		tableLayoutPanel3.Location = new Point(55, 3);
		tableLayoutPanel3.Name = "tableLayoutPanel3";
		tableLayoutPanel3.RowCount = 8;
		tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Absolute, 41F));
		tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Percent, 71.7791443F));
		tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Percent, 28.22086F));
		tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
		tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Absolute, 125F));
		tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F));
		tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Absolute, 41F));
		tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Absolute, 62F));
		tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Absolute, 73F));
		tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
		tableLayoutPanel3.Size = new Size(1364, 592);
		tableLayoutPanel3.TabIndex = 0;
		// 
		// dgvCDCEnabledObjects
		// 
		dgvCDCEnabledObjects.AllowUserToAddRows = false;
		dgvCDCEnabledObjects.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.ColumnHeader;
		dgvCDCEnabledObjects.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
		dgvCDCEnabledObjects.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
		dgvCDCEnabledObjects.Dock = DockStyle.Fill;
		dgvCDCEnabledObjects.Location = new Point(3, 44);
		dgvCDCEnabledObjects.Name = "dgvCDCEnabledObjects";
		tableLayoutPanel3.SetRowSpan(dgvCDCEnabledObjects, 4);
		dgvCDCEnabledObjects.Size = new Size(890, 381);
		dgvCDCEnabledObjects.TabIndex = 0;
		dgvCDCEnabledObjects.RowsAdded += dgvRowCountChanged;
		dgvCDCEnabledObjects.RowsRemoved += dgvRowCountChanged;
		// 
		// tableLayoutPanel10
		// 
		tableLayoutPanel10.ColumnCount = 26;
		tableLayoutPanel3.SetColumnSpan(tableLayoutPanel10, 2);
		tableLayoutPanel10.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
		tableLayoutPanel10.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
		tableLayoutPanel10.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
		tableLayoutPanel10.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
		tableLayoutPanel10.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
		tableLayoutPanel10.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
		tableLayoutPanel10.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
		tableLayoutPanel10.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
		tableLayoutPanel10.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
		tableLayoutPanel10.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
		tableLayoutPanel10.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
		tableLayoutPanel10.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
		tableLayoutPanel10.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
		tableLayoutPanel10.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
		tableLayoutPanel10.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
		tableLayoutPanel10.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
		tableLayoutPanel10.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
		tableLayoutPanel10.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
		tableLayoutPanel10.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
		tableLayoutPanel10.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
		tableLayoutPanel10.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
		tableLayoutPanel10.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
		tableLayoutPanel10.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
		tableLayoutPanel10.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
		tableLayoutPanel10.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
		tableLayoutPanel10.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
		tableLayoutPanel10.Controls.Add(button26, 25, 0);
		tableLayoutPanel10.Controls.Add(button25, 24, 0);
		tableLayoutPanel10.Controls.Add(button24, 23, 0);
		tableLayoutPanel10.Controls.Add(button23, 22, 0);
		tableLayoutPanel10.Controls.Add(button22, 21, 0);
		tableLayoutPanel10.Controls.Add(button21, 20, 0);
		tableLayoutPanel10.Controls.Add(button20, 19, 0);
		tableLayoutPanel10.Controls.Add(button19, 18, 0);
		tableLayoutPanel10.Controls.Add(button18, 17, 0);
		tableLayoutPanel10.Controls.Add(button17, 16, 0);
		tableLayoutPanel10.Controls.Add(button16, 15, 0);
		tableLayoutPanel10.Controls.Add(button15, 14, 0);
		tableLayoutPanel10.Controls.Add(button14, 13, 0);
		tableLayoutPanel10.Controls.Add(button13, 12, 0);
		tableLayoutPanel10.Controls.Add(button12, 11, 0);
		tableLayoutPanel10.Controls.Add(button11, 10, 0);
		tableLayoutPanel10.Controls.Add(button10, 9, 0);
		tableLayoutPanel10.Controls.Add(button9, 8, 0);
		tableLayoutPanel10.Controls.Add(button8, 7, 0);
		tableLayoutPanel10.Controls.Add(button7, 6, 0);
		tableLayoutPanel10.Controls.Add(button6, 5, 0);
		tableLayoutPanel10.Controls.Add(button5, 4, 0);
		tableLayoutPanel10.Controls.Add(button4, 3, 0);
		tableLayoutPanel10.Controls.Add(button3, 2, 0);
		tableLayoutPanel10.Controls.Add(button2, 1, 0);
		tableLayoutPanel10.Controls.Add(bsA, 0, 0);
		tableLayoutPanel10.Location = new Point(3, 431);
		tableLayoutPanel10.Name = "tableLayoutPanel10";
		tableLayoutPanel10.RowCount = 1;
		tableLayoutPanel10.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
		tableLayoutPanel10.Size = new Size(562, 30);
		tableLayoutPanel10.TabIndex = 11;
		// 
		// button26
		// 
		button26.Location = new Point(503, 3);
		button26.Name = "button26";
		button26.Size = new Size(14, 23);
		button26.TabIndex = 25;
		button26.Text = "Z";
		button26.UseVisualStyleBackColor = true;
		button26.Click += btnSort_Click;
		// 
		// button25
		// 
		button25.Location = new Point(483, 3);
		button25.Name = "button25";
		button25.Size = new Size(14, 23);
		button25.TabIndex = 24;
		button25.Text = "Y";
		button25.UseVisualStyleBackColor = true;
		button25.Click += btnSort_Click;
		// 
		// button24
		// 
		button24.Location = new Point(463, 3);
		button24.Name = "button24";
		button24.Size = new Size(14, 23);
		button24.TabIndex = 23;
		button24.Text = "X";
		button24.UseVisualStyleBackColor = true;
		button24.Click += btnSort_Click;
		// 
		// button23
		// 
		button23.Location = new Point(443, 3);
		button23.Name = "button23";
		button23.Size = new Size(14, 23);
		button23.TabIndex = 22;
		button23.Text = "W";
		button23.UseVisualStyleBackColor = true;
		button23.Click += btnSort_Click;
		// 
		// button22
		// 
		button22.Location = new Point(423, 3);
		button22.Name = "button22";
		button22.Size = new Size(14, 23);
		button22.TabIndex = 21;
		button22.Text = "V";
		button22.UseVisualStyleBackColor = true;
		button22.Click += btnSort_Click;
		// 
		// button21
		// 
		button21.Location = new Point(403, 3);
		button21.Name = "button21";
		button21.Size = new Size(14, 23);
		button21.TabIndex = 20;
		button21.Text = "U";
		button21.UseVisualStyleBackColor = true;
		button21.Click += btnSort_Click;
		// 
		// button20
		// 
		button20.Location = new Point(383, 3);
		button20.Name = "button20";
		button20.Size = new Size(14, 23);
		button20.TabIndex = 19;
		button20.Text = "T";
		button20.UseVisualStyleBackColor = true;
		button20.Click += btnSort_Click;
		// 
		// button19
		// 
		button19.Location = new Point(363, 3);
		button19.Name = "button19";
		button19.Size = new Size(14, 23);
		button19.TabIndex = 18;
		button19.Text = "S";
		button19.UseVisualStyleBackColor = true;
		button19.Click += btnSort_Click;
		// 
		// button18
		// 
		button18.Location = new Point(343, 3);
		button18.Name = "button18";
		button18.Size = new Size(14, 23);
		button18.TabIndex = 17;
		button18.Text = "R";
		button18.UseVisualStyleBackColor = true;
		button18.Click += btnSort_Click;
		// 
		// button17
		// 
		button17.Location = new Point(323, 3);
		button17.Name = "button17";
		button17.Size = new Size(14, 23);
		button17.TabIndex = 16;
		button17.Text = "Q";
		button17.UseVisualStyleBackColor = true;
		button17.Click += btnSort_Click;
		// 
		// button16
		// 
		button16.Location = new Point(303, 3);
		button16.Name = "button16";
		button16.Size = new Size(14, 23);
		button16.TabIndex = 15;
		button16.Text = "P";
		button16.UseVisualStyleBackColor = true;
		button16.Click += btnSort_Click;
		// 
		// button15
		// 
		button15.Location = new Point(283, 3);
		button15.Name = "button15";
		button15.Size = new Size(14, 23);
		button15.TabIndex = 14;
		button15.Text = "O";
		button15.UseVisualStyleBackColor = true;
		button15.Click += btnSort_Click;
		// 
		// button14
		// 
		button14.Location = new Point(263, 3);
		button14.Name = "button14";
		button14.Size = new Size(14, 23);
		button14.TabIndex = 13;
		button14.Text = "N";
		button14.UseVisualStyleBackColor = true;
		button14.Click += btnSort_Click;
		// 
		// button13
		// 
		button13.Location = new Point(243, 3);
		button13.Name = "button13";
		button13.Size = new Size(14, 23);
		button13.TabIndex = 12;
		button13.Text = "M";
		button13.UseVisualStyleBackColor = true;
		button13.Click += btnSort_Click;
		// 
		// button12
		// 
		button12.Location = new Point(223, 3);
		button12.Name = "button12";
		button12.Size = new Size(14, 23);
		button12.TabIndex = 11;
		button12.Text = "L";
		button12.UseVisualStyleBackColor = true;
		button12.Click += btnSort_Click;
		// 
		// button11
		// 
		button11.Location = new Point(203, 3);
		button11.Name = "button11";
		button11.Size = new Size(14, 23);
		button11.TabIndex = 10;
		button11.Text = "K";
		button11.UseVisualStyleBackColor = true;
		button11.Click += btnSort_Click;
		// 
		// button10
		// 
		button10.Location = new Point(183, 3);
		button10.Name = "button10";
		button10.Size = new Size(14, 23);
		button10.TabIndex = 9;
		button10.Text = "J";
		button10.UseVisualStyleBackColor = true;
		button10.Click += btnSort_Click;
		// 
		// button9
		// 
		button9.Location = new Point(163, 3);
		button9.Name = "button9";
		button9.Size = new Size(14, 23);
		button9.TabIndex = 8;
		button9.Text = "I";
		button9.UseVisualStyleBackColor = true;
		button9.Click += btnSort_Click;
		// 
		// button8
		// 
		button8.Location = new Point(143, 3);
		button8.Name = "button8";
		button8.Size = new Size(14, 23);
		button8.TabIndex = 7;
		button8.Text = "H";
		button8.UseVisualStyleBackColor = true;
		button8.Click += btnSort_Click;
		// 
		// button7
		// 
		button7.Location = new Point(123, 3);
		button7.Name = "button7";
		button7.Size = new Size(14, 23);
		button7.TabIndex = 6;
		button7.Text = "G";
		button7.UseVisualStyleBackColor = true;
		button7.Click += btnSort_Click;
		// 
		// button6
		// 
		button6.Location = new Point(103, 3);
		button6.Name = "button6";
		button6.Size = new Size(14, 23);
		button6.TabIndex = 5;
		button6.Text = "F";
		button6.UseVisualStyleBackColor = true;
		button6.Click += btnSort_Click;
		// 
		// button5
		// 
		button5.Location = new Point(83, 3);
		button5.Name = "button5";
		button5.Size = new Size(14, 23);
		button5.TabIndex = 4;
		button5.Text = "E";
		button5.UseVisualStyleBackColor = true;
		button5.Click += btnSort_Click;
		// 
		// button4
		// 
		button4.Location = new Point(63, 3);
		button4.Name = "button4";
		button4.Size = new Size(14, 23);
		button4.TabIndex = 3;
		button4.Text = "D";
		button4.UseVisualStyleBackColor = true;
		button4.Click += btnSort_Click;
		// 
		// button3
		// 
		button3.Location = new Point(43, 3);
		button3.Name = "button3";
		button3.Size = new Size(14, 23);
		button3.TabIndex = 2;
		button3.Text = "C";
		button3.UseVisualStyleBackColor = true;
		button3.Click += btnSort_Click;
		// 
		// button2
		// 
		button2.Location = new Point(23, 3);
		button2.Name = "button2";
		button2.Size = new Size(14, 23);
		button2.TabIndex = 1;
		button2.Text = "B";
		button2.UseVisualStyleBackColor = true;
		button2.Click += btnSort_Click;
		// 
		// bsA
		// 
		bsA.Location = new Point(3, 3);
		bsA.Name = "bsA";
		bsA.Size = new Size(14, 23);
		bsA.TabIndex = 0;
		bsA.Text = "A";
		bsA.UseVisualStyleBackColor = true;
		bsA.Click += btnSort_Click;
		// 
		// btnMoveRight
		// 
		btnMoveRight.Font = new Font("Segoe UI", 14.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
		btnMoveRight.Location = new Point(899, 199);
		btnMoveRight.Name = "btnMoveRight";
		btnMoveRight.Size = new Size(56, 40);
		btnMoveRight.TabIndex = 0;
		btnMoveRight.Text = ">";
		btnMoveRight.UseVisualStyleBackColor = true;
		btnMoveRight.Click += btnMoveRight_Click;
		// 
		// dgvRegisteredCDCCandidates
		// 
		dgvRegisteredCDCCandidates.AllowUserToAddRows = false;
		dgvRegisteredCDCCandidates.AllowUserToDeleteRows = false;
		dgvRegisteredCDCCandidates.AllowUserToResizeRows = false;
		dgvRegisteredCDCCandidates.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCellsExceptHeader;
		tableLayoutPanel3.SetColumnSpan(dgvRegisteredCDCCandidates, 2);
		dgvRegisteredCDCCandidates.Location = new Point(961, 44);
		dgvRegisteredCDCCandidates.Name = "dgvRegisteredCDCCandidates";
		dgvRegisteredCDCCandidates.RowHeadersWidth = 10;
		tableLayoutPanel3.SetRowSpan(dgvRegisteredCDCCandidates, 4);
		dgvRegisteredCDCCandidates.ScrollBars = ScrollBars.Vertical;
		dgvRegisteredCDCCandidates.Size = new Size(392, 352);
		dgvRegisteredCDCCandidates.TabIndex = 2;
		dgvRegisteredCDCCandidates.RowsAdded += dgvRowCountChanged;
		dgvRegisteredCDCCandidates.RowsRemoved += dgvRowCountChanged;
		// 
		// btnMoveLeft
		// 
		btnMoveLeft.Font = new Font("Segoe UI", 14.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
		btnMoveLeft.Location = new Point(899, 260);
		btnMoveLeft.Name = "btnMoveLeft";
		btnMoveLeft.Size = new Size(56, 40);
		btnMoveLeft.TabIndex = 1;
		btnMoveLeft.Text = "<";
		btnMoveLeft.UseVisualStyleBackColor = true;
		btnMoveLeft.Click += btnMoveLeft_Click;
		// 
		// label2
		// 
		label2.AutoSize = true;
		tableLayoutPanel3.SetColumnSpan(label2, 5);
		label2.Dock = DockStyle.Fill;
		label2.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
		label2.ForeColor = Color.Blue;
		label2.Location = new Point(3, 0);
		label2.Name = "label2";
		label2.Size = new Size(1358, 41);
		label2.TabIndex = 5;
		label2.Text = "Choose the objects that require Change Data Capture (CDC) subscription.";
		label2.TextAlign = ContentAlignment.MiddleCenter;
		// 
		// lblDestinationList
		// 
		lblDestinationList.AutoSize = true;
		tableLayoutPanel3.SetColumnSpan(lblDestinationList, 2);
		lblDestinationList.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
		lblDestinationList.ForeColor = Color.Brown;
		lblDestinationList.Location = new Point(961, 428);
		lblDestinationList.Name = "lblDestinationList";
		lblDestinationList.Size = new Size(96, 21);
		lblDestinationList.TabIndex = 8;
		lblDestinationList.Text = "Placeholder";
		lblDestinationList.TextAlign = ContentAlignment.MiddleCenter;
		// 
		// grpPrimaryKey
		// 
		grpPrimaryKey.AccessibleRole = AccessibleRole.None;
		tableLayoutPanel3.SetColumnSpan(grpPrimaryKey, 2);
		grpPrimaryKey.Controls.Add(pictureBox1);
		grpPrimaryKey.Controls.Add(label3);
		grpPrimaryKey.Controls.Add(textBox1);
		grpPrimaryKey.Controls.Add(chkAddIdentityField);
		grpPrimaryKey.Controls.Add(btnCommitToDB);
		grpPrimaryKey.Controls.Add(btnClearDestination);
		grpPrimaryKey.Location = new Point(961, 491);
		grpPrimaryKey.Name = "grpPrimaryKey";
		tableLayoutPanel3.SetRowSpan(grpPrimaryKey, 2);
		grpPrimaryKey.Size = new Size(400, 90);
		grpPrimaryKey.TabIndex = 10;
		grpPrimaryKey.TabStop = false;
		// 
		// pictureBox1
		// 
		pictureBox1.Location = new Point(73, 89);
		pictureBox1.Name = "pictureBox1";
		pictureBox1.Size = new Size(100, 50);
		pictureBox1.TabIndex = 12;
		pictureBox1.TabStop = false;
		// 
		// label3
		// 
		label3.AutoSize = true;
		label3.Location = new Point(142, 12);
		label3.Name = "label3";
		label3.Size = new Size(60, 15);
		label3.TabIndex = 11;
		label3.Text = "Col.Name";
		// 
		// textBox1
		// 
		textBox1.AccessibleRole = AccessibleRole.None;
		textBox1.Location = new Point(208, 8);
		textBox1.Name = "textBox1";
		textBox1.Size = new Size(100, 23);
		textBox1.TabIndex = 10;
		textBox1.Text = "Id";
		// 
		// chkAddIdentityField
		// 
		chkAddIdentityField.AccessibleRole = AccessibleRole.None;
		chkAddIdentityField.AutoSize = true;
		chkAddIdentityField.Checked = true;
		chkAddIdentityField.CheckState = CheckState.Checked;
		chkAddIdentityField.Location = new Point(6, 10);
		chkAddIdentityField.Name = "chkAddIdentityField";
		chkAddIdentityField.Size = new Size(114, 19);
		chkAddIdentityField.TabIndex = 9;
		chkAddIdentityField.Text = "Add Primary Key";
		chkAddIdentityField.UseVisualStyleBackColor = true;
		// 
		// btnCommitToDB
		// 
		btnCommitToDB.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
		btnCommitToDB.Location = new Point(0, 35);
		btnCommitToDB.Name = "btnCommitToDB";
		btnCommitToDB.Size = new Size(173, 32);
		btnCommitToDB.TabIndex = 4;
		btnCommitToDB.Text = "Register";
		btnCommitToDB.UseVisualStyleBackColor = true;
		btnCommitToDB.Click += btnRegisterCDCCandidate;
		// 
		// btnClearDestination
		// 
		btnClearDestination.Location = new Point(208, 35);
		btnClearDestination.Name = "btnClearDestination";
		btnClearDestination.Size = new Size(86, 32);
		btnClearDestination.TabIndex = 3;
		btnClearDestination.Text = "Clear";
		btnClearDestination.UseVisualStyleBackColor = true;
		btnClearDestination.Click += btnClearDestination_Click;
		// 
		// lblSourceList
		// 
		lblSourceList.AutoSize = true;
		lblSourceList.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
		lblSourceList.ForeColor = Color.Brown;
		lblSourceList.Location = new Point(3, 488);
		lblSourceList.Name = "lblSourceList";
		lblSourceList.Size = new Size(96, 21);
		lblSourceList.TabIndex = 6;
		lblSourceList.Text = "Placeholder";
		lblSourceList.TextAlign = ContentAlignment.MiddleCenter;
		// 
		// pictureBox2
		// 
		pictureBox2.Image = Properties.Resources.CacheOk;
		pictureBox2.Location = new Point(3, 532);
		pictureBox2.Name = "pictureBox2";
		pictureBox2.Size = new Size(100, 50);
		pictureBox2.TabIndex = 12;
		pictureBox2.TabStop = false;
		pictureBox2.Click += pictureBox2_Click;
		// 
		// tbpPubSub
		// 
		tbpPubSub.Controls.Add(tableLayoutPanel1);
		tbpPubSub.Location = new Point(4, 24);
		tbpPubSub.Name = "tbpPubSub";
		tbpPubSub.Padding = new Padding(3);
		tbpPubSub.Size = new Size(1415, 678);
		tbpPubSub.TabIndex = 1;
		tbpPubSub.Text = "Pub/Sub";
		tbpPubSub.UseVisualStyleBackColor = true;
		// 
		// tableLayoutPanel1
		// 
		tableLayoutPanel1.ColumnCount = 2;
		tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28.17601F));
		tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 71.82399F));
		tableLayoutPanel1.Controls.Add(splitContainer1, 0, 1);
		tableLayoutPanel1.Controls.Add(tableLayoutPanel2, 0, 2);
		tableLayoutPanel1.Controls.Add(tableLayoutPanel4, 1, 2);
		tableLayoutPanel1.Controls.Add(grpFilterOptions, 0, 0);
		tableLayoutPanel1.Dock = DockStyle.Fill;
		tableLayoutPanel1.Location = new Point(3, 3);
		tableLayoutPanel1.Name = "tableLayoutPanel1";
		tableLayoutPanel1.RowCount = 4;
		tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 47F));
		tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 55.35445F));
		tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 44.64555F));
		tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 8F));
		tableLayoutPanel1.Size = new Size(1409, 672);
		tableLayoutPanel1.TabIndex = 0;
		// 
		// splitContainer1
		// 
		tableLayoutPanel1.SetColumnSpan(splitContainer1, 2);
		splitContainer1.Dock = DockStyle.Fill;
		splitContainer1.Location = new Point(3, 50);
		splitContainer1.Name = "splitContainer1";
		// 
		// splitContainer1.Panel1
		// 
		splitContainer1.Panel1.Controls.Add(splitContainer2);
		// 
		// splitContainer1.Panel2
		// 
		splitContainer1.Panel2.Controls.Add(dgvObject);
		splitContainer1.Panel2.Controls.Add(lblSelectedTable);
		splitContainer1.Size = new Size(1403, 335);
		splitContainer1.SplitterDistance = 392;
		splitContainer1.TabIndex = 6;
		// 
		// splitContainer2
		// 
		splitContainer2.Dock = DockStyle.Fill;
		splitContainer2.Location = new Point(0, 0);
		splitContainer2.Name = "splitContainer2";
		splitContainer2.Orientation = Orientation.Horizontal;
		// 
		// splitContainer2.Panel1
		// 
		splitContainer2.Panel1.Controls.Add(lbxObjects);
		splitContainer2.Panel1.Controls.Add(lblPanel1);
		// 
		// splitContainer2.Panel2
		// 
		splitContainer2.Panel2.Controls.Add(rtxFieldsJsonArray);
		splitContainer2.Size = new Size(392, 335);
		splitContainer2.SplitterDistance = 167;
		splitContainer2.TabIndex = 7;
		// 
		// lbxObjects
		// 
		lbxObjects.Dock = DockStyle.Fill;
		lbxObjects.FormattingEnabled = true;
		lbxObjects.Location = new Point(0, 21);
		lbxObjects.Name = "lbxObjects";
		lbxObjects.Size = new Size(392, 146);
		lbxObjects.TabIndex = 5;
		lbxObjects.SelectedIndexChanged += lbxObjects_SelectedIndexChanged;
		// 
		// lblPanel1
		// 
		lblPanel1.AutoSize = true;
		lblPanel1.Dock = DockStyle.Top;
		lblPanel1.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
		lblPanel1.Location = new Point(0, 0);
		lblPanel1.Name = "lblPanel1";
		lblPanel1.Size = new Size(149, 21);
		lblPanel1.TabIndex = 7;
		lblPanel1.Text = "CDC Subscriptions";
		lblPanel1.TextAlign = ContentAlignment.MiddleCenter;
		// 
		// rtxFieldsJsonArray
		// 
		rtxFieldsJsonArray.Location = new Point(0, 3);
		rtxFieldsJsonArray.Name = "rtxFieldsJsonArray";
		rtxFieldsJsonArray.Size = new Size(389, 158);
		rtxFieldsJsonArray.TabIndex = 0;
		rtxFieldsJsonArray.Text = "";
		// 
		// dgvObject
		// 
		dgvObject.AllowUserToAddRows = false;
		dgvObject.AllowUserToDeleteRows = false;
		dgvObject.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.ColumnHeader;
		dgvObject.ColumnHeadersHeight = 40;
		dgvObject.Dock = DockStyle.Fill;
		dgvObject.Location = new Point(0, 21);
		dgvObject.Name = "dgvObject";
		dgvObject.Size = new Size(1007, 314);
		dgvObject.TabIndex = 4;
		dgvObject.CellContentClick += dgvObject_CellContentClick_1;
		// 
		// lblSelectedTable
		// 
		lblSelectedTable.AutoSize = true;
		lblSelectedTable.Dock = DockStyle.Top;
		lblSelectedTable.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
		lblSelectedTable.Location = new Point(0, 0);
		lblSelectedTable.Name = "lblSelectedTable";
		lblSelectedTable.Size = new Size(136, 21);
		lblSelectedTable.TabIndex = 8;
		lblSelectedTable.Text = "lblSelectedTable";
		lblSelectedTable.TextAlign = ContentAlignment.TopCenter;
		// 
		// tableLayoutPanel2
		// 
		tableLayoutPanel2.ColumnCount = 2;
		tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45.58304F));
		tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 54.41696F));
		tableLayoutPanel2.Controls.Add(btnListEvents, 0, 1);
		tableLayoutPanel2.Controls.Add(btnSubscribe, 0, 4);
		tableLayoutPanel2.Controls.Add(button1, 1, 4);
		tableLayoutPanel2.Controls.Add(btnRegisterFields, 1, 0);
		tableLayoutPanel2.Controls.Add(btnDeleteCDCRegistration, 0, 0);
		tableLayoutPanel2.Location = new Point(3, 391);
		tableLayoutPanel2.Name = "tableLayoutPanel2";
		tableLayoutPanel2.RowCount = 5;
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 51.6129036F));
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 48.3870964F));
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 55F));
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
		tableLayoutPanel2.Size = new Size(391, 269);
		tableLayoutPanel2.TabIndex = 3;
		// 
		// btnListEvents
		// 
		btnListEvents.BackColor = Color.Green;
		btnListEvents.ForeColor = Color.Yellow;
		btnListEvents.Location = new Point(3, 60);
		btnListEvents.Name = "btnListEvents";
		btnListEvents.Size = new Size(155, 44);
		btnListEvents.TabIndex = 9;
		btnListEvents.Text = "List Subscriptions";
		btnListEvents.UseVisualStyleBackColor = false;
		btnListEvents.Click += btnListEvents_Click;
		// 
		// btnSubscribe
		// 
		btnSubscribe.Location = new Point(3, 221);
		btnSubscribe.Name = "btnSubscribe";
		btnSubscribe.Size = new Size(104, 44);
		btnSubscribe.TabIndex = 0;
		btnSubscribe.Text = "Subscribe";
		btnSubscribe.UseVisualStyleBackColor = true;
		btnSubscribe.Click += btnSubscribe_Click;
		// 
		// button1
		// 
		button1.Location = new Point(181, 221);
		button1.Name = "button1";
		button1.Size = new Size(145, 44);
		button1.TabIndex = 2;
		button1.Text = "Clear";
		button1.UseVisualStyleBackColor = true;
		// 
		// btnRegisterFields
		// 
		btnRegisterFields.BackColor = Color.Green;
		btnRegisterFields.ForeColor = Color.Yellow;
		btnRegisterFields.Location = new Point(181, 3);
		btnRegisterFields.Name = "btnRegisterFields";
		btnRegisterFields.Size = new Size(155, 44);
		btnRegisterFields.TabIndex = 7;
		btnRegisterFields.Text = "Subscribe";
		btnRegisterFields.UseVisualStyleBackColor = false;
		btnRegisterFields.Click += btnRegisterFields_Click;
		// 
		// btnDeleteCDCRegistration
		// 
		btnDeleteCDCRegistration.BackColor = Color.Green;
		btnDeleteCDCRegistration.ForeColor = Color.Yellow;
		btnDeleteCDCRegistration.Location = new Point(3, 3);
		btnDeleteCDCRegistration.Name = "btnDeleteCDCRegistration";
		btnDeleteCDCRegistration.Size = new Size(155, 44);
		btnDeleteCDCRegistration.TabIndex = 8;
		btnDeleteCDCRegistration.Text = "Delete Subscription";
		btnDeleteCDCRegistration.UseVisualStyleBackColor = false;
		btnDeleteCDCRegistration.Click += btnDeleteCDCSubscription_Click;
		// 
		// tableLayoutPanel4
		// 
		tableLayoutPanel4.ColumnCount = 1;
		tableLayoutPanel4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
		tableLayoutPanel4.Controls.Add(dgvRelations, 0, 1);
		tableLayoutPanel4.Controls.Add(lblRelations, 0, 0);
		tableLayoutPanel4.Dock = DockStyle.Fill;
		tableLayoutPanel4.Location = new Point(400, 391);
		tableLayoutPanel4.Name = "tableLayoutPanel4";
		tableLayoutPanel4.RowCount = 2;
		tableLayoutPanel4.RowStyles.Add(new RowStyle(SizeType.Percent, 11.8942728F));
		tableLayoutPanel4.RowStyles.Add(new RowStyle(SizeType.Percent, 88.10573F));
		tableLayoutPanel4.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
		tableLayoutPanel4.Size = new Size(1006, 269);
		tableLayoutPanel4.TabIndex = 7;
		// 
		// dgvRelations
		// 
		dgvRelations.AllowUserToAddRows = false;
		dgvRelations.AllowUserToDeleteRows = false;
		dgvRelations.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.ColumnHeader;
		dgvRelations.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
		dgvRelations.Dock = DockStyle.Fill;
		dgvRelations.Location = new Point(3, 34);
		dgvRelations.Name = "dgvRelations";
		dgvRelations.Size = new Size(1000, 232);
		dgvRelations.TabIndex = 10;
		// 
		// lblRelations
		// 
		lblRelations.AutoSize = true;
		lblRelations.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
		lblRelations.Location = new Point(3, 0);
		lblRelations.Name = "lblRelations";
		lblRelations.Size = new Size(101, 21);
		lblRelations.TabIndex = 9;
		lblRelations.Text = "lblRelations";
		lblRelations.TextAlign = ContentAlignment.TopCenter;
		// 
		// grpFilterOptions
		// 
		grpFilterOptions.Controls.Add(rbtFilterNone);
		grpFilterOptions.Controls.Add(rbtFilterSubscribed);
		grpFilterOptions.Location = new Point(3, 3);
		grpFilterOptions.Name = "grpFilterOptions";
		grpFilterOptions.Size = new Size(233, 41);
		grpFilterOptions.TabIndex = 6;
		grpFilterOptions.TabStop = false;
		grpFilterOptions.Text = "Filter";
		// 
		// rbtFilterNone
		// 
		rbtFilterNone.AutoSize = true;
		rbtFilterNone.Checked = true;
		rbtFilterNone.Location = new Point(6, 16);
		rbtFilterNone.Name = "rbtFilterNone";
		rbtFilterNone.Size = new Size(54, 19);
		rbtFilterNone.TabIndex = 4;
		rbtFilterNone.TabStop = true;
		rbtFilterNone.Text = "None";
		rbtFilterNone.UseVisualStyleBackColor = true;
		rbtFilterNone.CheckedChanged += filterChanged;
		// 
		// rbtFilterSubscribed
		// 
		rbtFilterSubscribed.AutoSize = true;
		rbtFilterSubscribed.Location = new Point(66, 16);
		rbtFilterSubscribed.Name = "rbtFilterSubscribed";
		rbtFilterSubscribed.Size = new Size(110, 19);
		rbtFilterSubscribed.TabIndex = 5;
		rbtFilterSubscribed.Text = "Only subscribed";
		rbtFilterSubscribed.UseVisualStyleBackColor = true;
		rbtFilterSubscribed.CheckedChanged += filterChanged;
		// 
		// tbpOAuth2
		// 
		tbpOAuth2.Controls.Add(btnGetTokenAsync);
		tbpOAuth2.Controls.Add(btnAuthenticate);
		tbpOAuth2.Controls.Add(txtResult);
		tbpOAuth2.Location = new Point(4, 24);
		tbpOAuth2.Name = "tbpOAuth2";
		tbpOAuth2.Padding = new Padding(3);
		tbpOAuth2.Size = new Size(1415, 678);
		tbpOAuth2.TabIndex = 0;
		tbpOAuth2.Text = "OAuth2";
		tbpOAuth2.UseVisualStyleBackColor = true;
		// 
		// tbpSOQL
		// 
		tbpSOQL.Controls.Add(tableLayoutPanel11);
		tbpSOQL.Location = new Point(4, 24);
		tbpSOQL.Name = "tbpSOQL";
		tbpSOQL.Size = new Size(1415, 678);
		tbpSOQL.TabIndex = 6;
		tbpSOQL.Text = "SOQL";
		tbpSOQL.UseVisualStyleBackColor = true;
		// 
		// tableLayoutPanel11
		// 
		tableLayoutPanel11.ColumnCount = 3;
		tableLayoutPanel11.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 39.5882835F));
		tableLayoutPanel11.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60.4117165F));
		tableLayoutPanel11.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 151F));
		tableLayoutPanel11.Controls.Add(splitcSoql, 0, 1);
		tableLayoutPanel11.Controls.Add(lblSoqlText, 1, 0);
		tableLayoutPanel11.Controls.Add(tableLayoutPanel15, 0, 0);
		tableLayoutPanel11.Dock = DockStyle.Fill;
		tableLayoutPanel11.Location = new Point(0, 0);
		tableLayoutPanel11.Name = "tableLayoutPanel11";
		tableLayoutPanel11.RowCount = 2;
		tableLayoutPanel11.RowStyles.Add(new RowStyle(SizeType.Percent, 12.0943956F));
		tableLayoutPanel11.RowStyles.Add(new RowStyle(SizeType.Percent, 87.9056F));
		tableLayoutPanel11.Size = new Size(1415, 678);
		tableLayoutPanel11.TabIndex = 0;
		// 
		// splitcSoql
		// 
		tableLayoutPanel11.SetColumnSpan(splitcSoql, 4);
		splitcSoql.Location = new Point(3, 85);
		splitcSoql.Name = "splitcSoql";
		// 
		// splitcSoql.Panel1
		// 
		splitcSoql.Panel1.Controls.Add(tableLayoutPanel12);
		// 
		// splitcSoql.Panel2
		// 
		splitcSoql.Panel2.Controls.Add(tableLayoutPanel13);
		splitcSoql.Size = new Size(1368, 590);
		splitcSoql.SplitterDistance = 781;
		splitcSoql.TabIndex = 4;
		// 
		// tableLayoutPanel12
		// 
		tableLayoutPanel12.ColumnCount = 3;
		tableLayoutPanel12.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 79.59731F));
		tableLayoutPanel12.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20.4026852F));
		tableLayoutPanel12.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 105F));
		tableLayoutPanel12.Controls.Add(rtSoqlQuery, 0, 0);
		tableLayoutPanel12.Controls.Add(btnDeleteSoql, 1, 1);
		tableLayoutPanel12.Controls.Add(btnSaveSoql, 1, 0);
		tableLayoutPanel12.Controls.Add(tableLayoutPanel16, 1, 2);
		tableLayoutPanel12.Controls.Add(button30, 1, 3);
		tableLayoutPanel12.Controls.Add(button31, 2, 3);
		tableLayoutPanel12.Dock = DockStyle.Fill;
		tableLayoutPanel12.Location = new Point(0, 0);
		tableLayoutPanel12.Name = "tableLayoutPanel12";
		tableLayoutPanel12.RowCount = 5;
		tableLayoutPanel12.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
		tableLayoutPanel12.RowStyles.Add(new RowStyle(SizeType.Absolute, 43F));
		tableLayoutPanel12.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
		tableLayoutPanel12.RowStyles.Add(new RowStyle(SizeType.Absolute, 47F));
		tableLayoutPanel12.RowStyles.Add(new RowStyle(SizeType.Absolute, 421F));
		tableLayoutPanel12.Size = new Size(781, 590);
		tableLayoutPanel12.TabIndex = 3;
		// 
		// rtSoqlQuery
		// 
		rtSoqlQuery.Dock = DockStyle.Fill;
		rtSoqlQuery.Location = new Point(3, 3);
		rtSoqlQuery.Name = "rtSoqlQuery";
		tableLayoutPanel12.SetRowSpan(rtSoqlQuery, 5);
		rtSoqlQuery.Size = new Size(532, 584);
		rtSoqlQuery.TabIndex = 2;
		rtSoqlQuery.Text = "";
		// 
		// btnDeleteSoql
		// 
		btnDeleteSoql.Location = new Point(541, 40);
		btnDeleteSoql.Name = "btnDeleteSoql";
		btnDeleteSoql.Size = new Size(120, 33);
		btnDeleteSoql.TabIndex = 3;
		btnDeleteSoql.Text = "Delete";
		btnDeleteSoql.UseVisualStyleBackColor = true;
		btnDeleteSoql.Click += btnDeleteSoql_Click;
		// 
		// btnSaveSoql
		// 
		btnSaveSoql.Location = new Point(541, 3);
		btnSaveSoql.Name = "btnSaveSoql";
		btnSaveSoql.Size = new Size(120, 31);
		btnSaveSoql.TabIndex = 4;
		btnSaveSoql.Text = "Save";
		btnSaveSoql.UseVisualStyleBackColor = true;
		btnSaveSoql.Click += btnSaveSoql_Click;
		// 
		// tableLayoutPanel16
		// 
		tableLayoutPanel16.ColumnCount = 2;
		tableLayoutPanel12.SetColumnSpan(tableLayoutPanel16, 2);
		tableLayoutPanel16.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
		tableLayoutPanel16.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
		tableLayoutPanel16.Controls.Add(btnExecSoql, 1, 0);
		tableLayoutPanel16.Controls.Add(chkUseTooling, 0, 0);
		tableLayoutPanel16.Location = new Point(541, 83);
		tableLayoutPanel16.Name = "tableLayoutPanel16";
		tableLayoutPanel16.RowCount = 1;
		tableLayoutPanel16.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
		tableLayoutPanel16.Size = new Size(200, 36);
		tableLayoutPanel16.TabIndex = 7;
		// 
		// btnExecSoql
		// 
		btnExecSoql.Location = new Point(103, 3);
		btnExecSoql.Name = "btnExecSoql";
		btnExecSoql.Size = new Size(94, 30);
		btnExecSoql.TabIndex = 5;
		btnExecSoql.Text = "Execute";
		btnExecSoql.UseVisualStyleBackColor = true;
		btnExecSoql.Click += btnExecSoql_Click;
		// 
		// chkUseTooling
		// 
		chkUseTooling.AutoSize = true;
		chkUseTooling.Location = new Point(3, 3);
		chkUseTooling.Name = "chkUseTooling";
		chkUseTooling.Size = new Size(94, 19);
		chkUseTooling.TabIndex = 6;
		chkUseTooling.Text = "Use Tooling ?";
		chkUseTooling.UseVisualStyleBackColor = true;
		// 
		// button30
		// 
		button30.Location = new Point(541, 125);
		button30.Name = "button30";
		button30.Size = new Size(120, 38);
		button30.TabIndex = 8;
		button30.Text = "Describe tooling object";
		button30.UseVisualStyleBackColor = true;
		button30.Click += button30_Click;
		// 
		// button31
		// 
		button31.Location = new Point(678, 125);
		button31.Name = "button31";
		button31.Size = new Size(100, 38);
		button31.TabIndex = 9;
		button31.Text = "x";
		button31.UseVisualStyleBackColor = true;
		button31.Click += button31_Click;
		// 
		// tableLayoutPanel13
		// 
		tableLayoutPanel13.ColumnCount = 2;
		tableLayoutPanel13.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		tableLayoutPanel13.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 199F));
		tableLayoutPanel13.Controls.Add(dgvSOQLResult, 0, 0);
		tableLayoutPanel13.Controls.Add(tableLayoutPanel14, 1, 0);
		tableLayoutPanel13.Location = new Point(2, 0);
		tableLayoutPanel13.Name = "tableLayoutPanel13";
		tableLayoutPanel13.RowCount = 1;
		tableLayoutPanel13.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
		tableLayoutPanel13.Size = new Size(625, 590);
		tableLayoutPanel13.TabIndex = 6;
		// 
		// dgvSOQLResult
		// 
		dgvSOQLResult.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
		dgvSOQLResult.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
		dgvSOQLResult.Dock = DockStyle.Fill;
		dgvSOQLResult.Location = new Point(3, 3);
		dgvSOQLResult.Name = "dgvSOQLResult";
		dgvSOQLResult.Size = new Size(420, 584);
		dgvSOQLResult.TabIndex = 0;
		dgvSOQLResult.CellContentClick += dgvSOQLResult_CellContentClick;
		// 
		// tableLayoutPanel14
		// 
		tableLayoutPanel14.ColumnCount = 1;
		tableLayoutPanel14.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
		tableLayoutPanel14.Controls.Add(btnSoqlRDelete, 0, 2);
		tableLayoutPanel14.Controls.Add(btnSoqlSave, 0, 0);
		tableLayoutPanel14.Location = new Point(429, 3);
		tableLayoutPanel14.Name = "tableLayoutPanel14";
		tableLayoutPanel14.RowCount = 3;
		tableLayoutPanel14.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
		tableLayoutPanel14.RowStyles.Add(new RowStyle(SizeType.Absolute, 55F));
		tableLayoutPanel14.RowStyles.Add(new RowStyle(SizeType.Absolute, 55F));
		tableLayoutPanel14.Size = new Size(114, 160);
		tableLayoutPanel14.TabIndex = 1;
		// 
		// btnSoqlRDelete
		// 
		btnSoqlRDelete.Location = new Point(3, 108);
		btnSoqlRDelete.Name = "btnSoqlRDelete";
		btnSoqlRDelete.Size = new Size(108, 44);
		btnSoqlRDelete.TabIndex = 2;
		btnSoqlRDelete.Text = "Delete";
		btnSoqlRDelete.UseVisualStyleBackColor = true;
		btnSoqlRDelete.Click += btnSoqlRDelete_Click;
		// 
		// btnSoqlSave
		// 
		btnSoqlSave.Location = new Point(3, 3);
		btnSoqlSave.Name = "btnSoqlSave";
		btnSoqlSave.Size = new Size(108, 44);
		btnSoqlSave.TabIndex = 0;
		btnSoqlSave.Text = "Save";
		btnSoqlSave.UseVisualStyleBackColor = true;
		btnSoqlSave.Click += btnSObjectSave;
		// 
		// lblSoqlText
		// 
		lblSoqlText.AutoSize = true;
		lblSoqlText.Dock = DockStyle.Fill;
		lblSoqlText.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
		lblSoqlText.ForeColor = Color.Brown;
		lblSoqlText.Location = new Point(503, 0);
		lblSoqlText.Name = "lblSoqlText";
		lblSoqlText.Size = new Size(757, 82);
		lblSoqlText.TabIndex = 5;
		lblSoqlText.Text = "soql";
		// 
		// tableLayoutPanel15
		// 
		tableLayoutPanel15.ColumnCount = 2;
		tableLayoutPanel15.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 77.1255F));
		tableLayoutPanel15.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22.8744946F));
		tableLayoutPanel15.Controls.Add(btnBuildSelect, 1, 0);
		tableLayoutPanel15.Controls.Add(tableLayoutPanel17, 0, 0);
		tableLayoutPanel15.Location = new Point(3, 3);
		tableLayoutPanel15.Name = "tableLayoutPanel15";
		tableLayoutPanel15.RowCount = 1;
		tableLayoutPanel15.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
		tableLayoutPanel15.Size = new Size(494, 76);
		tableLayoutPanel15.TabIndex = 6;
		// 
		// btnBuildSelect
		// 
		btnBuildSelect.Location = new Point(384, 3);
		btnBuildSelect.Name = "btnBuildSelect";
		btnBuildSelect.Size = new Size(107, 44);
		btnBuildSelect.TabIndex = 1;
		btnBuildSelect.Text = "Build Select";
		btnBuildSelect.UseVisualStyleBackColor = true;
		btnBuildSelect.Click += btnBuildSelect_Click;
		// 
		// tableLayoutPanel17
		// 
		tableLayoutPanel17.ColumnCount = 1;
		tableLayoutPanel17.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
		tableLayoutPanel17.Controls.Add(lblSOQLRowCount, 0, 1);
		tableLayoutPanel17.Controls.Add(cmbSOQL, 0, 0);
		tableLayoutPanel17.Location = new Point(3, 3);
		tableLayoutPanel17.Name = "tableLayoutPanel17";
		tableLayoutPanel17.RowCount = 2;
		tableLayoutPanel17.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
		tableLayoutPanel17.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
		tableLayoutPanel17.Size = new Size(375, 70);
		tableLayoutPanel17.TabIndex = 7;
		// 
		// lblSOQLRowCount
		// 
		lblSOQLRowCount.AutoSize = true;
		lblSOQLRowCount.Dock = DockStyle.Fill;
		lblSOQLRowCount.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
		lblSOQLRowCount.ForeColor = Color.Brown;
		lblSOQLRowCount.Location = new Point(3, 35);
		lblSOQLRowCount.Name = "lblSOQLRowCount";
		lblSOQLRowCount.Size = new Size(369, 35);
		lblSOQLRowCount.TabIndex = 6;
		lblSOQLRowCount.Text = "soql";
		// 
		// cmbSOQL
		// 
		cmbSOQL.Anchor = AnchorStyles.Left;
		cmbSOQL.FormattingEnabled = true;
		cmbSOQL.Location = new Point(3, 6);
		cmbSOQL.Name = "cmbSOQL";
		cmbSOQL.Size = new Size(369, 23);
		cmbSOQL.TabIndex = 0;
		cmbSOQL.SelectedIndexChanged += cmbSOQL_SelectedIndexChanged;
		// 
		// tbpDescribeObject
		// 
		tbpDescribeObject.Controls.Add(tableLayoutPanel5);
		tbpDescribeObject.Location = new Point(4, 24);
		tbpDescribeObject.Name = "tbpDescribeObject";
		tbpDescribeObject.Size = new Size(1415, 678);
		tbpDescribeObject.TabIndex = 3;
		tbpDescribeObject.Text = "Describe Object";
		tbpDescribeObject.UseVisualStyleBackColor = true;
		// 
		// tableLayoutPanel5
		// 
		tableLayoutPanel5.ColumnCount = 5;
		tableLayoutPanel5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 78.14685F));
		tableLayoutPanel5.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 570F));
		tableLayoutPanel5.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 197F));
		tableLayoutPanel5.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 119F));
		tableLayoutPanel5.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 424F));
		tableLayoutPanel5.Controls.Add(btnDeleteCmbObjectSelected, 3, 0);
		tableLayoutPanel5.Controls.Add(label1, 0, 0);
		tableLayoutPanel5.Controls.Add(dgvSchema, 0, 2);
		tableLayoutPanel5.Controls.Add(btnDescribe, 2, 0);
		tableLayoutPanel5.Controls.Add(cmbObjects, 1, 0);
		tableLayoutPanel5.Controls.Add(lblCDCName, 1, 1);
		tableLayoutPanel5.Dock = DockStyle.Fill;
		tableLayoutPanel5.Location = new Point(0, 0);
		tableLayoutPanel5.Name = "tableLayoutPanel5";
		tableLayoutPanel5.RowCount = 3;
		tableLayoutPanel5.RowStyles.Add(new RowStyle(SizeType.Percent, 53.3333321F));
		tableLayoutPanel5.RowStyles.Add(new RowStyle(SizeType.Percent, 46.6666679F));
		tableLayoutPanel5.RowStyles.Add(new RowStyle(SizeType.Absolute, 617F));
		tableLayoutPanel5.Size = new Size(1415, 678);
		tableLayoutPanel5.TabIndex = 0;
		// 
		// btnDeleteCmbObjectSelected
		// 
		btnDeleteCmbObjectSelected.Location = new Point(874, 3);
		btnDeleteCmbObjectSelected.Name = "btnDeleteCmbObjectSelected";
		btnDeleteCmbObjectSelected.Size = new Size(113, 23);
		btnDeleteCmbObjectSelected.TabIndex = 6;
		btnDeleteCmbObjectSelected.Text = "Remove Object";
		btnDeleteCmbObjectSelected.UseVisualStyleBackColor = true;
		btnDeleteCmbObjectSelected.Click += btnDeleteCmbObjectSelected_Click;
		// 
		// label1
		// 
		label1.AutoSize = true;
		label1.Dock = DockStyle.Fill;
		label1.Location = new Point(3, 0);
		label1.Name = "label1";
		label1.Size = new Size(98, 32);
		label1.TabIndex = 0;
		label1.Text = "Object Name";
		label1.TextAlign = ContentAlignment.MiddleLeft;
		// 
		// dgvSchema
		// 
		dgvSchema.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
		tableLayoutPanel5.SetColumnSpan(dgvSchema, 5);
		dgvSchema.Dock = DockStyle.Fill;
		dgvSchema.Location = new Point(3, 63);
		dgvSchema.Name = "dgvSchema";
		dgvSchema.Size = new Size(1409, 612);
		dgvSchema.TabIndex = 3;
		// 
		// btnDescribe
		// 
		btnDescribe.Location = new Point(677, 3);
		btnDescribe.Name = "btnDescribe";
		btnDescribe.Size = new Size(75, 23);
		btnDescribe.TabIndex = 2;
		btnDescribe.Text = "Describe";
		btnDescribe.UseVisualStyleBackColor = true;
		btnDescribe.Click += btnDescribe_Click;
		// 
		// cmbObjects
		// 
		cmbObjects.FormattingEnabled = true;
		cmbObjects.Location = new Point(107, 3);
		cmbObjects.Name = "cmbObjects";
		cmbObjects.Size = new Size(564, 23);
		cmbObjects.TabIndex = 5;
		cmbObjects.SelectedIndexChanged += cmbObjects_SelectedIndexChanged;
		cmbObjects.Validated += cmbObjects_Validated;
		// 
		// lblCDCName
		// 
		lblCDCName.AutoSize = true;
		lblCDCName.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
		lblCDCName.ForeColor = Color.IndianRed;
		lblCDCName.Location = new Point(104, 32);
		lblCDCName.Margin = new Padding(0);
		lblCDCName.Name = "lblCDCName";
		lblCDCName.Size = new Size(40, 15);
		lblCDCName.TabIndex = 7;
		lblCDCName.Text = "label4";
		// 
		// tbpEventLog
		// 
		tbpEventLog.Controls.Add(splitContainer3);
		tbpEventLog.Location = new Point(4, 24);
		tbpEventLog.Name = "tbpEventLog";
		tbpEventLog.Size = new Size(1415, 678);
		tbpEventLog.TabIndex = 4;
		tbpEventLog.Text = "Event Log";
		tbpEventLog.UseVisualStyleBackColor = true;
		// 
		// splitContainer3
		// 
		splitContainer3.Dock = DockStyle.Fill;
		splitContainer3.Location = new Point(0, 0);
		splitContainer3.Name = "splitContainer3";
		// 
		// splitContainer3.Panel1
		// 
		splitContainer3.Panel1.Controls.Add(tableLayoutPanel6);
		// 
		// splitContainer3.Panel2
		// 
		splitContainer3.Panel2.Controls.Add(rtfLog);
		splitContainer3.Size = new Size(1415, 678);
		splitContainer3.SplitterDistance = 1338;
		splitContainer3.TabIndex = 0;
		// 
		// tableLayoutPanel6
		// 
		tableLayoutPanel6.ColumnCount = 1;
		tableLayoutPanel6.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
		tableLayoutPanel6.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
		tableLayoutPanel6.Controls.Add(tableLayoutPanel7, 0, 1);
		tableLayoutPanel6.Controls.Add(rtxLog, 0, 0);
		tableLayoutPanel6.Dock = DockStyle.Fill;
		tableLayoutPanel6.Location = new Point(0, 0);
		tableLayoutPanel6.Name = "tableLayoutPanel6";
		tableLayoutPanel6.RowCount = 2;
		tableLayoutPanel6.RowStyles.Add(new RowStyle(SizeType.Percent, 90.26549F));
		tableLayoutPanel6.RowStyles.Add(new RowStyle(SizeType.Percent, 9.734513F));
		tableLayoutPanel6.Size = new Size(1338, 678);
		tableLayoutPanel6.TabIndex = 0;
		// 
		// tableLayoutPanel7
		// 
		tableLayoutPanel7.ColumnCount = 2;
		tableLayoutPanel7.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
		tableLayoutPanel7.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
		tableLayoutPanel7.Controls.Add(btnLogTest, 1, 0);
		tableLayoutPanel7.Controls.Add(btnClearLog, 0, 0);
		tableLayoutPanel7.Location = new Point(3, 615);
		tableLayoutPanel7.Name = "tableLayoutPanel7";
		tableLayoutPanel7.RowCount = 2;
		tableLayoutPanel7.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
		tableLayoutPanel7.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
		tableLayoutPanel7.Size = new Size(200, 60);
		tableLayoutPanel7.TabIndex = 0;
		// 
		// btnClearLog
		// 
		btnClearLog.Location = new Point(3, 3);
		btnClearLog.Name = "btnClearLog";
		btnClearLog.Size = new Size(93, 23);
		btnClearLog.TabIndex = 0;
		btnClearLog.Text = "Clear log";
		btnClearLog.UseVisualStyleBackColor = true;
		btnClearLog.Click += btnClearLog_Click;
		// 
		// rtxLog
		// 
		rtxLog.BackColor = Color.Black;
		rtxLog.Dock = DockStyle.Fill;
		rtxLog.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
		rtxLog.ForeColor = Color.Lime;
		rtxLog.Location = new Point(3, 3);
		rtxLog.Name = "rtxLog";
		rtxLog.Size = new Size(1332, 606);
		rtxLog.TabIndex = 1;
		rtxLog.Text = "";
		// 
		// rtfLog
		// 
		rtfLog.Location = new Point(289, 0);
		rtfLog.Name = "rtfLog";
		rtfLog.Size = new Size(360, 678);
		rtfLog.TabIndex = 0;
		rtfLog.Text = "";
		// 
		// tbpCDCEvents
		// 
		tbpCDCEvents.Controls.Add(tableLayoutPanel8);
		tbpCDCEvents.Location = new Point(4, 24);
		tbpCDCEvents.Name = "tbpCDCEvents";
		tbpCDCEvents.Size = new Size(1415, 678);
		tbpCDCEvents.TabIndex = 5;
		tbpCDCEvents.Text = "CDC Events";
		tbpCDCEvents.UseVisualStyleBackColor = true;
		// 
		// tableLayoutPanel8
		// 
		tableLayoutPanel8.ColumnCount = 1;
		tableLayoutPanel8.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 73.5689F));
		tableLayoutPanel8.Controls.Add(splitContainer4, 0, 0);
		tableLayoutPanel8.Controls.Add(tableLayoutPanel9, 0, 1);
		tableLayoutPanel8.Controls.Add(lblCDCStatus, 0, 2);
		tableLayoutPanel8.Dock = DockStyle.Fill;
		tableLayoutPanel8.Location = new Point(0, 0);
		tableLayoutPanel8.Name = "tableLayoutPanel8";
		tableLayoutPanel8.RowCount = 3;
		tableLayoutPanel8.RowStyles.Add(new RowStyle(SizeType.Percent, 78.46608F));
		tableLayoutPanel8.RowStyles.Add(new RowStyle(SizeType.Percent, 21.5339241F));
		tableLayoutPanel8.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F));
		tableLayoutPanel8.Size = new Size(1415, 678);
		tableLayoutPanel8.TabIndex = 0;
		// 
		// splitContainer4
		// 
		splitContainer4.Dock = DockStyle.Fill;
		splitContainer4.Location = new Point(3, 3);
		splitContainer4.Name = "splitContainer4";
		// 
		// splitContainer4.Panel1
		// 
		splitContainer4.Panel1.Controls.Add(lbxCDCTopics);
		// 
		// splitContainer4.Panel2
		// 
		splitContainer4.Panel2.Controls.Add(splitContainer5);
		splitContainer4.Size = new Size(1409, 478);
		splitContainer4.SplitterDistance = 525;
		splitContainer4.TabIndex = 0;
		// 
		// lbxCDCTopics
		// 
		lbxCDCTopics.Dock = DockStyle.Fill;
		lbxCDCTopics.FormattingEnabled = true;
		lbxCDCTopics.HorizontalScrollbar = true;
		lbxCDCTopics.Location = new Point(0, 0);
		lbxCDCTopics.Name = "lbxCDCTopics";
		lbxCDCTopics.ScrollAlwaysVisible = true;
		lbxCDCTopics.Size = new Size(525, 478);
		lbxCDCTopics.TabIndex = 1;
		// 
		// splitContainer5
		// 
		splitContainer5.Dock = DockStyle.Fill;
		splitContainer5.Location = new Point(0, 0);
		splitContainer5.Name = "splitContainer5";
		// 
		// splitContainer5.Panel1
		// 
		splitContainer5.Panel1.Controls.Add(lbxCDCEvents);
		// 
		// splitContainer5.Panel2
		// 
		splitContainer5.Panel2.Controls.Add(dgvFilteredFields);
		splitContainer5.Size = new Size(880, 478);
		splitContainer5.SplitterDistance = 401;
		splitContainer5.TabIndex = 0;
		// 
		// lbxCDCEvents
		// 
		lbxCDCEvents.Dock = DockStyle.Fill;
		lbxCDCEvents.FormattingEnabled = true;
		lbxCDCEvents.HorizontalScrollbar = true;
		lbxCDCEvents.Location = new Point(0, 0);
		lbxCDCEvents.Name = "lbxCDCEvents";
		lbxCDCEvents.ScrollAlwaysVisible = true;
		lbxCDCEvents.Size = new Size(401, 478);
		lbxCDCEvents.TabIndex = 0;
		// 
		// dgvFilteredFields
		// 
		dgvFilteredFields.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
		dgvFilteredFields.Dock = DockStyle.Fill;
		dgvFilteredFields.Location = new Point(0, 0);
		dgvFilteredFields.Name = "dgvFilteredFields";
		dgvFilteredFields.Size = new Size(475, 478);
		dgvFilteredFields.TabIndex = 0;
		// 
		// tableLayoutPanel9
		// 
		tableLayoutPanel9.ColumnCount = 3;
		tableLayoutPanel9.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
		tableLayoutPanel9.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
		tableLayoutPanel9.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 235F));
		tableLayoutPanel9.Controls.Add(btnGetCDCSubscriptions, 1, 0);
		tableLayoutPanel9.Controls.Add(btnCDCStartSubscription, 0, 0);
		tableLayoutPanel9.Location = new Point(3, 487);
		tableLayoutPanel9.Name = "tableLayoutPanel9";
		tableLayoutPanel9.RowCount = 1;
		tableLayoutPanel9.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
		tableLayoutPanel9.Size = new Size(695, 41);
		tableLayoutPanel9.TabIndex = 1;
		// 
		// btnGetCDCSubscriptions
		// 
		btnGetCDCSubscriptions.Location = new Point(233, 3);
		btnGetCDCSubscriptions.Name = "btnGetCDCSubscriptions";
		btnGetCDCSubscriptions.Size = new Size(185, 35);
		btnGetCDCSubscriptions.TabIndex = 1;
		btnGetCDCSubscriptions.Text = "get CDC Subscriptions";
		btnGetCDCSubscriptions.UseVisualStyleBackColor = true;
		btnGetCDCSubscriptions.Click += btnGetCDCSubscriptions_Click;
		// 
		// btnCDCStartSubscription
		// 
		btnCDCStartSubscription.Location = new Point(3, 3);
		btnCDCStartSubscription.Name = "btnCDCStartSubscription";
		btnCDCStartSubscription.Size = new Size(185, 35);
		btnCDCStartSubscription.TabIndex = 0;
		btnCDCStartSubscription.Text = "Start Subscription";
		btnCDCStartSubscription.UseVisualStyleBackColor = true;
		btnCDCStartSubscription.Click += btnCDCStartSubscription_Click;
		// 
		// lblCDCStatus
		// 
		lblCDCStatus.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
		lblCDCStatus.AutoSize = true;
		lblCDCStatus.Location = new Point(3, 617);
		lblCDCStatus.Name = "lblCDCStatus";
		lblCDCStatus.Size = new Size(66, 61);
		lblCDCStatus.TabIndex = 2;
		lblCDCStatus.Text = "CDC Status";
		lblCDCStatus.TextAlign = ContentAlignment.MiddleLeft;
		// 
		// tbpX12
		// 
		tbpX12.Controls.Add(tableLayoutPanel18);
		tbpX12.Location = new Point(4, 24);
		tbpX12.Name = "tbpX12";
		tbpX12.Padding = new Padding(3);
		tbpX12.Size = new Size(1415, 678);
		tbpX12.TabIndex = 7;
		tbpX12.Text = "X12";
		tbpX12.UseVisualStyleBackColor = true;
		// 
		// tableLayoutPanel18
		// 
		tableLayoutPanel18.ColumnCount = 3;
		tableLayoutPanel18.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 815F));
		tableLayoutPanel18.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		tableLayoutPanel18.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
		tableLayoutPanel18.Controls.Add(cmbCDCTables, 0, 0);
		tableLayoutPanel18.Controls.Add(grpx, 1, 0);
		tableLayoutPanel18.Controls.Add(splitContainer7, 0, 2);
		tableLayoutPanel18.Dock = DockStyle.Fill;
		tableLayoutPanel18.Location = new Point(3, 3);
		tableLayoutPanel18.Name = "tableLayoutPanel18";
		tableLayoutPanel18.RowCount = 4;
		tableLayoutPanel18.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
		tableLayoutPanel18.RowStyles.Add(new RowStyle(SizeType.Absolute, 31F));
		tableLayoutPanel18.RowStyles.Add(new RowStyle(SizeType.Absolute, 602F));
		tableLayoutPanel18.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
		tableLayoutPanel18.Size = new Size(1409, 672);
		tableLayoutPanel18.TabIndex = 0;
		// 
		// cmbCDCTables
		// 
		cmbCDCTables.FormattingEnabled = true;
		cmbCDCTables.Location = new Point(3, 3);
		cmbCDCTables.Name = "cmbCDCTables";
		cmbCDCTables.Size = new Size(400, 23);
		cmbCDCTables.TabIndex = 1;
		cmbCDCTables.SelectedIndexChanged += cmbCDCTables_SelectedIndexChanged;
		// 
		// grpx
		// 
		grpx.Controls.Add(radioButton3);
		grpx.Controls.Add(radioButton2);
		grpx.Controls.Add(radioButton1);
		grpx.Location = new Point(815, 0);
		grpx.Margin = new Padding(0);
		grpx.Name = "grpx";
		grpx.Padding = new Padding(0);
		grpx.Size = new Size(574, 30);
		grpx.TabIndex = 3;
		grpx.TabStop = false;
		// 
		// radioButton3
		// 
		radioButton3.AutoSize = true;
		radioButton3.Location = new Point(12, 8);
		radioButton3.Name = "radioButton3";
		radioButton3.Size = new Size(43, 19);
		radioButton3.TabIndex = 2;
		radioButton3.Text = "997";
		radioButton3.UseVisualStyleBackColor = true;
		radioButton3.CheckedChanged += grpDocChanged;
		// 
		// radioButton2
		// 
		radioButton2.AutoSize = true;
		radioButton2.Checked = true;
		radioButton2.Location = new Point(110, 8);
		radioButton2.Name = "radioButton2";
		radioButton2.Size = new Size(43, 19);
		radioButton2.TabIndex = 1;
		radioButton2.TabStop = true;
		radioButton2.Text = "850";
		radioButton2.UseVisualStyleBackColor = true;
		radioButton2.CheckedChanged += grpDocChanged;
		// 
		// radioButton1
		// 
		radioButton1.AutoSize = true;
		radioButton1.Location = new Point(61, 8);
		radioButton1.Name = "radioButton1";
		radioButton1.Size = new Size(43, 19);
		radioButton1.TabIndex = 0;
		radioButton1.Text = "810";
		radioButton1.UseVisualStyleBackColor = true;
		radioButton1.CheckedChanged += grpDocChanged;
		// 
		// splitContainer7
		// 
		splitContainer7.Location = new Point(3, 64);
		splitContainer7.Name = "splitContainer7";
		splitContainer7.Orientation = Orientation.Horizontal;
		// 
		// splitContainer7.Panel1
		// 
		splitContainer7.Panel1.Controls.Add(splitContainer6);
		splitContainer7.Size = new Size(809, 501);
		splitContainer7.SplitterDistance = 446;
		splitContainer7.TabIndex = 4;
		// 
		// splitContainer6
		// 
		splitContainer6.Dock = DockStyle.Fill;
		splitContainer6.Location = new Point(0, 0);
		splitContainer6.Name = "splitContainer6";
		// 
		// splitContainer6.Panel1
		// 
		splitContainer6.Panel1.Controls.Add(dgvCDCTables);
		splitContainer6.Size = new Size(809, 446);
		splitContainer6.SplitterDistance = 496;
		splitContainer6.TabIndex = 0;
		// 
		// dgvCDCTables
		// 
		dgvCDCTables.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
		dgvCDCTables.Dock = DockStyle.Fill;
		dgvCDCTables.Location = new Point(0, 0);
		dgvCDCTables.Name = "dgvCDCTables";
		dgvCDCTables.Size = new Size(496, 446);
		dgvCDCTables.TabIndex = 0;
		// 
		// btnLogTest
		// 
		btnLogTest.Location = new Point(103, 3);
		btnLogTest.Name = "btnLogTest";
		btnLogTest.Size = new Size(93, 23);
		btnLogTest.TabIndex = 1;
		btnLogTest.Text = "Log Test";
		btnLogTest.UseVisualStyleBackColor = true;
		btnLogTest.Click += btnLogTest_Click;
		// 
		// MainForm
		// 
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		BackColor = Color.FromArgb(24, 26, 27);
		ClientSize = new Size(1423, 728);
		Controls.Add(tabControl1);
		Controls.Add(statusStrip1);
		Margin = new Padding(4, 3, 4, 3);
		Name = "MainForm";
		Text = "Salesforce OAuth2 Authentication";
		FormClosing += Form1_FormClosing;
		Load += Form1_Load;
		statusStrip1.ResumeLayout(false);
		statusStrip1.PerformLayout();
		tabControl1.ResumeLayout(false);
		tbpSfObjects.ResumeLayout(false);
		tableLayoutPanel3.ResumeLayout(false);
		tableLayoutPanel3.PerformLayout();
		((System.ComponentModel.ISupportInitialize)dgvCDCEnabledObjects).EndInit();
		tableLayoutPanel10.ResumeLayout(false);
		((System.ComponentModel.ISupportInitialize)dgvRegisteredCDCCandidates).EndInit();
		grpPrimaryKey.ResumeLayout(false);
		grpPrimaryKey.PerformLayout();
		((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
		((System.ComponentModel.ISupportInitialize)pictureBox2).EndInit();
		tbpPubSub.ResumeLayout(false);
		tableLayoutPanel1.ResumeLayout(false);
		splitContainer1.Panel1.ResumeLayout(false);
		splitContainer1.Panel2.ResumeLayout(false);
		splitContainer1.Panel2.PerformLayout();
		((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
		splitContainer1.ResumeLayout(false);
		splitContainer2.Panel1.ResumeLayout(false);
		splitContainer2.Panel1.PerformLayout();
		splitContainer2.Panel2.ResumeLayout(false);
		((System.ComponentModel.ISupportInitialize)splitContainer2).EndInit();
		splitContainer2.ResumeLayout(false);
		((System.ComponentModel.ISupportInitialize)dgvObject).EndInit();
		tableLayoutPanel2.ResumeLayout(false);
		tableLayoutPanel4.ResumeLayout(false);
		tableLayoutPanel4.PerformLayout();
		((System.ComponentModel.ISupportInitialize)dgvRelations).EndInit();
		grpFilterOptions.ResumeLayout(false);
		grpFilterOptions.PerformLayout();
		tbpOAuth2.ResumeLayout(false);
		tbpOAuth2.PerformLayout();
		tbpSOQL.ResumeLayout(false);
		tableLayoutPanel11.ResumeLayout(false);
		tableLayoutPanel11.PerformLayout();
		splitcSoql.Panel1.ResumeLayout(false);
		splitcSoql.Panel2.ResumeLayout(false);
		((System.ComponentModel.ISupportInitialize)splitcSoql).EndInit();
		splitcSoql.ResumeLayout(false);
		tableLayoutPanel12.ResumeLayout(false);
		tableLayoutPanel16.ResumeLayout(false);
		tableLayoutPanel16.PerformLayout();
		tableLayoutPanel13.ResumeLayout(false);
		((System.ComponentModel.ISupportInitialize)dgvSOQLResult).EndInit();
		tableLayoutPanel14.ResumeLayout(false);
		tableLayoutPanel15.ResumeLayout(false);
		tableLayoutPanel17.ResumeLayout(false);
		tableLayoutPanel17.PerformLayout();
		tbpDescribeObject.ResumeLayout(false);
		tableLayoutPanel5.ResumeLayout(false);
		tableLayoutPanel5.PerformLayout();
		((System.ComponentModel.ISupportInitialize)dgvSchema).EndInit();
		tbpEventLog.ResumeLayout(false);
		splitContainer3.Panel1.ResumeLayout(false);
		splitContainer3.Panel2.ResumeLayout(false);
		((System.ComponentModel.ISupportInitialize)splitContainer3).EndInit();
		splitContainer3.ResumeLayout(false);
		tableLayoutPanel6.ResumeLayout(false);
		tableLayoutPanel7.ResumeLayout(false);
		tbpCDCEvents.ResumeLayout(false);
		tableLayoutPanel8.ResumeLayout(false);
		tableLayoutPanel8.PerformLayout();
		splitContainer4.Panel1.ResumeLayout(false);
		splitContainer4.Panel2.ResumeLayout(false);
		((System.ComponentModel.ISupportInitialize)splitContainer4).EndInit();
		splitContainer4.ResumeLayout(false);
		splitContainer5.Panel1.ResumeLayout(false);
		splitContainer5.Panel2.ResumeLayout(false);
		((System.ComponentModel.ISupportInitialize)splitContainer5).EndInit();
		splitContainer5.ResumeLayout(false);
		((System.ComponentModel.ISupportInitialize)dgvFilteredFields).EndInit();
		tableLayoutPanel9.ResumeLayout(false);
		tbpX12.ResumeLayout(false);
		tableLayoutPanel18.ResumeLayout(false);
		grpx.ResumeLayout(false);
		grpx.PerformLayout();
		splitContainer7.Panel1.ResumeLayout(false);
		((System.ComponentModel.ISupportInitialize)splitContainer7).EndInit();
		splitContainer7.ResumeLayout(false);
		splitContainer6.Panel1.ResumeLayout(false);
		((System.ComponentModel.ISupportInitialize)splitContainer6).EndInit();
		splitContainer6.ResumeLayout(false);
		((System.ComponentModel.ISupportInitialize)dgvCDCTables).EndInit();
		ResumeLayout(false);
		PerformLayout();
		}

	// Fix for CS0407: Ensure the event handler method 'button31_Click' has the correct return type 'void'  




	private System.Windows.Forms.Button btnAuthenticate;
	private System.Windows.Forms.TextBox txtResult;
	private Button btnGetTokenAsync;
	private StatusStrip statusStrip1;
	private ToolStripStatusLabel toolStripStatusLabel1;
	private TabControl tabControl1;
	private TabPage tbpOAuth2;
	private TabPage tbpPubSub;
	private TableLayoutPanel tableLayoutPanel1;
	private Button btnSubscribe;
	private Button button1;
	private TableLayoutPanel tableLayoutPanel2;
	private DataGridView dgvObject;
	private ListBox lbxObjects;
	private SplitContainer splitContainer1;
	private Label lblPanel1;
	private Label lblPanel2;
	private RadioButton rbtFilterSubscribed;
	private RadioButton rbtFilterNone;
	private GroupBox grpFilterOptions;
	private TabPage tbpSfObjects;
	private Button btnCommit;
	private TabPage tbpDescribeObject;
	private TableLayoutPanel tableLayoutPanel5;
	private Label label1;
	private TableLayoutPanel tableLayoutPanel3;
	private Button btnCommitToDB;
	private DataGridView dgvCDCEnabledObjects;
	private Button btnMoveLeft;
	private DataGridView dgvRegisteredCDCCandidates;
	private Button btnClearDestination;
	private Button btnMoveRight;
	private Label label2;
	private Label lblSourceList;
	private Label lblDestinationList;
	private GroupBox grpPrimaryKey;
	private CheckBox chkAddIdentityField;
	private Label label3;
	private TextBox textBox1;
	private SplitContainer splitContainer2;
	private Label lblSelectedTable;
	private TableLayoutPanel tableLayoutPanel4;
	//	private DataGridView dgvRelations;
	private Label lblRelations;
	private DataGridView dgvRelations;
	private TabPage tbpEventLog;
	private SplitContainer splitContainer3;
	private TableLayoutPanel tableLayoutPanel6;
	private TableLayoutPanel tableLayoutPanel7;
	private Button btnClearLog;
	private RichTextBox rtfLog;
	private TabPage tbpCDCEvents;
	private TableLayoutPanel tableLayoutPanel8;
	private SplitContainer splitContainer4;
	private ListBox lbxCDCEvents;
	private ListBox lbxCDCTopics;
	private TableLayoutPanel tableLayoutPanel9;
	private Button btnCDCStartSubscription;
	private Label lblCDCStatus;
	private SplitContainer splitContainer5;
	private DataGridView dgvFilteredFields;
	private Button btnRegisterFields;
	private Button button2;
	private Button btnDeleteCDCRegistration;
	private RichTextBox rtxFieldsJsonArray;
	private TableLayoutPanel tableLayoutPanel10;
	private Button bsA;
	private Button button25;
	private Button button24;
	private Button button23;
	private Button button22;
	private Button button21;
	private Button button20;
	private Button button19;
	private Button button18;
	private Button button17;
	private Button button16;
	private Button button15;
	private Button button14;
	private Button button13;
	private Button button12;
	private Button button11;
	private Button button10;
	private Button button9;
	private Button button8;
	private Button button7;
	private Button button6;
	private Button button5;
	private Button button4;
	private Button button3;
	private Button button26;
	private Button btnDescribe;
	private Button btnGetCDCSubscriptions;
	private DataGridView dgvSchema;

	private TabPage tbpSOQL;
	private TableLayoutPanel tableLayoutPanel11;
	private ComboBox cmbSOQL;
	private SplitContainer splitcSoql;
	private RichTextBox rtSoqlQuery;
	private TableLayoutPanel tableLayoutPanel12;
	private Button btnDeleteSoql;
	private Button btnSaveSoql;
	private Button btnExecSoql;
	private DataGridView dgvSOQLResult;
	private Label lblSoqlText;
	private TableLayoutPanel tableLayoutPanel13;
	private TableLayoutPanel tableLayoutPanel14;
	private Button button29;
	private Button button28;
	private Button btnSoqlSave;
	private Button btnSoqlRDelete;
	private TableLayoutPanel tableLayoutPanel15;
	private Button btnBuildSelect;
	private CheckBox chkUseTooling;
	private TableLayoutPanel tableLayoutPanel16;
	private Button button30;
	private Button button31;
	private PictureBox pictureBox1;
	private PictureBox pictureBox2;
	private ComboBox cmbObjects;
	private Button btnDeleteCmbObjectSelected;
	private Label lblCDCName;
	private TableLayoutPanel tableLayoutPanel17;
	private Label lblSOQLRowCount;
	private Button btnListEvents;
	private TabPage tabPage1;
	private TabPage tbpX12;
	private TableLayoutPanel tableLayoutPanel18;
	private SplitContainer splitContainer6;
	private DataGridView dgvCDCTables;
	private ComboBox cmbCDCTables;
	private GroupBox grpx;
	private RadioButton radioButton3;
	private RadioButton radioButton2;
	private RadioButton radioButton1;
	private RichTextBox rtxLog;
	private SplitContainer splitContainer7;
	private Button btnLogTest;
	}
