/*
 * Created by SharpDevelop.
 * User: LYCJ
 * Date: 9/1/2007
 * Time: 15:45
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

// IniFiles.pas
// IniFile implementation for C#

//Copyright (C) Joseph Leung 2007 (author2004 (.at.) quickzip (.dot.) org)
//
//This library is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 2.1 of the License, or (at your option) any later version.
//
//This library is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//Lesser General Public License for more details.
//
//You should have received a copy of the GNU Lesser General Public
//License along with this library; if not, write to the Free Software
//Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
//using Cake3;

namespace qzLite.Components
{
	public class KeyEntry
	{
		private SectionEntry owner;
		private string key, value;
		public string Key { get { return key; } }
		public string Value { get { return value; } 
			set { this.value = value; owner.owner.NeedUpdate = true; } }
		public bool ValueAsBool { get { return (Value == "TRUE"); } 
			set { if (value) Value = "TRUE"; else Value = "FALSE"; } }
		public Int32 ValueAsInt { 
			get { try { return Convert.ToInt32(Value); } catch { return 0; } }
			set { Value = value.ToString(); } }
		
		
		public KeyEntry(SectionEntry aSection, string aKey, string aValue)
		{
			key = aKey;
			value = aValue;
			owner = aSection;
		}				
	}
	
	public class SectionEntry : List<KeyEntry>
	{
		internal QzIniFiles owner;
		private string section;
		public string Section { get { return section; } }
		
		public SectionEntry(QzIniFiles anIniFile, string aSection)
		{
			owner = anIniFile;
			section = aSection;
		}	
		
		public KeyEntry KeyExistsEx(string key)
		{
			foreach (KeyEntry entry in this)
				if (entry.Key == key)
					return entry;
			return null;
		}
		
		public bool KeyExists(string key)
		{
			return (KeyExistsEx(key) != null);
		}
		
		public void RemoveKey(string key)
		{
			foreach (KeyEntry entry in this)
				if (entry.Key == key)
				{
					Remove(entry);
					break;
				}								
		}
				
		public void AddKey(string key, string value)
		{
			RemoveKey(key);
			Add(new KeyEntry(this, key, value));
		}
		
	
		public KeyEntry this[string key]
		{
			get
			{
				KeyEntry retVal = KeyExistsEx(key);
				if (retVal != null)
					return retVal;
				if (!owner.ReadOnly)
				{
					retVal = new KeyEntry(this, key, null);
					Add(retVal);
					return retVal;
				}
				return null;		
			
			}			
		}
	}
		
	/// <summary>
	/// Description of IniFiles.
	/// </summary>
	public class QzIniFiles
	{
		private string iniFile;
		private List<SectionEntry> sections;
		public bool ReadOnly;
		public bool NeedUpdate;
		private System.Text.Encoding encoding = System.Text.Encoding.UTF8;
		public string Filename
		{
			get { return iniFile; }
		}
		public QzIniFiles(string filename)
		{
			iniFile = filename;
			sections = new List<SectionEntry>();
			ReadOnly = false;	
			NeedUpdate = false;
			
			if (File.Exists(iniFile)) 
			{
				StreamReader sr = new StreamReader(iniFile);
				this.readIni((TextReader)sr);				
				sr.Close();
				sr.Dispose();
			}
		}
		
		public QzIniFiles(string filename, System.Text.Encoding encoding)
		{
			iniFile = filename;
			sections = new List<SectionEntry>();
			ReadOnly = false;	
			NeedUpdate = false;
			this.encoding = encoding;
			
			if (File.Exists(iniFile)) 
			{
				StreamReader sr = new StreamReader(iniFile, this.encoding);				
				this.readIni((TextReader)sr);				
				sr.Close();
				sr.Dispose();
			}
		}
		
		public SectionEntry SectionExistsEx(string section)
		{
			foreach (SectionEntry entry in sections)
				if (entry.Section == section)
					return entry;
			return null;
		}
		
		public bool SectionExists(string key)
		{
			return (SectionExistsEx(key) != null);
		}
		
		public void RemoveSection(string section)
		{
			foreach (SectionEntry entry in sections)
				if (entry.Section == section)
				{
					sections.Remove(entry);
					break;
				}								
		}
							
		public SectionEntry this[string section]
		{
			get	
			{
				SectionEntry retVal = SectionExistsEx(section);
				if (retVal != null)
					return retVal;
				if (!ReadOnly)
				{
					retVal = new SectionEntry(this, section);
					sections.Add(retVal);
					return retVal;
				}
				return null;
			}
		}
		
		public void UpdateFile()
		{
			if (!NeedUpdate) return;
			
			TextWriter tw = new StreamWriter(iniFile, false, encoding);
			writeIni(tw);
			tw.Flush();
			tw.Close();
			tw.Dispose();
		}
		
		public void ReadFormBounds(Form form, string section)
		{
			if ((this[section].KeyExists("Top")) &&
			    (this[section].KeyExists("Left")) &&
			    (this[section].KeyExists("Height")) &&
			    (this[section].KeyExists("Width")))
			{
				int top = Math.Max(0,this[section]["Top"].ValueAsInt);
				int left = Math.Max(0,this[section]["Left"].ValueAsInt);
				int height = Math.Max(100,this[section]["Height"].ValueAsInt);
				int width = Math.Max(150,this[section]["Width"].ValueAsInt);
					
				form.SetBounds(left, top, width, height);
			}				
		}
		
		public void ReadFormSize(Form form, string section)
		{
			if ((this[section].KeyExists("Height")) &&
			    (this[section].KeyExists("Width")))
			{
				int height = Math.Max(100,this[section]["Height"].ValueAsInt);
				int width = Math.Max(150,this[section]["Width"].ValueAsInt);
					
				form.Size = new Size(width, height);
			}				
		}
		
		public void WriteFormBounds(Form form, string section)
		{
			if (!form.Visible) return;
			if (form.WindowState == System.Windows.Forms.FormWindowState.Maximized) return;
			
			this[section]["Top"].ValueAsInt = form.Top;
			this[section]["Left"].ValueAsInt = form.Left;
			this[section]["Height"].ValueAsInt = form.Height;
			this[section]["Width"].ValueAsInt = form.Width;
		}
		
		public ArrayList ReadStringList(string section, string key)
		{
			ArrayList retVal = new ArrayList();
			
			if (this[section].KeyExists(key+"-count"))
			{
				Int32 count = this[section][key+"-count"].ValueAsInt;
				for (Int32 i = 0; i < count; i++)
				{
					retVal.Add(this[section][key+"-"+i.ToString()].Value);
				}
			}
			
			return retVal;
		}
		
		public void WriteStringList(string section, string key, ArrayList list)
		{
			this[section][key+"-count"].ValueAsInt = list.Count;
			
			for (Int32 i = 0; i < list.Count; i++)
				this[section][key+"-"+i.ToString()].Value = (string)list[i];			 
		}
		
		
		private void readKey(string curSection, string keyString)
		{
			Int32 pos = keyString.IndexOf("=");
			if (pos != -1)
			{
				string key = keyString.Substring(0, pos);
				string value = keyString.Substring(pos+1, keyString.Length - pos - 1);
				this[curSection][key].Value = value;
			}
		}
		
		private void readIni(TextReader tr)
		{
			string curSection = "";
			
			string k = tr.ReadLine();
			while (k != null)
			{
				if ((k != "") && (k[0] != ';'))
				{
					if (k[0] == '[')
						curSection = k.Substring(1,k.Length-2);
					else readKey(curSection, k);					
				}
				k = tr.ReadLine();
			}
			
			NeedUpdate = false;
		}
		
		private void writeIni(TextWriter tw)
		{
			foreach (SectionEntry se in sections)
			{
				tw.WriteLine("[" + se.Section + "]");
				
				foreach (KeyEntry ke in se)
					tw.WriteLine(ke.Key + "=" + ke.Value);
			}
		}
		
		
		
	}
}
