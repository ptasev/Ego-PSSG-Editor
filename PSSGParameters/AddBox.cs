using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PSSGParameters {
	public partial class AddBox : Form {
		CPSSGFile pssgFile;
		public int AddType {
			get { return tabControl.SelectedIndex; }
		}
		public int NodeID {
			get { return nodeInfoComboBox1.SelectedIndex + 1; }
		}
		public int AttributeID {
			get { return attributeInfoComboBox.SelectedIndex + 1; }
		}
		public Type ValueType {
			get { return Type.GetType((string)valueTypeComboBox.SelectedItem); }
		}
		public string Value {
			get { return valueTextBox.Text; }
		}

		public AddBox(CPSSGFile file) {
			InitializeComponent();
			pssgFile = file;
			// NodeInfo Combo
			nodeInfoComboBox1.BeginUpdate();
			foreach (CNodeInfo nodeInfo in pssgFile.nodeInfo) {
				nodeInfoComboBox1.Items.Add(nodeInfo.name);
			}
			nodeInfoComboBox1.EndUpdate();
			// AttributeInfo Combo
			attributeInfoComboBox.BeginUpdate();
			foreach (CAttributeInfo attributeInfo in pssgFile.attributeInfo) {
				attributeInfoComboBox.Items.Add(attributeInfo.name);
			}
			attributeInfoComboBox.EndUpdate();
			// ValueType Combo
			valueTypeComboBox.Items.Add(typeof(System.UInt16).ToString());
			valueTypeComboBox.Items.Add(typeof(System.UInt32).ToString());
			valueTypeComboBox.Items.Add(typeof(System.Int16).ToString());
			valueTypeComboBox.Items.Add(typeof(System.Int32).ToString());
			valueTypeComboBox.Items.Add(typeof(System.Single).ToString());
            //valueTypeComboBox.Items.Add(typeof(System.Boolean).ToString());
			valueTypeComboBox.Items.Add(typeof(System.String).ToString());
			// Select
			nodeInfoComboBox1.SelectedIndex = 0;
            if (attributeInfoComboBox.Items.Count > 0)
            {
                attributeInfoComboBox.SelectedIndex = 0;
            }
			valueTypeComboBox.SelectedIndex = 5;
		}

		private void okButton_Click(object sender, EventArgs e) {
			if (AddType == 0) {
				if (nodeInfoComboBox1.SelectedIndex == -1) {
					this.DialogResult = DialogResult.None;
				}
			} else {
				if (attributeInfoComboBox.SelectedIndex == -1 || valueTypeComboBox.SelectedIndex == -1 || valueTextBox.Text == "") {
					this.DialogResult = DialogResult.None;
				}

				try {
					Convert.ChangeType(Value, ValueType);
				}
				catch {
					this.DialogResult = DialogResult.None;
				}
			}
		}

        private void tabControl_Selecting(object sender, TabControlCancelEventArgs e)
        {
            if (e.TabPage.Name == "attributeTabPage" && attributeInfoComboBox.Items.Count == 0)
            {
                e.Cancel = true;
            }
        }
	}
}
