// DotNetWikiBot Framework 2.101 - bot framework based on Microsoft .NET Framework 2.0 for wiki projects

// Distributed under the terms of the MIT (X11) license: http://www.opensource.org/licenses/mit-license.php

// Copyright (c) Iaroslav Vassiliev (2006-2012) codedriller@gmail.com



using System;

using System.IO;
using System.IO.Compression;
using System.Globalization;

using System.Threading;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;

using System.Net;

using System.Xml;
using System.Xml.XPath;
using System.Web;


namespace DotNetWikiBot
{
    /// <summary>Class defines wiki site object.</summary>

    [ClassInterface(ClassInterfaceType.AutoDispatch)]

    [Serializable]

    public class Site
    {

        /// <summary>Wiki site URL.</summary>

        public string site;

        /// <summary>User's account to login with.</summary>

        public string userName;

        /// <summary>User's password to login with.</summary>

        private string userPass;

        /// <summary>Default domain for LDAP authentication. Additional information can

        /// be found at http://www.mediawiki.org/wiki/Extension:LDAP_Authentication.</summary>

        public string userDomain = "";

        /// <summary>Site title.</summary>

        public string name;

        /// <summary>MediaWiki version as string.</summary>

        public string generator;

        /// <summary>MediaWiki version as number.</summary>

        public float version;

        /// <summary>MediaWiki version as Version object.</summary>

        public Version ver;

        /// <summary>Rule of page title capitalization.</summary>

        public string capitalization;

        /// <summary>Short relative path to wiki pages (if such alias is set on the server).

        /// See "http://www.mediawiki.org/wiki/Manual:Short URL" for details.</summary>

        public string wikiPath;		// = "/wiki/";

        /// <summary>Relative path to "index.php" file on server.</summary>

        public string indexPath;	// = "/w/";

        /// <summary>User's watchlist. Should be loaded manually with FillFromWatchList function,

        /// if it is necessary.</summary>

        public PageList watchList;

        /// <summary>MediaWiki interface messages. Should be loaded manually with

        /// GetMediaWikiMessagesEx function, if it is necessary.</summary>

        public PageList messages;

        /// <summary>Regular expression to find redirection target.</summary>

        public Regex redirectRE;

        /// <summary>Regular expression to find links to pages in list in HTML source.</summary>

        public static Regex linkToPageRE1 =

            new Regex("<li><a href=\"[^\"]*?\" (?:class=\"mw-redirect\" )?title=\"([^\"]+?)\">");

        /// <summary>Alternative regular expression to find links to pages in HTML source.</summary>

        public static Regex linkToPageRE2 =

            new Regex("<a href=\"[^\"]*?\" title=\"([^\"]+?)\">\\1</a>");

        /// <summary>Alternative regular expression to find links to pages (mostly image and file

        /// pages) in HTML source.</summary>

        public Regex linkToPageRE3;

        /// <summary>Regular expression to find links to subcategories in HTML source

        /// of category page on sites that use "CategoryTree" MediaWiki extension.</summary>

        public static Regex linkToSubCategoryRE =

            new Regex(">([^<]+)</a></div>\\s*<div class=\"CategoryTreeChildren\"");

        /// <summary>Regular expression to find links to image pages in galleries

        /// in HTML source.</summary>

        public static Regex linkToImageRE =

            new Regex("<div class=\"gallerytext\">\n<a href=\"[^\"]*?\" title=\"([^\"]+?)\">");

        /// <summary>Regular expression to find titles in markup.</summary>

        public static Regex pageTitleTagRE = new Regex("<title>(.+?)</title>");

        /// <summary>Regular expression to find internal wiki links in markup.</summary>

        public static Regex wikiLinkRE = new Regex(@"\[\[(.+?)(\|.+?)?]]");

        /// <summary>Regular expression to find wiki category links.</summary>

        public Regex wikiCategoryRE;

        /// <summary>Regular expression to find wiki templates in markup.</summary>

        public static Regex wikiTemplateRE = new Regex(@"(?s)\{\{(.+?)((\|.*?)*?)}}");

        /// <summary>Regular expression to find embedded images and files in wiki markup.</summary>

        public Regex wikiImageRE;

        /// <summary>Regular expression to find links to sister wiki projects in markup.</summary>

        public static Regex sisterWikiLinkRE;

        /// <summary>Regular expression to find interwiki links in wiki markup.</summary>

        public static Regex iwikiLinkRE;

        /// <summary>Regular expression to find displayed interwiki links in wiki markup,

        /// like "[[:de:...]]".</summary>

        public static Regex iwikiDispLinkRE;

        /// <summary>Regular expression to find external web links in wiki markup.</summary>

        public static Regex webLinkRE =

            new Regex("(https?|t?ftp|news|nntp|telnet|irc|gopher)://([^\\s'\"<>]+)");

        /// <summary>Regular expression to find sections of text, that are explicitly

        /// marked as non-wiki with special tag.</summary>

        public static Regex noWikiMarkupRE = new Regex("(?is)<nowiki>(.*?)</nowiki>");

        /// <summary>A template for disambiguation page. If some unusual template is used in your

        /// wiki for disambiguation, then it must be set in this variable. Use "|" as a delimiter

        /// when enumerating several templates here.</summary>

        public string disambigStr;

        /// <summary>Regular expression to extract language code from site URL.</summary>

        public static Regex siteLangRE = new Regex(@"https?://(.*?)\.(.+?\..+)");

        /// <summary>Regular expression to extract edit session time attribute.</summary>

        public static Regex editSessionTimeRE1 =

            new Regex("value=\"([^\"]*?)\" name=['\"]wpEdittime['\"]");

        /// <summary>Regular expression to extract edit session time attribute.</summary>

        public static Regex editSessionTimeRE3 = new Regex(" touched=\"(.+?)\"");

        /// <summary>Regular expression to extract edit session token attribute.</summary>

        public static Regex editSessionTokenRE1 =

            new Regex("value=\"([^\"]*?)\" name=['\"]wpEditToken['\"]");

        /// <summary>Regular expression to extract edit session token attribute.</summary>

        public static Regex editSessionTokenRE2 =

            new Regex("name=['\"]wpEditToken['\"](?: type=\"hidden\")? value=\"([^\"]*?)\"");

        /// <summary>Regular expression to extract edit session token attribute.</summary>

        public static Regex editSessionTokenRE3 = new Regex(" edittoken=\"(.+?)\"");

        /// <summary>Site cookies.</summary>

        public CookieContainer cookies = new CookieContainer();

        /// <summary>XML name table for parsing XHTML documents from wiki site.</summary>

        public NameTable xhtmlNameTable = new NameTable();

        /// <summary>XML namespace URI of wiki site's XHTML version.</summary>

        public string xhtmlNSUri = "http://www.w3.org/1999/xhtml";

        /// <summary>XML namespace manager for parsing XHTML documents from wiki site.</summary>

        public XmlNamespaceManager xmlNS;

        /// <summary>Local namespaces.</summary>

        public Hashtable namespaces = new Hashtable();

        /// <summary>Default namespaces.</summary>

        public static Hashtable wikiNSpaces = new Hashtable();

        /// <summary>List of Wikimedia Foundation sites and according prefixes.</summary>

        public static Hashtable WMSites = new Hashtable();

        /// <summary>Built-in variables of MediaWiki software, used in brackets {{...}}.

        /// To be distinguished from templates.

        /// (see http://meta.wikimedia.org/wiki/Help:Magic_words).</summary>

        public static string[] mediaWikiVars;

        /// <summary>Built-in parser functions (and similar prefixes) of MediaWiki software, used

        /// like first ... in {{...:...}}. To be distinguished from templates.

        /// (see http://meta.wikimedia.org/wiki/Help:Magic_words).</summary>

        public static string[] parserFunctions;

        /// <summary>Built-in template modifiers of MediaWiki software

        /// (see http://meta.wikimedia.org/wiki/Help:Magic_words).</summary>

        public static string[] templateModifiers;

        /// <summary>Interwiki links sorting order, based on local language by first word.

        /// See http://meta.wikimedia.org/wiki/Interwiki_sorting_order for details.</summary>

        public static string[] iwikiLinksOrderByLocalFW;

        /// <summary>Interwiki links sorting order, based on local language.

        /// See http://meta.wikimedia.org/wiki/Interwiki_sorting_order for details.</summary>

        public static string[] iwikiLinksOrderByLocal;

        /// <summary>Interwiki links sorting order, based on latin alphabet by first word.

        /// See http://meta.wikimedia.org/wiki/Interwiki_sorting_order for details.</summary>

        public static string[] iwikiLinksOrderByLatinFW;

        /// <summary>Wikimedia Foundation sites and prefixes in one regex-escaped string

        /// with "|" as separator.</summary>

        public static string WMSitesStr;

        /// <summary>ISO 639-1 language codes, used as prefixes to identify Wikimedia

        /// Foundation sites, gathered in one regex-escaped string with "|" as separator.</summary>

        public static string WMLangsStr;

        /// <summary>Availability of "api.php" MediaWiki extension (bot interface).</summary>

        public bool botQuery;

        /// <summary>Versions of "api.php" MediaWiki extension (bot interface) modules.</summary>

        public Hashtable botQueryVersions = new Hashtable();

        /// <summary>Set of lists of pages, produced by bot interface.</summary>

        public static Hashtable botQueryLists = new Hashtable();

        /// <summary>Set of lists of parsed data, produced by bot interface.</summary>

        public static Hashtable botQueryProps = new Hashtable();

        /// <summary>Site language.</summary>

        public string language;

        /// <summary>Site language text direction.</summary>

        public string langDirection;

        /// <summary>Site's neutral (language) culture.</summary>

        public CultureInfo langCulture;

        /// <summary>Randomly chosen regional (non-neutral) culture for site's language.</summary>

        public CultureInfo regCulture;

        /// <summary>Site encoding.</summary>

        public Encoding encoding = Encoding.UTF8;

        /// <summary>This constructor is used to generate most Site objects.</summary>

        /// <param name="site">Wiki site's URI. It must point to the main page of the wiki, e.g.

        /// "http://en.wikipedia.org" or "http://127.0.0.1:80/w/index.php?title=Main_page".</param>

        /// <param name="userName">User name to log in.</param>

        /// <param name="userPass">Password.</param>

        /// <returns>Returns Site object.</returns>

        public Site(string site, string userName, string userPass)

            : this(site, userName, userPass, "") { }

        /// <summary>This constructor is used for LDAP authentication. Additional information can

        /// be found at "http://www.mediawiki.org/wiki/Extension:LDAP_Authentication".</summary>

        /// <param name="site">Wiki site's URI. It must point to the main page of the wiki, e.g.

        /// "http://en.wikipedia.org" or "http://127.0.0.1:80/w/index.php?title=Main_page".</param>

        /// <param name="userName">User name to log in.</param>

        /// <param name="userPass">Password.</param>

        /// <param name="userDomain">Domain for LDAP authentication.</param>

        /// <returns>Returns Site object.</returns>

        public Site(string site, string userName, string userPass, string userDomain)
        {
            this.site = site;
            this.userName = userName;
            this.userPass = userPass;
            this.userDomain = userDomain;

            Initialize();
        }

        /// <summary>This constructor uses default site, userName and password. The site URL and

        /// account data can be stored in UTF8-encoded "Defaults.dat" file in bot's "Cache"

        /// subdirectory.</summary>

        /// <returns>Returns Site object.</returns>

        public Site()
        {

            if (File.Exists("Cache" + Path.DirectorySeparatorChar + "Defaults.dat") == true)
            {

                string[] lines = File.ReadAllLines(

                    "Cache" + Path.DirectorySeparatorChar + "Defaults.dat", Encoding.UTF8);

                if (lines.GetUpperBound(0) >= 2)
                {

                    this.site = lines[0];

                    this.userName = lines[1];

                    this.userPass = lines[2];

                    if (lines.GetUpperBound(0) >= 3)

                        this.userDomain = lines[3];

                    else

                        this.userDomain = "";

                }

                else

                    throw new WikiBotException(

                        Bot.Msg("\"\\Cache\\Defaults.dat\" file is invalid."));

            }

            else

                throw new WikiBotException(Bot.Msg("\"\\Cache\\Defaults.dat\" file not found."));

            Initialize();

        }

        /// <summary>This internal function establishes connection to site and loads general site

        /// info by the use of other functions. Function is called from the constructors.</summary>

        public void Initialize()
        {

            xmlNS = new XmlNamespaceManager(xhtmlNameTable);

            if (site.Contains("sourceforge"))
            {

                site = site.Replace("https://", "http://");
                GetPaths();
                xmlNS.AddNamespace("ns", xhtmlNSUri);
                LoadDefaults();
                LogInSourceForge();
                site = site.Replace("http://", "https://");

            }
            else if (site.Contains("wikia.com"))
            {
                GetPaths();
                xmlNS.AddNamespace("ns", xhtmlNSUri);
                LoadDefaults();
                LogInViaApi();
            }
            else
            {
                GetPaths();
                xmlNS.AddNamespace("ns", xhtmlNSUri);
                LoadDefaults();
                LogIn();
            }

            GetInfo();

            if (!Bot.isRunningOnMono)
                Bot.DisableCanonicalizingUriAsFilePath();	// .NET bug evasion

        }

        /// <summary>Gets path to "index.php", short path to pages (if present), and then
        /// saves paths to file.</summary>

        public void GetPaths()
        {
            ServicePointManager.ServerCertificateValidationCallback = delegate
            { return true; }; // WARNING: accepts all SSL certificates
            if (!site.StartsWith("http"))
                site = "http://" + site;
            if (Bot.CountMatches(site, "/", false) == 3 && site.EndsWith("/"))
                site = site.Substring(0, site.Length - 1);
            string filePathName = "Cache" + Path.DirectorySeparatorChar +

                HttpUtility.UrlEncode(site.Replace("://", ".").Replace("/", ".")) + ".dat";

            if (File.Exists(filePathName) == true)
            {

                string[] lines = File.ReadAllLines(filePathName, Encoding.UTF8);

                if (lines.GetUpperBound(0) >= 4)
                {

                    wikiPath = lines[0];

                    indexPath = lines[1];

                    xhtmlNSUri = lines[2];

                    language = lines[3];

                    langDirection = lines[4];

                    if (lines.GetUpperBound(0) >= 5)

                        site = lines[5];

                    return;

                }

            }

            Console.WriteLine(Bot.Msg("Logging in..."));
            HttpWebRequest webReq = (HttpWebRequest)WebRequest.Create(site);
            webReq.Proxy.Credentials = CredentialCache.DefaultCredentials;
            webReq.UseDefaultCredentials = true;
            webReq.ContentType = Bot.webContentType;
            webReq.UserAgent = Bot.botVer;

            if (Bot.unsafeHttpHeaderParsingUsed == 0)
            {

                webReq.ProtocolVersion = HttpVersion.Version10;

                webReq.KeepAlive = false;

            }

            HttpWebResponse webResp = null;

            for (int errorCounter = 0; true; errorCounter++)
            {

                try
                {

                    webResp = (HttpWebResponse)webReq.GetResponse();

                    break;

                }

                catch (WebException e)
                {

                    string message = e.Message;

                    if (Regex.IsMatch(message, ": \\(50[02349]\\) "))
                    {		// Remote problem

                        if (errorCounter > Bot.retryTimes)

                            throw;

                        Thread.Sleep(60000);

                    }

                    else if (message.Contains("Section=ResponseStatusLine"))
                    {	// Squid problem

                        Bot.SwitchUnsafeHttpHeaderParsing(true);

                        GetPaths();

                        return;

                    }

                    else
                    {

                        throw;

                    }

                }

            }

            site = webResp.ResponseUri.Scheme + "://" + webResp.ResponseUri.Authority;

            Regex wikiPathRE = new Regex("(?i)" + Regex.Escape(site) + "(/.+?/).+");

            Regex indexPathRE1 = new Regex("(?i)" + Regex.Escape(site) +

                "(/.+?/)index\\.php(\\?|/)");

            Regex indexPathRE2 = new Regex("(?i)href=\"(/[^\"\\s<>?]*?)index\\.php(\\?|/)");

            Regex indexPathRE3 = new Regex("(?i)wgScript=\"(/[^\"\\s<>?]*?)index\\.php");

            Regex xhtmlNSUriRE = new Regex("(?i)<html[^>]*( xmlns=\"(?'xmlns'[^\"]+)\")[^>]*>");

            Regex languageRE = new Regex("(?i)<html[^>]*( lang=\"(?'lang'[^\"]+)\")[^>]*>");

            Regex langDirectionRE = new Regex("(?i)<html[^>]*( dir=\"(?'dir'[^\"]+)\")[^>]*>");

            string mainPageUri = webResp.ResponseUri.ToString();

            if (mainPageUri.Contains("/index.php?"))

                indexPath = indexPathRE1.Match(mainPageUri).Groups[1].ToString();

            else

                wikiPath = wikiPathRE.Match(mainPageUri).Groups[1].ToString();

            if (string.IsNullOrEmpty(indexPath) && string.IsNullOrEmpty(wikiPath) &&

                mainPageUri[mainPageUri.Length - 1] != '/' &&

                Bot.CountMatches(mainPageUri, "/", false) == 3)

                wikiPath = "/";

            Stream respStream = webResp.GetResponseStream();

            if (webResp.ContentEncoding.ToLower().Contains("gzip"))

                respStream = new GZipStream(respStream, CompressionMode.Decompress);

            else if (webResp.ContentEncoding.ToLower().Contains("deflate"))

                respStream = new DeflateStream(respStream, CompressionMode.Decompress);

            StreamReader strmReader = new StreamReader(respStream, encoding);

            string src = strmReader.ReadToEnd();

            webResp.Close();

            if (!site.Contains("wikia.com"))

                indexPath = indexPathRE2.Match(src).Groups[1].ToString();

            else

                indexPath = indexPathRE3.Match(src).Groups[1].ToString();

            xhtmlNSUri = xhtmlNSUriRE.Match(src).Groups["xmlns"].ToString();

            if (string.IsNullOrEmpty(xhtmlNSUri))

                xhtmlNSUri = "http://www.w3.org/1999/xhtml";

            language = languageRE.Match(src).Groups["lang"].ToString();

            langDirection = langDirectionRE.Match(src).Groups["dir"].ToString();

            if (!Directory.Exists("Cache"))

                Directory.CreateDirectory("Cache");

            File.WriteAllText(filePathName, wikiPath + "\r\n" + indexPath + "\r\n" + xhtmlNSUri +

                "\r\n" + language + "\r\n" + langDirection + "\r\n" + site, Encoding.UTF8);

        }

        /// <summary>Gets a specified MediaWiki message.</summary>

        /// <param name="title">Title of the message.</param>

        /// <returns>Returns raw text of the message.

        /// If the message doesn't exist, exception is thrown.</returns>

        public string GetMediaWikiMessage(string title)
        {

            if (string.IsNullOrEmpty(title))

                throw new ArgumentNullException("title");

            title = namespaces["8"].ToString() + ":" + Bot.Capitalize(RemoveNSPrefix(title, 8));

            if (messages == null)

                messages = new PageList(this);

            else if (messages.Contains(title))

                return messages[title].text;

            string src;

            try
            {

                src = GetPageHTM(site + indexPath + "index.php?title=" +

                    HttpUtility.UrlEncode(title.Replace(" ", "_")) +

                    "&action=raw&usemsgcache=1&dontcountme=s");

            }

            catch (WebException e)
            {

                if (e.Message.Contains(": (404) "))

                    throw new WikiBotException(

                        string.Format(Bot.Msg("MediaWiki message \"{0}\" was not found."), title));

                else

                    throw;

            }

            if (string.IsNullOrEmpty(src))
            {

                throw new WikiBotException(

                    string.Format(Bot.Msg("MediaWiki message \"{0}\" was not found."), title));

            }

            messages.Add(new Page(this, title));

            messages[messages.Count() - 1].text = src;

            return src;

        }

        /// <summary>Gets all modified MediaWiki messages (to be more precise, all messages that are

        /// contained in database), loads them into site.messages PageList (both titles and texts)

        /// and dumps them to XML file.</summary>

        /// <param name="forceLoad">If true, the messages are updated unconditionally. Otherwise

        /// the messages are updated only if they are outdated.</param>

        public void GetModifiedMediaWikiMessages(bool forceLoad)
        {

            if (messages == null)

                messages = new PageList(this);

            string filePathName = "Cache" + Path.DirectorySeparatorChar +

                HttpUtility.UrlEncode(site.Replace("://", ".")) + ".mw_db_msg.xml";

            if (forceLoad == false && File.Exists(filePathName) &&

                (DateTime.Now - File.GetLastWriteTime(filePathName)).Days <= 90)
            {

                messages.FillAndLoadFromXMLDump(filePathName);

                return;

            }


            PageList pl = new PageList(this);

            bool prevBotQueryState = botQuery;

            botQuery = false;	// backward compatibility requirement

            pl.FillFromAllPages("!", 8, false, 100000);

            botQuery = prevBotQueryState;

            File.Delete(filePathName);

            pl.SaveXMLDumpToFile(filePathName);

            messages.FillAndLoadFromXMLDump(filePathName);

            Console.WriteLine(Bot.Msg("MediaWiki messages dump updated successfully."));

        }

        /// <summary>Gets all MediaWiki messages from "Special:Allmessages" page and loads them into

        /// site.messages PageList. The function is not backward compatible.</summary>

        public void GetMediaWikiMessages()
        {

            if (messages == null)

                messages = new PageList(this);

            Console.WriteLine(Bot.Msg("Updating MediaWiki messages dump. Please, wait..."));

            string res = site + indexPath + "index.php?title=Special:Allmessages";

            string src = "";

            Page p = null;

            Regex nextPortionRE = new Regex("offset=([^\"]+)\" title=\"[^\"]+\" rel=\"next\"");

            do
            {

                src = GetPageHTM(res + (src != ""

                    ? "&offset=" + HttpUtility.HtmlDecode(nextPortionRE.Match(src).Groups[1].Value)

                    : "&limit=5000"));

                using (XmlReader reader = GetXMLReader(src))
                {

                    reader.ReadToFollowing("tbody");

                    while (reader.Read())
                    {

                        if (reader.Name == "tr" && reader.NodeType == XmlNodeType.Element &&

                            reader["id"] != null)

                            p = new Page(this, namespaces["8"].ToString() + ":" +

                                Bot.Capitalize(reader["id"].Replace("msg_", "")));

                        else if (reader.Name == "td" &&

                            (reader["class"] == "am_default" || reader["class"] == "am_actual"))

                            p.text = reader.ReadString();

                        else if (reader.Name == "tr" && reader.NodeType == XmlNodeType.EndElement)

                            messages.Add(p);

                        else if (reader.Name == "tbody" &&

                            reader.NodeType == XmlNodeType.EndElement)

                            break;

                    }

                }

            } while (nextPortionRE.IsMatch(src));

            if (p != null)

                messages.Add(p);

            Console.WriteLine(Bot.Msg("MediaWiki messages dump updated successfully."));

        }

        /// <summary>Retrieves metadata and local namespace names from site.</summary>

        public void GetInfo()
        {

            try
            {

                langCulture = new CultureInfo(language, false);

            }

            catch (Exception)
            {

                langCulture = new CultureInfo("");

            }

            if (langCulture.Equals(CultureInfo.CurrentUICulture.Parent))

                regCulture = CultureInfo.CurrentUICulture;

            else
            {

                try
                {

                    regCulture = CultureInfo.CreateSpecificCulture(language);

                }

                catch (Exception)
                {

                    foreach (CultureInfo ci in

                        CultureInfo.GetCultures(CultureTypes.SpecificCultures))
                    {

                        if (langCulture.Equals(ci.Parent))
                        {

                            regCulture = ci;

                            break;

                        }

                    }

                    if (regCulture == null)

                        regCulture = CultureInfo.InvariantCulture;

                }

            }

            string src = GetPageHTM(site + indexPath + "index.php?title=Special:Export/" +

                DateTime.Now.Ticks.ToString("x"));

            XmlTextReader reader = new XmlTextReader(new StringReader(src));

            reader.WhitespaceHandling = WhitespaceHandling.None;

            reader.ReadToFollowing("sitename");

            name = reader.ReadString();

            reader.ReadToFollowing("generator");

            generator = reader.ReadString();

            ver = new Version(Regex.Replace(generator, @"[^\d\.]", ""));

            float.TryParse(ver.ToString(), NumberStyles.AllowDecimalPoint,

                new CultureInfo("en-US"), out version);

            reader.ReadToFollowing("case");

            capitalization = reader.ReadString();

            namespaces.Clear();

            while (reader.ReadToFollowing("namespace"))

                namespaces.Add(reader.GetAttribute("key"),

                    HttpUtility.HtmlDecode(reader.ReadString()));

            reader.Close();

            namespaces.Remove("0");

            foreach (DictionaryEntry ns in namespaces)
            {

                if (!wikiNSpaces.ContainsKey(ns.Key) ||

                    ns.Key.ToString() == "4" || ns.Key.ToString() == "5")

                    wikiNSpaces[ns.Key] = ns.Value;

            }

            if (ver >= new Version(1, 14))
            {

                wikiNSpaces["6"] = "File";

                wikiNSpaces["7"] = "File talk";

            }

            wikiCategoryRE = new Regex(@"\[\[(?i)(((" + Regex.Escape(wikiNSpaces["14"].ToString()) +

                "|" + Regex.Escape(namespaces["14"].ToString()) + @"):(.+?))(\|(.+?))?)]]");

            wikiImageRE = new Regex(@"\[\[(?i)((File|Image" +

                "|" + Regex.Escape(namespaces["6"].ToString()) + @"):(.+?))(\|(.+?))*?]]");

            string namespacesStr = "";

            foreach (DictionaryEntry ns in namespaces)

                namespacesStr += Regex.Escape(ns.Value.ToString()) + "|";

            namespacesStr = namespacesStr.Replace("||", "|").Trim("|".ToCharArray());

            linkToPageRE3 = new Regex("<a href=\"[^\"]*?\" title=\"(" +

                Regex.Escape(namespaces["6"].ToString()) + ":[^\"]+?)\">");

            string redirectTag = "REDIRECT";

            switch (language)
            {		// Revised 2010-07-02 (MediaWiki 1.15.4)

                case "af": redirectTag += "|aanstuur"; break;

                case "ar": redirectTag += "|تحويل"; break;

                case "arz": redirectTag += "|تحويل|تحويل#"; break;

                case "be": redirectTag += "|перанакіраваньне"; break;

                case "be-x-old": redirectTag += "|перанакіраваньне"; break;

                case "bg": redirectTag += "|пренасочване|виж"; break;

                case "br": redirectTag += "|adkas"; break;

                case "bs": redirectTag += "|preusmjeri"; break;

                case "cs": redirectTag += "|přesměruj"; break;

                case "cu": redirectTag += "|прѣнаправлєниѥ"; break;

                case "cy": redirectTag += "|ail-cyfeirio|ailgyfeirio"; break;

                case "de": redirectTag += "|weiterleitung"; break;

                case "el": redirectTag += "|ανακατευθυνση"; break;

                case "eo": redirectTag += "|alidirektu"; break;

                case "es": redirectTag += "|redireccíon"; break;

                case "et": redirectTag += "|suuna"; break;

                case "eu": redirectTag += "|birzuzendu"; break;

                case "fa": redirectTag += "|تغییرمسیر"; break;

                case "fi": redirectTag += "|uudelleenohjaus|ohjaus"; break;

                case "fr": redirectTag += "|redirection"; break;

                case "ga": redirectTag += "|athsheoladh"; break;

                case "gl": redirectTag += "|redirección"; break;

                case "he": redirectTag += "|הפניה"; break;

                case "hr": redirectTag += "|preusmjeri"; break;

                case "hu": redirectTag += "|átirányítás"; break;

                case "hy": redirectTag += "|վերահղում"; break;

                case "id": redirectTag += "|alih"; break;

                case "is": redirectTag += "|tilvísun"; break;

                case "it": redirectTag += "|redirezione"; break;

                case "ja": redirectTag += "|転送|リダイレクト|転送|リダイレクト"; break;

                case "ka": redirectTag += "|გადამისამართება"; break;

                case "kk": redirectTag += "|ايداۋ|айдау|aýdaw"; break;

                case "km": redirectTag += "|បញ្ជូនបន្ត|ប្ដូរទីតាំងទៅ #ប្តូរទីតាំងទៅ"

                    + "|ប្ដូរទីតាំង|ប្តូរទីតាំង|ប្ដូរចំណងជើង"; break;

                case "ko": redirectTag += "|넘겨주기"; break;

                case "ksh": redirectTag += "|ömleidung"; break;

                case "lt": redirectTag += "|peradresavimas"; break;

                case "mk": redirectTag += "|пренасочување|види"; break;

                case "ml": redirectTag += "|аґ¤аґїаґ°аґїаґљаµЌаґљаµЃаґµаґїаґџаµЃаґ•" +

                    "|аґ¤аґїаґ°аґїаґљаµЌаґљаµЃаґµаґїаґџаґІаµЌвЂЌ"; break;

                case "mr": redirectTag += "|а¤ЄаҐЃа¤Ёа¤°аҐЌа¤Ёа¤їа¤°аҐЌа¤¦аҐ‡а¤¶а¤Ё"; break;

                case "mt": redirectTag += "|rindirizza"; break;

                case "mwl": redirectTag += "|ancaminar"; break;

                case "nds": redirectTag += "|wiederleiden"; break;

                case "nds-nl": redirectTag += "|deurverwiezing|doorverwijzing"; break;

                case "nl": redirectTag += "|doorverwijzing"; break;

                case "nn": redirectTag += "|omdiriger"; break;

                case "oc": redirectTag += "|redireccion"; break;

                case "pl": redirectTag += "|patrz|przekieruj|tam"; break;

                case "pt": redirectTag += "|redirecionamento"; break;

                case "ro": redirectTag += "|redirecteaza"; break;

                case "ru": redirectTag += "|перенаправление|перенапр"; break;

                case "sa": redirectTag += "|а¤ЄаҐЃа¤Ёа¤°аҐЌа¤Ёа¤їа¤¦аҐ‡а¤¶а¤Ё"; break;

                case "sd": redirectTag += "|چوريو"; break;

                case "si": redirectTag += "|а¶єа·…а·’а¶єа·ња¶ёа·”а·Ђ"; break;

                case "sk": redirectTag += "|presmeruj"; break;

                case "sl": redirectTag += "|preusmeritev"; break;

                case "sq": redirectTag += "|ridrejto"; break;

                case "sr": redirectTag += "|преусмери|preusmeri"; break;

                case "srn": redirectTag += "|doorverwijzing"; break;

                case "sv": redirectTag += "|omdirigering"; break;

                case "ta": redirectTag += "|а®µа®ґа®їа®®а®ѕа®±аЇЌа®±аЇЃ"; break;

                case "te": redirectTag += "|а°¦а°ѕа°°а°їа°®а°ѕа°°а±Ќа°Єа±Ѓ"; break;

                case "tr": redirectTag += "|yönlendİrme"; break;

                case "tt": redirectTag += "перенаправление|перенапр|yünältü"; break;

                case "uk": redirectTag += "|перенаправлення|перенаправление|перенапр"; break;

                case "vi": redirectTag += "|đổi|đổi"; break;

                case "vro": redirectTag += "|saadaq|suuna"; break;

                case "yi": redirectTag += "|ווייטערפירן|#הפניה"; break;

                default: redirectTag = "REDIRECT"; break;

            }

            redirectRE = new Regex(@"(?i)^#(?:" + redirectTag + @")\s*:?\s*\[\[(.+?)(\|.+)?]]",

                RegexOptions.Compiled);

            string botQueryUriStr = site + indexPath + "api.php?version";

            string respStr;

            try
            {

                respStr = GetPageHTM(botQueryUriStr);

                if (respStr.Contains("<title>MediaWiki API</title>"))
                {

                    botQuery = true;

                    Regex botQueryVersionsRE = new Regex(@"(?i)<b><i>\$" +

                        @"Id: (\S+) (\d+) (.+?) \$</i></b>");

                    foreach (Match m in botQueryVersionsRE.Matches(respStr))

                        botQueryVersions[m.Groups[1].ToString()] = m.Groups[2].ToString();

                    if (!botQueryVersions.ContainsKey("ApiMain.php") && ver > new Version(1, 17))
                    {

                        // if versioning system is broken

                        botQueryVersions["ApiQueryCategoryMembers.php"] = "104449";

                        botQueryVersions["ApiQueryRevisions.php"] = "104449";

                    }

                }

            }

            catch (WebException)
            {

                botQuery = false;

            }

            if ((botQuery == false || !botQueryVersions.ContainsKey("ApiQueryCategoryMembers.php"))

                && ver < new Version(1, 16))
            {

                botQueryUriStr = site + indexPath + "query.php";

                try
                {

                    respStr = GetPageHTM(botQueryUriStr);

                    if (respStr.Contains("<title>MediaWiki Query Interface</title>"))
                    {

                        botQuery = true;

                        botQueryVersions["query.php"] = "Unknown";

                    }

                }

                catch (WebException)
                {

                    return;

                }

            }

        }

