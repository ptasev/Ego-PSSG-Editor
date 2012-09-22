using System;
using System.Collections.Generic;
using System.Text;
using MiscUtil.IO;
using MiscUtil.Conversion;
using System.Windows.Forms;
using System.Linq;

namespace PSSGParameters {
	public class CPSSGFile {
		public string magic;
		public CNodeInfo[] nodeInfo;
		public CAttributeInfo[] attributeInfo;
		public CNode rootNode;

		public CPSSGFile(System.IO.Stream fileStream) {
			using (EndianBinaryReaderEx reader = new EndianBinaryReaderEx(new BigEndianBitConverter(), fileStream)) {
				magic = reader.ReadPSSGString(4);
				if (magic != "PSSG") {
					throw new Exception("This is not a PSSG file!");
				}
				int size = reader.ReadInt32();
				int attributeInfoCount = reader.ReadInt32();
				int nodeInfoCount = reader.ReadInt32();

				attributeInfo = new CAttributeInfo[attributeInfoCount];
				nodeInfo = new CNodeInfo[nodeInfoCount];

				for (int i = 0; i < nodeInfoCount; i++) {
					nodeInfo[i] = new CNodeInfo(reader, this);
				}
				long positionAfterInfo = reader.BaseStream.Position;

				rootNode = new CNode(reader, this, null, true);
				if (reader.BaseStream.Position < reader.BaseStream.Length) {
					reader.BaseStream.Position = positionAfterInfo;
					rootNode = new CNode(reader, this, null, false);
					if (reader.BaseStream.Position < reader.BaseStream.Length) {
						throw new Exception("This file is improperly saved and not supported by this version of the PSSG editor." + Environment.NewLine + Environment.NewLine + 
							"Get an older version of the program if you wish to take out its contents, but, put it back together using this program and a non-modded version of the pssg file.");
					}
				}
			}
		}
        public CPSSGFile()
        {
            magic = "PSSG";
            nodeInfo = new CNodeInfo[0];
            attributeInfo = new CAttributeInfo[0];
        }

		public void Write(System.IO.Stream fileStream) {
			using (EndianBinaryWriterEx writer = new EndianBinaryWriterEx(new BigEndianBitConverter(), fileStream)) {
				writer.Write(Encoding.ASCII.GetBytes(magic));
				writer.Write(0);
				writer.Write(attributeInfo.Length);
				writer.Write(nodeInfo.Length);

				for (int i = 0; i < nodeInfo.Length; i++) {
					nodeInfo[i].Write(writer);
				}

				if (rootNode != null) {
					rootNode.UpdateSize();
					rootNode.Write(writer);
				}
				writer.BaseStream.Position = 4;
				writer.Write((int)writer.BaseStream.Length - 8);
			}
		}

		public TreeNode CreateTreeViewNode(CNode node) {
			TreeNode treeNode = new TreeNode();
			treeNode.Text = node.Name;
			treeNode.Tag = node;
			if (node.subNodes != null) {
				foreach (CNode subNode in node.subNodes) {
					treeNode.Nodes.Add(CreateTreeViewNode(subNode));
				}
			}
			node.TreeNode = treeNode;
			return treeNode;
		}
		public void CreateSpecificTreeViewNode(TreeView tv, string nodeName) {
			List<CNode> textureNodes = FindNodes(nodeName);
			TreeNode treeNode = new TreeNode();
			foreach (CNode texture in textureNodes) {
				if (texture.attributes.ContainsKey("id") == false) {
					continue;
				}
				treeNode.Text = texture.attributes["id"].ToString();
				treeNode.Tag = texture;
				tv.Nodes.Add(treeNode);
				treeNode = new TreeNode();
			}
		}
		public void CreateSpecificTreeViewNode(TreeView tv, string nodeName, string attributeName, string attributeValue) {
			List<CNode> textureNodes = FindNodes(nodeName, attributeName, attributeValue);
			TreeNode treeNode = new TreeNode();
			foreach (CNode texture in textureNodes) {
				treeNode.Text = texture.attributes["id"].ToString();
				treeNode.Tag = texture;
				tv.Nodes.Add(treeNode);
				treeNode = new TreeNode();
			}
		}

