using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.XPath;
using System.Globalization;
using System.Windows.Forms;


namespace SRWE
{
	static class SRWE_Defaults
	{
		internal static readonly bool ForceExitSizeMoveMessage = false;
		internal static readonly bool AutoAttachToLastKnownProcess = false;
		internal static readonly int MaxNumberOfRecentProfiles = 20;
	}

	/// <summary>
	/// SRWE_Settings class.
	/// </summary>
	static class SRWE_Settings
	{
		private static string s_settingsPath;
		private static XmlDocument s_xmlSettings, s_xmlDefaultSettings;
		private static int s_nUpdateInterval;
		private static bool s_bForceExitSizeMoveMessage, s_bAutoAttachToLastKnownProcess;
		private static List<string> s_recentProfiles;
		private static List<string> s_recentProcesses;
        private static List<SRWE_HotKey> s_hotKeys = new List<SRWE_HotKey>();

		static SRWE_Settings()
		{
			using(MemoryStream ms = new MemoryStream(Properties.Resources.XML_Settings))
			{
				s_xmlDefaultSettings = new XmlDocument();
				s_xmlDefaultSettings.Load(ms);
				ms.Close();
			}
			try
			{
				s_settingsPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				s_settingsPath = Path.Combine(s_settingsPath, "SRWE");
				Directory.CreateDirectory(s_settingsPath);
				s_settingsPath = Path.Combine(s_settingsPath, "Settings.xml");

				if(!File.Exists(s_settingsPath))
				{
					File.WriteAllBytes(s_settingsPath, Properties.Resources.XML_Settings);
				}
				s_xmlSettings = new XmlDocument();
				try
				{
					s_xmlSettings.Load(s_settingsPath);
				}
				catch
				{
					// failure during load, write out new settings file and load that one instead. This is nicer than flushing any older settings file as 
					// we can now migrate any old file to new versions without flushing old settings. 
					File.WriteAllBytes(s_settingsPath, Properties.Resources.XML_Settings);
					s_xmlSettings.Load(s_settingsPath);
				}
				bool versionMisMatch = !CheckSettingsVersion(s_xmlSettings.DocumentElement.Attributes["Version"]);
				LoadSettings();
				if(versionMisMatch)
				{
					// bump version to one in default xml document
					s_xmlSettings.DocumentElement.Attributes["Version"].Value = s_xmlDefaultSettings.DocumentElement.Attributes["Version"].Value;
					// write the settings out again, as they've been migrated to a new version
					s_xmlSettings.Save(s_settingsPath);
				}
			}
			catch
			{
				s_xmlSettings = (XmlDocument)s_xmlDefaultSettings.Clone();
				LoadSettings();
			}
		}

		public static void AddRecentProcess(string name)
		{
			XmlNode xmlProcess = s_xmlSettings.DocumentElement.SelectSingleNode("RecentProcesses/Process[@Name='" + name + "']");

			if (xmlProcess != null)
			{
				if (xmlProcess != xmlProcess.ParentNode.FirstChild)
				{
					XmlNode xmlParent = xmlProcess.ParentNode;
					xmlParent.RemoveChild(xmlProcess);
					xmlParent.PrependChild(xmlProcess);
				}
			}
			else
			{
				XmlNode xmlRecentProcesses = s_xmlSettings.DocumentElement["RecentProcesses"];
				if(xmlRecentProcesses.ChildNodes.Count > SRWE_Defaults.MaxNumberOfRecentProfiles)
				{
					xmlRecentProcesses.RemoveChild(xmlRecentProcesses.ChildNodes[xmlRecentProcesses.ChildNodes.Count - 1]);
				}
				xmlProcess = s_xmlSettings.CreateElement("Process");
				xmlProcess.Attributes.Append(s_xmlSettings.CreateAttribute("Name")).Value = name;
				s_xmlSettings.DocumentElement["RecentProcesses"].PrependChild(xmlProcess);
			}

			s_recentProcesses = new List<string>();

			foreach (XmlNode xmlItem in s_xmlSettings.DocumentElement.SelectNodes("RecentProcesses/Process"))
				s_recentProcesses.Add(xmlItem.Attributes["Name"].Value);

			s_xmlSettings.Save(s_settingsPath);
		}

		public static void RemoveRecentProcess(string name)
		{
			XmlNode xmlParent = s_xmlSettings.DocumentElement["RecentProcesses"];
			XmlNodeList xmlProcesses = xmlParent.SelectNodes("Process[@Name='" + name + "']");

			for (int i = xmlProcesses.Count - 1; i >= 0; i--)
				xmlParent.RemoveChild(xmlProcesses[i]);

			s_recentProcesses = new List<string>();

			foreach (XmlNode xmlItem in xmlParent.SelectNodes("Process"))
				s_recentProcesses.Add(xmlItem.Attributes["name"].Value);

			s_xmlSettings.Save(s_settingsPath);
		}