        /// <summary>Loads default English namespace names for site.</summary>

        public void LoadDefaults()
        {

            if (wikiNSpaces.Count != 0 && WMSites.Count != 0)

                return;

            string[] wikiNSNames = { "Media", "Special", "", "Talk", "User", "User talk", name,
				name + " talk", "Image", "Image talk", "MediaWiki", "MediaWiki talk", "Template",
				"Template talk", "Help", "Help talk", "Category", "Category talk" };

            for (int i = -2, j = 0; i < 16; i++, j++)

                wikiNSpaces.Add(i.ToString(), wikiNSNames[j]);

            wikiNSpaces.Remove("0");

            WMSites.Add("w", "wikipedia"); WMSites.Add("wikt", "wiktionary");

            WMSites.Add("b", "wikibooks"); WMSites.Add("n", "wikinews");

            WMSites.Add("q", "wikiquote"); WMSites.Add("s", "wikisource");

            foreach (DictionaryEntry s in WMSites)

                WMSitesStr += s.Key + "|" + s.Value + "|";

            // Revised 2010-07-02

            mediaWikiVars = new string[] { "currentmonth","currentmonthname","currentmonthnamegen",
				"currentmonthabbrev","currentday2","currentdayname","currentyear","currenttime",
				"currenthour","localmonth","localmonthname","localmonthnamegen","localmonthabbrev",
				"localday","localday2","localdayname","localyear","localtime","localhour",
				"numberofarticles","numberoffiles","sitename","server","servername","scriptpath",
				"pagename","pagenamee","fullpagename","fullpagenamee","namespace","namespacee",
				"currentweek","currentdow","localweek","localdow","revisionid","revisionday",
				"revisionday2","revisionmonth","revisionyear","revisiontimestamp","subpagename",
				"subpagenamee","talkspace","talkspacee","subjectspace","dirmark","directionmark",
				"subjectspacee","talkpagename","talkpagenamee","subjectpagename","subjectpagenamee",
				"numberofusers","rawsuffix","newsectionlink","numberofpages","currentversion",
				"basepagename","basepagenamee","urlencode","currenttimestamp","localtimestamp",
				"directionmark","language","contentlanguage","pagesinnamespace","numberofadmins",
				"currentday","numberofarticles:r","numberofpages:r","magicnumber",
				"numberoffiles:r", "numberofusers:r", "numberofadmins:r", "numberofactiveusers",
				"numberofactiveusers:r" };

            parserFunctions = new string[] { "ns:", "localurl:", "localurle:", "urlencode:",
				"anchorencode:", "fullurl:", "fullurle:",  "grammar:", "plural:", "lc:", "lcfirst:",
				"uc:", "ucfirst:", "formatnum:", "padleft:", "padright:", "#language:",
				"displaytitle:", "defaultsort:", "#if:", "#if:", "#switch:", "#ifexpr:",
				"numberingroup:", "pagesinns:", "pagesincat:", "pagesincategory:", "pagesize:",
				"gender:", "filepath:", "#special:", "#tag:" };

            templateModifiers = new string[] { ":", "int:", "msg:", "msgnw:", "raw:", "subst:" };

            // Revised 2010-07-02

            iwikiLinksOrderByLocalFW = new string[] {
				"ace", "af", "ak", "als", "am", "ang", "ab", "ar", "an", "arc",
				"roa-rup", "frp", "as", "ast", "gn", "av", "ay", "az", "id", "ms",
				"bm", "bn", "zh-min-nan", "nan", "map-bms", "jv", "su", "ba", "be",
				"be-x-old", "bh", "bcl", "bi", "bar", "bo", "bs", "br", "bug", "bg",
				"bxr", "ca", "ceb", "cv", "cs", "ch", "cbk-zam", "ny", "sn", "tum",
				"cho", "co", "cy", "da", "dk", "pdc", "de", "dv", "nv", "dsb", "na",
				"dz", "mh", "et", "el", "eml", "en", "myv", "es", "eo", "ext", "eu",
				"ee", "fa", "hif", "fo", "fr", "fy", "ff", "fur", "ga", "gv", "sm",
				"gd", "gl", "gan", "ki", "glk", "gu", "got", "hak", "xal", "ko",
				"ha", "haw", "hy", "hi", "ho", "hsb", "hr", "io", "ig", "ilo",
				"bpy", "ia", "ie", "iu", "ik", "os", "xh", "zu", "is", "it", "he",
				"kl", "kn", "kr", "pam", "ka", "ks", "csb", "kk", "kw", "rw", "ky",
				"rn", "sw", "kv", "kg", "ht", "ku", "kj", "lad", "lbe", "lo", "la",
				"lv", "to", "lb", "lt", "lij", "li", "ln", "jbo", "lg", "lmo", "hu",
				"mk", "mg", "ml", "krc", "mt", "mi", "mr", "arz", "mzn", "cdo",
				"mwl", "mdf", "mo", "mn", "mus", "my", "nah", "fj", "nl", "nds-nl",
				"cr", "ne", "new", "ja", "nap", "ce", "pih", "no", "nb", "nn",
				"nrm", "nov", "ii", "oc", "mhr", "or", "om", "ng", "hz", "uz", "pa",
				"pi", "pag", "pnb", "pap", "ps", "km", "pcd", "pms", "nds", "pl",
				"pnt", "pt", "aa", "kaa", "crh", "ty", "ksh", "ro", "rmy", "rm",
				"qu", "ru", "sah", "se", "sa", "sg", "sc", "sco", "stq", "st", "tn",
				"sq", "scn", "si", "simple", "sd", "ss", "sk", "sl", "cu", "szl",
				"so", "ckb", "srn", "sr", "sh", "fi", "sv", "tl", "ta", "kab",
				"roa-tara", "tt", "te", "tet", "th", "vi", "ti", "tg", "tpi",
				"tokipona", "tp", "chr", "chy", "ve", "tr", "tk", "tw", "udm", "uk",
				"ur", "ug", "za", "vec", "vo", "fiu-vro", "wa", "zh-classical",
				"vls", "war", "wo", "wuu", "ts", "yi", "yo", "zh-yue", "diq", "zea",
				"bat-smg", "zh", "zh-tw", "zh-cn"
			};

            iwikiLinksOrderByLocal = new string[] {
				"ace", "af", "ak", "als", "am", "ang", "ab", "ar", "an", "arc",
				"roa-rup", "frp", "as", "ast", "gn", "av", "ay", "az", "bm", "bn",
				"zh-min-nan", "nan", "map-bms", "ba", "be", "be-x-old", "bh", "bcl",
				"bi", "bar", "bo", "bs", "br", "bg", "bxr", "ca", "cv", "ceb", "cs",
				"ch", "cbk-zam", "ny", "sn", "tum", "cho", "co", "cy", "da", "dk",
				"pdc", "de", "dv", "nv", "dsb", "dz", "mh", "et", "el", "eml", "en",
				"myv", "es", "eo", "ext", "eu", "ee", "fa", "hif", "fo", "fr", "fy",
				"ff", "fur", "ga", "gv", "gd", "gl", "gan", "ki", "glk", "gu",
				"got", "hak", "xal", "ko", "ha", "haw", "hy", "hi", "ho", "hsb",
				"hr", "io", "ig", "ilo", "bpy", "id", "ia", "ie", "iu", "ik", "os",
				"xh", "zu", "is", "it", "he", "jv", "kl", "kn", "kr", "pam", "krc",
				"ka", "ks", "csb", "kk", "kw", "rw", "ky", "rn", "sw", "kv", "kg",
				"ht", "ku", "kj", "lad", "lbe", "lo", "la", "lv", "lb", "lt", "lij",
				"li", "ln", "jbo", "lg", "lmo", "hu", "mk", "mg", "ml", "mt", "mi",
				"mr", "arz", "mzn", "ms", "cdo", "mwl", "mdf", "mo", "mn", "mus",
				"my", "nah", "na", "fj", "nl", "nds-nl", "cr", "ne", "new", "ja",
				"nap", "ce", "pih", "no", "nb", "nn", "nrm", "nov", "ii", "oc",
				"mhr", "or", "om", "ng", "hz", "uz", "pa", "pi", "pag", "pnb",
				"pap", "ps", "km", "pcd", "pms", "tpi", "nds", "pl", "tokipona",
				"tp", "pnt", "pt", "aa", "kaa", "crh", "ty", "ksh", "ro", "rmy",
				"rm", "qu", "ru", "sah", "se", "sm", "sa", "sg", "sc", "sco", "stq",
				"st", "tn", "sq", "scn", "si", "simple", "sd", "ss", "sk", "cu",
				"sl", "szl", "so", "ckb", "srn", "sr", "sh", "su", "fi", "sv", "tl",
				"ta", "kab", "roa-tara", "tt", "te", "tet", "th", "ti", "tg", "to",
				"chr", "chy", "ve", "tr", "tk", "tw", "udm", "bug", "uk", "ur",
				"ug", "za", "vec", "vi", "vo", "fiu-vro", "wa", "zh-classical",
				"vls", "war", "wo", "wuu", "ts", "yi", "yo", "zh-yue", "diq", "zea",
				"bat-smg", "zh", "zh-tw", "zh-cn"
			};

            iwikiLinksOrderByLatinFW = new string[] {
				"ace", "af", "ak", "als", "am", "ang", "ab", "ar", "an", "arc",
				"roa-rup", "frp", "arz", "as", "ast", "gn", "av", "ay", "az", "id",
				"ms", "bg", "bm", "zh-min-nan", "nan", "map-bms", "jv", "su", "ba",
				"be", "be-x-old", "bh", "bcl", "bi", "bn", "bo", "bar", "bs", "bpy",
				"br", "bug", "bxr", "ca", "ceb", "ch", "cbk-zam", "sn", "tum", "ny",
				"cho", "chr", "co", "cy", "cv", "cs", "da", "dk", "pdc", "de", "nv",
				"dsb", "na", "dv", "dz", "mh", "et", "el", "eml", "en", "myv", "es",
				"eo", "ext", "eu", "ee", "fa", "hif", "fo", "fr", "fy", "ff", "fur",
				"ga", "gv", "sm", "gd", "gl", "gan", "ki", "glk", "got", "gu", "ha",
				"hak", "xal", "haw", "he", "hi", "ho", "hsb", "hr", "hy", "io",
				"ig", "ii", "ilo", "ia", "ie", "iu", "ik", "os", "xh", "zu", "is",
				"it", "ja", "ka", "kl", "kr", "pam", "krc", "csb", "kk", "kw", "rw",
				"ky", "rn", "sw", "km", "kn", "ko", "kv", "kg", "ht", "ks", "ku",
				"kj", "lad", "lbe", "la", "lv", "to", "lb", "lt", "lij", "li", "ln",
				"lo", "jbo", "lg", "lmo", "hu", "mk", "mg", "mt", "mi", "cdo",
				"mwl", "ml", "mdf", "mo", "mn", "mr", "mus", "my", "mzn", "nah",
				"fj", "ne", "nl", "nds-nl", "cr", "new", "nap", "ce", "pih", "no",
				"nb", "nn", "nrm", "nov", "oc", "mhr", "or", "om", "ng", "hz", "uz",
				"pa", "pag", "pap", "pi", "pcd", "pms", "nds", "pnb", "pl", "pt",
				"pnt", "ps", "aa", "kaa", "crh", "ty", "ksh", "ro", "rmy", "rm",
				"qu", "ru", "sa", "sah", "se", "sg", "sc", "sco", "sd", "stq", "st",
				"tn", "sq", "si", "scn", "simple", "ss", "sk", "sl", "cu", "szl",
				"so", "ckb", "srn", "sr", "sh", "fi", "sv", "ta", "tl", "kab",
				"roa-tara", "tt", "te", "tet", "th", "ti", "vi", "tg", "tokipona",
				"tp", "tpi", "chy", "ve", "tr", "tk", "tw", "udm", "uk", "ur", "ug",
				"za", "vec", "vo", "fiu-vro", "wa", "vls", "war", "wo", "wuu", "ts",
				"yi", "yo", "diq", "zea", "zh", "zh-tw", "zh-cn", "zh-classical",
				"zh-yue", "bat-smg"
			};

            botQueryLists.Add("allpages", "ap"); botQueryLists.Add("alllinks", "al");

            botQueryLists.Add("allusers", "au"); botQueryLists.Add("backlinks", "bl");

            botQueryLists.Add("categorymembers", "cm"); botQueryLists.Add("embeddedin", "ei");

            botQueryLists.Add("imageusage", "iu"); botQueryLists.Add("logevents", "le");

            botQueryLists.Add("recentchanges", "rc"); botQueryLists.Add("usercontribs", "uc");

            botQueryLists.Add("watchlist", "wl"); botQueryLists.Add("exturlusage", "eu");

            botQueryProps.Add("info", "in"); botQueryProps.Add("revisions", "rv");

            botQueryProps.Add("links", "pl"); botQueryProps.Add("langlinks", "ll");

            botQueryProps.Add("images", "im"); botQueryProps.Add("imageinfo", "ii");

            botQueryProps.Add("templates", "tl"); botQueryProps.Add("categories", "cl");

            botQueryProps.Add("extlinks", "el"); botQueryLists.Add("search", "sr");

        }

        /// <summary>Logs in and retrieves cookies.</summary>
        public void LogIn()
        {
            
        }



        /// <summary>Logs in via api.php and retrieves cookies.</summary>
        public void LogInViaApi()
        {
            
        }



        /// <summary>Logs in SourceForge.net and retrieves cookies for work with

        /// SourceForge-hosted wikis. That's a special version of LogIn() function.</summary>

        public void LogInSourceForge()
        {

            string postData = string.Format("form_loginname={0}&form_pw={1}" +

                "&ssl_status=&form_rememberme=yes&login=Log+in",

                HttpUtility.UrlEncode(userName.ToLower()), HttpUtility.UrlEncode(userPass));

            string respStr = PostDataAndGetResultHTM("https://sourceforge.net/account/login.php",

                postData, true, false);

            if (respStr.Contains(" class=\"error\""))

                throw new WikiBotException(

                    "\n\n" + Bot.Msg("Login failed. Check your username and password.") + "\n");

            Console.WriteLine(Bot.Msg("Logged in SourceForge as {0}."), userName);

        }



        /// <summary>Gets the list of Wikimedia Foundation wiki sites and ISO 639-1

        /// language codes, used as prefixes.</summary>

        public void GetWikimediaWikisList()
        {

            Uri wikimediaMeta = new Uri("http://meta.wikimedia.org/wiki/Special:SiteMatrix");

            string respStr = Bot.GetWebResource(wikimediaMeta, "");

            Regex langCodeRE = new Regex("<a id=\"([^\"]+?)\"");

            Regex siteCodeRE = new Regex("<li><a href=\"[^\"]+?\">([^\\s]+?)<");

            MatchCollection langMatches = langCodeRE.Matches(respStr);

            MatchCollection siteMatches = siteCodeRE.Matches(respStr);

            foreach (Match m in langMatches)

                WMLangsStr += Regex.Escape(HttpUtility.HtmlDecode(m.Groups[1].ToString())) + "|";

            WMLangsStr = WMLangsStr.Remove(WMLangsStr.Length - 1);

            foreach (Match m in siteMatches)

                WMSitesStr += Regex.Escape(HttpUtility.HtmlDecode(m.Groups[1].ToString())) + "|";

            WMSitesStr += "m";

            Site.iwikiLinkRE = new Regex(@"(?i)\[\[((" + WMLangsStr + "):(.+?))]]\r?\n?");

            Site.iwikiDispLinkRE = new Regex(@"(?i)\[\[:((" + WMLangsStr + "):(.+?))]]");

            Site.sisterWikiLinkRE = new Regex(@"(?i)\[\[((" + WMSitesStr + "):(.+?))]]");

        }



        /// <summary>This internal function gets the hypertext markup (HTM) of wiki-page.</summary>

        /// <param name="pageURL">Absolute or relative URL of page to get.</param>

        /// <returns>Returns HTM source code.</returns>

        public string GetPageHTM(string pageURL)
        {

            return PostDataAndGetResultHTM(pageURL, "", false, true);

        }



        /// <summary>This internal function posts specified string to requested resource

        /// and gets the result hypertext markup (HTM).</summary>

        /// <param name="pageURL">Absolute or relative URL of page to get.</param>

        /// <param name="postData">String to post to site with web request.</param>

        /// <returns>Returns code of hypertext markup (HTM).</returns>

        public string PostDataAndGetResultHTM(string pageURL, string postData)
        {

            return PostDataAndGetResultHTM(pageURL, postData, false, true);

        }



        /// <summary>This internal function posts specified string to requested resource

        /// and gets the result hypertext markup (HTM).</summary>

        /// <param name="pageURL">Absolute or relative URL of page to get.</param>

        /// <param name="postData">String to post to site with web request.</param>

        /// <param name="getCookies">If set to true, gets cookies from web response and

        /// saves it in site.cookies container.</param>

        /// <param name="allowRedirect">Allow auto-redirection of web request by server.</param>

        /// <returns>Returns code of hypertext markup (HTM).</returns>

        public string PostDataAndGetResultHTM(string pageURL, string postData, bool getCookies,

            bool allowRedirect)
        {

            if (string.IsNullOrEmpty(pageURL))

                throw new WikiBotException(Bot.Msg("No URL specified."));

            if (!pageURL.StartsWith(site) && !site.Contains("sourceforge"))

                pageURL = site + pageURL;

            HttpWebRequest webReq = (HttpWebRequest)WebRequest.Create(pageURL);

            webReq.Proxy.Credentials = CredentialCache.DefaultCredentials;

            webReq.UseDefaultCredentials = true;

            webReq.ContentType = Bot.webContentType;

            webReq.UserAgent = Bot.botVer;

            webReq.AllowAutoRedirect = allowRedirect;

            if (cookies.Count == 0)

                webReq.CookieContainer = new CookieContainer();

            else

                webReq.CookieContainer = cookies;

            if (Bot.unsafeHttpHeaderParsingUsed == 0)
            {

                webReq.ProtocolVersion = HttpVersion.Version10;

                webReq.KeepAlive = false;

            }

            webReq.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate");

            if (!string.IsNullOrEmpty(postData))
            {

                if (Bot.isRunningOnMono)	// Mono bug 636219 evasion

                    webReq.AllowAutoRedirect = false;

                // https://bugzilla.novell.com/show_bug.cgi?id=636219

                webReq.Method = "POST";

                //webReq.Timeout = 180000;

                byte[] postBytes = Encoding.UTF8.GetBytes(postData);

                webReq.ContentLength = postBytes.Length;

                Stream reqStrm = webReq.GetRequestStream();

                reqStrm.Write(postBytes, 0, postBytes.Length);

                reqStrm.Close();

            }

            HttpWebResponse webResp = null;

            for (int errorCounter = 0; true; errorCounter++)
            {

                try
                {

                    webResp = (HttpWebResponse)webReq.GetResponse();

                    break;

                }

                catch (WebException e)
                {

                    string message = e.Message;

                    if (webReq.AllowAutoRedirect == false &&

                        webResp.StatusCode == HttpStatusCode.Redirect)	// Mono bug 636219 evasion

                        return "";

                    if (Regex.IsMatch(message, ": \\(50[02349]\\) "))
                    {		// Remote problem

                        if (errorCounter > Bot.retryTimes)

                            throw;

                        Console.Error.WriteLine(message + " " + Bot.Msg("Retrying in 60 seconds."));

                        Thread.Sleep(60000);

                    }

                    else if (message.Contains("Section=ResponseStatusLine"))
                    {	// Squid problem

                        Bot.SwitchUnsafeHttpHeaderParsing(true);

                        //Console.Write("|");

                        return PostDataAndGetResultHTM(pageURL, postData, getCookies,

                            allowRedirect);

                    }

                    else

                        throw;

                }

            }

            Stream respStream = webResp.GetResponseStream();

            if (webResp.ContentEncoding.ToLower().Contains("gzip"))

                respStream = new GZipStream(respStream, CompressionMode.Decompress);

            else if (webResp.ContentEncoding.ToLower().Contains("deflate"))

                respStream = new DeflateStream(respStream, CompressionMode.Decompress);

            if (getCookies == true)
            {

                Uri siteUri = new Uri(site);

                foreach (Cookie cookie in webResp.Cookies)
                {

                    if (cookie.Domain[0] == '.' &&

                        cookie.Domain.Substring(1) == siteUri.Host)

                        cookie.Domain = cookie.Domain.TrimStart(new char[] { '.' });

                    cookies.Add(cookie);

                }

            }

            StreamReader strmReader = new StreamReader(respStream, encoding);

            string respStr = strmReader.ReadToEnd();

            strmReader.Close();

            webResp.Close();

            return respStr;

        }



        /// <summary>This internal function deletes everything before startTag and everything after

        /// endTag. Optionally it can insert back the DOCTYPE definition and root element of

        /// XML/XHTML documents.</summary>

        /// <param name="text">Source text.</param>

        /// <param name="startTag">The beginning of returned content.</param>

        /// <param name="endTag">The end of returned content.</param>

        /// <param name="removeTags">If true, tags will also be removed.</param>

        /// <param name="leaveHead">If true, DOCTYPE definition and root element will be left

        /// intact.</param>

        /// <returns>Returns stripped content.</returns>

        public string StripContent(string text, string startTag, string endTag,

            bool removeTags, bool leaveHead)
        {

            if (string.IsNullOrEmpty(startTag))

                startTag = "<!-- bodytext -->";

            if (startTag == "<!-- bodytext -->" && ver < new Version(1, 16))

                startTag = "<!-- start content -->";



            if (startTag == "<!-- bodytext -->" && string.IsNullOrEmpty(endTag))

                endTag = "<!-- /bodytext -->";

            else if (startTag == "<!-- content -->" && string.IsNullOrEmpty(endTag))

                endTag = "<!-- /content -->";

            else if (startTag == "<!-- bodyContent -->" && string.IsNullOrEmpty(endTag))

                endTag = "<!-- /bodyContent -->";

            else if (startTag == "<!-- start content -->" && string.IsNullOrEmpty(endTag))

                endTag = "<!-- end content -->";



            if (text[0] != '<')

                text = text.Trim();



            string headText = "";

            string rootEnd = "";

            if (leaveHead == true)
            {

                int headEndPos = ((text.StartsWith("<!") || text.StartsWith("<?"))

                    && text.IndexOf('>') != -1) ? text.IndexOf('>') + 1 : 0;

                if (text.IndexOf('>', headEndPos) != -1)

                    headEndPos = text.IndexOf('>', headEndPos) + 1;

                headText = text.Substring(0, headEndPos);

                int rootEndPos = text.LastIndexOf("</");

                if (rootEndPos == -1)

                    headText = "";

                else

                    rootEnd = text.Substring(rootEndPos);

            }



            int startPos = text.IndexOf(startTag) + (removeTags == true ? startTag.Length : 0);

            int endPos = text.IndexOf(endTag) + (removeTags == false ? endTag.Length : 0);

            if (startPos == -1 || endPos == -1 || endPos < startPos)

                return headText + text + rootEnd;

            else

                return headText + text.Substring(startPos, endPos - startPos) + rootEnd;

        }



        /// <summary>This internal function constructs XPathDocument, makes XPath query and

        /// returns XPathNodeIterator for selected nodes.</summary>

        /// <param name="xmlSource">Source XML data.</param>

        /// <param name="xpathQuery">XPath query to select specific nodes in XML data.</param>

        /// <returns>XPathNodeIterator object.</returns>

        public XPathNodeIterator GetXMLIterator(string xmlSource, string xpathQuery)
        {

            XmlReader reader = GetXMLReader(xmlSource);

            XPathDocument doc = new XPathDocument(reader);

            XPathNavigator nav = doc.CreateNavigator();

            return nav.Select(xpathQuery, xmlNS);

        }



        /// <summary>This internal function constructs and returns XmlReader object.</summary>

        /// <param name="xmlSource">Source XML data.</param>

        /// <returns>XmlReader object.</returns>

        public XmlReader GetXMLReader(string xmlSource)
        {

            StringReader strReader = new StringReader(xmlSource);

            XmlReaderSettings settings = new XmlReaderSettings();

            settings.XmlResolver = new XmlUrlResolverWithCache();

            settings.CheckCharacters = false;

            settings.IgnoreComments = true;

            settings.IgnoreProcessingInstructions = true;

            settings.IgnoreWhitespace = true;

            settings.ProhibitDtd = false;

            return XmlReader.Create(strReader, settings);

        }



        /// <summary>This internal function removes the namespace prefix from page title.</summary>

        /// <param name="pageTitle">Page title to remove prefix from.</param>

        /// <param name="nsIndex">Index of namespace to remove. If this parameter is 0,

        /// any found namespace prefix is removed.</param>

        /// <returns>Page title without prefix.</returns>

        public string RemoveNSPrefix(string pageTitle, int nsIndex)
        {

            if (string.IsNullOrEmpty(pageTitle))

                throw new ArgumentNullException("pageTitle");

            if (nsIndex != 0)
            {

                if (wikiNSpaces[nsIndex.ToString()] != null)

                    pageTitle = Regex.Replace(pageTitle, "(?i)^" +

                        Regex.Escape(wikiNSpaces[nsIndex.ToString()].ToString()) + ":", "");

                if (namespaces[nsIndex.ToString()] != null)

                    pageTitle = Regex.Replace(pageTitle, "(?i)^" +

                        Regex.Escape(namespaces[nsIndex.ToString()].ToString()) + ":", "");

                return pageTitle;

            }

            foreach (DictionaryEntry ns in wikiNSpaces)
            {

                if (ns.Value == null)

                    continue;

                pageTitle = Regex.Replace(pageTitle, "(?i)^" +

                    Regex.Escape(ns.Value.ToString()) + ":", "");

            }

            foreach (DictionaryEntry ns in namespaces)
            {

                if (ns.Value == null)

                    continue;

                pageTitle = Regex.Replace(pageTitle, "(?i)^" +

                    Regex.Escape(ns.Value.ToString()) + ":", "");

            }

            return pageTitle;

        }