		public CNodeInfo[] GetNodeInfo(string nodeInfoName) {
			CNodeInfo[] query = nodeInfo.Where(x => x.name == nodeInfoName).ToArray();
			return query;
		}
		public CAttributeInfo[] GetAttributeInfo(string attributeInfoName) {
			CAttributeInfo[] query = attributeInfo.Where(x => x.name == attributeInfoName).ToArray();
			return query;
		}

		public List<CNode> FindNodes(string name, string attributeName = null, string attributeValue = null) {
            if (rootNode == null)
            {
                return new List<CNode>();
            }
			return rootNode.FindNodes(name, attributeName, attributeValue);
		}

		public CNode AddNode(CNode parentNode, int nodeID) {
			if (rootNode == null) {
				CNode newRootNode = new CNode(nodeID, this, null, nodeInfo[nodeID - 1].isDataNode);
				rootNode = newRootNode;
				return newRootNode;
			}
			if (parentNode.isDataNode == true) {
				MessageBox.Show("Adding sub nodes to a data node is not allowed!", "Add Node", MessageBoxButtons.OK, MessageBoxIcon.Stop);
				return null;
			}
			if (parentNode.subNodes != null) {
				Array.Resize(ref parentNode.subNodes, parentNode.subNodes.Length + 1);
			} else {
				parentNode.subNodes = new CNode[1];
			}
			CNode newNode = new CNode(nodeID, this, parentNode, nodeInfo[nodeID - 1].isDataNode);
			parentNode.subNodes[parentNode.subNodes.Length - 1] = newNode;
			return newNode;
		}
		public CAttribute AddAttribute(CNode parentNode, int attributeID, object data) {
			if (parentNode == null) {
				return null;
			}
			if (parentNode.attributes == null) {
				parentNode.attributes = new Dictionary<string, CAttribute>();
            }
            else if (parentNode.HasAttribute(attributeID))
            {
                parentNode[attributeID].data = data;
                return parentNode[attributeID];
            }
            else if (parentNode.attributes.ContainsKey(attributeInfo[attributeID - 1].name))
            {
                return null;
            }
			CAttribute newAttr = new CAttribute(attributeID, data, this, parentNode);
			parentNode.attributes.Add(newAttr.Name, newAttr);
			return newAttr;
		}
		public void RemoveNode(CNode node) {
			if (node.ParentNode == null) {
				rootNode = null;
			} else {
				List<CNode> subNodes = new List<CNode>(node.ParentNode.subNodes);
				subNodes.Remove(node);
				node.ParentNode.subNodes = subNodes.ToArray();
				node = null;
			}
		}
		public void RemoveAttribute(CNode node, string attributeName) {
			node.attributes.Remove(attributeName);
		}

