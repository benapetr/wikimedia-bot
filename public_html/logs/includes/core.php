<?

require("config.php");
class Logs
{
    public static $title = "Wikimedia irc log browser";
    public static $page = "index";
    public static $mysql;
    public static function Init()
    {
        global $mysql_db, $mysql_user, $mysql_host, $mysql_pw;
        self::$mysql = mysql_connect($mysql_host, $mysql_user, $mysql_pw);
        mysql_select_db($mysql_db);
    }

    private static function GetChannels()
    {
        $list = array();
        $query = mysql_query("SELECT DISTINCT(channel) FROM logs");
        while($item = mysql_fetch_assoc($query))
        {
            $list[] = $item["channel"];
        }
        return $list;
    }

    private static function RenderMenu()
    {
        echo ("    <b>Channels:</b><br>\n<ul>\n");
        $channels = self::GetChannels();
        foreach ($channels as $channel)
        {
            echo ("<li><a href=\"index.php?display=$channel\">$channel</a></li>\n");
        }
        echo ( "</ul>" );
    }

    private static function RenderIndex()
    {
        echo("    <p>This is a wikimedia logs browser, please pick a channel from menu on left side. This page is open source, if you don't like it, please fix it instead of complains!</p>");
    }

    private static function RenderContent()
    {
        switch (self::$page)
        {
            case "index":
                self::RenderIndex();
                return;
        }
    }

    public static function Render()
    {
        echo ("<!DOCTYPE html>\n<HTML>\n<head>\n<title>" . self::$title ."</title>");
        echo ("  <meta http-equiv=\"content-type\" content=\"text/html; charset=UTF-8\">\n");
	echo ("  <link rel=\"stylesheet\" type=\"text/css\" href=\"./style/style.css\">\n");
	echo ("  <link href=\"./js/jquery-ui.css\" rel=\"stylesheet\" type=\"text/css\"/>\n");
    	echo ("  <script src=\"http://ajax.googleapis.com/ajax/libs/jquery/1.5/jquery.min.js\"></script>\n");
    	echo ("  <script src=\"http://ajax.googleapis.com/ajax/libs/jqueryui/1.8/jquery-ui.min.js\"></script>\n");
	echo ("  <script src=\"js/scripts.js\" type=\"text/javascript\"></script>\n");
	echo ("  <meta name=\"viewport\" content=\"width=device-width; initial-scale=1.0\">\n");
        echo ("</head>\n<body>\n<div class='bkg1'>");
        echo ("<table>\n  <tr>    <td width=200>\n"); 
        self::RenderMenu();
        echo ("    </td>\n    <td>");
        self::RenderContent();
        echo ("    </td>\n  </tr>\n</table>\n");
        echo ("</div>\n</body>\n</HTML>");
        mysql_close(self::$mysql);
    }
}