        /// <summary>Function changes default English namespace prefixes to correct local prefixes

        /// (e.g. for German wiki-sites it changes "Category:..." to "Kategorie:...").</summary>

        /// <param name="pageTitle">Page title to correct prefix in.</param>

        /// <returns>Page title with corrected prefix.</returns>

        public string CorrectNSPrefix(string pageTitle)
        {

            if (string.IsNullOrEmpty(pageTitle))

                throw new ArgumentNullException("pageTitle");

            foreach (DictionaryEntry ns in wikiNSpaces)
            {

                if (ns.Value == null)

                    continue;

                if (Regex.IsMatch(pageTitle, "(?i)" + Regex.Escape(ns.Value.ToString()) + ":"))

                    pageTitle = namespaces[ns.Key] + pageTitle.Substring(pageTitle.IndexOf(":"));

            }

            return pageTitle;

        }



        /// <summary>Parses the provided template body and returns the key/value pairs of it's

        /// parameters titles and values. Everything inside the double braces must be passed to

        /// this function, so first goes the template's title, then '|' character, and then go the

        /// parameters. Please, see the usage example.</summary>

        /// <param name="template">Complete template's body including it's title, but not

        /// including double braces.</param>

        /// <returns>Returns the Dictionary &lt;string, string&gt; object, where keys are parameters

        /// titles and values are parameters values. If parameter is untitled, it's number is

        /// returned as the (string) dictionary key. If parameter value is set several times in the

        /// template (normally that shouldn't occur), only the last value is returned. Template's

        /// title is not returned as a parameter.</returns>

        /// <example><code>

        /// Dictionary &lt;string, string&gt; parameters1 =

        /// 	site.ParseTemplate("TemplateTitle|param1=val1|param2=val2");

        /// string[] templates = page.GetTemplatesWithParams();

        /// Dictionary &lt;string, string&gt; parameters2 = site.ParseTemplate(templates[0]);

        /// parameters1["param2"] = "newValue";

        /// </code></example>

        public Dictionary<string, string> ParseTemplate(string template)
        {

            if (string.IsNullOrEmpty(template))

                throw new ArgumentNullException("template");

            if (template.StartsWith("{{"))

                template = template.Substring(2, template.Length - 4);



            int startPos, endPos, len = 0;

            string str = template;



            while ((startPos = str.LastIndexOf("{{")) != -1)
            {

                endPos = str.IndexOf("}}", startPos);

                len = (endPos != -1) ? endPos - startPos + 2 : 2;

                str = str.Remove(startPos, len);

                str = str.Insert(startPos, new String('_', len));

            }



            while ((startPos = str.LastIndexOf("[[")) != -1)
            {

                endPos = str.IndexOf("]]", startPos);

                len = (endPos != -1) ? endPos - startPos + 2 : 2;

                str = str.Remove(startPos, len);

                str = str.Insert(startPos, new String('_', len));

            }



            List<int> separators = Bot.GetMatchesPositions(str, "|", false);

            if (separators == null || separators.Count == 0)

                return new Dictionary<string, string>();

            List<string> parameters = new List<string>();

            endPos = template.Length;

            for (int i = separators.Count - 1; i >= 0; i--)
            {

                parameters.Add(template.Substring(separators[i] + 1, endPos - separators[i] - 1));

                endPos = separators[i];

            }

            parameters.Reverse();



            Dictionary<string, string> templateParams = new Dictionary<string, string>();

            for (int pos, i = 0; i < parameters.Count; i++)
            {

                pos = parameters[i].IndexOf('=');

                if (pos == -1)

                    templateParams[i.ToString()] = parameters[i].Trim();

                else

                    templateParams[parameters[i].Substring(0, pos).Trim()] =

                        parameters[i].Substring(pos + 1).Trim();

            }

            return templateParams;

        }



        /// <summary>Formats a template with the specified title and parameters. Default formatting

        /// options are used.</summary>

        /// <param name="templateTitle">Template's title.</param>

        /// <param name="templateParams">Template's parameters in Dictionary &lt;string, string&gt;

        /// object, where keys are parameters titles and values are parameters values.</param>

        /// <returns>Returns the complete template in double braces.</returns>

        public string FormatTemplate(string templateTitle,

            Dictionary<string, string> templateParams)
        {

            return FormatTemplate(templateTitle, templateParams, false, false, 0);

        }



        /// <summary>Formats a template with the specified title and parameters. Formatting

        /// options are got from provided reference template. That function is usually used to

        /// format modified template as it was in it's initial state, though absolute format

        /// consistency can not be guaranteed.</summary>

        /// <param name="templateTitle">Template's title.</param>

        /// <param name="templateParams">Template's parameters in Dictionary &lt;string, string&gt;

        /// object, where keys are parameters titles and values are parameters values.</param>

        /// <param name="referenceTemplate">Full template body to detect formatting options in.

        /// With or without double braces.</param>

        /// <returns>Returns the complete template in double braces.</returns>

        public string FormatTemplate(string templateTitle,

            Dictionary<string, string> templateParams, string referenceTemplate)
        {

            if (string.IsNullOrEmpty(referenceTemplate))

                throw new ArgumentNullException("referenceTemplate");



            bool inline = false;

            bool withoutSpaces = false;

            int padding = 0;



            if (!referenceTemplate.Contains("\n"))

                inline = true;

            if (!referenceTemplate.Contains(" ") && !referenceTemplate.Contains("\t"))

                withoutSpaces = true;

            if (withoutSpaces == false && referenceTemplate.Contains("  ="))

                padding = -1;



            return FormatTemplate(templateTitle, templateParams, inline, withoutSpaces, padding);

        }



        /// <summary>Formats a template with the specified title and parameters, allows extended

        /// format options to be specified.</summary>

        /// <param name="templateTitle">Template's title.</param>

        /// <param name="templateParams">Template's parameters in Dictionary &lt;string, string&gt;

        /// object, where keys are parameters titles and values are parameters values.</param>

        /// <param name="inline">When set to true, template is formatted in one line, without any

        /// line breaks. Default value is false.</param>

        /// <param name="withoutSpaces">When set to true, template is formatted without spaces.

        /// Default value is false.</param>

        /// <param name="padding">When set to positive value, template parameters titles are padded

        /// on the right with specified number of spaces, so "=" characters could form a nice

        /// straight column. When set to -1, the number of spaces is calculated automatically.

        /// Default value is 0 (no padding). The padding will occur only when "inline" option

        /// is set to false and "withoutSpaces" option is also set to false.</param>

        /// <returns>Returns the complete template in double braces.</returns>

        public string FormatTemplate(string templateTitle,

            Dictionary<string, string> templateParams, bool inline, bool withoutSpaces, int padding)
        {

            if (string.IsNullOrEmpty(templateTitle))

                throw new ArgumentNullException("templateTitle");

            if (templateParams == null || templateParams.Count == 0)

                throw new ArgumentNullException("templateParams");



            if (inline != false || withoutSpaces != false)

                padding = 0;

            if (padding == -1)

                foreach (KeyValuePair<string, string> kvp in templateParams)

                    if (kvp.Key.Length > padding)

                        padding = kvp.Key.Length;



            int i = 1;

            string template = "{{" + templateTitle;

            foreach (KeyValuePair<string, string> kvp in templateParams)
            {

                template += "\n| ";

                if (padding <= 0)
                {

                    if (kvp.Key == i.ToString())

                        template += kvp.Value;

                    else

                        template += kvp.Key + " = " + kvp.Value;

                }

                else
                {

                    if (kvp.Key == i.ToString())

                        template += kvp.Value.PadRight(padding + 3);

                    else

                        template += kvp.Key.PadRight(padding) + " = " + kvp.Value;

                }

                i++;

            }

            template += "\n}}";



            if (inline == true)

                template = template.Replace("\n", " ");

            if (withoutSpaces == true)

                template = template.Replace(" ", "");

            return template;

        }



        /// <summary>Shows names and integer keys of local and default namespaces.</summary>

        public void ShowNamespaces()
        {

            foreach (DictionaryEntry ns in namespaces)
            {

                Console.WriteLine(ns.Key.ToString() + "\t" + ns.Value.ToString().PadRight(20) +

                    "\t" + wikiNSpaces[ns.Key.ToString()]);

            }

        }

    }



    /// <summary>Class defines wiki page object.</summary>

    [ClassInterface(ClassInterfaceType.AutoDispatch)]

    [Serializable]

    public class Page
    {

        /// <summary>Page title.</summary>

        public string title;

        /// <summary>Page text.</summary>

        public string text;

        /// <summary>Page ID in internal MediaWiki database.</summary>

        public string pageID;

        /// <summary>Username or IP-address of last page contributor.</summary>

        public string lastUser;

        /// <summary>Last contributor ID in internal MediaWiki database.</summary>

        public string lastUserID;

        /// <summary>Page revision ID in the internal MediaWiki database.</summary>

        public string lastRevisionID;

        /// <summary>True, if last edit was minor edit.</summary>

        public bool lastMinorEdit;

        /// <summary>Amount of bytes, modified during last edit.</summary>

        public int lastBytesModified;

        /// <summary>Last edit comment.</summary>

        public string comment;

        /// <summary>Date and time of last edit expressed in UTC (Coordinated Universal Time).

        /// Call "timestamp.ToLocalTime()" to convert to local time if it is necessary.</summary>

        public DateTime timestamp;

        /// <summary>True, if this page is in bot account's watchlist. Call GetEditSessionData

        /// function to get the actual state of this property.</summary>

        public bool watched;

        /// <summary>This edit session time attribute is used to edit pages.</summary>

        public string editSessionTime;

        /// <summary>This edit session token attribute is used to edit pages.</summary>

        public string editSessionToken;

        /// <summary>Site, on which the page is.</summary>

        public Site site;



        /// <summary>This constructor creates Page object with specified title and specified

        /// Site object. This is preferable constructor. When constructed, new Page object doesn't

        /// contain text. Use Load() method to get text from live wiki. Or use LoadEx() to get

        /// both text and metadata via XML export interface.</summary>

        /// <param name="site">Site object, it must be constructed beforehand.</param>

        /// <param name="title">Page title as string.</param>

        /// <returns>Returns Page object.</returns>

        public Page(Site site, string title)
        {

            this.title = title;

            this.site = site;

        }



        /// <summary>This constructor creates empty Page object with specified Site object,

        /// but without title. Avoid using this constructor needlessly.</summary>

        /// <param name="site">Site object, it must be constructed beforehand.</param>

        /// <returns>Returns Page object.</returns>

        public Page(Site site)
        {

            this.site = site;

        }



        /// <summary>This constructor creates Page object with specified title. Site object

        /// with default properties is created internally and logged in. Constructing

        /// new Site object is too slow, don't use this constructor needlessly.</summary>

        /// <param name="title">Page title as string.</param>

        /// <returns>Returns Page object.</returns>

        public Page(string title)
        {

            this.site = new Site();

            this.title = title;

        }



        /// <summary>This constructor creates Page object with specified page's numeric revision ID

        /// (also called "oldid"). Page title is retrieved automatically

        /// in this constructor.</summary>

        /// <param name="site">Site object, it must be constructed beforehand.</param>

        /// <param name="revisionID">Page's numeric revision ID (also called "oldid").</param>

        /// <returns>Returns Page object.</returns>

        public Page(Site site, Int64 revisionID)
        {

            if (revisionID <= 0)

                throw new ArgumentOutOfRangeException("revisionID",

                    Bot.Msg("Revision ID must be positive."));

            this.site = site;

            lastRevisionID = revisionID.ToString();

            GetTitle();

        }



        /// <summary>This constructor creates Page object with specified page's numeric revision ID

        /// (also called "oldid"). Page title is retrieved automatically in this constructor.

        /// Site object with default properties is created internally and logged in. Constructing

        /// new Site object is too slow, don't use this constructor needlessly.</summary>

        /// <param name="revisionID">Page's numeric revision ID (also called "oldid").</param>

        /// <returns>Returns Page object.</returns>

        public Page(Int64 revisionID)
        {

            if (revisionID <= 0)

                throw new ArgumentOutOfRangeException("revisionID",

                    Bot.Msg("Revision ID must be positive."));

            this.site = new Site();

            lastRevisionID = revisionID.ToString();

            GetTitle();

        }



        /// <summary>This constructor creates empty Page object without title. Site object with

        /// default properties is created internally and logged in. Constructing new Site object

        /// is too slow, avoid using this constructor needlessly.</summary>

        /// <returns>Returns Page object.</returns>

        public Page()
        {

            this.site = new Site();

        }



        /// <summary>Loads actual page text for live wiki site via raw web interface.

        /// If Page.lastRevisionID is specified, the function gets that specified

        /// revision.</summary>

        public void Load()
        {

            if (string.IsNullOrEmpty(title))

                throw new WikiBotException(Bot.Msg("No title specified for page to load."));

            string res = site.site + site.indexPath + "index.php?title=" +

                HttpUtility.UrlEncode(title) +

                (string.IsNullOrEmpty(lastRevisionID) ? "" : "&oldid=" + lastRevisionID) +

                "&redirect=no&action=raw&ctype=text/plain&dontcountme=s";

            try
            {

                text = site.GetPageHTM(res);

            }

            catch (WebException e)
            {

                string message = e.Message;

                if (message.Contains(": (404) "))
                {		// Not Found


                    text = "";

                    return;

                }

                else

                    throw;

            }

        }



        /// <summary>Loads page text and metadata via XML export interface. It is slower,

        /// than Load(), don't use it if you don't need page metadata (page id, timestamp,

        /// comment, last contributor, minor edit mark).</summary>

        public void LoadEx()
        {

            if (string.IsNullOrEmpty(title))

                throw new WikiBotException(Bot.Msg("No title specified for page to load."));

            string res = site.site + site.indexPath + "index.php?title=Special:Export/" +

                HttpUtility.UrlEncode(title) + "&action=submit";

            string src = site.GetPageHTM(res);

            ParsePageXML(src);

        }



        /// <summary>This internal function parses MediaWiki XML export data using XmlDocument

        /// to get page text and metadata.</summary>

        /// <param name="xmlSrc">XML export source code.</param>

        public void ParsePageXML(string xmlSrc)
        {

            XmlDocument doc = new XmlDocument();

            doc.LoadXml(xmlSrc);

            if (doc.GetElementsByTagName("page").Count == 0)
            {


                return;

            }

            text = doc.GetElementsByTagName("text")[0].InnerText;

            pageID = doc.GetElementsByTagName("id")[0].InnerText;

            if (doc.GetElementsByTagName("username").Count != 0)
            {

                lastUser = doc.GetElementsByTagName("username")[0].InnerText;

                lastUserID = doc.GetElementsByTagName("id")[2].InnerText;

            }

            else if (doc.GetElementsByTagName("ip").Count != 0)

                lastUser = doc.GetElementsByTagName("ip")[0].InnerText;

            else

                lastUser = "(n/a)";

            lastRevisionID = doc.GetElementsByTagName("id")[1].InnerText;

            if (doc.GetElementsByTagName("comment").Count != 0)

                comment = doc.GetElementsByTagName("comment")[0].InnerText;

            timestamp = DateTime.Parse(doc.GetElementsByTagName("timestamp")[0].InnerText);

            timestamp = timestamp.ToUniversalTime();

            lastMinorEdit = (doc.GetElementsByTagName("minor").Count != 0) ? true : false;

            if (string.IsNullOrEmpty(title))

                title = doc.GetElementsByTagName("title")[0].InnerText;

        }



        /// <summary>Loads page text from the specified UTF8-encoded file.</summary>

        /// <param name="filePathName">Path and name of the file.</param>

        public void LoadFromFile(string filePathName)
        {

            StreamReader strmReader = new StreamReader(filePathName);

            text = strmReader.ReadToEnd();

            strmReader.Close();

            Console.WriteLine(

                Bot.Msg("Text for page \"{0}\" successfully loaded from \"{1}\" file."),

                title, filePathName);

        }



        /// <summary>This function is used internally to gain rights to edit page

        /// on a live wiki site.</summary>

        public void GetEditSessionData()
        {

            if (string.IsNullOrEmpty(title))

                throw new WikiBotException(

                    Bot.Msg("No title specified for page to get edit session data."));

            string src = site.GetPageHTM(site.indexPath + "index.php?title=" +

                HttpUtility.UrlEncode(title) + "&action=edit");

            editSessionTime = Site.editSessionTimeRE1.Match(src).Groups[1].ToString();

            editSessionToken = Site.editSessionTokenRE1.Match(src).Groups[1].ToString();

            if (string.IsNullOrEmpty(editSessionToken))

                editSessionToken = Site.editSessionTokenRE2.Match(src).Groups[1].ToString();

            watched = Regex.IsMatch(src, "<a href=\"[^\"]+&(amp;)?action=unwatch\"");

        }



        /// <summary>This function is used internally to gain rights to edit page on a live wiki

        /// site. The function queries rights, using bot interface, thus saving traffic.</summary>

        public void GetEditSessionDataEx()
        {

            if (string.IsNullOrEmpty(title))

                throw new WikiBotException(

                    Bot.Msg("No title specified for page to get edit session data."));

            string src = site.GetPageHTM(site.indexPath + "api.php?action=query&prop=info" +

                "&format=xml&intoken=edit&titles=" + HttpUtility.UrlEncode(title));

            editSessionToken = Site.editSessionTokenRE3.Match(src).Groups[1].ToString();

            if (editSessionToken == "+\\")

                editSessionToken = "";

            editSessionTime = Site.editSessionTimeRE3.Match(src).Groups[1].ToString();

            if (!string.IsNullOrEmpty(editSessionTime))

                editSessionTime = Regex.Replace(editSessionTime, "\\D", "");

            if (string.IsNullOrEmpty(editSessionTime) && !string.IsNullOrEmpty(editSessionToken))

                editSessionTime = DateTime.Now.ToUniversalTime().ToString("yyyyMMddHHmmss");

            if (site.watchList == null)
            {

                site.watchList = new PageList(site);

                site.watchList.FillFromWatchList();

            }

            watched = site.watchList.Contains(title);

        }



        /// <summary>Retrieves the title for this Page object using page's numeric revision ID

        /// (also called "oldid"), stored in "lastRevisionID" object's property. Make sure that

        /// "lastRevisionID" property is set before calling this function. Use this function

        /// when working with old revisions to detect if the page was renamed at some

        /// moment.</summary>

        public void GetTitle()
        {

            if (string.IsNullOrEmpty(lastRevisionID))

                throw new WikiBotException(

                    Bot.Msg("No revision ID specified for page to get title for."));

            string src = site.GetPageHTM(site.site + site.indexPath +

                "index.php?oldid=" + lastRevisionID);

            title = Regex.Match(src, "<h1 (?:id=\"firstHeading\" )?class=\"firstHeading\">" +

                "(.+?)</h1>").Groups[1].ToString();

        }



        /// <summary>Saves current contents of page.text on live wiki site. Uses default bot

        /// edit comment and default minor edit mark setting ("true" in most cases)/</summary>

        public void Save()
        {

            Save(text, Bot.editComment, Bot.isMinorEdit);

        }



        /// <summary>Saves specified text in page on live wiki. Uses default bot

        /// edit comment and default minor edit mark setting ("true" in most cases).</summary>

        /// <param name="newText">New text for this page.</param>

        public void Save(string newText)
        {

            Save(newText, Bot.editComment, Bot.isMinorEdit);

        }



        /// <summary>Saves current page.text contents on live wiki site.</summary>

        /// <param name="comment">Your edit comment.</param>

        /// <param name="isMinorEdit">Minor edit mark (true = minor edit).</param>

        public void Save(string comment, bool isMinorEdit)
        {

            Save(text, comment, isMinorEdit);

        }



        /// <summary>Saves specified text in page on live wiki.</summary>

        /// <param name="newText">New text for this page.</param>

        /// <param name="comment">Your edit comment.</param>

        /// <param name="isMinorEdit">Minor edit mark (true = minor edit).</param>

        public void Save(string newText, string comment, bool isMinorEdit)
        {

            if (string.IsNullOrEmpty(title))

                throw new WikiBotException(Bot.Msg("No title specified for page to save text to."));

            if (string.IsNullOrEmpty(newText) && string.IsNullOrEmpty(text))

                throw new WikiBotException(Bot.Msg("No text specified for page to save."));

            if (text != null && Regex.IsMatch(text, @"(?is)\{\{(nobots|bots\|(allow=none|" +

                @"deny=(?!none)[^\}]*(" + site.userName + @"|all)|optout=all))\}\}"))

                throw new WikiBotException(string.Format(Bot.Msg(

                    "Bot action on \"{0}\" page is prohibited " +

                    "by \"nobots\" or \"bots|allow=none\" template."), title));



            if (Bot.useBotQuery == true && site.botQuery == true &&

                (site.ver.Major > 1 || (site.ver.Major == 1 && site.ver.Minor >= 15)))

                GetEditSessionDataEx();

            else

                GetEditSessionData();

            if (string.IsNullOrEmpty(editSessionTime) || string.IsNullOrEmpty(editSessionToken))

                throw new WikiBotException(

                    string.Format(Bot.Msg("Insufficient rights to edit page \"{0}\"."), title));

            string postData = string.Format("wpSection=&wpStarttime={0}&wpEdittime={1}" +

                "&wpScrolltop=&wpTextbox1={2}&wpSummary={3}&wpSave=Save%20Page" +

                "&wpEditToken={4}{5}{6}",

                    // &wpAutoSummary=00000000000000000000000000000000&wpIgnoreBlankSummary=1

                DateTime.Now.ToUniversalTime().ToString("yyyyMMddHHmmss"),

                HttpUtility.UrlEncode(editSessionTime),

                HttpUtility.UrlEncode(newText),

                HttpUtility.UrlEncode(comment),

                HttpUtility.UrlEncode(editSessionToken),

                watched ? "&wpWatchthis=1" : "",

                isMinorEdit ? "&wpMinoredit=1" : "");

            if (Bot.askConfirm)
            {

                Console.Write("\n\n" +

                    Bot.Msg("The following text is going to be saved on page \"{0}\":"), title);

                Console.Write("\n\n" + text + "\n\n");

                if (!Bot.UserConfirms())

                    return;

            }

            string respStr = site.PostDataAndGetResultHTM(site.indexPath + "index.php?title=" +

                HttpUtility.UrlEncode(title) + "&action=submit", postData);

            if (respStr.Contains(" name=\"wpTextbox2\""))

                throw new WikiBotException(string.Format(

                    Bot.Msg("Edit conflict occurred while trying to savе page \"{0}\"."), title));

            if (respStr.Contains("<div class=\"permissions-errors\">"))

                throw new WikiBotException(

                    string.Format(Bot.Msg("Insufficient rights to edit page \"{0}\"."), title));

            if (respStr.Contains("input name=\"wpCaptchaWord\" id=\"wpCaptchaWord\""))

                throw new WikiBotException(

                    string.Format(Bot.Msg("Error occurred when saving page \"{0}\": " +

                    "Bot operation is not allowed for this account at \"{1}\" site."),

                    title, site.site));

            Console.WriteLine(Bot.Msg("Page \"{0}\" saved successfully."), title);

            text = newText;

        }



        /// <summary>Undoes the last edit, so page text reverts to previous contents.

        /// The function doesn't affect other actions like renaming.</summary>

        /// <param name="comment">Revert comment.</param>

        /// <param name="isMinorEdit">Minor edit mark (pass true for minor edit).</param>

        public void Revert(string comment, bool isMinorEdit)
        {

            if (string.IsNullOrEmpty(title))

                throw new WikiBotException(Bot.Msg("No title specified for page to revert."));

            PageList pl = new PageList(site);

            if (Bot.useBotQuery == true && site.botQuery == true &&

                site.botQueryVersions.ContainsKey("ApiQueryRevisions.php"))

                pl.FillFromPageHistoryEx(title, 2, false);

            else

                pl.FillFromPageHistory(title, 2);

            if (pl.Count() != 2)
            {

                Console.Error.WriteLine(Bot.Msg("Can't revert page \"{0}\"."), title);

                return;

            }

            pl[1].Load();

            Save(pl[1].text, comment, isMinorEdit);

            Console.WriteLine(Bot.Msg("Page \"{0}\" was reverted."), title);

        }



        /// <summary>Undoes all last edits of last page contributor, so page text reverts to

        /// previous contents. The function doesn't affect other operations

        /// like renaming or protecting.</summary>

        /// <param name="comment">Revert comment.</param>

        /// <param name="isMinorEdit">Minor edit mark (pass true for minor edit).</param>

        /// <returns>Returns true if last edits were undone.</returns>

        public bool UndoLastEdits(string comment, bool isMinorEdit)
        {

            if (string.IsNullOrEmpty(title))

                throw new WikiBotException(Bot.Msg("No title specified for page to revert."));

            PageList pl = new PageList(site);

            string lastEditor = "";

            for (int i = 50; i <= 5000; i *= 10)
            {

                if (Bot.useBotQuery == true && site.botQuery == true &&

                    site.botQueryVersions.ContainsKey("ApiQueryRevisions.php"))

                    pl.FillFromPageHistoryEx(title, i, false);

                else

                    pl.FillFromPageHistory(title, i);

                lastEditor = pl[0].lastUser;

                foreach (Page p in pl)

                    if (p.lastUser != lastEditor)
                    {

                        p.Load();

                        Save(p.text, comment, isMinorEdit);

                        Console.WriteLine(

                            Bot.Msg("Last edits of page \"{0}\" by user {1} were undone."),

                            title, lastEditor);

                        return true;

                    }

                if (pl.pages.Count < i)

                    break;

                pl.Clear();

            }

            Console.Error.WriteLine(Bot.Msg("Can't undo last edits of page \"{0}\" by user {1}."),

                title, lastEditor);

            return false;

        }



        /// <summary>Protects or unprotects the page, so only authorized group of users can edit or

        /// rename it. Changing page protection mode requires administrator (sysop)

        /// rights.</summary>

        /// <param name="editMode">Protection mode for editing this page (0 = everyone allowed

        /// to edit, 1 = only registered users are allowed, 2 = only administrators are allowed

        /// to edit).</param>

        /// <param name="renameMode">Protection mode for renaming this page (0 = everyone allowed to

        /// rename, 1 = only registered users are allowed, 2 = only administrators

        /// are allowed).</param>

        /// <param name="cascadeMode">In cascading mode, all the pages, included into this page

        /// (e.g., templates or images) are also automatically protected.</param>

        /// <param name="expiryDate">Date and time, expressed in UTC, when protection expires

        /// and page becomes unprotected. Use DateTime.ToUniversalTime() method to convert local

        /// time to UTC, if necessary. Pass DateTime.MinValue to make protection indefinite.</param>

        /// <param name="reason">Reason for protecting this page.</param>

        public void Protect(int editMode, int renameMode, bool cascadeMode,

            DateTime expiryDate, string reason)
        {

            if (string.IsNullOrEmpty(title))

                throw new WikiBotException(Bot.Msg("No title specified for page to protect."));

            string errorMsg =

                Bot.Msg("Only values 0, 1 and 2 are accepted. Please, consult documentation.");

            if (editMode > 2 || editMode < 0)

                throw new ArgumentOutOfRangeException("editMode", errorMsg);

            if (renameMode > 2 || renameMode < 0)

                throw new ArgumentOutOfRangeException("renameMode", errorMsg);

            if (expiryDate != DateTime.MinValue && expiryDate < DateTime.Now)

                throw new ArgumentOutOfRangeException("expiryDate",

                    Bot.Msg("Protection expiry date must be hereafter."));

            string res = site.site + site.indexPath +

                "index.php?title=" + HttpUtility.UrlEncode(title) + "&action=protect";

            string src = site.GetPageHTM(res);

            editSessionTime = Site.editSessionTimeRE1.Match(src).Groups[1].ToString();

            editSessionToken = Site.editSessionTokenRE1.Match(src).Groups[1].ToString();

            if (string.IsNullOrEmpty(editSessionToken))

                editSessionToken = Site.editSessionTokenRE2.Match(src).Groups[1].ToString();

            if (string.IsNullOrEmpty(editSessionToken))
            {

                Console.Error.WriteLine(

                    Bot.Msg("Unable to change protection mode for page \"{0}\"."), title);

                return;

            }

            string postData = string.Format("mwProtect-level-edit={0}&mwProtect-level-move={1}" +

                "&mwProtect-reason={2}&wpEditToken={3}&mwProtect-expiry={4}{5}",

                HttpUtility.UrlEncode(

                    editMode == 2 ? "sysop" : editMode == 1 ? "autoconfirmed" : ""),

                HttpUtility.UrlEncode(

                    renameMode == 2 ? "sysop" : renameMode == 1 ? "autoconfirmed" : ""),

                HttpUtility.UrlEncode(reason),

                HttpUtility.UrlEncode(editSessionToken),

                expiryDate == DateTime.MinValue ? "" : expiryDate.ToString("u"),

                cascadeMode == true ? "&mwProtect-cascade=1" : "");

            string respStr = site.PostDataAndGetResultHTM(site.indexPath +

                "index.php?title=" + HttpUtility.UrlEncode(title) + "&action=protect", postData);

            if (string.IsNullOrEmpty(respStr))
            {

                Console.Error.WriteLine(

                    Bot.Msg("Unable to change protection mode for page \"{0}\"."), title);

                return;

            }

            Console.WriteLine(

                Bot.Msg("Protection mode for page \"{0}\" changed successfully."), title);

        }