		public CNodeInfo AddNodeInfo(string name) {
            if (GetNodeInfo(name).Length > 0) {
                return null;
            }
            if (nodeInfo == null)
            {
                nodeInfo = new CNodeInfo[1];
            }
            else
            {
                Array.Resize(ref nodeInfo, nodeInfo.Length + 1);
            }
			CNodeInfo nInfo = new CNodeInfo(nodeInfo.Length, name);
			nodeInfo[nInfo.id - 1] = nInfo;
			return nInfo;
		}
		public CAttributeInfo AddAttributeInfo(string name, CNodeInfo nodeInfo) {
			// For each attributeInfo in nodeInfo, the ids have to be consecutive
            if (GetAttributeInfo(name).Length > 0)
            {
                return null;
            }
            if (attributeInfo == null)
            {
                attributeInfo = new CAttributeInfo[1];
            }
            else
            {
                Array.Resize(ref attributeInfo, attributeInfo.Length + 1);
            }
            int newID = 0;
			List<int> currentKeys = new List<int>(nodeInfo.attributeInfo.Keys);
			if (currentKeys.Count > 0) {
				foreach (int k in currentKeys) {
					newID = Math.Max(newID, k);
				}
				newID++;
			} else {
				newID = attributeInfo.Length;
			}
			CAttributeInfo attrInfo = new CAttributeInfo(newID, name);
			if (newID == attributeInfo.Length) {
				attributeInfo[attrInfo.id - 1] = attrInfo;
				this.nodeInfo[nodeInfo.id - 1].attributeInfo.Add(attrInfo.id, attrInfo);
			} else {
				for (int i = attributeInfo.Length - 2; i >= newID - 1; i--) {
					attributeInfo[i + 1] = attributeInfo[i];
					attributeInfo[i + 1].id = i + 2;
				}
				attributeInfo[attrInfo.id - 1] = attrInfo;
				this.nodeInfo[nodeInfo.id - 1].attributeInfo.Add(attrInfo.id, attrInfo);
				// Fix the NodeInfos
				foreach (CNodeInfo nInfo in this.nodeInfo) {
					List<int> keys = new List<int>(nInfo.attributeInfo.Keys);
					//keys.Sort();
					if (nInfo != nodeInfo) {
						for (int i = keys.Count - 1; i >= 0; i--) {
							if (keys[i] >= newID) {
								CAttributeInfo aInfo = attributeInfo[keys[i]];
								nInfo.attributeInfo.Remove(keys[i]);
								nInfo.attributeInfo.Add(keys[i] + 1, aInfo);
							}
						}
					}
				}
				// Edit CNode to fix CAttr.id
				rootNode.AddAttributeInfo(newID);
			}
			return attrInfo;
		}
		public void RemoveNodeInfo(int id) {
			// Remove all attributeInfos from nodeInfo
			List<int> attrKeys = new List<int>(nodeInfo[id - 1].attributeInfo.Keys);
			while (nodeInfo[id - 1].attributeInfo.Count > 0) {
				RemoveAttributeInfo(attrKeys[0]);
			}
			attrKeys = null;
			// Shift all succeeding nodeInfos and change their id
			for (int i = id - 1; i < nodeInfo.Length - 1; i++) {
				nodeInfo[i] = nodeInfo[i + 1];
				nodeInfo[i].id = i + 1;
			}
			Array.Resize(ref nodeInfo, nodeInfo.Length - 1);
			// Delete from CNode
            if (rootNode != null)
            {
                if (rootNode.id == id)
                {
                    rootNode = null;
                }
                else
                {
                    rootNode.RemoveNodeInfo(id);
                }
            }
		}
		public void RemoveAttributeInfo(int id) {
			// Shift all succeeding attributeInfos and change their id
			// 
			for (int i = id - 1; i < attributeInfo.Length - 1; i++) {
				attributeInfo[i] = attributeInfo[i + 1];
				attributeInfo[i].id--;
				//attributeInfo[i].id = i + 1;
			}
			Array.Resize(ref attributeInfo, attributeInfo.Length - 1);
			// Fix the NodeInfos
			foreach (CNodeInfo nInfo in nodeInfo) {
				List<int> keys = new List<int>(nInfo.attributeInfo.Keys);
				//keys.Sort();
				if (nInfo.attributeInfo.ContainsKey(id) == true) {
					nInfo.attributeInfo.Remove(id);
					for (int i = id + 1; nInfo.attributeInfo.ContainsKey(i); i++) {
						CAttributeInfo aInfo = attributeInfo[i - 2];
						nInfo.attributeInfo.Remove(i);
						nInfo.attributeInfo.Add(i - 1, aInfo);
					}
				} else {
					for (int i = 0; i < keys.Count; i++) {
						if (keys[i] > id) {
							CAttributeInfo aInfo = attributeInfo[keys[i] - 2];
							nInfo.attributeInfo.Remove(keys[i]);
							nInfo.attributeInfo.Add(keys[i] - 1, aInfo);
						}
					}
				}
			}
			// Edit CNode to fix CAttr.id
            if (rootNode != null)
            {
                rootNode.RemoveAttributeInfo(id);
            }
		}
	}

	public class CNodeInfo {
		public int id;
		public string name;
		public SortedDictionary<int, CAttributeInfo> attributeInfo;
		public bool isDataNode = false;

