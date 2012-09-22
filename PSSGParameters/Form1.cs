using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;
using FreeImageAPI;
using System.Drawing;
using MiscUtil.Conversion;
using System.Linq;

namespace PSSGParameters
{
    public partial class Form1 : Form
    {
        // Export as XML[X]
        // 1.7.2 - Quick Float4 Fix To Make work with all textures
        // 1.8 - Brand New Base PSSG Class from Miek, Export/Import CubeMaps, Remove Textures, Improved UI, Import All Textures, Better Image Preview, Brand New DDS Class
		// 2.0 - Support for texelformats "ui8x4" and "u8", Auto-Update "All Sections", Export/Import Data Nodes, Add/Remove Attributes to Nodes, Hogs Less Resources, PSSG Backend, textures search bar
        // 2.0.1 - Fixed Tag Errors on Save, Improved Open/SaveFileDialog Usability, Cleaned Up Some Code, Reduced Size from Icon
		CPSSGFile pssg;
        string filePath = "";
        string[] args;
        private bool BackendMode
        {
            get
            {
                if (backendDataGridView.Columns.Count > 0)
                {
                    return true;
                } else {
                    return false;
                }
            }
        }

        public Form1(string[] args)
        {
            InitializeComponent();
            this.Icon = Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location);
			texturePictureBox.BackColor = ColorTranslator.FromHtml("#BFBFBF");
			cubeMapPictureBox.BackColor = ColorTranslator.FromHtml("#BFBFBF");
			// DataGridSetup
			dataGridView.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
			dataGridView.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.AutoSizeToAllHeaders;
			dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
			dataGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
			dataGridView.CellEndEdit += new DataGridViewCellEventHandler(dataGridView_CellValueChanged);
			// BackendDataGridView
			backendDataGridView.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
			backendDataGridView.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.AutoSizeToAllHeaders;
			backendDataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
			backendDataGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
			backendDataGridView.CellEndEdit += new DataGridViewCellEventHandler(backendDataGridView_CellEndEdit);
            // MainTabControl
            mainTabControl.SelectedIndexChanged += new EventHandler(mainTabControl_SelectedIndexChanged);
            mainTabControl.Selecting += new TabControlCancelEventHandler(mainTabControl_Selecting);
            textureImageLabel.Text = "";
            cubeMapImageLabel.Text = "";
            this.args = args;
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            // File Association Handler (if arg passed, try to open it)
            //args = new List<string>() { @"C:\Games\Steam\steamapps\common\f1 2011\cars\fe1\livery_main\textures_high\temo.pssg" }.ToArray();
            if (args.Length > 0)
            {
                filePath = args[0];
                clearVars(true);
                try
                {
                    pssg = new CPSSGFile(File.Open(filePath, FileMode.Open, FileAccess.Read));
                    setupEditor(MainTabs.Auto);
                }
                catch (Exception excp)
                {
                    // Fail
                    this.Text = "Ryder PSSG Editor";
                    MessageBox.Show("The program could not open this file!" + Environment.NewLine + Environment.NewLine + excp.Message, "Could Not Open", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            args = null;
        }

        #region MainMenu
        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            clearVars(true);
            pssg = new CPSSGFile();
            setupBackend(0);
        }
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog.FilterIndex = 1;
            if (!string.IsNullOrEmpty(filePath))
            {
                openFileDialog.FileName = Path.GetFileName(filePath);
                openFileDialog.InitialDirectory = Path.GetDirectoryName(filePath);
            }
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                filePath = openFileDialog.FileName;
                openFileDialog.Dispose();
                clearVars(true);
				try {
					pssg = new CPSSGFile(File.Open(filePath, FileMode.Open, FileAccess.Read));
					setupEditor(MainTabs.Auto);
				}
				catch (Exception excp) {
					// Fail
					this.Text = "Ryder PSSG Editor";
					MessageBox.Show("The program could not open this file!" + Environment.NewLine + Environment.NewLine + excp.Message, "Could Not Open", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
            }
        }
        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (pssg == null)
            {
                return;
            }
            saveFileDialog.FilterIndex = 1;
            saveFileDialog.FileName = Path.GetFileName(filePath);
            saveFileDialog.InitialDirectory = Path.GetDirectoryName(filePath);
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    saveFileDialog.Dispose();
                    tag();
                    pssg.Write(File.Open(saveFileDialog.FileName, FileMode.Create));
                    filePath = saveFileDialog.FileName;
				}
				catch (Exception ex) {
					MessageBox.Show("The program could not save this file! The error is displayed below:" + Environment.NewLine + Environment.NewLine + ex.Message, "Could Not Save", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
                if (BackendMode)
                {
                    int index = backendTreeView.SelectedNode.Index;
                    clearBackend();
                    setupBackend(index);
                }
                else
                {
                    int index = mainTabControl.SelectedIndex;
                    clearVars(false);
                    setupEditor((MainTabs)index);
                }
            }
        }

		private void stampToolStripMenuItem_Click(object sender, EventArgs e) {
            if (pssg == null)
            {
                return;
            }
			var query = from CNodeInfo stampInfo in pssg.nodeInfo
						where stampInfo.name == "RPETAG"
						select stampInfo;
			if (query.Count() > 0) {
				return;
			}
			CNodeInfo nodeInfo = pssg.AddNodeInfo("PSSGTAGR");
            if (nodeInfo == null)
            {
                return;
            }
			CAttributeInfo attributeInfo = pssg.AddAttributeInfo("pssgAuthorr", nodeInfo);
            if (attributeInfo == null)
            {
                return;
            }
			CNode node = pssg.AddNode(pssg.rootNode, nodeInfo.id);
            //string name = Microsoft.VisualBasic.Interaction.InputBox("Author Name:", "Tag PSSG", "Ryder25");
            CAttribute attribute = pssg.AddAttribute(node, attributeInfo.id, "AuthorName");
            clearBackend();
            setupBackend(0);
		}
		private void pSSGBackendToolStripMenuItem_Click(object sender, EventArgs e) {
			if (pssg == null) {
				return;
			}

            setupBackend(0);
		}
        private void tag()
        {
            CNodeInfo nodeInfo = pssg.AddNodeInfo("PSSGDATABASE");
            if (nodeInfo == null)
            {
                nodeInfo = pssg.GetNodeInfo("PSSGDATABASE")[0];
            }
            CAttributeInfo attributeInfo = pssg.AddAttributeInfo("creatorApplication", nodeInfo);
            if (attributeInfo == null)
            {
                attributeInfo = pssg.GetAttributeInfo("creatorApplication")[0];
            }
            CNode node;
            if (pssg.rootNode == null)
            {
                node = pssg.AddNode(pssg.rootNode, nodeInfo.id);
            }
            else
            {
                node = pssg.rootNode;
            }
            CAttribute attribute = pssg.AddAttribute(node, attributeInfo.id, "RyderPSSGEditor");
            if (attribute == null)
            {
                node.attributes[attributeInfo.name].data = "RyderPSSGEditor";
            }
        }