        /// <summary>Adds page to bot account's watchlist.</summary>

        public void Watch()
        {

            if (string.IsNullOrEmpty(title))

                throw new WikiBotException(Bot.Msg("No title specified for page to watch."));

            string res = site.site + site.indexPath +

                "index.php?title=" + HttpUtility.UrlEncode(title);

            string respStr = site.GetPageHTM(res);

            string watchToken = "";

            Regex watchTokenRE = new Regex("&amp;action=watch&amp;token=([^\"]+?)\"");

            if (watchTokenRE.IsMatch(respStr))

                watchToken = watchTokenRE.Match(respStr).Groups[1].ToString();

            respStr = site.GetPageHTM(res + "&action=watch&token=" + watchToken);

            watched = true;

            Console.WriteLine(Bot.Msg("Page \"{0}\" added to watchlist."), title);

        }



        /// <summary>Removes page from bot account's watchlist.</summary>

        public void Unwatch()
        {

            if (string.IsNullOrEmpty(title))

                throw new WikiBotException(Bot.Msg("No title specified for page to unwatch."));

            string res = site.site + site.indexPath +

                "index.php?title=" + HttpUtility.UrlEncode(title);

            string respStr = site.GetPageHTM(res);

            string unwatchToken = "";

            Regex unwatchTokenRE = new Regex("&amp;action=unwatch&amp;token=([^\"]+?)\"");

            if (unwatchTokenRE.IsMatch(respStr))

                unwatchToken = unwatchTokenRE.Match(respStr).Groups[1].ToString();

            respStr = site.GetPageHTM(res + "&action=unwatch&token=" + unwatchToken);

            watched = false;

            Console.WriteLine(Bot.Msg("Page \"{0}\" was removed from watchlist."), title);

        }



        /// <summary>This function opens page text in Microsoft Word for editing.

        /// Just close Word after editing, and the revised text will appear in

        /// Page.text variable.</summary>

        /// <remarks>Appropriate PIAs (Primary Interop Assemblies) for available MS Office

        /// version must be installed and referenced in order to use this function. Follow

        /// instructions in "Compile and Run.bat" file to reference PIAs properly in compilation

        /// command, and then recompile the framework. Redistributable PIAs can be downloaded from

        /// http://www.microsoft.com/downloads/results.aspx?freetext=Office%20PIA</remarks>

        public void ReviseInMSWord()
        {

#if MS_WORD_INTEROP
			if (string.IsNullOrEmpty(text))
				throw new WikiBotException(Bot.Msg("No text on page to revise in Microsoft Word."));
			Microsoft.Office.Interop.Word.Application app =
				new Microsoft.Office.Interop.Word.Application();
			app.Visible = true;
			object mv = System.Reflection.Missing.Value;
			object template = mv;
			object newTemplate = mv;
			object documentType = Microsoft.Office.Interop.Word.WdDocumentType.wdTypeDocument;
			object visible = true;
			Microsoft.Office.Interop.Word.Document doc =
				app.Documents.Add(ref template, ref newTemplate, ref documentType, ref visible);
			doc.Words.First.InsertBefore(text);
			text = null;
			Microsoft.Office.Interop.Word.DocumentEvents_Event docEvents =
				(Microsoft.Office.Interop.Word.DocumentEvents_Event) doc;
			docEvents.Close +=
				new Microsoft.Office.Interop.Word.DocumentEvents_CloseEventHandler(
					delegate { text = doc.Range(ref mv, ref mv).Text; doc.Saved = true; } );
			app.Activate();
			while (text == null);
			text = Regex.Replace(text, "\r(?!\n)", "\r\n");
			app = null;
			doc = null;
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
			GC.WaitForPendingFinalizers();
			Console.WriteLine(
				Bot.Msg("Text of \"{0}\" page was revised in Microsoft Word."), title);
#else

            throw new WikiBotException(Bot.Msg("Page.ReviseInMSWord() function requires MS " +

                "Office PIAs to be installed and referenced. Please, see remarks in function's " +

                "documentation in \"Documentation.chm\" file for additional instructions.\n"));

#endif

        }



        /// <summary>Uploads local image to wiki site. Function also works with non-image files.

        /// Note: uploaded image title (wiki page title) will be the same as title of this Page

        /// object, not the title of source file.</summary>

        /// <param name="filePathName">Path and name of local file.</param>

        /// <param name="description">File (image) description.</param>

        /// <param name="license">File license type (may be template title). Used only on

        /// some wiki sites. Pass empty string, if the wiki site doesn't require it.</param>

        /// <param name="copyStatus">File (image) copy status. Used only on some wiki sites. Pass

        /// empty string, if the wiki site doesn't require it.</param>

        /// <param name="source">File (image) source. Used only on some wiki sites. Pass

        /// empty string, if the wiki site doesn't require it.</param>

        public void UploadImage(string filePathName, string description,

            string license, string copyStatus, string source)
        {

            if (string.IsNullOrEmpty(title))

                throw new WikiBotException(Bot.Msg("No title specified for image to upload."));

            if (!File.Exists(filePathName))

                throw new WikiBotException(

                    string.Format(Bot.Msg("Image file \"{0}\" doesn't exist."), filePathName));

            if (Path.GetFileNameWithoutExtension(filePathName).Length < 3)

                throw new WikiBotException(string.Format(Bot.Msg("Name of file \"{0}\" must " +

                    "contain at least 3 characters (excluding extension) for successful upload."),

                    filePathName));

            Console.WriteLine(Bot.Msg("Uploading image \"{0}\"..."), title);

            string targetName = site.RemoveNSPrefix(title, 6);

            targetName = Bot.Capitalize(targetName);
            string res = site.site + site.indexPath + "index.php?title=" +
                site.namespaces["-1"].ToString() + ":Upload";
            string src = site.GetPageHTM(res);
            editSessionToken = Site.editSessionTokenRE1.Match(src).Groups[1].ToString();
            if (string.IsNullOrEmpty(editSessionToken))
                editSessionToken = Site.editSessionTokenRE2.Match(src).Groups[1].ToString();

            HttpWebRequest webReq = (HttpWebRequest)WebRequest.Create(res);

            webReq.Proxy.Credentials = CredentialCache.DefaultCredentials;

            webReq.UseDefaultCredentials = true;

            webReq.Method = "POST";

            string boundary = "----------" + DateTime.Now.Ticks.ToString("x");

            webReq.ContentType = "multipart/form-data; boundary=" + boundary;

            webReq.UserAgent = Bot.botVer;

            webReq.CookieContainer = site.cookies;

            if (Bot.unsafeHttpHeaderParsingUsed == 0)
            {

                webReq.ProtocolVersion = HttpVersion.Version10;

                webReq.KeepAlive = false;

            }

            webReq.CachePolicy = new System.Net.Cache.HttpRequestCachePolicy(

                System.Net.Cache.HttpRequestCacheLevel.Refresh);

            StringBuilder sb = new StringBuilder();

            string ph = "--" + boundary + "\r\nContent-Disposition: form-data; name=\"";

            sb.Append(ph + "wpIgnoreWarning\"\r\n\r\n1\r\n");

            sb.Append(ph + "wpDestFile\"\r\n\r\n" + targetName + "\r\n");

            sb.Append(ph + "wpUploadAffirm\"\r\n\r\n1\r\n");

            sb.Append(ph + "wpWatchthis\"\r\n\r\n0\r\n");
            sb.Append(ph + "wpEditToken\"\r\n\r\n" + editSessionToken + "\r\n");

            sb.Append(ph + "wpUploadCopyStatus\"\r\n\r\n" + copyStatus + "\r\n");

            sb.Append(ph + "wpUploadSource\"\r\n\r\n" + source + "\r\n");

            sb.Append(ph + "wpUpload\"\r\n\r\n" + "upload bestand" + "\r\n");

            sb.Append(ph + "wpLicense\"\r\n\r\n" + license + "\r\n");

            sb.Append(ph + "wpUploadDescription\"\r\n\r\n" + description + "\r\n");

            sb.Append(ph + "wpUploadFile\"; filename=\"" +

                HttpUtility.UrlEncode(Path.GetFileName(filePathName)) + "\"\r\n" +

                "Content-Type: application/octet-stream\r\n\r\n");

            byte[] postHeaderBytes = Encoding.UTF8.GetBytes(sb.ToString());

            byte[] fileBytes = File.ReadAllBytes(filePathName);

            byte[] boundaryBytes = Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");

            webReq.ContentLength = postHeaderBytes.Length + fileBytes.Length + boundaryBytes.Length;

            Stream reqStream = webReq.GetRequestStream();

            reqStream.Write(postHeaderBytes, 0, postHeaderBytes.Length);

            reqStream.Write(fileBytes, 0, fileBytes.Length);

            reqStream.Write(boundaryBytes, 0, boundaryBytes.Length);

            WebResponse webResp = null;

            for (int errorCounter = 0; true; errorCounter++)
            {

                try
                {

                    webResp = (HttpWebResponse)webReq.GetResponse();

                    break;

                }

                catch (WebException e)
                {

                    string message = e.Message;

                    if (Regex.IsMatch(message, ": \\(50[02349]\\) "))
                    {		// Remote problem

                        if (errorCounter > Bot.retryTimes)

                            throw;

                        Console.Error.WriteLine(message + " " + Bot.Msg("Retrying in 60 seconds."));

                        Thread.Sleep(60000);

                    }

                    else if (message.Contains("Section=ResponseStatusLine"))
                    {	// Squid problem

                        Bot.SwitchUnsafeHttpHeaderParsing(true);

                        UploadImage(filePathName, description, license, copyStatus, source);

                        return;

                    }

                    else

                        throw;

                }

            }

            StreamReader strmReader = new StreamReader(webResp.GetResponseStream());

            string respStr = strmReader.ReadToEnd();

            strmReader.Close();

            webResp.Close();

            if (!respStr.Contains(HttpUtility.HtmlEncode(targetName)))

                throw new WikiBotException(string.Format(

                    Bot.Msg("Error occurred when uploading image \"{0}\"."), title));

            try
            {

                string errorMessage = site.GetMediaWikiMessage("MediaWiki:Uploadcorrupt");

                if (respStr.Contains(errorMessage))

                    throw new WikiBotException(string.Format(

                        Bot.Msg("Error occurred when uploading image \"{0}\"."), title));

            }

            catch (WikiBotException e)
            {

                if (!e.Message.Contains("Uploadcorrupt"))	// skip, if MediaWiki message not found

                    throw;

            }

            title = site.namespaces["6"] + ":" + targetName;

            text = description;

            Console.WriteLine(Bot.Msg("Image \"{0}\" uploaded successfully."), title);

        }



        /// <summary>Uploads web image to wiki site.</summary>

        /// <param name="imageFileUrl">Full URL of image file on the web.</param>

        /// <param name="description">Image description.</param>

        /// <param name="license">Image license type. Used only in some wiki sites. Pass

        /// empty string, if the wiki site doesn't require it.</param>

        /// <param name="copyStatus">Image copy status. Used only in some wiki sites. Pass

        /// empty string, if the wiki site doesn't require it.</param>

        public void UploadImageFromWeb(string imageFileUrl, string description,

            string license, string copyStatus)
        {

            if (string.IsNullOrEmpty(imageFileUrl))

                throw new WikiBotException(Bot.Msg("No URL specified of image to upload."));

            Uri res = new Uri(imageFileUrl);

            Bot.InitWebClient();

            string imageFileName = imageFileUrl.Substring(imageFileUrl.LastIndexOf("/") + 1);

            try
            {

                Bot.wc.DownloadFile(res, "Cache" + Path.DirectorySeparatorChar + imageFileName);

            }

            catch (System.Net.WebException)
            {

                throw new WikiBotException(string.Format(

                    Bot.Msg("Can't access image \"{0}\"."), imageFileUrl));

            }

            if (!File.Exists("Cache" + Path.DirectorySeparatorChar + imageFileName))

                throw new WikiBotException(string.Format(

                    Bot.Msg("Error occurred when downloading image \"{0}\"."), imageFileUrl));

            UploadImage("Cache" + Path.DirectorySeparatorChar + imageFileName,

                description, license, copyStatus, imageFileUrl);

            File.Delete("Cache" + Path.DirectorySeparatorChar + imageFileName);

        }



        /// <summary>Downloads image, audio or video file, pointed by this page title,

        /// from the wiki site. Redirection is resolved automatically.</summary>

        /// <param name="filePathName">Path and name of local file to save image to.</param>

        public void DownloadImage(string filePathName)
        {

            string res = site.site + site.indexPath + "index.php?title=" +

                HttpUtility.UrlEncode(title);

            string src = "";

            try
            {

                src = site.GetPageHTM(res);

            }

            catch (WebException e)
            {

                string message = e.Message;

                if (message.Contains(": (404) "))
                {		// Not Found

                    Console.Error.WriteLine(Bot.Msg("Page \"{0}\" doesn't exist."), title);

                    text = "";

                    return;

                }

                else

                    throw;

            }

            Regex fileLinkRE1 = new Regex("<a href=\"([^\"]+?)\" class=\"internal\"");

            Regex fileLinkRE2 =

                new Regex("<div class=\"fullImageLink\" id=\"file\"><a href=\"([^\"]+?)\"");

            string fileLink = "";

            if (fileLinkRE1.IsMatch(src))

                fileLink = fileLinkRE1.Match(src).Groups[1].ToString();

            else if (fileLinkRE2.IsMatch(src))

                fileLink = fileLinkRE2.Match(src).Groups[1].ToString();

            else

                throw new WikiBotException(string.Format(

                    Bot.Msg("Image \"{0}\" doesn't exist."), title));

            if (!fileLink.StartsWith("http"))

                fileLink = site.site + fileLink;

            Bot.InitWebClient();

            Console.WriteLine(Bot.Msg("Downloading image \"{0}\"..."), title);

            Bot.wc.DownloadFile(fileLink, filePathName);

            Console.WriteLine(Bot.Msg("Image \"{0}\" downloaded successfully."), title);

        }



        /// <summary>Saves page text to the specified file. If the target file already exists,

        /// it is overwritten.</summary>

        /// <param name="filePathName">Path and name of the file.</param>

        public void SaveToFile(string filePathName)
        {

            if (IsEmpty())
            {

                Console.Error.WriteLine(Bot.Msg("Page \"{0}\" contains no text to save."), title);

                return;

            }

            File.WriteAllText(filePathName, text, Encoding.UTF8);

            Console.WriteLine(Bot.Msg("Text of \"{0}\" page successfully saved in \"{1}\" file."),

                title, filePathName);

        }



        /// <summary>Saves page text to the ".txt" file in current directory.

        /// Use Directory.SetCurrentDirectory function to change the current directory (but don't

        /// forget to change it back after saving file). The name of the file is constructed

        /// from the title of the article. Forbidden characters in filenames are replaced

        /// with their Unicode numeric codes (also known as numeric character references

        /// or NCRs).</summary>

        public void SaveToFile()
        {

            string fileTitle = title;

            //Path.GetInvalidFileNameChars();

            fileTitle = fileTitle.Replace("\"", "&#x22;");

            fileTitle = fileTitle.Replace("<", "&#x3c;");

            fileTitle = fileTitle.Replace(">", "&#x3e;");

            fileTitle = fileTitle.Replace("?", "&#x3f;");

            fileTitle = fileTitle.Replace(":", "&#x3a;");

            fileTitle = fileTitle.Replace("\\", "&#x5c;");

            fileTitle = fileTitle.Replace("/", "&#x2f;");

            fileTitle = fileTitle.Replace("*", "&#x2a;");

            fileTitle = fileTitle.Replace("|", "&#x7c;");

            SaveToFile(fileTitle + ".txt");

        }



        /// <summary>Returns true, if page.text field is empty. Don't forget to call

        /// page.Load() before using this function.</summary>

        /// <returns>Returns bool value.</returns>

        public bool IsEmpty()
        {

            return string.IsNullOrEmpty(text);

        }



        /// <summary>Returns true, if page.text field is not empty. Don't forget to call

        /// Load() or LoadEx() before using this function.</summary>

        /// <returns>Returns bool value.</returns>

        public bool Exists()
        {

            return (string.IsNullOrEmpty(text) == true) ? false : true;

        }



        /// <summary>Returns true, if page redirects to another page. Don't forget to load

        /// actual page contents from live wiki "Page.Load()" before using this function.</summary>

        /// <returns>Returns bool value.</returns>

        public bool IsRedirect()
        {

            if (!Exists())

                return false;

            return site.redirectRE.IsMatch(text);

        }



        /// <summary>Returns redirection target. Don't forget to load

        /// actual page contents from live wiki "Page.Load()" before using this function.</summary>

        /// <returns>Returns redirection target page title as string. Or empty string, if this

        /// Page object does not redirect anywhere.</returns>

        public string RedirectsTo()
        {

            if (IsRedirect())

                return site.redirectRE.Match(text).Groups[1].ToString().Trim();

            else

                return string.Empty;

        }



        /// <summary>If this page is a redirection, this function loads the title and text

        /// of redirected-to page into this Page object.</summary>

        public void ResolveRedirect()
        {

            if (IsRedirect())
            {

                lastRevisionID = null;

                title = RedirectsTo();

                Load();

            }

        }



        /// <summary>Returns true, if this page is a disambiguation page. Don't forget to load

        /// actual page contents from live wiki  before using this function. Local redirect

        /// templates of Wikimedia sites are also recognized, but if this extended functionality

        /// is undesirable, then just set appropriate disambiguation template's title in

        /// "disambigStr" variable of Site object. Use "|" as a delimiter when enumerating

        /// several templates in "disambigStr" variable.</summary>

        /// <returns>Returns bool value.</returns>

        public bool IsDisambig()
        {

            if (string.IsNullOrEmpty(text))

                return false;

            if (!string.IsNullOrEmpty(site.disambigStr))

                return Regex.IsMatch(text, @"(?i)\{\{(" + site.disambigStr + ")}}");

            Console.WriteLine(Bot.Msg("Initializing disambiguation template tags..."));

            site.disambigStr = "disambiguation|disambig|dab";

            Uri res = new Uri("http://en.wikipedia.org/w/index.php?title=Template:Disambig/doc" +

                "&action=raw&ctype=text/plain&dontcountme=s");

            string buffer = text;

            text = Bot.GetWebResource(res, "");

            string[] iw = GetInterWikiLinks();

            foreach (string s in iw)

                if (s.StartsWith(site.language + ":"))
                {

                    site.disambigStr += "|" + s.Substring(s.LastIndexOf(":") + 1,

                        s.Length - s.LastIndexOf(":") - 1);

                    break;

                }

            text = buffer;

            return Regex.IsMatch(text, @"(?i)\{\{(" + site.disambigStr + ")}}");

        }



        /// <summary>This internal function removes the namespace prefix from page title.</summary>

        public void RemoveNSPrefix()
        {

            title = site.RemoveNSPrefix(title, 0);

        }



        /// <summary>Function changes default English namespace prefixes to correct local prefixes

        /// (e.g. for German wiki sites it changes "Category:..." to "Kategorie:...").</summary>

        public void CorrectNSPrefix()
        {

            title = site.CorrectNSPrefix(title);

        }



        /// <summary>Returns the array of strings, containing all wikilinks ([[...]])

        /// found in page text, excluding links in image descriptions, but including

        /// interwiki links, links to sister projects, categories, images, etc.</summary>

        /// <returns>Returns raw links in strings array.</returns>

        public string[] GetAllLinks()
        {

            MatchCollection matches = Site.wikiLinkRE.Matches(text);

            string[] matchStrings = new string[matches.Count];

            for (int i = 0; i < matches.Count; i++)

                matchStrings[i] = matches[i].Groups[1].Value;

            return matchStrings;

        }



        /// <summary>Finds all internal wikilinks in page text, excluding interwiki

        /// links, links to sister projects, categories, embedded images and links in

        /// image descriptions.</summary>

        /// <returns>Returns the PageList object, in which page titles are the wikilinks,

        /// found in text.</returns>

        public PageList GetLinks()
        {

            MatchCollection matches = Site.wikiLinkRE.Matches(text);

            StringCollection exclLinks = new StringCollection();

            exclLinks.AddRange(GetInterWikiLinks());

            exclLinks.AddRange(GetSisterWikiLinks(true));

            string str;

            int fragmentPosition;

            PageList pl = new PageList(site);

            for (int i = 0; i < matches.Count; i++)
            {

                str = matches[i].Groups[1].Value;

                if (str.StartsWith(site.namespaces["6"] + ":", true, site.langCulture) ||

                    str.StartsWith(Site.wikiNSpaces["6"] + ":", true, site.langCulture) ||

                    str.StartsWith(site.namespaces["14"] + ":", true, site.langCulture) ||

                    str.StartsWith(Site.wikiNSpaces["14"] + ":", true, site.langCulture))

                    continue;

                str = str.TrimStart(':');

                if (exclLinks.Contains(str))

                    continue;

                fragmentPosition = str.IndexOf("#");

                if (fragmentPosition != -1)

                    str = str.Substring(0, fragmentPosition);

                pl.Add(new Page(site, str));

            }

            return pl;

        }



        /// <summary>Returns the array of strings, containing external links,

        /// found in page text.</summary>

        /// <returns>Returns the string[] array.</returns>

        public string[] GetExternalLinks()
        {

            MatchCollection matches = Site.webLinkRE.Matches(text);

            string[] matchStrings = new string[matches.Count];

            for (int i = 0; i < matches.Count; i++)

                matchStrings[i] = matches[i].Value;

            return matchStrings;

        }



        /// <summary>Returns the array of strings, containing interwiki links,

        /// found in page text. But no displayed links are returned,

        /// like [[:de:Stern]] - these are returned by GetSisterWikiLinks(true)

        /// function. Interwiki links are returned without square brackets.</summary>

        /// <returns>Returns the string[] array.</returns>

        public string[] GetInterWikiLinks()
        {

            return GetInterWikiLinks(false);

        }



        /// <summary>Returns the array of strings, containing interwiki links,

        /// found in page text. Displayed links like [[:de:Stern]] are not returned,

        /// these are returned by GetSisterWikiLinks(true) function.</summary>

        /// <param name="inSquareBrackets">Pass "true" to get interwiki links

        ///in square brackets, for example "[[de:Stern]]", otherwise the result

        /// will be like "de:Stern", without brackets.</param>

        /// <returns>Returns the string[] array.</returns>

        public string[] GetInterWikiLinks(bool inSquareBrackets)
        {

            if (string.IsNullOrEmpty(Site.WMLangsStr))

                site.GetWikimediaWikisList();

            MatchCollection matches = Site.iwikiLinkRE.Matches(text);

            string[] matchStrings = new string[matches.Count];

            for (int i = 0; i < matches.Count; i++)
            {

                matchStrings[i] = matches[i].Groups[1].Value;

                if (inSquareBrackets)

                    matchStrings[i] = "[[" + matchStrings[i] + "]]";

            }

            return matchStrings;

        }



        /// <summary>Adds interwiki links to the page. It doesn't remove or replace old

        /// interwiki links, this can be done by calling RemoveInterWikiLinks function

        /// or manually, if necessary.</summary>

        /// <param name="iwikiLinks">Interwiki links as an array of strings, with or

        /// without square brackets, for example: "de:Stern" or "[[de:Stern]]".</param>

        public void AddInterWikiLinks(string[] iwikiLinks)
        {

            if (iwikiLinks.Length == 0)

                throw new ArgumentNullException("iwikiLinks");

            List<string> iwList = new List<string>(iwikiLinks);

            AddInterWikiLinks(iwList);

        }



        /// <summary>Adds interwiki links to the page. It doesn't remove or replace old

        /// interwiki links, this can be done by calling RemoveInterWikiLinks function

        /// or manually, if necessary.</summary>

        /// <param name="iwikiLinks">Interwiki links as List of strings, with or

        /// without square brackets, for example: "de:Stern" or "[[de:Stern]]".</param>

        public void AddInterWikiLinks(List<string> iwikiLinks)
        {

            if (iwikiLinks.Count == 0)

                throw new ArgumentNullException("iwikiLinks");

            if (iwikiLinks.Count == 1 && iwikiLinks[0] == null)

                iwikiLinks.Clear();

            for (int i = 0; i < iwikiLinks.Count; i++)

                iwikiLinks[i] = iwikiLinks[i].Trim("[]\f\n\r\t\v ".ToCharArray());

            iwikiLinks.AddRange(GetInterWikiLinks());

            SortInterWikiLinks(ref iwikiLinks);

            RemoveInterWikiLinks();

            text += "\r\n";

            foreach (string str in iwikiLinks)

                text += "\r\n[[" + str + "]]";

        }



        /// <summary>Sorts interwiki links in page text according to site rules.

        /// Only rules for some Wikipedia projects are implemented so far.

        /// In other cases links are ordered alphabetically.</summary>

        public void SortInterWikiLinks()
        {

            AddInterWikiLinks(new string[] { null });

        }



        /// <summary>This internal function sorts interwiki links in page text according 

        /// to site rules. Only rules for some Wikipedia projects are implemented

        /// so far. In other cases links are ordered alphabetically.</summary>

        /// <param name="iwList">Interwiki links without square brackets in

        /// List object, either ordered or unordered.</param>

        public void SortInterWikiLinks(ref List<string> iwList)
        {

            string[] iwikiLinksOrder = null;

            if (iwList.Count < 2)

                return;

            switch (site.site)
            {		// special sort orders

                case "http://en.wikipedia.org":

                case "http://simple.wikipedia.org":

                case "http://be-x-old.wikipedia.org":

                case "http://lb.wikipedia.org":

                case "http://mk.wikipedia.org":

                case "http://no.wikipedia.org":

                case "http://pl.wikipedia.org": iwikiLinksOrder = Site.iwikiLinksOrderByLocal; break;

                case "http://ms.wikipedia.org":

                case "http://et.wikipedia.org":

                case "http://vi.wikipedia.org":

                case "http://fi.wikipedia.org": iwikiLinksOrder = Site.iwikiLinksOrderByLocalFW; break;

                case "http://sr.wikipedia.org": iwikiLinksOrder = Site.iwikiLinksOrderByLatinFW; break;

                default: iwList.Sort(); break;

            }

            if (iwikiLinksOrder == null)

                return;

            List<string> sortedIwikiList = new List<string>();

            string prefix;

            foreach (string iwikiLang in iwikiLinksOrder)
            {

                prefix = iwikiLang + ":";

                foreach (string iwikiLink in iwList)

                    if (iwikiLink.StartsWith(prefix))

                        sortedIwikiList.Add(iwikiLink);

            }

            foreach (string iwikiLink in iwList)

                if (!sortedIwikiList.Contains(iwikiLink))

                    sortedIwikiList.Add(iwikiLink);

            iwList = sortedIwikiList;

            switch (site.site)
            {		// special sort orders, based on default iwList.Sort();

                case "http://hu.wikipedia.org":

                case "http://he.wikipedia.org": iwList.Remove("en"); iwList.Insert(0, "en"); break;

                case "http://nn.wikipedia.org":

                    iwList.Remove("no"); iwList.Remove("sv"); iwList.Remove("da");

                    iwList.InsertRange(0, new string[] { "no", "sv", "da" }); break;

                case "http://te.wikipedia.org":

                    iwList.Remove("en"); iwList.Remove("hi"); iwList.Remove("kn");

                    iwList.Remove("ta"); iwList.Remove("ml");

                    iwList.InsertRange(0, new string[] { "en", "hi", "kn", "ta", "ml" }); break;

                case "http://yi.wikipedia.org":

                    iwList.Remove("en"); iwList.Remove("he"); iwList.Remove("de");

                    iwList.InsertRange(0, new string[] { "en", "he", "de" }); break;

                case "http://ur.wikipedia.org":

                    iwList.Remove("ar"); iwList.Remove("fa"); iwList.Remove("en");

                    iwList.InsertRange(0, new string[] { "ar", "fa", "en" }); break;

            }

        }



        /// <summary>Removes all interwiki links from text of page.</summary>

        public void RemoveInterWikiLinks()
        {

            if (string.IsNullOrEmpty(Site.WMLangsStr))

                site.GetWikimediaWikisList();

            text = Site.iwikiLinkRE.Replace(text, "");

            text = text.TrimEnd("\r\n".ToCharArray());

        }



        /// <summary>Returns the array of strings, containing links to sister Wikimedia

        /// Foundation Projects, found in page text.</summary>

        /// <param name="includeDisplayedInterWikiLinks">Include displayed interwiki

        /// links like "[[:de:Stern]]".</param>

        /// <returns>Returns the string[] array.</returns>

        public string[] GetSisterWikiLinks(bool includeDisplayedInterWikiLinks)
        {

            if (string.IsNullOrEmpty(Site.WMLangsStr))

                site.GetWikimediaWikisList();

            MatchCollection sisterMatches = Site.sisterWikiLinkRE.Matches(text);

            MatchCollection iwikiMatches = Site.iwikiDispLinkRE.Matches(text);

            int size = (includeDisplayedInterWikiLinks == true) ?

                sisterMatches.Count + iwikiMatches.Count : sisterMatches.Count;

            string[] matchStrings = new string[size];

            int i = 0;

            for (; i < sisterMatches.Count; i++)

                matchStrings[i] = sisterMatches[i].Groups[1].Value;

            if (includeDisplayedInterWikiLinks == true)

                for (int j = 0; j < iwikiMatches.Count; i++, j++)

                    matchStrings[i] = iwikiMatches[j].Groups[1].Value;

            return matchStrings;

        }