		public CNodeInfo(EndianBinaryReaderEx reader, CPSSGFile file) {
			attributeInfo = new SortedDictionary<int, CAttributeInfo>();

			id = reader.ReadInt32();
			name = reader.ReadPSSGString();
			int attributeInfoCount = reader.ReadInt32();
			CAttributeInfo ai;
			for (int i = 0; i < attributeInfoCount; i++) {
				ai = new CAttributeInfo(reader);
				attributeInfo.Add(ai.id, ai);

				file.attributeInfo[ai.id - 1] = ai;
			}
		}
		public CNodeInfo(int id, string name) {
			this.id = id;
			this.name = name;
			attributeInfo = new SortedDictionary<int, CAttributeInfo>();
		}

		public void Write(EndianBinaryWriterEx writer) {
			writer.Write(id);
			writer.WritePSSGString(name);
			writer.Write(attributeInfo.Count);
			foreach (KeyValuePair<int, CAttributeInfo> info in attributeInfo) {
				writer.Write(info.Key);
				writer.WritePSSGString(info.Value.name);
			}
		}
	}

	public class CAttributeInfo {
		public int id;
		public string name;

		public CAttributeInfo(EndianBinaryReaderEx reader) {
			id = reader.ReadInt32();
			name = reader.ReadPSSGString();
		}
		public CAttributeInfo(int id, string name) {
			this.id = id;
			this.name = name;
		}
	}

	public class CNode {
		public int id;
		private int size;
		private int attributeSize;
		public int Size {
			get {
				return size;
			}
		}
		public int AttributeSize {
			get {
				return attributeSize;
			}
		}
		public Dictionary<string, CAttribute> attributes;
		public CNode[] subNodes;
		public bool isDataNode = false;
		public byte[] data;
		public string Name {
			get {
				return file.nodeInfo[id - 1].name;
			}
		}

		private CPSSGFile file;
		public CNode ParentNode;
		public TreeNode TreeNode;

