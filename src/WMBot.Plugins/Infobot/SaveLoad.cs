//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or   
//  (at your option) version 3.                                         

//  This program is distributed in the hope that it will be useful,     
//  but WITHOUT ANY WARRANTY; without even the implied warranty of      
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the       
//  GNU General Public License for more details.                        

//  You should have received a copy of the GNU General Public License   
//  along with this program; if not, write to the                       
//  Free Software Foundation, Inc.,                                     
//  51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

using System;
using System.IO;
using System.Xml;

namespace wmib.Extensions
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

                XmlDocument data = new XmlDocument();
                XmlNode xmlnode = data.CreateElement("database");
                lock (this)
                {
                    foreach (InfobotAlias key in Aliases)
                    {
                        XmlAttribute name = data.CreateAttribute("alias_key_name");
                        name.Value = key.Name;
                        XmlAttribute kk = data.CreateAttribute("alias_key_key");
                        kk.Value = key.Key;
                        XmlAttribute created = data.CreateAttribute("date");
                        created.Value = "";
                        XmlNode db = data.CreateElement("alias");
                        db.Attributes.Append(name);
                        db.Attributes.Append(kk);
                        db.Attributes.Append(created);
                        xmlnode.AppendChild(db);
                    }

                    foreach (InfobotKey key in Keys)
                    {
                        XmlAttribute name = data.CreateAttribute("key_name");
                        name.Value = key.Key;
                        XmlAttribute kk = data.CreateAttribute("data");
                        kk.Value = key.Text;
                        XmlAttribute created = data.CreateAttribute("created_date");
                        created.Value = key.CreationTime.ToBinary().ToString();
                        XmlAttribute nick = data.CreateAttribute("nickname");
                        nick.Value = key.User;
                        XmlAttribute last = data.CreateAttribute("touched");
                        last.Value = key.LastTime.ToBinary().ToString();
                        XmlAttribute triggered = data.CreateAttribute("triggered");
                        triggered.Value = key.Displayed.ToString();
                        XmlAttribute k = data.CreateAttribute("raw");
                        k.Value = key.Raw.ToString();
                        XmlNode db = data.CreateElement("key");
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
                            Aliases.Add(new InfobotAlias(name.Replace("<separator>", "|"), value.Replace("<separator>", "|")));
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
                    Aliases.Clear();
                }
                foreach (XmlNode xx in data.ChildNodes[0].ChildNodes)
                {
                    if (xx.Name == "alias")
                    {
                        InfobotAlias _Alias = new InfobotAlias(xx.Attributes[0].Value, xx.Attributes[1].Value);
                        lock (this)
                        {
                            Aliases.Add(_Alias);
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