        /// <summary>Function converts basic HTML markup in page text to wiki

        /// markup, except for tables markup, that is left unchanged. Use

        /// ConvertHtmlTablesToWikiTables function to convert HTML

        /// tables markup to wiki format.</summary>

        public void ConvertHtmlMarkupToWikiMarkup()
        {

            text = Regex.Replace(text, "(?is)n?<(h1)( [^/>]+?)?>(.+?)</\\1>n?", "\n= $3 =\n");

            text = Regex.Replace(text, "(?is)n?<(h2)( [^/>]+?)?>(.+?)</\\1>n?", "\n== $3 ==\n");

            text = Regex.Replace(text, "(?is)n?<(h3)( [^/>]+?)?>(.+?)</\\1>n?", "\n=== $3 ===\n");

            text = Regex.Replace(text, "(?is)n?<(h4)( [^/>]+?)?>(.+?)</\\1>n?", "\n==== $3 ====\n");

            text = Regex.Replace(text, "(?is)n?<(h5)( [^/>]+?)?>(.+?)</\\1>n?",

                "\n===== $3 =====\n");

            text = Regex.Replace(text, "(?is)n?<(h6)( [^/>]+?)?>(.+?)</\\1>n?",

                "\n====== $3 ======\n");

            text = Regex.Replace(text, "(?is)\n?\n?<p( [^/>]+?)?>(.+?)</p>", "\n\n$2");

            text = Regex.Replace(text, "(?is)<a href ?= ?[\"'](http:[^\"']+)[\"']>(.+?)</a>",

                "[$1 $2]");

            text = Regex.Replace(text, "(?i)</?(b|strong)>", "'''");

            text = Regex.Replace(text, "(?i)</?(i|em)>", "''");

            text = Regex.Replace(text, "(?i)\n?<hr ?/?>\n?", "\n----\n");

            text = Regex.Replace(text, "(?i)<(hr|br)( [^/>]+?)? ?/?>", "<$1$2 />");

        }



        /// <summary>Function converts HTML table markup in page text to wiki

        /// table markup.</summary>

        public void ConvertHtmlTablesToWikiTables()
        {

            if (!text.Contains("</table>"))

                return;

            text = Regex.Replace(text, ">\\s+<", "><");

            text = Regex.Replace(text, "<table( ?[^>]*)>", "\n{|$1\n");

            text = Regex.Replace(text, "</table>", "|}\n");

            text = Regex.Replace(text, "<caption( ?[^>]*)>", "|+$1 | ");

            text = Regex.Replace(text, "</caption>", "\n");

            text = Regex.Replace(text, "<tr( ?[^>]*)>", "|-$1\n");

            text = Regex.Replace(text, "</tr>", "\n");

            text = Regex.Replace(text, "<th([^>]*)>", "!$1 | ");

            text = Regex.Replace(text, "</th>", "\n");

            text = Regex.Replace(text, "<td([^>]*)>", "|$1 | ");

            text = Regex.Replace(text, "</td>", "\n");

            text = Regex.Replace(text, "\n(\\||\\|\\+|!) \\| ", "\n$1 ");

            text = text.Replace("\n\n|", "\n|");

        }



        /// <summary>Returns the array of strings, containing category names found in

        /// page text with namespace prefix, but without sorting keys. Use the result

        /// strings to call FillFromCategory(string) or FillFromCategoryTree(string)

        /// function. Categories, added by templates, are not returned. Use GetAllCategories

        /// function to get such categories too.</summary>

        /// <returns>Returns the string[] array.</returns>

        public string[] GetCategories()
        {

            return GetCategories(true, false);

        }



        /// <summary>Returns the array of strings, containing category names found in

        /// page text. Categories, added by templates, are not returned. Use GetAllCategories

        /// function to get categories added by templates too.</summary>

        /// <param name="withNameSpacePrefix">If true, function returns strings with

        /// namespace prefix like "Category:Stars", not just "Stars".</param>

        /// <param name="withSortKey">If true, function returns strings with sort keys,

        /// if found. Like "Stars|D3" (in [[Category:Stars|D3]]), not just "Stars".</param>

        /// <returns>Returns the string[] array.</returns>

        public string[] GetCategories(bool withNameSpacePrefix, bool withSortKey)
        {

            MatchCollection matches = site.wikiCategoryRE.Matches(

                Regex.Replace(text, "(?is)<nowiki>.+?</nowiki>", ""));

            string[] matchStrings = new string[matches.Count];

            for (int i = 0; i < matches.Count; i++)
            {

                matchStrings[i] = matches[i].Groups[4].Value.Trim();

                if (withSortKey == true)

                    matchStrings[i] += matches[i].Groups[5].Value.Trim();

                if (withNameSpacePrefix == true)

                    matchStrings[i] = site.namespaces["14"] + ":" + matchStrings[i];

            }

            return matchStrings;

        }



        /// <summary>Returns the array of strings, containing category names found in

        /// page text and added by page's templates. Categories are returned  with

        /// namespace prefix, but without sorting keys. Use the result strings

        /// to call FillFromCategory(string) or FillFromCategoryTree(string).</summary>

        /// <returns>Returns the string[] array.</returns>

        public string[] GetAllCategories()
        {

            return GetAllCategories(true);

        }



        /// <summary>Returns the array of strings, containing category names found in

        /// page text and added by page's templates.</summary>

        /// <param name="withNameSpacePrefix">If true, function returns strings with

        /// namespace prefix like "Category:Stars", not just "Stars".</param>

        /// <returns>Returns the string[] array.</returns>

        public string[] GetAllCategories(bool withNameSpacePrefix)
        {

            string uri;

            if (Bot.useBotQuery == true && site.botQuery == true && site.ver >= new Version(1, 15))

                uri = site.site + site.indexPath +

                    "api.php?action=query&prop=categories" +

                    "&clprop=sortkey|hidden&cllimit=5000&format=xml&titles=" +

                    HttpUtility.UrlEncode(title);

            else

                uri = site.site + site.indexPath + "index.php?title=" +

                    HttpUtility.UrlEncode(title) + "&redirect=no";



            string xpathQuery;

            if (Bot.useBotQuery == true && site.botQuery == true && site.ver >= new Version(1, 15))

                xpathQuery = "//categories/cl/@title";

            else if (site.ver >= new Version(1, 13))

                xpathQuery = "//ns:div[ @id='mw-normal-catlinks' or @id='mw-hidden-catlinks' ]" +

                    "/ns:span/ns:a";

            else

                xpathQuery = "//ns:div[ @id='catlinks' ]/ns:p/ns:span/ns:a";



            string src = site.GetPageHTM(uri);

            if (Bot.useBotQuery != true || site.botQuery != true || site.ver < new Version(1, 15))
            {

                int startPos = src.IndexOf("<!-- start content -->");

                int endPos = src.IndexOf("<!-- end content -->");

                if (startPos != -1 && endPos != -1 && startPos < endPos)

                    src = src.Remove(startPos, endPos - startPos);

                else
                {

                    startPos = src.IndexOf("<!-- bodytext -->");

                    endPos = src.IndexOf("<!-- /bodytext -->");

                    if (startPos != -1 && endPos != -1 && startPos < endPos)

                        src = src.Remove(startPos, endPos - startPos);

                }

            }



            XPathNodeIterator iterator = site.GetXMLIterator(src, xpathQuery);

            string[] matchStrings = new string[iterator.Count];

            iterator.MoveNext();

            for (int i = 0; i < iterator.Count; i++)
            {

                matchStrings[i] = (withNameSpacePrefix ? site.namespaces["14"] + ":" : "") +

                    site.RemoveNSPrefix(HttpUtility.HtmlDecode(iterator.Current.Value), 14);

                iterator.MoveNext();

            }



            return matchStrings;

        }



        /// <summary>Adds the page to the specified category by adding

        /// link to that category in page text. If the link to the specified category

        /// already exists, the function does nothing.</summary>

        /// <param name="categoryName">Category name, with or without prefix.

        /// Sort key can also be included after "|", like "Category:Stars|D3".</param>

        public void AddToCategory(string categoryName)
        {

            if (string.IsNullOrEmpty(categoryName))

                throw new ArgumentNullException("categoryName");

            categoryName = site.RemoveNSPrefix(categoryName, 14);

            string cleanCategoryName = !categoryName.Contains("|") ? categoryName.Trim()

                : categoryName.Substring(0, categoryName.IndexOf('|')).Trim();

            string[] categories = GetCategories(false, false);

            foreach (string category in categories)

                if (category == Bot.Capitalize(cleanCategoryName) ||

                    category == Bot.Uncapitalize(cleanCategoryName))

                    return;

            string[] iw = GetInterWikiLinks();

            RemoveInterWikiLinks();

            text += (categories.Length == 0 ? "\r\n" : "") +

                "\r\n[[" + site.namespaces["14"] + ":" + categoryName + "]]\r\n";

            if (iw.Length != 0)

                AddInterWikiLinks(iw);

            text = text.TrimEnd("\r\n".ToCharArray());

        }



        /// <summary>Removes the page from category by deleting link to that category in

        /// page text.</summary>

        /// <param name="categoryName">Category name, with or without prefix.</param>

        public void RemoveFromCategory(string categoryName)
        {

            if (string.IsNullOrEmpty(categoryName))

                throw new ArgumentNullException("categoryName");

            categoryName = site.RemoveNSPrefix(categoryName, 14).Trim();

            categoryName = !categoryName.Contains("|") ? categoryName

                : categoryName.Substring(0, categoryName.IndexOf('|'));

            string[] categories = GetCategories(false, false);

            if (Array.IndexOf(categories, Bot.Capitalize(categoryName)) == -1 &&

                Array.IndexOf(categories, Bot.Uncapitalize(categoryName)) == -1)

                return;

            string regexCategoryName = Regex.Escape(categoryName);

            regexCategoryName = regexCategoryName.Replace("_", "\\ ").Replace("\\ ", "[_\\ ]");

            int firstCharIndex = (regexCategoryName[0] == '\\') ? 1 : 0;

            regexCategoryName = "[" + char.ToLower(regexCategoryName[firstCharIndex]) +

                char.ToUpper(regexCategoryName[firstCharIndex]) + "]" +

                regexCategoryName.Substring(firstCharIndex + 1);

            text = Regex.Replace(text, @"\[\[((?i)" + site.namespaces["14"] + "|" +

                Site.wikiNSpaces["14"] + "): ?" + regexCategoryName + @"(\|.*?)?]]\r?\n?", "");

            text = text.TrimEnd("\r\n".ToCharArray());

        }



        /// <summary>Returns the array of strings, containing titles of templates, found on page.

        /// The "msgnw:" template modifier is not returned.

        /// Links to templates (like [[:Template:...]]) are not returned. Templates,

        /// mentioned inside &lt;nowiki&gt;&lt;/nowiki&gt; tags are also not returned. The

        /// "magic words" (see http://meta.wikimedia.org/wiki/Help:Magic_words) are recognized and

        /// not returned by this function as templates. When using this function on text of the

        /// template, parameters names and numbers (like {{{link}}} and {{{1}}}) are not returned

        /// by this function as templates too.</summary>

        /// <param name="withNameSpacePrefix">If true, function returns strings with

        /// namespace prefix like "Template:SomeTemplate", not just "SomeTemplate".</param>

        /// <returns>Returns the string[] array. Duplicates are possible.</returns>

        public string[] GetTemplates(bool withNameSpacePrefix)
        {

            string str = Site.noWikiMarkupRE.Replace(text, "");

            if (GetNamespace() == 10)

                str = Regex.Replace(str, @"\{\{\{.*?}}}", "");

            MatchCollection matches = Regex.Matches(str, @"(?s)\{\{(.+?)(}}|\|)");

            string[] matchStrings = new string[matches.Count];

            string match = "", matchLowerCase = "";

            int j = 0;

            for (int i = 0; i < matches.Count; i++)
            {

                match = matches[i].Groups[1].Value;

                matchLowerCase = match.ToLower();

                foreach (string mediaWikiVar in Site.mediaWikiVars)

                    if (matchLowerCase == mediaWikiVar)
                    {

                        match = "";

                        break;

                    }

                if (string.IsNullOrEmpty(match))

                    continue;

                foreach (string parserFunction in Site.parserFunctions)

                    if (matchLowerCase.StartsWith(parserFunction))
                    {

                        match = "";

                        break;

                    }

                if (string.IsNullOrEmpty(match))

                    continue;

                if (match.StartsWith("msgnw:") && match.Length > 6)

                    match = match.Substring(6);

                match = site.RemoveNSPrefix(match, 10).Trim();

                if (withNameSpacePrefix)

                    matchStrings[j++] = site.namespaces["10"] + ":" + match;

                else

                    matchStrings[j++] = match;

            }

            Array.Resize(ref matchStrings, j);

            return matchStrings;

        }



        /// <summary>Returns the array of strings, containing templates, found on page

        /// Everything inside braces is returned with all parameters

        /// untouched. Links to templates (like [[:Template:...]]) are not returned. Templates,

        /// mentioned inside &lt;nowiki&gt;&lt;/nowiki&gt; tags are also not returned. The

        /// "magic words" (see http://meta.wikimedia.org/wiki/Help:Magic_words) are recognized and

        /// not returned by this function as templates. When using this function on text of the

        /// template (on [[Template:NNN]] page), parameters names and numbers (like {{{link}}} 

        /// and {{{1}}}) are not returned by this function as templates too.</summary>

        /// <returns>Returns the string[] array.</returns>

        public string[] GetTemplatesWithParams()
        {

            Dictionary<int, int> templPos = new Dictionary<int, int>();

            StringCollection templates = new StringCollection();

            int startPos, endPos, len = 0;

            string str = text;

            while ((startPos = str.LastIndexOf("{{")) != -1)
            {

                endPos = str.IndexOf("}}", startPos);

                len = (endPos != -1) ? endPos - startPos + 2 : 2;

                if (len != 2)

                    templPos.Add(startPos, len);

                str = str.Remove(startPos, len);

                str = str.Insert(startPos, new String('_', len));

            }

            string[] templTitles = GetTemplates(false);

            Array.Reverse(templTitles);

            foreach (KeyValuePair<int, int> pos in templPos)

                templates.Add(text.Substring(pos.Key + 2, pos.Value - 4));

            for (int i = 0; i < templTitles.Length; i++)

                while (i < templates.Count &&

                    !templates[i].StartsWith(templTitles[i]) &&

                    !templates[i].StartsWith(site.namespaces["10"].ToString() + ":" +

                        templTitles[i], true, site.langCulture) &&

                    !templates[i].StartsWith(Site.wikiNSpaces["10"].ToString() + ":" +

                        templTitles[i], true, site.langCulture) &&

                    !templates[i].StartsWith("msgnw:" + templTitles[i]))

                    templates.RemoveAt(i);

            string[] arr = new string[templates.Count];

            templates.CopyTo(arr, 0);

            Array.Reverse(arr);

            return arr;

        }



        /// <summary>Adds a specified template to the end of the page text

        /// (right before categories).</summary>

        /// <param name="templateText">Complete template in double brackets,

        /// e.g. "{{TemplateTitle|param1=val1|param2=val2}}".</param>

        public void AddTemplate(string templateText)
        {

            if (string.IsNullOrEmpty(templateText))

                throw new ArgumentNullException("templateText");

            Regex templateInsertion = new Regex("([^}]\n|}})\n*\\[\\[((?i)" +

                Regex.Escape(site.namespaces["14"].ToString()) + "|" +

                Regex.Escape(Site.wikiNSpaces["14"].ToString()) + "):");

            if (templateInsertion.IsMatch(text))

                text = templateInsertion.Replace(text, "$1\n" + templateText + "\n\n[[" +

                    site.namespaces["14"] + ":", 1);

            else
            {

                string[] iw = GetInterWikiLinks();

                RemoveInterWikiLinks();

                text += "\n\n" + templateText;

                if (iw.Length != 0)

                    AddInterWikiLinks(iw);

                text = text.TrimEnd("\r\n".ToCharArray());

            }

        }



        /// <summary>Removes all instances of a specified template from page text.</summary>

        /// <param name="templateTitle">Title of template to remove.</param>

        public void RemoveTemplate(string templateTitle)
        {

            if (string.IsNullOrEmpty(templateTitle))

                throw new ArgumentNullException("templateTitle");

            templateTitle = Regex.Escape(templateTitle);

            templateTitle = "(" + Char.ToUpper(templateTitle[0]) + "|" +

                Char.ToLower(templateTitle[0]) + ")" +

                (templateTitle.Length > 1 ? templateTitle.Substring(1) : "");

            text = Regex.Replace(text, @"(?s)\{\{\s*" + templateTitle +

                @"(.*?)}}\r?\n?", "");

        }



        /// <summary>Returns specified parameter of a specified template. If several instances

        /// of specified template are found in text of this page, all parameter values

        /// are returned.</summary>

        /// <param name="templateTitle">Title of template to get parameter of.</param>

        /// <param name="templateParameter">Title of template's parameter. If parameter is

        /// untitled, specify it's number as string. If parameter is titled, but it's number is

        /// specified, the function will return empty List &lt;string&gt; object.</param>

        /// <returns>Returns the List &lt;string&gt; object with strings, containing values of

        /// specified parameters in all found template instances. Returns empty List &lt;string&gt;

        /// object if no specified template parameters were found.</returns>

        public List<string> GetTemplateParameter(string templateTitle, string templateParameter)
        {

            if (string.IsNullOrEmpty(templateTitle))

                throw new ArgumentNullException("templateTitle");

            if (string.IsNullOrEmpty(templateParameter))

                throw new ArgumentNullException("templateParameter");

            if (string.IsNullOrEmpty(text))

                throw new ArgumentNullException("text");



            List<string> parameterValues = new List<string>();

            Dictionary<string, string> parameters;

            templateTitle = templateTitle.Trim();

            templateParameter = templateParameter.Trim();

            Regex templateTitleRegex = new Regex("^\\s*(" +

                Bot.Capitalize(Regex.Escape(templateTitle)) + "|" +

                Bot.Uncapitalize(Regex.Escape(templateTitle)) +

                ")\\s*\\|");

            foreach (string template in GetTemplatesWithParams())
            {

                if (templateTitleRegex.IsMatch(template))
                {

                    parameters = site.ParseTemplate(template);

                    if (parameters.ContainsKey(templateParameter))

                        parameterValues.Add(parameters[templateParameter]);

                }

            }

            return parameterValues;

        }



        /// <summary>This helper method returns specified parameter of a first found instance of

        /// specified template. If no such template or no such parameter was found,

        /// empty string "" is returned.</summary>

        /// <param name="templateTitle">Title of template to get parameter of.</param>

        /// <param name="templateParameter">Title of template's parameter. If parameter is

        /// untitled, specify it's number as string. If parameter is titled, but it's number is

        /// specified, the function will return empty List &lt;string&gt; object.</param>

        /// <returns>Returns parameter as string or empty string "".</returns>

        /// <remarks>Thanks to Eyal Hertzog and metacafe.com team for idea of this

        /// function.</remarks>

        public string GetFirstTemplateParameter(string templateTitle, string templateParameter)
        {

            List<string> paramsList = GetTemplateParameter(templateTitle, templateParameter);

            if (paramsList.Count == 0)

                return "";

            else return paramsList[0];

        }



        /// <summary>Sets the specified parameter of the specified template to new value.

        /// If several instances of specified template are found in text of this page, either

        /// first value can be set, or all values in all instances.</summary>

        /// <param name="templateTitle">Title of template.</param>

        /// <param name="templateParameter">Title of template's parameter.</param>

        /// <param name="newParameterValue">New value to set the parameter to.</param>

        /// <param name="firstTemplateOnly">When set to true, only first found template instance

        /// is modified. When set to false, all found template instances are modified.</param>

        /// <returns>Returns the number of modified values.</returns>

        /// <remarks>Thanks to Eyal Hertzog and metacafe.com team for idea of this

        /// function.</remarks>

        public int SetTemplateParameter(string templateTitle, string templateParameter,

            string newParameterValue, bool firstTemplateOnly)
        {

            if (string.IsNullOrEmpty(templateTitle))

                throw new ArgumentNullException("templateTitle");

            if (string.IsNullOrEmpty(templateParameter))

                throw new ArgumentNullException("templateParameter");

            if (string.IsNullOrEmpty(templateParameter))

                throw new ArgumentNullException("newParameterValue");

            if (string.IsNullOrEmpty(text))

                throw new ArgumentNullException("text");



            int i = 0;

            Dictionary<string, string> parameters;

            templateTitle = templateTitle.Trim();

            templateParameter = templateParameter.Trim();

            Regex templateTitleRegex = new Regex("^\\s*(" +

                Bot.Capitalize(Regex.Escape(templateTitle)) + "|" +

                Bot.Uncapitalize(Regex.Escape(templateTitle)) +

                ")\\s*\\|");

            foreach (string template in GetTemplatesWithParams())
            {

                if (templateTitleRegex.IsMatch(template))
                {

                    parameters = site.ParseTemplate(template);

                    parameters[templateParameter] = newParameterValue;

                    Regex oldTemplate = new Regex(Regex.Escape(template));

                    string newTemplate = site.FormatTemplate(templateTitle, parameters, template);

                    newTemplate = newTemplate.Substring(2, newTemplate.Length - 4);

                    text = oldTemplate.Replace(text, newTemplate, 1);

                    i++;

                    if (firstTemplateOnly == true)

                        break;

                }

            }

            return i;

        }



        /// <summary>Returns the array of strings, containing names of files,

        /// embedded in page, including images in galleries (inside "gallery" tag).

        /// But no links to images and files, like [[:Image:...]] or [[:File:...]] or

        /// [[Media:...]].</summary>

        /// <param name="withNameSpacePrefix">If true, function returns strings with

        /// namespace prefix like "Image:Example.jpg" or "File:Example.jpg",

        /// not just "Example.jpg".</param>

        /// <returns>Returns the string[] array. The array can be empty (of size 0). Strings in

        /// array may recur, indicating that file was mentioned several times on the page.</returns>

        public string[] GetImages(bool withNameSpacePrefix)
        {

            return GetImagesEx(withNameSpacePrefix, false);

        }



        /// <summary>Returns the array of strings, containing names of files,

        /// mentioned on a page.</summary>

        /// <param name="withNameSpacePrefix">If true, function returns strings with

        /// namespace prefix like "Image:Example.jpg" or "File:Example.jpg",

        /// not just "Example.jpg".</param>

        /// <param name="includeFileLinks">If true, function also returns links to images,

        /// like [[:Image:...]] or [[:File:...]] or [[Media:...]]</param>

        /// <returns>Returns the string[] array. The array can be empty (of size 0).Strings in

        /// array may recur, indicating that file was mentioned several times on the page.</returns>

        public string[] GetImagesEx(bool withNameSpacePrefix, bool includeFileLinks)
        {

            if (string.IsNullOrEmpty(text))

                throw new ArgumentNullException("text");

            string nsPrefixes = "File|Image|" + Regex.Escape(site.namespaces["6"].ToString());

            if (includeFileLinks)
            {

                nsPrefixes += "|" + Regex.Escape(site.namespaces["-2"].ToString()) + "|" +

                    Regex.Escape(Site.wikiNSpaces["-2"].ToString());

            }

            MatchCollection matches;

            if (Regex.IsMatch(text, "(?is)<gallery>.*</gallery>"))

                matches = Regex.Matches(text, "(?i)" + (includeFileLinks ? "" : "(?<!:)") +

                    "(" + nsPrefixes + ")(:)(.*?)(\\||\r|\n|]])");		// FIXME: inexact matches

            else

                matches = Regex.Matches(text, @"\[\[" + (includeFileLinks ? ":?" : "") +

                    "(?i)((" + nsPrefixes + @"):(.+?))(\|(.+?))*?]]");

            string[] matchStrings = new string[matches.Count];

            for (int i = 0; i < matches.Count; i++)
            {

                if (withNameSpacePrefix == true)

                    matchStrings[i] = site.namespaces["6"] + ":" + matches[i].Groups[3].Value;

                else

                    matchStrings[i] = matches[i].Groups[3].Value;

            }

            return matchStrings;

        }



        /// <summary>Identifies the namespace of the page.</summary>

        /// <returns>Returns the integer key of the namespace.</returns>

        public int GetNamespace()
        {

            title = title.TrimStart(new char[] { ':' });

            foreach (DictionaryEntry ns in site.namespaces)
            {

                if (title.StartsWith(ns.Value + ":", true, site.langCulture))

                    return int.Parse(ns.Key.ToString());

            }

            foreach (DictionaryEntry ns in Site.wikiNSpaces)
            {

                if (title.StartsWith(ns.Value + ":", true, site.langCulture))

                    return int.Parse(ns.Key.ToString());

            }

            return 0;

        }



        /// <summary>Sends page title to console.</summary>

        public void ShowTitle()
        {

            Console.Write("\n" + Bot.Msg("The title of this page is \"{0}\".") + "\n", title);

        }



        /// <summary>Sends page text to console.</summary>

        public void ShowText()
        {

            Console.Write("\n" + Bot.Msg("The text of \"{0}\" page:"), title);

            Console.Write("\n\n" + text + "\n\n");

        }



        /// <summary>Renames the page.</summary>

        /// <param name="newTitle">New title of that page.</param>

        /// <param name="reason">Reason for renaming.</param>

        public void RenameTo(string newTitle, string reason)
        {

            if (string.IsNullOrEmpty(newTitle))

                throw new ArgumentNullException("newTitle");

            if (string.IsNullOrEmpty(title))

                throw new WikiBotException(Bot.Msg("No title specified for page to rename."));

            //Page mp = new Page(site, "Special:Movepage/" + HttpUtility.UrlEncode(title));

            Page mp = new Page(site, "Special:Movepage/" + title);

            mp.GetEditSessionData();

            if (string.IsNullOrEmpty(mp.editSessionToken))

                throw new WikiBotException(string.Format(

                    Bot.Msg("Unable to rename page \"{0}\" to \"{1}\"."), title, newTitle));

            if (Bot.askConfirm)
            {

                Console.Write("\n\n" +

                    Bot.Msg("The page \"{0}\" is going to be renamed to \"{1}\".\n"),

                    title, newTitle);

                if (!Bot.UserConfirms())

                    return;

            }

            string postData = string.Format("wpNewTitle={0}&wpOldTitle={1}&wpEditToken={2}" +

                "&wpReason={3}", HttpUtility.UrlEncode(newTitle), HttpUtility.UrlEncode(title),

                HttpUtility.UrlEncode(mp.editSessionToken), HttpUtility.UrlEncode(reason));

            string respStr = site.PostDataAndGetResultHTM(site.indexPath +

                "index.php?title=Special:Movepage&action=submit", postData);

            if (Site.editSessionTokenRE2.IsMatch(respStr))

                throw new WikiBotException(string.Format(

                    Bot.Msg("Failed to rename page \"{0}\" to \"{1}\"."), title, newTitle));

            Console.WriteLine(

                Bot.Msg("Page \"{0}\" was successfully renamed to \"{1}\"."), title, newTitle);

            title = newTitle;

        }



        /// <summary>Deletes the page. Sysop rights are needed to delete page.</summary>

        /// <param name="reason">Reason for deleting.</param>

        public void Delete(string reason)
        {

            if (string.IsNullOrEmpty(title))

                throw new WikiBotException(Bot.Msg("No title specified for page to delete."));

            string respStr1 = site.GetPageHTM(site.indexPath + "index.php?title=" +

                HttpUtility.UrlEncode(title) + "&action=delete");

            editSessionToken = Site.editSessionTokenRE1.Match(respStr1).Groups[1].ToString();

            if (string.IsNullOrEmpty(editSessionToken))

                editSessionToken = Site.editSessionTokenRE2.Match(respStr1).Groups[1].ToString();

            if (string.IsNullOrEmpty(editSessionToken))

                throw new WikiBotException(

                    string.Format(Bot.Msg("Unable to delete page \"{0}\"."), title));

            if (Bot.askConfirm)
            {

                Console.Write("\n\n" + Bot.Msg("The page \"{0}\" is going to be deleted.\n"), title);

                if (!Bot.UserConfirms())

                    return;

            }

            string postData = string.Format("wpReason={0}&wpEditToken={1}",

                HttpUtility.UrlEncode(reason), HttpUtility.UrlEncode(editSessionToken));

            string respStr2 = site.PostDataAndGetResultHTM(site.indexPath + "index.php?title=" +

                HttpUtility.UrlEncode(title) + "&action=delete", postData);

            if (Site.editSessionTokenRE2.IsMatch(respStr2))

                throw new WikiBotException(

                    string.Format(Bot.Msg("Failed to delete page \"{0}\"."), title));

            Console.WriteLine(Bot.Msg("Page \"{0}\" was successfully deleted."), title);

            title = "";

        }

    }



    /// <summary>Class defines a set of wiki pages (constructed inside as List object).</summary>

    [ClassInterface(ClassInterfaceType.AutoDispatch)]

    [Serializable]

    public class PageList
    {

        /// <summary>Internal generic List, that contains collection of pages.</summary>

        public List<Page> pages = new List<Page>();

        /// <summary>Site, on which the pages are located.</summary>

        public Site site;



        /// <summary>This constructor creates PageList object with specified Site object and fills

        /// it with Page objects with specified titles. When constructed, new Page objects

        /// in PageList don't contain text. Use Load() method to get text from live wiki,

        /// or use LoadEx() to get both text and metadata via XML export interface.</summary>

        /// <param name="site">Site object, it must be constructed beforehand.</param>

        /// <param name="pageNames">Page titles as array of strings.</param>