		public static void AddRecentProfile(string filepath)
		{
			if (!File.Exists(filepath)) return;

			XmlNode xmlProfile = s_xmlSettings.DocumentElement.SelectSingleNode("RecentProfiles/Profile[@FilePath='" + filepath + "']");

			if (xmlProfile != null)
			{
				if (xmlProfile != xmlProfile.ParentNode.FirstChild)
				{
					XmlNode xmlParent = xmlProfile.ParentNode;
					xmlParent.RemoveChild(xmlProfile);
					xmlParent.PrependChild(xmlProfile);
				}
			}
			else
			{
				XmlNode xmlRecentProfiles = s_xmlSettings.DocumentElement["RecentProfiles"];
				if(xmlRecentProfiles.ChildNodes.Count > 19)
				{
					xmlRecentProfiles.RemoveChild(xmlRecentProfiles.ChildNodes[xmlRecentProfiles.ChildNodes.Count - 1]);
				}

				xmlProfile = s_xmlSettings.CreateElement("Profile");
				xmlProfile.Attributes.Append(s_xmlSettings.CreateAttribute("FilePath")).Value = filepath;
				s_xmlSettings.DocumentElement["RecentProfiles"].PrependChild(xmlProfile);
			}

			s_recentProfiles = new List<string>();

			foreach (XmlNode xmlItem in s_xmlSettings.DocumentElement.SelectNodes("RecentProfiles/Profile"))
				s_recentProfiles.Add(xmlItem.Attributes["FilePath"].Value);

			s_xmlSettings.Save(s_settingsPath);
		}

		public static void RemoveRecentProfile(string filepath)
		{
			XmlNode xmlParent = s_xmlSettings.DocumentElement["RecentProfiles"];
			XmlNodeList xmlProfiles = xmlParent.SelectNodes("Profile[@FilePath='" + filepath + "']");

			for (int i = xmlProfiles.Count - 1; i >= 0; i--)
				xmlParent.RemoveChild(xmlProfiles[i]);

			s_recentProfiles = new List<string>();

			foreach (XmlNode xmlItem in xmlParent.SelectNodes("Profile"))
				s_recentProfiles.Add(xmlItem.Attributes["FilePath"].Value);

			s_xmlSettings.Save(s_settingsPath);
		}



		private static bool CheckSettingsVersion(XmlAttribute attribVersion)
		{
			return (attribVersion != null && attribVersion.Value == s_xmlDefaultSettings.DocumentElement.Attributes["Version"].Value);
		}


		private static void LoadSettings()
		{
			var settingsElement = s_xmlSettings.DocumentElement["Settings"];
			s_nUpdateInterval = SRWE_Utility.SAFE_String_2_Int(settingsElement["UpdateInterval"].Attributes["Value"].Value, 1000);
			var forceExitSizeMoveMessageElement = settingsElement["ForceExitSizeMoveMessage"];
			if(forceExitSizeMoveMessageElement == null)
			{
				// migrate settings file.
				forceExitSizeMoveMessageElement = SRWE_Utility.AppendChildElement(s_xmlSettings, settingsElement, "ForceExitSizeMoveMessage");
				SRWE_Utility.AppendAttribute(s_xmlSettings, forceExitSizeMoveMessageElement, "Value");
				SetForceExitSizeMoveMessageValue();
			}
			else
			{
				s_bForceExitSizeMoveMessage = SRWE_Utility.SAFE_String_2_Bool(forceExitSizeMoveMessageElement.Attributes["Value"].Value, SRWE_Defaults.ForceExitSizeMoveMessage);
			}
			var autoAttachToLastKnownProcessElement = settingsElement["AutoAttachToLastKnownProcess"];
			if(autoAttachToLastKnownProcessElement==null)
			{
				// migrate settings file
				autoAttachToLastKnownProcessElement = SRWE_Utility.AppendChildElement(s_xmlSettings, settingsElement, "AutoAttachToLastKnownProcess");
				SRWE_Utility.AppendAttribute(s_xmlSettings, autoAttachToLastKnownProcessElement, "Value");
				SetAutoAttachToLastKnownProcessValue();
			}
			else
			{
				s_bAutoAttachToLastKnownProcess = SRWE_Utility.SAFE_String_2_Bool(autoAttachToLastKnownProcessElement.Attributes["Value"].Value, SRWE_Defaults.AutoAttachToLastKnownProcess);
			}

			if(s_nUpdateInterval < 100)
				s_nUpdateInterval = 100;
			else if(s_nUpdateInterval > 30000)
				s_nUpdateInterval = 30000;

			XmlNodeList xmlNodes = s_xmlSettings.DocumentElement.SelectNodes("RecentProcesses/Process");
			s_recentProcesses = new List<string>();

			foreach(XmlNode xmlItem in xmlNodes)
				s_recentProcesses.Add(xmlItem.Attributes["Name"].Value);

			xmlNodes = s_xmlSettings.DocumentElement.SelectNodes("RecentProfiles/Profile");
			s_recentProfiles = new List<string>();

			foreach(XmlNode xmlItem in xmlNodes)
				s_recentProfiles.Add(xmlItem.Attributes["FilePath"].Value);

			s_hotKeys.Clear();
			xmlNodes = s_xmlSettings.DocumentElement.SelectNodes("HotKeys/HotKey");
			foreach(XmlNode hotkey in xmlNodes) s_hotKeys.Add(new SRWE_HotKey((XmlElement)hotkey));
		}

