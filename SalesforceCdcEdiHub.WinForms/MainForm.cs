using System.Data;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using Common;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NLog.Windows.Forms;
using SalesforceCdcEdiHub;
using Button = System.Windows.Forms.Button;
using Color = System.Drawing.Color;
using Control = System.Windows.Forms.Control;
using enmRetrievedFrom = WinForms.MainForm.enmRetrieveFrom;
using LogLevel = NLog.LogLevel;
using Properties = SalesforceCdcEdiHub.WinForms.Properties;
using ToolTip = System.Windows.Forms.ToolTip;
using SalesforceCdcEdiHub;
namespace WinForms;
public partial class MainForm : Form {
	#region enums
	public enum enmRetrieveFrom {
		SalesForce,
		SqlServer,
		None
		}
	public enum tbp {
		objects,
		pubsub,
		Oauth2,
		DescribeO,
		EventLog,
		CDCEvents
		}

	#endregion enums
	#region fields
	private readonly IMemoryCache _cache;
	private const string CacheKey = "SFoken";
	private readonly IHost _host;
	private readonly ISalesforceService _salesforceService;
	private readonly PubSubService _pubSubService;
	private readonly SalesforceConfig _config;
	private readonly ILogger<MainForm> _logger;
	private readonly X12 _x12;
	private HashSet<int> _higlightTabs = new();
	static string _token = "";
	static string _instanceUrl = "";
	static string _tenantId = "";
	private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
	private readonly object _dgvLock = new object();
	static bool _cdcObjectsLoaded = false;
	static bool _soqlLoaded = false;
	static bool _hasUnInitDbArtefacts = false;
	private List<string> _sfoTables = new List<string>(); // List of Salesforce objects from SQL Server
	private List<string> _qTypeSelecter = new List<string>();
	private readonly Dictionary<DataGridView, Dictionary<int, bool>> _rowHeaderCheckStatesMap
		= new Dictionary<DataGridView, Dictionary<int, bool>>();
	private DataTable _sourceTable; // Data source for dgvCDCEnabledObjects
	private DataTable _destinationTable; // Data source for dgvRegisteredCDCCandidates
	private DataTable _dtRegisteredCDCCandidates; // Data source for registered tables
	private DataTable _dtSoqlResults = new DataTable();
	private static int _lbxLogMw = 0;
	private List<DataRow> _rowsToMove = new List<DataRow>(); // Temp storage for rows to move
	private readonly SqlServerLib _sqlServerLib;
	private readonly object _lock = new object();
	private static enmRetrieveFrom _retrieveFrom = enmRetrieveFrom.SalesForce;
	private static enmRetrievedFrom _retrievedFrom = _retrieveFrom;
	private System.Drawing.Color _dfBColor;
	private Color _dfFColor;
	private Dictionary<TabPage, (Color bcolor, Color fcolor)> _tabColors = new Dictionary<TabPage, (Color, Color)>();
	#endregion fields
	#region events
	#region pubsubservice events
	private void PubSubService_ProgressUpdated(object sender, ProgressUpdateEventArgs e) {
		if (lbxCDCTopics.InvokeRequired) {
			lbxCDCTopics.Invoke(new Action(() => lbxCDCTopics.Items.Add(e.Message)));
			} else lbxCDCTopics.Items.Add(e.Message);
		}
	// do not handle _pubsubService.CDCEvent here, SqlServerLib will handle it
	#endregion pubsubservice events
	#region _sqlserver events
	private void SqlEventObjectExist(object? sender, SqlObjectQuery e) {
		_logger.LogInformation($"CDC {{e.ObjectType}} {{e.ObjectName}} exist={{e.Exist}}  id= {{e.Id}} ");
		btnDeleteCDCRegistration.Visible = e.Exist;
		toolStripStatusLabel1.ForeColor = Color.Yellow;
		if (e.Exist) {
			toolStripStatusLabel1.Text += $" object in the sql server with the Id {e.Id}";
			toolStripStatusLabel1.BackColor = Color.Green;
			_retrievedFrom = enmRetrieveFrom.SqlServer;
			_retrieveFrom = _retrievedFrom;
			btnRegisterFields.Text = "Update Fields";
			} else {
			_retrievedFrom = enmRetrieveFrom.SalesForce;// does not exist retrive from sf
			_retrieveFrom = _retrievedFrom;
			toolStripStatusLabel1.BackColor = Color.Brown;
			btnRegisterFields.Text = "Register Fields";
			}
		setControlColor(btnRegisterFields, e.Exist);
		}
	private void _sqlServerLib_SqlEvent(object? sender, SqlEventArg e) {
		_logger.LogDebug(e.Message);
		if (!e.HasErrors) {
			switch (e.ReturningFrom) {
				case "RegisterExludedCDCFields":
					lbxObjects_SelectedIndexChanged(sender, e);// redo it to refresh UI
					break;
				case "DeleteCDCObject":
					lbxObjects_SelectedIndexChanged(sender, e);// redo it to refresh UI	
					lbxObjects.Items.Remove(lbxObjects.SelectedItem);
					lblPanel1.Text = $"{lbxObjects.Items.Count} Subscribed CDC Object";
					break;
				}
			}
		}
	#endregion _sqlserver events
	#endregion events	
	#region form

	private void SetupRichTextBoxContextMenu(RichTextBox rtb) {
		var contextMenu = new ContextMenuStrip();

		// Copy (Unicode)
		contextMenu.Items.Add("Copy All", null, (s, ea) =>
		{
			if (!string.IsNullOrEmpty(rtb.Rtf)) {
				Clipboard.SetText(rtb.Rtf, TextDataFormat.Rtf);
				}
		});


		// Paste
		contextMenu.Items.Add("Paste", null, (s, ea) =>
		{
			if (Clipboard.ContainsText(TextDataFormat.Rtf)) {
				rtb.SelectedRtf = Clipboard.GetText(TextDataFormat.Rtf);
				} else if (Clipboard.ContainsText()) {
				rtb.Paste();
				}
		});

		// Select All
		contextMenu.Items.Add("Select All", null, (s, ea) =>
		{
			rtb.SelectAll();
		});

		contextMenu.Items.Add(new ToolStripSeparator());

		// Clear
		contextMenu.Items.Add("Clear", null, (s, ea) =>
		{
			rtb.Clear();
		});

		// Assign to RichTextBox
		rtb.ContextMenuStrip = contextMenu;
		}