        /// <returns>Returns the PageList object.</returns>

        public PageList(Site site, string[] pageNames)
        {

            this.site = site;

            foreach (string pageName in pageNames)

                pages.Add(new Page(site, pageName));

            CorrectNSPrefixes();

        }



        /// <summary>This constructor creates PageList object with specified Site object and fills

        /// it with Page objects with specified titles. When constructed, new Page objects

        /// in PageList don't contain text. Use Load() method to get text from live wiki,

        /// or use LoadEx() to get both text and metadata via XML export interface.</summary>

        /// <param name="site">Site object, it must be constructed beforehand.</param>

        /// <param name="pageNames">Page titles as StringCollection object.</param>

        /// <returns>Returns the PageList object.</returns>

        public PageList(Site site, StringCollection pageNames)
        {

            this.site = site;

            foreach (string pageName in pageNames)

                pages.Add(new Page(site, pageName));

            CorrectNSPrefixes();

        }



        /// <summary>This constructor creates empty PageList object with specified

        /// Site object.</summary>

        /// <param name="site">Site object, it must be constructed beforehand.</param>

        /// <returns>Returns the PageList object.</returns>

        public PageList(Site site)
        {

            this.site = site;

        }



        /// <summary>This constructor creates empty PageList object, Site object with default

        /// properties is created internally and logged in. Constructing new Site object

        /// is too slow, don't use this constructor needlessly.</summary>

        /// <returns>Returns the PageList object.</returns>

        public PageList()
        {

            site = new Site();

        }



        /// <summary>This index allows to call pageList[i] instead of pageList.pages[i].</summary>

        /// <param name="index">Zero-based index.</param>

        /// <returns>Returns the Page object.</returns>

        public Page this[int index]
        {

            get { return pages[index]; }

            set { pages[index] = value; }

        }



        /// <summary>This function allows to access individual pages in this PageList.

        /// But it's better to use simple pageList[i] index, when it is possible.</summary>

        /// <param name="index">Zero-based index.</param>

        /// <returns>Returns the Page object.</returns>

        public Page GetPageAtIndex(int index)
        {

            return pages[index];

        }



        /// <summary>This function allows to set individual pages in this PageList.

        /// But it's better to use simple pageList[i] index, when it is possible.</summary>

        /// <param name="page">Page object to set in this PageList.</param>

        /// <param name="index">Zero-based index.</param>

        /// <returns>Returns the Page object.</returns>

        public void SetPageAtIndex(Page page, int index)
        {

            pages[index] = page;

        }



        /// <summary>This index allows to call pageList["title"]. Don't forget to use correct

        /// local namespace prefixes. Call CorrectNSPrefixes function to correct namespace

        /// prefixes in a whole PageList at once.</summary>

        /// <param name="index">Title of page to get.</param>

        /// <returns>Returns the Page object, or null if there is no page with the specified

        /// title in this PageList.</returns>

        public Page this[string index]
        {

            get
            {

                foreach (Page p in pages)

                    if (p.title == index)

                        return p;

                return null;

            }

            set
            {

                for (int i = 0; i < pages.Count; i++)

                    if (pages[i].title == index)

                        pages[i] = value;

            }

        }



        /// <summary>This standard internal function allows to directly use PageList objects

        /// in "foreach" statements.</summary>

        /// <returns>Returns IEnumerator object.</returns>

        public IEnumerator GetEnumerator()
        {

            return pages.GetEnumerator();

        }



        /// <summary>This function adds specified page to the end of this PageList.</summary>

        /// <param name="page">Page object to add.</param>

        public void Add(Page page)
        {

            pages.Add(page);

        }



        /// <summary>Inserts an element into this PageList at the specified index.</summary>

        /// <param name="page">Page object to insert.</param>

        /// <param name="index">Zero-based index.</param>

        public void Insert(Page page, int index)
        {

            pages.Insert(index, page);

        }



        /// <summary>This function returns true, if in this PageList there exists a page with

        /// the same title, as a page specified as a parameter.</summary>

        /// <param name="page">.</param>

        /// <returns>Returns bool value.</returns>

        public bool Contains(Page page)
        {

            page.CorrectNSPrefix();

            CorrectNSPrefixes();

            foreach (Page p in pages)

                if (p.title == page.title)

                    return true;

            return false;

        }



        /// <summary>This function returns true, if a page with specified title exists

        /// in this PageList.</summary>

        /// <param name="title">Title of page to check.</param>

        /// <returns>Returns bool value.</returns>

        public bool Contains(string title)
        {

            Page page = new Page(site, title);

            page.CorrectNSPrefix();

            CorrectNSPrefixes();

            foreach (Page p in pages)

                if (p.title == page.title)

                    return true;

            return false;

        }



        /// <summary>This function returns the number of pages in PageList.</summary>

        /// <returns>Number of pages as positive integer value.</returns>

        public int Count()
        {

            return pages.Count;

        }



        /// <summary>Removes page at specified index from PageList.</summary>

        /// <param name="index">Zero-based index.</param>

        public void RemoveAt(int index)
        {

            pages.RemoveAt(index);

        }



        /// <summary>Removes a page with specified title from this PageList.</summary>

        /// <param name="title">Title of page to remove.</param>

        public void Remove(string title)
        {

            for (int i = 0; i < Count(); i++)

                if (pages[i].title == title)

                    pages.RemoveAt(i);

        }



        /// <summary>Gets page titles for this PageList from "Special:Allpages" MediaWiki page.

        /// That means a list of pages in alphabetical order.</summary>

        /// <param name="firstPageTitle">Title of page to start enumerating from. The title

        /// must have no namespace prefix (like "Talk:"), just the page title itself. Or you can

        /// specify just a letter or two instead of full real title. Pass the empty string or null

        /// to start from the very beginning.</param>

        /// <param name="neededNSpace">Integer, presenting the key of namespace to get pages

        /// from. Only one key of one namespace can be specified (zero for default).</param>

        /// <param name="acceptRedirects">Set this to "false" to exclude redirects.</param>

        /// <param name="quantity">Maximum allowed quantity of pages in this PageList.</param>

        public void FillFromAllPages(string firstPageTitle, int neededNSpace, bool acceptRedirects,

            int quantity)
        {

            if (quantity <= 0)

                throw new ArgumentOutOfRangeException("quantity",

                    Bot.Msg("Quantity must be positive."));

            if (Bot.useBotQuery == true && site.botQuery == true)
            {

                FillFromCustomBotQueryList("allpages", "apnamespace=" + neededNSpace +

                (acceptRedirects ? "" : "&apfilterredir=nonredirects") +

                (string.IsNullOrEmpty(firstPageTitle) ? "" : "&apfrom=" +

                HttpUtility.UrlEncode(firstPageTitle)), quantity);

                return;

            }


            int count = pages.Count;

            quantity += pages.Count;

            Regex linkToPageRE;

            if (acceptRedirects)

                linkToPageRE = new Regex("<td[^>]*>(?:<div class=\"allpagesredirect\">)?" +

                    "<a href=\"[^\"]*?\" title=\"([^\"]*?)\">");

            else

                linkToPageRE = new Regex("<td[^>]*><a href=\"[^\"]*?\" title=\"([^\"]*?)\">");

            MatchCollection matches;

            do
            {

                string res = site.site + site.indexPath +

                    "index.php?title=Special:Allpages&from=" +

                    HttpUtility.UrlEncode(

                        string.IsNullOrEmpty(firstPageTitle) ? "!" : firstPageTitle) +

                    "&namespace=" + neededNSpace.ToString();

                matches = linkToPageRE.Matches(site.GetPageHTM(res));

                if (matches.Count < 2)

                    break;

                for (int i = 1; i < matches.Count; i++)

                    pages.Add(new Page(site, HttpUtility.HtmlDecode(matches[i].Groups[1].Value)));

                firstPageTitle = site.RemoveNSPrefix(pages[pages.Count - 1].title, neededNSpace) +

                    "!";

            }

            while (pages.Count < quantity);

            if (pages.Count > quantity)

                pages.RemoveRange(quantity, pages.Count - quantity);

            Console.WriteLine(Bot.Msg("PageList filled with {0} page titles from " +

                "\"Special:Allpages\" MediaWiki page."), (pages.Count - count).ToString());

        }



        /// <summary>Gets page titles for this PageList from specified special page,

        /// e.g. "Deadendpages". The function does not filter namespaces. And the function

        /// does not clear the existing PageList, so new titles will be added.</summary>

        /// <param name="pageTitle">Title of special page, e.g. "Deadendpages".</param>

        /// <param name="quantity">Maximum number of page titles to get. Usually

        /// MediaWiki provides not more than 1000 titles.</param>

        public void FillFromCustomSpecialPage(string pageTitle, int quantity)
        {

            if (string.IsNullOrEmpty(pageTitle))

                throw new ArgumentNullException("pageTitle");

            if (quantity <= 0)

                throw new ArgumentOutOfRangeException("quantity",

                    Bot.Msg("Quantity must be positive."));


            string res = site.site + site.indexPath + "index.php?title=Special:" +

                HttpUtility.UrlEncode(pageTitle) + "&limit=" + quantity.ToString();

            string src = site.GetPageHTM(res);

            MatchCollection matches;

            if (pageTitle == "Unusedimages" || pageTitle == "Uncategorizedimages" ||

                pageTitle == "UnusedFiles" || pageTitle == "UncategorizedFiles")

                matches = site.linkToPageRE3.Matches(src);

            else

                matches = Site.linkToPageRE2.Matches(src);

            if (matches.Count == 0)

                throw new WikiBotException(string.Format(

                    Bot.Msg("Page \"Special:{0}\" does not contain page titles."), pageTitle));

            foreach (Match match in matches)

                pages.Add(new Page(site, HttpUtility.HtmlDecode(match.Groups[1].Value)));

            Console.WriteLine(Bot.Msg("PageList filled with {0} page titles from " +

                "\"Special:{1}\" page."), matches.Count, pageTitle);

        }



        /// <summary>Gets page titles for this PageList from specified special page,

        /// e.g. "Deadendpages". The function does not filter namespaces. And the function

        /// does not clear the existing PageList, so new titles will be added.

        /// The function uses XML (XHTML) parsing instead of regular expressions matching.

        /// This function is slower, than FillFromCustomSpecialPage.</summary>

        /// <param name="pageTitle">Title of special page, e.g. "Deadendpages".</param>

        /// <param name="quantity">Maximum number of page titles to get. Usually

        /// MediaWiki provides not more than 1000 titles.</param>

        public void FillFromCustomSpecialPageEx(string pageTitle, int quantity)
        {

            if (string.IsNullOrEmpty(pageTitle))

                throw new ArgumentNullException("pageTitle");

            if (quantity <= 0)

                throw new ArgumentOutOfRangeException("quantity",

                    Bot.Msg("Quantity must be positive."));


            string res = site.site + site.indexPath + "index.php?title=Special:" +

                HttpUtility.UrlEncode(pageTitle) + "&limit=" + quantity.ToString();

            string src = site.StripContent(site.GetPageHTM(res), null, null, true, true);

            XPathNodeIterator ni = site.GetXMLIterator(src, "//ns:ol/ns:li/ns:a[@title != '']");

            if (ni.Count == 0)

                throw new WikiBotException(string.Format(

                    Bot.Msg("Nothing was found on \"Special:{0}\" page."), pageTitle));

            while (ni.MoveNext())

                pages.Add(new Page(site,

                    HttpUtility.HtmlDecode(ni.Current.GetAttribute("title", ""))));

            Console.WriteLine(Bot.Msg("PageList filled with {0} page titles from " +

                "\"Special:{1}\" page."), ni.Count, pageTitle);

        }



        /// <summary>Gets page titles for this PageList from specified MediaWiki events log.

        /// The function does not filter namespaces. And the function does not clear the

        /// existing PageList, so new titles will be added.</summary>

        /// <param name="logType">Type of log, it could be: "block" for blocked users log;

        /// "protect" for protected pages log; "rights" for users rights log; "delete" for

        /// deleted pages log; "upload" for uploaded files log; "move" for renamed pages log;

        /// "import" for transwiki import log; "renameuser" for renamed accounts log;

        /// "newusers" for new users log; "makebot" for bot status assignment log.</param>

        /// <param name="userName">Select log entries only for specified account. Pass empty

        /// string, if that restriction is not needed.</param>

        /// <param name="pageTitle">Select log entries only for specified page. Pass empty

        /// string, if that restriction is not needed.</param>

        /// <param name="quantity">Maximum number of page titles to get.</param>

        public void FillFromCustomLog(string logType, string userName, string pageTitle,

            int quantity)
        {

            if (string.IsNullOrEmpty(logType))

                throw new ArgumentNullException("logType");

            if (quantity <= 0)

                throw new ArgumentOutOfRangeException("quantity",

                    Bot.Msg("Quantity must be positive."));


            string res = site.site + site.indexPath + "index.php?title=Special:Log&type=" +

                 logType + "&user=" + HttpUtility.UrlEncode(userName) + "&page=" +

                 HttpUtility.UrlEncode(pageTitle) + "&limit=" + quantity.ToString();

            string src = site.GetPageHTM(res);

            MatchCollection matches = Site.linkToPageRE2.Matches(src);

            if (matches.Count == 0)

                throw new WikiBotException(

                    string.Format(Bot.Msg("Log \"{0}\" does not contain page titles."), logType));

            foreach (Match match in matches)

                pages.Add(new Page(site, HttpUtility.HtmlDecode(match.Groups[1].Value)));

            Console.WriteLine(Bot.Msg("PageList filled with {0} page titles from \"{1}\" log."),

                matches.Count, logType);

        }



        /// <summary>Gets page titles for this PageList from specified list, produced by

        /// bot query interface ("api.php" MediaWiki extension). The function

        /// does not clear the existing PageList, so new titles will be added.</summary>

        /// <param name="listType">Title of list, the following values are supported: 

        /// "allpages", "alllinks", "allusers", "backlinks", "categorymembers",

        /// "embeddedin", "imageusage", "logevents", "recentchanges", 

        /// "usercontribs", "watchlist", "exturlusage". Detailed documentation

        /// can be found at "http://en.wikipedia.org/w/api.php".</param>

        /// <param name="queryParams">Additional query parameters, specific to the

        /// required list, e.g. "cmtitle=Category:Physical%20sciences&amp;cmnamespace=0|2".

        /// Parameter values must be URL-encoded with HttpUtility.UrlEncode function

        /// before calling this function.</param>

        /// <param name="quantity">Maximum number of page titles to get.</param>

        /// <example><code>

        /// pageList.FillFromCustomBotQueryList("categorymembers",

        /// 	"cmcategory=Physical%20sciences&amp;cmnamespace=0|14",

        /// 	int.MaxValue);

        /// </code></example>

        public void FillFromCustomBotQueryList(string listType, string queryParams, int quantity)
        {

            if (!site.botQuery)

                throw new WikiBotException(

                    Bot.Msg("The \"api.php\" MediaWiki extension is not available."));

            if (string.IsNullOrEmpty(listType))

                throw new ArgumentNullException("listType");

            if (!Site.botQueryLists.Contains(listType))

                throw new WikiBotException(

                    string.Format(Bot.Msg("The list \"{0}\" is not supported."), listType));

            if (quantity <= 0)

                throw new ArgumentOutOfRangeException("quantity",

                    Bot.Msg("Quantity must be positive."));

            string prefix = Site.botQueryLists[listType].ToString();

            string continueAttrTag1 = prefix + "from";

            string continueAttrTag2 = prefix + "continue";

            string attrTag = (listType != "allusers") ? "title" : "name";

            string queryUri = site.indexPath + "api.php?action=query&list=" + listType +

                "&format=xml&" + prefix + "limit=" +

                ((quantity > 500) ? "500" : quantity.ToString());

            string src = "", next = "", queryFullUri = "";

            int count = pages.Count;

            if (quantity != int.MaxValue)

                quantity += pages.Count;

            do
            {

                queryFullUri = queryUri;

                if (next != "")

                    queryFullUri += "&" + prefix + "continue=" + HttpUtility.UrlEncode(next);

                src = site.PostDataAndGetResultHTM(queryFullUri, queryParams);

                using (XmlTextReader reader = new XmlTextReader(new StringReader(src)))
                {

                    next = "";

                    while (reader.Read())
                    {

                        if (reader.IsEmptyElement && reader[attrTag] != null)

                            pages.Add(new Page(site, HttpUtility.HtmlDecode(reader[attrTag])));

                        if (reader.IsEmptyElement && reader[continueAttrTag1] != null)

                            next = reader[continueAttrTag1];

                        if (reader.IsEmptyElement && reader[continueAttrTag2] != null)

                            next = reader[continueAttrTag2];

                    }

                }

            }

            while (next != "" && pages.Count < quantity);

            if (pages.Count > quantity)

                pages.RemoveRange(quantity, pages.Count - quantity);

            if (!string.IsNullOrEmpty(Environment.StackTrace) &&

                !Environment.StackTrace.Contains("FillAllFromCategoryEx"))

                Console.WriteLine(Bot.Msg("PageList filled with {0} page titles " +

                    "from \"{1}\" bot interface list."),

                    (pages.Count - count).ToString(), listType);

        }



        /// <summary>Gets page titles for this PageList from recent changes page,

        /// "Special:Recentchanges". File uploads, page deletions and page renamings are

        /// not included, use FillFromCustomLog function instead to fill from respective logs.

        /// The function does not clear the existing PageList, so new titles will be added.

        /// Use FilterNamespaces() or RemoveNamespaces() functions to remove

        /// pages from unwanted namespaces.</summary>

        /// <param name="hideMinor">Ignore minor edits.</param>

        /// <param name="hideBots">Ignore bot edits.</param>

        /// <param name="hideAnons">Ignore anonymous users edits.</param>

        /// <param name="hideLogged">Ignore logged-in users edits.</param>

        /// <param name="hideSelf">Ignore edits of this bot account.</param>

        /// <param name="limit">Maximum number of changes to get.</param>

        /// <param name="days">Get changes for this number of recent days.</param>

        public void FillFromRecentChanges(bool hideMinor, bool hideBots, bool hideAnons,

            bool hideLogged, bool hideSelf, int limit, int days)
        {

            if (limit <= 0)

                throw new ArgumentOutOfRangeException("limit", Bot.Msg("Limit must be positive."));

            if (days <= 0)

                throw new ArgumentOutOfRangeException("days",

                    Bot.Msg("Number of days must be positive."));

            string uri = string.Format("{0}{1}index.php?title=Special:Recentchanges&" +

                "hideminor={2}&hideBots={3}&hideAnons={4}&hideliu={5}&hidemyself={6}&" +

                "limit={7}&days={8}", site.site, site.indexPath,

                hideMinor ? "1" : "0", hideBots ? "1" : "0", hideAnons ? "1" : "0",

                hideLogged ? "1" : "0", hideSelf ? "1" : "0",

                limit.ToString(), days.ToString());

            string respStr = site.GetPageHTM(uri);

            MatchCollection matches = Site.linkToPageRE2.Matches(respStr);

            foreach (Match match in matches)

                pages.Add(new Page(site, HttpUtility.HtmlDecode(match.Groups[1].Value)));

            Console.WriteLine(Bot.Msg("PageList filled with {0} page titles from " +

                "\"Special:Recentchanges\" page."), matches.Count);

        }



        /// <summary>Gets page titles for this PageList from specified wiki category page, excluding

        /// subcategories. Use FillSubsFromCategory function to get subcategories.</summary>

        /// <param name="categoryName">Category name, with or without prefix.</param>

        public void FillFromCategory(string categoryName)
        {

            int count = pages.Count;

            PageList pl = new PageList(site);

            pl.FillAllFromCategory(categoryName);

            pl.RemoveNamespaces(new int[] { 14 });

            pages.AddRange(pl.pages);

            if (pages.Count != count)

                Console.WriteLine(

                    Bot.Msg("PageList filled with {0} page titles, found in \"{1}\" category."),

                    (pages.Count - count).ToString(), categoryName);

            else

                Console.Error.WriteLine(

                    Bot.Msg("Nothing was found in \"{0}\" category."), categoryName);

        }



        /// <summary>Gets subcategories titles for this PageList from specified wiki category page,

        /// excluding other pages. Use FillFromCategory function to get other pages.</summary>

        /// <param name="categoryName">Category name, with or without prefix.</param>

        public void FillSubsFromCategory(string categoryName)
        {

            int count = pages.Count;

            PageList pl = new PageList(site);

            pl.FillAllFromCategory(categoryName);

            pl.FilterNamespaces(new int[] { 14 });

            pages.AddRange(pl.pages);

            if (pages.Count != count)

                Console.WriteLine(Bot.Msg("PageList filled with {0} subcategory page titles, " +

                    "found in \"{1}\" category."), (pages.Count - count).ToString(), categoryName);

            else

                Console.Error.WriteLine(

                    Bot.Msg("Nothing was found in \"{0}\" category."), categoryName);

        }



        /// <summary>This internal function gets all page titles for this PageList from specified

        /// category page, including subcategories.</summary>

        /// <param name="categoryName">Category name, with or without prefix.</param>

        public void FillAllFromCategory(string categoryName)
        {

            if (string.IsNullOrEmpty(categoryName))

                throw new ArgumentNullException("categoryName");

            categoryName = categoryName.Trim("[]\f\n\r\t\v ".ToCharArray());

            categoryName = site.RemoveNSPrefix(categoryName, 14);

            categoryName = site.namespaces["14"] + ":" + categoryName;


            //RemoveAll();

            if (Bot.useBotQuery == true && site.botQuery == true)
            {

                FillAllFromCategoryEx(categoryName);

                return;

            }

            string src = "";

            MatchCollection matches;

            Regex nextPortionRE = new Regex("&(?:amp;)?from=([^\"=]+)\" title=\"");

            do
            {

                string res = site.site + site.indexPath + "index.php?title=" +

                    HttpUtility.UrlEncode(categoryName) +

                    "&from=" + nextPortionRE.Match(src).Groups[1].Value;

                src = site.GetPageHTM(res);

                src = HttpUtility.HtmlDecode(src);

                matches = Site.linkToPageRE1.Matches(src);

                foreach (Match match in matches)

                    pages.Add(new Page(site, match.Groups[1].Value));

                if (src.Contains("<div class=\"gallerytext\">\n"))
                {

                    matches = Site.linkToImageRE.Matches(src);

                    foreach (Match match in matches)

                        pages.Add(new Page(site, match.Groups[1].Value));

                }

                if (src.Contains("<div class=\"CategoryTreeChildren\""))
                {

                    matches = Site.linkToSubCategoryRE.Matches(src);

                    foreach (Match match in matches)

                        pages.Add(new Page(site, site.namespaces["14"] + ":" +

                            match.Groups[1].Value));

                }

            }

            while (nextPortionRE.IsMatch(src));

        }



        /// <summary>This internal function gets all page titles for this PageList from specified

        /// category using "api.php" MediaWiki extension (bot interface), if it is available.

        /// It gets subcategories too.</summary>

        /// <param name="categoryName">Category name, with or without prefix.</param>

        public void FillAllFromCategoryEx(string categoryName)
        {

            if (string.IsNullOrEmpty(categoryName))

                throw new ArgumentNullException("categoryName");

            categoryName = categoryName.Trim("[]\f\n\r\t\v ".ToCharArray());

            categoryName = site.RemoveNSPrefix(categoryName, 14);

            if (site.botQueryVersions.ContainsKey("ApiQueryCategoryMembers.php"))
            {

                if (int.Parse(

                    site.botQueryVersions["ApiQueryCategoryMembers.php"].ToString()) >= 30533)

                    FillFromCustomBotQueryList("categorymembers", "cmtitle=" +

                        HttpUtility.UrlEncode(site.namespaces["14"].ToString() + ":" +

                        categoryName), int.MaxValue);

                else

                    FillFromCustomBotQueryList("categorymembers", "cmcategory=" +

                        HttpUtility.UrlEncode(categoryName), int.MaxValue);

            }

            else if (site.botQueryVersions.ContainsKey("query.php"))

                FillAllFromCategoryExOld(categoryName);

            else
            {

                Console.WriteLine(Bot.Msg("Can't get category members using bot interface.\n" +

                    "Switching to common user interface (\"site.botQuery\" is set to \"false\")."));

                site.botQuery = false;

                FillAllFromCategory(categoryName);

            }

        }



        /// <summary>This internal function is kept for backwards compatibility only.

        /// It gets all pages and subcategories in specified category using old obsolete 

        /// "query.php" bot interface and adds all found pages and subcategories to PageList object.

        /// It gets titles portion by portion. The "query.php" interface was superseded by

        /// "api.php" in MediaWiki 1.8.</summary>

        /// <param name="categoryName">Category name, with or without prefix.</param>

        public void FillAllFromCategoryExOld(string categoryName)
        {

            if (string.IsNullOrEmpty(categoryName))

                throw new ArgumentNullException("categoryName");

            string src = "";

            MatchCollection matches;

            Regex nextPortionRE = new Regex("<category next=\"(.+?)\" />");

            do
            {

                string res = site.site + site.indexPath + "query.php?what=category&cptitle=" +

                    HttpUtility.UrlEncode(categoryName) + "&cpfrom=" +

                    nextPortionRE.Match(src).Groups[1].Value + "&cplimit=500&format=xml";

                src = site.GetPageHTM(res);

                matches = Site.pageTitleTagRE.Matches(src);

                foreach (Match match in matches)

                    pages.Add(new Page(site, HttpUtility.HtmlDecode(match.Groups[1].Value)));

            }

            while (nextPortionRE.IsMatch(src));

        }



        /// <summary>Gets all levels of subcategories of some wiki category (that means

        /// subcategories, sub-subcategories, and so on) and fills this PageList with titles

        /// of all pages, found in all levels of subcategories. The multiplicates of recurring pages

        /// are removed. Use FillSubsFromCategoryTree function instead to get titles

        /// of subcategories. This operation may be very time-consuming and traffic-consuming.

        /// The function clears the PageList before filling.</summary>

        /// <param name="categoryName">Category name, with or without prefix.</param>

        public void FillFromCategoryTree(string categoryName)
        {

            FillAllFromCategoryTree(categoryName);

            RemoveNamespaces(new int[] { 14 });

            if (pages.Count != 0)

                Console.WriteLine(

                    Bot.Msg("PageList filled with {0} page titles, found in \"{1}\" category."),

                    Count().ToString(), categoryName);

            else

                Console.Error.WriteLine(

                    Bot.Msg("Nothing was found in \"{0}\" category."), categoryName);

        }



        /// <summary>Gets all levels of subcategories of some wiki category (that means

        /// subcategories, sub-subcategories, and so on) and fills this PageList with found

        /// subcategory titles. Use FillFromCategoryTree function instead to get pages of other

        /// namespaces. The multiplicates of recurring categories are removed. The operation may

        /// be very time-consuming and traffic-consuming. The function clears the PageList

        /// before filling.</summary>

        /// <param name="categoryName">Category name, with or without prefix.</param>

        public void FillSubsFromCategoryTree(string categoryName)
        {

            FillAllFromCategoryTree(categoryName);

            FilterNamespaces(new int[] { 14 });

            if (pages.Count != 0)

                Console.WriteLine(Bot.Msg("PageList filled with {0} subcategory page titles, " +

                    "found in \"{1}\" category."), Count().ToString(), categoryName);

            else

                Console.Error.WriteLine(

                    Bot.Msg("Nothing was found in \"{0}\" category."), categoryName);

        }



        /// <summary>Gets all levels of subcategories of some wiki category (that means

        /// subcategories, sub-subcategories, and so on) and fills this PageList with titles

        /// of all pages, found in all levels of subcategories, including the titles of

        /// subcategories. The multiplicates of recurring pages and subcategories are removed.

        /// The operation may be very time-consuming and traffic-consuming. The function clears

        /// the PageList before filling.</summary>

        /// <param name="categoryName">Category name, with or without prefix.</param>

        public void FillAllFromCategoryTree(string categoryName)
        {

            Clear();

            categoryName = site.CorrectNSPrefix(categoryName);

            StringCollection doneCats = new StringCollection();

            FillAllFromCategory(categoryName);

            doneCats.Add(categoryName);

            for (int i = 0; i < Count(); i++)

                if (pages[i].GetNamespace() == 14 && !doneCats.Contains(pages[i].title))
                {

                    FillAllFromCategory(pages[i].title);

                    doneCats.Add(pages[i].title);

                }

            RemoveRecurring();

        }



        /// <summary>Gets page history and fills this PageList with specified number of recent page

        /// revisions. Only revision identifiers, user names, timestamps and comments are

        /// loaded, not the texts. Call Load() (but not LoadEx) to load the texts of page revisions.

        /// The function combines XML (XHTML) parsing and regular expressions matching.</summary>

        /// <param name="pageTitle">Page to get history of.</param>

        /// <param name="lastRevisions">Number of last page revisions to get.</param>

