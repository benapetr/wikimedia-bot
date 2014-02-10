//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

// Created by Petr Bena benapetr@gmail.com
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace wmib
{
	/// <summary>
	/// Represent a channel
	/// </summary>
	[Serializable]
	public class Channel
	{
		/// <summary>
		/// Channel name
		/// </summary>
		public string Name = null;

		/// <summary>
		/// Language used in this channel
		/// </summary>
		public string Language = null;

		/// <summary>
		/// List of users
		/// </summary>
		public List<User> UserList = new List<User>();

		/// <summary>
		/// Whether the channel contains a fresh user list (in case it doesn't bot will auto reparse it from ircd)
		/// </summary>
		public bool HasFreshUserList = false;

		/// <summary>
		/// List of channels that have shared infobot db
		/// </summary>
		public List<string> SharedChans = new List<string>();

		/// <summary>
		/// Objects created by extensions
		/// </summary>
		public Dictionary<string, object> ExtensionObjects = new Dictionary<string, object>();
		private Dictionary<string, string> ExtensionData = new Dictionary<string, string>();

		/// <summary>
		/// If this is true, no messages are sent to this channel
		/// </summary>
		public bool Suppress = false;

		/// <summary>
		/// List of ignored names for infobot
		/// </summary>
		public List<string> Infobot_IgnoredNames = new List<string>();

		/// <summary>
		/// Wait time between responses to users who try to speak to the bot
		/// </summary>
		public int RespondWait = 120;

		/// <summary>
		/// Whether bot should respond to users who think that the bot is user and speak to him
		/// </summary>
		public bool RespondMessage = false;

		/// <summary>
		/// Time of last message received in channel
		/// </summary>
		public DateTime TimeOfLastMsg = DateTime.Now;

		/// <summary>
		/// Doesn't send any warnings on error
		/// </summary>
		public bool SuppressWarnings = false;

		/// <summary>
		/// Whether unknown users should be ignored or not
		/// </summary>
		public bool IgnoreUnknown = false;

		/// <summary>
		/// Target db of shared infobot
		/// </summary>
		public string SharedDB = null;

		/// <summary>
		/// List of channels we share db with
		/// </summary>
		public List<Channel> SharedLinkedChan = null;

		/// <summary>
		/// Default instance this channel belongs to
		/// </summary>
		public string DefaultInstance = "any";

		/// <summary>
		/// Current instance
		/// </summary>
		[NonSerialized]
		public Instance PrimaryInstance = null;

		/// <summary>
		/// Users
		/// </summary>
		public Security Users = null;
		private bool IsRemoved = false;

		/// <summary>
		/// Initializes a new instance of the <see cref="wmib.Channel"/> class.
		/// </summary>
		/// <param name='name'>
		/// Name.
		/// </param>
		public Channel(string name)
		{
			Name = name;
			Language = "en";
			SharedLinkedChan = new List<Channel>();
			SharedDB = "";
			Suppress = false;
			LoadConfig();
			if (DefaultInstance == "any")
			{
				PrimaryInstance = Core.GetInstance();
				// we need to save the instance so that next time bot reconnect to bouncer it uses the same instance
				DefaultInstance = PrimaryInstance.Nick;
				SaveConfig();
			} else
			{
				lock(Core.Instances)
				{
					if (!Core.Instances.ContainsKey(DefaultInstance))
					{
						Syslog.WarningLog("There is no instance " + DefaultInstance + " reassigning channel " + this.Name +
						                  " to a different instance");
						this.PrimaryInstance = Core.GetInstance();
						Syslog.Log("Reassigned to " + this.PrimaryInstance.Nick);
					} else
					{
						PrimaryInstance = Core.Instances[DefaultInstance];
					}
				}
			}
			if (!Directory.Exists(Configuration.WebPages.HtmlPath))
			{
				Directory.CreateDirectory(Configuration.WebPages.HtmlPath);
			}
			if (!Directory.Exists(Configuration.WebPages.HtmlPath + Path.DirectorySeparatorChar + Name))
			{
				Directory.CreateDirectory(Configuration.WebPages.HtmlPath + Path.DirectorySeparatorChar + Name);
			}
			Users = new Security(Name);
			lock(ExtensionHandler.Extensions)
			{
				foreach (Module module in ExtensionHandler.Extensions)
				{
					try
					{
						if (module.IsWorking)
						{
							Channel self = this;
							module.Hook_Channel(self);
						}
					} catch (Exception fail)
					{
						Syslog.Log("MODULE: exception at Hook_Channel in " + module.Name, true);
						Core.HandleException(fail);
					}
				}
			}
		}

		/// <summary>
		/// Returns true if this channel is already existing in memory
		/// </summary>
		/// <param name="_Channel"></param>
		/// <returns></returns>
		public static bool ConfigExists(string _Channel)
		{
			string conf_file = Variables.ConfigurationDirectory + "/" + _Channel + ".setting";
			return File.Exists(conf_file);
		}

		/// <summary>
		/// Change the config
		/// </summary>
		/// <param name="name"></param>
		/// <param name="data"></param>
		public void Extension_SetConfig(string name, string data)
		{
			lock(ExtensionData)
			{
				if (ExtensionData.ContainsKey(name))
				{
					ExtensionData[name] = data;
					return;
				}
			}
			ExtensionData.Add(name, data);
		}

		/// <summary>
		/// Retrieve a config
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public string Extension_GetConfig(string key)
		{
			lock(ExtensionData)
			{
				if (ExtensionData.ContainsKey(key))
				{
					return ExtensionData[key];
				}
			}
			return null;
		}

		/// <summary>
		/// Get an object created by extension of name
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public object RetrieveObject(string name)
		{
			try
			{
				lock(ExtensionObjects)
				{
					if (ExtensionObjects.ContainsKey(name))
					{
						return ExtensionObjects[name];
					}
				}
			} catch (Exception er)
			{
				Core.HandleException(er);
			}
			return null;
		}

		public User RetrieveUser(string Nick)
		{
			Nick = Nick.ToUpper();
			lock(UserList)
			{
				foreach (User xx in UserList)
				{
					if (Nick == xx.Nick.ToUpper())
					{
						return xx;
					}
				}
			}
			return null;
		}

		/// <summary>
		/// Remove object from memory
		/// </summary>
		/// <param name="Nm">Name of object</param>
		/// <returns></returns>
		public bool UnregisterObject(string Nm)
		{
			try
			{
				lock(ExtensionObjects)
				{
					if (!ExtensionObjects.ContainsKey(Nm))
					{
						return true;
					}
					ExtensionObjects.Remove(Nm);
					return true;
				}
			} catch (Exception er)
			{
				Core.HandleException(er);
			}
			return false;
		}

		/// <summary>
		/// Register a new object in memory
		/// </summary>
		/// <param name="Ob">Data</param>
		/// <param name="Nm">Name</param>
		/// <returns></returns>
		public bool RegisterObject(object Ob, string Nm)
		{
			try
			{
				lock(ExtensionObjects)
				{
					if (ExtensionObjects.ContainsKey(Nm))
					{
						return false;
					}
					ExtensionObjects.Add(Nm, Ob);
					return true;
				}
			} catch (Exception er)
			{
				Core.HandleException(er);
				return false;
			}
		}

		/// <summary>
		/// Load config of channel :)
		/// </summary>
		public void LoadConfig()
		{
			string conf_file = Variables.ConfigurationDirectory + "/" + Name + ".setting";
			Core.RecoverFile(conf_file, Name);
			try
			{
				XmlDocument data = new XmlDocument();
				if (!File.Exists(conf_file))
				{
					SaveConfig();
					return;
				}
				data.Load(conf_file);
				foreach (XmlNode xx in data.ChildNodes[0].ChildNodes)
				{
					switch (xx.Name)
					{
						case "extension":
							if (ExtensionData.ContainsKey(xx.Attributes[0].Value))
							{
								ExtensionData[xx.Attributes[0].Value] = xx.Attributes[1].Value;
							} else
							{
								ExtensionData.Add(xx.Attributes[0].Value, xx.Attributes[1].Value);
							}
							continue;
						case "ignored":
							Infobot_IgnoredNames.Add(xx.Attributes[1].Value);
							continue;
						case "sharedch":
							SharedChans.Add(xx.Attributes[1].Value);
							continue;

					}
					switch (xx.Attributes[0].Value)
					{
						case "talkmode":
							Suppress = bool.Parse(xx.Attributes[1].Value);
							break;
						case "langcode":
							Language = xx.Attributes[1].Value;
							break;
						case "respond_message":
							RespondMessage = bool.Parse(xx.Attributes[1].Value);
							break;
						case "ignore-unknown":
							IgnoreUnknown = bool.Parse(xx.Attributes[1].Value);
							break;
						case "suppress-warnings":
							SuppressWarnings = bool.Parse(xx.Attributes[1].Value);
							break;
						case "respond_wait":
							RespondWait = int.Parse(xx.Attributes[1].Value);
							break;
						case "sharedinfo":
							SharedDB = xx.Attributes[1].Value;
							break;
						case "defaultbot":
							DefaultInstance = xx.Attributes[1].Value;
							break;
					}
				}
			} catch (Exception fail)
			{
				Syslog.Log("Unable to load the config of " + Name, true);
				Core.HandleException(fail);
			}
		}

		private static void InsertData(string key, string value, ref XmlDocument document, ref XmlNode node, string Name = "local")
		{
			XmlAttribute name = document.CreateAttribute("key");
			name.Value = key;
			XmlAttribute kk = document.CreateAttribute("value");
			kk.Value = value;
			XmlNode db = document.CreateElement(Name);
			db.Attributes.Append(name);
			db.Attributes.Append(kk);
			node.AppendChild(db);
		}

		/// <summary>
		/// Save config
		/// </summary>
		public void SaveConfig()
		{
			try
			{
				XmlDocument data = new XmlDocument();
				XmlNode xmlnode = data.CreateElement("channel");
				InsertData("talkmode", Suppress.ToString(), ref data, ref xmlnode);
				InsertData("langcode", Language, ref data, ref xmlnode);
				InsertData("respond_message", RespondMessage.ToString(), ref data, ref xmlnode);
				InsertData("ignore-unknown", IgnoreUnknown.ToString(), ref data, ref xmlnode);
				InsertData("suppress-warnings", SuppressWarnings.ToString(), ref data, ref xmlnode);
				InsertData("respond_wait", RespondWait.ToString(), ref data, ref xmlnode);
				InsertData("sharedinfo", SharedDB, ref data, ref xmlnode);
				InsertData("defaultbot", DefaultInstance, ref data, ref xmlnode);
				if (!(SharedLinkedChan.Count < 1))
				{
					foreach (Channel current in SharedLinkedChan)
					{
						InsertData("name", current.Name, ref data, ref xmlnode, "sharedch");
					}
				}
				if (!(Infobot_IgnoredNames.Count < 1))
				{
					foreach (string curr in Infobot_IgnoredNames)
					{
						InsertData("name", curr, ref data, ref xmlnode, "ignored");
					}
				}
				if (ExtensionData.Count > 0)
				{
					foreach (KeyValuePair<string, string> item in ExtensionData)
					{
						InsertData(item.Key, item.Value, ref data, ref xmlnode, "extension");
					}
				}
				if (File.Exists(Variables.ConfigurationDirectory + "/" + Name + ".setting"))
				{
					Core.BackupData(Variables.ConfigurationDirectory + "/" + Name + ".setting");
					if (!File.Exists(Configuration.TempName(Variables.ConfigurationDirectory + "/" + Name + ".setting")))
					{
						Syslog.WarningLog("Unable to create backup file for " + Name);
					}
				}
				data.AppendChild(xmlnode);
				data.Save(Variables.ConfigurationDirectory + "/" + Name + ".setting");
				if (File.Exists(Configuration.TempName(Variables.ConfigurationDirectory + "/" + Name + ".setting")))
				{
					File.Delete(Configuration.TempName(Variables.ConfigurationDirectory + "/" + Name + ".setting"));
				}
			} catch (Exception)
			{
				Core.RecoverFile(Variables.ConfigurationDirectory + "/" + Name + ".setting", Name);
			}
		}

		/// <summary>
		/// Remove all refs
		/// </summary>
		public void Remove()
		{
			if (IsRemoved)
			{
				Syslog.DebugLog("Channel is already removed");
				return;
			}
			Users = null;
			lock(ExtensionData)
			{
				ExtensionData.Clear();
			}
			lock(ExtensionObjects)
			{
				ExtensionObjects.Clear();
			}

			SharedDB = null;
			SharedChans.Clear();
			SharedLinkedChan.Clear();
		}

		/// <summary>
		/// Return true if user is present in channel
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public bool ContainsUser(string name)
		{
			name = name.ToUpper();
			foreach (User us in UserList)
			{
				if (name == us.Nick.ToUpper())
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Return number of channels that infobot share db with
		/// </summary>
		/// <returns></returns>
		public int SharesNo()
		{
			foreach (string x in SharedChans)
			{
				string name = x.Replace(" ", "");
				if (name != "")
				{
					if (Core.GetChannel(name) != null)
					{
						if (SharedLinkedChan.Contains(Core.GetChannel(name)) == false)
						{
							SharedLinkedChan.Add(Core.GetChannel(name));
						}
					}
				}
			}
			return 0;
		}
	}
}