		private static void SetForceExitSizeMoveMessageValue()
		{
			s_xmlSettings.DocumentElement["Settings"]["ForceExitSizeMoveMessage"].Attributes["Value"].Value = XmlConvert.ToString(s_bForceExitSizeMoveMessage);
		}


		private static void SetAutoAttachToLastKnownProcessValue()
		{
			s_xmlSettings.DocumentElement["Settings"]["AutoAttachToLastKnownProcess"].Attributes["Value"].Value = XmlConvert.ToString(s_bAutoAttachToLastKnownProcess);
		}


		public static int UpdateInterval
		{
			get { return s_nUpdateInterval; }
		}

		public static List<string> RecentProcesses
		{
			get { return s_recentProcesses; }
		}

		public static List<string> RecentProfiles
		{
			get { return s_recentProfiles; }
		}

		public static List<SRWE_HotKey> HotKeys
		{
			get { return s_hotKeys; }
		}

		public static bool ForceExitSizeMoveMessage
		{
			get { return s_bForceExitSizeMoveMessage; }
			set
			{
				bool currentValue = s_bForceExitSizeMoveMessage;
				if(value != currentValue)
				{
					s_bForceExitSizeMoveMessage = value;
					SetForceExitSizeMoveMessageValue();
					s_xmlSettings.Save(s_settingsPath);
				}
			}
		}

		public static bool AutoAttachToLastKnownProcess
		{
			get { return s_bAutoAttachToLastKnownProcess; }
			set
			{
				bool currentValue = s_bAutoAttachToLastKnownProcess;
				if(currentValue != value)
				{
					s_bAutoAttachToLastKnownProcess = value;
					SetAutoAttachToLastKnownProcessValue();
					s_xmlSettings.Save(s_settingsPath);
				}
			}
		}
	}

	/// <summary>
	/// SRWE_Utility class.
	/// </summary>
	static class SRWE_Utility
	{
		public static XmlElement AppendChildElement(XmlDocument document, XmlNode parent, string elementName)
		{
			var toAdd = document.CreateElement(elementName);
			parent.AppendChild(toAdd);
			return toAdd;
		}

		public static XmlAttribute AppendAttribute(XmlDocument document, XmlNode node, string attributeName)
		{
			var toAdd = document.CreateAttribute(attributeName);
			node.Attributes.Append(toAdd);
			return toAdd;
		}

		public static int SAFE_String_2_Int(string value, int nDefValue)
		{
			int nResult;

			if (int.TryParse(value, out nResult))
				return nResult;
			return nDefValue;
		}

		public static float SAFE_String_2_Float(string value)
        {
			float result;
			if (float.TryParse(value, out result))
				return result;
			return 1f;
        }

		public static int SAFE_HexString_2_Int(string hexString, int nDefValue)
		{
			int nResult;

			if (int.TryParse(hexString, NumberStyles.AllowHexSpecifier, CultureInfo.CurrentCulture, out nResult))
				return nResult;
			return nDefValue;
		}

		public static bool SAFE_String_2_Bool(string value, bool defaultValue)
		{
			if(string.IsNullOrEmpty(value))
			{
				return defaultValue;
			}
			// TryParse doesn't work, as it can't deal with 1, 0, and true/false case differences.
			try
			{
				return XmlConvert.ToBoolean(value);
			}
			catch
			{
				// not a bool
				return defaultValue;
			}
		}

        public static string SAFE_XmlNodeValue(XmlNode xmlNode)
        {
            if (xmlNode != null) return xmlNode.InnerText;

            return "";
        }

        internal static float SAFE_ParseRatio(string text)
        {
			string[] ratios = text.Split(':');
			float w, h;
			if (!float.TryParse(ratios[0], out w))
				return 1f;
			if (!float.TryParse(ratios[1], out h))
				return 1f;
			return w / h;
        }
    }

    /// <summary>
    /// SRWE_HotKey class.
    /// </summary>
    class SRWE_HotKey
    {
        public string Name { get; private set; }
        public Keys? HotKey { get; private set; }
        public bool CTRL { get; private set; }
        public bool ALT { get; private set; }
        public bool SHIFT { get; private set; }

        public SRWE_HotKey(XmlElement xmlHotKey)
        {
            this.Name = SRWE_Utility.SAFE_XmlNodeValue(xmlHotKey.Attributes["Name"]);
            
            Keys key;
            if (Enum.TryParse(xmlHotKey.InnerText, out key)) this.HotKey = key;

            this.CTRL = SRWE_Utility.SAFE_XmlNodeValue(xmlHotKey.Attributes["CTRL"]) == "1";
            this.ALT = SRWE_Utility.SAFE_XmlNodeValue(xmlHotKey.Attributes["ALT"]) == "1";
            this.SHIFT = SRWE_Utility.SAFE_XmlNodeValue(xmlHotKey.Attributes["SHIFT"]) == "1";
        }
    }
}