        private void clearVars(bool clearPSSG)
        {
			if (pssg == null) return;

			// All tab
			mainTabControl.SelectedTab = mainTabControl.TabPages["allTabPage"];
			treeView.Nodes.Clear();
			idTextBox.Text = "";
			richTextBox1.Text = "";
			dataGridView.Tag = null;
			dataGridView.Rows.Clear();
			dataGridView.Columns.Clear();
			dataGridView.BringToFront();

			// Textures tab
            textureImageLabel.Text = "";
			textureTreeView.Nodes.Clear();
			if (texturePictureBox.Image != null) {
				texturePictureBox.Image.Dispose();
                texturePictureBox.Image = null;
			}

            // CubeMap Tab
            cubeMapImageLabel.Text = "";
			cubeMapTreeView.Nodes.Clear();
			if (cubeMapPictureBox.Image != null) {
				cubeMapPictureBox.Image.Dispose();
                cubeMapPictureBox.Image = null;
			}

			// BackEnd Tab
			mainTabControl.BringToFront();
            clearBackend();

            this.Text = "Ryder PSSG Editor";
			if (clearPSSG == true) {
				pssg = null;
			}
        }
		private void setupEditor(MainTabs tabToSelect) {
			dataGridView.Columns.Add("valueColumn", "Value");
			dataGridView.Columns[0].SortMode = DataGridViewColumnSortMode.NotSortable;
			if (pssg.rootNode != null) {
				treeView.Nodes.Add(pssg.CreateTreeViewNode(pssg.rootNode));
				pssg.CreateSpecificTreeViewNode(textureTreeView, "TEXTURE");
				pssg.CreateSpecificTreeViewNode(cubeMapTreeView, "CUBEMAPTEXTURE");
			}
			// Select Starting Tab
			if (tabToSelect != MainTabs.Auto) {
				mainTabControl.SelectedTab = mainTabControl.TabPages[(int)tabToSelect];
			} else {
				mainTabControl.SelectedTab = mainTabControl.TabPages["allTabPage"];
				mainTabControl.SelectedTab = mainTabControl.TabPages["cubeMapTabPage"];
				mainTabControl.SelectedTab = mainTabControl.TabPages["texturesTabPage"];
			}
			this.Text = "Ryder PSSG Editor - " + Path.GetFileName(filePath);
		}
		public enum MainTabs {
			All, // allTabPage
			Textures, // texturesTabPage
			CubeMaps, // cubeMapTabPage
			Auto
		}
		#endregion

		#region BackEnd
		private void backendTreeView_AfterSelect(object sender, TreeViewEventArgs e) {
			if (backendTreeView.SelectedNode == null) {
				MessageBox.Show("Tree node not selected!", "Select a Node", MessageBoxButtons.OK, MessageBoxIcon.Stop);
				return;
			}
			CNodeInfo nodeInfo = pssg.nodeInfo[backendTreeView.SelectedNode.Index];
            createBackendView(nodeInfo);
		}
        private void createBackendView(CNodeInfo nodeInfo)
        {
            backendDataGridView.SuspendDrawing();
            backendDataGridView.Rows.Clear();
            backendDataGridView.TopLeftHeaderCell.Value = "Node Info ID " + nodeInfo.id.ToString();
            int i = 0;
            foreach (KeyValuePair<int, CAttributeInfo> pair in nodeInfo.attributeInfo)
            {
                backendDataGridView.Rows.Add(pair.Value.name);
                backendDataGridView.Rows[i].HeaderCell.Value = "AttrID " + pair.Key;
                backendDataGridView.Rows[i].Cells[0].ValueType = pair.Value.name.GetType();
                backendDataGridView.Rows[i].Tag = pair.Value;
                i++;
            }
            backendDataGridView.Tag = nodeInfo;
            backendDataGridView.ResumeDrawing();
        }
        private void clearBackend()
        {
            backendTreeView.Nodes.Clear();
            backendDataGridView.Tag = null;
            backendDataGridView.Rows.Clear();
            backendDataGridView.Columns.Clear();
        }
        private void setupBackend(int backendTreeViewSelectIndex)
        {
            if (BackendMode)
            {
                return;
            }
            backendTreeView.BeginUpdate();
            foreach (CNodeInfo nInfo in pssg.nodeInfo)
            {
                backendTreeView.Nodes.Add(nInfo.name);
            }
            backendTreeView.EndUpdate();
            backendDataGridView.Columns.Add("nameColumn", "Name");
            backendDataGridView.Columns[0].SortMode = DataGridViewColumnSortMode.NotSortable;
            if (backendTreeView.Nodes.Count > 0)
            {
                if (backendTreeView.Nodes.Count > backendTreeViewSelectIndex && backendTreeViewSelectIndex >= 0)
                {
                    backendTreeView.SelectedNode = backendTreeView.Nodes[backendTreeViewSelectIndex];
                }
                else
                {
                    backendTreeView.SelectedNode = backendTreeView.Nodes[0];
                }
            }
            backendTreeView.Focus();
            backendPanel.BringToFront();
        }
		private void backendDataGridView_CellEndEdit(object sender, DataGridViewCellEventArgs e) {
			CNodeInfo nodeInfo = ((CNodeInfo)backendDataGridView.Tag);
			CAttributeInfo attrInfo = (CAttributeInfo)backendDataGridView.Rows[e.RowIndex].Tag;
			string oldName = attrInfo.name;
			if (oldName == (string)backendDataGridView.Rows[e.RowIndex].Cells[0].Value) {
				return;
			} else if (pssg.GetAttributeInfo((string)backendDataGridView.Rows[e.RowIndex].Cells[0].Value).Length > 0) {
				backendDataGridView.Rows[e.RowIndex].Cells[0].Value = attrInfo.name;
				return;
			}

			attrInfo.name = (string)backendDataGridView.Rows[e.RowIndex].Cells[0].Value;
			pssg.attributeInfo[attrInfo.id - 1].name = attrInfo.name;
			nodeInfo.attributeInfo[attrInfo.id].name = attrInfo.name;
			string newName = attrInfo.name;
			List<CNode> queryNodes = pssg.FindNodes(nodeInfo.name, oldName);
			foreach (CNode node in queryNodes) {
				Dictionary<string, CAttribute> attributes = new Dictionary<string, CAttribute>();
				foreach (KeyValuePair<string, CAttribute> pair in node.attributes) {
					if (pair.Key != oldName) {
						attributes.Add(pair.Key, pair.Value);
					} else {
						attributes.Add(newName, pair.Value);
					}
				}
				node.attributes = attributes;
			}
		}