        public void FillFromPageHistory(string pageTitle, int lastRevisions)
        {

            if (string.IsNullOrEmpty(pageTitle))

                throw new ArgumentNullException("pageTitle");

            if (lastRevisions <= 0)

                throw new ArgumentOutOfRangeException("quantity",

                    Bot.Msg("Quantity must be positive."));

            Console.WriteLine(

                Bot.Msg("Getting {0} last revisons of \"{1}\" page..."), lastRevisions, pageTitle);

            string res = site.site + site.indexPath + "index.php?title=" +

                HttpUtility.UrlEncode(pageTitle) + "&limit=" + lastRevisions.ToString() +

                    "&action=history";

            string src = site.GetPageHTM(res);

            src = src.Substring(src.IndexOf("<ul id=\"pagehistory\">"));

            src = src.Substring(0, src.IndexOf("</ul>") + 5);

            Page p = null;

            using (XmlReader reader = site.GetXMLReader(src))
            {

                while (reader.Read())
                {

                    if (reader.Name == "li" && reader.NodeType == XmlNodeType.Element)
                    {

                        p = new Page(site, pageTitle);

                        p.lastMinorEdit = false;

                        p.comment = "";

                    }

                    else if (reader.Name == "span" && reader["class"] == "mw-history-histlinks")
                    {

                        reader.ReadToFollowing("a");

                        p.lastRevisionID = reader["href"].Substring(

                            reader["href"].IndexOf("oldid=") + 6);

                        DateTime.TryParse(reader.ReadString(),

                            site.regCulture, DateTimeStyles.AssumeLocal, out p.timestamp);

                    }

                    else if (reader.Name == "span" && reader["class"] == "history-user")
                    {

                        reader.ReadToFollowing("a");

                        p.lastUser = reader.ReadString();

                    }

                    else if (reader.Name == "abbr")

                        p.lastMinorEdit = true;

                    else if (reader.Name == "span" && reader["class"] == "history-size")

                        int.TryParse(Regex.Replace(reader.ReadString(), @"[^-+\d]", ""),

                            out p.lastBytesModified);

                    else if (reader.Name == "span" && reader["class"] == "comment")
                    {

                        p.comment = Regex.Replace(reader.ReadInnerXml().Trim(), "<.+?>", "");

                        p.comment = p.comment.Substring(1, p.comment.Length - 2);	// brackets

                    }

                    if (reader.Name == "li" && reader.NodeType == XmlNodeType.EndElement)

                        pages.Add(p);

                }

            }

            Console.WriteLine(Bot.Msg("PageList filled with {0} last revisons of \"{1}\" page..."),

                pages.Count, pageTitle);

        }



        /// <summary>Gets page history using  bot query interface ("api.php" MediaWiki extension)

        /// and fills this PageList with specified number of last page revisions, optionally loading

        /// revision texts as well. On most sites not more than 50 last revisions can be obtained.

        /// Thanks to Jutiphan Mongkolsuthree for idea and outline of this function.</summary>

        /// <param name="pageTitle">Page to get history of.</param>

        /// <param name="lastRevisions">Number of last page revisions to obtain.</param>

        /// <param name="loadTexts">Load revision texts right away.</param>

        public void FillFromPageHistoryEx(string pageTitle, int lastRevisions, bool loadTexts)
        {

            if (!site.botQuery)

                throw new WikiBotException(

                    Bot.Msg("The \"api.php\" MediaWiki extension is not available."));

            if (string.IsNullOrEmpty(pageTitle))

                throw new ArgumentNullException("pageTitle");

            if (lastRevisions <= 0)

                throw new ArgumentOutOfRangeException("lastRevisions",

                    Bot.Msg("Quantity must be positive."));

            Console.WriteLine(

                Bot.Msg("Getting {0} last revisons of \"{1}\" page..."), lastRevisions, pageTitle);

            string queryUri = site.site + site.indexPath +

                "api.php?action=query&prop=revisions&titles=" +

                HttpUtility.UrlEncode(pageTitle) + "&rvprop=ids|user|comment|timestamp" +

                (loadTexts ? "|content" : "") + "&format=xml&rvlimit=" + lastRevisions.ToString();

            string src = site.GetPageHTM(queryUri);

            Page p;

            using (XmlReader reader = XmlReader.Create(new StringReader(src)))
            {

                reader.ReadToFollowing("api");

                reader.Read();

                if (reader.Name == "error")

                    Console.Error.WriteLine(Bot.Msg("Error: {0}"), reader.GetAttribute("info"));

                while (reader.ReadToFollowing("rev"))
                {

                    p = new Page(site, pageTitle);

                    p.lastRevisionID = reader.GetAttribute("revid");

                    p.lastUser = reader.GetAttribute("user");

                    p.comment = reader.GetAttribute("comment");

                    p.timestamp =

                        DateTime.Parse(reader.GetAttribute("timestamp")).ToUniversalTime();

                    if (loadTexts)

                        p.text = reader.ReadString();

                    pages.Add(p);

                }

            }

            Console.WriteLine(Bot.Msg("PageList filled with {0} last revisons of \"{1}\" page."),

                pages.Count, pageTitle);

        }



        /// <summary>Gets page titles for this PageList from links in some wiki page. But only

        /// links to articles and pages from Project, Template and Help namespaces will be

        /// retrieved. And no interwiki links. Use FillFromAllPageLinks function instead

        /// to filter namespaces manually.</summary>

        /// <param name="pageTitle">Page name to get links from.</param>

        public void FillFromPageLinks(string pageTitle)
        {

            if (string.IsNullOrEmpty(pageTitle))

                throw new ArgumentNullException("pageTitle");

            FillFromAllPageLinks(pageTitle);

            FilterNamespaces(new int[] { 0, 4, 10, 12 });

        }



        /// <summary>Gets page titles for this PageList from all links in some wiki page. All links

        /// will be retrieved, from all standard namespaces, except interwiki links to other

        /// sites. Use FillFromPageLinks function instead to filter namespaces

        /// automatically.</summary>

        /// <param name="pageTitle">Page title as string.</param>

        /// <example><code>pageList.FillFromAllPageLinks("Art");</code></example>

        public void FillFromAllPageLinks(string pageTitle)
        {

            if (string.IsNullOrEmpty(pageTitle))

                throw new ArgumentNullException("pageTitle");

            if (string.IsNullOrEmpty(Site.WMLangsStr))

                site.GetWikimediaWikisList();

            Regex wikiLinkRE = new Regex(@"\[\[:*(.+?)(]]|\|)");

            Page page = new Page(site, pageTitle);

            page.Load();

            MatchCollection matches = wikiLinkRE.Matches(page.text);

            Regex outWikiLink = new Regex("^(" + Site.WMLangsStr +

                /*"|" + Site.WMSitesStr + */ "):");

            foreach (Match match in matches)

                if (!outWikiLink.IsMatch(match.Groups[1].Value))

                    pages.Add(new Page(site, match.Groups[1].Value));

            Console.WriteLine(

                Bot.Msg("PageList filled with links, found on \"{0}\" page."), pageTitle);

        }



        /// <summary>Gets page titles for this PageList from "Special:Whatlinkshere" Mediawiki page

        /// of specified page. That means the titles of pages, referring to the specified page.

        /// But not more than 5000 titles. The function does not internally remove redirecting

        /// pages from the results. Call RemoveRedirects() manually, if you need it. And the

        /// function does not clear the existing PageList, so new titles will be added.</summary>

        /// <param name="pageTitle">Page title as string.</param>

        public void FillFromLinksToPage(string pageTitle)
        {

            if (string.IsNullOrEmpty(pageTitle))

                throw new ArgumentNullException("pageTitle");

            //RemoveAll();

            string res = site.site + site.indexPath +

                "index.php?title=Special:Whatlinkshere/" +

                HttpUtility.UrlEncode(pageTitle) + "&limit=5000";

            string src = site.GetPageHTM(res);

            MatchCollection matches = Site.linkToPageRE1.Matches(src);

            foreach (Match match in matches)

                pages.Add(new Page(site, HttpUtility.HtmlDecode(match.Groups[1].Value)));

            //RemoveRedirects();

            Console.WriteLine(

                Bot.Msg("PageList filled with titles of pages, referring to \"{0}\" page."),

                pageTitle);

        }



        /// <summary>Gets titles of pages which transclude the specified page. No more than

        /// 5000 titles are listed. The function does not internally remove redirecting

        /// pages from results. Call RemoveRedirects() manually, if you need it. And the

        /// function does not clear the existing PageList, so new titles will be added.</summary>

        /// <param name="pageTitle">Page title as string.</param>

        public void FillFromTransclusionsOfPage(string pageTitle)
        {

            if (string.IsNullOrEmpty(pageTitle))

                throw new ArgumentNullException("pageTitle");

            string res = site.site + site.indexPath +

                "index.php?title=Special:Whatlinkshere/" +

                HttpUtility.UrlEncode(pageTitle) + "&limit=5000&hidelinks=1";

            string src = site.GetPageHTM(res);

            MatchCollection matches = Site.linkToPageRE1.Matches(src);

            foreach (Match match in matches)

                pages.Add(new Page(site, HttpUtility.HtmlDecode(match.Groups[1].Value)));

            Console.WriteLine(

                Bot.Msg("PageList filled with titles of pages, which transclude \"{0}\" page."),

                pageTitle);

        }



        /// <summary>Gets titles of pages, in which the specified image file is included.

        /// Function also works with non-image files.</summary>

        /// <param name="imageFileTitle">File title. With or without "Image:" or

        /// "File:" prefix.</param>

        public void FillFromPagesUsingImage(string imageFileTitle)
        {

            if (string.IsNullOrEmpty(imageFileTitle))

                throw new ArgumentNullException("imageFileTitle");

            int pagesCount = Count();

            imageFileTitle = site.RemoveNSPrefix(imageFileTitle, 6);

            string res = site.site + site.indexPath + "index.php?title=" +

                HttpUtility.UrlEncode(site.namespaces["6"].ToString()) + ":" +

                HttpUtility.UrlEncode(imageFileTitle);

            string src = site.GetPageHTM(res);

            int startPos = src.IndexOf("<h2 id=\"filelinks\">");

            int endPos = src.IndexOf("<div class=\"printfooter\">");

            if (startPos == -1 || endPos == -1)
            {

                Console.Error.WriteLine(Bot.Msg("No page contains \"{0}\" image."), imageFileTitle);

                return;

            }

            src = src.Substring(startPos, endPos - startPos);

            MatchCollection matches = Site.linkToPageRE1.Matches(src);

            foreach (Match match in matches)

                pages.Add(new Page(site, HttpUtility.HtmlDecode(match.Groups[1].Value)));

            if (pagesCount == Count())

                Console.Error.WriteLine(Bot.Msg("No page contains \"{0}\" image."), imageFileTitle);

            else

                Console.WriteLine(

                    Bot.Msg("PageList filled with titles of pages, that contain \"{0}\" image."),

                    imageFileTitle);

        }



        /// <summary>Gets page titles for this PageList from user contributions

        /// of specified user. The function does not internally remove redirecting

        /// pages from the results. Call RemoveRedirects() manually, if you need it. And the

        /// function does not clears the existing PageList, so new titles will be added.</summary>

        /// <param name="userName">User's name.</param>

        /// <param name="limit">Maximum number of page titles to get.</param>

        public void FillFromUserContributions(string userName, int limit)
        {

            if (string.IsNullOrEmpty(userName))

                throw new ArgumentNullException("userName");

            if (limit <= 0)

                throw new ArgumentOutOfRangeException("limit", Bot.Msg("Limit must be positive."));

            string res = site.site + site.indexPath +

                "index.php?title=Special:Contributions&target=" + HttpUtility.UrlEncode(userName) +

                "&limit=" + limit.ToString();

            string src = site.GetPageHTM(res);

            MatchCollection matches = Site.linkToPageRE2.Matches(src);

            foreach (Match match in matches)

                pages.Add(new Page(site, HttpUtility.HtmlDecode(match.Groups[1].Value)));

            Console.WriteLine(

                Bot.Msg("PageList filled with user's \"{0}\" contributions."), userName);

        }



        /// <summary>Gets page titles for this PageList from watchlist

        /// of bot account. The function does not internally remove redirecting

        /// pages from the results. Call RemoveRedirects() manually, if you need that. And the

        /// function neither filters namespaces, nor clears the existing PageList,

        /// so new titles will be added to the existing in PageList.</summary>

        public void FillFromWatchList()
        {

            string src = site.GetPageHTM(site.indexPath + "index.php?title=Special:Watchlist/edit");

            MatchCollection matches = Site.linkToPageRE2.Matches(src);

            foreach (Match match in matches)

                pages.Add(new Page(site, HttpUtility.HtmlDecode(match.Groups[1].Value)));

            Console.WriteLine(Bot.Msg("PageList filled with bot account's watchlist."));

        }



        /// <summary>Gets page titles for this PageList from list of recently changed

        /// watched articles (watched by bot account). The function does not internally

        /// remove redirecting pages from the results. Call RemoveRedirects() manually,

        /// if you need it. And the function neither filters namespaces, nor clears

        /// the existing PageList, so new titles will be added to the existing

        /// in PageList.</summary>

        public void FillFromChangedWatchedPages()
        {

            string src = site.GetPageHTM(site.indexPath + "index.php?title=Special:Watchlist/edit");

            MatchCollection matches = Site.linkToPageRE2.Matches(src);

            Console.WriteLine(src);

            foreach (Match match in matches)

                pages.Add(new Page(site, HttpUtility.HtmlDecode(match.Groups[1].Value)));

            Console.WriteLine(

                Bot.Msg("PageList filled with changed pages from bot account's watchlist."));

        }



        /// <summary>Gets page titles for this PageList from wiki site internal search results.

        /// The function does not filter namespaces. And the function does not clear

        /// the existing PageList, so new titles will be added.</summary>

        /// <param name="searchStr">String to search.</param>

        /// <param name="limit">Maximum number of page titles to get.</param>

        public void FillFromSearchResults(string searchStr, int limit)
        {

            if (string.IsNullOrEmpty(searchStr))

                throw new ArgumentNullException("searchStr");

            if (limit <= 0)

                throw new ArgumentOutOfRangeException("limit", Bot.Msg("Limit must be positive."));

            string res = site.site + site.indexPath +

                "index.php?title=Special:Search&fulltext=Search&search=" +

                HttpUtility.UrlEncode(searchStr) + "&limit=" + limit.ToString();

            string src = site.GetPageHTM(res);

            src = Bot.GetStringPortion(src, "<ul class='mw-search-results'>", "</ul>");

            Regex linkRE = new Regex("<a href=\".+?\" title=\"(.+?)\">");

            MatchCollection matches = linkRE.Matches(src);

            foreach (Match match in matches)

                pages.Add(new Page(site, HttpUtility.HtmlDecode(match.Groups[1].Value)));

            Console.WriteLine(Bot.Msg("PageList filled with search results."));

        }



        /// <summary>Gets page titles for this PageList from www.google.com search results.

        /// The function does not filter namespaces. And the function does not clear

        /// the existing PageList, so new titles will be added.</summary>

        /// <param name="searchStr">Words to search for. Use quotes to find exact phrases.</param>

        /// <param name="limit">Maximum number of page titles to get.</param>

        public void FillFromGoogleSearchResults(string searchStr, int limit)
        {

            if (string.IsNullOrEmpty(searchStr))

                throw new ArgumentNullException("searchStr");

            if (limit <= 0)

                throw new ArgumentOutOfRangeException("limit", Bot.Msg("Limit must be positive."));

            Uri res = new Uri("http://www.google.com/search?q=" + HttpUtility.UrlEncode(searchStr) +

                "+site:" + site.site.Substring(site.site.IndexOf("://") + 3) +

                "&num=" + limit.ToString());

            string src = Bot.GetWebResource(res, "");

            Regex GoogleLinkToPageRE = new Regex(

                "<h3[^>]*><a href=\"" + Regex.Escape(site.site) +

                "(" + (string.IsNullOrEmpty(site.wikiPath) == false ?

                    Regex.Escape(site.wikiPath) + "|" : "") +

                    Regex.Escape(site.indexPath) + @"index\.php\?title=)" +

                "([^\"]+?)\"");		// ..." class=\"?l\"?

            MatchCollection matches = GoogleLinkToPageRE.Matches(src);

            foreach (Match match in matches)

                pages.Add(new Page(site,

                    HttpUtility.UrlDecode(match.Groups[2].Value).Replace("_", " ")));

            Console.WriteLine(Bot.Msg("PageList filled with www.google.com search results."));

        }



        /// <summary>Gets page titles from UTF8-encoded file. Each title must be on new line.

        /// The function does not clear the existing PageList, so new pages will be added.</summary>

        public void FillFromFile(string filePathName)
        {

            //RemoveAll();

            StreamReader strmReader = new StreamReader(filePathName);

            string input;

            while ((input = strmReader.ReadLine()) != null)
            {

                input = input.Trim(" []".ToCharArray());

                if (string.IsNullOrEmpty(input) != true)

                    pages.Add(new Page(site, input));

            }

            strmReader.Close();

            Console.WriteLine(

                Bot.Msg("PageList filled with titles, found in \"{0}\" file."), filePathName);

        }



        /// <summary>Protects or unprotects all pages in this PageList, so only chosen category

        /// of users can edit or rename it. Changing page protection modes requires administrator

        /// (sysop) rights on target wiki.</summary>

        /// <param name="editMode">Protection mode for editing this page (0 = everyone allowed

        /// to edit, 1 = only registered users are allowed, 2 = only administrators are allowed 

        /// to edit).</param>

        /// <param name="renameMode">Protection mode for renaming this page (0 = everyone allowed to

        /// rename, 1 = only registered users are allowed, 2 = only administrators

        /// are allowed).</param>

        /// <param name="cascadeMode">In cascading mode, all the pages, included into this page

        /// (e.g., templates or images) are also fully automatically protected.</param>

        /// <param name="expiryDate">Date ant time, expressed in UTC, when the protection expires

        /// and page becomes fully unprotected. Use DateTime.ToUniversalTime() method to convert

        /// local time to UTC, if necessary. Pass DateTime.MinValue to make protection

        /// indefinite.</param>

        /// <param name="reason">Reason for protecting this page.</param>

        public void Protect(int editMode, int renameMode, bool cascadeMode,

            DateTime expiryDate, string reason)
        {

            if (IsEmpty())

                throw new WikiBotException(Bot.Msg("The PageList is empty. Nothing to protect."));

            foreach (Page p in pages)

                p.Protect(editMode, renameMode, cascadeMode, expiryDate, reason);

        }



        /// <summary>Adds all pages in this PageList to bot account's watchlist.</summary>

        public void Watch()
        {

            if (IsEmpty())

                throw new WikiBotException(Bot.Msg("The PageList is empty. Nothing to watch."));

            foreach (Page p in pages)

                p.Watch();

        }



        /// <summary>Removes all pages in this PageList from bot account's watchlist.</summary>

        public void Unwatch()
        {

            if (IsEmpty())

                throw new WikiBotException(Bot.Msg("The PageList is empty. Nothing to unwatch."));

            foreach (Page p in pages)

                p.Unwatch();

        }



        /// <summary>Removes the pages, that are not in given namespaces.</summary>

        /// <param name="neededNSs">Array of integers, presenting keys of namespaces

        /// to retain.</param>

        /// <example><code>pageList.FilterNamespaces(new int[] {0,3});</code></example>

        public void FilterNamespaces(int[] neededNSs)
        {

            for (int i = pages.Count - 1; i >= 0; i--)
            {

                if (Array.IndexOf(neededNSs, pages[i].GetNamespace()) == -1)

                    pages.RemoveAt(i);
            }

        }



        /// <summary>Removes the pages, that are in given namespaces.</summary>

        /// <param name="needlessNSs">Array of integers, presenting keys of namespaces

        /// to remove.</param>

        /// <example><code>pageList.RemoveNamespaces(new int[] {2,4});</code></example>

        public void RemoveNamespaces(int[] needlessNSs)
        {

            for (int i = pages.Count - 1; i >= 0; i--)
            {

                if (Array.IndexOf(needlessNSs, pages[i].GetNamespace()) != -1)

                    pages.RemoveAt(i);
            }

        }



        /// <summary>This function sorts all pages in PageList by titles.</summary>

        public void Sort()
        {

            if (IsEmpty())

                throw new WikiBotException(Bot.Msg("The PageList is empty. Nothing to sort."));

            pages.Sort(ComparePagesByTitles);

        }



        /// <summary>This internal function compares pages by titles (alphabetically).</summary>

        /// <returns>Returns 1 if x is greater, -1 if y is greater, 0 if equal.</returns>

        public int ComparePagesByTitles(Page x, Page y)
        {

            int r = string.Compare(x.title, y.title, false, site.langCulture);

            return (r != 0) ? r / Math.Abs(r) : 0;

        }



        /// <summary>Removes all pages in PageList from specified category by deleting

        /// links to that category in pages texts.</summary>

        /// <param name="categoryName">Category name, with or without prefix.</param>

        public void RemoveFromCategory(string categoryName)
        {

            foreach (Page p in pages)

                p.RemoveFromCategory(categoryName);

        }



        /// <summary>Adds all pages in PageList to the specified category by adding

        /// links to that category in pages texts.</summary>

        /// <param name="categoryName">Category name, with or without prefix.</param>

        public void AddToCategory(string categoryName)
        {

            foreach (Page p in pages)

                p.AddToCategory(categoryName);

        }



        /// <summary>Adds a specified template to the end of all pages in PageList.</summary>

        /// <param name="templateText">Template text, like "{{template_name|...|...}}".</param>

        public void AddTemplate(string templateText)
        {

            foreach (Page p in pages)

                p.AddTemplate(templateText);

        }



        /// <summary>Removes a specified template from all pages in PageList.</summary>

        /// <param name="templateTitle">Title of template  to remove.</param>

        public void RemoveTemplate(string templateTitle)
        {

            foreach (Page p in pages)

                p.RemoveTemplate(templateTitle);

        }



        /// <summary>Loads text for pages in PageList from site via common web interface.

        /// Please, don't use this function when going to edit big amounts of pages on

        /// popular public wikis, as it compromises edit conflict detection. In that case,

        /// each page's text should be loaded individually right before its processing

        /// and saving.</summary>

        public void Load()
        {

            if (IsEmpty())

                throw new WikiBotException(Bot.Msg("The PageList is empty. Nothing to load."));

            foreach (Page page in pages)

                page.Load();

        }



        /// <summary>Loads texts and metadata for pages in PageList via XML export interface.

        /// Non-existent pages will be automatically removed from the PageList.

        /// Please, don't use this function when going to edit big amounts of pages on

        /// popular public wikis, as it compromises edit conflict detection. In that case,

        /// each page's text should be loaded individually right before its processing

        /// and saving.</summary>

        public void LoadEx()
        {

            if (IsEmpty())

                throw new WikiBotException(Bot.Msg("The PageList is empty. Nothing to load."));

            Console.WriteLine(Bot.Msg("Loading {0} pages..."), pages.Count);

            string res = site.site + site.indexPath +

                "index.php?title=Special:Export&action=submit";

            string postData = "curonly=True&pages=";

            foreach (Page page in pages)

                postData += HttpUtility.UrlEncode(page.title) + "\r\n";

            XmlReader reader = XmlReader.Create(

                new StringReader(site.PostDataAndGetResultHTM(res, postData)));

            Clear();

            while (reader.ReadToFollowing("page"))
            {

                Page p = new Page(site, "");

                p.ParsePageXML(reader.ReadOuterXml());

                pages.Add(p);

            }

            reader.Close();

        }



        /// <summary>Loads text and metadata for pages in PageList via XML export interface.

        /// The function uses XPathNavigator and is less efficient than LoadEx().</summary>

        public void LoadEx2()
        {

            if (IsEmpty())

                throw new WikiBotException("The PageList is empty. Nothing to load.");

            Console.WriteLine(Bot.Msg("Loading {0} pages..."), pages.Count);

            string res = site.site + site.indexPath +

                "index.php?title=Special:Export&action=submit";

            string postData = "curonly=True&pages=";

            foreach (Page page in pages)

                postData += HttpUtility.UrlEncode(page.title + "\r\n");

            string src = site.PostDataAndGetResultHTM(res, postData);

            src = Bot.RemoveXMLRootAttributes(src);

            StringReader strReader = new StringReader(src);

            XPathDocument doc = new XPathDocument(strReader);

            strReader.Close();

            XPathNavigator nav = doc.CreateNavigator();

            foreach (Page page in pages)
            {

                if (page.title.Contains("'"))
                {

                    page.LoadEx();

                    continue;

                }

                string query = "//page[title='" + page.title + "']/";

                try
                {

                    page.text =

                        nav.SelectSingleNode(query + "revision/text").InnerXml;

                }

                catch (System.NullReferenceException)
                {

                    continue;

                }

                page.text = HttpUtility.HtmlDecode(page.text);

                page.pageID = nav.SelectSingleNode(query + "id").InnerXml;

                try
                {

                    page.lastUser = nav.SelectSingleNode(query +

                        "revision/contributor/username").InnerXml;

                    page.lastUserID = nav.SelectSingleNode(query +

                        "revision/contributor/id").InnerXml;

                }

                catch (System.NullReferenceException)
                {

                    page.lastUser = nav.SelectSingleNode(query +

                        "revision/contributor/ip").InnerXml;

                }

                page.lastUser = HttpUtility.HtmlDecode(page.lastUser);

                page.lastRevisionID = nav.SelectSingleNode(query + "revision/id").InnerXml;

                page.lastMinorEdit = (nav.SelectSingleNode(query +

                    "revision/minor") == null) ? false : true;

                try
                {

                    page.comment = nav.SelectSingleNode(query + "revision/comment").InnerXml;

                    page.comment = HttpUtility.HtmlDecode(page.comment);

                }

                catch (System.NullReferenceException) { ;}

                page.timestamp = nav.SelectSingleNode(query + "revision/timestamp").ValueAsDateTime;

            }

            Console.WriteLine(Bot.Msg("Pages download completed."));

        }



        /// <summary>Loads text and metadata for pages in PageList via XML export interface.

        /// The function loads pages one by one, it is slightly less efficient

        /// than LoadEx().</summary>

        public void LoadEx3()
        {

            if (IsEmpty())

                throw new WikiBotException("The PageList is empty. Nothing to load.");

            foreach (Page p in pages)

                p.LoadEx();

        }



        /// <summary>Gets page titles and page text from local XML dump.

        /// This function consumes much resources.</summary>

        /// <param name="filePathName">The path to and name of the XML dump file as string.</param>

        public void FillAndLoadFromXMLDump(string filePathName)
        {

            Console.WriteLine(Bot.Msg("Loading pages from XML dump..."));

            XmlReader reader = XmlReader.Create(filePathName);

            while (reader.ReadToFollowing("page"))
            {

                Page p = new Page(site, "");

                p.ParsePageXML(reader.ReadOuterXml());

                pages.Add(p);

            }

            reader.Close();

            Console.WriteLine(Bot.Msg("XML dump loaded successfully."));

        }



        /// <summary>Gets page titles and page texts from all ".txt" files in the specified

        /// directory (folder). Each file becomes a page. Page titles are constructed from

        /// file names. Page text is read from file contents. If any Unicode numeric codes

        /// (also known as numeric character references or NCRs) of the forbidden characters

        /// (forbidden in filenames) are recognized in filenames, those codes are converted

        /// to characters (e.g. "&#x7c;" is converted to "|").</summary>

        /// <param name="dirPath">The path and name of a directory (folder)

        /// to load files from.</param>

        public void FillAndLoadFromFiles(string dirPath)
        {

            foreach (string fileName in Directory.GetFiles(dirPath, "*.txt"))
            {

                Page p = new Page(site, Path.GetFileNameWithoutExtension(fileName));

                p.title = p.title.Replace("&#x22;", "\"");

                p.title = p.title.Replace("&#x3c;", "<");

                p.title = p.title.Replace("&#x3e;", ">");

                p.title = p.title.Replace("&#x3f;", "?");

                p.title = p.title.Replace("&#x3a;", ":");

                p.title = p.title.Replace("&#x5c;", "\\");

                p.title = p.title.Replace("&#x2f;", "/");

                p.title = p.title.Replace("&#x2a;", "*");

                p.title = p.title.Replace("&#x7c;", "|");

                p.LoadFromFile(fileName);

                pages.Add(p);

            }

        }



        /// <summary>Saves all pages in PageList to live wiki site. Uses default bot

        /// edit comment and default minor edit mark setting ("true" by default). This function

        /// doesn't limit the saving speed, so in case of working on public wiki, it's better

        /// to use SaveSmoothly function in order not to overload public server (HTTP errors or

        /// framework errors may arise in case of overloading).</summary>

        public void Save()
        {

            Save(Bot.editComment, Bot.isMinorEdit);

        }



        /// <summary>Saves all pages in PageList to live wiki site. This function

        /// doesn't limit the saving speed, so in case of working on public wiki, it's better

        /// to use SaveSmoothly function in order not to overload public server (HTTP errors or

        /// framework errors may arise in case of overloading).</summary>

        /// <param name="comment">Your edit comment.</param>

        /// <param name="isMinorEdit">Minor edit mark (true = minor edit).</param>

        public void Save(string comment, bool isMinorEdit)
        {

            foreach (Page page in pages)

                page.Save(page.text, comment, isMinorEdit);

        }



        /// <summary>Saves all pages in PageList to live wiki site. The function waits for 5 seconds

        /// between each page save operation in order not to overload server. Uses default bot

        /// edit comment and default minor edit mark setting ("true" by default). This function

        /// doesn't limit the saving speed, so in case of working on public wiki, it's better

        /// to use SaveSmoothly function in order not to overload public server (HTTP errors or

        /// framework errors may arise in case of overloading).</summary>

        public void SaveSmoothly()
        {

            SaveSmoothly(5, Bot.editComment, Bot.isMinorEdit);

        }



        /// <summary>Saves all pages in PageList to live wiki site. The function waits for specified

        /// number of seconds between each page save operation in order not to overload server.

        /// Uses default bot edit comment and default minor edit mark setting

        /// ("true" by default).</summary>