		public CNode(EndianBinaryReaderEx reader, CPSSGFile file, CNode node, bool useDataNodeCheck) {
			this.file = file;
			ParentNode = node;

			id = reader.ReadInt32();
			size = reader.ReadInt32();
			long end = reader.BaseStream.Position + size;

			attributeSize = reader.ReadInt32();
			long attributeEnd = reader.BaseStream.Position + attributeSize;
			if (attributeEnd > reader.BaseStream.Length || end > reader.BaseStream.Length) {
				throw new Exception("This file is improperly saved and not supported by this version of the PSSG editor." + Environment.NewLine + Environment.NewLine +
							"Get an older version of the program if you wish to take out its contents, but, put it back together using this program and a non-modded version of the pssg file.");
			}
			// Each attr is at least 8 bytes (id + size), so take a conservative guess
			attributes = new Dictionary<string, CAttribute>();
			CAttribute attr;
			while (reader.BaseStream.Position < attributeEnd) {
				attr = new CAttribute(reader, file, this);
				attributes.Add(attr.Name, attr);
			}

			switch (Name) {
				case "BOUNDINGBOX":
				case "DATA":
				case "DATABLOCKDATA":
				case "DATABLOCKBUFFERED":
				case "INDEXSOURCEDATA":
				case "INVERSEBINDMATRIX":
				case "MODIFIERNETWORKINSTANCEUNIQUEMODIFIERINPUT":
				case "NeAnimPacketData_B1":
				case "NeAnimPacketData_B4":
				case "RENDERINTERFACEBOUNDBUFFERED":
				case "SHADERINPUT":
				case "TEXTUREIMAGEBLOCKDATA":
				case "TRANSFORM":
					isDataNode = true;
					break;
			}
			if (isDataNode == false && useDataNodeCheck == true) {
				long currentPos = reader.BaseStream.Position;
				// Check if it has subnodes
				while (reader.BaseStream.Position < end) {
					int tempID = reader.ReadInt32();
					if (tempID > file.nodeInfo.Length || tempID < 0) {
						isDataNode = true;
						break;
					} else {
						int tempSize = reader.ReadInt32();
						if ((reader.BaseStream.Position + tempSize > end) || (tempSize == 0 && tempID == 0) || tempSize < 0) {
							isDataNode = true;
							break;
						} else if (reader.BaseStream.Position + tempSize == end) {
							break;
						} else {
							reader.BaseStream.Position += tempSize;
						}
					}
				}
				reader.BaseStream.Position = currentPos;
			}

			if (isDataNode) {
				data = reader.ReadBytes((int)(end - reader.BaseStream.Position));
			} else {
				// Each node at least 12 bytes (id + size + arg size)
				subNodes = new CNode[(end - reader.BaseStream.Position) / 12];
				int nodeCount = 0;
				while (reader.BaseStream.Position < end) {
					subNodes[nodeCount] = new CNode(reader, file, this, useDataNodeCheck);
					nodeCount++;
				}
				Array.Resize(ref subNodes, nodeCount);
			}
			
			file.nodeInfo[id - 1].isDataNode = isDataNode;
		}
		public CNode(CNode nodeToCopy) {
			this.file = nodeToCopy.file;
			ParentNode = nodeToCopy.ParentNode;

			id = nodeToCopy.id;
			size = nodeToCopy.size;
			attributeSize = nodeToCopy.attributeSize;
			attributes = new Dictionary<string, CAttribute>();
			CAttribute attr;
			foreach (KeyValuePair<string, CAttribute> attrToCopy in nodeToCopy.attributes) {
				attr = new CAttribute(attrToCopy.Value);
				attributes.Add(attr.Name, attr);
			}

			isDataNode = nodeToCopy.isDataNode;

			if (isDataNode) {
				data = nodeToCopy.data;
			} else {
				// Each node at least 12 bytes (id + size + arg size)
				subNodes = new CNode[nodeToCopy.subNodes.Length];
				int nodeCount = 0;
				foreach (CNode subNodeToCopy in nodeToCopy.subNodes) {
					subNodes[nodeCount] = new CNode(subNodeToCopy);
					nodeCount++;
				}
				Array.Resize(ref subNodes, nodeCount);
			}
		}
		public CNode(int id, CPSSGFile file, CNode node, bool isDataNode) {
			this.id = id;
			this.file = file;
			this.ParentNode = node;
			this.isDataNode = isDataNode;
			attributes = new Dictionary<string, CAttribute>();
			if (isDataNode == true) {
				data = new byte[0];
			}
		}

		public void Write(EndianBinaryWriterEx writer) {
			writer.Write(id);
			writer.Write(size);
			writer.Write(attributeSize);
			if (attributes != null) {
				foreach (KeyValuePair<string, CAttribute> attr in attributes) {
					attr.Value.Write(writer);
				}
			}
			if (subNodes != null) {
				foreach (CNode node in subNodes) {
					node.Write(writer);
				}
			}
			if (isDataNode) {
				writer.Write(data);
			}
		}
		public void UpdateSize() {
			attributeSize = 0;
			if (attributes != null) {
				foreach (KeyValuePair<string, CAttribute> attr in attributes) {
					attr.Value.UpdateSize();
					attributeSize += 8 + attr.Value.Size;
				}
			}
			size = 4 + attributeSize;
			if (subNodes != null) {
				foreach (CNode node in subNodes) {
					node.UpdateSize();
					size += 8 + node.Size;
				}
			}
			if (isDataNode) {
				size += data.Length;
			}
		}

		public List<CNode> FindNodes(string nodeName, string attributeName = null, string attributeValue = null) {
			List<CNode> ret = new List<CNode>();
			if (this.Name == nodeName) {
				if (attributeName != null && attributeValue != null) {
					CAttribute attr;
					if (attributes.TryGetValue(attributeName, out attr) && attr.ToString() == attributeValue) {
						ret.Add(this);
					}
				} else if (attributeName != null) {
					if (attributes.ContainsKey(attributeName) == true) {
						ret.Add(this);
					}
				} else {
					ret.Add(this);
				}
			}
			if (subNodes != null) {
				foreach (CNode subNode in subNodes) {
					ret.AddRange(subNode.FindNodes(nodeName, attributeName, attributeValue));
				}
			}
			return ret;
		}