	public MainForm(IMemoryCache cache, ISalesforceService salesforceService, PubSubService pubSubService, IOptions<SalesforceConfig> config, SqlServerLib sqlServerLib, ILogger<MainForm> logger, X12 x12) {
		InitializeComponent();
		#region tt
		ToolTip tt = new ToolTip();
		tt.OwnerDraw = true;
		tt.InitialDelay = 1;
		tt.IsBalloon = false;
		tt.Draw += (s, e) => {
			e.Graphics.FillRectangle(Brushes.LightCyan, e.Bounds);
			e.Graphics.DrawRectangle(Pens.SteelBlue, e.Bounds);
			e.Graphics.DrawString(e.ToolTipText, SystemFonts.DefaultFont, Brushes.Black, e.Bounds);
		};
		string ttText = "Schemas of selected Salesforce objects must be persisted in SQL Server.\n" +
			"These objects are selected from the Objects tab.\n" +
			"Only objects marked as CDC Candidates are eligible for Pub/Sub operations.\n" +
			"To view all registered schemas, select 'Filter None' from the filter radio buttons.\n" +
			"In the Pub/Sub tab, choose the objects that require subscription.\n" +
			"Selected objects are automatically converted into gRPC topic format.\n" +
			"Select the excluded fields (IsExcluded) that should still be transferred\n" +
			"to SQL Server when a gRPC change event is received from Salesforce Service Bus.";
		tt.SetToolTip(lbxObjects, ttText);
		tt.SetToolTip(dgvObject, ttText);
		tt.SetToolTip(lblSelectedTable, ttText);
		#endregion tt
		_cache = cache;
		_salesforceService = salesforceService;
		_pubSubService = pubSubService;
		_config = config.Value;
		_sqlServerLib = sqlServerLib;
		_x12 = x12;
		_pubSubService.ProgressUpdated += PubSubService_ProgressUpdated!;
		if (_salesforceService is SalesforceService cs) {
			cs.AuthenticationAttempt += SalesforceService_AuthenticationAttempt!;
			}
		_sqlServerLib.SqlEvent += (s, e) => {
			Log(e.Message, e.LogLevel);
		};
		_sqlServerLib.SqlObjectExist += SqlEventObjectExist;
		_sqlServerLib.SqlEvent += _sqlServerLib_SqlEvent;
		_sqlServerLib = sqlServerLib ?? throw new ArgumentNullException(nameof(sqlServerLib));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_logger.LogDebug("MainForm initialized.");
		_logger.LogInformation("(logInformation)MainForm initialized.");
		saveTabPageColors();
		#region soql tab & controls
		rtSoqlQuery.Text = "";

		dgvSOQLResult.DataSource = null;
		dgvSOQLResult.AllowUserToAddRows = true;
		dgvSOQLResult.RowsAdded += dgvSOQLResult_RowsAdded;
		_dtSoqlResults!.RowChanged += _dtSoqlResults_RowChanged;
		_x12 = x12;
		#endregion soql tab & controls
		SetupRichTextBoxContextMenu(rtxLog);
		RichTextBoxTarget.ReInitializeAllTextboxes(this);
		}
	private void Form1_Load(object sender, EventArgs e) {
		string savedTab = string.IsNullOrEmpty(Properties.Settings.Default.SelectedTab) ? "tbpSfObjects" : Properties.Settings.Default.SelectedTab;
		if (!string.IsNullOrEmpty(savedTab) && tabControl1.TabPages.ContainsKey(savedTab)) {
			//TabPage tbp = tabControl1.TabPages[savedTab]!;
			//tabControl1_Selected(sender, new TabControlEventArgs(tbp, tabControl1.SelectedIndex, TabControlAction.Selected));
			tabControl1.SelectedTab = tabControl1.TabPages[savedTab];
			}
		lblPanel1.Parent = splitContainer1.Panel1;
		lblDestinationList.Text = "";
		SetupDataGridViewHeaders("");
		btnSubscribe_Click(null, null);
		string savedItems = Properties.Settings.Default.cmbObjects;
		if (!string.IsNullOrWhiteSpace(savedItems)) {
			var items = savedItems.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			cmbObjects.Items.AddRange(items);
			}
		}
	private void Form1_FormClosing(object sender, FormClosingEventArgs e) {
		Properties.Settings.Default.SelectedTab = tabControl1.SelectedTab.Name;
		Properties.Settings.Default.Save();
		}
	#endregion	 form	
	#region buttons
	private async void btnAuthenticate_Click(object sender, EventArgs e) {
		await _semaphore.WaitAsync();
		try {
			btnAuthenticate.Enabled = false;
			this.Invoke((Action)(() => txtResult.Clear()));
			//	var token = await _salesforceService.GetSFTokenAsync();
			//string token, instanceUrl, tenantId;
			var (token, instanceUrl, tenantId) = await _salesforceService.GetAccessTokenAsync();
			this.Invoke((Action)(() => txtResult.Text = $"Token copied to clipboard: {token}..."));
			Clipboard.SetText(token);

			} catch (Exception ex) {
			this.Invoke((Action)(() => txtResult.Text = $"Authentication failed: {ex.Message}"));
			}
		finally {
			btnAuthenticate.Enabled = true;
			_semaphore.Release();
			}
		}
	private void btnGetTokenAsync_Click(object sender, EventArgs e) {
		_ = GetAccessToken();
		}
	private async void btnSubscribe_Click(object sender, EventArgs e) {
		try {
			if (!_cdcObjectsLoaded) await loadCDCObjects();// get the SqlServer registered objects to _destinationTable 
														   //var topics = new HashSet<string?>(
														   //	_destinationTable.AsEnumerable()//.Where(r => !r.IsNull("name"))
														   //	.Select(r => $"/data/{r.Field<string>("name")}ChangeEvent"));

			//		topics.Add("/event/ProductSelected");// experimental to subscribe event from ebikes ProductSelected message channel -> LightningMessageChannel
			//var topics = new HashSet<string> { "/data/AccountChangeEvent", "/data/Order__ChangeEvent", "/data/Order_Item__ChangeEvent" };
			DataTable dt = await _salesforceService.ExecSoqlToTable("select selectedEntity from PlatformEventChannelMember", useTooling: true);
			var topics = new HashSet<string>(dt.AsEnumerable()
				.Select(row => $"/data/{row["SelectedEntity"]}"));




			await _pubSubService.StartSubscriptionsAsync(topics!);
			toolStripStatusLabel1.Text = "Token copied to Clipboard.";
			} catch (Exception ex) {
			MessageBox.Show($"Error: {ex.Message}");
			}
		}
	private async void btnGetSchema_Click(object sender, EventArgs e) {
		await _semaphore.WaitAsync();
		try {
			if (lbxObjects.SelectedItems.Count == 0) {
				MessageBox.Show("Please select a topic from the list.");
				return;
				}
			string oN = ObjectNameFromEventDeclaration((string)lbxObjects.SelectedItem!);
			DataSet ds = await _salesforceService.GetObjectSchemaAsDataSetAsync(oN);
			DataTable dt = ds.Tables[oN];// Now synchronize access to the UI with lock
			lock (_dgvLock) {
				this.Invoke((Action)(() => {
					dgvObject.DataSource = dt;
					dgvObject.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
					toolStripStatusLabel1.Text = $"Schema for {dt.TableName} having {dt.Rows.Count} rows loaded successfully.";
				}));
				}
			} catch (Exception ex) {
			this.Invoke((Action)(() => txtResult.Text = $"Error: {ex.Message}"));
			}
		finally {
			_semaphore.Release();
			}
		}
	#region move right and left
	private async void btnMoveRight_Click(object sender, EventArgs e) {
		_logger.LogDebug($"Selected rowcount={dgvCDCEnabledObjects.SelectedRows.Count}");
		if (dgvCDCEnabledObjects.SelectedRows.Count == 0) return;
		_sourceTable = (DataTable)dgvCDCEnabledObjects.DataSource!;
		if (dgvRegisteredCDCCandidates.DataSource == null) {
			_destinationTable = _sourceTable!.Clone(); // Clone structure only
			dgvRegisteredCDCCandidates.DataSource = _destinationTable;
			} else {
			_destinationTable = (DataTable)dgvRegisteredCDCCandidates.DataSource;
			}
		List<DataRow> rowsToRemove = new List<DataRow>();// Create a list to store rows to remove (to avoid modifying collection during iteration)
		foreach (DataGridViewRow row in dgvCDCEnabledObjects.SelectedRows) {// Move selected rows
			DataRow sourceRow = ((DataRowView)row.DataBoundItem!).Row;
			try {
				await _salesforceService.AddCDCChannelMember(row.Cells["name"].Value!.ToString()!);
				_destinationTable!.ImportRow(sourceRow); // Add to destination
				rowsToRemove.Add(sourceRow);// Mark for removal from source
				} catch (Exception ex) {
				_logger.LogError($"Error adding CDC channel member: {ex.Message}");
				MessageBox.Show($"Error adding CDC channel member: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				Clipboard.SetText(ex.Message);
				}
			foreach (DataRow rx in rowsToRemove) { // Remove rows from source after iteration
				_sourceTable!.Rows.Remove(rx);
				}
			}
		btnSubscribe_Click(null, null);// Refresh the subscription list after moving rows
		dgvCDCEnabledObjects.DataSource = null;// Refresh both DataGridViews
		dgvCDCEnabledObjects.DataSource = _sourceTable;
		dgvCDCEnabledObjects.Columns[0].HeaderText = "Salesforce Objects";
		dgvCDCEnabledObjects.Refresh();
		dgvCDCEnabledObjects.AutoResizeColumns();// Optional: Adjust column sizes
		dgvRegisteredCDCCandidates.DataSource = null;
		dgvRegisteredCDCCandidates.DataSource = _destinationTable;
		dgvRegisteredCDCCandidates.AutoResizeColumns();
		lblSourceList.Text = $"{dgvCDCEnabledObjects.Rows.Count} Salesforce objects";
		Log($"Source count = {_sourceTable.Rows.Count} Destination={_destinationTable.Rows.Count}", LogLevel.Debug);
		}

	private async void btnMoveLeft_Click(object sender, EventArgs e) {
		if (dgvRegisteredCDCCandidates.SelectedRows.Count == 0) return;
		_sourceTable = (DataTable)dgvCDCEnabledObjects.DataSource;
		_destinationTable = (DataTable)dgvRegisteredCDCCandidates.DataSource;
		if (_sourceTable == null) {
			_sourceTable = _destinationTable.Clone();
			dgvCDCEnabledObjects.DataSource = _sourceTable;
			}
		List<DataRow> rowsToRemove = new List<DataRow>();
		foreach (DataGridViewRow row in dgvRegisteredCDCCandidates.SelectedRows) {
			DataRow deletedRow = ((DataRowView)row.DataBoundItem).Row;
			DataRow sourceRow = _sourceTable.NewRow();

			sourceRow["name"] = deletedRow["name"].ToString();
			sourceRow["QualifiedApiName"] = SalesforceService.ObjectNameToChangeEvent(deletedRow["name"].ToString()!);
			_sourceTable.Rows.Add(sourceRow);
			rowsToRemove.Add(deletedRow);
			}
		foreach (DataRow row in rowsToRemove) {
			string name = row["name"].ToString();
			MessageBox.Show($"object name: {name}:{SalesforceService.ObjectNameToChangeEvent(name)} ");
			string recordId = await _salesforceService.IdOfPlatformEventChannelMember(name);

			bool x = await _salesforceService.DeleteToolingRecord("PlatformEventChannelMember", recordId);
			btnSubscribe_Click(null, null);
			_destinationTable.Rows.Remove(row);
			}
		dgvCDCEnabledObjects.DataSource = null;
		dgvCDCEnabledObjects.DataSource = _sourceTable;
		dgvCDCEnabledObjects.Refresh();
		dgvCDCEnabledObjects.Columns[0].HeaderText = "Salesforce Objects";

		dgvRegisteredCDCCandidates.DataSource = null;
		dgvRegisteredCDCCandidates.DataSource = _destinationTable;
		dgvRegisteredCDCCandidates.Refresh();
		dgvRegisteredCDCCandidates.Columns[0].HeaderText = "CDC Candidates";
		dgvCDCEnabledObjects.AutoResizeColumns();
		dgvRegisteredCDCCandidates.AutoResizeColumns();
		}
	#endregion move right and left
	private void btnClearDestination_Click(object sender, EventArgs e) {
		dgvRegisteredCDCCandidates.SelectAll();
		_destinationTable.Clear();
		dgvRegisteredCDCCandidates.Refresh();

		lblDestinationList.Text = $"{dgvRegisteredCDCCandidates.Rows.Count} candidate rows";
		}
	private void btnClearLog_Click(object sender, EventArgs e) {
		rtxLog.Text = "";
		}

	private async Task<string> PopulateDbTableFromSfObject(string objectName) {
		string schemaName = "sfo"; // Default schema name for Salesforce objects
		JsonElement je = await _salesforceService.GetObjectSchemaAsync(objectName, default);
		string s = $"SELECT {string.Join(",", je.GetProperty("fields").EnumerateArray().Select(f => f.GetProperty("name")))} FROM {objectName}";
		DataTable dt = await _salesforceService.ExecSoqlToTable(s, false);
		_sqlServerLib.UpdateServerTable(dt, s);
		return null; // Return null or appropriate value if needed
		}
	private async void btnRegisterCDCCandidate(object sender, EventArgs e) {
		DataTable dt = (DataTable)dgvRegisteredCDCCandidates.DataSource!;
		DataTable dtSfoTables = _sqlServerLib.Select("select * from ftTablesOfSchema('sfo')");
		HashSet<string> existingNames = new HashSet<string>(dtSfoTables.AsEnumerable().Select(r => r.Field<string>("name")!));
		HashSet<string> listToCreate = new HashSet<string>();
		foreach (DataGridViewRow row in dgvRegisteredCDCCandidates.Rows) {
			string name = row.Cells["name"].Value?.ToString() ?? string.Empty;
			if (row.Cells["Initialize"].Value is true) {
				listToCreate.Add(name);
				row.Cells["Create"].Value = Properties.Resources.CacheOk; // Set warning icon for non-existing names

				}
			}
		if (listToCreate.Count > 0) {
			await createCDCReplica(listToCreate);
			}
		foreach (string name in listToCreate) {
			_ = await PopulateDbTableFromSfObject(name);
			}
		foreach (string name in listToCreate) {
			Console.WriteLine($"name to create:{name}");
			}
		}
	private void btnRegisterFields_Click(object sender, EventArgs e) {
		throw new NotImplementedException("btnRegisterFields_Click is not implemented. Please implement the method to register fields for CDC.");
		var b = (Button)sender;
		if (b.Text.Contains("Update")) {
			updateFields();
			return;
			}
		DataTable? Fields = dgvObject.DataSource as DataTable;
		Fields.TableName = ObjectNameFromEventDeclaration(lbxObjects.Text);
		Fields.Columns["Exclude"].DefaultValue = false;
		Fields.AsEnumerable()
			  .Where(row => row.IsNull("Exclude") || string.IsNullOrEmpty(row["Exclude"].ToString()))
			  .ToList()
			  .ForEach(row => row["Exclude"] = false);
		string sxml = Fields.GetXml("Name,Exclude");
		var (rowsInserted, tableName) = _sqlServerLib.RegisterExludedCDCFields(sxml);
		Log($"{rowsInserted} rows inserted into {tableName}", LogLevel.Debug);
		}
	private void updateFields() {
		DataTable Fields = dgvObject.DataSource as DataTable;
		_sqlServerLib.UpdateServerTable(Fields, "SELECT [Id],[IsExcluded]  FROM [dbo].[CDCObjectFields] ");
		}
	private void btnDeleteCDCSubscription_Click(object sender, EventArgs e) {
		_sqlServerLib.DeleteCDCObject(ObjectNameFromEventDeclaration(lbxObjects.Text));
		}
	private void btnSort_Click(object sender, EventArgs e) {
		Button b = (Button)sender;
		char l = b.Text[0];
		_sourceTable.DefaultView.Sort = $"name ASC"; // Sort the source table by name
		int ti = -1;
		for (int i = 0; i < _sourceTable.Rows.Count; i++) {
			string tn = _sourceTable.Rows[i][0].ToString();
			if (l == tn[0]) {
				ti = i;
				break;
				}
			}
		if (ti >= 0 && ti < _sourceTable.Rows.Count) {
			dgvCDCEnabledObjects.FirstDisplayedScrollingRowIndex = ti;
			dgvCDCEnabledObjects.ClearSelection();
			dgvCDCEnabledObjects.Rows[ti].Selected = true;
			}
		}
	private void btnCDCStartSubscription_Click(object sender, EventArgs e) {
		}
	private async void btnGetCDCSubscriptions_Click(object sender, EventArgs e) {
		//	var x = _salesforceService.GetCDCSubscriptions();
		//DataTable dt =await	_salesforceService.GetAllObjects();
		}
	private async void btnGetPlatformEventChannel_Click(object sender, EventArgs e) {
		try {
			JsonElement je = await _salesforceService.GetPlaformEventChannelMembers();
			} catch (Exception ex) {
			MessageBox.Show(ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			}
		}
	private async void btnDescribe_Click(object sender, EventArgs e) {
		try {
			DataSet ds = await _salesforceService.GetObjectSchemaAsDataSetAsync(cmbObjects.Text!.ToString());
			dgvSchema.DataSource = ds.Tables[0];
			Console.WriteLine(ds.GetXml());
			} catch (Exception ex) {
			dgvSchema.DataSource = null;
			if (ex.Message.Contains(": Not Found")) {
				DialogResult dr = MessageBox.Show($"The object {cmbObjects.Text} not found in the Standard objects, look inn tooling ?", "", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
				if (dr == DialogResult.Yes) {

					try {
						DataSet ds = await _salesforceService.GetObjectSchemaAsDataSetAsync(cmbObjects.Text, useTooling: true);
						dgvSchema.DataSource = ds.Tables[0];
						} catch (Exception) {


						}
					}
				}
			}
		}
	private void btnSaveSoql_Click(object sender, EventArgs e) {

		// Check if the query key already exists in the SoqlQueries table
		if (_sqlServerLib.ExecuteScalar<int>($"SELECT COUNT(*) FROM SoqlQueries WHERE qKey = '{cmbSOQL.Text.Replace("'", "''")}'") > 0) {
			// Existing record, perform UPDATE
			string sql = $"UPDATE SoqlQueries SET q = '{rtSoqlQuery.Text.Replace("'", "''")}', UseTooling = {(chkUseTooling.Checked ? 1 : 0)} WHERE qKey = '{cmbSOQL.Text.Replace("'", "''")}'";
			int count = _sqlServerLib.ExecuteNoneQuery(sql);
			} else {
			// New record, perform INSERT
			string sql = $"INSERT INTO SoqlQueries (q, qKey, UseTooling) VALUES ('{rtSoqlQuery.Text.Replace("'", "''")}', '{cmbSOQL.Text.Replace("'", "''")}', {(chkUseTooling.Checked ? 1 : 0)})";
			int count = _sqlServerLib.ExecuteNoneQuery(sql);
			}

		_soqlLoaded = false;
		tabControl1_Selected(this, new TabControlEventArgs(tbpSOQL, tabControl1.TabPages.IndexOf(tbpSOQL), TabControlAction.Selected)).GetAwaiter().GetResult();
		}
	private void btnDeleteSoql_Click(object sender, EventArgs e) {
		string sql = "Delete from soqlQueries where qKey='" + cmbSOQL.Text + "'";
		_sqlServerLib.ExecuteNoneQuery(sql);
		_soqlLoaded = false;
		tabControl1_Selected(this, new TabControlEventArgs(tbpSOQL, tabControl1.TabPages.IndexOf(tbpSOQL), TabControlAction.Selected)).GetAwaiter().GetResult();
		}
	private async void btnExecSoql_Click(object sender, EventArgs e) {
		Size sw = new Size(tabControl1.ClientSize.Width, splitcSoql.Panel2.ClientSize.Height);
		splitcSoql.Panel2.ClientSize = sw;
		splitcSoql.Panel1Collapsed = true;
		tableLayoutPanel13.Width = sw.Width;

		dgvSOQLResult.DataSource = null;

		_dtSoqlResults = await _salesforceService.ExecSoqlToTable(rtSoqlQuery.Text, chkUseTooling.Checked);
		if (_dtSoqlResults.Columns.Contains("Id")) _dtSoqlResults.Columns["Id"].ReadOnly = true;
		_dtSoqlResults.AcceptChanges();
		dgvSOQLResult.DataSource = _dtSoqlResults;
		lblSOQLRowCount.Text = $"Rows: {_dtSoqlResults.Rows.Count}";

		}
	private async void btnSObjectSave(object sender, EventArgs e) {// saves Edited Soql Object 	
		dgvSOQLResult.EndEdit();
		if (dgvSOQLResult.DataSource is BindingSource bindingSource) {
			bindingSource.EndEdit();

			} else {
			BindingContext[dgvSOQLResult.DataSource].EndCurrentEdit();

			}

		DataTable dtChanged = _dtSoqlResults.GetChanges(DataRowState.Modified);
		if (dtChanged != null) {
			foreach (DataRow dr in dtChanged.Rows) {
				string id = dr["Id"].ToString();

				string json = dr.ToJson(indented: true, excludedColumns: "Id");
				DataTable dt = await _salesforceService.UpsertSobject(dtChanged.TableName, id, json);
				}
			_dtSoqlResults.AcceptChanges();
			}

		DataTable dtAdded = _dtSoqlResults.GetChanges(DataRowState.Added);
		if (dtAdded != null) {
			dtAdded.Columns.Remove("Id");
			dtAdded = removeNullColumns(dtAdded);
			DataTable dt = await _salesforceService.UpsertSobject(dtAdded.TableName, null, dtAdded.ToJson());
			_dtSoqlResults.AcceptChanges();
			}

		}
	private DataTable removeNullColumns(DataTable dt) {
		foreach (DataColumn column in dt.Columns.Cast<DataColumn>().ToList()) {// Remove columns with all null values
			if (dt.AsEnumerable().All(row => row.IsNull(column))) {
				dt.Columns.Remove(column);
				}
			}
		return dt;
		}
	private async void btnSoqlRDelete_Click(object sender, EventArgs e) {
		if (dgvSOQLResult.SelectedRows.Count == 0) {
			MessageBox.Show("Please select a row to delete.", "No Row Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			return;
			}
		DataGridViewRow selectedRow = dgvSOQLResult.SelectedRows[0];
		DataRowView rowView = selectedRow.DataBoundItem as DataRowView;

		DataRow row = rowView.Row;
		string tableName = row.Table.TableName;
		switch (row.Table.TableName) {
			case "PlatformEventChannelMember":
				bool x = await _salesforceService.DeleteToolingRecord(tableName, row["Id"].ToString()!);
				break;
			default:
				await _salesforceService.DeleteSobject(tableName, row["Id"].ToString()!);
				break;
			}
		row.Delete();
		_dtSoqlResults.AcceptChanges();
		this.Invoke((Action)(() => dgvSOQLResult.Refresh()));
		}
	private string objectNameFromSoql(string soql) {
		string pattern = @"FROM\s+([a-zA-Z0-9_]+)\b";
		var match = Regex.Match(soql, @"FROM\s+([a-zA-Z0-9_]+)\b", RegexOptions.IgnoreCase);
		return match.Groups[1].Value;
		}
	private async void btnBuildSelect_Click(object sender, EventArgs e) {

		string oN = objectNameFromSoql(rtSoqlQuery.Text);
		JsonElement je = chkUseTooling.Checked ? await _salesforceService.DescribeToolingObject(oN) : await _salesforceService.GetObjectSchemaAsync(oN, default);
		rtSoqlQuery.Text = $"SELECT {string.Join(",", je.GetProperty("fields").EnumerateArray().Select(f => f.GetProperty("name")))} FROM {oN}";
		}
	private async void btnListEvents_Click(object sender, EventArgs e) {
		await _salesforceService.GetEventDefinitions();
		}

	private void button27_Click(object sender, EventArgs e) {

		}
	#endregion buttons
	#region dgv
	private void dgvSOQLResult_RowsAdded(object? sender, DataGridViewRowsAddedEventArgs e) {
		if (e.RowIndex >= 0 && dgvSOQLResult.Rows[e.RowIndex].IsNewRow == false) {
			string colName = dgvSOQLResult.Columns[0].Name;
			}
		}

	private void SetupDataGridViewHeaders(string tn) {
		dgvObject.ColumnHeadersDefaultCellStyle.BackColor = System.Drawing.Color.LightBlue;
		dgvObject.ColumnHeadersDefaultCellStyle.Font = new System.Drawing.Font("Arial", 12, FontStyle.Bold);
		dgvObject.ColumnHeadersDefaultCellStyle.ForeColor = System.Drawing.Color.DarkBlue;
		dgvObject.TopLeftHeaderCell.Value = "Subscribe";
		dgvObject.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
		dgvObject.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;
		dgvObject.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

		dgvCDCEnabledObjects.ColumnHeadersDefaultCellStyle.BackColor = System.Drawing.Color.LightBlue;
		dgvCDCEnabledObjects.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
		dgvCDCEnabledObjects.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
		dgvCDCEnabledObjects.ColumnHeadersHeight = 50;

		dgvCDCEnabledObjects.ColumnHeadersDefaultCellStyle.Font = new System.Drawing.Font("Arial", 12, FontStyle.Bold);
		dgvCDCEnabledObjects.ColumnHeadersDefaultCellStyle.ForeColor = System.Drawing.Color.DarkBlue;

		dgvCDCEnabledObjects.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

		dgvCDCEnabledObjects.RowTemplate.Height = 30; // Set the height of the row template
		dgvCDCEnabledObjects.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;
		dgvCDCEnabledObjects.AutoGenerateColumns = true;
		dgvCDCEnabledObjects.DataSource = null;
		dgvCDCEnabledObjects.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

		//============================dgvRegisteredCDCCandidates=====================
		dgvRegisteredCDCCandidates.ColumnHeadersDefaultCellStyle.BackColor = System.Drawing.Color.LightBlue;
		dgvRegisteredCDCCandidates.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
		dgvRegisteredCDCCandidates.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
		dgvRegisteredCDCCandidates.ColumnHeadersHeight = 40;
		dgvRegisteredCDCCandidates.ColumnHeadersDefaultCellStyle.Font = new System.Drawing.Font("Arial", 12, FontStyle.Bold);
		dgvRegisteredCDCCandidates.ColumnHeadersDefaultCellStyle.ForeColor = System.Drawing.Color.DarkBlue;
		dgvRegisteredCDCCandidates.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
		dgvRegisteredCDCCandidates.RowTemplate.Height = 30; // Set the height of the row template
		dgvRegisteredCDCCandidates.DataSource = null;
		dgvRegisteredCDCCandidates.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
		//==================================================================
		dgvRelations.ColumnHeadersDefaultCellStyle.BackColor = System.Drawing.Color.LightBlue;
		dgvRelations.ColumnHeadersDefaultCellStyle.ForeColor = System.Drawing.Color.DarkBlue;
		dgvRelations.TopLeftHeaderCell.Value = "Subscribe";
		dgvRelations.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
		dgvRelations.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;
		dgvRelations.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;


		}
	private void SetContainedControlsEnabled(Control control, bool enabled) {
		foreach (Control child in control.Controls) {
			child.Enabled = enabled;
			//SetContainedControlsEnabled(child, enabled);
			}
		}
	private void dgvRowCountChanged(object sender, EventArgs e) {

		switch (sender) {
			case DataGridView s when s == dgvCDCEnabledObjects:
				lblSourceList.Text = $"{dgvCDCEnabledObjects.Rows.Count} Salesforce objects";
				btnMoveRight.Enabled = dgvCDCEnabledObjects.Rows.Count > 0;
				btnCommitToDB.Enabled = dgvRegisteredCDCCandidates.Rows.Count > 0;
				SetContainedControlsEnabled(grpPrimaryKey, btnCommitToDB.Enabled);
				break;

			case DataGridView s when s == dgvRegisteredCDCCandidates:
				lblDestinationList.Text = $"{dgvRegisteredCDCCandidates.Rows.Count} candidate Object";
				btnCommitToDB.Enabled = dgvRegisteredCDCCandidates.Rows.Count > 0;
				SetContainedControlsEnabled(grpPrimaryKey, btnCommitToDB.Enabled);
				break;

			}
		}
	private void dgvObject_CellContentClick_1(object sender, DataGridViewCellEventArgs e) {
		if (e.ColumnIndex < 0) return;
		if (dgvObject.Columns[e.ColumnIndex] is DataGridViewCheckBoxColumn) {
			dgvObject.CommitEdit(DataGridViewDataErrorContexts.Commit);
			DataTable dtObject = (DataTable)dgvObject.DataSource;
			dtObject.TableName = lbxObjects.Text;
			bool currentValue = Convert.ToBoolean(dgvObject.Rows[e.RowIndex].Cells[e.ColumnIndex].Value);
			if (dtObject.Columns.Contains("IsExcluded")) { // if it is a sql server table
				this.Invoke((Action)(() =>/* get only the field Names for the lbxFields*/  {
					List<string?> fields = dtObject.AsEnumerable()
						.Where(r => !(r["IsExcluded"] is bool b && b)) // filter out rows where Exclude == true 
						.Select(r => r["FieldName"]?.ToString())
						.Where(v => !string.IsNullOrEmpty(v))
						.ToList();
					rtxFieldsJsonArray.Text = JsonConvert.SerializeObject(fields);
					lblSelectedTable.Text = $"{dtObject.TableName} - filtered: {fields.Count}";
				}));
				} else {
				this.Invoke((Action)(() =>/* get only the field Names for the lbxFields*/  {
					List<string?> fields = dtObject.AsEnumerable()
						.Where(r => !(r["Exclude"] is bool b && b)) // filter out rows where Exclude == true 
						.Select(r => r["Name"]?.ToString())
						.Where(v => !string.IsNullOrEmpty(v))
						.ToList();
					rtxFieldsJsonArray.Text = JsonConvert.SerializeObject(fields);
					lblSelectedTable.Text = $"{dtObject.TableName} - filtered: {fields.Count}";
				}));
				}
			}
		}

	private void dgvSOQLResult_CellContentClick(object sender, DataGridViewCellEventArgs e) {
		DataTable dt = dgvSOQLResult.DataSource as DataTable;
		if (dt == null) return;
		if (e.RowIndex < 0 || e.ColumnIndex < 0) return; // Ensure valid row and column indices
		if (dt.Columns.Contains("SelectedEntity")) {
			string se = dt.Rows[e.RowIndex]["SelectedEntity"]?.ToString() ?? string.Empty;
			MessageBox.Show($"Selected Entity: {se} : **  {SalesforceService.ChangeEventToObjectName(se)}  **");
			}
		}
	#endregion dgv
	private void _dtSoqlResults_RowChanged(object sender, DataRowChangeEventArgs e) {
		throw new NotImplementedException();
		}

	private void SalesforceService_AuthenticationAttempt(object sender, SalesforceService.AuthenticationEventArgs e) {
		Invoke((Action)(() => {
			Log($"Authenticating: {e.Message}", e.LogLevel);
			btnAuthenticate.Enabled = false;
			toolStripStatusLabel1.Text = "Authenticating...";
		}));
		}
	#region helpers
	private void saveTabPageColors() {
		foreach (TabPage page in tabControl1.TabPages) _tabColors[page] = (page.BackColor, page.ForeColor);
		}
	private void LoadTopics(ListBox listBox, bool filtered) {
		listBox.Items.Clear();
		DataTable dataTable = filtered ? _sqlServerLib.Select("select * from dbo.ftcdcObjects()") : _sqlServerLib.GetAll_sfoTables();
		listBox.Items.AddRange(_sqlServerLib.GetChangeEventUrls(dataTable).ToArray());
		}
	public static string ObjectNameFromEventDeclaration(string eventDeclaration) {
		string oN = SalesforceService.PlatformEventChannelMemeberToObjectName(eventDeclaration);
		return oN;
		}
	private async Task GetAccessToken() {
		if (!_cache.TryGetValue(CacheKey, out string token)) {
			(token, _instanceUrl, _tenantId) = await _salesforceService.GetAccessTokenAsync();
			_cache.Set(CacheKey, token, TimeSpan.FromMinutes(30));
			}
		this.Invoke((Action)(() => txtResult.Text = $"Token: {token}, Instance URL: {_instanceUrl}, Tenant ID: {_tenantId}"));
		}
	public async Task<DataTable> RemoveRowsNotInColumnList(DataTable dataTable, List<string> allColumns) {
		if (dataTable == null || !dataTable.Columns.Contains("Name") || allColumns == null)
			return null;

		var rowsToDelete = dataTable.AsEnumerable()
			.Where(row => !allColumns.Contains(row.Field<string>("Name")))
			.ToList();

		foreach (var row in rowsToDelete) {
			dataTable.Rows.Remove(row);
			}
		return dataTable;
		}
	private void CopySourceScheama(DataGridView source, DataGridView destination) {
		destination.Columns.Clear();
		if (destination.Rows.Count > 0) destination.Rows.Clear();
		destination.ColumnHeadersDefaultCellStyle = source.ColumnHeadersDefaultCellStyle.Clone();
		destination.ColumnHeadersHeight = source.ColumnHeadersHeight;
		destination.ColumnHeadersHeightSizeMode = source.ColumnHeadersHeightSizeMode;
		destination.EnableHeadersVisualStyles = source.EnableHeadersVisualStyles;
		destination.RowHeadersWidth = source.RowHeadersWidth;
		foreach (DataGridViewColumn col in source.Columns) { // copy columns
			DataGridViewColumn ncol = (DataGridViewColumn)col.Clone();
			ncol.Width = col.Width;                    // Set the exact width
			ncol.MinimumWidth = col.MinimumWidth;      // Set minimum width
			ncol.FillWeight = col.FillWeight;          // Set fill weight for proportional sizing
			ncol.Resizable = col.Resizable;            // Copy resizable property
			ncol.AutoSizeMode = col.AutoSizeMode;             // DataGridViewAutoSizeColumnMode.None;// Disable auto-sizing to preserve the exact width
			destination.Columns.Add(ncol);// Add the column to destination
			}
		destination.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
		if (dgvRegisteredCDCCandidates.Columns.Count > 0)
			dgvRegisteredCDCCandidates.Columns[0].HeaderText = "CDC Candidates";

		}
	private async Task CommitObjectsAsDbArtefactsAsync(object sender, EventArgs e) {
		try {
			if (lbxObjects.SelectedItem == null) {// Validate selection
				toolStripStatusLabel1.Text = "Please select an object first.";
				return;
				}
			List<string> selectedFields = _config.Topics.GetFieldsToFilterByName((string)lbxObjects.SelectedItem);// Get selected fields
			_logger.LogDebug($"Selected fields: {string.Join(", ", selectedFields)}");
			_destinationTable.TableName = "sfSObjects";// Configure destination table
			_logger.LogDebug($"Processing {_destinationTable.Rows.Count} rows in destination table");
			foreach (DataRow row in _destinationTable.Rows) {// Process each row
				string tableName = row["name"]?.ToString();
				if (string.IsNullOrEmpty(tableName)) {
					_logger.LogWarning("Encountered empty table name, skipping...");
					continue;
					}
				try {
					DataSet schema = await _salesforceService.GetObjectSchemaAsDataSetAsync(tableName);// Fetch schema and process

					lbxObjects.Items.Add($"/data/{SalesforceService.ObjectNameToChangeEvent(tableName)}");
					} catch (Exception ex) {
					_logger.LogError($"Failed to process table {tableName}: {ex.Message}");
					continue;
					}
				}
			toolStripStatusLabel1.Text = $"Processed {_destinationTable.Rows.Count} tables. Select tables and fields in pub/sub tab.";
			} catch (Exception ex) {
			_logger.LogError($"Unexpected error during commit: {ex.Message}");
			toolStripStatusLabel1.Text = "Error processing tables. Check logs for details.";
			MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
	private void setControlColor(Object sender, bool c) {

		switch (sender.GetType().Name.ToLower()) {
			case "button":
				var o = (Button)sender;
				o.BackColor = c ? Color.Green : Color.Brown;

				break;
			default:
				Log($"sender gettype().name={sender.GetType().Name}", LogLevel.Info);
				break;
			}

		}
	private (string jsonString, int count) tableColumnToJsonArray(DataTable dt, string sColumn, string fColumn) {

		List<string?> fields = dt.AsEnumerable()
							.Where(r => !(r[fColumn] is bool b && b))
								.Select(r => r[sColumn]?.ToString())
								.Where(v => !string.IsNullOrEmpty(v))
								.ToList();
		return (JsonConvert.SerializeObject(fields), fields.Count);
		}
	#endregion helpers
	private static DataTable? removeCDCRegistered(DataTable dt, HashSet<string> exclude, string keyColumn = "Name") {
		if (dt == null || !dt.Columns.Contains("name") || exclude is null || exclude.Count == 0) return null;
		var rowsToDelete = dt.AsEnumerable()
			.Where(row => exclude.Contains(row.Field<string>(keyColumn)))
			.ToList();
		foreach (var row in rowsToDelete) dt.Rows.Remove(row);
		return dt;
		}
	private async Task loadCDCObjects() {
		if (_cdcObjectsLoaded) throw new Exception("Attempted loadCDCObjects() when _cdcObjectsLoded");
		Log("loadCDCObjects()", LogLevel.Debug);
		this.Invoke((Action)(() => Cursor.Current = Cursors.WaitCursor));
		_sourceTable = await _salesforceService.GetCDCEnabledEntitiesAsync();
		_dtRegisteredCDCCandidates = await _salesforceService.ExecSoqlToTable("SELECT SelectedEntity FROM PlatformEventChannelMember", useTooling: true);//Get subscribable entries from tooling 
		_dtRegisteredCDCCandidates = _dtRegisteredCDCCandidates.DeriveColumn("SelectedEntity", "name");// derive name from SelectedEntity
		var remRows = _dtRegisteredCDCCandidates.AsEnumerable() // remove rows from source that are already enabled for pubsub
			   .Select(row => row.Field<string>("name"))
			   .Where(value => !string.IsNullOrEmpty(value))
			   .ToHashSet();
		_dtRegisteredCDCCandidates.TableName = "RegisteredCDCCandidates";
		_dtRegisteredCDCCandidates.Columns.Add("Initialize", typeof(bool)); // Add a column for status icons
		_dtRegisteredCDCCandidates.Columns.Add("DB", typeof(Image)); // Add a column for status iconsRow
		_dtRegisteredCDCCandidates.Columns.Add("Create", typeof(Image));
		HashSet<string> replicatedNames = new HashSet<string>(
			 _sqlServerLib.Select("select name from [dbo].[ftTablesOfSchema] ('sfo')").AsEnumerable()
				.Select(r => r["name"].ToString())!, StringComparer.OrdinalIgnoreCase);
		_dtRegisteredCDCCandidates.AsEnumerable()
			.Where(r => remRows.Contains(r.Field<string>("name")))
			.ToList()
			.ForEach(r => {
				string name = r.Field<string>("name")!;
				if (!string.IsNullOrWhiteSpace(name) && replicatedNames.Contains(name)) {
					r["Create"] = Properties.Resources.CacheOk;
					r["Initialize"] = false;
					} else {
					r["Create"] = Properties.Resources.DocumentError;
					r["Initialize"] = true;
					_hasUnInitDbArtefacts = true;
					}
			});
		dgvCDCEnabledObjects.DataSource = removeCDCRegistered(_sourceTable, remRows!, "name");
		dgvRegisteredCDCCandidates.DataSource = null;
		dgvRegisteredCDCCandidates.DataSource = _dtRegisteredCDCCandidates;
		_cdcObjectsLoaded = true;
		}
	private async Task LoadSOQL() {
		Log("LoadSOQL", LogLevel.Debug);
		cmbSOQL.DataSource = _sqlServerLib.Select("select qkey,q,UseTooling from soqlqueries");
		cmbSOQL.DisplayMember = "qKey";
		cmbSOQL.ValueMember = "q";
		_soqlLoaded = true;
		}
	#region list and combo boxes
	private async void lbxObjects_SelectedIndexChanged(object sender, EventArgs e) {
		if (lbxObjects.SelectedItem == null) return;
		this.UseWaitCursor = true;
		await _semaphore.WaitAsync();
		try {
			string selectedTopic = lbxObjects.SelectedItem.ToString();
			string selectedObject = ObjectNameFromEventDeclaration(selectedTopic);
			dgvObject.DataSource = null;
			dgvRelations.DataSource = null;
			_sqlServerLib.AssertCDCObjectExist(selectedObject);// this will fire event to set series of changes 
			switch (_retrieveFrom) {// set by AssertCDCObjectExists
				case enmRetrieveFrom.SalesForce:
				
					DataSet ds = await _salesforceService.GetObjectSchemaAsDataSetAsync(selectedObject);// async operations outside the lock
					DataTable dtObject = ds.Tables[selectedObject];
					toolStripStatusLabel1.Text = $" {ObjectNameFromEventDeclaration(selectedTopic)} has {dtObject.Rows.Count} fields.";
					if (rbtFilterSubscribed.Checked) {
						dtObject = await RemoveRowsNotInColumnList(dtObject, _config.Topics.GetFieldsToFilterByName(selectedTopic));
						}
					lock (_dgvLock) {// Synchronize UI updates with lock
						this.Invoke((Action)(() => {
							dtObject.Columns["Name"]!.SetOrdinal(0);
							dtObject.Columns["type"]!.SetOrdinal(1);
							dtObject.Columns["length"]!.SetOrdinal(2);
							DataColumn dc = dtObject.Columns.Add("Exclude", typeof(bool));
							dc.DefaultValue = false;
							dc.SetOrdinal(3);
							dgvObject.DataSource = dtObject;
							dgvObject.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
							dgvObject.Columns["Exclude"]!.Width = 80;
							lblSelectedTable.Text = ObjectNameFromEventDeclaration(selectedTopic);
							dgvRelations.DataSource = ds.Tables["relations"];
							this.Invoke((Action)(() =>/* get only the field Names for the lbxFields*/  {
								lblSelectedTable.Text = ObjectNameFromEventDeclaration(selectedTopic);
								var r = tableColumnToJsonArray(dtObject, "Name", "Exclude");
								lblSelectedTable.Text += " - filtered:" + r.count.ToString();
								rtxFieldsJsonArray.Text = r.jsonString;
								//rtxFieldsJsonArray.Text = tableColumnToJsonArray(dtObject,"Name","Exclude");
								//	var r = tableColumnToJsonArray(ds.Tables["relations"], "Name", "Exclude");
								//	rtxFieldsJsonArray.Text = r.jsonString;
								//		lblSelectedTable.Text +=" - filtered:"+ r.count.ToString();
							}));
						}));
						}
					break;
				case enmRetrievedFrom.SqlServer:
					this.Invoke((Action)(() => {
						//string sqlSelect = $"select * from cdcObjectFields c join CDCObject p on c.CdcObject_Id=p.Id where p.ObjectName='{selectedObject}'";

						string sqlSelect = $"select * from ftcdcObjectFields('{selectedObject}',1)";
						dtObject = _sqlServerLib.Select(sqlSelect);
						dtObject.Columns["FieldName"]!.SetOrdinal(0);
						dgvObject.DataSource = dtObject;
						lblSelectedTable.Text = ObjectNameFromEventDeclaration(selectedTopic);
						toolStripStatusLabel1.Text = $"Object {selectedObject} already exists in the SQL Server.";
						btnDeleteCDCRegistration.Visible = true;
						btnRegisterFields.Text = "Update Fields";
						var r = tableColumnToJsonArray(dtObject, "FieldName", "IsExcluded");
						rtxFieldsJsonArray.Text = r.jsonString;
						lblSelectedTable.Text += " - filtered:" + r.count.ToString();
					}));
					break;
				}

			} catch (Exception ex) {
			this.Invoke((Action)(() => toolStripStatusLabel1.Text = $"Error: {ex.Message}"));
			}

		finally {
			_semaphore.Release();
			this.UseWaitCursor = false;
			}

		}
	#region lbxLog
	private void Log(string msg, LogLevel l, [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0, [CallerFilePath] string fp = "") {
		msg = $"{msg}:{callerMemberName}:{callerLineNumber}:{fp.Split('\\').Last()}";

		BeginInvoke(() => rtxLog.AppendText($"<%Info%> LogEmitted = {msg}{Environment.NewLine}"));
		}


	#endregion lbxLog
	private void cmbSOQL_SelectedIndexChanged(object sender, EventArgs e) {
		if (splitcSoql.Panel1Collapsed)
			splitcSoql.Panel1Collapsed = false;
		splitcSoql.Panel2Collapsed = !splitcSoql.Panel1Collapsed;

		if (cmbSOQL.SelectedValue == null) {
			rtSoqlQuery.Text = string.Empty;
			return;
			}
		var selectedRow = cmbSOQL.SelectedItem as DataRowView;
		if (selectedRow != null) {
			rtSoqlQuery.Text = selectedRow["Q"].ToString();

			} else {
			rtSoqlQuery.Text = string.Empty;
			}
		lblSoqlText.Text = $"Selected SOQL: {rtSoqlQuery.Text}";
		chkUseTooling.Checked = false;
		chkUseTooling.Checked = selectedRow != null && selectedRow["UseTooling"] != DBNull.Value && Convert.ToBoolean(selectedRow["UseTooling"]);



		}
	#endregion  list and combo boxes
	#region radio buttons
	private void filterChanged(object sender, EventArgs e) {
		dgvObject.DataSource = null;
		lblSelectedTable.Text = ""; // clethe dgvObject panel label
		rtxFieldsJsonArray.Text = "";
		var x = (RadioButton)sender;
		if (!x.Checked) return;
		bool showOnlySubscribed = (x == rbtFilterSubscribed);

		LoadTopics(lbxObjects, showOnlySubscribed);
		lblPanel1.Text = $"{lbxObjects.Items.Count} {(showOnlySubscribed == true ? "Subscribed" : "Registered")} CDC objects";

		}
	#endregion radio buttons
	#region tabs
	private void tabControl1_DrawItem(object sender, DrawItemEventArgs e) {
		TabPage tbp = tabControl1.TabPages[e.Index];
		Rectangle paddedBounds = new Rectangle(e.Bounds.X + 2, e.Bounds.Y + 2, e.Bounds.Width - 4, e.Bounds.Height - 4);
		using var bgBrush = new SolidBrush(_higlightTabs.Contains(e.Index) ? Color.Red : SystemColors.Control);
		using var tBrush = new SolidBrush(_higlightTabs.Contains(e.Index) ? Color.Yellow : SystemColors.ControlText);
		e.Graphics.FillRectangle(bgBrush, paddedBounds);
		e.Graphics.DrawString(tbp.Text, e.Font, tBrush, paddedBounds);
		}
	private async Task createCDCReplica(HashSet<string> list) {
		string script = "";
		foreach (string cdcEntry in list) {
			Log($"Processing {cdcEntry}", LogLevel.Debug);
			DataSet ds = await _salesforceService.GetObjectSchemaAsDataSetAsync(cdcEntry);
			if (ds != null) {
				script = _sqlServerLib.GenerateCreateTableScript(ds.Tables[0], "sfo", cdcEntry);
				_sqlServerLib.ExecuteNoneQuery(script);
				rtfLog.Text = script;
				} else Log($"Schema for the table {cdcEntry} could not be retrived..", LogLevel.Error);
			}
		}
	private async Task tabControl1_Selected(object sender, TabControlEventArgs e) {
		_logger.LogDebug($"(logger) tabpage={e.TabPage.Name}");
		switch (e.TabPage.Name.ToLower()) {
			case "tbpsfobjects":
				break;
			case "tbppubsub":
				LoadTopics(lbxObjects, rbtFilterSubscribed.Checked); // Load sfo Tables from sql server  topics into the listbox
				lblPanel1.Text = $"{lbxObjects.Items.Count} registered CDC objects";
				break;

			case "tbpcdcevents":
				_higlightTabs.Remove(e.TabPageIndex);
				tabControl1.Invalidate();
				break;
			case "tbpsoql":
				if (!_soqlLoaded) await LoadSOQL();
				break;
			case "tbpx12":
				if (dgvRegisteredCDCCandidates.DataSource != null) {
					DataTable dt = (DataTable)dgvRegisteredCDCCandidates.DataSource;
					cmbCDCTables.DataSource = dt.AsEnumerable().Select(r => r.Field<string>("name"))
											.Where(n => !string.IsNullOrEmpty(n))
											.ToList();
					}
				break;
			default: break;

			}
		if (_hasUnInitDbArtefacts) {
			HashSet<string> x = _dtRegisteredCDCCandidates.AsEnumerable()
					.Where(row => row.Field<bool>("Initialize"))
				 .Select(r => r.Field<string>("name"))
				 .ToHashSet()!;
			if (x.Count > 0) {
				createCDCReplica(x);
				}
			tabControl1.SelectedTab = e.TabPage;
			}
		}
	private async void TabControl1_Selected1(object sender, TabControlEventArgs e) {
		await tabControl1_Selected(sender, e); // Call the async Task method
		}
	#endregion tabs
	#region tooltip

	#endregion tooltip
	#region utility classes
	public class LogItem {
		public string Message { get; set; }
		public LogLevel Level { get; set; }
		public LogItem(string message, LogLevel level) {
			Message = message;
			Level = level;
			}
		public override string ToString() {
			return Message;
			}
		}



	#endregion utility classes
	private async void button30_Click(object sender, EventArgs e) {
		Size sw = new Size(tabControl1.ClientSize.Width, splitcSoql.Panel2.ClientSize.Height);
		splitcSoql.Panel2.ClientSize = sw;
		splitcSoql.Panel1Collapsed = true;
		tableLayoutPanel13.Width = sw.Width;
		dgvSOQLResult.DataSource = null;
		_dtSoqlResults = await _salesforceService.DescribeToolingObjectToDataTable("PlatformEventChannelMember");
		if (_dtSoqlResults.Columns.Contains("Id")) _dtSoqlResults.Columns["Id"].ReadOnly = true;
		dgvSOQLResult.DataSource = _dtSoqlResults;
		}
	private async void button31_Click(object sender, EventArgs e) {
		bool x = await _salesforceService.DeleteToolingRecord("PlatformEventChannelMember", "0v8DP0000004EsyYAE");
		}
	private void pictureBox2_Click(object sender, EventArgs e) {
		}
	private void cmbObjects_Validated(object sender, EventArgs e) {
		string newItem = cmbObjects.Text.Trim();
		if (!string.IsNullOrEmpty(newItem) && !cmbObjects.Items.Contains(newItem)) {
			cmbObjects.Items.Add(newItem);
			}
		var allItems = cmbObjects.Items.Cast<string>(); // Save all items as a comma-separated string
		string joined = string.Join(",", allItems);
		Properties.Settings.Default.cmbObjects = joined;
		Properties.Settings.Default.Save();
		}
	private void btnDeleteCmbObjectSelected_Click(object sender, EventArgs e) {
		var i = cmbObjects.SelectedItem;
		if (i != null) {
			cmbObjects.Items.Remove(i);
			var allItems = cmbObjects.Items.Cast<string>();
			string joined = string.Join(",", allItems);
			Properties.Settings.Default.cmbObjects = joined;
			Properties.Settings.Default.Save();
			}
		}
	private void cmbObjects_SelectedIndexChanged(object sender, EventArgs e) {
		if (cmbObjects.SelectedItem == null) return;
		lblCDCName.Text = SalesforceService.ObjectNameToChangeEvent(cmbObjects.SelectedItem.ToString());
		}
	private void cmbCDCTables_SelectedIndexChanged(object sender, EventArgs e) {
		string x = cmbCDCTables.SelectedItem?.ToString() ?? string.Empty;
		dgvCDCTables.DataSource = _sqlServerLib.Select($"select * from sfo.{x}");
		}
	private void grpDocChanged(object sender, EventArgs e) {
		var ISA = _x12.CreateISA(1, false, "s12312", "r123123");
		}
	private void btnLogTest_Click(object sender, EventArgs e) {
		_logger.LogError("This is an error message from the button click event.");
		_logger.LogEmail("This is an email log message from the button click event.");
		}
	}