        /// <param name="intervalSeconds">Number of seconds to wait between each

        /// save operation.</param>

        public void SaveSmoothly(int intervalSeconds)
        {

            SaveSmoothly(intervalSeconds, Bot.editComment, Bot.isMinorEdit);

        }



        /// <summary>Saves all pages in PageList to live wiki site. The function waits for specified

        /// number of seconds between each page save operation in order not to overload

        /// server.</summary>

        /// <param name="intervalSeconds">Number of seconds to wait between each

        /// save operation.</param>

        /// <param name="comment">Your edit comment.</param>

        /// <param name="isMinorEdit">Minor edit mark (true = minor edit).</param>

        public void SaveSmoothly(int intervalSeconds, string comment, bool isMinorEdit)
        {

            if (intervalSeconds == 0)

                intervalSeconds = 1;

            foreach (Page page in pages)
            {

                Thread.Sleep(intervalSeconds * 1000);

                page.Save(page.text, comment, isMinorEdit);

            }

        }



        /// <summary>Undoes the last edit of every page in this PageList, so every page text reverts

        /// to previous contents. The function doesn't affect other operations

        /// like renaming.</summary>

        /// <param name="comment">Your edit comment.</param>

        /// <param name="isMinorEdit">Minor edit mark (true = minor edit).</param>

        public void Revert(string comment, bool isMinorEdit)
        {

            foreach (Page page in pages)

                page.Revert(comment, isMinorEdit);

        }



        /// <summary>Saves titles of all pages in PageList to the specified file. Each title

        /// on a new line. If the target file already exists, it is overwritten.</summary>

        /// <param name="filePathName">The path to and name of the target file as string.</param>

        public void SaveTitlesToFile(string filePathName)
        {

            SaveTitlesToFile(filePathName, false);

        }



        /// <summary>Saves titles of all pages in PageList to the specified file. Each title

        /// on a separate line. If the target file already exists, it is overwritten.</summary>

        /// <param name="filePathName">The path to and name of the target file as string.</param>

        /// <param name="useSquareBrackets">If true, each page title is enclosed

        /// in square brackets.</param>

        public void SaveTitlesToFile(string filePathName, bool useSquareBrackets)
        {

            StringBuilder titles = new StringBuilder();

            foreach (Page page in pages)

                titles.Append(useSquareBrackets ?

                    "[[" + page.title + "]]\r\n" : page.title + "\r\n");

            File.WriteAllText(filePathName, titles.ToString().Trim(), Encoding.UTF8);

            Console.WriteLine(Bot.Msg("Titles in PageList saved to \"{0}\" file."), filePathName);

        }



        /// <summary>Saves the contents of all pages in pageList to ".txt" files in specified

        /// directory. Each page is saved to separate file, the name of that file is constructed

        /// from page title. Forbidden characters in filenames are replaced with their

        /// Unicode numeric codes (also known as numeric character references or NCRs).

        /// If the target file already exists, it is overwritten.</summary>

        /// <param name="dirPath">The path and name of a directory (folder)

        /// to save files to.</param>

        public void SaveToFiles(string dirPath)
        {

            string curDirPath = Directory.GetCurrentDirectory();

            Directory.SetCurrentDirectory(dirPath);

            foreach (Page page in pages)

                page.SaveToFile();

            Directory.SetCurrentDirectory(curDirPath);

        }



        /// <summary>Loads the contents of all pages in pageList from live site via XML export

        /// and saves the retrieved XML content to the specified file. The functions just dumps

        /// data, it does not load pages in PageList itself, call LoadEx() or

        /// FillAndLoadFromXMLDump() to do that. Note, that on some sites, MediaWiki messages

        /// from standard namespace 8 are not available for export.</summary>

        /// <param name="filePathName">The path to and name of the target file as string.</param>

        public void SaveXMLDumpToFile(string filePathName)
        {

            Console.WriteLine(Bot.Msg("Loading {0} pages for XML dump..."), this.pages.Count);

            string res = site.site + site.indexPath +

                "index.php?title=Special:Export&action=submit";

            string postData = "catname=&curonly=true&action=submit&pages=";

            foreach (Page page in pages)

                postData += HttpUtility.UrlEncode(page.title + "\r\n");

            string rawXML = site.PostDataAndGetResultHTM(res, postData);

            rawXML = Bot.RemoveXMLRootAttributes(rawXML).Replace("\n", "\r\n");

            if (File.Exists(filePathName))

                File.Delete(filePathName);

            FileStream fs = File.Create(filePathName);

            byte[] XMLBytes = new System.Text.UTF8Encoding(true).GetBytes(rawXML);

            fs.Write(XMLBytes, 0, XMLBytes.Length);

            fs.Close();

            Console.WriteLine(

                Bot.Msg("XML dump successfully saved in \"{0}\" file."), filePathName);

        }



        /// <summary>Removes all empty pages from PageList. But firstly don't forget to load

        /// the pages from site using pageList.LoadEx().</summary>

        public void RemoveEmpty()
        {

            for (int i = pages.Count - 1; i >= 0; i--)

                if (pages[i].IsEmpty())

                    pages.RemoveAt(i);

        }



        /// <summary>Removes all recurring pages from PageList. Only one page with some title will

        /// remain in PageList. This makes all page elements in PageList unique.</summary>

        public void RemoveRecurring()
        {

            for (int i = pages.Count - 1; i >= 0; i--)

                for (int j = i - 1; j >= 0; j--)

                    if (pages[i].title == pages[j].title)
                    {

                        pages.RemoveAt(i);

                        break;

                    }

        }



        /// <summary>Removes all redirecting pages from PageList. But firstly don't forget to load

        /// the pages from site using pageList.LoadEx().</summary>

        public void RemoveRedirects()
        {

            for (int i = pages.Count - 1; i >= 0; i--)

                if (pages[i].IsRedirect())

                    pages.RemoveAt(i);

        }



        /// <summary>For all redirecting pages in this PageList, this function loads the titles and

        /// texts of redirected-to pages.</summary>

        public void ResolveRedirects()
        {

            foreach (Page page in pages)
            {

                if (page.IsRedirect() == false)

                    continue;

                page.title = page.RedirectsTo();

                page.Load();

            }

        }



        /// <summary>Removes all disambiguation pages from PageList. But firstly don't

        /// forget to load the pages from site using pageList.LoadEx().</summary>

        public void RemoveDisambigs()
        {

            for (int i = pages.Count - 1; i >= 0; i--)

                if (pages[i].IsDisambig())

                    pages.RemoveAt(i);

        }





        /// <summary>Removes all pages from PageList.</summary>

        public void RemoveAll()
        {

            pages.Clear();

        }



        /// <summary>Removes all pages from PageList.</summary>

        public void Clear()
        {

            pages.Clear();

        }



        /// <summary>Function changes default English namespace prefixes to correct local prefixes

        /// (e.g. for German wiki-sites it changes "Category:..." to "Kategorie:...").</summary>

        public void CorrectNSPrefixes()
        {

            foreach (Page p in pages)

                p.CorrectNSPrefix();

        }



        /// <summary>Shows if there are any Page objects in this PageList.</summary>

        /// <returns>Returns bool value.</returns>

        public bool IsEmpty()
        {

            return (pages.Count == 0) ? true : false;

        }



        /// <summary>Sends titles of all contained pages to console.</summary>

        public void ShowTitles()
        {

            Console.WriteLine("\n" + Bot.Msg("Pages in this PageList:"));

            foreach (Page p in pages)

                Console.WriteLine(p.title);

            Console.WriteLine("\n");

        }



        /// <summary>Sends texts of all contained pages to console.</summary>

        public void ShowTexts()
        {

            Console.WriteLine("\n" + Bot.Msg("Texts of all pages in this PageList:"));

            Console.WriteLine("--------------------------------------------------");

            foreach (Page p in pages)
            {

                p.ShowText();

                Console.WriteLine("--------------------------------------------------");

            }

            Console.WriteLine("\n");

        }

    }



    /// <summary>Class establishes custom application exceptions.</summary>

    [ClassInterface(ClassInterfaceType.AutoDispatch)]

    [Serializable]

    public class WikiBotException : System.Exception
    {

        /// <summary>Just overriding default constructor.</summary>

        /// <returns>Returns Exception object.</returns>

        public WikiBotException() { }

        /// <summary>Just overriding constructor.</summary>

        /// <returns>Returns Exception object.</returns>

        public WikiBotException(string message)

            : base(message) { Console.Beep(); /*Console.ForegroundColor = ConsoleColor.Red;*/ }

        /// <summary>Just overriding constructor.</summary>

        /// <returns>Returns Exception object.</returns>

        public WikiBotException(string message, System.Exception inner)

            : base(message, inner) { }

        /// <summary>Just overriding constructor.</summary>

        /// <returns>Returns Exception object.</returns>

        protected WikiBotException(System.Runtime.Serialization.SerializationInfo info,

            System.Runtime.Serialization.StreamingContext context)

            : base(info, context) { }

        /// <summary>Destructor is invoked automatically when exception object becomes

        /// inaccessible.</summary>

        ~WikiBotException() { }

    }



    /// <summary>Class defines custom XML URL resolver, that has a caching capability. See

    /// http://www.w3.org/blog/systeam/2008/02/08/w3c_s_excessive_dtd_traffic for details.</summary>

    //[PermissionSetAttribute(SecurityAction.InheritanceDemand, Name = "FullTrust")]

    [ClassInterface(ClassInterfaceType.AutoDispatch)]

    class XmlUrlResolverWithCache : XmlUrlResolver
    {

        /// <summary>List of cached files absolute URIs.</summary>

        static string[] cachedFilesURIs = {
			"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd",
			"http://www.w3.org/TR/xhtml1/DTD/xhtml-lat1.ent",
			"http://www.w3.org/TR/xhtml1/DTD/xhtml-symbol.ent",
			"http://www.w3.org/TR/xhtml1/DTD/xhtml-special.ent"
			//http://www.mediawiki.org/xml/export-0.4/ http://www.mediawiki.org/xml/export-0.4.xsd
		};

        /// <summary>List of cached files names.</summary>

        static string[] cachedFiles = {
			"xhtml1-transitional.dtd",
			"xhtml-lat1.ent",
			"xhtml-symbol.ent",
			"xhtml-special.ent"
		};

        /// <summary>Local cache directory.</summary>

        static string cacheDir = "Cache" + Path.DirectorySeparatorChar;



        /// <summary>Overriding GetEntity() function to implement local cache.</summary>

        /// <param name="absoluteUri">Absolute URI of requested entity.</param>

        /// <param name="role">User's role for accessing specified URI.</param>

        /// <param name="ofObjectToReturn">Type of object to return.</param>

        /// <returns>Returns object or requested type.</returns>

        public override object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn)
        {

            for (int i = 0; i < XmlUrlResolverWithCache.cachedFilesURIs.Length; i++)

                if (absoluteUri.OriginalString == XmlUrlResolverWithCache.cachedFilesURIs[i])

                    return new FileStream(XmlUrlResolverWithCache.cacheDir +

                        XmlUrlResolverWithCache.cachedFiles[i],

                        FileMode.Open, FileAccess.Read, FileShare.Read);

            return base.GetEntity(absoluteUri, role, ofObjectToReturn);

        }

    }



    /// <summary>Class defines a Bot instance, some configuration settings

    /// and some auxiliary functions.</summary>

    [ClassInterface(ClassInterfaceType.AutoDispatch)]

    public class Bot
    {

        /// <summary>Title and description of web agent.</summary>

        public static readonly string botVer = "DotNetWikiBot";

        /// <summary>Version of DotNetWikiBot Framework.</summary>

        public static readonly Version version = new Version("2.101");

        /// <summary>Desired bot's messages language (ISO 639-1 language code).

        /// If not set explicitly, the language will be detected automatically.</summary>

        /// <example><code>Bot.botMessagesLang = "fr";</code></example>

        public static string botMessagesLang = null;

        /// <summary>Default edit comment. You can set it to what you like.</summary>

        /// <example><code>Bot.editComment = "My default edit comment";</code></example>

        public static string editComment = "Automatic page editing";

        /// <summary>If set to true, all the bot's edits are marked as minor by default.</summary>

        public static bool isMinorEdit = true;

        /// <summary>If true, the bot uses "MediaWiki API" extension

        /// (special MediaWiki bot interface, "api.php"), if it is available.

        /// If false, the bot uses common user interface. True by default.

        /// Set it to false manually, if some problem with bot interface arises on site.</summary>

        /// <example><code>Bot.useBotQuery = false;</code></example>

        public static bool useBotQuery = true;

        /// <summary>Number of times to retry bot action in case of temporary connection failure or

        /// some other common net problems.</summary>

        public static int retryTimes = 3;

        /// <summary>If true, the bot asks user to confirm next Save, RenameTo or Delete operation.

        /// False by default. Set it to true manually, when necessary.</summary>

        /// <example><code>Bot.askConfirm = true;</code></example>

        public static bool askConfirm = false;

        /// <summary>If true, bot only reports errors and warnings. Call EnableSilenceMode

        /// function to enable that mode, don't change this variable's value manually.</summary>

        public static bool silenceMode = false;

        /// <summary>If set to some file name (e.g. "DotNetWikiBot_Report.txt"), the bot

        /// writes all output to that file instead of a console. If no path was specified,

        /// the bot creates that file in it's current directory. File is encoded in UTF-8.

        /// Call EnableLogging function to enable log writing, don't change this variable's

        /// value manually.</summary>

        public static string logFile = null;



        /// <summary>Array, containing localized DotNetWikiBot interface messages.</summary>

        public static SortedDictionary<string, string> messages =

            new SortedDictionary<string, string>();

        /// <summary>Internal web client, that is used to access sites.</summary>

        public static WebClient wc = new WebClient();

        /// <summary>Content type for HTTP header of web client.</summary>

        public static readonly string webContentType = "application/x-www-form-urlencoded";

        /// <summary>If true, assembly is running on Mono framework. If false,

        /// it is running on original Microsoft .NET Framework. This variable is set

        /// automatically, just get it's value, don't change it.</summary>

        public static readonly bool isRunningOnMono = (Type.GetType("Mono.Runtime") != null);

        /// <summary>Initial state of HttpWebRequestElement.UseUnsafeHeaderParsing boolean

        /// configuration setting. 0 means true, 1 means false, 2 means unchanged.</summary>

        public static int unsafeHttpHeaderParsingUsed = 2;



        /// <summary>This constructor is used to generate Bot object.</summary>

        /// <returns>Returns Bot object.</returns>

        static Bot()
        {

            if (botMessagesLang == null)

                botMessagesLang = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

            if (botMessagesLang != "en")

                if (!LoadLocalizedMessages(botMessagesLang))

                    botMessagesLang = "en";

            botVer += "/" + version + " (" + Environment.OSVersion.VersionString + "; " +

                ".NET CLR " + Environment.Version.ToString() + ")";

            ServicePointManager.Expect100Continue = false;

        }



        /// <summary>The destructor is used to uninitialize Bot objects.</summary>

        ~Bot()
        {

            //if (unsafeHttpHeaderParsingUsed != 2)

            //SwitchUnsafeHttpHeaderParsing(unsafeHttpHeaderParsingUsed == 1 ? true : false);

        }



        /// <summary>Call this function to make bot write all output to the specified file
        /// instead of a console. If only error logging is desirable, first call this
        /// function, and after that call EnableSilenceMode function.</summary>
        /// <param name="logFileName">Path and name of a file to write output to.
        /// If no path was specified, the bot creates that file in it's current directory.
        /// File is encoded in UTF-8.</param>
        public static void EnableLogging(string logFileName)
        {

            logFile = logFileName;

            StreamWriter log = File.AppendText(logFile);

            log.AutoFlush = true;

            Console.SetError(log);

            if (!silenceMode)

                Console.SetOut(log);

        }



        /// <summary>Call this function to make bot report only errors and warnings,

        /// no other messages will be displayed or logged.</summary>

        public static void EnableSilenceMode()
        {

            silenceMode = true;

            Console.SetOut(new StringWriter());

        }


        /// <summary>Call this function to disable silent mode previously enabled by
        /// EnableSilenceMode() function.</summary>
        public static void DisableSilenceMode()
        {
            silenceMode = false;
            StreamWriter standardOutput = new StreamWriter(Console.OpenStandardOutput());
            standardOutput.AutoFlush = true;
            Console.SetOut(standardOutput);
        }


        /// <summary>Function loads localized bot interface messages from 

        /// "DotNetWikiBot.i18n.xml" file. Function is called in Bot class constructor, 

        /// but can also be called manually to change interface language at runtime.</summary>

        /// <param name="language">Desired language's ISO 639-1 code.</param>

        /// <returns>Returns false, if messages for specified language were not found.

        /// Returns true on success.</returns>

        public static bool LoadLocalizedMessages(string language)
        {

            if (!File.Exists("DotNetWikiBot.i18n.xml"))
            {

                Console.Error.WriteLine("Localization file \"DotNetWikiBot.i18n.xml\" is missing.");

                return false;

            }

            using (XmlReader reader = XmlReader.Create("DotNetWikiBot.i18n.xml"))
            {

                if (!reader.ReadToFollowing(language))
                {

                    Console.Error.WriteLine("\nLocalized messages not found for language \"{0}\"." +

                        "\nYou can help DotNetWikiBot project by translating the messages in\n" +

                        "\"DotNetWikiBot.i18n.xml\" file and sending it to developers for " +

                        "distribution.\n", language);

                    return false;

                }

                if (!reader.ReadToDescendant("msg"))

                    return false;

                else
                {

                    if (messages.Count > 0)

                        messages.Clear();

                    messages[reader["id"]] = reader.ReadString();

                }

                while (reader.ReadToNextSibling("msg"))

                    messages[reader["id"]] = reader.ReadString();

            }

            return true;

        }



        /// <summary>The function gets localized (translated) form of the specified bot

        /// interface message.</summary>

        /// <param name="message">Message itself, placeholders for substituted parameters are

        /// denoted in curly brackets like {0}, {1}, {2} and so on.</param>

        /// <returns>Returns localized form of the specified bot interface message,

        /// or English form if localized form was not found.</returns>

        public static string Msg(string message)
        {

            if (botMessagesLang == "en")

                return message;

            try
            {

                return messages[message];

            }

            catch (KeyNotFoundException)
            {

                return message;

            }

        }



        /// <summary>This function asks user to confirm next action. The message

        /// "Would you like to proceed (y/n/a)? " is displayed and user response is

        /// evaluated. Make sure to set "askConfirm" variable to "true" before

        /// calling this function.</summary>

        /// <returns>Returns true, if user has confirmed the action.</returns>

        /// <example><code>

        /// if (Bot.askConfirm) {

        ///     Console.Write("Some action on live wiki is going to occur.\n\n");

        ///     if(!Bot.UserConfirms())

        ///         return;

        /// }

        /// </code></example>

        public static bool UserConfirms()
        {

            if (!askConfirm)

                return true;

            ConsoleKeyInfo k;

            Console.Write(Bot.Msg("Would you like to proceed (y/n/a)?") + " ");

            k = Console.ReadKey();

            Console.Write("\n");

            if (k.KeyChar == 'y')

                return true;

            else if (k.KeyChar == 'a')
            {

                askConfirm = false;

                return true;

            }

            else

                return false;

        }



        /// <summary>This auxiliary function counts the occurrences of specified string

        /// in specified text. This count is often needed, but strangely there is no

        /// such function in .NET Framework's String class.</summary>

        /// <param name="text">String to look in.</param>

        /// <param name="str">String to look for.</param>

        /// <param name="ignoreCase">Pass "true" if you need case-insensitive search.

        /// But remember that case-sensitive search is faster.</param>

        /// <returns>Returns the number of found occurrences.</returns>

        /// <example><code>int m = CountMatches("Bot Bot bot", "Bot", false); // =2</code></example>

        public static int CountMatches(string text, string str, bool ignoreCase)
        {

            if (string.IsNullOrEmpty(text))

                throw new ArgumentNullException("text");

            if (string.IsNullOrEmpty(str))

                throw new ArgumentNullException("str");

            int matches = 0;

            int position = 0;

            StringComparison rule = ignoreCase

                ? StringComparison.OrdinalIgnoreCase

                : StringComparison.Ordinal;

            while ((position = text.IndexOf(str, position, rule)) != -1)
            {

                matches++;

                position++;

            }

            return matches;

        }



        /// <summary>This auxiliary function returns the zero-based indexes of all occurrences

        /// of specified string in specified text.</summary>

        /// <param name="text">String to look in.</param>

        /// <param name="str">String to look for.</param>

        /// <param name="ignoreCase">Pass "true" if you need case-insensitive search.

        /// But remember that case-sensitive search is faster.</param>

        /// <returns>Returns the List of positions (zero-based integer indexes) of all found

        /// instances, or empty List if nothing was found.</returns>

        public static List<int> GetMatchesPositions(string text, string str, bool ignoreCase)
        {

            if (string.IsNullOrEmpty(text))

                throw new ArgumentNullException("text");

            if (string.IsNullOrEmpty(str))

                throw new ArgumentNullException("str");

            List<int> positions = new List<int>();

            StringComparison rule = ignoreCase

                ? StringComparison.OrdinalIgnoreCase

                : StringComparison.Ordinal;

            int position = 0;

            while ((position = text.IndexOf(str, position, rule)) != -1)
            {

                positions.Add(position);

                position++;

            }

            return positions;

        }



        /// <summary>This auxiliary function returns portion of the string which begins

        /// with some specified substring and ends with some specified substring.</summary>

        /// <param name="src">Source string.</param>

        /// <param name="startMark">Substring that the resultant string portion

        /// must begin with. Can be null.</param>

        /// <param name="endMark">Substring that the resultant string portion

        /// must end with. Can be null.</param>

        /// <returns>Final portion of the source string.</returns>

        public static string GetStringPortion(string src, string startMark, string endMark)
        {

            return GetStringPortionEx(src, startMark, endMark, false, false, true);

        }



        /// <summary>This auxiliary function returns portion of the string which begins

        /// with some specified substring and ends with some specified substring.</summary>

        /// <param name="src">Source string.</param>

        /// <param name="startMark">Substring that the resultant string portion

        /// must begin with. Can be null.</param>

        /// <param name="endMark">Substring that the resultant string portion

        /// must end with. Can be null.</param>

        /// <param name="removeStartMark">Don't include startMark in returned substring.

        /// Default is false.</param>

        /// <param name="removeEndMark">Don't include endMark in returned substring.

        /// Default is false.</param>

        /// <param name="raiseExceptionOnError">Raise ArgumentOutOfRangeException if specified

        /// startMark or endMark was not found. Default is true.</param>

        /// <returns>Final portion of the source string.</returns>

        public static string GetStringPortionEx(string src, string startMark, string endMark,

            bool removeStartMark, bool removeEndMark, bool raiseExceptionOnError)
        {

            if (string.IsNullOrEmpty(src))

                throw new ArgumentNullException("src");

            int startPos = 0;

            int endPos = src.Length;



            if (!string.IsNullOrEmpty(startMark))
            {

                startPos = src.IndexOf(startMark);

                if (startPos == -1)
                {

                    if (raiseExceptionOnError == true)

                        throw new ArgumentOutOfRangeException("startPos");

                    else

                        startPos = 0;

                }

                else if (removeStartMark)

                    startPos += startMark.Length;

            }



            if (!string.IsNullOrEmpty(endMark))
            {

                endPos = src.IndexOf(endMark, startPos);

                if (endPos == -1)
                {

                    if (raiseExceptionOnError == true)

                        throw new ArgumentOutOfRangeException("endPos");

                    else

                        endPos = src.Length;

                }

                else if (!removeEndMark)

                    endPos += endMark.Length;

            }



            return src.Substring(startPos, endPos - startPos);

        }



        /// <summary>This auxiliary function makes the first letter in specified string upper-case.

        /// This is often needed, but strangely there is no such function in .NET Framework's

        /// String class.</summary>

        /// <param name="str">String to capitalize.</param>

        /// <returns>Returns capitalized string.</returns>

        public static string Capitalize(string str)
        {

            return char.ToUpper(str[0]) + str.Substring(1);

        }



        /// <summary>This auxiliary function makes the first letter in specified string lower-case.

        /// This is often needed, but strangely there is no such function in .NET Framework's

        /// String class.</summary>

        /// <param name="str">String to uncapitalize.</param>

        /// <returns>Returns uncapitalized string.</returns>

        public static string Uncapitalize(string str)
        {

            return char.ToLower(str[0]) + str.Substring(1);

        }



        /// <summary>Suspends execution for specified number of seconds.</summary>

        /// <param name="seconds">Number of seconds to wait.</param>

        public static void Wait(int seconds)
        {

            Thread.Sleep(seconds * 1000);

        }



        /// <summary>This internal function switches unsafe HTTP headers parsing on or off.

        /// This is needed to ignore unimportant HTTP protocol violations,

        /// committed by misconfigured web servers.</summary>

        public static void SwitchUnsafeHttpHeaderParsing(bool enabled)
        {

            System.Configuration.Configuration config =

                System.Configuration.ConfigurationManager.OpenExeConfiguration(

                    System.Configuration.ConfigurationUserLevel.None);

            System.Net.Configuration.SettingsSection section =

                (System.Net.Configuration.SettingsSection)config.GetSection("system.net/settings");

            if (unsafeHttpHeaderParsingUsed == 2)

                unsafeHttpHeaderParsingUsed = section.HttpWebRequest.UseUnsafeHeaderParsing ? 1 : 0;

            section.HttpWebRequest.UseUnsafeHeaderParsing = enabled;

            config.Save();

            System.Configuration.ConfigurationManager.RefreshSection("system.net/settings");

        }



        /// <summary>This internal function clears the CanonicalizeAsFilePath attribute in

        /// .NET UriParser to fix a major .NET bug when System.Uri incorrectly strips trailing 

        /// dots in URIs. The bug was discussed in details at:

        /// https://connect.microsoft.com/VisualStudio/feedback/details/386695/system-uri-in

        /// </summary>

        public static void DisableCanonicalizingUriAsFilePath()
        {

            MethodInfo getSyntax = typeof(UriParser).GetMethod("GetSyntax",

                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

            FieldInfo flagsField = typeof(UriParser).GetField("m_Flags",

                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            if (getSyntax != null && flagsField != null)
            {

                foreach (string scheme in new string[] { "http", "https" })
                {

                    UriParser parser = (UriParser)getSyntax.Invoke(null, new object[] { scheme });

                    if (parser != null)
                    {

                        int flagsValue = (int)flagsField.GetValue(parser);

                        // Clear the CanonicalizeAsFilePath attribute

                        if ((flagsValue & 0x1000000) != 0)

                            flagsField.SetValue(parser, flagsValue & ~0x1000000);

                    }

                }

            }

        }



        /// <summary>This internal function removes all attributes from root XML/XHTML element

        /// (XML namespace declarations, schema links, etc.) for easy processing.</summary>

        /// <returns>Returns document without unnecessary declarations.</returns>

        public static string RemoveXMLRootAttributes(string xmlSource)
        {

            int startPos = ((xmlSource.StartsWith("<!") || xmlSource.StartsWith("<?"))

                && xmlSource.IndexOf('>') != -1) ? xmlSource.IndexOf('>') + 1 : 0;

            int firstSpacePos = xmlSource.IndexOf(' ', startPos);

            int firstCloseTagPos = xmlSource.IndexOf('>', startPos);

            if (firstSpacePos != -1 && firstCloseTagPos != -1 && firstSpacePos < firstCloseTagPos)

                return xmlSource.Remove(firstSpacePos, firstCloseTagPos - firstSpacePos);

            return xmlSource;

        }



        /// <summary>This internal function initializes web client to get resources

        /// from web.</summary>

        public static void InitWebClient()
        {

            if (!Bot.isRunningOnMono)

                wc.UseDefaultCredentials = true;

            wc.Encoding = Encoding.UTF8;

            wc.Headers.Add("Content-Type", webContentType);

            wc.Headers.Add("User-agent", botVer);

        }



        /// <summary>This internal wrapper function gets web resource in a fault-tolerant manner.

        /// It should be used only in simple cases, because it sends no cookies, it doesn't support

        /// traffic compression and lacks other special features.</summary>

        /// <param name="address">Web resource address.</param>

        /// <param name="postData">Data to post with web request, can be "" or null.</param>

        /// <returns>Returns web resource as text.</returns>

        public static string GetWebResource(Uri address, string postData)
        {

            string webResourceText = null;

            for (int errorCounter = 0; true; errorCounter++)
            {

                try
                {

                    Bot.InitWebClient();

                    if (string.IsNullOrEmpty(postData))

                        webResourceText = Bot.wc.DownloadString(address);

                    else

                        webResourceText = Bot.wc.UploadString(address, postData);

                    break;

                }

                catch (WebException e)
                {

                    if (errorCounter > retryTimes)

                        throw;

                    string message = e.Message;

                    if (Regex.IsMatch(message, ": \\(50[02349]\\) "))
                    {		// Remote problem

                        Console.Error.WriteLine(message + " " + Bot.Msg("Retrying in 60 seconds."));

                        Thread.Sleep(60000);

                    }

                    else if (message.Contains("Section=ResponseStatusLine"))
                    {	// Squid problem

                        SwitchUnsafeHttpHeaderParsing(true);

                        Console.Error.WriteLine(message + " " + Bot.Msg("Retrying in 60 seconds."));

                        Thread.Sleep(60000);

                    }

                    else

                        throw;

                }

            }

            return webResourceText;

        }

    }

}