		public void AddAttributeInfo(int id) {
			foreach (KeyValuePair<string, CAttribute> pair in attributes) {
				if (pair.Value.id >= id) {
					pair.Value.id++;
				}
			}

			if (subNodes != null) {
				foreach (CNode subNode in subNodes) {
					subNode.AddAttributeInfo(id);
				}
			}
		}
		public void RemoveNodeInfo(int id) {
			if (this.id > id) {
				this.id--;
			}

			if (subNodes != null) {
				List<CNode> newSubNodes = new List<CNode>();
				for (int i = 0; i < subNodes.Length; i++) {
					if (subNodes[i].id != id) {
						subNodes[i].RemoveNodeInfo(id);
						newSubNodes.Add(subNodes[i]);
					}
				}
				subNodes = newSubNodes.ToArray();
			}
		}
		public void RemoveAttributeInfo(int id) {
			string toDelete = "";
			foreach (KeyValuePair<string, CAttribute> pair in attributes) {
				if (pair.Value.id == id) {
					toDelete = pair.Key;
				} else if (pair.Value.id > id) {
					pair.Value.id--;
				}
			}
			if (attributes.ContainsKey(toDelete) == true) {
				attributes.Remove(toDelete);
			}

			if (subNodes != null) {
				foreach (CNode subNode in subNodes) {
					subNode.RemoveAttributeInfo(id);
				}
			}
		}

        /// <summary>
        /// Determines whether the current node has an attribute with the specified id.
        /// </summary>
        /// <param name="id">The id of the attribute to find.</param>
        public bool HasAttribute(int id)
        {
            return attributes.Count(x => x.Value.id == id) > 0;
        }
        /// <summary>
        /// Gets or sets the node attribute associated with the specified attribute id.
        /// </summary>
        /// <param name="id">The id of the attribute to get or set.</param>
        public CAttribute this[int id]
        {
            get
            {
                return attributes.First(x => x.Value.id == id).Value;
            }
            set
            {
                this[id] = value;
            }
        }

		public override string ToString() { return Name; }
	}

	public class CAttribute {
		public int id;
		private int size;
		public int Size {
			get {
				return size;
			}
		}
		public object data;
		public object Value {
			get {
				object ret = "Byte Data - Do Not Edit";
				if (data is string) {
					return (string)data;
				} else if ((data is byte[]) == false) {
					return Convert.ChangeType(data, data.GetType());
				} else if (((byte[])data).Length == 4) {
					ret = EndianBitConverter.Big.ToUInt32((byte[])data, 0);
					if ((uint)ret > 1000000000) {
						ret = EndianBitConverter.Big.ToSingle((byte[])data, 0);
					}
					if (ParentNode.Name == "FETEXTLAYOUT") {
						if (Name == "height" ||
							Name == "depth" ||
							Name == "tracking") {
							ret = EndianBitConverter.Big.ToSingle((byte[])data, 0);
						}
					}
					if (ParentNode.Name == "NEGLYPHMETRICS") {
						if (Name == "advanceWidth" ||
							Name == "horizontalBearing" ||
							Name == "verticalBearing" ||
							Name == "physicalWidth" ||
							Name == "physicalHeight") {
							ret = EndianBitConverter.Big.ToSingle((byte[])data, 0);
						} else if (Name == "codePoint") {
							//ret = EndianBitConverter.Big.ToInt32((byte[])data, 0);
						}
					}
					if (ParentNode.Name == "SHADERGROUP") {
						if (Name == "defaultRenderSortPriority") {
							ret = EndianBitConverter.Big.ToSingle((byte[])data, 0);
						}
					}
					if (ParentNode.Name == "FEATLASINFODATA") {
						if (Name == "u0" ||
							Name == "v0" ||
							Name == "u1" ||
							Name == "v1") {
							ret = EndianBitConverter.Big.ToSingle((byte[])data, 0);
						}
					}
				} else if (((byte[])data).Length == 2) {
					ret = EndianBitConverter.Big.ToUInt16((byte[])data, 0);
				}
				
				return ret;
			}
		}
		public string Name {
			get {
				return file.attributeInfo[id - 1].name;
			}
		}
		public override string ToString() {
			if (Value is string) {
				return (string)Value;
			} else if (Value is UInt16) {
				return ((UInt16)Value).ToString();
			} else if (Value is UInt32) {
				return ((uint)Value).ToString();
			} else if (Value is Int16) {
				return ((Int16)Value).ToString();
			} else if (Value is Int32) {
				return ((int)Value).ToString();
			} else if (Value is float) {
				return ((float)Value).ToString();
            } else if (Value is bool) {
                return ((bool)Value).ToString();
            }
            else
            {
				return "Byte Data - Do Not Edit";
			}
		}

