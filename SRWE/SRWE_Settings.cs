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
	/// <summary>
	/// SRWE_Settings class.
	/// </summary>
	static class SRWE_Settings
	{
		private static string s_settingsPath;
		private static XmlDocument s_xmlSettings;
		private static int s_nUpdateInterval;
		private static List<string> s_recentProfiles;
		private static List<string> s_recentProcesses;
        private static List<SRWE_HotKey> s_hotKeys = new List<SRWE_HotKey>();

		static SRWE_Settings()
		{
			try
			{
				s_settingsPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				s_settingsPath = Path.Combine(s_settingsPath, "SRWE");
				Directory.CreateDirectory(s_settingsPath);
				s_settingsPath = Path.Combine(s_settingsPath, "Settings.xml");

				if (!File.Exists(s_settingsPath))
					File.WriteAllBytes(s_settingsPath, Properties.Resources.XML_Settings);

				s_xmlSettings = new XmlDocument();
				s_xmlSettings.Load(s_settingsPath);

				if (!CheckSettingsVersion(s_xmlSettings.DocumentElement.Attributes["Version"]))
				{
					File.WriteAllBytes(s_settingsPath, Properties.Resources.XML_Settings);
					s_xmlSettings.Load(s_settingsPath);
				}
				LoadSettings();
			}
			catch
			{
				MemoryStream ms = new MemoryStream(Properties.Resources.XML_Settings);
				s_xmlSettings = new XmlDocument();
				s_xmlSettings.Load(ms);
				ms.Close();
				LoadSettings();
			}
		}

		private static bool CheckSettingsVersion(XmlAttribute attribVersion)
		{
			MemoryStream ms = new MemoryStream(Properties.Resources.XML_Settings);
			XmlDocument xmlActualSettings = new XmlDocument();
			xmlActualSettings.Load(ms);
			ms.Close();

			return (attribVersion != null && attribVersion.Value == xmlActualSettings.DocumentElement.Attributes["Version"].Value);
		}

		private static void LoadSettings()
		{
			s_nUpdateInterval = SRWE_Utility.SAFE_String_2_Int(s_xmlSettings.DocumentElement["Settings"]["UpdateInterval"].Attributes["Value"].Value, 1000);

			if (s_nUpdateInterval < 100)
				s_nUpdateInterval = 100;
			else if (s_nUpdateInterval > 30000)
				s_nUpdateInterval = 30000;

			XmlNodeList xmlNodes = s_xmlSettings.DocumentElement.SelectNodes("RecentProcesses/Process");
			s_recentProcesses = new List<string>();

			foreach (XmlNode xmlItem in xmlNodes)
				s_recentProcesses.Add(xmlItem.Attributes["Name"].Value);

			xmlNodes = s_xmlSettings.DocumentElement.SelectNodes("RecentProfiles/Profile");
			s_recentProfiles = new List<string>();

			foreach (XmlNode xmlItem in xmlNodes)
				s_recentProfiles.Add(xmlItem.Attributes["FilePath"].Value);

            s_hotKeys.Clear();
            xmlNodes = s_xmlSettings.DocumentElement.SelectNodes("HotKeys/HotKey");
            foreach (XmlNode hotkey in xmlNodes) s_hotKeys.Add(new SRWE_HotKey((XmlElement)hotkey));
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
				if (xmlRecentProcesses.ChildNodes.Count > 9) xmlRecentProcesses.RemoveChild(xmlRecentProcesses.ChildNodes[xmlRecentProcesses.ChildNodes.Count - 1]);

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
				if (xmlRecentProfiles.ChildNodes.Count > 9) xmlRecentProfiles.RemoveChild(xmlRecentProfiles.ChildNodes[xmlRecentProfiles.ChildNodes.Count - 1]);

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
	}

	/// <summary>
	/// SRWE_Utility class.
	/// </summary>
	static class SRWE_Utility
	{
		public static int SAFE_String_2_Int(string value, int nDefValue)
		{
			int nResult;

			if (int.TryParse(value, out nResult))
				return nResult;
			return nDefValue;
		}

		public static int SAFE_HexString_2_Int(string hexString, int nDefValue)
		{
			int nResult;

			if (int.TryParse(hexString, NumberStyles.AllowHexSpecifier, CultureInfo.CurrentCulture, out nResult))
				return nResult;
			return nDefValue;
		}

        public static string SAFE_XmlNodeValue(XmlNode xmlNode)
        {
            if (xmlNode != null) return xmlNode.InnerText;

            return "";
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
