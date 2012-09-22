using System;
using System.Collections.Generic;
using System.Text;
using MiscUtil.Conversion;

namespace PSSGParameters {
	public struct DDS_HEADER {
		public uint size;
		public Flags flags;
		public uint height;
		public uint width;
		public uint pitchOrLinearSize;
		public uint depth;
		public uint mipMapCount;
		public uint[] reserved1; //  = new uint[11]
		public DDS_PIXELFORMAT ddspf;
		public Caps caps;
		public Caps2 caps2;
		public uint caps3;
		public uint caps4;
		public uint reserved2;

		public enum Flags {
			DDSD_CAPS = 0x1,
			DDSD_HEIGHT = 0x2,
			DDSD_WIDTH = 0x4,
			DDSD_PITCH = 0x8,
			DDSD_PIXELFORMAT = 0x1000,
			DDSD_MIPMAPCOUNT = 0x20000,
			DDSD_LINEARSIZE = 0x80000,
			DDSD_DEPTH = 0x800000
		}

		public enum Caps {
			DDSCAPS_COMPLEX = 0x8,
			DDSCAPS_MIPMAP = 0x400000,
			DDSCAPS_TEXTURE = 0x1000
		}

		public enum Caps2 {
			DDSCAPS2_CUBEMAP = 0x200,
			DDSCAPS2_CUBEMAP_POSITIVEX = 0x400,
			DDSCAPS2_CUBEMAP_NEGATIVEX = 0x800,
			DDSCAPS2_CUBEMAP_POSITIVEY = 0x1000,
			DDSCAPS2_CUBEMAP_NEGATIVEY = 0x2000,
			DDSCAPS2_CUBEMAP_POSITIVEZ = 0x4000,
			DDSCAPS2_CUBEMAP_NEGATIVEZ = 0x8000,
			DDSCAPS2_VOLUME = 0x200000
		}
	}

	public struct DDS_PIXELFORMAT {
		public uint size;
		public Flags flags;
		public uint fourCC;
		public uint rGBBitCount;
		public uint rBitMask;
		public uint gBitMask;
		public uint bBitMask;
		public uint aBitMask;

		public enum Flags {
			DDPF_ALPHAPIXELS = 0x1,
			DDPF_ALPHA = 0x2,
			DDPF_FOURCC = 0x4,
			DDPF_RGB = 0x40,
			DDPF_YUV = 0x200,
			DDPF_LUMINANCE = 0x20000
		}
	}

	class DDS {
		uint magic;
		DDS_HEADER header;
		byte[] bdata;
		Dictionary<int, byte[]> bdata2;