		private void backendAddToolStripButton_Click(object sender, EventArgs e) {
			DialogResult result = MessageBox.Show("What would you like to add?" + Environment.NewLine + Environment.NewLine + 
				"Select Yes for NodeInfo, or No for AttributeInfo.", "Add Info", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
			if (result == System.Windows.Forms.DialogResult.Yes) {
				// Add NodeInfo
                CNodeInfo nInfo = pssg.AddNodeInfo("NodeInfo" + (pssg.nodeInfo.Length + 1));
				if (nInfo == null) {
                    return;
                }
                clearBackend();
                setupBackend(nInfo.id - 1);
			} else if (result == System.Windows.Forms.DialogResult.No) {
                if (backendTreeView.SelectedNode == null)
                {
                    return;
                }
				// Add AttributeInfo
				CAttributeInfo aInfo = pssg.AddAttributeInfo("AttributeInfo" + (pssg.attributeInfo.Length + 1), (CNodeInfo)backendDataGridView.Tag);
                if (aInfo == null)
                {
                    return;
                }
                createBackendView((CNodeInfo)backendDataGridView.Tag);
			}
		}
		private void backendRenameToolStripButton_Click(object sender, EventArgs e) {
			if (backendTreeView.SelectedNode == null) {
				return;
			}

			CNodeInfo nodeInfo = pssg.nodeInfo[backendTreeView.SelectedNode.Index];
			string name = Microsoft.VisualBasic.Interaction.InputBox("Node Name:", "Rename Node", nodeInfo.name);
			if (name != "" && pssg.GetNodeInfo(name).Length == 0) {
				nodeInfo.name = name;
				backendTreeView.SelectedNode.Text = name;
			}
		}
		private void backendRemoveToolStripButton_Click(object sender, EventArgs e) {
			if (backendTreeView.SelectedNode == null) {
				return;
			}

            int index = backendTreeView.SelectedNode.Index;
            CNodeInfo nodeInfo = pssg.nodeInfo[index];
			if (backendDataGridView.SelectedRows.Count == 0) {
				pssg.RemoveNodeInfo(nodeInfo.id);
                clearBackend();
                setupBackend(index);
			} else {
				CAttributeInfo attrInfo = (CAttributeInfo)backendDataGridView.SelectedRows[0].Tag;
				pssg.RemoveAttributeInfo(attrInfo.id);
                createBackendView(nodeInfo);
			}
		}

		private void backendCloseToolStripButton_Click(object sender, EventArgs e) {
			if (backendTreeView.Nodes.Count == 0) {
				return;
			}
			clearVars(false);
			setupEditor(MainTabs.Auto);
		}
		#endregion

		#region All
		private void treeView_AfterSelect(object sender, TreeViewEventArgs e) {
			if (treeView.SelectedNode == null && textureTreeView.SelectedNode == null) {
				MessageBox.Show("Tree node not selected!", "Select a Node", MessageBoxButtons.OK, MessageBoxIcon.Stop);
				return;
			}

			CNode node = ((CNode)treeView.SelectedNode.Tag);
			if (node.attributes.ContainsKey("id") == true) {
				idTextBox.Text = node.attributes["id"].Value.ToString();
			}
			createView(node);
		}
		private void createView(CNode node) {
			// Hide show buttons based on Node Type
			if (node.isDataNode == true) {
				allExportDataToolStripButton.Visible = true;
				allImportDataToolStripButton.Visible = true;
			} else {
				allExportDataToolStripButton.Visible = false;
				allImportDataToolStripButton.Visible = false;
			}
			if (node.attributes.Count > 0) {
				allExportAttribToolStripButton.Visible = true;
				allImportAttribToolStripButton.Visible = true;
			} else {
				allExportAttribToolStripButton.Visible = false;
				allImportAttribToolStripButton.Visible = false;
			}
			// Determine if we need a DataGridView or a RichTextBox based on data to be displayed
			if (node.isDataNode == true && node.attributes.Count == 0) {
				richTextBox1.Text = EndianBitConverter.ToString(node.data);
				richTextBox1.BringToFront();
			} else {
				dataGridView.Rows.Clear();
				dataGridView.TopLeftHeaderCell.Value = node.Name;
				int i = 0;
				foreach (KeyValuePair<string, CAttribute> pair in node.attributes) {
					dataGridView.Rows.Add(pair.Value.Value);
					dataGridView.Rows[i].HeaderCell.Value = pair.Key;
					dataGridView.Rows[i].Cells[0].ValueType = pair.Value.Value.GetType();
					dataGridView.Rows[i].Tag = pair.Value;
					i++;
				}
				dataGridView.Tag = node;
				dataGridView.BringToFront();
			}
		}
		private void dataGridView_CellValueChanged(object sender, DataGridViewCellEventArgs e) {
			CNode node = ((CNode)dataGridView.Tag);
			string attrName = (string)dataGridView.Rows[e.RowIndex].HeaderCell.Value;
			CAttribute attr = node.attributes[(string)dataGridView.Rows[e.RowIndex].HeaderCell.Value];
			if (attr.Value == dataGridView.Rows[e.RowIndex].Cells[0].Value) {
				return;
			}
			switch (attr.Value.GetType().ToString()) {
				case "System.UInt16":
					attr.data = EndianBitConverter.Big.GetBytes((UInt16)dataGridView.Rows[e.RowIndex].Cells[0].Value);
					break;
				case "System.Int16":
					attr.data = EndianBitConverter.Big.GetBytes((Int16)dataGridView.Rows[e.RowIndex].Cells[0].Value);
					break;
				case "System.UInt32":
					attr.data = EndianBitConverter.Big.GetBytes((uint)dataGridView.Rows[e.RowIndex].Cells[0].Value);
					break;
				case "System.Int32":
					attr.data = EndianBitConverter.Big.GetBytes((int)dataGridView.Rows[e.RowIndex].Cells[0].Value);
					break;
				case "System.String":
					if ((string)attr.Value == "Byte Data - Do Not Edit") {
						dataGridView.Rows[e.RowIndex].Cells[0].Value = "Byte Data - Do Not Edit";
						return;
					}
					attr.data = dataGridView.Rows[e.RowIndex].Cells[0].Value;
					break;
				case "System.Single":
					attr.data = EndianBitConverter.Big.GetBytes((float)dataGridView.Rows[e.RowIndex].Cells[0].Value);
					break;
				default:
					dataGridView.Rows[e.RowIndex].Cells[0].Value = attr.Value;
					MessageBox.Show("This field cannot be edited!", "Stop", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					break;
			}
		}

		private void allAddToolStripButton_Click(object sender, EventArgs e) {
			if ((treeView.Nodes.Count == 0 || treeView.SelectedNode.Index == -1) && pssg != null && pssg.rootNode != null) {
				return;
			} else if (pssg == null) {
				return;
            }
            if (pssg.nodeInfo.Length == 0)
            {
                return;
            }

			CNode parentNode = pssg.rootNode == null ? null : ((CNode)treeView.SelectedNode.Tag);
            using (AddBox aBox = new AddBox(pssg))
            {
                if (aBox.ShowDialog() == DialogResult.OK)
                {
                    if (aBox.AddType == 0)
                    {
                        if (pssg.AddNode(parentNode, aBox.NodeID) == null)
                        {
                            return;
                        }
                        clearVars(false);
                        setupEditor(MainTabs.All);
                    }
                    else
                    {
                        if (pssg.AddAttribute(parentNode, aBox.AttributeID, Convert.ChangeType(aBox.Value, aBox.ValueType)) == null)
                        {
                            return;
                        }
                        createView(parentNode);
                    }
                }
            }
		}
		private void allRemoveToolStripButton_Click(object sender, EventArgs e) {
			if (treeView.Nodes.Count == 0 || treeView.SelectedNode.Index == -1) {
				return;
			}

			CNode node = ((CNode)treeView.SelectedNode.Tag);
			if (dataGridView.SelectedRows.Count == 0) {
				pssg.RemoveNode(node);
				clearVars(false);
				setupEditor(MainTabs.All);
			} else {
				CAttribute attr = (CAttribute)dataGridView.Rows[dataGridView.SelectedRows[0].Index].Tag;
				pssg.RemoveAttribute(node, attr.Name);
				createView(node);
			}
		}

		private void allExportAttribToolStripButton_Click(object sender, EventArgs e) {
			if (treeView.Nodes.Count == 0 || treeView.SelectedNode.Index == -1) {
				return;
			}

			SaveFileDialog dialog = new SaveFileDialog();
			dialog.Filter = "BIN files|*.bin|All files|*.*";
			dialog.Title = "Select the attributes data save location and file name";
			dialog.DefaultExt = "bin";
			dialog.FileName = "nodeAttribs.bin";
			if (dialog.ShowDialog() == DialogResult.OK) {
				try {
					CNode node = ((CNode)treeView.SelectedNode.Tag);
					using (EndianBinaryWriterEx writer = new EndianBinaryWriterEx(new BigEndianBitConverter(), File.Open(dialog.FileName, FileMode.Create))) {
						int i = 1;

						foreach (KeyValuePair<string, CAttribute> attrib in node.attributes) {
							if (attrib.Value.data is string) {
								writer.Write(System.Text.Encoding.UTF8.GetBytes((string)attrib.Value.data));
							} else if (attrib.Value.data is UInt16) {
								writer.Write((UInt16)attrib.Value.data);
							} else if (attrib.Value.data is UInt32) {
								writer.Write((UInt32)attrib.Value.data);
							} else if (attrib.Value.data is Int16) {
								writer.Write((Int16)attrib.Value.data);
							} else if (attrib.Value.data is Int32) {
								writer.Write((Int32)attrib.Value.data);
							} else if (attrib.Value.data is Single) {
								writer.Write((Single)attrib.Value.data);
							} else {
								writer.Write((byte[])attrib.Value.data);
							}
							writer.Write(BitConverter.GetBytes(i)[0]);
							writer.Write(System.Text.Encoding.UTF8.GetBytes(attrib.Value.Name));
							//writer.Write(System.Text.Encoding.UTF8.GetBytes("RAS"));
							writer.Write(BitConverter.GetBytes(i)[0]);
							i++;
						}
					}
				}
				catch (Exception ex) {
					MessageBox.Show("Could not export attributes!" + Environment.NewLine + Environment.NewLine +
						ex.Message, "Export Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
		}
		private void allExportToolStripButton_Click(object sender, EventArgs e) {
			if (treeView.Nodes.Count == 0 || treeView.SelectedNode.Index == -1) {
				return;
			}

			SaveFileDialog dialog = new SaveFileDialog();
			dialog.Filter = "BIN files|*.bin|All files|*.*";
			dialog.Title = "Select the byte data save location and file name";
			dialog.DefaultExt = "bin";
			dialog.FileName = "nodeData.bin";
			if (dialog.ShowDialog() == DialogResult.OK) {
				try {
					CNode node = ((CNode)treeView.SelectedNode.Tag);
					using (EndianBinaryWriterEx writer = new EndianBinaryWriterEx(new BigEndianBitConverter(), File.Open(dialog.FileName, FileMode.Create))) {
						writer.Write(node.data);
					}
				}
				catch (Exception ex) {
					MessageBox.Show("Could not export data!" + Environment.NewLine + Environment.NewLine +
						ex.Message, "Export Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
		}

		private void allImportAttribToolStripButton_Click(object sender, EventArgs e) {
			if (treeView.Nodes.Count == 0 || treeView.SelectedNode.Index == -1) {
				return;
			}

			OpenFileDialog dialog = new OpenFileDialog();
			dialog.Filter = "BIN files|*.bin|All files|*.*";
			dialog.Title = "Select a bin file";
			dialog.FileName = "nodeAttribs.bin";
			if (dialog.ShowDialog() == DialogResult.OK) {
				try {
					CNode node = ((CNode)treeView.SelectedNode.Tag);
					/*using (EndianBinaryReaderEx reader = new EndianBinaryReaderEx(new BigEndianBitConverter(), File.Open(dialog.FileName, FileMode.Open, FileAccess.Read))) {
						List<byte> bytes = new List<byte>();
						List<string> keys = node.attributes.Keys.ToList();
						int i = 1;
						while (reader.BaseStream.Position < reader.BaseStream.Length) {
							bytes.Add(reader.ReadByte());
							if (bytes[bytes.Count - 1] == BitConverter.GetBytes(i)[0]) {
								byte[] sep = reader.ReadBytes(3);
								if (System.Text.Encoding.UTF8.GetString(sep) == "RAS" && reader.ReadByte() == BitConverter.GetBytes(i)[0]) {
									bytes.RemoveAt(bytes.Count - 1);
									if (node.attributes[keys[i - 1]].data is string || bytes.Count > 4) {
										node.attributes[keys[i - 1]].data = System.Text.Encoding.UTF8.GetString(bytes.ToArray());
									} else {
										node.attributes[keys[i - 1]].data = bytes.ToArray();
									}
									bytes.Clear();
									i++;
								} else {
									reader.BaseStream.Position -= 3;
								}
							}
						}
						createView(node);
					}*/
					using (EndianBinaryReaderEx reader = new EndianBinaryReaderEx(new BigEndianBitConverter(), File.Open(dialog.FileName, FileMode.Open, FileAccess.Read))) {
						List<byte> bytes = new List<byte>();
						List<string> keys = node.attributes.Keys.ToList();
						reader.BaseStream.Position = reader.BaseStream.Length - 1;
						int i = (int)reader.ReadByte();
						if (keys.Count != i) {
							throw new Exception("The attributes in the PSSG is different from the attributes in this file.");
						} else {
							reader.BaseStream.Position = 0;
							i = 1;
						}
						while (reader.BaseStream.Position < reader.BaseStream.Length) {
							bytes.Add(reader.ReadByte());
							if (bytes[bytes.Count - 1] == BitConverter.GetBytes(i)[0]) {
								byte[] sep = reader.ReadBytes(keys[i - 1].Length);
								if (System.Text.Encoding.UTF8.GetString(sep) == keys[i - 1] && reader.ReadByte() == BitConverter.GetBytes(i)[0]) {
									bytes.RemoveAt(bytes.Count - 1);
									if (node.attributes[keys[i - 1]].data is string || bytes.Count > 4) {
										node.attributes[keys[i - 1]].data = System.Text.Encoding.UTF8.GetString(bytes.ToArray());
									} else {
										node.attributes[keys[i - 1]].data = bytes.ToArray();
									}
									bytes.Clear();
									i++;
								} else {
									reader.BaseStream.Position -= keys[i - 1].Length;
								}
							}
						}
						createView(node);
					}
				}
				catch (Exception ex) {
					MessageBox.Show("Could not import attributes!" + Environment.NewLine + Environment.NewLine +
						ex.Message, "Import Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
		}
		private void allImportToolStripButton_Click(object sender, EventArgs e) {
			if (treeView.Nodes.Count == 0 || treeView.SelectedNode.Index == -1) {
				return;
			}

			OpenFileDialog dialog = new OpenFileDialog();
			dialog.Filter = "BIN files|*.bin|All files|*.*";
			dialog.Title = "Select a bin file";
			dialog.FileName = "nodeData.bin";
			if (dialog.ShowDialog() == DialogResult.OK) {
				try {
					CNode node = ((CNode)treeView.SelectedNode.Tag);
					using (EndianBinaryReaderEx reader = new EndianBinaryReaderEx(new BigEndianBitConverter(), File.Open(dialog.FileName, FileMode.Open, FileAccess.Read))) {
						List<byte> bytes = new List<byte>();
						while (reader.BaseStream.Position < reader.BaseStream.Length) {
							bytes.AddRange(reader.ReadBytes(128));
						}
						node.data = bytes.ToArray();
						createView(node);
					}
				}
				catch (Exception ex) {
					MessageBox.Show("Could not import data!" + Environment.NewLine + Environment.NewLine +
						ex.Message, "Import Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
		}

		public void ReadSection(string filePath) {
			using (EndianBinaryReaderEx reader = new EndianBinaryReaderEx(new BigEndianBitConverter(), File.Open(filePath, FileMode.Open, FileAccess.Read))) {
				CNode node = ((CNode)treeView.SelectedNode.Tag);
			}
		}
		public void WriteSection(string filePath) {
			using (EndianBinaryWriterEx writer = new EndianBinaryWriterEx(new BigEndianBitConverter(), File.Open(filePath, FileMode.Create))) {
				CNode node = ((CNode)treeView.SelectedNode.Tag);
				writer.Write(node.id);
				writer.Write(0);
				writer.Write(0);
				if (node.attributes != null) {
					foreach (KeyValuePair<string, CAttribute> attr in node.attributes) {
						writer.Write(attr.Value.id);
						writer.Write(attr.Value.Size);
						if (attr.Value.data is string) {
							writer.WritePSSGString((string)attr.Value.data);
						} else {
							writer.Write((byte[])attr.Value.data);
						}
					}
				}
				if (node.isDataNode) {
					writer.Write(node.data);
				}
			}
		}
		#endregion

		#region Textures
		private void textureTreeView_AfterSelect(object sender, TreeViewEventArgs e) {
			treeView.SelectedNode = ((CNode)textureTreeView.SelectedNode.Tag).TreeNode;
			//SelectCorrespondingNode(treeView.Nodes[0], ((CNode)textureTreeView.SelectedNode.Tag).attributes["id"].ToString());
			// Create Preview
			createPreview(((CNode)textureTreeView.SelectedNode.Tag));
		}
		private bool SelectCorrespondingNode(TreeNode tNode, string id) {
			CNode tag = (CNode)tNode.Tag;
			if (tag.attributes.ContainsKey("id") == true) {
				if (tag.attributes["id"].ToString() == id) {
					treeView.SelectedNode = tNode;
					return true;
				} else {
					foreach (TreeNode sub in tNode.Nodes) {
						bool result = SelectCorrespondingNode(sub, id);
						if (result == true) {
							return result;
						}
					}
				}
			} else {
				foreach (TreeNode sub in tNode.Nodes) {
					bool result = SelectCorrespondingNode(sub, id);
					if (result == true) {
						return result;
					}
				}
			}
			return false;
		}
		private void createPreview(CNode node) {
			// Make Preview
			try {
                textureImageLabel.Text = "";
				int height = 0; int width = 0;
				texturePictureBox.Dock = DockStyle.Fill;
				height = texturePictureBox.Height;
				width = texturePictureBox.Width;
				DDS dds = new DDS(node, false);
				dds.Write(File.Open(Application.StartupPath + "\\temp.dds", FileMode.Create, FileAccess.ReadWrite, FileShare.Read), -1);
				// Dispose of Old Images
				if (texturePictureBox.Image != null) {
					texturePictureBox.Image.Dispose();
                    texturePictureBox.Image = null;
				}
				// Setup New Image
				FREE_IMAGE_FORMAT format = FREE_IMAGE_FORMAT.FIF_DDS;
				System.Drawing.Bitmap image = FreeImage.LoadBitmap(Application.StartupPath + "\\temp.dds", FREE_IMAGE_LOAD_FLAGS.DEFAULT, ref format);
				if (image.Height <= height && image.Width <= width) {
					texturePictureBox.Dock = DockStyle.None;
					texturePictureBox.Width = image.Width;
					texturePictureBox.Height = image.Height;
				}
				texturePictureBox.Image = image;
			}
			catch (Exception ex) {
				if (texturePictureBox.Image != null) {
					texturePictureBox.Image.Dispose();
                    texturePictureBox.Image = null;
				}
                textureImageLabel.Text = "Could not create preview! Export/Import may still work in certain circumstances." + Environment.NewLine + Environment.NewLine + ex.Message;
				//MessageBox.Show("Could not create preview! Export/Import may still work in certain circumstances." + Environment.NewLine + Environment.NewLine 
				//	+ ex.Message, "No Preview", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}
		private void texturesTextBox_TextChanged(object sender, EventArgs e) {
			textureTreeView.Nodes.Clear();
			pssg.CreateSpecificTreeViewNode(textureTreeView, "TEXTURE");
            if (textureTreeView.Nodes.Count == 0)
            {
                return;
            }

			textureTreeView.BeginUpdate();
			for (int i = 0; i < textureTreeView.Nodes.Count; i++) {
				if (textureTreeView.Nodes[i].Text.StartsWith(texturesTextBox.Text, StringComparison.CurrentCultureIgnoreCase) ||
					textureTreeView.Nodes[i].Text.Contains(texturesTextBox.Text.ToLower())) {
				} else {
					textureTreeView.Nodes[i].Remove();
					i--;
				}
			}
			textureTreeView.EndUpdate();
			if (textureTreeView.Nodes.Count > 0) {
				textureTreeView.SelectedNode = textureTreeView.Nodes[0];
			}
		}

		private void exportToolStripButton_Click(object sender, EventArgs e) {
			if (textureTreeView.Nodes.Count == 0 || textureTreeView.SelectedNode.Index == -1) {
				return;
			}
			CNode node = ((CNode)textureTreeView.SelectedNode.Tag);
			SaveFileDialog dialog = new SaveFileDialog();
			dialog.Filter = "DDS files|*.dds|All files|*.*";
			dialog.Title = "Select the dds save location and file name";
			dialog.FileName = node.attributes["id"].ToString() + ".dds";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    DDS dds = new DDS(node, false);
                    dds.Write(File.Open(dialog.FileName, FileMode.Create), -1);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Could not export texture!" + Environment.NewLine + Environment.NewLine +
                        ex.Message, "Export Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
		}
		private void exportAllToolStripButton_Click(object sender, EventArgs e) {
			if (textureTreeView.Nodes.Count == 0) {
				return;
			}
			try {
				Directory.CreateDirectory(filePath.Replace(".", "_") + "_textures");
				DDS dds;
				for (int i = 0; i < textureTreeView.Nodes.Count; i++) {
					dds = new DDS(((CNode)textureTreeView.Nodes[i].Tag), false);
					dds.Write(File.Open(filePath.Replace(".", "_") + "_textures" + "\\" + textureTreeView.Nodes[i].Text + ".dds", FileMode.Create), -1);
				}
				MessageBox.Show("Textures exported successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			catch {
				MessageBox.Show("There was an error, could not export all textures!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void importToolStripButton_Click(object sender, EventArgs e) {
			if (textureTreeView.Nodes.Count == 0 || textureTreeView.SelectedNode.Index == -1) {
				return;
			}
			CNode node = ((CNode)textureTreeView.SelectedNode.Tag);
			OpenFileDialog dialog = new OpenFileDialog();
			dialog.Filter = "DDS files|*.dds|All files|*.*";
			dialog.Title = "Select a dds file";
			dialog.FileName = node.attributes["id"].ToString() + ".dds";
			if (dialog.ShowDialog() == DialogResult.OK) {
                try
                {
                    DDS dds = new DDS(File.Open(dialog.FileName, FileMode.Open));
                    dds.Write(node);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Could not import texture!" + Environment.NewLine + Environment.NewLine +
                        ex.Message, "Import Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
				createPreview(node);
			}
		}
		private void importAllToolStripButton_Click(object sender, EventArgs e) {
			if (textureTreeView.Nodes.Count == 0) {
				return;
			}
			try {
				string directory = filePath.Replace(".", "_") + "_textures";
				if (Directory.Exists(directory) == true) {
					DDS dds;
					foreach (string fileName in Directory.GetFiles(directory, "*.dds")) {
						for (int i = 0; i < textureTreeView.Nodes.Count; i++) {
							if (Path.GetFileNameWithoutExtension(fileName) == textureTreeView.Nodes[i].Text) {
								dds = new DDS(File.Open(fileName, FileMode.Open));
								dds.Write(((CNode)textureTreeView.Nodes[i].Tag));
								break;
							}
						}
					}
					if (textureTreeView.SelectedNode != null) {
						createPreview(((CNode)textureTreeView.SelectedNode.Tag));
					}
					MessageBox.Show("Textures imported successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
				} else {
					MessageBox.Show("Could not find textures folder!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
			catch {
				MessageBox.Show("There was an error, could not export all textures!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void addToolStripButton_Click(object sender, EventArgs e) {
			if (textureTreeView.SelectedNode == null)
			{
				MessageBox.Show("Select a texture to copy first!", "Select a Texture", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			AddTexture ATForm = new AddTexture(idTextBox.Text + "_2");
			if (ATForm.ShowDialog() == DialogResult.OK)
			{
				// Copy and Edit Name
				CNode nodeToCopy = (CNode)textureTreeView.SelectedNode.Tag;
				CNode newTexture = new CNode(nodeToCopy);
				newTexture.attributes["id"].data = ATForm.TName;
				// Add to Library
				Array.Resize(ref nodeToCopy.ParentNode.subNodes, nodeToCopy.ParentNode.subNodes.Length + 1);
				nodeToCopy.ParentNode.subNodes[nodeToCopy.ParentNode.subNodes.Length - 1] = newTexture;
				// Populate treeViews
				clearVars(false);
				setupEditor(MainTabs.Textures);
				textureTreeView.SelectedNode = textureTreeView.Nodes[textureTreeView.Nodes.Count - 1];
			}
		}
		private void removeToolStripButton_Click(object sender, EventArgs e) {
			if (textureTreeView.SelectedNode == null) {
				return;
			}
			// Remove from Parent Node
			CNode textureNode = (CNode)textureTreeView.SelectedNode.Tag;
			CNode parentNode = textureNode.ParentNode;
			List<CNode> list = new List<CNode>(parentNode.subNodes);
			list.Remove(textureNode);
			parentNode.subNodes = list.ToArray();
			textureNode = null;
			clearVars(false);
			setupEditor(MainTabs.Textures);
		}
		#endregion

		#region CubeMaps
		private void cubeMapTreeView_AfterSelect(object sender, TreeViewEventArgs e) {
			treeView.SelectedNode = ((CNode)cubeMapTreeView.SelectedNode.Tag).TreeNode;
			cubeMapPictureBox.Tag = 0;
			CubeMapCreatePreview(((CNode)cubeMapTreeView.SelectedNode.Tag), 0);
		}
		private void cubeMapPictureBox_Click(object sender, EventArgs e) {
			CubeMapCreatePreview(((CNode)cubeMapTreeView.SelectedNode.Tag), (int)cubeMapPictureBox.Tag + 1);
		}
		private void CubeMapCreatePreview(CNode node, int targetCount) {
			// Make Preview
            try
            {
                cubeMapImageLabel.Text = "";
				int height = 0; int width = 0;
				cubeMapPictureBox.Dock = DockStyle.Fill;
				height = cubeMapPictureBox.Height;
				width = cubeMapPictureBox.Width;
				FREE_IMAGE_FORMAT format = FREE_IMAGE_FORMAT.FIF_DDS;
				System.Drawing.Bitmap image = null;
				if (targetCount > 5) {
					targetCount = 0;
					cubeMapPictureBox.Tag = 0;
				} else {
					cubeMapPictureBox.Tag = targetCount;
				}
				DDS dds = new DDS(node, false);
				dds.Write(File.Open(Application.StartupPath + "\\temp.dds", FileMode.Create), targetCount);
				image = FreeImage.LoadBitmap(Application.StartupPath + "\\temp.dds", FREE_IMAGE_LOAD_FLAGS.DEFAULT, ref format);
				if (cubeMapPictureBox.Image != null) {
					cubeMapPictureBox.Image.Dispose();
                    cubeMapPictureBox.Image = null;
				}
				/*foreach (CNode sub in node.subNodes) {
					if (targetCount == 0 && sub.attributes["typename"].ToString() == "Raw") {
						CubeMapWriteDDS(Application.StartupPath + "\\temp" + "Raw" + ".dds", node, targetCount);
						image = FreeImage.LoadBitmap(Application.StartupPath + "\\temp" + "Raw" + ".dds", FREE_IMAGE_LOAD_FLAGS.DEFAULT, ref format);
					} else if (targetCount == 1 && sub.attributes["typename"].ToString() == "RawNegativeX") {
						CubeMapWriteDDS(Application.StartupPath + "\\temp" + "RawNegativeX" + ".dds", node, targetCount);
						image = FreeImage.LoadBitmap(Application.StartupPath + "\\temp" + "RawNegativeX" + ".dds", FREE_IMAGE_LOAD_FLAGS.DEFAULT, ref format);
					} else if (targetCount == 2 && sub.attributes["typename"].ToString() == "RawPositiveY") {
						CubeMapWriteDDS(Application.StartupPath + "\\temp" + "RawPositiveY" + ".dds", node, targetCount);
						image = FreeImage.LoadBitmap(Application.StartupPath + "\\temp" + "RawPositiveY" + ".dds", FREE_IMAGE_LOAD_FLAGS.DEFAULT, ref format);
					} else if (targetCount == 3 && sub.attributes["typename"].ToString() == "RawNegativeY") {
						CubeMapWriteDDS(Application.StartupPath + "\\temp" + "RawNegativeY" + ".dds", node, targetCount);
						image = FreeImage.LoadBitmap(Application.StartupPath + "\\temp" + "RawNegativeY" + ".dds", FREE_IMAGE_LOAD_FLAGS.DEFAULT, ref format);
					} else if (targetCount == 4 && sub.attributes["typename"].ToString() == "RawPositiveZ") {
						CubeMapWriteDDS(Application.StartupPath + "\\temp" + "RawPositiveZ" + ".dds", node, targetCount);
						image = FreeImage.LoadBitmap(Application.StartupPath + "\\temp" + "RawPositiveZ" + ".dds", FREE_IMAGE_LOAD_FLAGS.DEFAULT, ref format);
					} else if (targetCount == 5 && sub.attributes["typename"].ToString() == "RawNegativeZ") {
						CubeMapWriteDDS(Application.StartupPath + "\\temp" + "RawNegativeZ" + ".dds", node, targetCount);
						image = FreeImage.LoadBitmap(Application.StartupPath + "\\temp" + "RawNegativeZ" + ".dds", FREE_IMAGE_LOAD_FLAGS.DEFAULT, ref format);
					}
				}*/
				if (image.Height <= height && image.Width <= width) {
					cubeMapPictureBox.Dock = DockStyle.None;
					cubeMapPictureBox.Width = image.Width;
					cubeMapPictureBox.Height = image.Height;
				}
				cubeMapPictureBox.Image = image;
			}
			catch {
                if (cubeMapPictureBox.Image != null)
                {
                    cubeMapPictureBox.Image.Dispose();
                    cubeMapPictureBox.Image = null;
                }
                cubeMapImageLabel.Text = "Could not create preview!";
				//MessageBox.Show("Could not create preview!", "No Preview", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}

		private void cubeMapExportToolStripButton_Click(object sender, EventArgs e) {
			if (cubeMapTreeView.Nodes.Count == 0 || cubeMapTreeView.SelectedNode.Index == -1) {
				return;
			}
			CNode node = ((CNode)cubeMapTreeView.SelectedNode.Tag);
			SaveFileDialog dialog = new SaveFileDialog();
			dialog.Filter = "DDS files|*.dds|All files|*.*";
			dialog.Title = "Select the dds save location and file name";
			dialog.FileName = node.attributes["id"].ToString() + ".dds";
			if (dialog.ShowDialog() == DialogResult.OK) {
				try {
					DDS dds = new DDS(node, false);
					dds.Write(File.Open(dialog.FileName, FileMode.Create), -1);
				}
                catch (Exception ex)
                {
                    MessageBox.Show("Could not export cubemap!" + Environment.NewLine + Environment.NewLine +
                        ex.Message, "Export Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
			}
		}

		private void cubeMapImportToolStripButton_Click(object sender, EventArgs e) {
			if (cubeMapTreeView.Nodes.Count == 0 || cubeMapTreeView.SelectedNode.Index == -1) {
				return;
			}
			CNode node = ((CNode)cubeMapTreeView.SelectedNode.Tag);
			OpenFileDialog dialog = new OpenFileDialog();
			dialog.Filter = "DDS files|*.dds|All files|*.*";
			dialog.Title = "Select a cubemap dds file";
			dialog.FileName = node.attributes["id"].ToString() + ".dds";
			dialog.Multiselect = true;
			if (dialog.ShowDialog() == DialogResult.OK) {
				try {
					DDS dds = new DDS(File.Open(dialog.FileName, FileMode.Open));
					dds.Write(node);
				}
                catch (Exception ex)
                {
                    MessageBox.Show("Could not import cubemap!" + Environment.NewLine + Environment.NewLine +
                        ex.Message, "Import Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
				cubeMapPictureBox.Tag = 0;
				CubeMapCreatePreview(node, 0);
			}
		}
		#endregion

		private void mainTabControl_SelectedIndexChanged(object sender, EventArgs e) {
			if (mainTabControl.SelectedTab.Name == "texturesTabPage") {
				textureTreeView.Focus();
			} else if (mainTabControl.SelectedTab.Name == "cubeMapTabPage") {
				cubeMapTreeView.Focus();
			} else {
				treeView.Focus();
			}
		}

		private void mainTabControl_Selecting(object sender, TabControlCancelEventArgs e) {
			// If No Textures Don't Select
            if (e.TabPage.Name == "texturesTabPage")
            {
                if (textureTreeView.Nodes.Count == 0)
                {
                    texturesTextBox.Text = "";
                    e.Cancel = true;
                }
                else
                {
                    TreeNode selected = textureTreeView.SelectedNode;
                    if (selected == null)
                    {
                        textureTreeView.SelectedNode = textureTreeView.Nodes[0];
                    }
                    else
                    {
                        textureTreeView.SelectedNode = null;
                        textureTreeView.SelectedNode = selected;
                    }
                }
            }
            else if (e.TabPage.Name == "cubeMapTabPage")
            {
                if (cubeMapTreeView.Nodes.Count == 0)
                {
                    e.Cancel = true;
                }
                else
                {
                    TreeNode selected = cubeMapTreeView.SelectedNode;
                    if (selected == null)
                    {
                        cubeMapTreeView.SelectedNode = cubeMapTreeView.Nodes[0];
                    }
                    else
                    {
                        cubeMapTreeView.SelectedNode = null;
                        cubeMapTreeView.SelectedNode = selected;
                    }
                }
            }
            else
            {
                if (treeView.Nodes.Count == 0)
                {
                    e.Cancel = true;
                }
                else
                {
                    TreeNode selected = treeView.SelectedNode;
                    if (selected == null && treeView.Nodes.Count > 0)
                    {
                        treeView.SelectedNode = treeView.Nodes[0];
                    }
                    else
                    {
                        treeView.SelectedNode = null;
                        treeView.SelectedNode = selected;
                    }
                }
			}
		}
    }

	public static class ControlHelper {
		#region Redraw Suspend/Resume
		[System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "SendMessageA", ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Ansi, SetLastError = true)]
		private static extern int SendMessage(IntPtr hwnd, int wMsg, int wParam, int lParam);
		private const int WM_SETREDRAW = 0xB;

		public static void SuspendDrawing(this Control target) {
			SendMessage(target.Handle, WM_SETREDRAW, 0, 0);
		}

		public static void ResumeDrawing(this Control target) { ResumeDrawing(target, true); }
		public static void ResumeDrawing(this Control target, bool redraw) {
			SendMessage(target.Handle, WM_SETREDRAW, 1, 0);

			if (redraw) {
				target.Refresh();
			}
		}
		#endregion
	}
}

/* Old XML Maker
			XmlDocument PXML = new XmlDocument();
			PXML.AppendChild(PXML.CreateElement(node.Name));
			PXML.LastChild.Attributes.Append(PXML.CreateAttribute("id"));
			PXML.LastChild.Attributes["id"].InnerText = node.id.ToString();
			PXML.LastChild.Attributes.Append(PXML.CreateAttribute("size"));
			PXML.LastChild.Attributes["size"].InnerText = node.Size.ToString();
			foreach (KeyValuePair<string, CAttribute> attr in ((CNode)treeView.SelectedNode.Tag).attributes) {
				PXML.LastChild.AppendChild(PXML.CreateElement(attr.Key));
				PXML.LastChild.LastChild.Attributes.Append(PXML.CreateAttribute("type"));
				PXML.LastChild.LastChild.Attributes["type"].InnerText = attr.Value.Value.GetType().ToString();
				PXML.LastChild.LastChild.InnerText = attr.Value.ToString();
				if (attr.Key == "id") {
					idTextBox.Text = attr.Value.ToString();
				}
			}
			StringWriter stringWriter = new StringWriter();
			XmlTextWriter xmlWriter = new XmlTextWriter(stringWriter);
			xmlWriter.Formatting = Formatting.Indented;
			PXML.WriteTo(xmlWriter);
			xmlWriter.Flush();
			xmlWriter.Close();
			richTextBox1.Text = stringWriter.ToString();
*/
/*
        Dictionary<string, int> headers2 = new Dictionary<string, int>();
        Dictionary<int, Head> headers3 = new Dictionary<int, Head>();
        private void printSecsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string fN2 = Application.StartupPath + "\\file.pssg";
            MessageBox.Show(Convert.ToString(ReadPSSG2(fN2)));

            // HEADERS Change
            Dictionary<int, Head> headersNew = new Dictionary<int, Head>();
            Dictionary<int, int> connector = new Dictionary<int, int>();
            Dictionary<int, int> connector2 = new Dictionary<int, int>();
            int target = 0;
            int target2 = 0;
            foreach (KeyValuePair<int, Head> header in headers)
            {
                if (headers2.ContainsKey(header.Value.Name) == true)
                {
                    // SUBHEADER
                    Dictionary<int, Subhead> subheadersNew = new Dictionary<int, Subhead>();
                    foreach (KeyValuePair<int, Subhead> subheader in header.Value.Subheaders)
                    {
                        foreach (KeyValuePair<int, Subhead> subheader2 in headers3[headers2[header.Value.Name]].Subheaders)
                        {
                            if (subheader2.Value.Name == subheader.Value.Name)
                            {
                                connector2.Add(subheader.Key, subheader2.Key);
                                //subheadersNew.Add(subheader2.Key, subheader.Value);
                                target2 = subheader2.Key;
                                break;
                            }
                        }
                    }
                    foreach (KeyValuePair<int, int> connect in connector2)
                    {
                        if (header.Value.Subheaders.ContainsKey(connect.Key) == true)
                        {
                            Subhead s = header.Value.Subheaders[connect.Key];
                            header.Value.Subheaders.Remove(connect.Key);
                            header.Value.Subheaders.Add(connect.Value, s);
                        }
                    }
                    //header.Value.Subheaders = subheadersNew;
                    connector.Add(header.Key, headers2[header.Value.Name]);
                    //headersNew.Add(headers2[header.Value.Name], header.Value);
                    target = headers2[header.Value.Name];
                }
            }
            foreach (KeyValuePair<int, int> connect in connector)
            {
                Head h = headers[connect.Key];
                headers.Remove(connect.Key);
                headers.Remove(connect.Value);
                headers.Add(connect.Value, h);
            }
            //headers = headersNew;
            //headers = headers3;
            List<int> keys = new List<int>(headers.Keys);
            List<int> keys2 = new List<int>(headers[keys[keys.Count - 1]].Subheaders.Keys);
            // FIX HEADER
            byte[] tmp = intToBytes(keys[keys.Count - 1]);
            byte[] tmp2 = intToBytes(keys2[keys2.Count - 1]);
            pssgHead[8] = tmp2[0];
            pssgHead[9] = tmp2[1];
            pssgHead[10] = tmp2[2];
            pssgHead[11] = tmp2[3];
            pssgHead[12] = tmp[0];
            pssgHead[13] = tmp[1];
            pssgHead[14] = tmp[2];
            pssgHead[15] = tmp[3];

            // CONVERT ITEMS
            foreach (Section section in sections)
            {
                if (connector.ContainsKey(section.ID) == true)
                {
                    section.ID = connector[section.ID];
                }
                foreach (KeyValuePair<string, Subsection> subsection in section.Subsections)
                {
                    if (connector2.ContainsKey(bytesToInt(subsection.Value.ID)) == true)
                    {
                        subsection.Value.ID = intToBytes(connector2[bytesToInt(subsection.Value.ID)]);
                    }
                }
            }
        
        }

        public bool ReadPSSG2(string fN)
        {
            try
            {
                using (BinaryReader b = new BinaryReader(File.Open(fN, FileMode.Open, FileAccess.Read)))
                {
                    int target = -1;
                    bool stop = false;
                    Head header;
                    Subhead subheader;

                    // Read Head
                    pssgHead = b.ReadBytes(16);
                    target = bytesToInt(pssgHead, 12);
                    while (stop == false)
                    {
                        int id = -1;
                        header = new Head();
                        id = bytesToInt(b.ReadBytes(4));
                        header.Size = bytesToInt(b.ReadBytes(4));
                        header.Name = Encoding.UTF8.GetString(b.ReadBytes(header.Size));
                        header.Param = bytesToInt(b.ReadBytes(4));
                        headers2.Add(header.Name, id);
                        headers3.Add(id, header);
                        for (int i = 0; i < header.Param; i++)
                        {
                            int id2 = -1;
                            subheader = new Subhead();
                            id2 = bytesToInt(b.ReadBytes(4));
                            subheader.Size = bytesToInt(b.ReadBytes(4));
                            subheader.Name = Encoding.UTF8.GetString(b.ReadBytes(subheader.Size));
                            subheaders.Add(id2, subheader);
                        }
                        // Stop if last head was reached
                        if (id == target)
                        {
                            stop = true;
                        }
                    }
                    //MessageBox.Show(b.BaseStream.Position.ToString());
                }
                return true;
            }
            catch (Exception excp)
            {
                MessageBox.Show(excp.Message);
                return false;
            }
        }

        private void tempToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Dictionary<string, Subsection> sd = new Dictionary<string, Subsection>();
            Subsection sub = new Subsection();
            sub.ID = intToBytes(1);
            sd.Add("A", sub);
            Subsection sub2 = new Subsection();
            sub2.ID = intToBytes(2);
            sd.Add("B", sub2);
            Subsection sub3 = new Subsection();
            sub3.ID = intToBytes(3);
            sd.Add("C", sub3);
            Dictionary<string, Subsection> sd2 = new Dictionary<string, Subsection>(sd);
            Subsection sub4 = new Subsection();
            sub4.ID = intToBytes(4);
            sd.Add("D", sub4);
            MessageBox.Show(sd.Count.ToString() + " | " + sd2.Count.ToString());
            MessageBox.Show(sd.Count.ToString() + " | " + sd2.Count.ToString());
        }
*/