		private CPSSGFile file;
		public CNode ParentNode;

		public CAttribute(int id, object data, CPSSGFile file, CNode ParentNode) {
			this.id = id;
			this.data = data;
			this.file = file;
			this.ParentNode = ParentNode;
		}
		public CAttribute(EndianBinaryReaderEx reader, CPSSGFile file, CNode node) {
			this.file = file;
			ParentNode = node;

			id = reader.ReadInt32();
			size = reader.ReadInt32();
			if (size == 4) {
				data = reader.ReadBytes(size);
				return;
			} else if (size > 4) {
				int strlen = reader.ReadInt32();
				if (size - 4 == strlen) {
					data = reader.ReadPSSGString(strlen);
					return;
				} else {
					reader.Seek(-4, System.IO.SeekOrigin.Current);
				}
			}
			data = reader.ReadBytes(size);
		}
		public CAttribute(CAttribute attrToCopy) {
			this.file = attrToCopy.file;
			ParentNode = attrToCopy.ParentNode;

			id = attrToCopy.id;
			size = attrToCopy.size;
			data = attrToCopy.data;
		}

		public void Write(EndianBinaryWriterEx writer) {
			writer.Write(id);
			writer.Write(size);
			if (data is string) {
				writer.WritePSSGString((string)data);
			} else if (data is UInt16) {
				writer.Write((UInt16)data);
			} else if (data is UInt32) {
				writer.Write((UInt32)data);
			} else if (data is Int16) {
				writer.Write((Int16)data);
			} else if (data is Int32) {
				writer.Write((Int32)data);
			} else if (data is Single) {
				writer.Write((Single)data);
            }
            else if (data is bool)
            {
                writer.Write((bool)data);
            }
            else
            {
				writer.Write((byte[])data);
			}
		}

		public void UpdateSize() {
			if (data is string) {
				size = 4 + Encoding.UTF8.GetBytes((string)data).Length;
			} else if (data is UInt16) {
				size = EndianBitConverter.Big.GetBytes((UInt16)data).Length;
			} else if (data is UInt32) {
				size = EndianBitConverter.Big.GetBytes((UInt32)data).Length;
			} else if (data is Int16) {
				size = EndianBitConverter.Big.GetBytes((Int16)data).Length;
			} else if (data is Int32) {
				size = EndianBitConverter.Big.GetBytes((Int32)data).Length;
			} else if (data is Single) {
				size = EndianBitConverter.Big.GetBytes((Single)data).Length;
            }
            else if (data is bool)
            {
                size = EndianBitConverter.Big.GetBytes((bool)data).Length;
            }
            else
            {
				size = ((byte[])data).Length;
			}
		}
	}

	public class EndianBinaryReaderEx : EndianBinaryReader {
		public EndianBinaryReaderEx(EndianBitConverter bitConvertor, System.IO.Stream stream)
			: base(bitConvertor, stream) {
		}

		public string ReadPSSGString() {
			int length = this.ReadInt32();
			return this.ReadPSSGString(length);
		}
		public string ReadPSSGString(int length) {
			return Encoding.UTF8.GetString(this.ReadBytes(length));
		}
	}

	public class EndianBinaryWriterEx : EndianBinaryWriter {
		public EndianBinaryWriterEx(EndianBitConverter bitConvertor, System.IO.Stream stream)
			: base(bitConvertor, stream) {
		}

		public void WritePSSGString(string str) {
			byte[] bytes = Encoding.UTF8.GetBytes(str);
			this.Write(bytes.Length);
			this.Write(bytes);
		}
	}
}