		public DDS(CNode node, bool cubePreview) {
			magic = 0x20534444;
			header.size = 124;
			header.flags |= DDS_HEADER.Flags.DDSD_CAPS | DDS_HEADER.Flags.DDSD_HEIGHT | DDS_HEADER.Flags.DDSD_WIDTH | DDS_HEADER.Flags.DDSD_PIXELFORMAT;
			header.height = (uint)(node.attributes["height"].Value);
			header.width = (uint)(node.attributes["width"].Value);
			switch ((string)node.attributes["texelFormat"].Value) {
				case "dxt1":
					header.flags |= DDS_HEADER.Flags.DDSD_LINEARSIZE;
					header.pitchOrLinearSize = (uint)(Math.Max(1, (((uint)node.attributes["width"].Value) + 3) / 4) * 8);
					header.ddspf.flags |= DDS_PIXELFORMAT.Flags.DDPF_FOURCC;
					header.ddspf.fourCC = BitConverter.ToUInt32(Encoding.UTF8.GetBytes(((string)node.attributes["texelFormat"].Value).ToUpper()), 0);
					break;
				case "dxt2":
				case "dxt3":
				case "dxt4":
				case "dxt5":
					header.flags |= DDS_HEADER.Flags.DDSD_LINEARSIZE;
					header.pitchOrLinearSize = (uint)(Math.Max(1, (((uint)node.attributes["width"].Value) + 3) / 4) * 16);
					header.ddspf.flags |= DDS_PIXELFORMAT.Flags.DDPF_FOURCC;
					header.ddspf.fourCC = BitConverter.ToUInt32(Encoding.UTF8.GetBytes(((string)node.attributes["texelFormat"].Value).ToUpper()), 0);
					break;
				case "ui8x4":
					header.flags |= DDS_HEADER.Flags.DDSD_LINEARSIZE;
					header.pitchOrLinearSize = (uint)(Math.Max(1, (((uint)node.attributes["width"].Value) + 3) / 4) * 16); // is this right?
					header.ddspf.flags |= DDS_PIXELFORMAT.Flags.DDPF_ALPHAPIXELS | DDS_PIXELFORMAT.Flags.DDPF_RGB;
					header.ddspf.fourCC = 0;
					header.ddspf.rGBBitCount = 32;
					header.ddspf.rBitMask = 0xFF0000;
					header.ddspf.gBitMask = 0xFF00;
					header.ddspf.bBitMask = 0xFF;
					header.ddspf.aBitMask = 0xFF000000;
					break;
				case "u8":
					header.flags |= DDS_HEADER.Flags.DDSD_LINEARSIZE;
					header.pitchOrLinearSize = (uint)(Math.Max(1, (((uint)node.attributes["width"].Value) + 3) / 4) * 16); // is this right?
					// Interchanging the commented values will both work, not sure which is better
					header.ddspf.flags |= DDS_PIXELFORMAT.Flags.DDPF_LUMINANCE;
					//header.ddspf.flags |= DDS_PIXELFORMAT.Flags.DDPF_ALPHA;
					header.ddspf.fourCC = 0;
					header.ddspf.rGBBitCount = 8;
					header.ddspf.rBitMask = 0xFF;
					//header.ddspf.aBitMask = 0xFF;
					break;
			}
			if (node.attributes.ContainsKey("automipmap") == true && node.attributes.ContainsKey("numberMipMapLevels") == true) {
				if ((uint)node.attributes["automipmap"].Value == 0 && (uint)node.attributes["numberMipMapLevels"].Value > 0) {
					header.flags |= DDS_HEADER.Flags.DDSD_MIPMAPCOUNT;
					header.mipMapCount = (uint)((uint)node.attributes["numberMipMapLevels"].Value + 1);
					header.caps |= DDS_HEADER.Caps.DDSCAPS_MIPMAP | DDS_HEADER.Caps.DDSCAPS_COMPLEX;
				}
			}
			header.reserved1 = new uint[11];
			header.ddspf.size = 32;
			header.caps |= DDS_HEADER.Caps.DDSCAPS_TEXTURE;
			List<CNode> textureImageBlocks = node.FindNodes("TEXTUREIMAGEBLOCK");
			if ((uint)node.attributes["imageBlockCount"].Value > 1) {
				bdata2 = new Dictionary<int, byte[]>();
				for (int i = 0; i < textureImageBlocks.Count; i++) {
					switch (textureImageBlocks[i].attributes["typename"].ToString()) {
						case "Raw":
							header.caps2 |= DDS_HEADER.Caps2.DDSCAPS2_CUBEMAP_POSITIVEX;
							bdata2.Add(0, textureImageBlocks[i].FindNodes("TEXTUREIMAGEBLOCKDATA")[0].data);
							break;
						case "RawNegativeX":
							header.caps2 |= DDS_HEADER.Caps2.DDSCAPS2_CUBEMAP_NEGATIVEX;
							bdata2.Add(1, textureImageBlocks[i].FindNodes("TEXTUREIMAGEBLOCKDATA")[0].data);
							break;
						case "RawPositiveY":
							header.caps2 |= DDS_HEADER.Caps2.DDSCAPS2_CUBEMAP_POSITIVEY;
							bdata2.Add(2, textureImageBlocks[i].FindNodes("TEXTUREIMAGEBLOCKDATA")[0].data);
							break;
						case "RawNegativeY":
							header.caps2 |= DDS_HEADER.Caps2.DDSCAPS2_CUBEMAP_NEGATIVEY;
							bdata2.Add(3, textureImageBlocks[i].FindNodes("TEXTUREIMAGEBLOCKDATA")[0].data);
							break;
						case "RawPositiveZ":
							header.caps2 |= DDS_HEADER.Caps2.DDSCAPS2_CUBEMAP_POSITIVEZ;
							bdata2.Add(4, textureImageBlocks[i].FindNodes("TEXTUREIMAGEBLOCKDATA")[0].data);
							break;
						case "RawNegativeZ":
							header.caps2 |= DDS_HEADER.Caps2.DDSCAPS2_CUBEMAP_NEGATIVEZ;
							bdata2.Add(5, textureImageBlocks[i].FindNodes("TEXTUREIMAGEBLOCKDATA")[0].data);
							break;
					}
				}
				if (cubePreview == true) {
					header.caps2 = 0;
				} else if (bdata2.Count == (uint)node.attributes["imageBlockCount"].Value) {
					header.caps2 |= DDS_HEADER.Caps2.DDSCAPS2_CUBEMAP;
					header.flags = header.flags ^ DDS_HEADER.Flags.DDSD_LINEARSIZE;
					header.pitchOrLinearSize = 0;
					header.caps |= DDS_HEADER.Caps.DDSCAPS_COMPLEX;
				} else {
					throw new Exception("Loading cubemap failed because not all blocks were found. (Read)");
				}
			} else {
				bdata = textureImageBlocks[0].FindNodes("TEXTUREIMAGEBLOCKDATA")[0].data;
			}
		}
		public DDS(System.IO.Stream fileStream) {
			using (System.IO.BinaryReader b = new System.IO.BinaryReader(fileStream)) {
				b.BaseStream.Position = 12;
				header.height = b.ReadUInt32();
				header.width = b.ReadUInt32();
				b.BaseStream.Position += 8;
				header.mipMapCount = b.ReadUInt32();
				b.BaseStream.Position += 52;
				header.ddspf.fourCC = b.ReadUInt32();
				header.ddspf.rGBBitCount = b.ReadUInt32();
				b.BaseStream.Position += 20;
				header.caps2 = (DDS_HEADER.Caps2)b.ReadUInt32();
				b.BaseStream.Position += 12;
				int count = 0;
				if ((uint)header.caps2 != 0) {
					bdata2 = new Dictionary<int, byte[]>();
					if ((header.caps2 & DDS_HEADER.Caps2.DDSCAPS2_CUBEMAP_POSITIVEX) == DDS_HEADER.Caps2.DDSCAPS2_CUBEMAP_POSITIVEX) {
						count++;
						bdata2.Add(0, null);
					} else {
						bdata2.Add(-1, null);
					}
					if ((header.caps2 & DDS_HEADER.Caps2.DDSCAPS2_CUBEMAP_NEGATIVEX) == DDS_HEADER.Caps2.DDSCAPS2_CUBEMAP_NEGATIVEX) {
						count++;
						bdata2.Add(1, null);
					} else {
						bdata2.Add(-2, null);
					}
					if ((header.caps2 & DDS_HEADER.Caps2.DDSCAPS2_CUBEMAP_POSITIVEY) == DDS_HEADER.Caps2.DDSCAPS2_CUBEMAP_POSITIVEY) {
						count++;
						bdata2.Add(2, null);
					} else {
						bdata2.Add(-3, null);
					}
					if ((header.caps2 & DDS_HEADER.Caps2.DDSCAPS2_CUBEMAP_NEGATIVEY) == DDS_HEADER.Caps2.DDSCAPS2_CUBEMAP_NEGATIVEY) {
						count++;
						bdata2.Add(3, null);
					} else {
						bdata2.Add(-4, null);
					}
					if ((header.caps2 & DDS_HEADER.Caps2.DDSCAPS2_CUBEMAP_POSITIVEZ) == DDS_HEADER.Caps2.DDSCAPS2_CUBEMAP_POSITIVEZ) {
						count++;
						bdata2.Add(4, null);
					} else {
						bdata2.Add(-5, null);
					}
					if ((header.caps2 & DDS_HEADER.Caps2.DDSCAPS2_CUBEMAP_NEGATIVEZ) == DDS_HEADER.Caps2.DDSCAPS2_CUBEMAP_NEGATIVEZ) {
						count++;
						bdata2.Add(5, null);
					} else {
						bdata2.Add(-6, null);
					}
					if (count > 0) {
						int length = (int)((b.BaseStream.Length - (long)128) / (long)count);
						//System.Windows.Forms.MessageBox.Show(count.ToString() + "  " + length.ToString());
						for (int i = 0; i < bdata2.Count; i++) {
							if (bdata2.ContainsKey(i) == true) {
								bdata2[i] = b.ReadBytes(length);
							}
						}
					} else {
						throw new Exception("Loading cubemap failed because not all blocks were found. (Read)");
					}
				} else {
					bdata = b.ReadBytes((int)(b.BaseStream.Length - (long)128));
				}
			}
		}

