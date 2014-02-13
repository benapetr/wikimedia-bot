using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using System.Text;

namespace wmib
{
    public partial class Infobot
    {
                /// <summary>
        /// Save to a file
        /// </summary>
        public void Save()
        {
            update = true;
            try
            {
                Parent.DebugLog("Saving database of infobot", 2);
                if (File.Exists(datafile_xml))
                {
                    Core.BackupData(datafile_xml);
                    if (!File.Exists(Configuration.TempName(datafile_xml)))
                    {
                        Parent.Log("Unable to create backup file for " + this.pChannel.Name);
                    }
                }

                Parent.DebugLog("Generating xml document", 4);

                System.Xml.XmlDocument data = new System.Xml.XmlDocument();
                System.Xml.XmlNode xmlnode = data.CreateElement("database");
                lock (this)
                {
                    foreach (InfobotAlias key in Alias)
                    {
                        System.Xml.XmlAttribute name = data.CreateAttribute("alias_key_name");
                        name.Value = key.Name;
                        System.Xml.XmlAttribute kk = data.CreateAttribute("alias_key_key");
                        kk.Value = key.Key;
                        System.Xml.XmlAttribute created = data.CreateAttribute("date");
                        created.Value = "";
                        System.Xml.XmlNode db = data.CreateElement("alias");
                        db.Attributes.Append(name);
                        db.Attributes.Append(kk);
                        db.Attributes.Append(created);
                        xmlnode.AppendChild(db);
                    }

                    foreach (InfobotKey key in Keys)
                    {
                        System.Xml.XmlAttribute name = data.CreateAttribute("key_name");
                        name.Value = key.Key;
                        System.Xml.XmlAttribute kk = data.CreateAttribute("data");
                        kk.Value = key.Text;
                        System.Xml.XmlAttribute created = data.CreateAttribute("created_date");
                        created.Value = key.CreationTime.ToBinary().ToString();
                        System.Xml.XmlAttribute nick = data.CreateAttribute("nickname");
                        nick.Value = key.User;
                        System.Xml.XmlAttribute last = data.CreateAttribute("touched");
                        last.Value = key.LastTime.ToBinary().ToString();
                        System.Xml.XmlAttribute triggered = data.CreateAttribute("triggered");
                        triggered.Value = key.Displayed.ToString();
                        XmlAttribute k = data.CreateAttribute("raw");
                        k.Value = key.Raw.ToString();
                        System.Xml.XmlNode db = data.CreateElement("key");
                        db.Attributes.Append(name);
                        db.Attributes.Append(kk);
                        db.Attributes.Append(nick);
                        db.Attributes.Append(created);
                        db.Attributes.Append(last);
                        db.Attributes.Append(triggered);
                        db.Attributes.Append(k);
                        xmlnode.AppendChild(db);
                    }
                    data.AppendChild(xmlnode);
                }
                Parent.DebugLog("Writing xml document to a file");
                data.Save(datafile_xml);
                Parent.DebugLog("Checking the previous file", 6);
                if (File.Exists(Configuration.TempName(datafile_xml)))
                {
                    Parent.DebugLog("Removing temp file", 6);
                    File.Delete(Configuration.TempName(datafile_xml));
                }
            }
            catch (Exception b)
            {
                try
                {
                    if (Core.RecoverFile(datafile_xml, pChannel.Name))
                    {
                        Parent.Log("Recovered db for channel " + pChannel.Name);
                    }
                    else
                    {
                        Parent.HandleException(b, pChannel.Name);
                    }
                }
                catch (Exception bb)
                {
                    Parent.HandleException(bb, pChannel.Name);
                }
            }
        }

        public bool LoadAncientDB()
        {
            lock (this)
            {
                Keys.Clear();
                // Checking if db isn't broken
                Core.RecoverFile(datafile_raw, pChannel.Name);
                if (!File.Exists(datafile_raw))
                {
                    return false;
                }

                string[] db = File.ReadAllLines(datafile_raw);
                foreach (string x in db)
                {
                    if (x.Contains(Configuration.System.Separator))
                    {
                        string[] info = x.Split(Char.Parse(Configuration.System.Separator));
                        string type = info[2];
                        string value = info[1];
                        string name = info[0];
                        if (type == "key")
                        {
                            string Locked = info[3];
                            Keys.Add(new InfobotKey(name.Replace("<separator>", "|"), value.Replace("<separator>", "|"), "", Locked, NA.ToBinary().ToString(),
                                NA.ToBinary().ToString()));
                        }
                        else
                        {
                            Alias.Add(new InfobotAlias(name.Replace("<separator>", "|"), value.Replace("<separator>", "|")));
                        }
                    }
                }
            }
            return true;
        }

        public bool LoadData()
        {
            lock (this)
            {
                Keys.Clear();
            }
            // Checking if db isn't broken
            Core.RecoverFile(datafile_xml, pChannel.Name);
            if (LoadAncientDB())
            {
                Parent.Log("Obsolete database found for " + pChannel.Name + " converting to new format");
                Save();
                File.Delete(datafile_raw);
                return true;
            }
            if (!File.Exists(datafile_xml))
            {
                // Create db
                Save();
                return true;
            }
            try
            {
                XmlDocument data = new XmlDocument();
                if (!File.Exists(datafile_xml))
                {
                    lock (this)
                    {
                        Keys.Clear();
                    }
                    return true;
                }
                data.Load(datafile_xml);
                lock (this)
                {
                    Keys.Clear();
                    Alias.Clear();
                }
                foreach (XmlNode xx in data.ChildNodes[0].ChildNodes)
                {
                    if (xx.Name == "alias")
                    {
                        InfobotAlias _Alias = new InfobotAlias(xx.Attributes[0].Value, xx.Attributes[1].Value);
                        lock (this)
                        {
                            Alias.Add(_Alias);
                        }
                        continue;
                    }
                    bool raw = false;
                    if (xx.Attributes.Count > 6)
                    {
                        raw = bool.Parse(xx.Attributes[6].Value);
                    }
                    InfobotKey _key = new InfobotKey(xx.Attributes[0].Value, xx.Attributes[1].Value, xx.Attributes[2].Value, "false", xx.Attributes[3].Value,
                    xx.Attributes[4].Value, int.Parse(xx.Attributes[5].Value), raw);
                    lock (this)
                    {
                        Keys.Add(_key);
                    }
                }
            }
            catch (Exception fail)
            {
                Parent.HandleException(fail, "infobot");
            }
            return true;
        }
    }
}