		public void Write(System.IO.Stream fileStream, int cubeIndex) {
			using (System.IO.BinaryWriter b = new System.IO.BinaryWriter(fileStream)) {
				b.Write(magic);
				b.Write(header.size);
				b.Write((uint)header.flags);
				b.Write(header.height);
				b.Write(header.width);
				b.Write(header.pitchOrLinearSize);
				b.Write(header.depth);
				b.Write(header.mipMapCount);
				foreach (uint u in header.reserved1) {
					b.Write(u);
				}
				b.Write(header.ddspf.size);
				b.Write((uint)header.ddspf.flags);
				b.Write(header.ddspf.fourCC);
				b.Write(header.ddspf.rGBBitCount);
				b.Write(header.ddspf.rBitMask);
				b.Write(header.ddspf.gBitMask);
				b.Write(header.ddspf.bBitMask);
				b.Write(header.ddspf.aBitMask);
				b.Write((uint)header.caps);
				b.Write((uint)header.caps2);
				b.Write(header.caps3);
				b.Write(header.caps4);
				b.Write(header.reserved2);
				if (cubeIndex != -1) {
					b.Write(bdata2[cubeIndex]);
				} else if (bdata2 != null && bdata2.Count > 0) {
					for (int i = 0; i < bdata2.Count; i++) {
						if (bdata2.ContainsKey(i) == true) {
							b.Write(bdata2[i]);
						}
					}
				} else {
					b.Write(bdata);
				}
			}
		}
		public void Write(CNode node) {
			node.attributes["height"].data = MiscUtil.Conversion.EndianBitConverter.Big.GetBytes(header.height);
			node.attributes["width"].data = MiscUtil.Conversion.EndianBitConverter.Big.GetBytes(header.width);
			if (node.attributes.ContainsKey("numberMipMapLevels") == true) {
				if ((int)header.mipMapCount - 1 >= 0) {
					node.attributes["numberMipMapLevels"].data = MiscUtil.Conversion.EndianBitConverter.Big.GetBytes(header.mipMapCount - 1);
				} else {
					node.attributes["numberMipMapLevels"].data = MiscUtil.Conversion.EndianBitConverter.Big.GetBytes(0);
				}
			}
			if (header.ddspf.rGBBitCount == 32) {
				node.attributes["texelFormat"].data = "ui8x4";
			} else if (header.ddspf.rGBBitCount == 8) {
				node.attributes["texelFormat"].data = "u8";
			} else {
				node.attributes["texelFormat"].data = Encoding.UTF8.GetString(BitConverter.GetBytes(header.ddspf.fourCC)).ToLower();
			}
			List<CNode> textureImageBlocks = node.FindNodes("TEXTUREIMAGEBLOCK");
			if (bdata2 != null && bdata2.Count > 0) {
				for (int i = 0; i < textureImageBlocks.Count; i++) {
					switch (textureImageBlocks[i].attributes["typename"].ToString()) {
						case "Raw":
							if (bdata2.ContainsKey(0) == true) {
								textureImageBlocks[i].FindNodes("TEXTUREIMAGEBLOCKDATA")[0].data = bdata2[0];
								textureImageBlocks[i].attributes["size"].data = EndianBitConverter.Big.GetBytes(bdata2[0].Length);
							} else {
								throw new Exception("Loading cubemap failed because not all blocks were found. (Write)");
							}
							break;
						case "RawNegativeX":
							if (bdata2.ContainsKey(1) == true) {
								textureImageBlocks[i].FindNodes("TEXTUREIMAGEBLOCKDATA")[0].data = bdata2[1];
								textureImageBlocks[i].attributes["size"].data = EndianBitConverter.Big.GetBytes(bdata2[1].Length);
							} else {
								throw new Exception("Loading cubemap failed because not all blocks were found. (Write)");
							}
							break;
						case "RawPositiveY":
							if (bdata2.ContainsKey(2) == true) {
								textureImageBlocks[i].FindNodes("TEXTUREIMAGEBLOCKDATA")[0].data = bdata2[2];
								textureImageBlocks[i].attributes["size"].data = EndianBitConverter.Big.GetBytes(bdata2[2].Length);
							} else {
								throw new Exception("Loading cubemap failed because not all blocks were found. (Write)");
							}
							break;
						case "RawNegativeY":
							if (bdata2.ContainsKey(3) == true) {
								textureImageBlocks[i].FindNodes("TEXTUREIMAGEBLOCKDATA")[0].data = bdata2[3];
								textureImageBlocks[i].attributes["size"].data = EndianBitConverter.Big.GetBytes(bdata2[3].Length);
							} else {
								throw new Exception("Loading cubemap failed because not all blocks were found. (Write)");
							}
							break;
						case "RawPositiveZ":
							if (bdata2.ContainsKey(4) == true) {
								textureImageBlocks[i].FindNodes("TEXTUREIMAGEBLOCKDATA")[0].data = bdata2[4];
								textureImageBlocks[i].attributes["size"].data = EndianBitConverter.Big.GetBytes(bdata2[4].Length);
							} else {
								throw new Exception("Loading cubemap failed because not all blocks were found. (Write)");
							}
							break;
						case "RawNegativeZ":
							if (bdata2.ContainsKey(5) == true) {
								textureImageBlocks[i].FindNodes("TEXTUREIMAGEBLOCKDATA")[0].data = bdata2[5];
								textureImageBlocks[i].attributes["size"].data = EndianBitConverter.Big.GetBytes(bdata2[5].Length);
							} else {
								throw new Exception("Loading cubemap failed because not all blocks were found. (Write)");
							}
							break;
					}
				}
			} else {
				if ((uint)node.attributes["imageBlockCount"].Value > 1) {
					throw new Exception("Loading cubemap failed because not all blocks were found. (Write)");
				}
				textureImageBlocks[0].FindNodes("TEXTUREIMAGEBLOCKDATA")[0].data = bdata;
				textureImageBlocks[0].attributes["size"].data = EndianBitConverter.Big.GetBytes(bdata.Length);
			}
		}
	}
}
